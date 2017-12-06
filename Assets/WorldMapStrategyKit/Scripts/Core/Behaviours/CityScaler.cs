using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {
	/// <summary>
	/// City scaler. Checks the city icons' size is always appropiate
	/// </summary>
	public class CityScaler : MonoBehaviour {

		const float CITY_SIZE_ON_SCREEN = 10.0f;
		Vector3 lastCamPos, lastPos;
		float lastIconSize;
		float lastCustomSize;
		float lastOrtographicSize;
		[NonSerialized]
		public WMSK
			map;

		void Start () {
			ScaleCities ();
		}
	
		// Update is called once per frame
		void Update () {
			if (lastPos != transform.position || lastCamPos != map.currentCamera.transform.position || lastIconSize != map.cityIconSize || 
				map.currentCamera.orthographic && map.currentCamera.orthographicSize != lastOrtographicSize) {
				ScaleCities ();
			}
		}

		public void ScaleCities () {
			if (map==null || map.currentCamera==null || map.currentCamera.pixelWidth == 0)
				return; // Camera pending setup

			// annotate current values
			lastPos = transform.position;
			lastCamPos = map.currentCamera.transform.position;
			lastIconSize = map.cityIconSize;
			lastOrtographicSize = map.currentCamera.orthographicSize;

			// get mouse position relative to map in world coordinates
			Vector3 worldPos;
			map.GetLocalHitFromScreenPos (Input.mousePosition, out worldPos);
			worldPos = transform.TransformPoint (worldPos);

			// calculates optimal city icon size - deals with WorldToScreenPoint lack of precision
			Vector3 a = map.currentCamera.WorldToScreenPoint (transform.position);
			Vector3 b = new Vector3 (a.x, a.y + CITY_SIZE_ON_SCREEN, a.z);
			Vector3 c = new Vector3 (a.x - CITY_SIZE_ON_SCREEN, a.y, a.z);
			Vector3 aa = map.currentCamera.ScreenToWorldPoint (a);
			Vector3 bb = map.currentCamera.ScreenToWorldPoint (b);
			Vector3 cc = map.currentCamera.ScreenToWorldPoint (c);

			float dist = ((aa - bb).magnitude + (aa - cc).magnitude) * 0.5f;
			float scale = dist * map.cityIconSize;
			if (map.currentCamera.orthographic) {
				scale /= 1.0f + (map.currentCamera.orthographicSize * map.currentCamera.orthographicSize) * (0.1f / map.transform.localScale.x);
			} else {
				scale /= 1.0f + (lastCamPos - worldPos).sqrMagnitude * (0.1f / map.transform.localScale.x);
			}
			Vector3 newScale = new Vector3 (scale / WMSK.mapWidth, scale / WMSK.mapHeight, 1.0f);

			// check if scale has changed
			Transform t1 = transform.Find("Normal Cities");
			if (t1!=null) t1 = t1.GetChild(0);
			if (t1!=null) {
				if (t1.localScale == newScale) return;
			}

			// apply scale to all cities children
			foreach (Transform t in transform.Find("Normal Cities"))
				t.localScale = newScale;
			foreach (Transform t in transform.Find("Region Capitals"))
				t.localScale = newScale * 1.75f;
			foreach (Transform t in transform.Find("Country Capitals"))
				t.localScale = newScale * 2.0f;		
		}

		public void ScaleCities (float customSize) {
			if (customSize == lastCustomSize)
				return;
			lastCustomSize = customSize;
			Vector3 newScale = new Vector3 (customSize / WMSK.mapWidth, customSize / WMSK.mapHeight, 1);
			foreach (Transform t in transform.Find("Normal Cities"))
				t.localScale = newScale;
			foreach (Transform t in transform.Find("Region Capitals"))
				t.localScale = newScale * 1.75f;
			foreach (Transform t in transform.Find("Country Capitals"))
				t.localScale = newScale * 2.0f;
		}
	}

}