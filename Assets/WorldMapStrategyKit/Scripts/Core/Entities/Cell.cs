using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {

	public class Cell: IFader {

		public int row, column;

		/// <summary>
		/// Center of this cell in local space coordinates (-0.5..0.5)
		/// </summary>
		public Vector2 center;
		
		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		public JSONObject attrib { get; set; }

		List<Vector3> _points;
		/// <summary>
		/// List of vertices of this cell.
		/// </summary>
		public List<Vector3> points {
			get {
				if (_points!=null) return _points;
				_points = new List<Vector3>(6);
				for (int k=0;k<segments.Count;k++) {
					_points.Add (segments[k].start);
				}
				return _points;
			}
		}

		/// <summary>
		/// Segments of this cell. Internal use.
		/// </summary>
		public List<CellSegment> segments;
		public Rect rect2D;

		public Material customMaterial { get; set; }
		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;
		public bool isFading { get; set; }


		public Cell(int row, int column, Vector2 center) {
			this.row = row;
			this.column = column;
			this.center = center;
			segments = new List<CellSegment>(6);
		}

		public bool ContainsPoint (float x, float y) { 
			int numPoints = points.Count;
			int j = numPoints-1; 
			bool inside = false; 
			for (int i = 0; i < numPoints; j = i++) { 
				if (((_points [i].y <= y && y < _points [j].y) || (_points [j].y <= y && y < _points [i].y)) && 
				    (x < (_points [j].x - _points [i].x) * (y - _points [i].y) / (_points [j].y - _points [i].y) + _points [i].x))  
					inside = !inside; 
			} 
			return inside; 
		}



	}
}

