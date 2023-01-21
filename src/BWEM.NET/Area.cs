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

namespace BWEM.NET
{
    /// <summary>
    /// Areas are regions that BWEM automatically computes from Brood War's maps
    /// Areas aim at capturing relevant regions that can be walked, though they may contain small inner non walkable regions called lakes.
    /// More formally:
    ///  - An area consists in a set of 4-connected MiniTiles, which are either Terrain-MiniTiles or Lake-MiniTiles.
    ///  - An Area is delimited by the side of the Map, by Water-MiniTiles, or by other Areas. In the latter case
    ///    the adjoining Areas are called neighbouring Areas, and each pair of such Areas defines at least one ChokePoint.
    /// Like ChokePoints and Bases, the number and the addresses of Area instances remain unchanged.
    /// To access Areas one can use their ids or their addresses with equivalent efficiency.
    ///
    /// Areas inherit utils::Markable, which provides marking ability
    /// Areas inherit utils::UserData, which provides free-to-use data.
    /// </summary>
    public class Area : IEquatable<Area>
    {
        private readonly AreaId _id;
        private readonly Graph _graph;
        private readonly WalkPosition _top;
        private readonly int _miniTiles;
        private readonly Altitude _maxAltitude;
        private readonly Dictionary<Area, List<ChokePoint>> _chokePointsByArea;
        private readonly List<Area> _accessibleNeighbours;
        private readonly List<ChokePoint> _chokePoints;
        private readonly List<Mineral> _minerals;
        private readonly List<Geyser> _geysers;
        private GroupId _groupId;
        private TilePosition _topLeft;
        private TilePosition _bottomRight;
        private int _tiles;
        private int _buildableTiles;
        private int _highGroundTiles;
        private int _veryHighGroundTiles;
        private List<Base> _bases;

        internal Area(Graph graph, short areaId, WalkPosition top, int miniTiles)
        {
            Debug.Assert(areaId > 0);

            _id = areaId;
            _graph = graph;
            _top = top;
            _miniTiles = miniTiles;

            _topLeft = new TilePosition(int.MaxValue, int.MaxValue);
            _bottomRight = new TilePosition(int.MinValue, int.MinValue);

            var topMiniTile = _graph.Map.GetTile(top);
            Debug.Assert(topMiniTile.AreaId == areaId);

            _maxAltitude = topMiniTile.Altitude;

            _chokePointsByArea = new Dictionary<Area, List<ChokePoint>>();
            _accessibleNeighbours = new List<Area>();
            _chokePoints = new List<ChokePoint>();
            _minerals = new List<Mineral>();
            _geysers = new List<Geyser>();
        }

        /// <summary>
        /// Gets the unique id > 0 of this Area.
        /// Range = 1 .. Map::Areas().size()
        /// this == Map::GetArea(Id())
        /// Id() == Map::GetMiniTile(w).AreaId() for each walkable MiniTile w in this Area.
        /// Area::ids are guaranteed to remain unchanged.
        /// </summary>
        public AreaId Id
        {
            get => _id;
        }

        /// <summary>
        /// Gets the unique id > 0 of the group of Areas which are accessible from this Area.
        /// For each pair (a, b) of Areas: a->GroupId() == b->GroupId()  <==>  a->AccessibleFrom(b)
        /// A groupId uniquely identifies a maximum set of mutually accessible Areas, that is, in the absence of blocking ChokePoints, a continent.
        /// </summary>
        public GroupId GroupId
        {
            get => _groupId;
            internal set
            {
                Debug.Assert(value >= 1);
                _groupId = value;
            }
        }

        /// <summary>
        /// Gets the top left position of the bounding box of this Area.
        /// </summary>
        public TilePosition TopLeft
        {
            get => _topLeft;
        }

        /// <summary>
        /// Gets the bottom right position of the bounding box of this Area.
        /// </summary>
        public TilePosition BottomRight
        {
            get => _bottomRight;
        }

        /// <summary>
        /// Gets the size of the bounding box of this Area.
        /// </summary>
        public TilePosition BoundingBoxSize
        {
            get => new TilePosition(
                _bottomRight.x - _topLeft.x + 1,
                _bottomRight.y - _topLeft.y + 1
            );
        }

        /// <summary>
        /// Position of the MiniTile with the highest Altitude() value.
        /// </summary>
        public WalkPosition Top
        {
            get => _top;
        }

        /// <summary>
        /// Returns Map::GetMiniTile(Top()).Altitude().
        /// </summary>
	    public Altitude MaxAltitude
        {
            get => _maxAltitude;
        }

        /// <summary>
        /// Returns the number of MiniTiles in this Area.
        /// This most accurately defines the size of this Area.
        /// </summary>
	    public int MiniTiles
        {
            get => _miniTiles;
        }

        /// <summary>
        /// Returns the percentage of low ground Tiles in this Area.
        /// </summary>
	    public int LowGroundPercentage
        {
            get => (_tiles - _highGroundTiles - _veryHighGroundTiles) * 100 / _tiles;
        }

	    /// <summary>
        /// Returns the percentage of high ground Tiles in this Area.
        /// </summary>
	    public int HighGroundPercentage
        {
            get => _highGroundTiles * 100 / _tiles;
        }

	    /// <summary>
        /// Returns the percentage of very high ground Tiles in this Area.
        /// </summary>
	    public int VeryHighGroundPercentage
        {
            get => _veryHighGroundTiles * 100 / _tiles;
        }

        // Returns the ChokePoints between this Area and the neighbouring ones.
        // Note: if there are no neighbouring Areas, then an empty set is returned.
        // Note there may be more ChokePoints returned than the number of neighbouring Areas, as there may be several ChokePoints between two Areas (Cf. ChokePoints(const Area * pArea)).
        public List<ChokePoint> ChokePoints
        {
            get => _chokePoints;
        }

        // Returns the ChokePoints of this Area grouped by neighbouring Areas
        // Note: if there are no neighbouring Areas, than an empty set is returned.
        public Dictionary<Area, List<ChokePoint>> ChokePointsByArea
        {
            get => _chokePointsByArea;
        }

        // Returns the accessible neighbouring Areas.
        // The accessible neighbouring Areas are a subset of the neighbouring Areas (the neighbouring Areas can be iterated using ChokePointsByArea()).
        // Two neighbouring Areas are accessible from each over if at least one the ChokePoints they share is not Blocked (Cf. ChokePoint::Blocked).
        public List<Area> AccessibleNeighbours
        {
            get => _accessibleNeighbours;
        }

        // Returns the Minerals contained in this Area.
        // Note: only a call to Map::OnMineralDestroyed(BWAPI::Unit u) may change the result (by removing eventually one element).
        public List<Mineral> Minerals
        {
            get => _minerals;
        }

        // Returns the Geysers contained in this Area.
        // Note: the result will remain unchanged.
        public List<Geyser> Geysers
        {
            get => _geysers;
        }

        // Returns the Bases contained in this Area.
        // Note: the result will remain unchanged.
        public List<Base> Bases
        {
            get => _bases;
        }

        public Map Map
        {
            get => _graph.Map;
        }

        public bool Equals(Area other)
        {
            return _id == other._id;
        }

        public override bool Equals(object obj)
        {
            return obj is Area other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return $"Area: {_id}";
        }

        // Returns the ChokePoints between this Area and pArea.
        // Assumes pArea is a neighbour of this Area, i.e. ChokePointsByArea().find(pArea) != ChokePointsByArea().end()
        // Note: there is always at least one ChokePoint between two neighbouring Areas.
        public List<ChokePoint> GetChokePoints(Area area)
        {
            Debug.Assert(_chokePointsByArea.ContainsKey(area));
            return _chokePointsByArea[area];
        }

        // Returns whether this Area is accessible from pArea, that is, if they share the same GroupId().
        // Note: accessibility is always symmetrical.
        // Note: even if a and b are neighbouring Areas,
        //       we can have: a->AccessibleFrom(b)
        //       and not:     contains(a->AccessibleNeighbours(), b)
        // See also GroupId()
        public bool	AccessibleFrom(Area area)
        {
            return _groupId == area._groupId;
        }

        internal void AddChokePoints(Area area, List<ChokePoint> chokePoints)
        {
            Debug.Assert(!_chokePointsByArea.ContainsKey(area) && chokePoints != null);

            _chokePointsByArea[area] = chokePoints;

            foreach (var cp in chokePoints)
            {
                _chokePoints.Add(cp);
            }
        }

        internal void AddMineral(Mineral mineral)
        {
            Debug.Assert(mineral != null && !_minerals.Contains(mineral));
	        _minerals.Add(mineral);
        }

        internal void AddGeyser(Geyser geyser)
        {
            Debug.Assert(geyser != null && !_geysers.Contains(geyser));
	        _geysers.Add(geyser);
        }

        internal void AddTileInformation(TilePosition t, Tile tile)
        {
            ++_tiles;
            if (tile.Buildable) ++_buildableTiles;
            if (tile.GroundHeight == Tile.GroundHeight_.HIGH_GROUND) ++_highGroundTiles;
            if (tile.GroundHeight == Tile.GroundHeight_.VERY_HIGH_GROUND) ++_veryHighGroundTiles;

            if (t.x < _topLeft.x) _topLeft = new TilePosition(t.x, _topLeft.y);
            if (t.y < _topLeft.y) _topLeft = new TilePosition(_topLeft.x, t.y);
            if (t.x > _bottomRight.x) _bottomRight = new TilePosition(t.x, _bottomRight.y);
            if (t.y > _bottomRight.y) _bottomRight = new TilePosition(_bottomRight.x, t.y);
        }

        internal void OnMineralDestroyed(Mineral mineral)
        {
            Debug.Assert(mineral != null);

            _minerals.FastRemove(mineral);

            // let's examine the bases even if pMineral was not found in this Area,
            // which could arise if Minerals were allowed to be assigned to neighbouring Areas.
            foreach (var base_ in Bases)
            {
                base_.OnMineralDestroyed(mineral);
            }
        }

        // Called after AddTileInformation(t) has been called for each tile t of this Area
        internal void PostCollectInformation()
        {
        }

        internal List<int> ComputeDistances(ChokePoint startChokePoint, List<ChokePoint> targetChokePoints)
        {
            Debug.Assert(!targetChokePoints.Contains(startChokePoint));

            var start = _graph.Map.BreadthFirstSearch(
                new TilePosition(startChokePoint.PosInArea(ChokePoint.Node.Middle, this)),
                (Tile tile, TilePosition _) => tile.AreaId == Id,   // findCond
                (Tile _0, TilePosition _1) => true                  // visitCond
            );

            var targets = new List<TilePosition>();

            foreach (var cp in targetChokePoints)
            {
                targets.Add(
                    _graph.Map.BreadthFirstSearch(
                        new TilePosition(cp.PosInArea(ChokePoint.Node.Middle, this)),
                        (Tile tile, TilePosition _) => tile.AreaId == Id,   // findCond
                        (Tile _0, TilePosition _1) => true                  // visitCond
                    )
                );
            }

            return ComputeDistances(start, targets);
        }

        internal void UpdateAccessibleNeighbours()
        {
            _accessibleNeighbours.Clear();

            foreach (var (area, chokePoints) in ChokePointsByArea)
            {
                if (chokePoints.Any(cp => !cp.Blocked))
                {
                    _accessibleNeighbours.Add(area);
                }
            }
        }

        // Fills in m_Bases with good locations in this Area.
        // The algorithm repeatedly searches the best possible location L (near ressources)
        // When it finds one, the nearby ressources are assigned to L, which makes the remaining ressources decrease.
        // This causes the algorithm to always terminate due to the lack of remaining ressources.
        // To efficiently compute the distances to the ressources, with use Potiential Fields in the InternalData() value of the Tiles.
        internal void CreateBases()
        {
            var dimCC = UnitType.Terran_Command_Center.TileSize();

            // Initialize the RemainingRessources with all the Minerals and Geysers in this Area satisfying some conditions:
            var remainingRessources = new List<Ressource>();

            foreach (var m in _minerals)
            {
                if ((m.InitialAmount >= 40) && !m.Blocking)
                {
                    remainingRessources.Add(m);
                }
            }

            foreach (var g in _geysers)
            {
                if ((g.InitialAmount >= 300) && !g.Blocking)
                {
                    remainingRessources.Add(g);
                }
            }

            _bases = new List<Base>(Math.Min(100, remainingRessources.Count));

            var data = new Dictionary<TilePosition, int>();

            while (remainingRessources.Count > 0)
            {
                // 1) Calculate the SearchBoundingBox (needless to search too far from the RemainingRessources):
                var topLeftRessources = new TilePosition(int.MaxValue, int.MaxValue);
                var bottomRightRessources = new TilePosition(int.MinValue, int.MinValue);

                foreach (var r in remainingRessources)
                {
                    (topLeftRessources, bottomRightRessources) = Ex.MakeBoundingBoxIncludePoint(topLeftRessources, bottomRightRessources, r.TopLeft);
                    (topLeftRessources, bottomRightRessources) = Ex.MakeBoundingBoxIncludePoint(topLeftRessources, bottomRightRessources, r.BottomRight);
                }

                var topLeftSearchBoundingBox = topLeftRessources - dimCC - Map.MaxTilesBetweenCommandCenterAndRessources;
                var bottomRightSearchBoundingBox = bottomRightRessources + 1 + Map.MaxTilesBetweenCommandCenterAndRessources;

                topLeftSearchBoundingBox = Ex.MakePointFitToBoundingBox(topLeftSearchBoundingBox, _topLeft, _bottomRight - dimCC + 1);
                bottomRightSearchBoundingBox = Ex.MakePointFitToBoundingBox(bottomRightSearchBoundingBox, _topLeft, _bottomRight - dimCC + 1);

                // 2) Mark the Tiles with their distances from each remaining Ressource (Potential Fields >= 0)
                foreach (var r in remainingRessources)
                {
                    for (var dy = -dimCC.y - Map.MaxTilesBetweenCommandCenterAndRessources; dy < r.Size.y + dimCC.y + Map.MaxTilesBetweenCommandCenterAndRessources; ++dy)
                    {
                        for (var dx = -dimCC.x - Map.MaxTilesBetweenCommandCenterAndRessources; dx < r.Size.x + dimCC.x + Map.MaxTilesBetweenCommandCenterAndRessources; ++dx)
                        {
                            var t = r.TopLeft + new TilePosition(dx, dy);
                            if (_graph.Map.Valid(t))
                            {
                                var tile = _graph.Map.GetTile(t, CheckMode.NoCheck);
                                var dist = (Ex.DistToRectangle(Ex.Center(t), r.TopLeft, r.Size) + 16) / 32;
                                var score = Math.Max(Map.MaxTilesBetweenCommandCenterAndRessources + 3 - dist, 0);
                                if (r is Geyser)
                                {
                                    score *= 3; // somewhat compensates for Geyser alone vs the several Minerals
                                }

                                if (tile.AreaId == _id)
                                {
                                    if (!data.ContainsKey(t))
                                    {
                                        data.Add(t, 0);
                                    }

                                    data[t] += score; // note the additive effect (assume tile.InternalData() is 0 at the begining)
                                }
                            }
                        }
                    }
                }

                // 3) Invalidate the 7 x 7 Tiles around each remaining Ressource (Starcraft rule)
                foreach (var r in remainingRessources)
                {
                    for (var dy = -3 ; dy < r.Size.y + 3 ; ++dy)
                    {
                        for (var dx = -3 ; dx < r.Size.x + 3 ; ++dx)
                        {
                            var t = r.TopLeft + new TilePosition(dx, dy);
                            if (_graph.Map.Valid(t))
                            {
                                data[t] = -1;
                            }
                        }
                    }
                }

                // 4) Search the best location inside the SearchBoundingBox:
                var bestLocation = new TilePosition();
                var bestScore = 0;
                var blockingMinerals = new List<Mineral>();

                for (var y = topLeftSearchBoundingBox.y ; y <= bottomRightSearchBoundingBox.y ; ++y)
                {
                    for (var x = topLeftSearchBoundingBox.x ; x <= bottomRightSearchBoundingBox.x ; ++x)
                    {
                        var score = ComputeBaseLocationScore(new TilePosition(x, y), data);
                        if (score > bestScore)
                        {
                            if (ValidateBaseLocation(new TilePosition(x, y), blockingMinerals))
                            {
                                bestScore = score;
                                bestLocation = new TilePosition(x, y);
                            }
                        }
                    }
                }

                // 5) Clear Tile::m_internalData (required due to our use of Potential Fields: see comments in 2))
                foreach (var r in remainingRessources)
                {
                    for (var dy = -dimCC.y-Map.MaxTilesBetweenCommandCenterAndRessources ; dy < r.Size.y + dimCC.y+Map.MaxTilesBetweenCommandCenterAndRessources ; ++dy)
                    {
                        for (var dx = -dimCC.x-Map.MaxTilesBetweenCommandCenterAndRessources ; dx < r.Size.x + dimCC.x+Map.MaxTilesBetweenCommandCenterAndRessources ; ++dx)
                        {
                            var t = r.TopLeft + new TilePosition(dx, dy);
                            if (_graph.Map.Valid(t))
                            {
                                data[t] = 0;
                            }
                        }
                    }
                }

                if (bestScore == 0)
                {
                    break;
                }

                // 6) Create a new Base at bestLocation, assign to it the relevant ressources and remove them from RemainingRessources:
                var assignedRessources = new List<Ressource>();
                foreach (var r in remainingRessources)
                {
                    if (Ex.DistToRectangle(r.Pos, bestLocation, dimCC) + 2 <= Map.MaxTilesBetweenCommandCenterAndRessources*32)
                    {
                        assignedRessources.Add(r);
                    }
                }

                remainingRessources.RemoveAll(r => assignedRessources.Contains(r));

                if (assignedRessources.Count == 0)
                {
                    break;
                }

                _bases.Add(new Base(this, bestLocation, assignedRessources, blockingMinerals));
            }
        }

        private int ComputeBaseLocationScore(TilePosition location, Dictionary<TilePosition, int> data)
        {
            var dimCC = UnitType.Terran_Command_Center.TileSize();

            var sumScore = 0;
            for (var dy = 0 ; dy < dimCC.y ; ++dy)
            {
                for (var dx = 0 ; dx < dimCC.x ; ++dx)
                {
                    var t = location + new TilePosition(dx, dy);
                    var tile = _graph.Map.GetTile(t, CheckMode.NoCheck);
                    if (!tile.Buildable)
                    {
                        return -1;
                    }

                    if (!data.ContainsKey(t) || data[t] == -1)
                    {
                        return -1; // The special value InternalData() == -1 means there is some ressource at maximum 3 tiles, which Starcraft rules forbid.
                    }

                    // Unfortunately, this is guaranteed only for the ressources in this Area, which is the very reason of ValidateBaseLocation
                    if (tile.AreaId != _id)
                    {
                        return -1;
                    }

                    if (tile.Neutral != null && tile.Neutral is StaticBuilding)
                    {
                        return -1;
                    }

                    sumScore += data[t];
                }
            }

            return sumScore;
        }

        // Checks if 'location' is a valid location for the placement of a Base Command Center.
        // If the location is valid except for the presence of Mineral patches of less than 9 (see Andromeda.scx),
        // the function returns true, and these Minerals are reported in BlockingMinerals
        // The function is intended to be called after ComputeBaseLocationScore, as it is more expensive.
        // See also the comments inside ComputeBaseLocationScore.
        private bool ValidateBaseLocation(TilePosition location, List<Mineral> blockingMinerals)
        {
            var dimCC = UnitType.Terran_Command_Center.TileSize();

            blockingMinerals.Clear();

            for (var dy = -3 ; dy < dimCC.y + 3 ; ++dy)
            {
                for (var dx = -3 ; dx < dimCC.x + 3 ; ++dx)
                {
                    var t = location + new TilePosition(dx, dy);
                    if (_graph.Map.Valid(t))
                    {
                        var tile = _graph.Map.GetTile(t, CheckMode.NoCheck);
                        if (tile.Neutral != null)
                        {
                            if (tile.Neutral is Geyser) return false;
                            if (tile.Neutral is Mineral m)
                            {
                                if (m.InitialAmount <= 8) blockingMinerals.Add(m);
                                else return false;
                            }
                        }
                    }
                }
            }

            // checks the distance to the Bases already created:
            foreach (var base_ in Bases)
            {
                if (Ex.RoundedDist(base_.Location, location) < Map.MinTilesBetweenBases)
                {
                    return false;
                }
            }

            return true;
        }

        // Returns Distances such that Distances[i] == ground_distance(start, Targets[i]) in pixels
        // Note: same algorithm than Graph::ComputeDistances (derived from Dijkstra)
        private List<int> ComputeDistances(TilePosition start, List<TilePosition> targets)
        {
            var deltas = new[]
            {
                new TilePosition(-1, -1),
                new TilePosition(0, -1),
                new TilePosition(+1, -1),

                new TilePosition(-1,  0),
                new TilePosition(+1,  0),

                new TilePosition(-1, +1),
                new TilePosition(0, +1),
                new TilePosition(+1, +1)
            };

            var distanceToTargets = new List<int>(targets.Count);
            distanceToTargets.AddRepeat(targets.Count, 0);

            var remainingTargets = targets.Count;

            var visited = new HashSet<TilePosition>();
            var distances = new Dictionary<TilePosition, int>();
            var toVisit = new PriorityQueue<TilePosition, int>(); // a priority queue holding the tiles to visit ordered by their distance to start.

            toVisit.Enqueue(start, 0);
            distances[start] = 0;

            while (toVisit.TryDequeue(out var current, out var currentDist))
            {
                Debug.Assert(distances[current] == currentDist);

                visited.Add(current);

                for (var i = 0 ; i < targets.Count ; ++i)
                {
                    if (current.Equals(targets[i]))
                    {
                        distanceToTargets[i] = (int)(0.5 + currentDist * 32 / 10000.0);
                        remainingTargets--;
                    }
                }

                if (remainingTargets == 0)
                {
                    break;
                }

                foreach (var delta in deltas)
                {
                    var next = current + delta;

                    var diagonalMove = (delta.x != 0) && (delta.y != 0);
                    var newNextDist = currentDist + (diagonalMove ? 14142 : 10000);

                    if (_graph.Map.Valid(next) && !visited.Contains(next))
                    {
                        var nextTile = _graph.Map.GetTile(next, CheckMode.NoCheck);
                        if (distances.TryGetValue(next, out var nextOldDist)) // next already in ToVisit
                        {
                            if (newNextDist < nextOldDist)
                            {
                                // To update next's distance, we need to remove-insert it from ToVisit:
                                distances[next] = newNextDist;
                                toVisit.Remove(next);
                                toVisit.Enqueue(next, newNextDist);
                            }
                        }
                        else if ((nextTile.AreaId == _id) || (nextTile.AreaId == -1))
                        {
                            distances[next] = newNextDist;
                            toVisit.Enqueue(next, newNextDist);
                        }
                    }
                }
            }

            Debug.Assert(remainingTargets == 0);

            return distanceToTargets;
        }
    }
}
