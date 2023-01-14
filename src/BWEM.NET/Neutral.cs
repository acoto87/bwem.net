using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BWAPI.NET;

namespace BWEM.NET
{
    /// <summary>
    /// Neutral is the base base class for a small hierarchy of wrappers around some BWAPI::Units
    /// The units concerned are the Ressources (Minerals and Geysers) and the static Buildings.
    /// Stacked Neutrals are supported, provided they share the same type at the same location.
    /// </summary>
    public class Neutral : IDisposable
    {
        private readonly Unit _unit;
        private readonly UnitType _unitType;
        private readonly Position _pos;
        private readonly TilePosition _topLeft;
        private readonly TilePosition _size;
        private readonly Map _map;
        private readonly List<WalkPosition> _blockedAreas;

        private Neutral _nextStacked;

        public Neutral(Unit unit, Map map)
        {
            _unit = unit;
            _unitType = unit.GetUnitType();
            _map = map;
            _pos = unit.GetInitialPosition();
            _topLeft = unit.GetInitialTilePosition();
            _size = unit.GetInitialType().TileSize();

            if (unit.GetUnitType() == UnitType.Special_Right_Pit_Door)
            {
                _topLeft = new TilePosition(_topLeft.x + 1, _topLeft.y);
            }

            PutOnTiles();
        }

        public virtual void Dispose()
        {
            RemoveFromTiles();

            if (Blocking)
            {
                _map.OnBlockingNeutralDestroyed(this);
            }
        }

        /// <summary>
        /// Returns the BWAPI::Unit this Neutral is wrapping around.
        /// </summary>
        public Unit Unit
        {
            get => _unit;
        }

        /// <summary>
        /// Returns the BWAPI::UnitType of the BWAPI::Unit this Neutral is wrapping around.
        /// </summary>
        public UnitType UnitType
        {
            get => _unitType;
        }

        /// <summary>
        /// Returns the center of this Neutral, in pixels (same as Unit()->getInitialPosition()).
        /// </summary>
        public Position Pos
        {
            get => _pos;
        }

        /// <summary>
        /// Returns the top left Tile position of this Neutral (same as Unit()->getInitialTilePosition()).
        /// </summary>
        public TilePosition TopLeft
        {
            get => _topLeft;
        }

        /// <summary>
        /// Returns the bottom right Tile position of this Neutral
        /// </summary>
        public TilePosition BottomRight
        {
            get => _topLeft + _size - new TilePosition(-1, -1);
        }

        /// <summary>
        /// Returns the size of this Neutral, in Tiles (same as Type()->tileSize())
        /// </summary>
        public TilePosition Size
        {
            get => _size;
        }

        /// <summary>
        /// Tells whether this Neutral is blocking some ChokePoint.
        /// This applies to Minerals and StaticBuildings only.
        /// For each blocking Neutral, a pseudo ChokePoint (which is Blocked()) is created on top of it,
        /// with the exception of stacked blocking Neutrals for which only one pseudo ChokePoint is created.
        /// Cf. definition of pseudo ChokePoints in class ChokePoint comment.
        /// Cf. ChokePoint::BlockingNeutral and ChokePoint::Blocked.
        /// </summary>
        public bool Blocking
        {
            get => _blockedAreas.Count > 0;
        }

        /// <summary>
        /// If Blocking() == true, returns the set of Areas blocked by this Neutral.
        /// </summary>
        public List<Area> BlockedAreas
        {
            get => _blockedAreas.Select(x => _map.GetArea(x)).ToList();
        }

        /// <summary>
        /// Returns the next Neutral stacked over this Neutral, if ever.
        /// To iterate through the whole stack, one can use the following:
        /// for (const Neutral * n = Map::GetTile(TopLeft()).GetNeutral() ; n ; n = n->NextStacked())
        /// </summary>
        public Neutral NextStacked
        {
            get => _nextStacked;
        }

        /// <summary>
        /// Returns the last Neutral stacked over this Neutral, if ever.
        /// </summary>
        public Neutral LastStacked
        {
            get
            {
                var top = this;
                while (top._nextStacked != null)
                {
                    top = top._nextStacked;
                }
                return top;
            }
        }

        /// <summary>
        /// The map.
        /// </summary>
        public Map Map
        {
            get => _map;
        }

        private void PutOnTiles()
        {
            Debug.Assert(_nextStacked == null);

            for (int dy = 0 ; dy < _size.y ; ++dy)
            {
                for (int dx = 0 ; dx < _size.x ; ++dx)
                {
                    var tile = _map->GetTile_(_topLeft + new TilePosition(dx, dy));
                    if (!tile.GetNeutral())
                    {
                        tile.AddNeutral(this);
                    }
                    else
                    {
                        var lastStacked = tile.Neutral.LastStacked;

                        Debug.Assert(this != tile.GetNeutral());
                        Debug.Assert(this != lastStacked);
                        Debug.Assert(!lastStacked->IsGeyser());
                        Debug.Assert(lastStacked.Type == _unitType, "stacked neutrals have different types: " + lastStacked.Type.Name + " / " + _unitType.GetName());
                        Debug.Assert(lastStacked.TopLeft == TopLeft, "stacked neutrals not aligned: " + lastStacked.TopLeft + " / " + TopLeft);
                        Debug.Assert((dx == 0) && (dy == 0));

                        lastStacked->_nextStacked = this;
                        return;
                    }
                }
            }
        }

	    private void RemoveFromTiles()
        {
            for (int dy = 0 ; dy < _size.y ; ++dy)
            {
                for (int dx = 0 ; dx < _size.x ; ++dx)
                {
                    var tile = _map.GetTile_(_topLeft + new TilePosition(dx, dy));

                    var tileNeutral = tile.GetNeutral();
                    Debug.Assert(tileNeutral != null);

                    if (tileNeutral == this)
                    {
                        tile.RemoveNeutral(this);
                        if (_nextStacked != null)
                        {
                            tile.AddNeutral(_nextStacked);
                        }
                    }
                    else
                    {
                        var _prevStacked = tile.GetNeutral();
                        while (_prevStacked.NextStacked() != this)
                        {
                            _prevStacked = _prevStacked.NextStacked();
                        }

                        Debug.Assert(_prevStacked.UnitType == UnitType);
                        Debug.Assert(_prevStacked.TopLeft == TopLeft);
                        Debug.Assert((dx == 0) && (dy == 0));

                        _prevStacked._nextStacked = _nextStacked;
                        _nextStacked = null;
                        return;
                    }
                }
            }

            _nextStacked = null;
        }
    }

    /// <summary>
    /// A Ressource is either a Mineral or a Geyser
    /// </summary>
    public class Ressource : Neutral
    {
        private readonly int _initialAmount;

        public Ressource(Unit unit, Map map)
            : base(unit, map)
        {
            Debug.Assert(UnitType.IsMineralField() || (UnitType == UnitType.Resource_Vespene_Geyser));

            _initialAmount = unit.GetInitialResources();
        }

        /// <summary>
        /// Returns the initial amount of ressources for this Ressource (same as Unit()->getInitialResources).
        /// </summary>
        public int InitialAmount
        {
            get => _initialAmount;
        }

        /// <summary>
        /// Returns the current amount of ressources for this Ressource (same as Unit()->getResources).
        /// </summary>
        public int Amount
        {
            get => Unit.GetResources();
        }
    }

    /// <summary>
    /// Minerals Correspond to the units in BWAPI::getStaticNeutralUnits() for which getType().isMineralField()
    /// </summary>
    public class Mineral : Ressource
    {
        public Mineral(Unit unit, Map map)
            : base(unit, map)
        {
            Debug.Assert(UnitType.IsMineralField());
        }

        public override void Dispose()
        {
            Map.OnMineralDestroyed(this);
            base.Dispose();
        }
    }

    /// <summary>
    /// Geysers Correspond to the units in BWAPI::getStaticNeutralUnits() for which getType() == Resource_Vespene_Geyser
    /// </summary>
    public class Geyser : Ressource
    {
        public Geyser(Unit unit, Map map)
            : base(unit, map)
        {
            Debug.Assert(UnitType == UnitType.Resource_Vespene_Geyser);
        }
    }

    /// <summary>
    /// StaticBuildings Correspond to the units in BWAPI::getStaticNeutralUnits() for which getType().isSpecialBuilding
    /// StaticBuilding also wrappers some special units like Special_Pit_Door.
    /// </summary>
    public class StaticBuilding : Neutral
    {
        public StaticBuilding(Unit unit, Map map)
            : base(unit, map)
        {
            Debug.Assert(UnitType.IsSpecialBuilding() || (UnitType == UnitType.Special_Pit_Door) || UnitType == UnitType.Special_Right_Pit_Door);
        }
    }
}