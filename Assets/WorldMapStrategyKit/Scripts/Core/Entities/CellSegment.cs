using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {

	public class CellSegment {
		public Vector2 start, end;
		public bool isRepeated;	// true if this segment is already used by another hex

		public CellSegment (Vector2 start, Vector2 end) {
			this.start = start;
			this.end = end;
		}

		public override string ToString () {
			return string.Format ("start:" + start.ToString ("F5") + ", end:" + end.ToString ("F5"));
		}

		public CellSegment swapped {
			get { 
				CellSegment n = new CellSegment(end, start); 
				n.isRepeated = true;
				return n;
			} 
		}


	}

}
	