using System;

namespace WorldMapStrategyKit.PathFinding {
	public class Point {
		private int _x;
		private int _y;

		public Point (int x, int y)
		{
			this._x = x;
			this._y = y;
		}

		public int X { get { return this._x; } set { this._x = value; } }

		public int Y { get { return this._y; } set { this._y = value; } }

		// For debugging
		public override string ToString () {
			return string.Format ("{0}, {1}", this.X, this.Y);
		}
	}
}
