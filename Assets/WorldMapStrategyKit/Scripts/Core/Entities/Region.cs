using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class Region: IFader
	{
		public Vector2[] points { get; set; }
		
		public Vector2 center;
		
		/// <summary>
		/// 2D rect in the billboard
		/// </summary>
		public Rect rect2D;
		
		public Material customMaterial { get; set; }
		
		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;
		
		public List<Region>neighbours { get; set; }
		public IAdminEntity entity { get; set; }	// country or province index
		public int regionIndex { get; set; }
		public bool isFading { get; set; }

		/// <summary>
		/// Internal cache for path finding engine.
		/// </summary>
		public List<int> pathFindingPositions;

		public Region(IAdminEntity entity, int regionIndex) {
			this.entity = entity;
			this.regionIndex = regionIndex;
			neighbours = new List<Region>();
		}
		
		public Region Clone() {
			Region c = new Region(entity, regionIndex);
			c.center = this.center;
			c.rect2D = this.rect2D;
			c.customMaterial = this.customMaterial;
			c.customTextureScale = this.customTextureScale;
			c.customTextureOffset = this.customTextureOffset;
			c.customTextureRotation = this.customTextureRotation;
			c.points = new Vector2[points.Length];
			Array.Copy(points, c.points, points.Length);
			return c;
		}

		public bool ContainsPoint (Vector2 p) { 

			if (!rect2D.Contains(p)) return false;

			int numPoints = points.Length;
			int j = numPoints - 1; 
			bool inside = false; 
			for (int i = 0; i < numPoints; j = i++) { 
				if (((points [i].y <= p.y && p.y < points [j].y) || (points [j].y <= p.y && p.y < points [i].y)) && 
				    (p.x < (points [j].x - points [i].x) * (p.y - points [i].y) / (points [j].y - points [i].y) + points [i].x))  
					inside = !inside; 
			} 
			return inside; 
		}

		
	}


}