using BWAPI.NET;
using BWEM.NET;

namespace ExampleBot
{
    /// <summary>
    /// Example bot that attempts to build marines and send them to the nearest chokepoint in the map.
    /// For that is start by building SCV and then necessary supply depots a barrack and then the marines.
    /// </summary>
    public class ExampleBot : DefaultBWListener
    {
        private BWClient _bwClient;
        private Game _game;
        private Player _self;

        public void Run()
        {
            _bwClient = new BWClient(this);
            _bwClient.StartGame();
        }

        public override void OnStart()
        {
            _game = _bwClient.Game;
            _self = _game.Self();

            Map.Instance.Initialize(_game);
        }

        public override void OnFrame()
        {
            var workers = new List<Unit>();
            var marines = new List<Unit>();
            Unit barrack = null;
            Unit commandCenter = null;

            var buildingSupply = false;

            // iterate through my units and collect relevant information
            foreach (var myUnit in _self.GetUnits())
            {
                var unitType = myUnit.GetUnitType();
                if (unitType == UnitType.Terran_SCV)
                {
                    workers.Add(myUnit);
                }
                else if (unitType == UnitType.Terran_Command_Center)
                {
                    commandCenter = myUnit;
                }
                else if (unitType == UnitType.Terran_Barracks)
                {
                    if (!myUnit.IsBeingConstructed())
                    {
                        barrack = myUnit;
                    }
                }
                else if (unitType == UnitType.Terran_Marine)
                {
                    marines.Add(myUnit);
                }
                else if (unitType == UnitType.Terran_Supply_Depot)
                {
                    if (myUnit.IsBeingConstructed())
                    {
                        buildingSupply = true;
                    }
                }
            }

            // if it's a worker and it's idle, send it to the closest mineral patch
            foreach (var myUnit in workers)
            {
                if (myUnit.GetUnitType().IsWorker() && myUnit.IsIdle())
                {
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
                        myUnit.Gather(closestMineral, false);
                    }
                }
            }

            // build SCV if we can
            if (commandCenter != null && commandCenter.GetTrainingQueue().Count == 0 && workers.Count < 12 && _self.Minerals() >= 50)
            {
                commandCenter.Build(UnitType.Terran_SCV);
            }

            var i = 1;
            foreach (var worker in workers)
            {
                if (worker.IsGatheringMinerals())
                {
                    // check if we can build a barrack and build it
                    if (_self.Minerals() >= 150 * i && barrack == null)
                    {
                        var buildTile = BuildingPlacer.GetBuildLocation(UnitType.Terran_Barracks, _self.GetStartLocation(), 40, false, _game);
                        if (buildTile != TilePosition.Invalid)
                        {
                            worker.Build(UnitType.Terran_Barracks, buildTile);
                        }
                    }

                    // check if we can and need to build a supply depot and build it
                    if (_self.Minerals() >= i * 100 && _self.SupplyUsed() + (_self.SupplyUsed() / 3) >= _self.SupplyTotal() && _self.SupplyTotal() < 400 && !buildingSupply)
                    {
                        var buildTile = BuildingPlacer.GetBuildLocation(UnitType.Terran_Supply_Depot, _self.GetStartLocation(), 40, false, _game);
                        if (buildTile != TilePosition.Invalid)
                        {
                            worker.Build(UnitType.Terran_Supply_Depot, buildTile);
                            buildingSupply = true;
                        }
                    }
                }

                i++;
            }

            // build a marine if we can
            if (barrack != null && !barrack.IsBeingConstructed() && barrack.GetTrainingQueue().Count == 0)
            {
                barrack.Build(UnitType.Terran_Marine);
            }

            // move the marines to the nearest chokepoint in the map
            foreach (var marine in marines)
            {
                if (!marine.IsMoving())
                {
                    var marinePosition = marine.GetPosition();
                    var nearestChokePoint = Map.Instance.ChokePoints.MinBy(x => x.Center.ToPosition().GetDistance(marinePosition));
                    var nearestChokePointPosition = nearestChokePoint.Center.ToPosition();
                    marine.Move(nearestChokePointPosition, false);
                }
            }
        }
    }
}