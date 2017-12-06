using System;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit.PathFinding {
	interface IPathFinder {

		bool Stopped {
			get;
		}

		HeuristicFormula Formula {
			get;
			set;
		}

		bool Diagonals {
			get;
			set;
		}

		bool HeavyDiagonals {
			get;
			set;
		}

		int HeuristicEstimate {
			get;
			set;
		}

		bool PunishChangeDirection {
			get;
			set;
		}

		bool TieBreaker {
			get;
			set;
		}

		int SearchLimit {
			get;
			set;
		}

		float CompletedTime {
			get;
			set;
		}

		void FindPathStop ();

		List<PathFinderNode> FindPath (Point start, Point end);

	}
}
