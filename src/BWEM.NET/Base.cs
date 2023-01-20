using System.Collections.Generic;
using System.Diagnostics;
using BWAPI.NET;

namespace BWEM.NET
{
    /// <summary>
    /// After Areas and ChokePoints, Bases are the third kind of object BWEM automatically computes from Brood War's maps.
    /// A Base is essentially a suggested location (intended to be optimal) to put a Command Center, Nexus, or Hatchery.
    /// It also provides information on the ressources available, and some statistics.
    /// A Base alway belongs to some Area. An Area may contain zero, one or several Bases.
    /// Like Areas and ChokePoints, the number and the addresses of Base instances remain unchanged.
    ///
    /// Bases inherit utils::UserData, which provides free-to-use data.
    /// </summary>
    public class Base
    {
        private readonly Area _area;
        private TilePosition _location;
        private Position _center;
        private readonly List<Mineral> _minerals;
        private readonly List<Geyser> _geysers;
        private readonly List<Mineral> _blockingMinerals;
        private bool _starting;

        internal Base(Area area, TilePosition location, List<Ressource> assignedRessources, List<Mineral> blockingMinerals)
        {
            Debug.Assert(assignedRessources.Count > 0);

            _area = area;
            _location = location;
            _center = location.ToPosition() + UnitType.Terran_Command_Center.TileSize().ToPosition() / 2;

            _minerals = new List<Mineral>();
            _geysers = new List<Geyser>();
            _blockingMinerals = blockingMinerals ?? new List<Mineral>();

            foreach (var r in assignedRessources)
            {
                switch (r)
                {
                    case Mineral mineral:
                        _minerals.Add(mineral);
                        break;
                    case Geyser geyser:
                        _geysers.Add(geyser);
                        break;
                }
            }
        }

        internal void SetStartingLocation(TilePosition actualLocation)
        {
            _starting = true;
            _location = actualLocation;
            _center = actualLocation.ToPosition() + UnitType.Terran_Command_Center.TileSize().ToPosition() / 2;
        }

        internal void OnMineralDestroyed(Mineral mineral)
        {
            Debug.Assert(mineral != null);

            _minerals.FastRemove(mineral);
            _blockingMinerals.FastRemove(mineral);
        }

        /// <summary>
        /// Tells whether this Base's location is contained in Map::StartingLocations()
        /// Note: all players start at locations taken from Map::StartingLocations(),
        ///       which doesn't mean all the locations in Map::StartingLocations() are actually used.
        /// </summary>
        public bool Starting
        {
            get => _starting;
        }

        /// <summary>
        /// Returns the Area this Base belongs to.
        /// </summary>
        public Area Area
        {
            get => _area;
        }

        /// <summary>
        /// Returns the location of this Base (top left Tile position).
        /// If Starting() == true, it is guaranteed that the loction corresponds exactly to one of Map::StartingLocations().
        /// </summary>
        public TilePosition Location
        {
            get => _location;
        }

        /// <summary>
        /// Returns the location of this Base (center in pixels).
        /// </summary>
        public Position Center
        {
            get => _center;
        }

        /// <summary>
        /// Returns the available Minerals.
        /// These Minerals are assigned to this Base (it is guaranteed that no other Base provides them).
        /// Note: The size of the returned list may decrease, as some of the Minerals may get destroyed.
        /// </summary>
        public List<Mineral> Minerals
        {
            get => _minerals;
        }

        /// <summary>
        /// Returns the available Geysers.
        /// These Geysers are assigned to this Base (it is guaranteed that no other Base provides them).
        /// Note: The size of the returned list may NOT decrease, as Geysers never get destroyed.
        /// </summary>
        public List<Geyser> Geysers
        {
            get => _geysers;
        }

        /// <summary>
        /// Returns the blocking Minerals.
        /// These Minerals are special ones: they are placed at the exact location of this Base (or very close),
        /// thus blocking the building of a Command Center, Nexus, or Hatchery.
        /// So before trying to build this Base, one have to finish gathering these Minerals first.
        /// Fortunately, these are guaranteed to have their InitialAmount() <= 8.
        /// As an example of blocking Minerals, see the two islands in Andromeda.scx.
        /// Note: if Starting() == true, an empty list is returned.
        /// Note Base::BlockingMinerals() should not be confused with ChokePoint::BlockingNeutral() and Neutral::Blocking():
        ///      the last two refer to a Neutral blocking a ChokePoint, not a Base.
        /// </summary>
        public List<Mineral> BlockingMinerals
        {
            get => _blockingMinerals;
        }
    }
}