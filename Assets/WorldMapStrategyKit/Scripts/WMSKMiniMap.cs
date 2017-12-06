using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit {
	public class WMSKMiniMap : MonoBehaviour {

		const string MINIMAP_NAME = "WMSK_Minimap";

		Vector4 _normalizedScreenRect;
		public Vector4 normalizedScreenRect {
			get { return _normalizedScreenRect; }
			set { if (value!=_normalizedScreenRect) { _normalizedScreenRect = value; RepositionMiniMap(); } }
		}

		/// <summary>
		/// This is a reference to the main map.
		/// </summary>
		public WMSK primaryMap;

		[Range(0.01f, 1f)]
		public float zoomLevel = 0.1f;

		[Range(0f, 8f)]
		public float duration = 2f;

		/// <summary>
		/// Reference to the minimap. Useful for customizing its appearance.
		/// </summary>
		public WMSK map;

		static GameObject _instance;

		static GameObject instance {
			get {
				if (_instance==null) _instance = GameObject.Find (MINIMAP_NAME);
				return _instance;
			}
		}

		/// <summary>
		///	Opens the mini map at the provided normalized screen rect.
		/// </summary>
		/// <param name="screenRect">Screen rectangle in normalized coordinates (0..1)</param>
		public static WMSKMiniMap Show (Vector4 screenRect) {
			GameObject minimapObj = Instantiate (Resources.Load<GameObject> ("WMSK/Prefabs/WorldMapStrategyKit"));
			minimapObj.name = MINIMAP_NAME;
			minimapObj.transform.SetParent(Camera.main.transform, false);

			WMSKMiniMap mm = minimapObj.AddComponent<WMSKMiniMap> ();
			mm.primaryMap = WMSK.instance;
			mm.normalizedScreenRect = screenRect;
			return mm;
		}

		/// <summary>
		/// Hides minimap
		/// </summary>
		public static void Hide() {
			if (instance!=null) GameObject.Destroy(instance);
		}

		/// <summary>
		/// Returns true if minimap is visible
		/// </summary>
		public static bool IsVisible() {
			return instance!=null;
		}

		/// <summary>
		/// Changes position and size of the minimap.
		/// </summary>
		/// <param name="normalizedScreenRect">Normalized screen rect.</param>
		public static void RepositionAt(Vector4 normalizedScreenRect) {
			if (instance!=null) _instance.GetComponent<WMSKMiniMap>().normalizedScreenRect = normalizedScreenRect;
		}


		void Awake() {
			map = GetComponent<WMSK>();
			map.showCountryNames = false;
			map.showCities = false;
			map.showProvinces = false;
			map.showFrontiers = false;
			map.showLatitudeLines = false;
			map.showOutline = false;
			map.showLongitudeLines = false;
			map.frontiersDetail = FRONTIERS_DETAIL.Low;
			map.earthStyle = EARTH_STYLE.Alternate1;
			map.allowUserDrag = false;
			map.allowUserKeys = false;
			map.allowUserZoom = false;
			map.enableCountryHighlight = false;
			map.zoomMaxDistance = 0;
			map.zoomMinDistance = 0;
			map.cursorColor = new Color(0.6f, 0.8f, 1f, 1f);
			map.cursorAlwaysVisible = false;
			map.respectOtherUI = false;
			map.OnClick += (x,y) => primaryMap.FlyToLocation(new Vector2(x,y), duration, zoomLevel);
		}

		void RepositionMiniMap() {
			float z = Camera.main.nearClipPlane + 0.01f;
			Vector3 pbl = Camera.main.ViewportToWorldPoint(new Vector3(normalizedScreenRect.x, normalizedScreenRect.y, z));
			Vector3 ptr = Camera.main.ViewportToWorldPoint(new Vector3(normalizedScreenRect.x + normalizedScreenRect.z, normalizedScreenRect.y + normalizedScreenRect.w, z)); 
			transform.rotation = Camera.main.transform.rotation;
			transform.position = (pbl + ptr) * 0.5f;
			transform.localScale = new Vector3(ptr.x - pbl.x, (ptr.y - pbl.y) * 2.0f, 1f);
		}
	}

}