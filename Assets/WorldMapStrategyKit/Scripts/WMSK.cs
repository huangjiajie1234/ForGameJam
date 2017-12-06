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

	public enum VIEWPORT_QUALITY
	{
		Low = 0,
		Medium =1,
		High = 2
	}

	public enum HEIGHT_OFFSET_MODE {
		ABSOLUTE_ALTITUDE = 0,
		ABSOLUTE_CLAMPED = 1,
		RELATIVE_TO_GROUND = 2
	}

	public partial class WMSK : MonoBehaviour
	{

		#region Public properties

		static WMSK _instance;

		/// <summary>
		/// Instance of the world map. Use this property to access World Map functionality.
		/// </summary>
		public static WMSK instance {
			get {
				if (_instance == null) {
					GameObject obj = GameObject.Find ("WorldMapStrategyKit");
					if (obj == null) {
						Debug.LogWarning ("'WorldMapStrategyKit' GameObject could not be found in the scene. Make sure it's created with this name before using any map functionality.");
					} else {
						_instance = obj.GetComponent<WMSK> ();
					}
				}
				return _instance;
			}
		}

		public static bool instanceExists {
			get {
				if (_instance == null) {
					GameObject obj = GameObject.Find ("WorldMapStrategyKit");
					if (obj == null) {
						return false;
					}
					_instance = obj.GetComponent<WMSK> ();
				}
				return true;
			}
		}
	
		/// <summary>
		/// Target gameobject to display de map (optional)
		/// </summary>
		[SerializeField]
		GameObject
			_renderViewport;

		public GameObject renderViewport {
			get {
				return _renderViewport;
			}
			set {
				if (value != _renderViewport) {
					if (value == null)
						_renderViewport = gameObject;
					else
						_renderViewport = value;
					isDirty = true;
					SetupViewport ();
					CenterMap ();
				}
			}
		}

		Rect _renderViewportRect;
		/// <summary>
		/// Returns the visible rectangle of the map represented by current viewport location and zoom
		/// </summary>
		public Rect renderViewportRect {
			get {
				ComputeViewportRect();
				return _renderViewportRect;
			}
		}

		[SerializeField]
		bool _prewarm = false;

		/// <summary>
		/// Precomputes big country surfaces and path finding matrices during initialization to allow smoother performance during play.
		/// </summary>
		public bool prewarm {
			get { return _prewarm; }
			set { if (_prewarm!=value) { _prewarm = value; isDirty = true; } }
		}

		[SerializeField]
		string _geodataResourcesPath = "WMSK/Geodata";
		
		/// <summary>
		/// Path where geodata files reside. This path is a relative path below Resources folder. So a geodata file would be read as Resources/<geodataResourcesPath>/cities10 for example.
		/// Note that your project can contain several Resources folders. Create your own Resources folder so you don't have to backup your geodata folder on each update if you make any modifications to the files.
		/// </summary>
		public string geodataResourcesPath {
			get { return _geodataResourcesPath; }
			set { if (_geodataResourcesPath!=value) { 
					_geodataResourcesPath = value.Trim();
					if (_geodataResourcesPath.Length<1) {
						_geodataResourcesPath = "WMSK/Geodata";
					}
					string lc = _geodataResourcesPath.Substring(_geodataResourcesPath.Length - 1, 1);
					if (lc.Equals("/") || lc.Equals("\\")) _geodataResourcesPath = _geodataResourcesPath.Substring(0, _geodataResourcesPath.Length-1);
					isDirty = true; 
				}
			}
		}


//		[SerializeField]
//		VIEWPORT_QUALITY _renderViewportQuality = VIEWPORT_QUALITY.Low;
//
//		public VIEWPORT_QUALITY renderViewportQuality {
//			get { return _renderViewportQuality; }
//			set { if (value!=_renderViewportQuality) {
//					_renderViewportQuality = value;
//					isDirty = true;
//					SetupViewport();
//				}
//			}
//		}

	#endregion

	#region Public API area


		/// <summary>
		/// Returns the position in map local coordinates (x, y)
		/// </summary>
		public Vector2 WorldToMap2DPosition(Vector3 position) {
			if (_renderViewport == null || _renderViewport == gameObject) {
				return transform.InverseTransformPoint(position);
			} else {
				return _renderViewport.transform.InverseTransformPoint(position);
			}
		}

		/// <summary>
		/// Returns the world position of the given map coordinate.
		/// This takes into account the viewport and ground elevation is used,
		/// unless you pass -1 to height which will assume absolute 0 height.
		/// </summary>
		public Vector3 Map2DToWorldPosition(Vector2 position, float height) {
			return Map2DToWorldPosition(position, height, HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED, false);
		}

		/// <summary>
		/// Returns the world position of the given map coordinate.
		/// This takes into account the viewport and ground elevation is used,
		/// unless you pass -1 to height which will assume absolute 0 height.
		/// If viewport is enabled, you can use the ignoreViewport param to return the flat 2D Map position.
		/// Use heightOffsetMode to position wisely the height:
		/// - Absolute Altitude will return an absolute height irrespective of altitude at map point (it can cross ground)
		/// - Absolute Clamped will return either the ground altitude or the absolute height (the greater value)
		/// - Relative to the ground will simply add the height to the ground altitude
		/// </summary>
		public Vector3 Map2DToWorldPosition(Vector2 position, float height, HEIGHT_OFFSET_MODE heightOffsetMode, bool ignoreViewport) {
			if (!renderViewportIsEnabled || ignoreViewport) {
				return transform.TransformPoint(position);
			}

			Vector3 worldPos = transform.TransformPoint(position);
			Vector3 viewportPos = _currentCamera.WorldToViewportPoint(worldPos);

			switch(heightOffsetMode) {
			case HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND:
				height += ComputeEarthHeight(position, true);
				break;
			case HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED:
				height *= renderViewportElevationFactor;
				height = Mathf.Max (height, ComputeEarthHeight(position, true));
				break;
			case HEIGHT_OFFSET_MODE.ABSOLUTE_ALTITUDE:
				height *= renderViewportElevationFactor;
				break;
			}
			worldPos = _renderViewport.transform.TransformPoint(new Vector3(viewportPos.x - 0.5f, viewportPos.y - 0.5f, -height));
			return worldPos;
		}

		/// <summary>
		/// Enables Calculator component and returns a reference to its API.
		/// </summary>
		public WMSK_Calculator calc { get { return GetComponent<WMSK_Calculator> () ?? gameObject.AddComponent<WMSK_Calculator> (); } }

		/// <summary>
		/// Enables Ticker component and returns a reference to its API.
		/// </summary>
		public WMSK_Ticker ticker { get { return GetComponent<WMSK_Ticker> () ?? gameObject.AddComponent<WMSK_Ticker> (); } }

		/// <summary>
		/// Enables Decorator component and returns a reference to its API.
		/// </summary>
		public WMSK_Decorator decorator { get { return GetComponent<WMSK_Decorator> () ?? gameObject.AddComponent<WMSK_Decorator> (); } }
		
		/// <summary>
		/// Enables Editor component and returns a reference to its API.
		/// </summary>
		public WorldMapStrategyKit_Editor.WMSK_Editor editor { get { return GetComponent<WorldMapStrategyKit_Editor.WMSK_Editor> () ?? gameObject.AddComponent<WorldMapStrategyKit_Editor.WMSK_Editor> (); } }

		public delegate bool attribPredicate (JSONObject json);
		

		#endregion

	}

}