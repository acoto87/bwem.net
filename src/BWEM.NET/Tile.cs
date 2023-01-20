using System.Diagnostics;

namespace BWEM.NET
{
    public interface ITile
    {
        /// <summary>
        /// The <see cref="Area"/> associated with the tile.
        /// </summary>
        AreaId AreaId { get; }
    }

    /// <summary>
    /// Corresponds to BWAPI/Starcraft's concept of minitile (8x8 pixels).
    /// MiniTiles are accessed using WalkPositions (Cf. Map::GetMiniTile).
    /// A Map holds Map::WalkSize().x * Map::WalkSize().y MiniTiles as its "MiniTile map".
    /// A MiniTile contains essentialy 3 informations:
    ///	- its Walkability
    ///	- its altitude (distance from the nearest non walkable MiniTile, except those which are part of small enough zones (lakes))
    ///	- the id of the Area it is part of, if ever.
    /// The whole process of analysis of a Map relies on the walkability information
    /// from which are derived successively : altitudes, Areas, ChokePoints.
    /// </summary>
    public class MiniTile : ITile
    {
        private static readonly AreaId _blockingCP = short.MinValue;

        //    0 for seas;
        // != 0 for terrain and lakes (-1 = not computed yet);
        // 1 = SeaOrLake intermediate value
        private Altitude _altitude = -1;

        //   0 -> unwalkable;
        // > 0 -> index of some Area;
        // < 0 -> some walkable terrain, but too small to be part of an Area
        private AreaId _areaId = -1;

        // Corresponds approximatively to BWAPI::isWalkable
        // The differences are:
        //  - For each BWAPI's unwalkable MiniTile, we also mark its 8 neighbours as not walkable.
        //    According to some tests, this prevents from wrongly pretending one small unit can go by some thin path.
        //  - The relation buildable ==> walkable is enforced, by marking as walkable any MiniTile part of a buildable Tile (Cf. Tile::Buildable)
        // Among the MiniTiles having Altitude() > 0, the walkable ones are considered Terrain-MiniTiles, and the other ones Lake-MiniTiles.
        public bool Walkable
        {
            get => _areaId != 0;
            internal set
            {
                _areaId = value ? -1 : 0;
                _altitude = value ? -1 : 1;
            }
        }

        // Distance in pixels between the center of this MiniTile and the center of the nearest Sea-MiniTile
        // Sea-MiniTiles all have their Altitude() equal to 0.
        // MiniTiles having Altitude() > 0 are not Sea-MiniTiles. They can be either Terrain-MiniTiles or Lake-MiniTiles.
        public Altitude Altitude
        {
            get => _altitude;
            internal set
            {
                Debug.Assert(AltitudeMissing && (value > 0));
                _altitude = value;
            }
        }

        // Sea-MiniTiles are unwalkable MiniTiles that have their Altitude() equal to 0.
        public bool Sea
        {
            get => _altitude == 0;
        }

        // Lake-MiniTiles are unwalkable MiniTiles that have their Altitude() > 0.
        // They form small zones (inside Terrain-zones) that can be eaysily walked around (e.g. Starcraft's doodads)
        // The intent is to preserve the continuity of altitudes inside Areas.
        public bool Lake
        {
            get => (_altitude != 0) && !Walkable;
        }

        // Terrain MiniTiles are just walkable MiniTiles
        public bool Terrain
        {
            get => Walkable;
        }

        // For Sea and Lake MiniTiles, returns 0
        // For Terrain MiniTiles, returns a non zero id:
        //    - if (id > 0), id uniquely identifies the Area A that contains this MiniTile.
        //      Moreover we have: A.Id() == id and Map::GetArea(id) == A
        //      For more information about positive Area::ids, see Area::Id()
        //    - if (id < 0), then this MiniTile is part of a Terrain-zone that was considered too small to create an Area for it.
        //      Note: negative Area::ids start from -2
        // Note: because of the lakes, Map::GetNearestArea should be prefered over Map::GetArea.
        public AreaId AreaId
        {
            get => _areaId;
            internal set
            {
                Debug.Assert(AreaIdMissing && (value >= 1));
                _areaId = value;
            }
        }

        internal bool SeaOrLake
        {
            get =>  _altitude == 1;
        }

        internal bool AltitudeMissing
        {
            get => _altitude == -1;
        }

        internal bool AreaIdMissing
        {
            get => _areaId == -1;
        }

        internal bool Blocked
        {
            get => _areaId == _blockingCP;
        }

        internal void SetSea()
        {
            Debug.Assert(!Walkable && SeaOrLake);
            _altitude = 0;
        }

        internal void SetLake()
        {
            Debug.Assert(!Walkable && Sea);
            _altitude = -1;
        }

        internal void SetBlocked()
        {
            Debug.Assert(AreaIdMissing);
            _areaId = _blockingCP;
        }

        internal void ReplaceAreaId(AreaId id)
        {
            Debug.Assert((_areaId > 0) && ((id >= 1) || (id <= -2)) && (id != _areaId));
            _areaId = id;
        }

        internal void ReplaceBlockedAreaId(AreaId id)
        {
            Debug.Assert((_areaId == _blockingCP) && (id >= 1));
            _areaId = id;
        }
    }

    /// <summary>
    /// Corresponds to BWAPI/Starcraft's concept of tile (32x32 pixels).
    /// Tiles are accessed using TilePositions (Cf. Map::GetTile).
    /// A Map holds Map::Size().x * Map::Size().y Tiles as its "Tile map".
    ///
    /// It should be noted that a Tile exactly overlaps 4 x 4 MiniTiles.
    /// As there are 16 times as many MiniTiles as Tiles, we allow a Tiles to contain more data than MiniTiles.
    /// As a consequence, Tiles should be preferred over MiniTiles, for efficiency.
    /// The use of Tiles is further facilitated by some functions like Tile::AreaId or Tile::MinAltitude
    /// which somewhat aggregate the MiniTile's corresponding information
    ///
    /// Tiles inherit utils::Markable, which provides marking ability
    /// Tiles inherit utils::UserData, which provides free-to-use data.
    /// </summary>
    public class Tile : ITile
    {
        private Neutral _neutral;
        private Altitude _minAltitude;
        private AreaId _areaId;
        private GroundHeight_ _groundHeight;
        private bool _buildable;
        private bool _doodad;

        // Corresponds to BWAPI::isBuildable
	    // Note: BWEM enforces the relation buildable ==> walkable (Cf. MiniTile::Walkable)
	    public bool Buildable
        {
            get => _buildable;
        }

        /// <summary>
        /// Tile::AreaId() somewhat aggregates the MiniTile::AreaId() values of the 4 x 4 sub-MiniTiles.
        /// Let S be the set of MiniTile::AreaId() values for each walkable MiniTile in this Tile.
        /// If empty(S), returns 0. Note: in this case, no contained MiniTile is walkable, so all of them have their AreaId() == 0.
        /// If S = {a}, returns a (whether positive or negative).
        /// If size(S) > 1 returns -1 (note that -1 is never returned by MiniTile::AreaId()).
        /// </summary>
        public AreaId AreaId
        {
            get => _areaId;
            internal set
            {
                Debug.Assert((value == -1) || _areaId == 0 && value != 0);
                _areaId = value;
            }
        }

        /// <summary>
        /// Tile::MinAltitude() somewhat aggregates the MiniTile::Altitude() values of the 4 x 4 sub-MiniTiles.
        /// Returns the minimum value.
        /// </summary>
        public Altitude MinAltitude
        {
            get => _minAltitude;
            internal set
            {
                Debug.Assert(value >= 0);
                _minAltitude = value;
            }
        }

        /// <summary>
        /// Tells if at least one of the sub-MiniTiles is Walkable.
        /// </summary>
        public bool Walkable
        {
            get => _areaId != 0;
        }

        /// <summary>
        /// Tells if at least one of the sub-MiniTiles is a Terrain-MiniTile.
        /// </summary>
        public bool Terrain
        {
            get => Walkable;
        }

        /// <summary>
        /// 0: lower ground    1: high ground    2: very high ground
        /// Corresponds to BWAPI::getGroundHeight / 2
        /// </summary>
        public GroundHeight_ GroundHeight
        {
            get => _groundHeight;
            internal set => _groundHeight = value;
        }

        /// <summary>
        /// Tells if this Tile is part of a doodad.
        /// Corresponds to BWAPI::getGroundHeight % 2
        /// </summary>
        public bool Doodad
        {
            get => _doodad;
        }

        // If any Neutral occupies this Tile, returns it (note that all the Tiles it occupies will then return it).
        // Otherwise, returns nullptr.
        // Neutrals are Minerals, Geysers and StaticBuildings (Cf. Neutral).
        // In some maps (e.g. Benzene.scx), several Neutrals are stacked at the same location.
        // In this case, only the "bottom" one is returned, while the other ones can be accessed using Neutral::NextStacked().
        // Because Neutrals never move on the Map, the returned value is guaranteed to remain the same, unless some Neutral
        // is destroyed and BWEM is informed of that by a call of Map::OnMineralDestroyed(BWAPI::Unit u) for exemple. In such a case,
        // BWEM automatically updates the data by deleting the Neutral instance and clearing any reference to it such as the one
        // returned by Tile::GetNeutral(). In case of stacked Neutrals, the next one is then returned.
        public Neutral Neutral
        {
            get => _neutral;
        }

        /// <summary>
        /// Returns the number of Neutrals that occupy this Tile (Cf. GetNeutral).
        /// </summary>
        public int StackedNeutrals()
        {
            var stackSize = 0;

            for (var stacked = Neutral ; stacked != null ; stacked = stacked.NextStacked)
            {
                ++stackSize;
            }

            return stackSize;
        }

        internal void SetBuildable()
        {
            _buildable = true;
        }

        internal void SetDoodad()
        {
            _doodad = true;
        }

        internal void AddNeutral(Neutral neutral)
        {
            Debug.Assert(_neutral == null && neutral != null);
            _neutral = neutral;
        }

        internal void ResetAreaId()
        {
            _areaId = 0;
        }

        internal void RemoveNeutral(Neutral neutral)
        {
            Debug.Assert(neutral != null && (_neutral == neutral));
            _neutral = null;
        }

        /// <summary>
        /// Corresponds to BWAPI::getGroundHeight divided by 2.
        /// </summary>
        public enum GroundHeight_ {
            LOW_GROUND = 0,
            HIGH_GROUND = 1,
            VERY_HIGH_GROUND = 2
        }
    }
}