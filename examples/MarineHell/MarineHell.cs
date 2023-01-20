using BWAPI.NET;
using BWEM.NET;
using Nito.Collections;

namespace MarineHell
{
    // https://github.com/libor-vilimek/marine-hell
    // http://sscaitournament.com/index.php?action=tutorial

    public class MarineHell : DefaultBWListener
    {
        private BWClient _bwClient;
        private Game _game;
        private Player _self;
        private int _frameskip = 0;
        private int _cyclesForSearching = 0;
        private int _maxCyclesForSearching = 0;
        private int _searchingScv = 0;
        private int _searchingTimeout = 0;
        private bool _dontBuild = false;
        private int _timeout = 0;
        private Unit _bunkerBuilder;
        private Unit _searcher;
        private string _debugText = "";
        private readonly HashSet<Position> _enemyBuildingMemory = new HashSet<Position>();
        private Strategy _selectedStrategy = Strategy.WaitFor50;

        public void Run()
        {
            _bwClient = new BWClient(this);
            _bwClient.StartGame();
        }

        public override void OnStart()
        {
            _game = _bwClient.Game;

            _frameskip = 0;
            _cyclesForSearching = 0;
            _maxCyclesForSearching = 0;
            _searchingScv = 0;
            _searchingTimeout = 0;
            _dontBuild = false;
            _timeout = 0;
            _bunkerBuilder = null;
            _searcher = null;

            _self = _game.Self();
            _game.SetLocalSpeed(0);

            // Use BWTA to analyze map
            // This may take a few minutes if the map is processed first time!
            BWTA.Initialize(_game);
        }

        public override void OnFrame()
        {
            // _game.setTextSize(10);
            _game.DrawTextScreen(10, 10, $"Playing as {_self.GetName()} - {_self.GetRace()}");
            _game.DrawTextScreen(10, 20, $"Units: {_self.GetUnits().Count}; Enemies: {_enemyBuildingMemory.Count}");
            _game.DrawTextScreen(10, 30, $"Cycles for buildings: {_cyclesForSearching}; Max cycles: {_maxCyclesForSearching}");
            _game.DrawTextScreen(10, 40, $"Elapsed time: {_game.ElapsedTime()}; Strategy: {_selectedStrategy}");
            _game.DrawTextScreen(10, 50, _debugText);
            _game.DrawTextScreen(10, 60, $"supply: {_self.SupplyTotal()} used: {_self.SupplyUsed()}");

            /*
            * if (_game.elapsedTime() > 2001) { int x = (_game.elapsedTime() / 500) %
            * 2; if (x == 0) { selectedStrategy = Strategy.FindEnemy; } else {
            * selectedStrategy = Strategy.HugeAttack; } }
            */

            if (_maxCyclesForSearching > 300000)
            {
                _dontBuild = true;
            }

            _game.SetLocalSpeed(0);

            if (_maxCyclesForSearching < _cyclesForSearching)
            {
                _maxCyclesForSearching = _cyclesForSearching;
            }

            _cyclesForSearching = 0;

            var workers = new List<Unit>();
            var barracks = new List<Unit>();
            var marines = new List<Unit>();
            var baseLocations = new List<BaseLocation>();
            var allLocations = new List<BaseLocation>();
            var workerAttacked = Position.Invalid;
            Unit commandCenter = null;
            Unit bunker = null;

            if (_bunkerBuilder != null && _bunkerBuilder.Exists() == false)
            {
                _bunkerBuilder = null;
            }

            if (_searcher != null && _searcher.Exists() == false)
            {
                _searcher = null;
            }

            if (_searcher != null)
            {
                _game.DrawTextMap(_searcher.GetPosition(), "Mr. Searcher");
            }

            // iterate through my units
            foreach (var myUnit in _self.GetUnits())
            {
                // units.append(myUnit.getType()).append("
                // ").append(myUnit.getTilePosition()).append("\n");

                if (myUnit.GetUnitType().IsWorker())
                {
                    workers.Add(myUnit);
                }

                // if there's enough minerals, train an SCV
                if (myUnit.GetUnitType() == UnitType.Terran_Command_Center)
                {
                    commandCenter = myUnit;
                }

                if (myUnit.GetUnitType() == UnitType.Terran_Barracks && myUnit.IsBeingConstructed() == false)
                {
                    barracks.Add(myUnit);
                }

                if (myUnit.GetUnitType() == UnitType.Terran_Marine)
                {
                    marines.Add(myUnit);
                }

                if (myUnit.GetUnitType() == UnitType.Terran_Bunker && myUnit.IsBeingConstructed() == false)
                {
                    bunker = myUnit;
                }

                if (myUnit.IsUnderAttack() && myUnit.CanAttack())
                {
                    _game.SetLocalSpeed(1);
                    myUnit.Attack(myUnit.GetPosition());
                }

            }

            foreach (var myUnit in workers)
            {
                // if it's a worker and it's idle, send it to the closest mineral
                // patch
                if (myUnit.GetUnitType().IsWorker() && myUnit.IsIdle())
                {
                    var skip = false;
                    if (bunker == null && _bunkerBuilder != null && myUnit.Equals(_bunkerBuilder) && barracks.Count > 0)
                    {
                        skip = true;
                    }

                    Unit closestMineral = null;

                    // find the closest mineral
                    foreach (var neutralUnit in _game.Neutral().GetUnits())
                    {
                        if (neutralUnit.GetUnitType().IsMineralField())
                        {
                            if (closestMineral == null || myUnit.GetDistance(neutralUnit) < myUnit.GetDistance(closestMineral))
                            {
                                closestMineral = neutralUnit;
                            }
                        }
                    }

                    // if a mineral patch was found, send the worker to gather it
                    if (closestMineral != null)
                    {
                        if (skip == false)
                        {
                            myUnit.Gather(closestMineral, false);
                        }
                    }
                }

                if (myUnit.IsUnderAttack() && myUnit.CanAttack())
                {
                    _game.SetLocalSpeed(1);
                    myUnit.Attack(myUnit.GetPosition());
                }

                if (myUnit.IsUnderAttack() && myUnit.IsGatheringMinerals())
                {
                    workerAttacked = myUnit.GetPosition();
                }
            }

            if (_bunkerBuilder == null && workers.Count > 10)
            {
                _bunkerBuilder = workers[10];
            }

            if (bunker == null && barracks.Count >= 1 && workers.Count > 10 && _dontBuild == false)
            {
                _game.SetLocalSpeed(20);

                if (_timeout < 200)
                {
                    _game.DrawTextMap(_bunkerBuilder.GetPosition(), "Moving to create bunker " + _timeout + "/400");
                    _bunkerBuilder.Move(BWTA.GetNearestChokepoint(_bunkerBuilder.GetPosition()).Center);
                    _timeout++;
                }
                else
                {
                    _game.DrawTextMap(_bunkerBuilder.GetPosition(), "Buiding bunker");
                    var buildTile = getBuildTile(_bunkerBuilder, UnitType.Terran_Barracks, _bunkerBuilder.GetTilePosition());
                    if (buildTile != TilePosition.Invalid)
                    {
                        _bunkerBuilder.Build(UnitType.Terran_Bunker, buildTile);
                    }
                }
            }
            else if (workers.Count > 10)
            {
                _game.SetLocalSpeed(10);
                _game.DrawTextMap(workers[10].GetPosition(), "He will build bunker");
            }

            if (bunker != null && _bunkerBuilder != null && _bunkerBuilder.IsRepairing() == false)
            {
                _game.DrawTextMap(_bunkerBuilder.GetPosition(), "Reparing bunker");
                _bunkerBuilder.Repair(bunker);
            }

            if (commandCenter.GetTrainingQueue().Count == 0 && workers.Count < 20 && _self.Minerals() >= 50)
            {
                commandCenter.Build(UnitType.Terran_SCV);
            }

            _frameskip++;
            if (_frameskip == 20)
            {
                _frameskip = 0;
            }

            if (_frameskip != 0)
            {
                return;
            }

            _searchingTimeout++;

            var i = 1;
            foreach (var worker in workers)
            {
                if (worker.IsGatheringMinerals() && _dontBuild == false)
                {
                    if (_self.Minerals() >= 150 * i && barracks.Count < 6)
                    {
                        var buildTile = getBuildTile(worker, UnitType.Terran_Barracks, _self.GetStartLocation());
                        if (buildTile != TilePosition.Invalid)
                        {
                            worker.Build(UnitType.Terran_Barracks, buildTile);
                        }
                    }

                    if (_self.Minerals() >= i * 100 && _self.SupplyUsed() + (_self.SupplyUsed() / 3) >= _self.SupplyTotal() && _self.SupplyTotal() < 400)
                    {
                        var buildTile = getBuildTile(worker, UnitType.Terran_Supply_Depot, _self.GetStartLocation());
                        // and, if found, send the worker to build it (and leave
                        // others
                        // alone - break;)
                        if (buildTile != TilePosition.Invalid)
                        {
                            worker.Build(UnitType.Terran_Supply_Depot, buildTile);
                        }
                    }
                }

                i++;
            }

            foreach (var barrack in barracks)
            {
                if (barrack.GetTrainingQueue().Count == 0)
                {
                    barrack.Build(UnitType.Terran_Marine);
                }
            }

            foreach (var b in BWTA.BaseLocations)
            {
                // If this is a possible start location,
                if (b.IsStartLocation)
                {
                    baseLocations.Add(b);
                }

                allLocations.Add(b);
            }

            var k = 0;
            foreach (var marine in marines)
            {
                if (marine.IsAttacking() == false && marine.IsMoving() == false)
                {
                    if (marines.Count > 50 || _selectedStrategy == Strategy.AttackAtAllCost)
                    {
                        if (marines.Count > 40)
                        {
                            _selectedStrategy = Strategy.AttackAtAllCost;
                        }
                        else
                        {
                            _selectedStrategy = Strategy.WaitFor50;
                        }
                        if (_enemyBuildingMemory.Count == 0)
                        {
                            marine.Attack(allLocations[k % allLocations.Count].Position);
                        }
                        else
                        {
                            foreach (var p in _enemyBuildingMemory)
                            {
                                marine.Attack(p);
                            }
                        }

                        if (marines.Count > 70)
                        {
                            if (k < allLocations.Count)
                            {
                                marine.Attack(allLocations[k].Position);
                            }
                        }
                    }
                    else
                    {
                        Position newPos;

                        if (bunker != null)
                        {
                            var path = BWTA.GetShortestPath(bunker.GetTilePosition(), BWTA.GetStartLocation(_game.Self()).TilePosition);

                            if (path.Count > 1)
                            {
                                newPos = path[1].ToPosition();
                            }
                            else
                            {
                                newPos = BWTA.GetNearestChokepoint(marine.GetPosition()).Center;
                            }
                        }
                        else
                        {
                            newPos = BWTA.GetNearestChokepoint(marine.GetPosition()).Center;
                        }

                        marine.Attack(newPos);
                    }
                }
                k++;

                if (bunker != null && bunker.GetLoadedUnits().Count < 4 && k < 5)
                {
                    marine.Load(bunker);
                }

                if (workerAttacked != Position.Invalid)
                {
                    marine.Attack(workerAttacked);
                }
            }

            if (workers.Count > 7 && _searcher == null)
            {
                _searcher = workers[7];
            }

            if (_searcher != null && _searcher.IsGatheringMinerals() && _searchingScv < baseLocations.Count && _searchingTimeout % 10 == 0)
            {
                _searcher.Move(baseLocations[_searchingScv].Position);
                _searchingScv++;
            }

            _debugText = "Size: " + workers.Count + "; isGathering" + (workers.Count > 7 ? workers[7].IsGatheringMinerals() : workers[^1].IsGatheringMinerals()) + "; location: " + baseLocations.Count + "; num: " + _searchingScv;

            foreach (var u in _game.Enemy().GetUnits())
            {
                // if this unit is in fact a building
                if (u.GetUnitType().IsBuilding())
                {
                    // check if we have it's position in memory and add it if we
                    // don't
                    if (!_enemyBuildingMemory.Contains(u.GetPosition()))
                        _enemyBuildingMemory.Add(u.GetPosition());
                }
            }

            // loop over all the positions that we remember
            foreach (var p in _enemyBuildingMemory)
            {
                // compute the TilePosition corresponding to our remembered Position
                // p
                var tileCorrespondingToP = new TilePosition(p.X / 32, p.Y / 32);

                // if that tile is currently visible to us...
                if (_game.IsVisible(tileCorrespondingToP))
                {

                    // loop over all the visible enemy buildings and find out if at
                    // least
                    // one of them is still at that remembered position
                    var buildingStillThere = false;
                    foreach (var u in _game.Enemy().GetUnits())
                    {
                        if ((u.GetUnitType().IsBuilding()) && (u.GetPosition() == p))
                        {
                            buildingStillThere = true;
                            break;
                        }
                    }

                    // if there is no more any building, remove that position from
                    // our memory
                    if (buildingStillThere == false)
                    {
                        _enemyBuildingMemory.Remove(p);
                        break;
                    }
                }
            }

            // draw my units on screen
            // _game.drawTextScreen(10, 25, units.toString());
        }

        // Returns a suitable TilePosition to build a given building type near
        // specified TilePosition aroundTile, or null if not found. (builder
        // parameter is our worker)
        public TilePosition getBuildTile(Unit builder, UnitType buildingType, TilePosition aroundTile)
        {
            var ret = TilePosition.Invalid;
            var maxDist = 3;
            var stopDist = 40;

            // Refinery, Assimilator, Extractor
            if (buildingType.IsRefinery())
            {
                foreach (var n in _game.Neutral().GetUnits())
                {
                    _cyclesForSearching++;
                    if ((n.GetUnitType() == UnitType.Resource_Vespene_Geyser) &&
                        (Math.Abs(n.GetTilePosition().X - aroundTile.X) < stopDist) &&
                        (Math.Abs(n.GetTilePosition().Y - aroundTile.Y) < stopDist))
                    {
                        return n.GetTilePosition();
                    }
                }
            }

            while ((maxDist < stopDist) && (ret == TilePosition.Invalid))
            {
                for (var i = aroundTile.X - maxDist; i <= aroundTile.X + maxDist; i++)
                {
                    for (var j = aroundTile.Y - maxDist; j <= aroundTile.Y + maxDist; j++)
                    {
                        if (_game.CanBuildHere(new TilePosition(i, j), buildingType, builder, false))
                        {
                            // units that are blocking the tile
                            var unitsInWay = false;
                            foreach (var u in _game.GetAllUnits())
                            {
                                _cyclesForSearching++;
                                if (u.GetID() == builder.GetID())
                                    continue;
                                if ((Math.Abs(u.GetTilePosition().X - i) < 4) && (Math.Abs(u.GetTilePosition().Y - j) < 4))
                                    unitsInWay = true;
                            }

                            if (!unitsInWay)
                            {
                                _cyclesForSearching++;
                                return new TilePosition(i, j);
                            }

                            // creep for Zerg
                            if (buildingType.RequiresCreep())
                            {
                                var creepMissing = false;
                                for (var k = i; k <= i + buildingType.TileWidth(); k++)
                                {
                                    for (var l = j; l <= j + buildingType.TileHeight(); l++)
                                    {
                                        _cyclesForSearching++;

                                        if (!_game.HasCreep(k, l))
                                        {
                                            creepMissing = true;
                                        }

                                        break;
                                    }
                                }

                                if (creepMissing)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                maxDist += 2;
            }

            if (ret == TilePosition.Invalid)
            {
                _game.Printf("Unable to find suitable build position for " + buildingType.ToString());
            }

            return ret;
        }

        static class BWTA
        {
            internal static Dictionary<Area, Region> _regionMap;
            internal static Dictionary<ChokePoint, Chokepoint> _chokeMap;
            internal static Dictionary<Base, BaseLocation> _baseMap;

            public static void Initialize(Game game)
            {
                Map.Instance.Initialize(game);

                _regionMap = new Dictionary<Area, Region>();
                foreach (var a in Map.Instance.Areas)
                {
                    _regionMap[a] = new Region(a);
                }

                _chokeMap = new Dictionary<ChokePoint, Chokepoint>();
                foreach (var c in Map.Instance.ChokePoints)
                {
                    _chokeMap[c] = new Chokepoint(c);
                }

                _baseMap = new Dictionary<Base, BaseLocation>();
                foreach (var b in Map.Instance.Bases)
                {
                    _baseMap[b] = new BaseLocation(b);
                }
            }

            public static List<Region> Regions
            {
                get => _regionMap.Values.ToList();
            }

            public static List<Chokepoint> Chokepoints
            {
                get => _chokeMap.Values.ToList();
            }

            public static List<BaseLocation> BaseLocations
            {
                get => _baseMap.Values.ToList();
            }

            public static List<BaseLocation> StartLocations
            {
                get => BaseLocations.Where(x => x.IsStartLocation).ToList();
            }

            public static BaseLocation GetStartLocation(Player player)
            {
                return GetNearestBaseLocation(player.GetStartLocation());
            }

            public static Region GetRegion(TilePosition tileposition)
            {
                return _regionMap[Map.Instance.GetNearestArea(tileposition)];
            }

            public static Region GetRegion(Position position)
            {
                return _regionMap[Map.Instance.GetNearestArea(position.ToWalkPosition())];
            }

            public static Chokepoint GetNearestChokepoint(TilePosition tileposition)
            {
                return GetNearestChokepoint(tileposition.ToPosition());
            }

            public static Chokepoint GetNearestChokepoint(Position position)
            {
                return Enumerable.MinBy(Chokepoints, x => x.Center.GetDistance(position));
            }

            public static BaseLocation GetNearestBaseLocation(TilePosition tilePosition)
            {
                return Enumerable.MinBy(BaseLocations, x => x.TilePosition.GetDistance(tilePosition));
            }

            public static BaseLocation GetNearestBaseLocation(Position position)
            {
                return Enumerable.MinBy(BaseLocations, x => x.Position.GetDistance(position));
            }

            public static bool IsConnected(TilePosition a, TilePosition b)
            {
                return Map.Instance.GetNearestArea(a).AccessibleFrom(Map.Instance.GetNearestArea(b));
            }

            public static double GetGroundDistance(TilePosition start, TilePosition end)
            {
                var path = Map.Instance.GetPath(start.ToPosition(), end.ToPosition(), out var distance);
                return path.Count > 0 ? distance : -1.0;
            }

            public static List<TilePosition> GetShortestPath(TilePosition start, TilePosition end)
            {
                var shortestPath = new List<TilePosition>();

                var it = Map.Instance.GetPath(start.ToPosition(), end.ToPosition(), out _).GetEnumerator();

                ChokePoint curr = null;

                while (it.MoveNext())
                {
                    var next = it.Current;
                    if (curr != null)
                    {
                        var t0 = curr.Center.ToTilePosition();
                        var t1 = next.Center.ToTilePosition();

                        //trace a ray
                        var dx = Math.Abs(t1.x - t0.x);
                        var dy = Math.Abs(t1.y - t0.y);
                        var x = t0.x;
                        var y = t0.y;
                        var n = 1 + dx + dy;
                        var x_inc = (t1.x > t0.x) ? 1 : -1;
                        var y_inc = (t1.x > t0.x) ? 1 : -1;
                        var error = dx - dy;

                        dx *= 2;
                        dy *= 2;

                        for (; n > 0; --n)
                        {
                            shortestPath.Add(new TilePosition(x, y));

                            if (error > 0)
                            {
                                x += x_inc;
                                error -= dy;
                            }
                            else
                            {
                                y += y_inc;
                                error += dx;
                            }
                        }
                    }

                    curr = next;
                }

                return shortestPath;
            }
        }

        class Chokepoint
        {
            private readonly ChokePoint _chokePoint;
            private readonly Pair<Position, Position> _sides;
            private readonly Position _center;
            private readonly double _width;

            internal Chokepoint(ChokePoint chokePoint)
            {
                _chokePoint = chokePoint;
                _sides = CalculateSides(chokePoint.Geometry);
                _center = (_sides.Left + _sides.Right) / 2;
                _width = _sides.Left.GetDistance(_sides.Right);
            }

            public Pair<Region, Region> Regions
            {
                get => new(BWTA._regionMap[_chokePoint.Areas.Left], BWTA._regionMap[_chokePoint.Areas.Right]);
            }

            public Pair<Position, Position> Sides
            {
                get => _sides;
            }

            public Position Center
            {
                get => _center;
            }

            public double Width
            {
                get => _width;
            }

            public bool equals(object o)
            {
                return o is Chokepoint chokepoint && _chokePoint.Equals(chokepoint._chokePoint);
            }

            public int hashCode()
            {
                return _chokePoint.GetHashCode();
            }

            private static Pair<Position, Position> CalculateSides(Deque<WalkPosition> wp)
            {
                var p1 = wp[0];
                var p2 = wp[0];
                var d_max = -1.0;

                for (var i = 0; i < wp.Count; i++)
                {
                    for (var j = i + 1; j < wp.Count; j++)
                    {
                        var d = wp[i].GetDistance(wp[j]);
                        if (d > d_max)
                        {
                            d_max = d;
                            p1 = wp[i];
                            p2 = wp[j];
                        }
                    }
                }

                return new Pair<Position, Position>(p1.ToPosition(), p2.ToPosition());
            }
        }

        class Region : IEquatable<Region>
        {
            private readonly Area _area;
            private readonly Position _center;

            internal Region(Area area)
            {
                _area = area;
                _center = area.Top.ToPosition();
            }

            public Position Center
            {
                get => _center;
            }

            public List<Chokepoint> Chokepoints
            {
                get => _area.ChokePoints.Select(x => BWTA._chokeMap[x]).ToList();
            }

            public List<BaseLocation> BaseLocations
            {
                get => _area.Bases.Select(x => BWTA._baseMap[x]).ToList();
            }

            public bool IsReachable(Region region)
            {
                return _area.AccessibleFrom(region._area);
            }

            public List<Region> GetReachableRegions()
            {
                return _area.AccessibleNeighbours.Select(x => BWTA._regionMap[x]).ToList();
            }

            public bool Equals(Region other)
            {
                return _area.Equals(other._area);
            }

            public override bool Equals(object o)
            {
                return o is Region other && Equals(other);
            }

            public override int GetHashCode()
            {
                return _area.GetHashCode();
            }
        }

        class BaseLocation : IEquatable<BaseLocation>
        {
            private readonly Base _base;
            private readonly Position _position;
            private readonly TilePosition _tilePosition;
            private readonly int _mineralsAmount;
            private readonly int _gasAmount;
            private readonly List<Unit> _minerals;
            private readonly List<Unit> _geysers;
            private readonly bool _isIsland;
            private readonly bool _isMineralOnly;
            private readonly bool _isStartLocation;

            internal BaseLocation(Base base_)
            {
                _base = base_;
                _position = base_.Center;
                _tilePosition = base_.Location;
                _mineralsAmount = 1;
                _gasAmount = 1;
                _minerals = base_.Minerals.Select(x => x.Unit).ToList();
                _geysers = base_.Geysers.Select(x => x.Unit).ToList();
                _isIsland = base_.Area.AccessibleNeighbours.Count == 0;
                _isMineralOnly = _minerals.Count > 0 && _geysers.Count == 0;
                _isStartLocation = base_.Starting;
            }

            public Position Position
            {
                get => _position;
            }

            public TilePosition TilePosition
            {
                get => _tilePosition;
            }

            public Region Region
            {
                get => BWTA._regionMap[_base.Area];
            }

            public int Minerals
            {
                get => _mineralsAmount;
            }

            public int Gas
            {
                get => _gasAmount;
            }

            public List<Unit> GetMinerals()
            {
                return _minerals;
            }

            public List<Unit> GetGeysers()
            {
                return _geysers;
            }

            public double GetGroundDistance(BaseLocation other)
            {
                return BWTA.GetGroundDistance(_tilePosition, other._tilePosition);
            }

            public double GetAirDistance(BaseLocation other)
            {
                return _position.GetDistance(other._position);
            }

            public bool IsIsland
            {
                get => _isIsland;
            }

            public bool IsMineralOnly
            {
                get => _isMineralOnly;
            }

            public bool IsStartLocation
            {
                get => _isStartLocation;
            }

            public bool Equals(BaseLocation other)
            {
                return _base.Equals(other._base);
            }

            public override bool Equals(object o)
            {
                return o is BaseLocation other && Equals(other);
            }

            public override int GetHashCode()
            {
                return _base.GetHashCode();
            }
        }

        private enum Strategy
        {
            WaitFor50,
            AttackAtAllCost
        };
    }
}