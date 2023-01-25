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
using System.Runtime.CompilerServices;
using BWAPI.NET;

namespace BWEM.NET
{
    public static class Ex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int QueenWiseNorm(int dx, int dy)
        {
            return Math.Max(Math.Abs(dx), Math.Abs(dy));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SquaredNorm(int dx, int dy)
        {
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Norm(int dx, int dy)
        {
            return Math.Sqrt(SquaredNorm(dx, dy));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ScalarProduct(int ax, int ay, int bx, int by)
        {
            return ax * bx + ay * by;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position Center<T>(T p)
            where T : IPoint<T>
        {
            return PointHelper.New<T, Position>(p).Add(PointHelper.GetScale<T>() / 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position Center(Position p)
        {
            return p + Position.Scale / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position Center(WalkPosition p)
        {
            return new Position(p) + WalkPosition.Scale / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position Center(TilePosition p)
        {
            return new Position(p) + TilePosition.Scale / 2;
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <typeparam name="T">The type of the point.</typeparam>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="newTopLeft">The new top left point of the bounding box.</param>
        /// <param name="newBottomRight">The initial bottom right point of the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakeBoundingBoxIncludePoint<T>(T topLeft, T bottomRight, T a, out T newTopLeft, out T newBottomRight)
            where T : IPoint<T>
        {
            newTopLeft = PointHelper.New<T>(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y));
            newBottomRight = PointHelper.New<T>(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y));
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="newTopLeft">The new top left point of the bounding box.</param>
        /// <param name="newBottomRight">The initial bottom right point of the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakeBoundingBoxIncludePoint(Position topLeft, Position bottomRight, Position a, out Position newTopLeft, out Position newBottomRight)
        {
            newTopLeft = new Position(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y));
            newBottomRight = new Position(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y));
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="newTopLeft">The new top left point of the bounding box.</param>
        /// <param name="newBottomRight">The initial bottom right point of the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakeBoundingBoxIncludePoint(WalkPosition topLeft, WalkPosition bottomRight, WalkPosition a, out WalkPosition newTopLeft, out WalkPosition newBottomRight)
        {
            newTopLeft = new WalkPosition(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y));
            newBottomRight = new WalkPosition(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y));
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="newTopLeft">The new top left point of the bounding box.</param>
        /// <param name="newBottomRight">The initial bottom right point of the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakeBoundingBoxIncludePoint(TilePosition topLeft, TilePosition bottomRight, TilePosition a, out TilePosition newTopLeft, out TilePosition newBottomRight)
        {
            newTopLeft = new TilePosition(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y));
            newBottomRight = new TilePosition(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y));
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <typeparam name="T">The type of the point.</typeparam>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <returns>A tuple with the top left and bottom right points for the new bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T newTopLeft, T newBottomRight) MakeBoundingBoxIncludePoint<T>(T topLeft, T bottomRight, T a)
            where T : IPoint<T>
        {
            return (
                PointHelper.New<T>(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y)),
                PointHelper.New<T>(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y))
            );
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <returns>A tuple with the top left and bottom right points for the new bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Position newTopLeft, Position newBottomRight) MakeBoundingBoxIncludePoint(Position topLeft, Position bottomRight, Position a)
        {
            return (
                new Position(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y)),
                new Position(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y))
            );
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <returns>A tuple with the top left and bottom right points for the new bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (WalkPosition newTopLeft, WalkPosition newBottomRight) MakeBoundingBoxIncludePoint(WalkPosition topLeft, WalkPosition bottomRight, WalkPosition a)
        {
            return (
                new WalkPosition(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y)),
                new WalkPosition(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y))
            );
        }

        /// <summary>
        /// Enlarges the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>] so that it includes <paramref name="a"/>.
        /// It left the new bounding box into the [<paramref name="newTopLeft"/>, <paramref name="newBottomRight"/>] arguments.
        /// </summary>
        /// <param name="topLeft">The initial top left point of the bounding box.</param>
        /// <param name="bottomRight">The initial bottom right point of the bounding box.</param>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <returns>A tuple with the top left and bottom right points for the new bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (TilePosition newTopLeft, TilePosition newBottomRight) MakeBoundingBoxIncludePoint(TilePosition topLeft, TilePosition bottomRight, TilePosition a)
        {
            return (
                new TilePosition(Math.Min(a.X, topLeft.X), Math.Min(a.Y, topLeft.Y)),
                new TilePosition(Math.Max(a.X, bottomRight.X), Math.Max(a.Y, bottomRight.Y))
            );
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <typeparam name="T">The type of the point.</typeparam>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <param name="newA">The new point included in the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakePointFitToBoundingBox<T>(T a, T topLeft, T bottomRight, out T newA)
            where T : IPoint<T>
        {
            newA = PointHelper.New<T>(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <param name="newA">The new point included in the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakePointFitToBoundingBox(Position a, Position topLeft, Position bottomRight, out Position newA)
        {
            newA = new Position(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <param name="newA">The new point included in the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakePointFitToBoundingBox(WalkPosition a, WalkPosition topLeft, WalkPosition bottomRight, out WalkPosition newA)
        {
            newA = new WalkPosition(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <param name="newA">The new point included in the bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakePointFitToBoundingBox(TilePosition a, TilePosition topLeft, TilePosition bottomRight, out TilePosition newA)
        {
            newA = new TilePosition(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <typeparam name="T">The type of the point.</typeparam>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <returns>The new point included in the bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MakePointFitToBoundingBox<T>(T a, T topLeft, T bottomRight)
            where T : IPoint<T>
        {
            return PointHelper.New<T>(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <returns>The new point included in the bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position MakePointFitToBoundingBox(Position a, Position topLeft, Position bottomRight)
        {
            return new Position(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <returns>The new point included in the bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WalkPosition MakePointFitToBoundingBox(WalkPosition a, WalkPosition topLeft, WalkPosition bottomRight)
        {
            return new WalkPosition(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        /// <summary>
        /// Makes the smallest change to <paramref name="a"/> so that it is included in the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <param name="a">The point to be included in the bounding box.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <returns>The new point included in the bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TilePosition MakePointFitToBoundingBox(TilePosition a, TilePosition topLeft, TilePosition bottomRight)
        {
            return new TilePosition(Math.Clamp(a.X, topLeft.X, bottomRight.X), Math.Clamp(a.Y, topLeft.Y, bottomRight.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InBoundingBox<T>(T a, T topLeft, T bottomRight)
            where T : IPoint<T>
        {
            return (a.X >= topLeft.X) && (a.X <= bottomRight.X) && (a.Y >= topLeft.Y) && (a.Y <= bottomRight.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int QueenWiseDist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return QueenWiseNorm(a.X, a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SquaredDist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return SquaredNorm(a.X, a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dist<T>(T a, T b)
            where T : IPoint<T>
        {
            a = a.Subtract(b);
            return Norm(a.X, a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dist(Position a, Position b)
        {
            a -= b;
            return Norm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dist(WalkPosition a, WalkPosition b)
        {
            a -= b;
            return Norm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dist(TilePosition a, TilePosition b)
        {
            a -= b;
            return Norm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquaredDist(Position a, Position b)
        {
            a -= b;
            return SquaredNorm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquaredDist(WalkPosition a, WalkPosition b)
        {
            a -= b;
            return SquaredNorm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquaredDist(TilePosition a, TilePosition b)
        {
            a -= b;
            return SquaredNorm(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedDist<T>(T a, T b)
            where T : IPoint<T>
        {
            return (int)(0.5 + Dist(a, b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedDist(Position a, Position b)
        {
            return (int)(0.5 + Dist(a, b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedDist(WalkPosition a, WalkPosition b)
        {
            return (int)(0.5 + Dist(a, b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedDist(TilePosition a, TilePosition b)
        {
            return (int)(0.5 + Dist(a, b));
        }

        /// <summary>
        /// Returns the distance of the point <paramref name="a"/> to the bounding box [<paramref name="topLeft"/>, <paramref name="bottomRight"/>].
        /// </summary>
        /// <typeparam name="T">The type of the point.</typeparam>
        /// <param name="a">The point to calculate distance from.</param>
        /// <param name="topLeft">The top left point of the bounding box.</param>
        /// <param name="bottomRight">The bottom right point of the bounding box.</param>
        /// <returns>The distance of the point <paramref name="a"/> to the bounding box.</returns>
        public static int DistToRectangle<T>(T a, T topLeft, T bottomRight)
            where T : IPoint<T>
        {
            if (a.X >= topLeft.X)
            {
                if (a.X <= bottomRight.X)
                {
                    if (a.Y > bottomRight.Y) return a.Y - bottomRight.Y;                                            // S
                    else if (a.Y < topLeft.Y) return topLeft.Y - a.Y;                                               // N
                    else return 0;                                                                                  // inside
                }
                else
                {
                    if (a.Y > bottomRight.Y) return RoundedDist(a, bottomRight);                                    // SE
                    else if (a.Y < topLeft.Y) return RoundedDist(a, PointHelper.New<T>(bottomRight.X, topLeft.Y));  // NE
                    else return a.X - bottomRight.X;                                                                // E
                }
            }
            else
            {
                if (a.Y > bottomRight.Y) return RoundedDist(a, PointHelper.New<T>(topLeft.X, bottomRight.Y));       // SW
                else if (a.Y < topLeft.Y) return RoundedDist(a, topLeft);                                           // NW
                else return topLeft.X - a.X;                                                                        // W
            }
        }

        public static int DistToRectangle(Position a, Position topLeft, Position bottomRight)
        {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DistToRectangle(Position a, WalkPosition walkTopLeft, WalkPosition size)
        {
            var topLeft = walkTopLeft.ToPosition();
            var bottomRight = new Position(walkTopLeft + size) - 1;
            return DistToRectangle(a, topLeft, bottomRight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DistToRectangle(Position a, TilePosition tileTopLeft, TilePosition size)
        {
            var topLeft = tileTopLeft.ToPosition();
            var bottomRight = new Position(tileTopLeft + size) - 1;
            return DistToRectangle(a, topLeft, bottomRight);
        }

        public static List<T> InnerBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            var border = new List<T>();

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
                            border.Add(topLeft.Add(PointHelper.New<T>(dx, dy)));
                        }
                    }
                }
            }

            return border;
        }

        public static List<Position> InnerBorder(Position topLeft, Position size, bool noCorner = false)
        {
            var border = new List<Position>();

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
                            border.Add(topLeft + new Position(dx, dy));
                        }
                    }
                }
            }

            return border;
        }

        public static List<WalkPosition> InnerBorder(WalkPosition topLeft, WalkPosition size, bool noCorner = false)
        {
            var border = new List<WalkPosition>();

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
                            border.Add(topLeft + new WalkPosition(dx, dy));
                        }
                    }
                }
            }

            return border;
        }

        public static List<TilePosition> InnerBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            var border = new List<TilePosition>();

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
                            border.Add(topLeft + new TilePosition(dx, dy));
                        }
                    }
                }
            }

            return border;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> OuterBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            return InnerBorder(topLeft.Subtract(1), size.Add(2), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Position> OuterBorder(Position topLeft, Position size, bool noCorner = false)
        {
            return InnerBorder(topLeft - 1, size + 2, noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> OuterBorder(WalkPosition topLeft, WalkPosition size, bool noCorner = false)
        {
            return InnerBorder(topLeft - 1, size + 2, noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TilePosition> OuterBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            return InnerBorder(topLeft - 1, size + 2, noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> OuterMiniTileBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            return OuterBorder(PointHelper.New<T, WalkPosition>(topLeft), PointHelper.New<T, WalkPosition>(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> OuterMiniTileBorder(Position topLeft, Position size, bool noCorner = false)
        {
            return OuterBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> OuterMiniTileBorder(WalkPosition topLeft, WalkPosition size, bool noCorner = false)
        {
            return OuterBorder(topLeft, size, noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> OuterMiniTileBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            return OuterBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> InnerMiniTileBorder<T>(T topLeft, T size, bool noCorner = false)
            where T : IPoint<T>
        {
            return InnerBorder(PointHelper.New<T, WalkPosition>(topLeft), PointHelper.New<T, WalkPosition>(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> InnerMiniTileBorder(Position topLeft, Position size, bool noCorner = false)
        {
            return InnerBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> InnerMiniTileBorder(WalkPosition topLeft, WalkPosition size, bool noCorner = false)
        {
            return InnerBorder(topLeft, size, noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<WalkPosition> InnerMiniTileBorder(TilePosition topLeft, TilePosition size, bool noCorner = false)
        {
            return InnerBorder(new WalkPosition(topLeft), new WalkPosition(size), noCorner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlap<T>(T topLeft1, T size1, T topLeft2, T size2)
            where T : IPoint<T>
        {
            return topLeft2.X < topLeft1.X + size1.X &&
                   topLeft2.Y < topLeft1.Y + size1.Y &&
                   topLeft1.X < topLeft2.X + size2.X &&
                   topLeft1.Y < topLeft2.Y + size2.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Disjoint<T>(T topLeft1, T size1, T topLeft2, T size2)
            where T : IPoint<T>
        {
            return topLeft2.X > topLeft1.X + size1.X ||
                   topLeft2.Y > topLeft1.Y + size1.Y ||
                   topLeft1.X > topLeft2.X + size2.X ||
                   topLeft1.Y > topLeft2.Y + size2.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Crop<T>(T p, int sizeX, int sizeY)
            where T : IPoint<T>
        {
            var resX = p.X;
            var resY = p.Y;

            if (resX < 0) resX = 0;
            else if (resX >= sizeX) resX = sizeX - 1;

            if (resY < 0) resY = 0;
            else if (resY >= sizeY) resY = sizeY - 1;

            return PointHelper.New<T>(resX, resY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position Crop(Position p, int sizeX, int sizeY)
        {
            var resX = p.X;
            var resY = p.Y;

            if (resX < 0) resX = 0;
            else if (resX >= sizeX) resX = sizeX - 1;

            if (resY < 0) resY = 0;
            else if (resY >= sizeY) resY = sizeY - 1;

            return new Position(resX, resY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WalkPosition Crop(WalkPosition p, int sizeX, int sizeY)
        {
            var resX = p.X;
            var resY = p.Y;

            if (resX < 0) resX = 0;
            else if (resX >= sizeX) resX = sizeX - 1;

            if (resY < 0) resY = 0;
            else if (resY >= sizeY) resY = sizeY - 1;

            return new WalkPosition(resX, resY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TilePosition Crop(TilePosition p, int sizeX, int sizeY)
        {
            var resX = p.X;
            var resY = p.Y;

            if (resX < 0) resX = 0;
            else if (resX >= sizeX) resX = sizeX - 1;

            if (resY < 0) resY = 0;
            else if (resY >= sizeY) resY = sizeY - 1;

            return new TilePosition(resX, resY);
        }
    }
}
