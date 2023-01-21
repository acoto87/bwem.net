// Original work Copyright (c) 2015, 2017, Igor Dimitrijevic
// Modified work Copyright (c) 2017-2018 OpenBW Team

//////////////////////////////////////////////////////////////////////////
//
// This file is part of the BWEM Library.
// BWEM is free software, licensed under the MIT/X11 License.
// A copy of the license is provided with the library in the LICENSE file.
// Copyright (c) 2015, 2017, Igor Dimitrijevic
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using BWAPI.NET;

namespace BWEM.NET
{
    public static class Ex
    {
        public static int QueenWiseNorm(int dx, int dy)
        {
            return Math.Max(Math.Abs(dx), Math.Abs(dy));
        }


        public static int SquaredNorm(int dx, int dy)
        {
            return dx * dx + dy * dy;
        }


        public static double Norm(int dx, int dy)
        {
            return Math.Sqrt(SquaredNorm(dx, dy));
        }

        public static int ScalarProduct(int ax, int ay, int bx, int by)
        {
            return ax * bx + ay * by;
        }

        public static bool Intersect(int ax, int ay, int bx, int by, int cx, int cy, int dx, int dy)
        {
            return GetLineIntersection(ax, ay, bx, by, cx, cy, dx, dy, out _, out _);
        }

        // From http://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
        // Returns true if the lines intersect, otherwise false. In addition, if the lines
        // intersect the intersection point may be stored in i_x and i_y.
        public static bool GetLineIntersection(
            double p0_x, double p0_y,
            double p1_x, double p1_y,
            double p2_x, double p2_y,
            double p3_x, double p3_y,
            out double i_x,
            out double i_y)
        {
            double s1_x, s1_y, s2_x, s2_y;

            s1_x = p1_x - p0_x;
            s1_y = p1_y - p0_y;

            s2_x = p3_x - p2_x;
            s2_y = p3_y - p2_y;

            double s, t;
            s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
            t = ( s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected
                i_x = p0_x + (t * s1_x);
                i_y = p0_y + (t * s1_y);
                return true;
            }

            i_x = 0;
            i_y = 0;
            return false; // No collision
        }

        public static Position Center<T>(T p)
            where T : IPoint<T>
        {
            return PointHelperEx.New<T, Position>(p).Add(PointHelperEx.GetScale<T>() / 2);
        }

        // Enlarges the bounding box [TopLeft, BottomRight] so that it includes A.
        public static void MakeBoundingBoxIncludePoint<T>(T topLeft, T bottomRight, T a, out T newTopLeft, out T newBottomRight)
            where T : IPoint<T>
        {
            newTopLeft = PointHelperEx.New<T>(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y));
            newBottomRight = PointHelperEx.New<T>(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y));
        }

        // Enlarges the bounding box [TopLeft, BottomRight] so that it includes A.
        public static (T newTopLeft, T newBottomRight) MakeBoundingBoxIncludePoint<T>(T topLeft, T bottomRight, T a)
            where T : IPoint<T>
        {
            return (
                PointHelperEx.New<T>(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y)),
                PointHelperEx.New<T>(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y))
            );
        }

        // Makes the smallest change to A so that it is included in the bounding box [TopLeft, BottomRight].
        public static void MakePointFitToBoundingBox<T>(T a, T topLeft, T bottomRight, out T newA)
            where T : IPoint<T>
        {
            newA = PointHelperEx.New<T>(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        // Makes the smallest change to A so that it is included in the bounding box [TopLeft, BottomRight].
        public static T MakePointFitToBoundingBox<T>(T a, T topLeft, T bottomRight)
            where T : IPoint<T>
        {
            return PointHelperEx.New<T>(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        public static bool InBoundingBox<T>(T a, T topLeft, T bottomRight)
            where T : IPoint<T>
        {
            return (a.X >= topLeft.X) && (a.X <= bottomRight.X) && (a.Y >= topLeft.Y) && (a.Y <= bottomRight.Y);
        }

        public static int QueenWiseDist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return QueenWiseNorm(a.X, a.Y);
        }

        public static int SquaredDist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return SquaredNorm(a.X, a.Y);
        }

        public static double Dist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return Norm(a.X, a.Y);
        }

        public static double Dist(WalkPosition a, WalkPosition b)
        {
            a -= b;
            return Norm(a.x, a.y);
        }

        public static double Dist(TilePosition a, TilePosition b)
        {
            a -= b;
            return Norm(a.x, a.y);
        }

        public static int RoundedDist<T>(T a, T b)
            where T : IPoint<T>
        {
            return (int)(0.5 + Dist(a, b));
        }

        public static int DistToRectangle(Position a, TilePosition tileTopLeft, TilePosition size)
        {
            var topLeft = tileTopLeft.ToPosition();
            var bottomRight = new Position(tileTopLeft + size) - 1;

            if (a.x >= topLeft.x)
            {
                if (a.x <= bottomRight.x)
                {
                    if (a.y > bottomRight.y) return a.y - bottomRight.y;                                    // S
                    else if (a.y < topLeft.y) return topLeft.y - a.y;                                       // N
                    else return 0;                                                                          // inside
                }
                else
                {
                    if (a.y > bottomRight.y) return RoundedDist(a, bottomRight);                            // SE
                    else if (a.y < topLeft.y) return RoundedDist(a, new Position(bottomRight.x, topLeft.y));// NE
                    else return a.x - bottomRight.x;                                                        // E
                }
            }
            else
            {
                if (a.y > bottomRight.y) return RoundedDist(a, new Position(topLeft.x, bottomRight.y));     // SW
                else if (a.y < topLeft.y) return RoundedDist(a, topLeft);                                   // NW
                else return topLeft.x - a.x;                                                                // W
            }
        }

        public static List<T> InnerBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            var border = new List<T>(); ;
            for (var dy = 0; dy < size.Y; ++dy)
            {
                for (var dx = 0; dx < size.X; ++dx)
                {
                    if ((dy == 0) || (dy == size.Y - 1) || (dx == 0) || (dx == size.X - 1))
                    {
                        if (!noCorner ||
                            !(((dx == 0) && (dy == 0)) || ((dx == size.X - 1) && (dy == size.Y - 1)) ||
                             ((dx == 0) && (dy == size.Y - 1)) || ((dx == size.X - 1) && (dy == 0))))
                        {
                            border.Add(topLeft.Add(PointHelperEx.New<T>(dx, dy)));
                        }
                    }
                }
            }

            return border;
        }

        public static List<T> OuterBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            return InnerBorder(topLeft.Subtract(1), size.Add(2), noCorner);
        }

        public static List<WalkPosition> OuterMiniTileBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            return OuterBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        public static List<WalkPosition> InnerMiniTileBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            return InnerBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        public static bool Overlap<T>(T topLeft1, T size1, T topLeft2, T size2)
            where T : IPoint<T>
        {
            if (topLeft2.X >= topLeft1.X + size1.X) return false;
            if (topLeft2.Y >= topLeft1.Y + size1.Y) return false;
            if (topLeft1.X >= topLeft2.X + size2.X) return false;
            if (topLeft1.Y >= topLeft2.Y + size2.Y) return false;
            return true;
        }

        public static bool Disjoint<T>(T topLeft1, T size1, T topLeft2, T size2)
            where T : IPoint<T>
        {
            if (topLeft2.X > topLeft1.X + size1.X) return true;
            if (topLeft2.Y > topLeft1.Y + size1.Y) return true;
            if (topLeft1.X > topLeft2.X + size2.X) return true;
            if (topLeft1.Y > topLeft2.Y + size2.Y) return true;
            return false;
        }

        public static T Crop<T>(T p, int sizeX, int sizeY)
            where T : IPoint<T>
        {
            var resX = p.X;
            var resY = p.Y;

            if (resX < 0) resX = 0;
            else if (resX >= sizeX) resX = sizeX-1;

            if (resY < 0) resY = 0;
            else if (resY >= sizeY)	resY = sizeY-1;

            return PointHelperEx.New<T>(resX, resY);
        }
    }

    public static class PointHelperEx
    {
        public static T New<T>(int x, int y)
            where T : IPoint<T>
        {
            if (typeof(T) == typeof(Position)) return (T)(new Position(x, y) as IPoint<Position>);
            if (typeof(T) == typeof(WalkPosition)) return (T)(new WalkPosition(x, y) as IPoint<WalkPosition>);
            if (typeof(T) == typeof(TilePosition)) return (T)(new TilePosition(x, y) as IPoint<TilePosition>);
            throw new NotSupportedException("Unknown point type " + typeof(T));
        }

        public static TTo New<TFrom, TTo>(TFrom p)
            where TFrom : IPoint<TFrom>
            where TTo : IPoint<TTo>
        {
            var scaleFrom = GetScale<TFrom>();
            var scaleTo = GetScale<TTo>();
            var x = (int)(p.X * (double)scaleFrom / scaleTo);
            var y = (int)(p.Y * (double)scaleFrom / scaleTo);
            return New<TTo>(x, y);
        }

        public static int GetScale<T>()
            where T : IPoint<T>
        {
            if (typeof(T) == typeof(Position)) return PointHelper.PositionScale;
            if (typeof(T) == typeof(WalkPosition)) return PointHelper.WalkPositionScale;
            if (typeof(T) == typeof(TilePosition)) return PointHelper.TilePositionScale;
            throw new NotSupportedException("Unknown point type " + typeof(T));
        }
    }
}