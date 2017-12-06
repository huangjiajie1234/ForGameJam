// World Political Map - Globe Edition for Unity - Main Script
// Copyright (C) Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit {

	public partial class WMSK : MonoBehaviour {

		#region Internal variables

		// resources
		Material fogOfWarMat;
		Dictionary<int, GameObjectAnimator> vgos;	// viewport game objects

		// Overlay & Viewport
		RenderTexture overlayRT;
		Camera _currentCamera;

		// Earth effects
		float earthLastElevation = -1;
		List<Vector3> viewportElevationPointsAdjusted;
		List<Vector2> viewportUV;
		const int EARTH_ELEVATION_WIDTH = 360;
		const int EARTH_ELEVATION_HEIGHT = 180;
		bool viewportColliderNeedsUpdate;
		float[] viewportElevationPoints;
		float renderViewportOffsetX, renderViewportOffsetY, renderViewportScaleX, renderViewportScaleY, renderViewportElevationFactor;
		Vector3 renderViewportClip0, renderViewportClip1;
		float  renderViewportClipWidth, renderViewportClipHeight;
		bool lastRenderViewportGood;
		float _renderViewportScaleFactor;
		Vector3 lastRenderViewportRotation, lastRenderViewportPosition;

		public float renderViewportScaleFactor { get { return _renderViewportScaleFactor; } }


		public Camera currentCamera { get {
				if (_currentCamera==null) SetupViewport();
				return _currentCamera;
			}
		}

		#endregion

		#region Viewport mesh building

		/// <summary>
		/// Build an extruded mesh for the viewport
		/// </summary>
		void EarthBuildMesh() {
			// Real Earth relief is only available when viewport is enabled
			if (_renderViewport==null || _renderViewport == gameObject) return;

			if (earthLastElevation<0) { // first time -- get elevation and build entire mesh
				EarthGetElevationInfo();
			}
			EarthUpdateElevation();	// just update vertices positions
			earthLastElevation = _earthElevation;

			// Updates objects elevation
			UpdateViewportObjects();
		}

		void EarthGetElevationInfo() {
			viewportElevationPoints = new float[EARTH_ELEVATION_WIDTH*EARTH_ELEVATION_HEIGHT];

			// Get elevation info
			Texture2D heightMap = Resources.Load<Texture2D>("WMSK/Textures/EarthHeightMap");
			Color[] colors = heightMap.GetPixels();
			float baseElevation = 24.0f/255.0f;
			for (int j=0;j<EARTH_ELEVATION_HEIGHT;j++) {
				int jj = j * EARTH_ELEVATION_WIDTH;
				int texjj = (j * heightMap.height / EARTH_ELEVATION_HEIGHT) * heightMap.width;
				for (int k=0;k<EARTH_ELEVATION_WIDTH;k++) {
					int pos = texjj + k * heightMap.width / EARTH_ELEVATION_WIDTH;
					float gCol = Mathf.Max (colors[pos].g - baseElevation, 0);
					viewportElevationPoints[jj+k] = gCol;
				}
			}

			// Create and assign a quad mesh
			MeshFilter mf = _renderViewport.GetComponent<MeshFilter> ();
			Mesh mesh = mf.sharedMesh;
			if (mesh==null) {
				mesh = new Mesh();
				mesh.hideFlags = HideFlags.DontSave;
			}
			mesh.Clear();
			mesh.vertices = new Vector3[] { new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, -0.5f), new Vector2(-0.5f, -0.5f) };
			mesh.SetIndices (new int[] {0, 1, 2, 3}, MeshTopology.Quads, 0);
			mesh.uv = new Vector2[] { new Vector2(0,1), new Vector2(1,1), new Vector2(1,0), new Vector2(0,0) };
			mesh.RecalculateBounds ();
			mesh.RecalculateNormals();
			mf.sharedMesh = mesh;
		}

		void EarthUpdateElevation() {

			// Reserve mesh memory
			if (viewportElevationPointsAdjusted==null) {
				viewportElevationPointsAdjusted = new List<Vector3>(65000);
			} else {
				viewportElevationPointsAdjusted.Clear();
			}
			if (viewportUV==null) {
				viewportUV = new List<Vector2>(65000);
			} else {
				viewportUV.Clear();
			}

			// Get window rect
			int rmin=int.MaxValue;
			int rmax=int.MinValue;
			int cmin=int.MaxValue;
			int cmax=int.MinValue;
			for (int j=0;j<EARTH_ELEVATION_HEIGHT;j++) {
				float j0 = renderViewportClip1.y + renderViewportClipHeight * (float)j / EARTH_ELEVATION_HEIGHT;
				float j1 = renderViewportClip1.y + renderViewportClipHeight * (j+1.0f) / EARTH_ELEVATION_HEIGHT;
				if ( (j0>=0f && j0<=1.0f) || (j1>=0f && j1<=1.0f) || (j0<0f && j1>1.0f)) {
					for (int k=0;k<EARTH_ELEVATION_WIDTH;k++) {
						float k0 = renderViewportClip0.x + renderViewportClipWidth * (float)k / EARTH_ELEVATION_WIDTH;
						float k1 = renderViewportClip0.x + renderViewportClipWidth * (k+1.0f) / EARTH_ELEVATION_WIDTH;
						if ( (k0>=0f && k0<=1.0f) || (k1>=0f && k1<=1.0f) || (k0<0f && k1>1.0f)) {
							if (j<rmin) rmin = j;
							if (j>rmax) rmax = j;
							if (k<cmin) cmin = k;
							if (k>cmax) cmax = k;
						}
					}
				}
			}

			rmax ++;
			cmax ++;

			// Compute surface vertices and uv
//			_renderViewportScaleFactor = 1.0f / Mathf.Sqrt (lastDistanceFromCameraSqr+1f);
			_renderViewportScaleFactor = 1.0f / (lastDistanceFromCamera+1f);
			renderViewportElevationFactor = _earthElevation * 100.0f * _renderViewportScaleFactor;
			for (int j=rmin;j<=rmax;j++) {
				float jj0 = renderViewportClip1.y + renderViewportClipHeight * j / EARTH_ELEVATION_HEIGHT;
				float j0 = Mathf.Clamp01(jj0);
				int jj = Mathf.Min ( j,EARTH_ELEVATION_HEIGHT-1) * EARTH_ELEVATION_WIDTH;
				for (int k=cmin;k<=cmax;k++) {
					float kk0 = renderViewportClip0.x + renderViewportClipWidth * k / EARTH_ELEVATION_WIDTH;
					float k0 = Mathf.Clamp01(kk0);
					// add uv mapping
					Vector2 uv = new Vector2(k0, j0);
					viewportUV.Add (uv);
					// add vertex location
					int pos = jj +  Mathf.Min (k, EARTH_ELEVATION_WIDTH-1);
					float elev = viewportElevationPoints[pos];
					// as this pos get clamped at borders, interpolate with previous row or col
					if (j==rmin && rmin<EARTH_ELEVATION_HEIGHT-1) {
						float elev1 = viewportElevationPoints[pos + EARTH_ELEVATION_WIDTH];
						float jj1 = renderViewportClip1.y + renderViewportClipHeight * (j+1.0f) / EARTH_ELEVATION_HEIGHT;
						elev = Mathf.Lerp(elev, elev1, (j0 - jj0) / (jj1-jj0));
					} else if (j==rmax && rmax>0) {
						float elev1 = viewportElevationPoints[pos - EARTH_ELEVATION_WIDTH];
						float jj1 = renderViewportClip1.y + renderViewportClipHeight * (j-1.0f) / EARTH_ELEVATION_HEIGHT;
						elev = Mathf.Lerp(elev, elev1, (jj0 - j0) / (jj0-jj1));
					}
					if (k==cmin && cmin < EARTH_ELEVATION_WIDTH-1) {
						float elev1 = viewportElevationPoints[pos + 1];
						float kk1 = renderViewportClip0.x + renderViewportClipWidth * (k+1.0f) / EARTH_ELEVATION_WIDTH;
						elev = Mathf.Lerp(elev, elev1, (k0 - kk0) / (kk1-kk0));
					} else if (k==cmax && cmax>0) {
						float elev1 = viewportElevationPoints[pos - 1];
						float kk1 = renderViewportClip0.x + renderViewportClipWidth * (k-1.0f) / EARTH_ELEVATION_WIDTH;
						elev = Mathf.Lerp(elev, elev1, (kk0 - k0) / (kk0-kk1));
					}

					Vector3 v = new Vector3( k0 - 0.5f, j0 - 0.5f, -elev * renderViewportElevationFactor);
					viewportElevationPointsAdjusted.Add (v);
				}
			}

			// Set surface geometry
			int h = rmax - rmin;
			int w = cmax - cmin;

			int bindex = 0;
			int[] viewportIndices = new int[w*h*6];
			for (int j=0;j<h;j++) {
				int pos = j * (w+1);
				int posEnd = pos+w;
				while(pos<posEnd) {
					viewportIndices[bindex] = pos + 1;
					viewportIndices[bindex+1] = pos;
					viewportIndices[bindex+2] = pos + w + 2;
					viewportIndices[bindex+3] = pos;
					viewportIndices[bindex+4] = pos + w + 1;
					viewportIndices[bindex+5] = pos + w + 2;
					bindex+=6;
					pos++;
				}
			}

			// Create and assign mesh
			if (viewportElevationPointsAdjusted.Count>0) {
				MeshFilter mf = _renderViewport.GetComponent<MeshFilter> ();
				Mesh mesh = mf.sharedMesh;
				mesh.Clear();
			    mesh.vertices = viewportElevationPointsAdjusted.ToArray();
				mesh.uv = viewportUV.ToArray();
				mesh.SetIndices (viewportIndices, MeshTopology.Triangles, 0);
				mesh.RecalculateBounds ();
				mesh.RecalculateNormals();
				mf.sharedMesh = mesh;


				// Update collider, but only if vertex count is not too high (otherwise, this operation is too expensive)
				viewportColliderNeedsUpdate = viewportIndices.Length < 25000;
			}
		}
	
	#endregion


		#region Render viewport setup

		void DetachViewport() {

			if (vgos!=null) vgos.Clear();

			if (overlayRT != null) {
				overlayRT.Release ();
				DestroyImmediate (overlayRT);
				overlayRT = null;
			}

			_currentCamera = Camera.main;
			if (this.overlayLayer!=null) {
				DestroyMapperCam();
			}
			_renderViewport = gameObject;
		}

		void SetupViewport () {
			
			if (!gameObject.activeInHierarchy) {
				DetachViewport();
				return;
			}
			if (_renderViewport==null) _renderViewport = gameObject;

			if (_renderViewport==gameObject ) {
				DetachViewport();
				return;
			}
			
			// Setup Render texture
			int imageWidth, imageHeight;
//			switch (_renderViewportQuality) {
//			case VIEWPORT_QUALITY.Medium:
//				imageWidth = 4096;
//				imageHeight = 2048;
//				break;
//			case VIEWPORT_QUALITY.High:
//				imageWidth = 8192;
//				imageHeight = 4096;
//				break;
//			default:
				imageWidth = 2048;
				imageHeight = 1024;
//				break;
//			}
			if (overlayRT != null && (overlayRT.width != imageWidth || overlayRT.height != imageHeight)) {
				overlayRT.Release ();
				DestroyImmediate (overlayRT);
				overlayRT = null;
			}
			GameObject overlayLayer = GetOverlayLayer(true);
			if (overlayRT == null) {
				overlayRT = new RenderTexture (imageWidth, imageHeight, 0, RenderTextureFormat.ARGB32);
				overlayRT.hideFlags = HideFlags.DontSave;
				overlayRT.filterMode = FilterMode.Trilinear;
				overlayRT.anisoLevel = 1;
				overlayRT.useMipMap = true;
			}
			
			// Camera
			GameObject camObj = GameObject.Find (MAPPER_CAM);
			if (camObj == null) {
				camObj = new GameObject(MAPPER_CAM, typeof(Camera));
				camObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				camObj.layer = overlayLayer.layer;
			}
			Camera cam = camObj.GetComponent<Camera> ();
			cam.aspect = 2;
			cam.cullingMask = 1 << camObj.layer;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0,0,0,1);
			cam.targetTexture = overlayRT;
			cam.nearClipPlane = 0.01f;
			cam.farClipPlane = 1000;
			cam.targetTexture = overlayRT;
			if (_currentCamera!=cam) {
				_currentCamera = cam;
				transform.position = new Vector3(0,500,0); // moves the main gameobject away: note: can't use 10000,10000,10000 for precision problems
				_currentCamera.transform.position = transform.position + Misc.Vector3back * 86.5f; // default camera position for a standard height of 100
				CenterMap();
			}

			// Assigns render texture to current material and recreates the camera
			_renderViewport.GetComponent<Renderer>().sharedMaterial.mainTexture = overlayRT;
			PointerTrigger pt = _renderViewport.GetComponent<PointerTrigger>() ?? _renderViewport.AddComponent<PointerTrigger>();
			pt.map = this;

			if (vgos == null) vgos = new Dictionary<int, GameObjectAnimator>();

			// Setup 3d surface, cloud and other visual effects
			UpdateViewport();
		}

		void DestroyMapperCam() {
			if (!renderViewportIsEnabled) return;
			GameObject o = GameObject.Find (MAPPER_CAM);
			if (o!=null) DestroyImmediate(o);
		}

		#endregion

		#region Viewport FX

		void UpdateCloudLayer() {
			if (_renderViewport==null || _renderViewport == gameObject) return;

			Transform t = _renderViewport.transform.Find("CloudLayer1");
			if (t==null) {
				Debug.Log ("Cloud layer not found under Viewport gameobject. Remove it and create it again from prefab.");
				return;
			}
			Renderer renderer = t.GetComponent<MeshRenderer>();
			renderer.enabled = _earthCloudLayer;

//			float cameraDistance = Mathf.Sqrt (lastDistanceFromCameraSqr);
//			if (cameraDistance<=0) return;
			if (lastDistanceFromCamera<=0) return;

			// Compute cloud layer position and texture scale and offset
			Vector3 clip0 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(-0.55f,0.55f, 0));
			Vector3 clip1 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0.55f,-0.55f, 0));
			float dx = clip1.x - clip0.x;
			float scaleX = 1.0f / dx;
			float offsetX = -clip0.x / dx;
			float dy = clip0.y - clip1.y;
			float scaleY = 1.0f / dy;
			float offsetY = -clip0.y / dy;

			t.transform.localPosition = new Vector3(0,0,_earthCloudLayerElevation * renderViewportElevationFactor);
			Material cloudMat = renderer.sharedMaterial;
			cloudMat.mainTextureScale = new Vector2(scaleX, scaleY);
//			float brightness = Mathf.Clamp01 ( (cameraDistance + t.transform.localPosition.z - 5f) / 5f);
			float brightness = Mathf.Clamp01 ( (lastDistanceFromCamera + t.transform.localPosition.z - 5f) / 5f);
			renderer.enabled = _earthCloudLayer && brightness>0f;	// optimization: hide cloud layer entirely if it's 100% transparent
			Color cloudBrightness = Color.Lerp(Misc.ColorClear, Misc.ColorWhite, brightness * _earthCloudLayerAlpha);
			cloudMat.SetColor ("_EmissionColor", cloudBrightness);
			earthMat.SetFloat ("_CloudShadowStrength", _earthCloudLayer ?_earthCloudLayerShadowStrength * _earthCloudLayerAlpha : 0f);
			CloudLayerAnimator cla = t.GetComponent<CloudLayerAnimator>();
			cla.earthMat = earthMat;
			cla.cloudMainTextureOffset = new Vector2(offsetX, offsetY);
			cla.speed = _earthCloudLayerSpeed;
			cla.Update();
		}


		void UpdateFogOfWarLayer() {
			if (_renderViewport==null || _renderViewport == gameObject) return;
			
			Transform t = _renderViewport.transform.Find("FogOfWarLayer");
			if (t==null) {
				Debug.Log ("Fog of War layer not found under Viewport gameobject. Remove it and create it again from prefab.");
				return;
			}
			Renderer renderer = t.GetComponent<MeshRenderer>();
			renderer.enabled = _fogOfWarLayer;
			
			if (lastDistanceFromCamera<=0) return;
			
			// Compute fog layer position and texture scale and offset
			float elevationFactor = _earthElevation * 100.0f / lastDistanceFromCamera;
			t.transform.localPosition = new Vector3(0,0,_earthCloudLayerElevation * elevationFactor * 0.99f); // make it behind clouds
			if (fogOfWarMat==null) {
				fogOfWarMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/FogOfWar"));
				fogOfWarMat.hideFlags = HideFlags.DontSave;
			}
			renderer.sharedMaterial = fogOfWarMat;
			fogOfWarMat.mainTextureScale = new Vector2(renderViewportScaleX, renderViewportScaleY);
			fogOfWarMat.mainTextureOffset = new Vector2(renderViewportOffsetX, renderViewportOffsetY);
			fogOfWarMat.SetColor ("_EmissionColor", _fogOfWarColor);
		}


		void UpdateSun() {
			if (_sun==null) return;
			sun.transform.rotation = _renderViewport.transform.rotation;
			sun.transform.Rotate(Vector3.up, 180f + _timeOfDay * 360f / 24f, Space.Self);
		}

		#endregion




		#region internal viewport API
	
		void UpdateViewport() {

			// Calculates viewport rect
			ComputeViewportRect();

			// Generates 3D surface
			EarthBuildMesh();
			
			// Updates cloud layer
			UpdateCloudLayer();
			
			// Update fog layer
			UpdateFogOfWarLayer();
			
		}


		/// <summary>
		/// Updates renderViewportRect field
		/// </summary>
		void ComputeViewportRect() {
			if (lastRenderViewportGood) return;
			// Get clip rect
			renderViewportClip0 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(-0.5f,0.5f, 0));
			renderViewportClip1 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0.5f,-0.5f, 0));
			renderViewportClipWidth = renderViewportClip1.x - renderViewportClip0.x;
			renderViewportClipHeight = renderViewportClip0.y - renderViewportClip1.y;
			
			// Computes and saves current viewport scale, offset and rect
			renderViewportScaleX = 1.0f / renderViewportClipWidth;
			renderViewportOffsetX = -renderViewportClip0.x / renderViewportClipWidth;
			renderViewportScaleY = 1.0f / renderViewportClipHeight;
			renderViewportOffsetY = -renderViewportClip0.y / renderViewportClipHeight;
			_renderViewportRect = new Rect(renderViewportOffsetX - 0.5f, renderViewportOffsetY + 0.5f, renderViewportScaleX, renderViewportScaleY);
			lastRenderViewportGood = true;
		}
		
		void UpdateViewportObjects() {
			// Update animators
			foreach(KeyValuePair<int, GameObjectAnimator> entry in vgos) {
				if (entry.Value!=null) {
					entry.Value.UpdateTransformAndVisibility();
				}
			}
		}


		#endregion
	}

}