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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BWAPI.NET;
using BWGame = BWAPI.NET.Game;

namespace BWEM.NET
{
    /// <summary>
    /// Map is the entry point:
    ///  - to access general information on the Map
    ///  - to access the Tiles and the MiniTiles
    ///  - to access the Areas
    ///  - to access the StartingLocations
    ///  - to access the Minerals, the Geysers and the StaticBuildings
    ///  - to parametrize the analysis process
    ///  - to update the information
    ///
    /// Map also provides some useful tools such as Paths between ChokePoints and generic algorithms like BreadthFirstSearch
    /// Map functionnality is provided through its singleton Map::Instance().
    /// </summary>
    public class Map
    {
        internal const int AreaMinMiniTiles = 64; // At least area_min_miniTiles connected MiniTiles are necessary for an Area to be created.
        internal const int LakeMaxWidthInMiniTiles = 8 * 4;
        internal const int LakeMaxMiniTiles = 300; // These constants control how to decide between Seas and Lakes.
        internal const int MaxTilesBetweenCommandCenterAndRessources = 10;
        internal const int MaxTilesBetweenStartingLocationAndItsAssignedBase = 3;
        internal const int MinTilesBetweenBases = 10;

        private static readonly Random _rnd = new Random();
        private static readonly Dictionary<Pair<AreaId, AreaId>, int> _mapAreaPairCounter = new Dictionary<Pair<AreaId, AreaId>, int>();

        private readonly BWGame _game;
        private Graph _graph;
        private List<Mineral> _minerals;
        private List<Geyser> _geysers;
        private List<StaticBuilding> _staticBuildings;
        private List<TilePosition> _startingLocations;
        private List<Pair<Pair<AreaId, AreaId>, WalkPosition>> _rawFrontier;

        private TilePosition _tileSize;
        private WalkPosition _walkSize;

        private Tile[] _tiles;
        private MiniTile[] _miniTiles;

        private Position _center;

        private Altitude _maxAltitude;
        private bool _automaticPathUpdate;

        public Map(BWGame game)
        {
            Debug.Assert(game != null);
            _game = game;
        }

        /// <summary>
        /// Will return true once Initialize() has been called.
        /// </summary>
        public bool Initialized
        {
            get => _tiles != null;
        }

        /// <summary>
        /// Returns the size of the Map in Tiles.
        /// </summary>
        public TilePosition Size
        {
            get => _tileSize;
        }

        /// <summary>
        /// Returns the size of the Map in MiniTiles.
        /// </summary>
        public WalkPosition WalkSize
        {
            get => _walkSize;
        }

        /// <summary>
        /// Returns the center of the Map in pixels.
        /// </summary>
        public Position Center
        {
            get => _center;
        }

        // Returns the status of the automatic path update (off (false) by default).
        // When on, each time a blocking Neutral (either Mineral or StaticBuilding) is destroyed,
        // any information relative to the paths through the Areas is updated accordingly.
        // For this to function, the Map still needs to be informed of such destructions
        // (by calling OnMineralDestroyed and OnStaticBuildingDestroyed).
        public bool AutomaticPathUpdate
        {
            get => _automaticPathUpdate;
        }

        // Returns a reference to the starting Locations.
        // Note: these correspond to BWAPI::getStartLocations().
        public List<TilePosition> StartingLocations
        {
            get => _startingLocations;
        }

        // Returns a reference to the Minerals (Cf. Mineral).
        public List<Mineral> Minerals
        {
            get => _minerals;
        }

        // Returns a reference to the Geysers (Cf. Geyser).
        public List<Geyser> Geysers
        {
            get => _geysers;
        }

        // Returns a reference to the StaticBuildings (Cf. StaticBuilding).
        public List<StaticBuilding> StaticBuildings
        {
            get => _staticBuildings;
        }

        // Returns the maximum altitude in the whole Map (Cf. MiniTile::Altitude()).
        public Altitude MaxAltitude
        {
            get => _maxAltitude;
        }

        // Returns the number of Bases.
        public int BaseCount
        {
            get => _graph.BaseCount;
        }

        // Returns the number of Bases.
        public List<Base> Bases
        {
            get => _graph.Bases;
        }

        // Returns the number of ChokePoints.
        public int ChokePointCount
        {
            get => _graph.ChokePoints.Count;
        }

        // Returns the ChokePoints.
        public List<ChokePoint> ChokePoints
        {
            get => _graph.ChokePoints;
        }

        // Provides access to the internal array of Tiles.
        public Tile[] Tiles
        {
            get => _tiles;
        }

        // Provides access to the internal array of MiniTiles.
        public MiniTile[] MiniTiles
        {
            get => _miniTiles;
        }

        // Returns a reference to the Areas.
        public List<Area> Areas
        {
            get => _graph.Areas;
        }

        // Returns the union of the geometry of all the ChokePoints. Cf. ChokePoint::Geometry()
        public List<Pair<Pair<AreaId, AreaId>, WalkPosition>> RawFrontier
        {
            get => _rawFrontier;
        }

        /// <summary>
        /// This has to be called before any other function is called.
        /// A good place to do this is in ExampleAIModule::onStart()
        /// </summary>
        public void Initialize()
        {
            _tileSize = new TilePosition(_game.MapWidth(), _game.MapHeight());
            _walkSize = new WalkPosition(_tileSize);

            var tilesCount = _tileSize.x * _tileSize.y;
            var walkTilesCount = _walkSize.x * _walkSize.y;

            _tiles = new Tile[tilesCount];
            for (var i = 0; i < tilesCount; i++)
            {
                _tiles[i] = new Tile();
            }

            _miniTiles = new MiniTile[walkTilesCount];
            for (var i = 0; i < walkTilesCount; i++)
            {
                _miniTiles[i] = new MiniTile();
            }

            _center = new Position(_tileSize.x / 2, _tileSize.y / 2);

            _startingLocations = new List<TilePosition>();
            foreach (var startLocation in _game.GetStartLocations())
            {
                _startingLocations.Add(startLocation);
            }

            _minerals = new List<Mineral>();
            _geysers = new List<Geyser>();
            _staticBuildings = new List<StaticBuilding>();
            _rawFrontier = new List<Pair<Pair<AreaId, AreaId>, WalkPosition>>();

            _graph = new Graph(this);

            LoadData();
            DecideSeasOrLakes();
            InitializeNeutrals();
            ComputeAltitude();
            ProcessBlockingNeutrals();
            ComputeAreas();

            _graph.CreateChokePoints();
            _graph.ComputeChokePointDistanceMatrix();
            _graph.CollectInformation();
            _graph.CreateBases();
        }

        // Enables the automatic path update (Cf. AutomaticPathUpdate()).
        // One might NOT want to call this function, in order to make the accessibility between Areas remain the same throughout the game.
        // Even in this case, one should keep calling OnMineralDestroyed and OnStaticBuildingDestroyed.
        public void EnableAutomaticPathAnalysis()
        {
            _automaticPathUpdate = true;
        }

        // Tries to assign one Base for each starting Location in StartingLocations().
        // Only nearby Bases can be assigned (Cf. detail::MaxTilesBetweenStartingLocationAndItsAssignedBase).
        // Each such assigned Base then has Starting() == true, and its Location() is updated.
        // Returns whether the function succeeded (a fail may indicate a failure in BWEM's Base placement analysis
        // or a suboptimal placement in one of the starting Locations).
        // You normally should call this function, unless you want to compare the StartingLocations() with
        // BWEM's suggested locations for the Bases.
        public bool FindBasesForStartingLocations()
        {
            var atLeastOneFailed = false;
            foreach (var location in _startingLocations)
            {
                var found = false;

                foreach (var area in _graph.Areas)
                {
                    if (!found)
                    {
                        foreach (var base_ in area.Bases)
                        {
                            if (!found)
                            {
                                if (Ex.QueenWiseDist(base_.Location, location) <= MaxTilesBetweenStartingLocationAndItsAssignedBase)
                                {
                                    base_.SetStartingLocation(location);
                                    found = true;
                                }
                            }
                        }
                    }
                }

                if (!found)
                {
                    atLeastOneFailed = true;
                }
            }

            return !atLeastOneFailed;
        }

        /// <summary>
        /// Returns a Tile, given its position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public Tile GetTile(TilePosition p, CheckMode checkMode = CheckMode.Check)
        {
            Debug.Assert((checkMode == CheckMode.NoCheck) || Valid(p));
            return _tiles[_tileSize.x * p.y + p.x];
        }

        /// <summary>
        /// Returns a MiniTile, given its position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MiniTile GetTile(WalkPosition p, CheckMode checkMode = CheckMode.Check)
        {
            Debug.Assert((checkMode == CheckMode.NoCheck) || Valid(p));
            return _miniTiles[_walkSize.x * p.y + p.x];
        }

        // Returns a Tile or a MiniTile, given its position.
        // Provided as a support of generic algorithms.
        public TTile GetTile<TPosition, TTile>(TPosition p, CheckMode checkMode = CheckMode.Check)
            where TPosition : IPoint<TPosition>
            where TTile : ITile
        {
            if (typeof(TPosition) == typeof(TilePosition))
            {
                var p_ = (TilePosition)(p as IPoint<TilePosition>);
                return (TTile)(GetTile(p_, checkMode) as ITile);
            }

            if (typeof(TPosition) == typeof(WalkPosition))
            {
                var p_ = (WalkPosition)(p as IPoint<WalkPosition>);
                return (TTile)(GetTile(p_, checkMode) as ITile);
            }

            throw new NotSupportedException("Invalid tile type " + typeof(TPosition));
        }

        // Returns whether the position p is valid.
        public bool Valid(TilePosition p)
        {
            return (0 <= p.x) && (p.x < _tileSize.x) && (0 <= p.y) && (p.y < _tileSize.y);
        }

        public bool Valid(WalkPosition p)
        {
            return (0 <= p.x) && (p.x < _walkSize.x) && (0 <= p.y) && (p.y < _walkSize.y);
        }

        public bool Valid(Position p)
        {
            return Valid(new WalkPosition(p));
        }

        public bool Valid<TPosition>(TPosition p)
            where TPosition : IPoint<TPosition>
        {
            if (typeof(TPosition) == typeof(Position))
            {
                return Valid((Position)(p as IPoint<Position>));
            }

            if (typeof(TPosition) == typeof(TilePosition))
            {
                return Valid((TilePosition)(p as IPoint<TilePosition>));
            }

            if (typeof(TPosition) == typeof(WalkPosition))
            {
                return Valid((WalkPosition)(p as IPoint<WalkPosition>));
            }

            throw new NotSupportedException("Invalid tile type " + typeof(TPosition));
        }

        // Returns the position closest to p that is valid.
        public WalkPosition Crop(WalkPosition p)
        {
            return Ex.Crop(p, _tileSize.x, _tileSize.y);
        }

        public TilePosition Crop(TilePosition p)
        {
            return Ex.Crop(p, _tileSize.x, _tileSize.y);
        }

        public Position Crop(Position p)
        {
            return Ex.Crop(p, _tileSize.x, _tileSize.y);
        }

        // If a Mineral wrappers the given BWAPI unit, returns a pointer to it.
        // Otherwise, returns nullptr.
        public Mineral GetMineral(Unit u)
        {
            return _minerals.Find(x => x.Unit == u);
        }

        // If a Geyser wrappers the given BWAPI unit, returns a pointer to it.
        // Otherwise, returns nullptr.
        public Geyser GetGeyser(Unit u)
        {
            return _geysers.Find(x => x.Unit == u);
        }

        // Should be called for each destroyed BWAPI unit u having u->getType().isMineralField() == true
        public void OnMineralDestroyed(Unit u)
        {
            var mineralIdx = _minerals.FindIndex(x => x.Unit == u);
            Debug.Assert(mineralIdx >= 0);

            _minerals[mineralIdx].Destroy();
            _minerals.FastRemoveAt(mineralIdx);
        }

        public void OnMineralDestroyed(Mineral mineral)
        {
            foreach (var area in _graph.Areas)
            {
                area.OnMineralDestroyed(mineral);
            }
        }

        // Should be called for each destroyed BWAPI unit u having u->getType().isSpecialBuilding() == true
        public void OnStaticBuildingDestroyed(Unit u)
        {
            var staticBuildingIdx = _staticBuildings.FindIndex(x => x.Unit == u);
            Debug.Assert(staticBuildingIdx >= 0);

            _staticBuildings[staticBuildingIdx].Destroy();
            _staticBuildings.FastRemoveAt(staticBuildingIdx);
        }

        public void OnBlockingNeutralDestroyed(Neutral blockingNeutral)
        {
            Debug.Assert(blockingNeutral != null && blockingNeutral.Blocking);

            foreach (var area in blockingNeutral.BlockedAreas)
            {
                foreach (var cp in area.ChokePoints)
                {
                    cp.OnBlockingNeutralDestroyed(blockingNeutral);
                }
            }

            var tile = GetTile(blockingNeutral.TopLeft);
            if (tile.Neutral != null)
            {
                // there remains some blocking Neutrals at the same location
                return;
            }

            // Unblock the miniTiles of pBlocking:
            var newId = blockingNeutral.BlockedAreas[0].Id;
            for (var dy = 0; dy < new WalkPosition(blockingNeutral.Size).y; ++dy)
            {
                for (var dx = 0; dx < new WalkPosition(blockingNeutral.Size).x; ++dx)
                {
                    var miniTile = GetTile(new WalkPosition(blockingNeutral.TopLeft) + new WalkPosition(dx, dy));
                    if (miniTile.Walkable)
                    {
                        miniTile.ReplaceBlockedAreaId(newId);
                    }
                }
            }

            // Unblock the Tiles of pBlocking:
            for (var dy = 0; dy < blockingNeutral.Size.y; ++dy)
            {
                for (var dx = 0; dx < blockingNeutral.Size.x; ++dx)
                {
                    tile = GetTile(blockingNeutral.TopLeft + new TilePosition(dx, dy));
                    tile.ResetAreaId();
                    SetAreaIdInTile(blockingNeutral.TopLeft + new TilePosition(dx, dy));
                }
            }

            if (AutomaticPathUpdate)
            {
                _graph.ComputeChokePointDistanceMatrix();
            }
        }

        // Returns an Area given its id.
        public Area GetArea(AreaId id)
        {
            return _graph.GetArea(id);
        }

        // If the MiniTile at w is walkable and is part of an Area, returns that Area.
        // Otherwise, returns nullptr;
        // Note: because of the lakes, GetNearestArea should be prefered over GetArea.
        public Area GetArea(WalkPosition w)
        {
            return _graph.GetArea(w);
        }

        // If the Tile at t contains walkable sub-MiniTiles which are all part of the same Area, returns that Area.
        // Otherwise, returns nullptr;
        // Note: because of the lakes, GetNearestArea should be prefered over GetArea.
        public Area GetArea(TilePosition t)
        {
            return _graph.GetArea(t);
        }

        // Returns the nearest Area from w.
        // Returns nullptr only if Areas().empty()
        // Note: Uses a breadth first search.
        public Area GetNearestArea(WalkPosition w)
        {
            return _graph.GetNearestArea<WalkPosition, MiniTile>(w);
        }

        // Returns the nearest Area from t.
        // Returns nullptr only if Areas().empty()
        // Note: Uses a breadth first search.
        public Area GetNearestArea(TilePosition t)
        {
            return _graph.GetNearestArea<TilePosition, Tile>(t);
        }

        // Returns a list of ChokePoints, which is intended to be the shortest walking path from 'a' to 'b'.
        // Furthermore, if pLength != nullptr, the pointed integer is set to the corresponding length in pixels.
        // If 'a' is not accessible from 'b', the empty Path is returned, and -1 is put in *pLength (if pLength != nullptr).
        // If 'a' and 'b' are in the same Area, the empty Path is returned, and a.getApproxDistance(b) is put in *pLength (if pLength != nullptr).
        // Otherwise, the function relies on ChokePoint::GetPathTo.
        // Cf. ChokePoint::GetPathTo for more information.
        // Note: in order to retrieve the Areas of 'a' and 'b', the function starts by calling
        //       GetNearestArea(TilePosition(a)) and GetNearestArea(TilePosition(b)).
        //       While this brings robustness, this could yield surprising results in the case where 'a' and/or 'b' are in the Water.
        //       To avoid this and the potential performance penalty, just make sure GetArea(a) != nullptr and GetArea(b) != nullptr.
        //       Then GetPath should perform very quick.
        public CPPath GetPath(Position a, Position b, out int length)
        {
            return _graph.GetPath(a, b, out length);
        }

        // Generic algorithm for breadth first search in the Map.
        // See the several use cases in BWEM source files.
        public TPosition BreadthFirstSearch<TPosition, TTile>(TPosition start, Func<TTile, TPosition, bool> findCond, Func<TTile, TPosition, bool> visitCond, bool connect8 = true)
            where TPosition : IPoint<TPosition>
            where TTile : ITile
        {
            var startTile = GetTile<TPosition, TTile>(start);
            if (findCond(startTile, start))
            {
                return start;
            }

            var visited = new HashSet<TPosition>();
            var toVisit = new Stack<TPosition>();

            toVisit.Push(start);
            visited.Add(start);

            var directions = connect8
                ? new[]
                {
                    PointHelper.New<TPosition>(-1, -1),
                    PointHelper.New<TPosition>(0, -1),
                    PointHelper.New<TPosition>(+1, -1),

                    PointHelper.New<TPosition>(-1, 0),
                    PointHelper.New<TPosition>(+1, 0),

                    PointHelper.New<TPosition>(-1, +1),
                    PointHelper.New<TPosition>(0, +1),
                    PointHelper.New<TPosition>(+1, +1)
                }
                : new[]
                {
                    PointHelper.New<TPosition>(0, -1),
                    PointHelper.New<TPosition>(-1, 0),
                    PointHelper.New<TPosition>(+1, 0),
                    PointHelper.New<TPosition>(0, +1)
                };

            while (toVisit.TryPop(out var current))
            {
                foreach (var delta in directions)
                {
                    var next = current.Add(delta);
                    if (Valid(next))
                    {
                        var nextTile = GetTile<TPosition, TTile>(next, CheckMode.NoCheck);
                        if (findCond(nextTile, next))
                        {
                            return next;
                        }

                        if (!visited.Contains(next) && visitCond(nextTile, next))
                        {
                            toVisit.Push(next);
                            visited.Add(next);
                        }
                    }
                }
            }

            Debug.Assert(false);
            return start;
        }

        // Breadth first search in the Map over tiles.
        public TilePosition BreadthFirstSearch(TilePosition start, Func<Tile, TilePosition, bool> findCond, Func<Tile, TilePosition, bool> visitCond, bool connect8 = true)
        {
            var startTile = GetTile(start);
            if (findCond(startTile, start))
            {
                return start;
            }

            var visited = new bool[_tileSize.x, _tileSize.y];
            var toVisit = new Queue<TilePosition>();

            toVisit.Enqueue(start);
            visited[start.x, start.y] = true;

            Span<TilePosition> directions = connect8
                ? stackalloc[]
                {
                    TilePosition.TopLeft,
                    TilePosition.Top,
                    TilePosition.TopRight,

                    TilePosition.Left,
                    TilePosition.Right,

                    TilePosition.BottomLeft,
                    TilePosition.Bottom,
                    TilePosition.BottomRight
                }
                : stackalloc[]
                {
                    TilePosition.Top,
                    TilePosition.Left,
                    TilePosition.Right,
                    TilePosition.Bottom
                };

            while (toVisit.TryDequeue(out var current))
            {
                foreach (var delta in directions)
                {
                    var next = current.Add(delta);
                    if (Valid(next))
                    {
                        var nextTile = GetTile(next, CheckMode.NoCheck);
                        if (findCond(nextTile, next))
                        {
                            return next;
                        }

                        if (!visited[next.x, next.y] && visitCond(nextTile, next))
                        {
                            toVisit.Enqueue(next);
                            visited[next.x, next.y] = true;
                        }
                    }
                }
            }

            Debug.Assert(false);
            return start;
        }

        // Breadth first search in the Map over minitiles.
        public WalkPosition BreadthFirstSearch(WalkPosition start, Func<MiniTile, WalkPosition, bool> findCond, Func<MiniTile, WalkPosition, bool> visitCond, bool connect8 = true)
        {
            var startTile = GetTile(start);
            if (findCond(startTile, start))
            {
                return start;
            }

            var visited = new bool[_walkSize.x, _walkSize.y];
            var toVisit = new Stack<WalkPosition>();

            toVisit.Push(start);
            visited[start.x, start.y] = true;

            Span<WalkPosition> directions = connect8
                ? stackalloc[]
                {
                    WalkPosition.TopLeft,
                    WalkPosition.Top,
                    WalkPosition.TopRight,

                    WalkPosition.Left,
                    WalkPosition.Right,

                    WalkPosition.BottomLeft,
                    WalkPosition.Bottom,
                    WalkPosition.BottomRight
                }
                : stackalloc[]
                {
                    WalkPosition.Top,
                    WalkPosition.Left,
                    WalkPosition.Right,
                    WalkPosition.Bottom
                };

            while (toVisit.TryPop(out var current))
            {
                foreach (var delta in directions)
                {
                    var next = current.Add(delta);
                    if (Valid(next))
                    {
                        var nextTile = GetTile(next, CheckMode.NoCheck);
                        if (findCond(nextTile, next))
                        {
                            return next;
                        }

                        if (!visited[next.x, next.y] && visitCond(nextTile, next))
                        {
                            toVisit.Push(next);
                            visited[next.x, next.y] = true;
                        }
                    }
                }
            }

            Debug.Assert(false);
            return start;
        }

        // Returns a random position in the Map in pixels.
        public Position RandomPosition()
        {
            var PixelSize = new Position(_tileSize);
            return new Position(_rnd.Next() % PixelSize.x, _rnd.Next() % PixelSize.y);
        }

        // Computes walkability, buildability and groundHeight and doodad information, using BWAPI corresponding functions
        private void LoadData()
        {
            // Mark unwalkable minitiles (minitiles are walkable by default)
            for (var y = 0; y < _walkSize.y; ++y)
            {
                for (var x = 0; x < _walkSize.x; ++x)
                {
                    if (!_game.IsWalkable(x, y)) // For each unwalkable minitile, we also mark its 8 neighbours as not walkable.
                    {
                        for (var dy = -1; dy <= +1; ++dy) // According to some tests, this prevents from wrongly pretending one Marine can go by some thin path.
                        {
                            for (var dx = -1; dx <= +1; ++dx)
                            {
                                var w = new WalkPosition(x + dx, y + dy);
                                if (Valid(w))
                                {
                                    var miniTile = GetTile(w, CheckMode.NoCheck);
                                    miniTile.Walkable = false;
                                }
                            }
                        }
                    }
                }
            }

            // Mark buildable tiles (tiles are unbuildable by default)
            for (var y = 0; y < _tileSize.y; ++y)
            {
                for (var x = 0; x < _tileSize.x; ++x)
                {
                    var tilePosition = new TilePosition(x, y);
                    var tile = GetTile(tilePosition);

                    if (_game.IsBuildable(tilePosition))
                    {
                        tile.SetBuildable();

                        // Ensures buildable ==> walkable:
                        for (var dy = 0; dy < 4; ++dy)
                        {
                            for (var dx = 0; dx < 4; ++dx)
                            {
                                var miniTile = GetTile(tilePosition.ToWalkPosition() + new WalkPosition(dx, dy), CheckMode.NoCheck);
                                miniTile.Walkable = true;
                            }
                        }
                    }

                    // Add groundHeight and doodad information:
                    var groundHeight = _game.GetGroundHeight(tilePosition);
                    tile.GroundHeight = (Tile.GroundHeight_)(groundHeight / 2);
                    if (groundHeight % 2 != 0)
                    {
                        tile.SetDoodad();
                    }
                }
            }
        }

        private void DecideSeasOrLakes()
        {
            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.Top,
                WalkPosition.Left,
                WalkPosition.Right,
                WalkPosition.Bottom
            };

            var toSearch = new Queue<WalkPosition>();
            var seaExtent = new List<MiniTile>();

            for (var y = 0; y < _walkSize.y; ++y)
            {
                for (var x = 0; x < _walkSize.x; ++x)
                {
                    var originWalkPosition = new WalkPosition(x, y);
                    var originMiniTile = GetTile(originWalkPosition, CheckMode.NoCheck);
                    if (originMiniTile.SeaOrLake)
                    {
                        toSearch.Clear();
                        seaExtent.Clear();

                        toSearch.Enqueue(originWalkPosition);
                        seaExtent.Add(originMiniTile);

                        originMiniTile.SetSea();

                        var topLeft = originWalkPosition;
                        var bottomRight = originWalkPosition;

                        while (toSearch.TryDequeue(out var current))
                        {
                            if (current.x < topLeft.x) topLeft = new WalkPosition(current.x, topLeft.y);
                            if (current.y < topLeft.y) topLeft = new WalkPosition(topLeft.X, current.y);
                            if (current.x > bottomRight.x) bottomRight = new WalkPosition(current.x, bottomRight.y);
                            if (current.y > bottomRight.y) bottomRight = new WalkPosition(bottomRight.x, current.y);

                            foreach (var delta in deltas)
                            {
                                var next = current + delta;
                                if (Valid(next))
                                {
                                    var nextTile = GetTile(next, CheckMode.NoCheck);
                                    if (nextTile.SeaOrLake)
                                    {
                                        toSearch.Enqueue(next);

                                        if (seaExtent.Count <= LakeMaxMiniTiles)
                                        {
                                            seaExtent.Add(nextTile);
                                        }

                                        nextTile.SetSea();
                                    }
                                }
                            }
                        }

                        if ((seaExtent.Count <= LakeMaxMiniTiles) &&
                            (bottomRight.x - topLeft.x <= LakeMaxWidthInMiniTiles) &&
                            (bottomRight.y - topLeft.y <= LakeMaxWidthInMiniTiles) &&
                            (topLeft.x >= 2) && (topLeft.y >= 2) &&
                            (bottomRight.x < _walkSize.x - 2) &&
                            (bottomRight.y < _walkSize.y - 2))
                        {
                            foreach (var sea in seaExtent)
                            {
                                sea.SetLake();
                            }
                        }
                    }
                }
            }
        }

        private void InitializeNeutrals()
        {
            foreach (var n in _game.GetStaticNeutralUnits())
            {
                if (n.GetUnitType().IsBuilding())
                {
                    if (n.GetUnitType().IsMineralField())
                    {
                        _minerals.Add(new Mineral(n, this));
                    }
                    else if (n.GetUnitType() == UnitType.Resource_Vespene_Geyser)
                    {
                        _geysers.Add(new Geyser(n, this));
                    }
                    else
                    {
                        Debug.Assert(n.GetUnitType().IsSpecialBuilding());
                        _staticBuildings.Add(new StaticBuilding(n, this));
                    }
                }
                else if (n.GetUnitType() == UnitType.Zerg_Egg)
                {
                    if (!n.GetUnitType().IsCritter())
                    {
                        Debug.Assert(n.GetUnitType().IsSpecialBuilding(), n.GetUnitType().ToString());
                        Debug.Assert(
                            n.GetUnitType() == UnitType.Special_Pit_Door ||
                            n.GetUnitType() == UnitType.Special_Right_Pit_Door,
                            n.GetUnitType().ToString()
                        );

                        if (n.GetUnitType() == UnitType.Special_Pit_Door)
                        {
                            _staticBuildings.Add(new StaticBuilding(n, this));
                        }

                        if (n.GetUnitType() == UnitType.Special_Right_Pit_Door)
                        {
                            _staticBuildings.Add(new StaticBuilding(n, this));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assigns <see cref="MiniTile.Altitude"/> for each miniTile having <seealso cref="MiniTile.AltitudeMissing"/> == true
        /// Altitudes are computed using the straightforward Dijkstra's algorithm : the lower ones are computed first, starting from the seaside-miniTiles neighbours.
        /// The point here is to precompute all possible altitudes for all possible tiles, and sort them.
        /// </summary>
        private void ComputeAltitude()
        {
            const int AltitudeScale = 8; // 8 provides a pixel definition for Altitude, since altitudes are computed from miniTiles which are 8x8 pixels

            // 1) Fill in and sort DeltasByAscendingAltitude
            var range = Math.Max(_walkSize.x, _walkSize.y) / 2 + 3; // should suffice for maps with no Sea.

            var deltasByAscendingAltitude = new List<Pair<WalkPosition, Altitude>>();

            for (var dy = 0; dy <= range; ++dy)
            {
                for (var dx = dy; dx <= range; ++dx) // Only consider 1/8 of possible deltas. Other ones obtained by symmetry.
                {
                    if (dx != 0 || dy != 0)
                    {
                        deltasByAscendingAltitude.Add(new Pair<WalkPosition, Altitude>(new WalkPosition(dx, dy), new Altitude((short)Math.Round(Ex.Norm(dx, dy) * AltitudeScale, MidpointRounding.AwayFromZero))));
                    }
                }
            }

            // NOTE: Need OrderBy for a stable sort implementation
            deltasByAscendingAltitude = deltasByAscendingAltitude.OrderBy(x => x.Second).ToList();
            // deltasByAscendingAltitude.Sort((p1, p2) => p1.Second.CompareTo(p2.Second));

            // 2) Fill in activeSeaSideMiniTiles, which basically contains all the seaside miniTiles (from which altitudes are to be computed)
            //    It also includes extra border-miniTiles which are considered as seaside miniTiles too.
            var activeSeaSideMiniTiles = new List<ActiveSeaSide>();

            for (var y = -1; y <= _walkSize.y; ++y)
            {
                for (var x = -1; x <= _walkSize.x; ++x)
                {
                    var w = new WalkPosition(x, y);
                    if (!Valid(w) || SeaSide(w))
                    {
                        activeSeaSideMiniTiles.Add(new ActiveSeaSide(w, 0));
                    }
                }
            }

            // 3) Dijkstra's algorithm
            Span<WalkPosition> deltas = stackalloc WalkPosition[8];

            foreach (var deltaAltitude in deltasByAscendingAltitude)
            {
                var d = deltaAltitude.First;
                var altitude = deltaAltitude.Second;

                deltas[0] = new WalkPosition(d.x, d.y);
                deltas[1] = new WalkPosition(-d.x, d.y);
                deltas[2] = new WalkPosition(d.x, -d.y);
                deltas[3] = new WalkPosition(-d.x, -d.y);
                deltas[4] = new WalkPosition(d.y, d.x);
                deltas[5] = new WalkPosition(-d.y, d.x);
                deltas[6] = new WalkPosition(d.y, -d.x);
                deltas[7] = new WalkPosition(-d.y, -d.x);

                for (var i = 0; i < activeSeaSideMiniTiles.Count; ++i)
                {
                    var current = activeSeaSideMiniTiles[i];

                    if (altitude - current.LastAltitudeGenerated >= 2 * AltitudeScale) // optimization : once a seaside miniTile verifies this condition,
                    {
                        activeSeaSideMiniTiles.FastRemoveAt(i--); // we can throw it away as it will not generate min altitudes anymore
                    }
                    else
                    {
                        foreach (var delta in deltas)
                        {
                            var w = current.Origin + delta;
                            if (Valid(w))
                            {
                                var miniTile = GetTile(w, CheckMode.NoCheck);
                                if (miniTile.AltitudeMissing)
                                {
                                    current.LastAltitudeGenerated = altitude;
                                    miniTile.Altitude = altitude;
                                    _maxAltitude = altitude;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessBlockingNeutrals()
        {
            var candidates = new List<Neutral>(_staticBuildings.Count + _minerals.Count);
            candidates.AddRange(_staticBuildings);
            candidates.AddRange(_minerals);

            var toVisit = new Stack<WalkPosition>();
            var visited = new bool[_walkSize.x, _walkSize.y];
            var doors = new List<WalkPosition>();
            var trueDoors = new List<WalkPosition>();
            var visitedCount = 0;

            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.Top,
                WalkPosition.Left,
                WalkPosition.Right,
                WalkPosition.Bottom
            };

            foreach (var candidate in candidates)
            {
                if (candidate.NextStacked == null) // in the case where several neutrals are stacked, we only consider the top one
                {
                    // 1) Retrieve the Border: the outer border of candidate
                    var border = Ex.OuterMiniTileBorder(candidate.TopLeft, candidate.Size);
                    border.RemoveAll(w => !Valid(w) || !GetTile(w, CheckMode.NoCheck).Walkable || GetTile(new TilePosition(w), CheckMode.NoCheck).Neutral != null);

                    // 2) Find the doors in Border: one door for each connected set of walkable, neighbouring miniTiles.
                    //    The searched connected miniTiles all have to be next to some lake or some static building, though they can't be part of one.
                    doors.Clear();

                    while (border.Count > 0)
                    {
                        var door = border[^1];
                        border.RemoveAt(border.Count - 1);

                        doors.Add(door);

                        toVisit.Clear();
                        Array.Clear(visited, 0, visited.Length);

                        toVisit.Push(door);
                        visited[door.x, door.y] = true;
                        visitedCount = 1;

                        while (toVisit.TryPop(out var current))
                        {
                            foreach (var delta in deltas)
                            {
                                var next = current + delta;
                                if (Valid(next) && !visited[next.x, next.y])
                                {
                                    var miniTile = GetTile(next, CheckMode.NoCheck);
                                    if (miniTile.Walkable)
                                    {
                                        var tile = GetTile(new TilePosition(next), CheckMode.NoCheck);
                                        if (tile.Neutral == null)
                                        {
                                            if (Adjoins8SomeLakeOrNeutral(next))
                                            {
                                                toVisit.Push(next);
                                                visited[next.x, next.Y] = true;
                                                visitedCount++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        border.RemoveAll(w => visited[w.x, w.y]);
                    }

                    // 3)  If at least 2 doors, find the true doors in Border: a true door is a door that gives onto an area big enough
                    trueDoors.Clear();

                    if (doors.Count >= 2)
                    {
                        foreach (var door in doors)
                        {
                            toVisit.Clear();
                            Array.Clear(visited, 0, visited.Length);

                            toVisit.Push(door);
                            visited[door.x, door.y] = true;
                            visitedCount = 1;

                            var limit = candidate is StaticBuilding ? 10 : 400;

                            while (toVisit.TryPop(out var current) && (visitedCount < limit))
                            {
                                foreach (var delta in deltas)
                                {
                                    var next = current + delta;
                                    if (Valid(next) && !visited[next.x, next.y])
                                    {
                                        var miniTile = GetTile(next, CheckMode.NoCheck);
                                        if (miniTile.Walkable)
                                        {
                                            var tile = GetTile(new TilePosition(next), CheckMode.NoCheck);
                                            if (tile.Neutral == null)
                                            {
                                                toVisit.Push(next);
                                                visited[next.x, next.y] = true;
                                                visitedCount++;
                                            }
                                        }
                                    }
                                }
                            }

                            if (visitedCount >= limit)
                            {
                                trueDoors.Add(door);
                            }
                        }
                    }

                    // 4)  If at least 2 true doors, pCandidate is a blocking static building
                    if (trueDoors.Count >= 2)
                    {
                        // Marks pCandidate (and any Neutral stacked with it) as blocking.
                        var tile = GetTile(candidate.TopLeft);

                        for (var neutral = tile.Neutral; neutral != null; neutral = neutral.NextStacked)
                        {
                            neutral.SetBlocking(trueDoors);
                        }

                        // Marks all the miniTiles of pCandidate as blocked.
                        // This way, areas at TrueDoors won't merge together.
                        for (var dy = 0; dy < new WalkPosition(candidate.Size).y; ++dy)
                        {
                            for (var dx = 0; dx < new WalkPosition(candidate.Size).x; ++dx)
                            {
                                var miniTile = GetTile(new WalkPosition(candidate.TopLeft) + new WalkPosition(dx, dy));
                                if (miniTile.Walkable)
                                {
                                    miniTile.SetBlocked();
                                }
                            }
                        }
                    }
                }
            }
        }

        // Assigns MiniTile::m_areaId for each miniTile having AreaIdMissing()
        // Areas are computed using MiniTile::Altitude() information only.
        // The miniTiles are considered successively in descending order of their Altitude().
        // Each of them either:
        //   - involves the creation of a new area.
        //   - is added to some existing neighbouring area.
        //   - makes two neighbouring areas merge together.
        private void ComputeAreas()
        {
            var miniTilesByDescendingAltitude = SortMiniTiles();
            var tempAreas = ComputeTempAreas(miniTilesByDescendingAltitude);
            CreateAreas(tempAreas);
            SetAreaIdInTiles();
        }

        private List<Pair<WalkPosition, MiniTile>> SortMiniTiles()
        {
            var miniTilesByDescendingAltitude = new List<Pair<WalkPosition, MiniTile>>();

            for (var y = 0; y < _walkSize.y; ++y)
            {
                for (var x = 0; x < _walkSize.x; ++x)
                {
                    var w = new WalkPosition(x, y);
                    var miniTile = GetTile(w, CheckMode.NoCheck);
                    if (miniTile.AreaIdMissing)
                    {
                        miniTilesByDescendingAltitude.Add(new Pair<WalkPosition, MiniTile>(w, miniTile));
                    }
                }
            }

            miniTilesByDescendingAltitude = miniTilesByDescendingAltitude.OrderBy(x => x.Second.Altitude).ToList();
            miniTilesByDescendingAltitude.Reverse();

            return miniTilesByDescendingAltitude;
        }

        private List<TempAreaInfo> ComputeTempAreas(List<Pair<WalkPosition, MiniTile>> MiniTilesByDescendingAltitude)
        {
            var tempAreas = new List<TempAreaInfo>() { new TempAreaInfo() }; // TempAreaList[0] left unused, as AreaIds are > 0
            foreach (var current in MiniTilesByDescendingAltitude)
            {
                var pos = current.First;
                var cur = current.Second;

                var neighboringAreas = FindNeighboringAreas(pos);
                if (neighboringAreas.First == 0) // no neighboring area : creates of a new area
                {
                    tempAreas.Add(new TempAreaInfo((AreaId)tempAreas.Count, cur, pos));
                }
                else if (neighboringAreas.Second == 0) // one neighboring area : adds cur to the existing area
                {
                    tempAreas[neighboringAreas.First.Value].Add(cur);
                }
                else // two neighboring areas : adds cur to one of them  &  possible merging
                {
                    var smaller = neighboringAreas.First;
                    var bigger = neighboringAreas.Second;

                    if (tempAreas[smaller.Value].Size > tempAreas[bigger.Value].Size)
                    {
                        (smaller, bigger) = (bigger, smaller);
                    }

                    // Condition for the neighboring areas to merge:
                    if ((tempAreas[smaller.Value].Size < 80) ||
                        (tempAreas[smaller.Value].HighestAltitude < 80) ||
                        (cur.Altitude.Value / (double)tempAreas[bigger.Value].HighestAltitude.Value >= 0.90) ||
                        (cur.Altitude.Value / (double)tempAreas[smaller.Value].HighestAltitude.Value >= 0.90) ||
                        _startingLocations.Any(startingLoc => Ex.Dist(new TilePosition(pos), startingLoc + new TilePosition(2, 1)) <= 3))
                    {
                        // adds cur to the absorbing area:
                        tempAreas[bigger.Value].Add(cur);

                        // merges the two neighboring areas:
                        ReplaceAreaIds(tempAreas[smaller.Value].Top, bigger);
                        tempAreas[bigger.Value].Merge(tempAreas[smaller.Value]);
                    }
                    else // no merge : cur starts or continues the frontier between the two neighboring areas
                    {
                        // adds cur to the chosen Area:
                        tempAreas[ChooseNeighboringArea(smaller, bigger).Value].Add(cur);
                        _rawFrontier.Add(new Pair<Pair<AreaId, AreaId>, WalkPosition>(neighboringAreas, pos));
                    }
                }
            }

            // Remove from the frontier obsolete positions
            _rawFrontier.RemoveAll(f => f.First.First == f.First.Second);

            return tempAreas;
        }

        private Pair<AreaId, AreaId> FindNeighboringAreas(WalkPosition p)
        {
            AreaId area1 = 0;
            AreaId area2 = 0;

            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.Top,
                WalkPosition.Left,
                WalkPosition.Right,
                WalkPosition.Bottom
            };

            foreach (var delta in deltas)
            {
                if (Valid(p + delta))
                {
                    var miniTile = GetTile(p + delta, CheckMode.NoCheck);
                    if (miniTile.AreaId > 0)
                    {
                        if (area1 == 0)
                        {
                            area1 = miniTile.AreaId;
                        }
                        else if (area1 != miniTile.AreaId)
                        {
                            if (area2 == 0 || (miniTile.AreaId < area2))
                            {
                                area2 = miniTile.AreaId;
                            }
                        }
                    }
                }
            }

            return new Pair<AreaId, AreaId>(area1, area2);
        }


        private AreaId ChooseNeighboringArea(AreaId a, AreaId b)
        {
            // return NeighboringAreaChooser.ChooseNeighboringArea(a, b);

            if (a > b)
            {
                (a, b) = (b, a);
            }

            var pair = new Pair<AreaId, AreaId>(a, b);

            if (!_mapAreaPairCounter.ContainsKey(pair))
            {
                _mapAreaPairCounter.Add(pair, 0);
            }

            return (_mapAreaPairCounter[pair]++ % 2 == 0) ? a : b;
        }

        // private static class NeighboringAreaChooser
        // {
        //     private static BitArray _areaPairFlag = new BitArray(800000);

        //     public static AreaId ChooseNeighboringArea(AreaId a, AreaId b)
        //     {
        //         var aId = a.Value;
        //         var bId = b.Value;

        //         var cantor = (aId + bId) * (aId + bId + 1);
        //         if (aId > bId)
        //         {
        //             cantor += bId;
        //         }
        //         else
        //         {
        //             cantor += aId;
        //         }

        //         if (_areaPairFlag.Length < cantor + 1)
        //         {
        //             _areaPairFlag.Length = cantor + 1;
        //         }

        //         Flip(cantor);
        //         return _areaPairFlag.Get(cantor) ? a : b;
        //     }

        //     private static void Flip(int index)
        //     {
        //         var value = _areaPairFlag.Get(index);
        //         _areaPairFlag.Set(index, !value);
        //     }
        // }

        private void ReplaceAreaIds(WalkPosition position, AreaId newAreaId)
        {
            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.Top,
                WalkPosition.Left,
                WalkPosition.Right,
                WalkPosition.Bottom
            };

            var origin = GetTile(position, CheckMode.NoCheck);
            var oldAreaId = origin.AreaId;
            origin.ReplaceAreaId(newAreaId);

            var toSearch = new Queue<WalkPosition>();
            toSearch.Enqueue(position);

            while (toSearch.TryDequeue(out var current))
            {
                foreach (var delta in deltas)
                {
                    var next = current + delta;
                    if (Valid(next))
                    {
                        var nextMiniTile = GetTile(next, CheckMode.NoCheck);
                        if (nextMiniTile.AreaId == oldAreaId)
                        {
                            toSearch.Enqueue(next);
                            nextMiniTile.ReplaceAreaId(newAreaId);
                        }
                    }
                }
            }

            // also replaces references of oldAreaId by newAreaId in m_RawFrontier:
            if (newAreaId > 0)
            {
                for (var i = 0; i < _rawFrontier.Count; i++)
                {
                    if (_rawFrontier[i].First.First == oldAreaId)
                    {
                        _rawFrontier[i] = new Pair<Pair<AreaId, AreaId>, WalkPosition>(new Pair<AreaId, AreaId>(newAreaId, _rawFrontier[i].First.Second), _rawFrontier[i].Second);
                    }

                    if (_rawFrontier[i].First.Second == oldAreaId)
                    {
                        _rawFrontier[i] = new Pair<Pair<AreaId, AreaId>, WalkPosition>(new Pair<AreaId, AreaId>(_rawFrontier[i].First.First, newAreaId), _rawFrontier[i].Second);
                    }
                }
            }
        }

        // Initializes m_Graph with the valid and big enough areas in TempAreaList.
        private void CreateAreas(List<TempAreaInfo> tempAreas)
        {
            var areas = new List<Pair<WalkPosition, int>>();

            AreaId newAreaId = 1;
            AreaId newTinyAreaId = -2;

            foreach (var tempArea in tempAreas)
            {
                if (tempArea.Valid)
                {
                    if (tempArea.Size >= AreaMinMiniTiles)
                    {
                        Debug.Assert(newAreaId <= tempArea.Id);

                        if (newAreaId != tempArea.Id)
                        {
                            ReplaceAreaIds(tempArea.Top, newAreaId);
                        }

                        areas.Add(new Pair<WalkPosition, int>(tempArea.Top, tempArea.Size));
                        newAreaId = new AreaId((short)(newAreaId.Value + 1));
                    }
                    else
                    {
                        ReplaceAreaIds(tempArea.Top, newTinyAreaId);
                        newTinyAreaId = new AreaId((short)(newTinyAreaId.Value - 1));
                    }
                }
            }

            _graph.CreateAreas(areas);
        }

        private void SetAreaIdInTiles()
        {
            for (var y = 0; y < _tileSize.Y; ++y)
            {
                for (var x = 0; x < _tileSize.x; ++x)
                {
                    var t = new TilePosition(x, y);
                    SetAreaIdInTile(t);
                    SetAltitudeInTile(t);
                }
            }
        }

        private void SetAreaIdInTile(TilePosition t)
        {
            var tile = GetTile(t);
            Debug.Assert(tile.AreaId == 0);	// initialized to 0

            for (var dy = 0; dy < 4; ++dy)
            {
                for (var dx = 0; dx < 4; ++dx)
                {
                    var miniTile = GetTile(new WalkPosition(t) + new WalkPosition(dx, dy), CheckMode.NoCheck);
                    if (miniTile.AreaId != 0)
                    {
                        if (tile.AreaId == 0)
                        {
                            tile.AreaId = miniTile.AreaId;
                        }
                        else if (tile.AreaId != miniTile.AreaId)
                        {
                            tile.AreaId = -1;
                            return;
                        }
                    }
                }
            }
        }

        private void SetAltitudeInTile(TilePosition t)
        {
            Altitude minAltitude = short.MaxValue;

            for (var dy = 0; dy < 4; ++dy)
            {
                for (var dx = 0; dx < 4; ++dx)
                {
                    var miniTile = GetTile(new WalkPosition(t) + new WalkPosition(dx, dy), CheckMode.NoCheck);
                    if (miniTile.Altitude < minAltitude)
                    {
                        minAltitude = miniTile.Altitude;
                    }
                }
            }

            var tile = GetTile(t);
            tile.MinAltitude = minAltitude;
        }

        private bool SeaSide(WalkPosition p)
        {
            if (!GetTile(p).Sea)
            {
                return false;
            }

            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.Top,
                WalkPosition.Left,
                WalkPosition.Right,
                WalkPosition.Bottom
            };

            foreach (var delta in deltas)
            {
                if (Valid(p + delta))
                {
                    var tile = GetTile(p + delta, CheckMode.NoCheck);
                    if (!tile.Sea)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Adjoins8SomeLakeOrNeutral(WalkPosition p)
        {
            Span<WalkPosition> deltas = stackalloc[]
            {
                WalkPosition.TopLeft,
                WalkPosition.Top,
                WalkPosition.TopRight,

                WalkPosition.Left,
                WalkPosition.Right,

                WalkPosition.BottomLeft,
                WalkPosition.Bottom,
                WalkPosition.BottomRight
            };

            foreach (var delta in deltas)
            {
                var next = p + delta;
                if (Valid(next))
                {
                    var tile = GetTile(new TilePosition(next), CheckMode.NoCheck);
                    if (tile.Neutral != null)
                    {
                        return true;
                    }

                    var miniTile = GetTile(next, CheckMode.NoCheck);
                    if (miniTile.Lake)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        class ActiveSeaSide
        {
            public WalkPosition Origin;
            public Altitude LastAltitudeGenerated;

            public ActiveSeaSide(WalkPosition origin, Altitude lastAltitudeGenerated)
            {
                Origin = origin;
                LastAltitudeGenerated = lastAltitudeGenerated;
            }
        }

        // Helper class for void Map::ComputeAreas()
        // Maintains some information about an area being computed
        // A TempAreaInfo is not Valid() in two cases:
        //   - a default-constructed TempAreaInfo instance is never Valid (used as a dummy value to simplify the algorithm).
        //   - any other instance becomes invalid when absorbed (see Merge)
        class TempAreaInfo
        {
            private bool _valid;
            private readonly AreaId _id;
            private readonly WalkPosition _top;
            private readonly Altitude _highestAltitude;
            private int _size;

            public TempAreaInfo()
            {
                _valid = false;
                _id = 0;
                _top = WalkPosition.Origin;
                _size = 0;
                _highestAltitude = 0;
            }

            public TempAreaInfo(AreaId id, MiniTile miniTile, WalkPosition pos)
            {
                _valid = true;
                _id = id;
                _top = pos;
                _size = 0;
                _highestAltitude = miniTile.Altitude;

                Add(miniTile);
            }

            public bool Valid
            {
                get => _valid;
            }

            public AreaId Id
            {
                get
                {
                    Debug.Assert(_valid);
                    return _id;
                }
            }

            public WalkPosition Top
            {
                get
                {
                    Debug.Assert(_valid);
                    return _top;
                }
            }

            public int Size
            {
                get
                {
                    Debug.Assert(_valid);
                    return _size;
                }
            }

            public Altitude HighestAltitude
            {
                get
                {
                    Debug.Assert(_valid);
                    return _highestAltitude;
                }
            }

            public void Add(MiniTile miniTile)
            {
                Debug.Assert(_valid);
                _size++;
                miniTile.AreaId = _id;
            }

            public void Merge(TempAreaInfo other)
            {
                Debug.Assert(_valid && other._valid);
                Debug.Assert(_size >= other._size);
                _size += other._size;
                other._valid = false;
            }
        }
    }

    public enum CheckMode
    {
        NoCheck,
        Check
    }
}
