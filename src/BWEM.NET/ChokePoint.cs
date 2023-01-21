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

using System.Collections.Generic;
using System.Diagnostics;
using BWAPI.NET;
using Nito.Collections;

namespace BWEM.NET
{
    /// <summary>
    /// ChokePoints are frontiers that BWEM automatically computes from Brood War's maps
    /// A ChokePoint represents (part of) the frontier between exactly 2 Areas. It has a form of line.
    /// A ChokePoint doesn't contain any MiniTile: All the MiniTiles whose positions are returned by its Geometry()
    /// are just guaranteed to be part of one of the 2 Areas.
    /// Among the MiniTiles of its Geometry, 3 particular ones called nodes can also be accessed using Pos(middle), Pos(end1) and Pos(end2).
    /// ChokePoints play an important role in BWEM:
    ///   - they define accessibility between Areas.
    ///   - the Paths provided by Map::GetPath are made of ChokePoints.
    /// Like Areas and Bases, the number and the addresses of ChokePoint instances remain unchanged.
    ///
    /// Pseudo ChokePoints:
    /// Some Neutrals can be detected as blocking Neutrals (Cf. Neutral::Blocking).
    /// Because only ChokePoints can serve as frontiers between Areas, BWEM automatically creates a ChokePoint
    /// for each blocking Neutral (only one in the case of stacked blocking Neutral).
    /// Such ChokePoints are called pseudo ChokePoints and they behave differently in several ways.
    ///
    /// ChokePoints inherit utils::Markable, which provides marking ability
    /// ChokePoints inherit utils::UserData, which provides free-to-use data.
    /// </summary>
    public class ChokePoint
    {
        private readonly Graph _graph;
        private readonly bool _pseudo;
        private readonly int _index;
        private readonly Pair<Area, Area> _areas;
        private readonly WalkPosition[] _nodes;
        private readonly Pair<WalkPosition, WalkPosition>[] _nodesInArea;
        private readonly Deque<WalkPosition> _geometry;
        private bool _blocked;
        private Neutral _blockingNeutral;
        private ChokePoint _pathBackTrace;

        internal ChokePoint(Graph graph, int idx, Area area1, Area area2, Deque<WalkPosition> geometry, Neutral blockingNeutral = null)
        {
            Debug.Assert(geometry.Count > 0);

            _nodes = new WalkPosition[(int)Node.node_count];
            _nodesInArea = new Pair<WalkPosition, WalkPosition>[(int)Node.node_count];

            _graph = graph;
            _index = idx;
            _areas = new Pair<Area, Area>(area1, area2);
            _geometry = geometry;
            _blockingNeutral = blockingNeutral;
            _blocked = blockingNeutral != null;
            _pseudo = blockingNeutral != null;

            // Ensures that in the case where several neutrals are stacked, m_pBlockingNeutral points to the bottom one:
	        if (_blockingNeutral != null)
            {
                _blockingNeutral = _graph.Map.GetTile(_blockingNeutral.TopLeft).Neutral;
            }

            _nodes[(int)Node.End1] = geometry[0];
            _nodes[(int)Node.End2] = geometry[geometry.Count - 1];

            var i = geometry.Count / 2;
            while ((i > 0) && (_graph.Map.GetTile(geometry[i-1]).Altitude > _graph.Map.GetTile(geometry[i]).Altitude))
            {
                --i;
            }

            while ((i < (int)geometry.Count-1) && (_graph.Map.GetTile(geometry[i+1]).Altitude > _graph.Map.GetTile(geometry[i]).Altitude))
            {
                ++i;
            }

            _nodes[(int)Node.Middle] = geometry[i];

            for (var n = 0 ; n < (int)Node.node_count ; ++n)
            {
                foreach (var pArea in new[] {area1, area2})
                {
                    var nodeInArea = (pArea == _areas.First) ? _nodesInArea[n].First : _nodesInArea[n].Second;
                    nodeInArea = _graph.Map.BreadthFirstSearch(
                        _nodes[n],
                        (MiniTile miniTile, WalkPosition w) => (miniTile.AreaId == pArea.Id) && _graph.Map.GetTile(new TilePosition(w), CheckMode.NoCheck).Neutral == null, // findCond
                        (MiniTile miniTile, WalkPosition w) => (miniTile.AreaId == pArea.Id) || (Blocked && (miniTile.Blocked || _graph.Map.GetTile(new TilePosition(w), CheckMode.NoCheck).Neutral != null)) // visitCond
                    );
                }
            }
        }

        // Tells whether this ChokePoint is a pseudo ChokePoint, i.e., it was created on top of a blocking Neutral.
	    public bool IsPseudo
        {
            get => _pseudo;
        }

        // Returns the two Areas of this ChokePoint.
	    public Pair<Area, Area> Areas
        {
            get => _areas;
        }

        // Returns the center of this ChokePoint.
        public WalkPosition Center
        {
            get => Pos(Node.Middle);
        }

        // Returns the set of positions that defines the shape of this ChokePoint.
        // Note: none of these MiniTiles actually belongs to this ChokePoint (a ChokePoint doesn't contain any MiniTile).
        //       They are however guaranteed to be part of one of the 2 Areas.
        // Note: the returned set contains Pos(middle), Pos(end1) and Pos(end2).
        // If IsPseudo(), returns {p} where p is the position of a walkable MiniTile near from BlockingNeutral()->Pos().
        public Deque<WalkPosition> Geometry
        {
            get => _geometry;
        }

        // If !IsPseudo(), returns false.
        // Otherwise, returns whether this ChokePoint is considered blocked.
        // Normally, a pseudo ChokePoint either remains blocked, or switches to not blocked when BlockingNeutral()
        // is destroyed and there is no remaining Neutral stacked with it.
        // However, in the case where Map::AutomaticPathUpdate() == false, Blocked() will always return true
        // whatever BlockingNeutral() returns.
        // Cf. Area::AccessibleNeighbours().
        public bool Blocked
        {
            get => _blocked;
        }

        // If !IsPseudo(), returns nullptr.
        // Otherwise, returns a pointer to the blocking Neutral on top of which this pseudo ChokePoint was created,
        // unless this blocking Neutral has been destroyed.
        // In this case, returns a pointer to the next blocking Neutral that was stacked at the same location,
        // or nullptr if no such Neutral exists.
        public Neutral BlockingNeutral
        {
            get => _blockingNeutral;
        }

        public Map Map
        {
            get => _graph.Map;
        }

        // Returns the position of one of the 3 nodes of this ChokePoint (Cf. node definition).
        // Note: the returned value is contained in Geometry()
        public WalkPosition Pos(Node n)
        {
            Debug.Assert(n < Node.node_count);
            return _nodes[(int)n];
        }

        // Pretty much the same as Pos(n), except that the returned MiniTile position is guaranteed to be part of pArea.
        // That is: Map::GetArea(PosInArea(n, pArea)) == pArea.
        public WalkPosition PosInArea(Node n, Area pArea)
        {
            Debug.Assert((pArea == _areas.First) || (pArea == _areas.Second));
	        return (pArea == _areas.First) ? _nodesInArea[(int)n].First : _nodesInArea[(int)n].Second;
        }

        // If AccessibleFrom(cp) == false, returns -1.
	    // Otherwise, returns the ground distance in pixels between Center() and cp->Center().
        // Note: if this == cp, returns 0.
        // Time complexity: O(1)
        // Note: Corresponds to the length in pixels of GetPathTo(cp). So it suffers from the same lack of accuracy.
        //       In particular, the value returned tends to be slightly higher than expected when GetPathTo(cp).size() is high.
        public int DistanceFrom(ChokePoint cp)
        {
            return _graph.Distance(this, cp);
        }

        // Returns whether this ChokePoint is accessible from cp (through a walkable path).
        // Note: the relation is symmetric: this->AccessibleFrom(cp) == cp->AccessibleFrom(this)
        // Note: if this == cp, returns true.
        // Time complexity: O(1)
        public bool AccessibleFrom(ChokePoint cp)
        {
            return DistanceFrom(cp) >= 0;
        }

        // Returns a list of ChokePoints, which is intended to be the shortest walking path from this ChokePoint to cp.
        // The path always starts with this ChokePoint and ends with cp, unless AccessibleFrom(cp) == false.
        // In this case, an empty list is returned.
        // Note: if this == cp, returns [cp].
        // Time complexity: O(1)
        // To get the length of the path returned in pixels, use DistanceFrom(cp).
        // Note: all the possible Paths are precomputed during Map::Initialize().
        //       The best one is then stored for each pair of ChokePoints.
        //       However, only the center of the ChokePoints is considered.
        //       As a consequence, the returned path may not be the shortest one.
        public CPPath GetPathTo(ChokePoint cp)
        {
            return _graph.GetPath(this, cp);
        }

        internal int Index
        {
            get => _index;
        }

        internal ChokePoint PathBackTrace
        {
            get => _pathBackTrace;
            set => _pathBackTrace = value;
        }

        internal void OnBlockingNeutralDestroyed(Neutral pBlocking)
        {
            Debug.Assert(pBlocking != null && pBlocking.Blocking);

            if (_blockingNeutral == pBlocking)
            {
                // Ensures that in the case where several neutrals are stacked, m_pBlockingNeutral points to the bottom one:
                _blockingNeutral = _graph.Map.GetTile(_blockingNeutral.TopLeft).Neutral;

                if (_blockingNeutral == null)
                {
                    if (_graph.Map.AutomaticPathUpdate)
                    {
                        _blocked = false;
                    }
                }
            }
        }

        // ChokePoint::middle denotes the "middle" MiniTile of Geometry(), while
        // ChokePoint::end1 and ChokePoint::end2 denote its "ends".
        // It is guaranteed that, among all the MiniTiles of Geometry(), ChokePoint::middle has the highest altitude value (Cf. MiniTile::Altitude()).
        public enum Node
        {
            End1,
            Middle,
            End2,
            node_count
        };
    }

    public class CPPath : List<ChokePoint>
    {
        public CPPath()
        {
        }

        public CPPath(IEnumerable<ChokePoint> collection)
            : base(collection)
        {
        }

        public CPPath(int capacity)
            : base(capacity)
        {
        }
    }
}