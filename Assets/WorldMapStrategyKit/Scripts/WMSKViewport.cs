// World Strategy Kit for Unity - Main Script
// Copyright (C) Kronnect Games
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

	public partial class WMSK : MonoBehaviour
	{

		#region Public properties

		[SerializeField]
		float _earthElevation = 1.0f;

		/// <summary>
		/// Ground elevation when viewport is used.
		/// </summary>
		/// <value>The earth elevation.</value>
		public float earthElevation {
			get {
				return _earthElevation;
			}
			set {
				if (value != _earthElevation) {
					_earthElevation = value;
					isDirty = true;
					EarthBuildMesh ();
				}
			}
		}


		[SerializeField]
		bool _earthCloudLayer = false;

		/// <summary>
		/// Enables/disables the cloud layer when viewport is used.
		/// </summary>
		public bool earthCloudLayer {
			get {
				return _earthCloudLayer;
			}
			set {
				if (value !=_earthCloudLayer) {
					_earthCloudLayer = value;
					isDirty = true;
					UpdateCloudLayer ();
				}
			}
		}

		[SerializeField]
		float _earthCloudLayerSpeed = 1.2f;

		/// <summary>
		/// Speed of the cloud animation of cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerSpeed {
			get {
				return _earthCloudLayerSpeed;
			}
			set {
				if (value !=_earthCloudLayerSpeed) {
					_earthCloudLayerSpeed = value;
					isDirty = true;
					UpdateCloudLayer ();
				}
			}
		}

		
		[SerializeField]
		float _earthCloudLayerElevation = -5.0f;

		/// <summary>
		/// Elevation of cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerElevation {
			get {
				return _earthCloudLayerElevation;
			}
			set {
				if (value !=_earthCloudLayerElevation) {
					_earthCloudLayerElevation = value;
					isDirty = true;
					UpdateCloudLayer ();
				}
			}
		}


		
		[SerializeField]
		float _earthCloudLayerAlpha = 0.3f;

		/// <summary>
		/// Global alpha for the optional cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerAlpha {
			get {
				return _earthCloudLayerAlpha;
			}
			set {
				if (value !=_earthCloudLayerAlpha) {
					_earthCloudLayerAlpha = value;
					isDirty = true;
					UpdateCloudLayer ();
				}
			}
		}

		[SerializeField]
		float _earthCloudLayerShadowStrength = 0.35f;

		/// <summary>
		/// Global alpha for the optional cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerShadowStrength {
			get {
				return _earthCloudLayerShadowStrength;
			}
			set {
				if (value !=_earthCloudLayerShadowStrength) {
					_earthCloudLayerShadowStrength = value;
					isDirty = true;
					UpdateCloudLayer ();
				}
			}
		}


		[SerializeField]
		float _renderViewportGOAutoScaleMultiplier = 25;

		/// <summary>
		/// Global scale multiplier for game objects put on top of the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMultiplier {
			get {
				return _renderViewportGOAutoScaleMultiplier;
			}
			set {
				if (value != _renderViewportGOAutoScaleMultiplier) {
					_renderViewportGOAutoScaleMultiplier = value;
					isDirty = true;
					UpdateViewportObjects();
				}
			}
		}

		
		[SerializeField]
		float _renderViewportGOAutoScaleMin = 1f;
		
		/// <summary>
		/// Minimum scale applied to game objects on the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMin {
			get {
				return _renderViewportGOAutoScaleMin;
			}
			set {
				if (value != _renderViewportGOAutoScaleMin) {
					_renderViewportGOAutoScaleMin = value;
					isDirty = true;
					UpdateViewportObjects();
				}
			}
		}

		[SerializeField]
		float _renderViewportGOAutoScaleMax = 1f;
		
		/// <summary>
		/// Maximum scale applied to game objects on the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMax {
			get {
				return _renderViewportGOAutoScaleMax;
			}
			set {
				if (value != _renderViewportGOAutoScaleMax) {
					_renderViewportGOAutoScaleMax = value;
					isDirty = true;
					UpdateViewportObjects();
				}
			}
		}

		[SerializeField]
		GameObject _sun;
		public GameObject sun {
			get { return _sun; }
			set { if (value!=_sun) { _sun = value; UpdateSun(); } }
		}

		[SerializeField]
		float _timeOfDay;
		/// <summary>
		/// Simulated time of day (0-24). This would move the light gameobject orientation referenced by sun property around the map.
		/// </summary>
		public float timeOfDay {
			get {
				return _timeOfDay;
			}
			set {
				if (value != _timeOfDay) {
					_timeOfDay = value;
					isDirty = true;
					UpdateSun();
				}
			}
		}

	#endregion

		#region Viewport APIs

		public bool renderViewportIsEnabled {
			get {
				return _renderViewport != null && _renderViewport != gameObject;
			}
		}

		/// <summary>
		/// Computes the interpolated, perspective adjusted or not, height on given position.
		/// </summary>
		public float ComputeEarthHeight(Vector2 position, bool perspectiveAjusted) {
			
			if (position.x<-0.5f || position.x>0.5f || position.y<-0.5f || position.y>0.5f) return 0;
			
			position.x += 0.5f;
			position.y += 0.5f;
			
			int x0 = Mathf.FloorToInt(position.x * EARTH_ELEVATION_WIDTH);
			int y0 = Mathf.FloorToInt(position.y * EARTH_ELEVATION_HEIGHT);
			int x1 = x0 + 1;
			if (x1>= EARTH_ELEVATION_WIDTH-1) x1 = EARTH_ELEVATION_WIDTH-1;
			int y1 = y0 + 1;
			if (y1>= EARTH_ELEVATION_HEIGHT-1) y1 = EARTH_ELEVATION_HEIGHT-1;
			
			int pos00 = (int)(y0 * EARTH_ELEVATION_WIDTH + x0);
			int pos10 = (int)(y0 * EARTH_ELEVATION_WIDTH + x1);
			int pos01 = (int)(y1 * EARTH_ELEVATION_WIDTH + x0);
			int pos11 = (int)(y1 * EARTH_ELEVATION_WIDTH + x1);
			float elev00 = viewportElevationPoints[pos00];
			float elev10 = viewportElevationPoints[pos10];
			float elev01 = viewportElevationPoints[pos01];
			float elev11 = viewportElevationPoints[pos11];
			if (perspectiveAjusted) {
				elev00 *= renderViewportElevationFactor;
				elev10 *= renderViewportElevationFactor;
				elev01 *= renderViewportElevationFactor;
				elev11 *= renderViewportElevationFactor;
			}
			
			float cellWidth = 1.0f / EARTH_ELEVATION_WIDTH;
			float cellHeight = 1.0f / EARTH_ELEVATION_HEIGHT;
			float cellx = (position.x - (float)x0 * cellWidth) / cellWidth;
			float celly = (position.y - (float)y0 * cellHeight) / cellHeight;
			
			float elev = elev00 * (1.0f - cellx) * (1.0f - celly) + 
						 elev10 * cellx * (1.0f - celly) +
						 elev01 * (1.0f - cellx) * celly + 
						 elev11 * cellx * celly;

			return elev;
		}

		/// <summary>
		/// Returns the surface normal of the renderViewport at the position in World coordinates.
		/// </summary>
		public bool RenderViewportGetNormal(Vector3 worldPosition, out Vector3 normal) {
			Ray ray = new Ray(worldPosition - _renderViewport.transform.forward * 50.0f, _renderViewport.transform.forward);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 100.0f, layerMask)) {
				normal = hit.normal;
				return true;
			}
			normal = Misc.Vector3zero;
			return false;
		}

		#endregion

	}

}