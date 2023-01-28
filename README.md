# BWEM.NET

A pure .NET BWEM 1.4 implementation. It's ported from the [BWEM](https://bwem.sourceforge.net/) library.

Brood War Easy Map for .NET library that analyses Brood War's maps and provides relevant information such as areas, choke points and base locations.

It is built on top of the [BWAPI.NET](https://github.com/acoto87/bwapi.net) library.

It first aims at simplifying the development of bots for Brood War, but can be used for any task requiring high level map information. It can be used as a replacement for the BWTA2 add-on, as it performs faster and shows better robustness while providing similar information.

## Capabilities

 - To access general information on the Map.
 - To access the Tiles (32x32) and the MiniTiles (8x8) information.
 - To access the Areas information.
 - To access the Starting Locations information.
 - To access the Minerals, the Geysers and the Static Buildings information.
 - To parametrize the analysis process.
 - Provides some useful tools such as Paths between Choke Points and generic algorithms like Breadth First Search.

## Quick Start

1. Installation
    * Install [.NET SDK](https://dotnet.microsoft.com/en-us/download)
    * Install **StarCraft: Brood War**
    * Update **StarCraft: Brood War to 1.16.1**
    * Install [BWAPI](https://bwapi.github.io/)
2. Create a bot project
    * Run `dotnet new console -o MyBot`
    * Run `cd MyBot` to change directy into `MyBot` folder
    * Run `dotnet add MyBot.csproj package BWAPI.NET` to add the reference to the BWAPI.NET nuget package
    * Run `dotnet add MyBot.csproj package BWEM.NET` to add the reference to the BWEM.NET nuget package
    * Copy and paste example bot below into `Program.cs` or develop your own bot
    * Run `dotnet run` (At this point you should see _"Game table mapping not found."_ printed each second)
3. Run StarCraft through **Chaoslauncher**
    * Run _Chaoslauncher.exe_ as administrator
        * Chaoslauncher is found in Chaoslauncher directory of [BWAPI](https://bwapi.github.io/) install directory
    * Check the _BWAPI Injector x.x.x [RELEASE]_
    * Click Start
        * Make sure the version is set to Starcraft 1.16.1, not ICCup 1.16.1
4. Run a game against Blizzard's AI
    * Go to **Single Player** -> **Expansion**
    * Select any user and click **OK**
    * Click **Play Custom**, select a map, and start a game
5. Run a game against yourself
    * Run _Chaoslauncher - MultiInstance.exe_ as administrator
    * Start
        * Go to **Multiplayer** -> **Expansion** -> **Local PC**
        * Select any user and click **OK**
        * Click **Create Game**, select a map, and click **OK**
    * Start – Uncheck _BWAPI Injector x.x.x [RELEASE]_ to let a human play, leave alone to make AI play itself
        * Go to **Multiplayer** -> **Expansion** -> **Local PC**
        * Select any user and click **OK**
        * Join the existing game created by the other client

## Bot Example

```csharp
using BWAPI.NET;
using BWEM.NET;

namespace ExampleBot
{
    public class ExampleBot : DefaultBWListener
    {
        private BWClient _bwClient;
        private Game _game;
        private Map _map;
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

            _map = new Map(_game);
            _map.Initialize();
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
                    var nearestChokePoint = _map.ChokePoints.MinBy(x => x.Center.ToPosition().GetDistance(marinePosition));
                    var nearestChokePointPosition = nearestChokePoint.Center.ToPosition();
                    marine.Move(nearestChokePointPosition, false);
                }
            }
        }
    }
}

```

## Limitations

BWEM doesn't provide any geometric description (polygon) of the computed areas.

## Legal

[Starcraft](https://www.blizzard.com/games/sc/) and [Starcraft: Broodwar](https://www.blizzard.com/games/sc/) are trademarks of [Blizzard Entertainment](https://www.blizzard.com/). [BWAPI.NET](https://github.com/acoto87/bwapi.net) through [BWAPI](https://bwapi.github.io/) is a third party "hack" that violates the End User License Agreement (EULA). It is strongly recommended to purchase a legitimate copy of Starcraft: Broodwar from Blizzard Entertainment before using [BWAPI.NET](https://github.com/acoto87/bwapi.net) and/or [BWAPI](https://bwapi.github.io/).