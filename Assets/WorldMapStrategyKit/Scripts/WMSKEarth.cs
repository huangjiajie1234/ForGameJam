// World Strategy Kit for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{
	public enum EARTH_STYLE
	{
		Natural = 0,
		Alternate1= 1,
		Alternate2= 2,
		Alternate3= 3,
		SolidColor = 4,
		NaturalHighRes = 5,
		NaturalScenic = 6,
		NaturalScenicPlus = 7,
		NaturalScenicPlusAlternate1 = 8
	}


	public partial class WMSK : MonoBehaviour
	{

		#region Public properties

		[SerializeField]
		bool _showWorld = true;

		/// <summary>
		/// Toggle Earth visibility.
		/// </summary>
		public bool showEarth { 
			get {
				return _showWorld; 
			}
			set {
				if (value != _showWorld) {
					_showWorld = value;
					isDirty = true;
					gameObject.GetComponent<MeshRenderer> ().enabled = _showWorld;
				}
			}
		}

		[SerializeField]
		EARTH_STYLE
			_earthStyle = EARTH_STYLE.Natural;

		/// <summary>
		/// Earth globe style.
		/// </summary>
		public EARTH_STYLE earthStyle {
			get {
				return _earthStyle;
			}
			set {
				if (value != _earthStyle) {
					_earthStyle = value;
					isDirty = true;
					RestyleEarth ();
				}
			}
		}
	

		[SerializeField]
		Color
			_earthColor = Color.black;
		
		/// <summary>
		/// Color for Earth (for SolidColor style)
		/// </summary>
		public Color earthColor {
			get {
				return _earthColor;
			}
			set {
				if (value != _earthColor) {
					_earthColor = value;
					isDirty = true;

					if (_earthStyle == EARTH_STYLE.SolidColor) {
						Material mat = GetComponent<Renderer> ().sharedMaterial;
						mat.color = _earthColor;
					}
				}
			}
		}
	
		[SerializeField]
		Color _waterColor = new Color(0, 106.0f/255.0f, 148.0f/255.0f);

		/// <summary>
		/// Defines the base water color used in Scenic Plus style.
		/// </summary>
		public Color waterColor {
			get {
				return _waterColor;
			}
			set {
				if (value !=_waterColor) {
					_waterColor = value;
					isDirty = true;
					RestyleEarth();
				}
			}
		}

		#endregion


		#region Earth related APIs

		/// <summary>
		/// Returns true if specified position is on water.
		/// </summary>
		public bool ContainsWater(Vector2 position) {
			position.x += 0.5f;
			position.y += 0.5f;
			if (position.x<0 || position.x>=1.0f || position.y<0 || position.y>=1.0f) return false;
			CheckRouteWaterMask();
			int jj = ((int)(position.y * earthWaterMaskHeight)) * earthWaterMaskWidth;
			int kk = (int)(position.x * earthWaterMaskWidth);
			bool hasWater = earthWaterMask[jj + kk];
			return hasWater;
		}
		
		/// <summary>
		/// Returns true if specified area with center at "position" contains water.
		/// </summary>
		/// <param name="boxSize">Box size.</param>
		/// <param name="waterPosition">Exact position where water was found.</param>
		public bool ContainsWater(Vector2 position, float boxSize, out Vector2 waterPosition) {
			
			CheckRouteWaterMask();

			float halfSize = boxSize * 0.5f;
			float stepX = 1.0f/earthWaterMaskWidth;
			float stepY = 1.0f/earthWaterMaskHeight;

			position.x += 0.5f;
			position.y += 0.5f;
			float y0 = position.y - halfSize + 0.5f;
			float y1 = position.y + halfSize + 0.5f;
			float x0 = position.x - halfSize + 0.5f;
			float x1 = position.x + halfSize + 0.5f;
			for (float y = y0; y<= y1; y+=stepY) {
				if (y<0 || y>=1.0f) continue;
				int jj = ((int)(y * earthWaterMaskHeight)) * earthWaterMaskWidth;
				for (float x = x0; x<= x1; x+=stepX) {
					if (x<0 || x>=1.0f) continue;
					int kk = (int)(x * earthWaterMaskWidth);
					if (earthWaterMask[jj + kk]) {
						waterPosition = new Vector2(x - 0.5f, y - 0.5f);
						return true;
					}
				}
			}
			waterPosition = Misc.Vector2zero;
			return false;
		}

		/// <summary>
		/// Returns true if specified area with center at "position" contains water.
		/// </summary>
		/// <param name="boxSize">Box size in cell units.</param>
		bool ContainsWater(Vector2 position, int boxSize, out Vector2 waterPosition) {
			
			CheckRouteWaterMask();
			
			int halfSize = boxSize / 2;

			int yc = (int)((position.y + 0.5f) * earthWaterMaskHeight);
			int xc = (int)((position.x + 0.5f) * earthWaterMaskWidth);
			int y0 = yc - halfSize;
			int y1 = yc + halfSize;
			int x0 = xc - halfSize;
			int x1 = xc + halfSize;
			for (int y = y0; y<= y1; y++) {
				if (y<0 || y>=earthWaterMaskHeight) continue;
				int yy = y * earthWaterMaskWidth;
				for (int x = x0; x<= x1; x++) {
					if (x<0 || x>=earthWaterMaskWidth) continue;
					if (earthWaterMask[yy + x]) {
						waterPosition = new Vector2( (float)x /earthWaterMaskWidth - 0.5f, (float)y/earthWaterMaskHeight - 0.5f);
						return true;
					}
				}
			}
			waterPosition = Misc.Vector2zero;
			return false;
		}



	#endregion

	}

}