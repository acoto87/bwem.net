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
using System.Diagnostics;
using System.Linq;
using BWAPI.NET;
using Nito.Collections;

namespace BWEM.NET
{
    public class Graph
    {
        internal readonly Map _map;
        private List<ChokePoint> _chokePoints;
        private List<Area> _areas;
        private List<ChokePoint>[,] _chokePointsMatrix; // index == Area::id x Area::id
        private int[,] _chokePointDistanceMatrix; // index == ChokePoint::index x ChokePoint::index
        private CPPath[,] _pathsBetweenChokePoints; // index == ChokePoint::index x ChokePoint::index
        private List<Base> _bases;

        public Graph(Map map)
        {
            _map = map;
        }

        public Map Map
        {
            get => _map;
        }

        public List<Area> Areas
        {
            get => _areas;
        }

        public int AreasCount
        {
            get => _areas.Count;
        }

        /// <summary>
        /// Returns the list of all the ChokePoints in the Map.
        /// </summary>
	    public List<ChokePoint> ChokePoints
        {
            get => _chokePoints;
        }

        public int BaseCount
        {
            get => _bases.Count;
        }

        public List<Base> Bases
        {
            get => _bases;
        }

        public Area GetArea(AreaId id)
        {
            Debug.Assert(Valid(id));
            return _areas[id.Value - 1];
        }

        public Area GetArea(WalkPosition w)
        {
            var miniTile = _map.GetTile(w);
            return miniTile.AreaId > 0 ? GetArea(miniTile.AreaId) : null;
        }

        public Area GetArea(TilePosition t)
        {
            var tile = _map.GetTile(t);
            return tile.AreaId > 0 ? GetArea(tile.AreaId) : null;
        }

        public Area GetArea<TPosition, TTile>(TPosition p)
            where TPosition : IPoint<TPosition>
            where TTile : ITile
        {
            var tile = _map.GetTile<TPosition, TTile>(p);
            return tile.AreaId > 0 ? GetArea(tile.AreaId) : null;
        }

        public Area GetNearestArea<TPosition, TTile>(TPosition p)
            where TPosition : IPoint<TPosition>
            where TTile : ITile
        {
            // typedef typename TileOfPosition<TPosition>::type Tile_t;
            var area = GetArea<TPosition, TTile>(p);
            if (area != null)
            {
                return area;
            }

            p = _map.BreadthFirstSearch(p,
                (Tile t, TPosition _1) => t.AreaId > 0, // findCond
                (Tile _0, TPosition _1) => true         // visitCond
            );

            return GetArea<TPosition, Tile>(p);
        }

        // Returns the ChokePoints between two Areas.
        public List<ChokePoint> GetChokePoints(AreaId a, AreaId b)
        {
            Debug.Assert(Valid(a));
            Debug.Assert(Valid(b));
            Debug.Assert(a != b);

            if (a > b)
            {
                (a, b) = (b, a);
            }

            return _chokePointsMatrix[b.Value, a.Value];
        }

        // Returns the ground distance in pixels between cpA->Center() and cpB>Center()
        public int Distance(ChokePoint cpA, ChokePoint cpB)
        {
            return _chokePointDistanceMatrix[cpA.Index, cpB.Index];
        }

        // Returns a list of ChokePoints, which is intended to be the shortest walking path from cpA to cpB.
        public CPPath GetPath(ChokePoint cpA, ChokePoint cpB)
        {
            return _pathsBetweenChokePoints[cpA.Index, cpB.Index];
        }

        public CPPath GetPath(Position a, Position b, out int length)
        {
            var areaA = GetNearestArea<WalkPosition, MiniTile>(new WalkPosition(a));
            var areaB = GetNearestArea<WalkPosition, MiniTile>(new WalkPosition(b));

            if (areaA == areaB)
            {
                length = a.GetApproxDistance(b);
                return new CPPath();
            };

            if (!areaA.AccessibleFrom(areaB))
            {
                length = -1;
                return new CPPath();
            };

            var minDistAB = int.MaxValue;

            ChokePoint bestCpA = null;
            ChokePoint bestCpB = null;

            foreach (var cpA in areaA.ChokePoints)
            {
                if (!cpA.Blocked)
                {
                    var distAcpA = a.GetApproxDistance(new Position(cpA.Center));
                    foreach (var cpB in areaB.ChokePoints)
                    {
                        if (!cpB.Blocked)
                        {
                            var distBcpB = b.GetApproxDistance(new Position(cpB.Center));
                            var distAB = distAcpA + distBcpB + Distance(cpA, cpB);
                            if (distAB < minDistAB)
                            {
                                minDistAB = distAB;
                                bestCpA = cpA;
                                bestCpB = cpB;
                            }
                        }
                    }
                }
            }

            Debug.Assert(minDistAB != int.MaxValue);

            var path = GetPath(bestCpA, bestCpB);

            Debug.Assert(path.Count >= 1);

            length = minDistAB;

            if (path.Count == 1)
            {
                Debug.Assert(bestCpA == bestCpB);

                var cp = bestCpA;

                var cpEnd1 = Ex.Center(cp.Pos(ChokePoint.Node.End1));
                var cpEnd2 = Ex.Center(cp.Pos(ChokePoint.Node.End2));
                if (Ex.Intersect(a.x, a.y, b.x, b.y, cpEnd1.x, cpEnd1.y, cpEnd2.x, cpEnd2.y))
                {
                    length = a.GetApproxDistance(b);
                }
                else
                {
                    var nodes = new[] { ChokePoint.Node.End1, ChokePoint.Node.End2 };
                    foreach (var node in nodes)
                    {
                        var center = Ex.Center(cp.Pos(node));
                        var distAB = a.GetApproxDistance(center) + b.GetApproxDistance(center);
                        if (distAB < length)
                        {
                            length = distAB;
                        }
                    }
                }
            }

            return GetPath(bestCpA, bestCpB);
        }

        public List<ChokePoint> GetChokePoints(Area a, Area b)
        {
            return GetChokePoints(a.Id, b.Id);
        }

        // Creates a new Area for each pair (top, miniTiles) in AreasList (See Area::Top() and Area::MiniTiles())
        public void CreateAreas(List<Pair<WalkPosition, int>> areas)
        {
            _areas = new List<Area>(areas.Count);
            for (var id = 1; id <= areas.Count; ++id)
            {
                var (top, miniTiles) = areas[id - 1];
                _areas.Add(new Area(this, (short)id, top, miniTiles));
            }
        }

        // Creates a new Area for each pair (top, miniTiles) in AreasList (See Area::Top() and Area::MiniTiles())
        public void CreateChokePoints()
        {
            var newIndex = 0;

            var blockingNeutrals = new List<Neutral>();

            foreach (var s in _map.StaticBuildings)
            {
                if (s.Blocking)
                {
                    blockingNeutrals.Add(s);
                }
            }

            foreach (var m in _map.Minerals)
            {
                if (m.Blocking)
                {
                    blockingNeutrals.Add(m);
                }
            }

            // var pseudoChokePointsToCreate = blockingNeutrals.Count(n => n.NextStacked == null);

            // 1) Size the matrix
            _chokePointsMatrix = new List<ChokePoint>[_areas.Count + 1, _areas.Count + 1];
            for (var i = 1; i <= _areas.Count; ++i)
            {
                for (var j = 0; j < i; j++)
                {
                    _chokePointsMatrix[i, j] = new List<ChokePoint>();
                }
            }

            // 2) Dispatch the global raw frontier between all the relevant pairs of Areas:
            var rawFrontierByAreaPair = new Dictionary<Pair<AreaId, AreaId>, List<WalkPosition>>();

            foreach (var raw in _map.RawFrontier)
            {
                var a = raw.First.First;
                var b = raw.First.Second;
                if (a > b)
                {
                    (a, b) = (b, a);
                }

                Debug.Assert(a <= b);
                Debug.Assert((a >= 1) && (b <= AreasCount));

                var pair = new Pair<AreaId, AreaId>(a, b);
                if (!rawFrontierByAreaPair.ContainsKey(pair))
                {
                    rawFrontierByAreaPair.Add(pair, new List<WalkPosition>());
                }

                rawFrontierByAreaPair[pair].Add(raw.Second);
            }

            // 3) For each pair of Areas (A, B):
            foreach (var raw in rawFrontierByAreaPair)
            {
                var a = raw.Key.First;
                var b = raw.Key.Second;

                var rawFrontierAB = raw.Value;

                // Because our dispatching preserved order,
                // and because Map::m_RawFrontier was populated in descending order of the altitude (see Map::ComputeAreas),
                // we know that RawFrontierAB is also ordered the same way, but let's check it:
                {
                    var altitudes = new List<Altitude>();
                    foreach (var w in rawFrontierAB)
                    {
                        altitudes.Add(_map.GetTile(w).Altitude);
                    }

                    // Check if the altitudes array is sorted in descending order.
                    for (var i = 1; i < altitudes.Count; ++i) {
                        Debug.Assert(altitudes[i - 1] >= altitudes[i]);
                    }
                }

                // 3.1) Use that information to efficiently cluster RawFrontierAB in one or several chokepoints.
                //    Each cluster will be populated starting with the center of a chokepoint (max altitude)
                //    and finishing with the ends (min altitude).
                var clusterMinDist = Math.Sqrt(Map.LakeMaxMiniTiles);
                var clusters = new List<Deque<WalkPosition>>();
                foreach (var w in rawFrontierAB)
                {
                    var added = false;
                    foreach (var cluster in clusters)
                    {
                        var distToFront = Ex.QueenWiseDist(cluster[0], w);
                        var distToBack = Ex.QueenWiseDist(cluster[^1], w);
                        if (Math.Min(distToFront, distToBack) <= clusterMinDist)
                        {
                            if (distToFront < distToBack)
                            {
                                cluster.AddToFront(w);
                            }
                            else
                            {
                                cluster.AddToBack(w);
                            }

                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        var q = new Deque<WalkPosition>();
                        q.AddToBack(w);
                        clusters.Add(q);
                    }
                }

                // 3.2) Create one Chokepoint for each cluster:
                var chokePoints = GetChokePoints(a, b);
                var areaA = GetArea(a);
                var areaB = GetArea(b);
                foreach (var cluster in clusters)
                {
                    chokePoints.Add(new ChokePoint(this, newIndex, areaA, areaB, cluster.ToArray()));
                    newIndex++;
                }
            }

            // 4) Create one Chokepoint for each pair of blocked areas, for each blocking Neutral:
            foreach (var neutral in blockingNeutrals)
            {
                if (neutral.NextStacked == null) // in the case where several neutrals are stacked, we only consider the top
                {
                    var blockedAreas = neutral.BlockedAreas;
                    foreach (var pA in blockedAreas)
                    {
                        foreach (var pB in blockedAreas)
                        {
                            if (pB == pA)
                            {
                                break;
                            }

                            var center = _map.BreadthFirstSearch(
                                new WalkPosition(neutral.Pos),
                                (MiniTile miniTile, WalkPosition _) => miniTile.Walkable,   // findCond
                                (MiniTile _0, WalkPosition _1) => true                      // visitCond
                            );

                            var chokePoints = GetChokePoints(pA, pB);
                            chokePoints.Add(new ChokePoint(this, newIndex, pA, pB, new WalkPosition[] { center }, neutral));
                            newIndex++;
                        }
                    }
                }
            }

            // 5) Set the references to the freshly created Chokepoints:
            _chokePoints = new List<ChokePoint>();
            for (var a = 1; a <= AreasCount; ++a)
            {
                for (var b = 1; b < a; ++b)
                {
                    var chokePointsAB = GetChokePoints(a, b);
                    if (chokePointsAB.Count > 0)
                    {
                        var areaA = GetArea(a);
                        var areaB = GetArea(b);
                        areaA.AddChokePoints(areaB, chokePointsAB);
                        areaB.AddChokePoints(areaA, chokePointsAB);

                        foreach (var cp in chokePointsAB)
                        {
                            _chokePoints.Add(cp);
                        }
                    }
                }
            }
        }

        public void ComputeChokePointDistanceMatrix()
        {
            // 1) Size the matrix
            _chokePointDistanceMatrix = new int[_chokePoints.Count, _chokePoints.Count];
            for (var i = 0; i < _chokePoints.Count; i++)
            {
                for (var j = 0; j < _chokePoints.Count; j++)
                {
                    _chokePointDistanceMatrix[i, j] = -1;
                }
            }

            _pathsBetweenChokePoints = new CPPath[_chokePoints.Count, _chokePoints.Count];
            for (var i = 0; i < _chokePoints.Count; i++)
            {
                for (var j = 0; j < _chokePoints.Count; j++)
                {
                    _pathsBetweenChokePoints[i, j] = new CPPath();
                }
            }

            // 2) Compute distances inside each Area
            foreach (var area in Areas)
            {
                ComputeChokePointDistances(area);
            }

            // 3) Compute distances through connected Areas
            ComputeChokePointDistances();

            foreach (var cp in ChokePoints)
            {
                SetDistance(cp, cp, 0);
                SetPath(cp, cp, new CPPath() { cp });
            }

            // 4) Update Area::m_AccessibleNeighbours for each Area
            foreach (var area in Areas)
            {
                area.UpdateAccessibleNeighbours();
            }

            // 5)  Update Area::m_groupId for each Area
            UpdateGroupIds();
        }

        public void CollectInformation()
        {
            // 1) Process the whole Map:
            foreach (var m in _map.Minerals)
            {
                var area = MainArea(m.TopLeft, m.Size);
                area?.AddMineral(m);
            }

            foreach (var g in _map.Geysers)
            {
                var area = MainArea(g.TopLeft, g.Size);
                area?.AddGeyser(g);
            }

            for (var y = 0; y < _map.Size.y; ++y)
            {
                for (var x = 0; x < _map.Size.x; ++x)
                {
                    var tile = _map.GetTile(new TilePosition(x, y));
                    if (tile.AreaId > 0)
                    {
                        var area = GetArea(tile.AreaId);
                        area.AddTileInformation(new TilePosition(x, y), tile);
                    }
                }
            }

            // 2) Post-process each Area separately:
            foreach (var area in _areas)
            {
                area.PostCollectInformation();
            }
        }


        public void CreateBases()
        {
            _bases = new List<Base>();

            foreach (var area in _areas)
            {
                area.CreateBases();
                _bases.AddRange(area.Bases);
            }
        }

        private void ComputeChokePointDistances()
        {
            foreach (var startCP in ChokePoints)
            {
                var targets = new List<ChokePoint>();
                foreach (var cp in ChokePoints)
                {
                    if (cp == startCP)
                    {
                        break; // breaks symmetry
                    }

                    targets.Add(cp);
                }

                var distanceToTargets = ComputeDistances(startCP, targets);

                for (var i = 0; i < targets.Count; ++i)
                {
                    var newDist = distanceToTargets[i];
                    var existingDist = Distance(startCP, targets[i]);

                    if (newDist != 0 && ((existingDist == -1) || (newDist < existingDist)))
                    {
                        SetDistance(startCP, targets[i], newDist);

                        var path = new CPPath() { targets[i] };

                        // Collect the intermediate ChokePoints (in the reverse order) and insert them into Path:
                        for (var prevCP = targets[i].PathBackTrace; prevCP != startCP; prevCP = prevCP.PathBackTrace)
                        {
                            path.Add(prevCP);
                        }

                        path.Add(startCP);
                        path.Reverse();

                        SetPath(startCP, targets[i], path);
                    }
                }
            }
        }

        private void ComputeChokePointDistances(Area area)
        {
            foreach (var startCP in area.ChokePoints)
            {
                var targets = new List<ChokePoint>();
                foreach (var cp in area.ChokePoints)
                {
                    if (cp == startCP)
                    {
                        break;
                    }

                    targets.Add(cp);
                }

                var distanceToTargets = area.ComputeDistances(startCP, targets);

                for (var i = 0; i < targets.Count; ++i)
                {
                    var newDist = distanceToTargets[i];
                    var existingDist = Distance(startCP, targets[i]);

                    if (newDist != 0 && ((existingDist == -1) || (newDist < existingDist)))
                    {
                        SetDistance(startCP, targets[i], newDist);

                        // Build the path from pStart to Targets[i]:
                        SetPath(startCP, targets[i], new CPPath() { startCP, targets[i] });
                    }
                }
            }
        }

        // Returns Distances such that Distances[i] == ground_distance(start, Targets[i]) in pixels
        // Any Distances[i] may be 0 (meaning Targets[i] is not reachable).
        // This may occur in the case where start and Targets[i] leave in different continents or due to Bloqued intermediate ChokePoint(s).
        // For each reached target, the shortest path can be derived using
        // the backward trace set in cp->PathBackTrace() for each intermediate ChokePoint cp from the target.
        // Note: same algo than Area::ComputeDistances (derived from Dijkstra)
        private int[] ComputeDistances(ChokePoint start, List<ChokePoint> targets)
        {
            var distanceToTargets = new int[targets.Count];

            var remainingTargets = targets.Count;

            using var marked = new Markable2D(_map._tileSize.x, _map._tileSize.y);
            var distances = new Dictionary<TilePosition, int>();
            var toVisit = new PriorityQueue<ChokePoint, int>(); // a priority queue holding the tiles to visit ordered by their distance to start.

            toVisit.Enqueue(start, 0);
            distances.Add(new TilePosition(start.Center), 0);

            while (toVisit.TryDequeue(out var currentChokePoint, out var currentDist))
            {
                var current = new TilePosition(currentChokePoint.Center);
                Debug.Assert(distances[current] == currentDist);

                marked.Mark(current.x, current.y);

                for (var i = 0; i < targets.Count; ++i)
                {
                    if (currentChokePoint == targets[i])
                    {
                        distanceToTargets[i] = currentDist;
                        remainingTargets--;
                    }
                }

                if (remainingTargets == 0)
                {
                    break;
                }

                if (currentChokePoint.Blocked && (currentChokePoint != start))
                {
                    continue;
                }

                var areas = new[] { currentChokePoint.Areas.First, currentChokePoint.Areas.Second };
                foreach (var area in areas)
                {
                    foreach (var nextChokePoint in area.ChokePoints)
                    {
                        var next = new TilePosition(nextChokePoint.Center);
                        if (nextChokePoint != currentChokePoint)
                        {
                            var newNextDist = currentDist + Distance(currentChokePoint, nextChokePoint);
                            if (!marked.IsMarked(next.x, next.y))
                            {
                                if (distances.TryGetValue(next, out var nextOldDist))	// next already in ToVisit
                                {
                                    if (newNextDist < nextOldDist)
                                    {
                                        // To update next's distance, we need to remove-insert it from ToVisit:
                                        distances[next] = newNextDist;
                                        toVisit.Remove(nextChokePoint);
                                        toVisit.Enqueue(nextChokePoint, newNextDist);

                                        nextChokePoint.PathBackTrace = currentChokePoint;
                                    }
                                }
                                else
                                {
                                    distances[next] = newNextDist;
                                    nextChokePoint.PathBackTrace = currentChokePoint;
                                    toVisit.Enqueue(nextChokePoint, newNextDist);
                                }
                            }
                        }
                    }
                }
            }

            // Debug.Assert(remainingTargets == 0);

            return distanceToTargets;
        }

        private void SetDistance(ChokePoint cpA, ChokePoint cpB, int value)
        {
            _chokePointDistanceMatrix[cpA.Index, cpB.Index] =
            _chokePointDistanceMatrix[cpB.Index, cpA.Index] = value;
        }

        private void UpdateGroupIds()
        {
            var nextGroupId = new GroupId(1);

            var visited = new HashSet<Area>();
            var toVisit = new Stack<Area>();

            foreach (var area in Areas)
            {
                if (!visited.Contains(area))
                {
                    toVisit.Clear();
                    toVisit.Push(area);

                    while (toVisit.TryPop(out var current))
                    {
                        current.GroupId = nextGroupId;

                        foreach (var next in current.AccessibleNeighbours)
                        {
                            if (!visited.Contains(next))
                            {
                                visited.Add(next);
                                toVisit.Push(next);
                            }
                        }
                    }

                    nextGroupId = new GroupId((short)(nextGroupId.Value + 1));
                }
            }
        }

        private void SetPath(ChokePoint cpA, ChokePoint cpB, CPPath PathAB)
        {
            _pathsBetweenChokePoints[cpA.Index, cpB.Index] = PathAB;
            _pathsBetweenChokePoints[cpB.Index, cpA.Index] = new CPPath(Enumerable.Reverse(PathAB));
        }

        private Area MainArea(TilePosition topLeft, TilePosition size)
        {
            // graph.cpp:30:Area * mainArea(MapImpl * pMap, TilePosition topLeft, TilePosition size)
            // Note: The original C++ code appears to return the last discovered area instead of the area with
            // the highest frequency.
            // Bytekeeper: Further analysis shows there is usually one exactly one area, so we just return thatv
            // TODO: Determine if we desire the last discovered area or the area with the highest frequency.

            // var areaFreq = new Dictionary<Area, int>();

            Area mostFreqArea = null;
            // var mostFreq = 0;

            for (var dy = 0 ; dy < size.y ; ++dy)
            {
                for (var dx = 0 ; dx < size.x ; ++dx)
                {
                    var area = _map.GetArea(topLeft + new TilePosition(dx, dy));
                    if (area != null)
                    {
                        return area;

                        // if (!areaFreq.ContainsKey(area))
                        // {
                        //     areaFreq.Add(area, 0);
                        // }

                        // areaFreq[area]++;

                        // if (areaFreq[area] > mostFreq)
                        // {
                        //     mostFreqArea = area;
                        //     mostFreq = areaFreq[area];
                        // }
                    }
                }
            }

            return mostFreqArea;
        }

        private bool Valid(AreaId id)
        {
            return (1 <= id) && (id <= AreasCount);
        }
    }
}