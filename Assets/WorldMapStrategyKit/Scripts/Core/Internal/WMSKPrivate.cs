// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit {

	public static class EarthStyleExtensions {

		public static bool isScenicPlus(this EARTH_STYLE earthStyle) {
			return earthStyle== EARTH_STYLE.NaturalScenicPlus || earthStyle == EARTH_STYLE.NaturalScenicPlusAlternate1;
		}

	}

	[Serializable]
	[ExecuteInEditMode]
	public partial class WMSK : MonoBehaviour {

		public const float MAP_PRECISION = 5000000f;

		const string OVERLAY_BASE = "OverlayLayer";
		const string OVERLAY_TEXT_ROOT = "TextRoot";
		const string SURFACE_LAYER = "Surfaces";
		const string MAPPER_CAM = "WMSKMapperCam";

		#region Internal variables

		// resources
		Material coloredMat, coloredAlphaMat, texturizedMat;
		Material outlineMat, cursorMatH, cursorMatV, imaginaryLinesMat;
		Material markerMat;
		Material earthMat;

		// gameObjects
		GameObject _surfacesLayer;
		GameObject surfacesLayer { get { if (_surfacesLayer==null) CreateSurfacesLayer(); return _surfacesLayer; } }
		GameObject cursorLayerHLine, cursorLayerVLine, latitudeLayer, longitudeLayer;
		GameObject markersLayer;

		// caché and gameObject lifetime control
		Dictionary<int, GameObject>surfaces;
		Dictionary<Color, Material>coloredMatCache;
		Dictionary<Color, Material>markerMatCache;
		Dictionary<double,Region> frontiersCacheHit;
		List<Vector3> frontiersPoints;

		// FlyTo functionality
		Quaternion flyToStartQuaternion, flyToEndQuaternion;
		Vector3 flyToStartLocation, flyToEndLocation;
		bool flyToActive;
		float flyToStartTime, flyToDuration;

		// UI interaction variables
		Vector3 mouseDragStart, dragDirection, mouseDragStartHitPos;
		int dragDamping;
		float wheelAccel, dragSpeed, maxFrustumDistance, lastDistanceFromCamera, distanceFromCameraStartingFrame;
		bool dragging, hasDragged, lastMouseMapHitPosGood;
		bool mouseIsOverUIElement;
		float lastCamOrtographicSize;
		Vector3 lastMapPosition, lastCamPosition;
		Vector3 lastMouseMapHitPos;

		// Overlay (Labels, tickers, ...)
		GameObject overlayLayer;
		public static float mapWidth { get { return WMSK.instanceExists ? WMSK.instance.transform.localScale.x: 200.0f; } }
		public static float mapHeight { get { return WMSK.instanceExists ? WMSK.instance.transform.localScale.y: 100.0f; } }
		Material labelsShadowMaterial;

		// Earth effects
		RenderTexture earthBlurred;

		int layerMask { get {
				if (renderViewportIsEnabled)
					return 1 << renderViewport.layer;
				else
					return 1 << gameObject.layer;
			}
		}
		

		[NonSerialized]
		public bool
			isDirty; // internal variable used to confirm changes in custom inspector - don't change its value

		#endregion



	#region Game loop events

		void OnEnable () {
#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": enable wpm");
#endif
			if (countries == null) {
				Init ();
			}

			// Check material
			Renderer renderer= GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
			if (renderer.sharedMaterial == null) {
				RestyleEarth();
			}

			if (hudMatCountry != null && hudMatCountry.color != _fillColor) {
				hudMatCountry.color = _fillColor;
			}
			if (frontiersMat != null) {
				frontiersMat.color = _frontiersColor;
				frontiersMat.SetColor("_OuterColor", frontiersColorOuter);
			}
			if (hudMatProvince != null && hudMatProvince.color != _provincesFillColor) {
				hudMatProvince.color = _provincesFillColor;
			}
			if (provincesMat != null && provincesMat.color != _provincesColor) {
				provincesMat.color = _provincesColor;
			}
			if (citiesNormalMat.color != _citiesColor) {
				citiesNormalMat.color = _citiesColor;
			}
			if (citiesRegionCapitalMat.color != _citiesRegionCapitalColor) {
				citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
			}
			if (citiesCountryCapitalMat.color != _citiesCountryCapitalColor) {
				citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
			}
			if (outlineMat.color != _outlineColor) {
				outlineMat.color = _outlineColor;
			}
			if (cursorMatH.color != _cursorColor) {
				cursorMatH.color = _cursorColor;
			}
			if (cursorMatV.color != _cursorColor) {
				cursorMatV.color = _cursorColor;
			}
			if (imaginaryLinesMat.color != _imaginaryLinesColor) {
				imaginaryLinesMat.color = _imaginaryLinesColor;
			}
			if (hudMatCell != null && hudMatCell.color != _cellHighlightColor) {
				hudMatCell.color = _cellHighlightColor;
			}
			if (gridMat != null && gridMat.color != _gridColor) {
				gridMat.color = _gridColor;
			}
			if (_enableCellHighlight) {
				_enableCountryHighlight = _enableProvinceHighlight = false;
				showLatitudeLines = showLongitudeLines = false;
			}

			// Unity 5.3.1 prevents raycasting in the scene view if rigidbody is present - we have to delete it in editor time but re-enable it here during play mode
			if (Application.isPlaying) {

				if (GetComponent<Rigidbody>()==null) {
					Rigidbody rb = gameObject.AddComponent<Rigidbody>();
					rb.useGravity = false;
					rb.isKinematic = true;
				}

				if (_prewarm) {
					CountriesPrewarmBigSurfaces();
					PathFindingPrewarm();
				}
				Redraw ();
			}

			if (_fitWindowWidth || _fitWindowHeight) CenterMap();
		}


		void OnDestroy () {
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": destroy wpm");
			#endif
			if (_surfacesLayer!=null) GameObject.DestroyImmediate(_surfacesLayer);
			overlayLayer = null;
			DestroyMapperCam();
		}

		void Reset () {
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": reset");
#endif
			Redraw ();
		}

		void Update () {
			if (currentCamera == null || !Application.isPlaying) {
				// For some reason, when saving the scene, the renderview port loses the attached rendertexture.
				// No event is fired, except for Update(), so we need to refresh the attached rendertexture of the render viewport here.
				SetupViewport();		
				return;
			}
			
			// Check if navigateTo... has been called and in this case rotate the globe until the country is centered
			if (flyToActive) MoveMapToDestination ();

			// Check whether the points is on an UI element, then avoid user interaction
			bool canInteract = true;
			if (respectOtherUI) {
			if (UnityEngine.EventSystems.EventSystem.current!=null) {
				if (Input.touchSupported && Input.touchCount>0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
					canInteract = false;
				} else if(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1)) 
					canInteract = false;

				if (!canInteract) {
					if (!mouseIsOverUIElement) {
						mouseIsOverUIElement = true;
						HideCountryRegionHighlight ();
					}
					return;
				}
			}
			}
			if (canInteract) {
				mouseIsOverUIElement = false;
				PerformUserInteraction();
			}

			CheckCursorVisibility();

			// Check boundaries
			if (transform.position != lastMapPosition || _currentCamera.transform.position != lastCamPosition || _currentCamera.orthographicSize != lastCamOrtographicSize) {
				// Last distance
				if (_currentCamera.orthographic) {
					_currentCamera.orthographicSize = Mathf.Clamp(_currentCamera.orthographicSize, 1, maxFrustumDistance);
					// updates frontiers LOD
					frontiersMat.shader.maximumLOD = _currentCamera.orthographicSize < 2.2 ? 100: (_currentCamera.orthographicSize < 8 ? 200: 300);
				} else if (_zoomMinDistance>0 || _zoomMaxDistance>0) {
					float minDistance = _zoomMinDistance * transform.localScale.y;
					Plane plane = new Plane(transform.forward, transform.position);
					Ray ray = _currentCamera.ScreenPointToRay (Input.mousePosition);
					float enterPos;
					if (plane.Raycast(ray, out enterPos)) {
						Vector3 dest = ray.GetPoint(enterPos);
						lastDistanceFromCamera = Mathf.Abs (plane.GetDistanceToPoint(_currentCamera.transform.position));
						if (lastDistanceFromCamera < minDistance) {
							transform.position -= (dest - _currentCamera.transform.position).normalized * (lastDistanceFromCamera- minDistance);
							wheelAccel = 0;
						} else {
							float maxDistance = Mathf.Min (maxFrustumDistance, _zoomMaxDistance * transform.localScale.y);
							if (lastDistanceFromCamera > maxDistance) {
								// Get intersection point from camera with plane
								Vector3 planePoint = _currentCamera.transform.position + plane.normal * lastDistanceFromCamera;
								transform.position -= (planePoint - _currentCamera.transform.position).normalized * (lastDistanceFromCamera-maxDistance);
								wheelAccel = 0;
							}
						}
						// updates frontiers LOD
						frontiersMat.shader.maximumLOD = lastDistanceFromCamera < 4.472f ? 100: (lastDistanceFromCamera < 17.888f ? 200 : 300);
					}
				}

				// Constraint to limits if user interaction is enabled
				if (_allowUserDrag || _allowUserZoom) {
					float limitLeft, limitRight;
					if (_fitWindowWidth) {
						limitLeft = 0f;
						limitRight = 1f;
					} else {
						limitLeft = 0.9f;
						limitRight = 0.1f;
					}

					// Reduce floating-point errors
					Vector3 apos = transform.position;
					if (renderViewportIsEnabled) {
						transform.position -= apos;
						_currentCamera.transform.position -= apos;
					}

					Vector3 pos = _currentCamera.WorldToViewportPoint (transform.TransformPoint (0.5f - (1f-_windowRect.xMax - 0.5f), 0, 0));
					if (pos.x < limitRight) {
						pos.x = limitRight;
						transform.position = _currentCamera.ViewportToWorldPoint (pos) - transform.right * (0.5f - (1f-_windowRect.xMax - 0.5f)) * mapWidth;
						dragDamping = 0;
					} else {
						pos = _currentCamera.WorldToViewportPoint (transform.TransformPoint (-0.5f + _windowRect.xMin + 0.5f, 0, 0));
						if (pos.x > limitLeft) {
							pos.x = limitLeft;
							transform.position = _currentCamera.ViewportToWorldPoint (pos) + transform.right * (0.5f - _windowRect.xMin - 0.5f) * mapWidth;
							dragDamping = 0;
						}
					}

					float limitTop, limitBottom;
					if (_fitWindowHeight) {
						limitTop = 1.0f;
						limitBottom = 0f;
					} else {
						limitTop = 0.1f;
						limitBottom = 0.9f;
					}
				
					pos = _currentCamera.WorldToViewportPoint (transform.TransformPoint (0, 0.5f - (1f -_windowRect.yMax - 0.5f), 0));
					if (pos.y < limitTop) {
						pos.y = limitTop;
						transform.position = _currentCamera.ViewportToWorldPoint (pos) - transform.up * (0.5f - (1f -_windowRect.yMax - 0.5f)) * mapHeight;
						dragDamping = 0;
					} else {
						pos = _currentCamera.WorldToViewportPoint (transform.TransformPoint (0, -0.5f + _windowRect.yMin + 0.5f, 0));
						if (pos.y > limitBottom) {
							pos.y = limitBottom;
							transform.position = _currentCamera.ViewportToWorldPoint (pos) + transform.up * (0.5f - _windowRect.yMin - 0.5f) * mapHeight;
							dragDamping = 0;
						}
					}

					// Reduce floating-point errors
					if (renderViewportIsEnabled) {
						transform.position += apos;
						_currentCamera.transform.position += apos;
					}

					hasDragged = true;	// annotate that the map has been moved
				}
				lastMapPosition = transform.position;
				lastCamPosition = _currentCamera.transform.position;
				lastCamOrtographicSize = _currentCamera.orthographicSize;
				lastMouseMapHitPosGood = false;	// forces check again CheckMousePos()
				lastRenderViewportGood = false; // forces calculation of the viewport rect

				// Map has moved: apply changes
				if (distanceFromCameraStartingFrame!=lastDistanceFromCamera) {
					distanceFromCameraStartingFrame = lastDistanceFromCamera;

					// Update distance param in ScenicPlus material
					if (_earthStyle.isScenicPlus()) {
						UpdateScenicPlusDistance();
					}
					// Fades country labels
					if (_showCountryNames) FadeCountryLabels();
				}

				// Update everything related to viewport
				lastRenderViewportGood = false;
				if (renderViewportIsEnabled) UpdateViewport();

				// Update grid
				if (_showGrid) CheckGridRect();

			} else {
				// Map has not moved
				if (viewportColliderNeedsUpdate) {
					Mesh ms = _renderViewport.GetComponent<MeshFilter>().sharedMesh;
					if (ms.vertexCount>6) {
						MeshCollider mc = _renderViewport.GetComponent<MeshCollider>();
						mc.sharedMesh = null;
						mc.sharedMesh = ms;
					}
					viewportColliderNeedsUpdate = false;
				}

				// Check if viewport rotation has changed or has moved
				if (renderViewportIsEnabled) {
					if (_renderViewport.transform.localRotation.eulerAngles != lastRenderViewportRotation || _renderViewport.transform.position != lastRenderViewportPosition) {
						lastRenderViewportRotation = _renderViewport.transform.localRotation.eulerAngles;
						lastRenderViewportPosition = _renderViewport.transform.position;
						UpdateViewportObjects();
					}
				}

			}

			if (_showGrid) {
				GridUpdateHighlightFade(); 	// Fades current selection
			}
		}

		/// <summary>
		/// Check controls (keys, mouse, ...) and react
		/// </summary>
		void PerformUserInteraction() {

			bool buttonLeftPressed = Input.GetMouseButton (0) && (!Input.touchSupported || Input.touchCount == 1);
			
			// Verify if mouse enter a country boundary - we only check if mouse is inside the sphere of world
			if (mouseIsOver) {

				// Check highlighting only if flyTo is not active to prevent hiccups during movement
				if (!flyToActive) {
					CheckMousePos ();
					GridCheckMousePos (); 		// Verify if mouse enter a territory boundary - we only check if mouse is inside the sphere of world
				}

				// Remember the last element clicked
				if (Input.GetMouseButtonUp (0) || Input.GetMouseButtonUp (1)) {
					_countryLastClicked = _countryHighlightedIndex;
					_countryRegionLastClicked = _countryRegionHighlightedIndex;
					if (_countryLastClicked>=0 && OnCountryClick!=null) OnCountryClick(_countryLastClicked, _countryRegionHighlightedIndex);
					_provinceLastClicked = _provinceHighlightedIndex;
					_provinceRegionLastClicked = _provinceRegionHighlightedIndex;
					if (_provinceLastClicked>=0 && OnProvinceClick!=null) OnProvinceClick(_provinceLastClicked, _provinceRegionLastClicked);
					_cityLastClicked = _cityHighlightedIndex;
					if (_cityLastClicked>=0 && OnCityClick!=null) OnCityClick(_cityLastClicked);
					if (OnClick!=null && !hasDragged) OnClick(_cursorLocation.x, _cursorLocation.y);
					_cellLastClickedIndex = _cellHighlightedIndex;
					if (_cellLastClickedIndex>=0 && OnCellClick!=null) OnCellClick(_cellLastClickedIndex);
				}
				
				// if mouse/finger is over map, implement drag and zoom of the world
				if (_allowUserDrag && !flyToActive) {
					// Use left mouse button to drag the map
					if (Input.GetMouseButtonDown (0)) {
						mouseDragStart = Input.mousePosition;
						mouseDragStartHitPos = transform.TransformPoint(lastMouseMapHitPos);
						dragging = true;
						hasDragged = false;
					}
					
					// Use right mouse button and fly and center on target country
					if (Input.GetMouseButtonDown (1) && !Input.touchSupported) {	// two fingers can be interpreted as right mouse button -> prevent this.
						if (_countryHighlightedIndex >= 0 && Input.GetMouseButtonDown (1) && _centerOnRightClick) {
							FlyToCountry (_countryHighlightedIndex, 0.8f);
						}
					}
				}
			}
			
			if (dragging) {
				if (buttonLeftPressed) {
					if (_dragConstantSpeed) {
						if (lastMouseMapHitPosGood && mouseIsOver) {
							Vector3 hitPos = transform.TransformPoint(lastMouseMapHitPos);
							dragDirection = hitPos - mouseDragStartHitPos;
							mouseDragStartHitPos = hitPos;
							transform.Translate(dragDirection, Space.World);
							dragDamping = 0;
						}
					} else {
						dragDirection = (Input.mousePosition - mouseDragStart);
						if (_currentCamera.orthographic) {
							dragSpeed = _currentCamera.orthographicSize * _mouseDragSensitivity * 0.00035f;
						} else {
//							dragSpeed = Mathf.Sqrt (lastDistanceFromCameraSqr) * _mouseDragSensitivity * 0.00035f;
							dragSpeed = lastDistanceFromCamera * _mouseDragSensitivity * 0.00035f;
						}
						dragDamping = 1;
						dragDirection *= dragSpeed;
						transform.Translate (dragDirection);
					}
				} else
					dragging = false;
			}
			
			// Check special keys
			if (_allowUserKeys && _allowUserDrag) {
				bool pressed = false;
				dragDirection = Misc.Vector3zero;
				if (Input.GetKey (KeyCode.W)) {
					dragDirection += Misc.Vector3down;
					pressed = true;
				} 
				if (Input.GetKey (KeyCode.S)) {
					dragDirection += Misc.Vector3up;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.A)) {
					dragDirection += Misc.Vector3right;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.D)) {
					dragDirection += Misc.Vector3left;
					pressed = true;
				}
				if (pressed) {
					if (_currentCamera.orthographic) {
						dragSpeed =  _currentCamera.orthographicSize * 10.0f * _mouseDragSensitivity;
					} else {
//						dragSpeed = Mathf.Sqrt (lastDistanceFromCameraSqr) * _mouseDragSensitivity;
						dragSpeed = lastDistanceFromCamera * _mouseDragSensitivity;
					}
					dragDirection *= 0.1f * dragSpeed;
					if (dragFlipDirection)
						dragDirection *= -1;
					dragDamping = 1;
				}
			}
			
			// Check scroll on borders
			if (_allowScrollOnScreenEdges && _allowUserDrag) {
				bool onEdge = false;
				float mx = Input.mousePosition.x;
				float my = Input.mousePosition.y;
				dragDirection = Misc.Vector3zero;
				if (mx>=0 && mx<Screen.width && my>=0 && my<Screen.height) {
					if ( my < _screenEdgeThickness ) {
						dragDirection += Misc.Vector3up;
						onEdge = true;
					} 
					if ( my >= Screen.height- _screenEdgeThickness ) {
						dragDirection += Misc.Vector3down;
						onEdge = true;
					}
					if ( mx < _screenEdgeThickness) {
						dragDirection += Misc.Vector3right;
						onEdge = true;
					}
					if ( mx >= Screen.width-_screenEdgeThickness ) {
						dragDirection += Misc.Vector3left;
						onEdge = true;
					}
				}
				if (onEdge) {
					if (_currentCamera.orthographic) {
//						dragSpeed = Mathf.Sqrt ( _currentCamera.orthographicSize) * 10.0f * _mouseDragSensitivity;
						dragSpeed = _currentCamera.orthographicSize * 10.0f * _mouseDragSensitivity;
					} else {
						dragSpeed = lastDistanceFromCamera * _mouseDragSensitivity;
					}
					dragDirection *= 0.1f * dragSpeed;
					if (dragFlipDirection)
						dragDirection *= -1;
					dragDamping = 1;
				}
			}
			
			if (dragDamping > 0 && !buttonLeftPressed) {
				if (++dragDamping < 20) {
					dragging = true;
					transform.Translate (dragDirection / dragDamping, Space.Self);
				} else {
					dragDamping = 0;
				}
			}
			
			// Use mouse wheel to zoom in and out
			if (allowUserZoom && (mouseIsOver || wheelAccel != 0)) {
				float wheel = Input.GetAxis ("Mouse ScrollWheel");
				wheelAccel += wheel * (_invertZoomDirection ? -1: 1);;
				
				// Support for pinch on mobile
				if (Input.touchSupported && Input.touchCount == 2) {
					// Store both touches.
					Touch touchZero = Input.GetTouch (0);
					Touch touchOne = Input.GetTouch (1);
					
					// Find the position in the previous frame of each touch.
					Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
					Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
					
					// Find the magnitude of the vector (the distance) between the touches in each frame.
					float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
					float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
					
					// Find the difference in the distances between each frame.
					float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
					
					// Pass the delta to the wheel accel
					wheelAccel += deltaMagnitudeDiff;
				}
				
				if (wheelAccel != 0) {
					wheelAccel = Mathf.Clamp (wheelAccel, -0.1f, 0.1f);
					if (wheelAccel >= 0.01f || wheelAccel <= -0.01f) {
						Vector3 dest;
						GetLocalHitFromMousePos (out dest);
						if (dest != Misc.Vector3zero)
							dest = transform.TransformPoint (dest);
						else
							dest = transform.position;
						
						if (_currentCamera.orthographic) {
							_currentCamera.orthographicSize += _currentCamera.orthographicSize * wheelAccel * _mouseWheelSensitivity;
						} else  {
							transform.Translate((dest - _currentCamera.transform.position) * wheelAccel * _mouseWheelSensitivity);
						}
						wheelAccel /= 1.15f;
					} else {
						wheelAccel = 0;
					}
				}
			}



		}
		public void OnMouseEnter ()
		{
			mouseIsOver = true;
		}
		
		public void OnMouseExit ()
		{
			mouseIsOver = false;
			HideCountryRegionHighlight ();
		}
		
	#endregion

	#region System initialization

		public void Init () {

			// Boot initialization
			int mapLayer = gameObject.layer;
			foreach (Transform t in transform) {
				t.gameObject.layer = mapLayer;
			}
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb!=null) rb.detectCollisions = false;

			SetupViewport();

			// Labels materials
			ReloadFont();

			// Map materials
			frontiersMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Frontiers"));
			frontiersMat.hideFlags = HideFlags.DontSave;
			frontiersMat.shader.maximumLOD = 300;
			hudMatCountry = Instantiate (Resources.Load <Material> ("WMSK/Materials/HudCountry"));
			hudMatCountry.hideFlags = HideFlags.DontSave;
			hudMatProvince = Instantiate (Resources.Load <Material> ("WMSK/Materials/HudProvince"));
			hudMatProvince.hideFlags = HideFlags.DontSave;
			hudMatProvince.renderQueue++;	// render on top of country highlight
			CheckCityIcons();
			citiesNormalMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Cities"));
			citiesNormalMat.name = "Cities";
			citiesNormalMat.hideFlags = HideFlags.DontSave;
			citiesRegionCapitalMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/CitiesCapitalRegion"));
			citiesRegionCapitalMat.name = "CitiesCapitalRegion";
			citiesRegionCapitalMat.hideFlags = HideFlags.DontSave;
			citiesCountryCapitalMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/CitiesCapitalCountry"));
			citiesCountryCapitalMat.name = "CitiesCapitalCountry";
			citiesCountryCapitalMat.hideFlags = HideFlags.DontSave;

			provincesMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Provinces"));
			provincesMat.hideFlags = HideFlags.DontSave;
			outlineMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Outline"));
			outlineMat.hideFlags = HideFlags.DontSave;
			coloredMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/ColorizedRegion"));
			coloredMat.hideFlags = HideFlags.DontSave;
			coloredAlphaMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/ColorizedTranspRegion"));
			coloredAlphaMat.hideFlags = HideFlags.DontSave;
			texturizedMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/TexturizedRegion"));
			texturizedMat.hideFlags = HideFlags.DontSave;
			cursorMatH = Instantiate (Resources.Load <Material> ("WMSK/Materials/CursorH"));
			cursorMatH.hideFlags = HideFlags.DontSave;
			cursorMatV = Instantiate (Resources.Load <Material> ("WMSK/Materials/CursorV"));
			cursorMatV.hideFlags = HideFlags.DontSave;
			imaginaryLinesMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/ImaginaryLines"));
			imaginaryLinesMat.hideFlags = HideFlags.DontSave;
			markerMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Marker"));
			markerMat.hideFlags = HideFlags.DontSave;
			mountPointSpot = Resources.Load <GameObject> ("WMSK/Prefabs/MountPointSpot");
			mountPointsMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Mount Points"));
			mountPointsMat.hideFlags = HideFlags.DontSave;
			gridMat = Instantiate (Resources.Load <Material> ("WMSK/Materials/Grid"));
			gridMat.hideFlags = HideFlags.DontSave;
			hudMatCell = Instantiate (Resources.Load <Material> ("WMSK/Materials/HudCell"));
			hudMatCell.hideFlags = HideFlags.DontSave;

			coloredMatCache = new Dictionary<Color, Material>();
			markerMatCache = new Dictionary<Color, Material>();

			ReloadData ();

			// Redraw frontiers and cities -- destroy layers if they already exists
			if (!Application.isPlaying) Redraw ();

		}

		/// <summary>
		/// Reloads the data of frontiers and cities from datafiles and redraws the map.
		/// </summary>
		public void ReloadData () {
			// Destroy surfaces layer
			DestroySurfaces();
			// read precomputed data
			ReadCountriesPackedString ();
			if (_showCities || GetComponent<WorldMapStrategyKit_Editor.WMSK_Editor>()!=null) ReadCitiesPackedString ();
			if (_showProvinces || _enableProvinceHighlight || GetComponent<WorldMapStrategyKit_Editor.WMSK_Editor>()!=null) ReadProvincesPackedString ();
			ReadMountPointsPackedString();
		}


		void DestroySurfaces() {
			HideCountryRegionHighlights(true);
			HideProvinceRegionHighlight();
			if (frontiersCacheHit!=null) frontiersCacheHit.Clear ();
			InitSurfacesCache();
			if (_surfacesLayer!=null) DestroyImmediate(_surfacesLayer);
			if (provincesObj!=null) DestroyImmediate(provincesObj);
		}

	
	#endregion

	#region Drawing stuff

		/// <summary>
		/// Used internally and by other components to redraw the layers in specific moments. You shouldn't call this method directly.
		/// </summary>
		public void Redraw () {
			if (!gameObject.activeInHierarchy)
				return;
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": Redraw");
#endif

			InitSurfacesCache();	// Initialize surface cache, destroys already generated surfaces

			RestyleEarth ();	// Apply texture to Earth

			DrawFrontiers ();	// Redraw frontiers -- the next method is also called from width property when this is changed

			DrawAllProvinceBorders(false); // Redraw province borders

			DrawCities (); 		// Redraw cities layer

			DrawMountPoints();	// Redraw mount points (only in Editor time)

			DrawCursor (); 		// Draw cursor lines

			DrawImaginaryLines ();    	// Draw longitude & latitude lines

			DrawMapLabels ();	// Destroy existing texts and draw them again

			DrawGrid ();

			SetupViewport();
			if (lastDistanceFromCamera == 0) 
				lastDistanceFromCamera = (transform.position - currentCamera.transform.position).magnitude;

		}

		void InitSurfacesCache() {
			if (surfaces != null) {
				List<GameObject> cached = new List<GameObject> (surfaces.Values);
				for (int k=0; k<cached.Count; k++) {
					if (cached [k] != null)
						DestroyImmediate (cached [k]);
				}
				surfaces.Clear();
			} else {
				surfaces = new Dictionary<int, GameObject> ();
			}
		}

		void CreateSurfacesLayer() {
			Transform t = transform.Find (SURFACE_LAYER);
			if (t != null) {
				DestroyImmediate (t.gameObject);
				for (int k=0;k<countries.Length;k++) 
					for (int r=0;r<countries[k].regions.Count;r++)
						countries[k].regions[r].customMaterial = null;
			}
			_surfacesLayer = new GameObject (SURFACE_LAYER);
			_surfacesLayer.transform.SetParent (transform, false);
			_surfacesLayer.transform.localPosition = Misc.Vector3back * 0.001f;
			_surfacesLayer.layer = gameObject.layer;
		}

		void RestyleEarth () {
			if (gameObject == null)
				return;

			string materialName;
			switch (_earthStyle) {
			case EARTH_STYLE.Alternate1:
				materialName = "Earth2";
				break;
			case EARTH_STYLE.Alternate2:
				materialName = "Earth4";
				break;
			case EARTH_STYLE.Alternate3:
				materialName = "Earth5";
				break;
			case EARTH_STYLE.SolidColor:
				materialName = "EarthSolidColor";
				break;
			case EARTH_STYLE.NaturalHighRes:
				materialName = "EarthHighRes";
				break;
			case EARTH_STYLE.NaturalScenic:
				materialName = "EarthScenic";
				break;
			case EARTH_STYLE.NaturalScenicPlus:
				materialName = "EarthScenicPlus";
				break;
			case EARTH_STYLE.NaturalScenicPlusAlternate1:
				materialName = "EarthScenicPlusAlternate1";
				break;
			default:
				materialName = "Earth";
				break;
			}

			MeshRenderer renderer = gameObject.GetComponent<MeshRenderer> ();
			if (renderer.sharedMaterial == null || !renderer.sharedMaterial.name.Equals (materialName)) {
				earthMat = Instantiate (Resources.Load<Material> ("WMSK/Materials/" + materialName));
				earthMat.hideFlags = HideFlags.DontSave;
				if (_earthStyle == EARTH_STYLE.SolidColor) {
					earthMat.color = _earthColor;
				}
				earthMat.name = materialName;
				renderer.material = earthMat;
				if (earthBlurred!=null && RenderTexture.active != earthBlurred) {
					RenderTexture.DestroyImmediate(earthBlurred);
					earthBlurred = null;
				}
			}

			if (_earthStyle.isScenicPlus()) {
				earthMat.SetColor("_WaterColor", _waterColor);
				if (earthBlurred == null ) {
					EarthPrepareBlurredTexture();
				}
				UpdateScenicPlusDistance();
			}

		}

		void EarthPrepareBlurredTexture() {

			Texture2D earthTex = (Texture2D)earthMat.GetTexture("_MainTex");
			if (earthBlurred == null) {
				earthBlurred = new RenderTexture(earthTex.width/8, earthTex.height/8, 0);
				earthBlurred.hideFlags = HideFlags.DontSave;
			}
			Graphics.Blit(earthTex, earthBlurred);
			earthMat.SetTexture("_EarthBlurred", earthBlurred);
		}


	#endregion



	#region Highlighting

		bool GetLocalHitFromMousePos(out Vector3 localPoint) {
			Vector3 mousePos = Input.mousePosition;
			if (mousePos.x<0 || mousePos.x>=Screen.width || mousePos.y<0 || mousePos.y>=Screen.height) {
				localPoint = Misc.Vector3zero;
				return false;
			}
			return GetLocalHitFromScreenPos(mousePos, out localPoint);
		}

		/// <summary>
		/// Check mouse hit on the map and return the local plane coordinate. Handles viewports.
		/// </summary>
		public bool GetLocalHitFromScreenPos(Vector3 screenPos, out Vector3 localPoint) {
			Ray ray = Camera.main.ScreenPointToRay (screenPos);
			RaycastHit[] hits = Physics.RaycastAll (ray.origin, ray.direction, 500, layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					// Hit the map?
					if (hits [k].collider.gameObject == _renderViewport) {
						localPoint = _renderViewport.transform.InverseTransformPoint (hits [k].point);
						localPoint.z = 0;
						// Is the viewport a render viewport or the map itself? If it's a render viewport projects hit into mapper cam space
						if (renderViewportIsEnabled) {
							// Get plane in screen space
							Vector3 tl = _currentCamera.WorldToViewportPoint(transform.TransformPoint (new Vector3(-0.5f, 0.5f)));
							Vector3 br = _currentCamera.WorldToViewportPoint(transform.TransformPoint (new Vector3(0.5f, -0.5f)));
							localPoint.x += 0.5f;
							localPoint.y += 0.5f;
							// Trace the ray from this position in mapper cam space
							if (localPoint.x>=tl.x && localPoint.x<=br.x && localPoint.y>=br.y && localPoint.y<=tl.y) {
								localPoint.x = (localPoint.x - tl.x) / (br.x-tl.x) - 0.5f;
								localPoint.y = (localPoint.y - br.y) / (tl.y - br.y) - 0.5f;
								return true;
							}
						} else
							return true;
					}
				}
			}
			localPoint = Misc.Vector3zero;
			return false;
		}

		void CheckMousePos () {

			Vector3 localPoint;
			bool goodHit = GetLocalHitFromMousePos(out localPoint);
			if (localPoint == lastMouseMapHitPos && lastMouseMapHitPosGood) {
				return;	// no changes so early exit!
			}
			lastMouseMapHitPos = localPoint;
			lastMouseMapHitPosGood = goodHit;

			if (goodHit) {

				// Cursor follow
				if (_cursorFollowMouse) {
					cursorLocation = localPoint;
				}

				// verify if hitPos is inside any country polygon
				for (int c=0; c<countries.Length; c++) {
					Country country = countries[c];
					if (country.hidden) continue;
					if (!country.regionsRect2D.Contains(localPoint)) continue;
					for (int cr=0; cr<country.regions.Count; cr++) {
						if (country.regions [cr].ContainsPoint (localPoint)) {
							if (c != _countryHighlightedIndex || (c == _countryHighlightedIndex && cr!= _countryRegionHighlightedIndex) ) {
								HighlightCountryRegion (c, cr, false, _showOutline);
								// Raise enter event
								if (OnCountryEnter!=null) OnCountryEnter(c, cr);
							}
							// if show provinces is enabled, then we draw provinces borders
							if (_countryHighlighted.provinces!=null && (_showProvinces || _enableProvinceHighlight)) {
								if (_showProvinces) {
									DrawProvinces (_countryHighlightedIndex, false, false); // draw provinces borders if not drawn
								}
								for (int p=0; p<_countryHighlighted.provinces.Length; p++) {
									// and now, we check if the mouse if inside a province, so highlight it
									Province province = _countryHighlighted.provinces [p];
									if (province.regions == null) { // read province data the first time we need it
										ReadProvincePackedString (province);
									}
									if (province.regionsRect2D.Contains(localPoint)) {
										int provinceIndex = GetProvinceIndex (province);
										for (int pr=0; pr<province.regions.Count; pr++) {
											if (province.regions [pr].ContainsPoint (localPoint)) {
												if (provinceIndex != _provinceHighlightedIndex || (provinceIndex == _provinceHighlightedIndex && pr != _provinceRegionHighlightedIndex) ) {
													HighlightProvinceRegion (provinceIndex, pr, false);
													// Raise enter event
													if (OnProvinceEnter!=null) OnProvinceEnter(provinceIndex, pr);
												}
											}
										}
									}
								}
							}
							// Verify if a city is hit inside selected country
							if (_showCities) CheckMousePosCity(localPoint);

							// Raise mouse move event
							if (OnMouseMove!=null) OnMouseMove(localPoint.x, localPoint.y);
							return;
						}	
					}
				}

				// Verify if a city outside of any country frontier is hit
				if (_showCities) CheckMousePosCity(localPoint);

				// Raise mouse move event
				if (OnMouseMove!=null) OnMouseMove(localPoint.x, localPoint.y);
			}
			HideCountryRegionHighlight ();
			if (!_drawAllProvinces) HideProvinces ();

		}


		void CheckMousePosCity(Vector3 localPoint) {
			int ci = GetCityNearPoint(localPoint, _countryHighlightedIndex);
			if (ci>=0) {
				if (ci!=_cityHighlightedIndex) {
					HideCityHighlight();
					HighlightCity(ci);
				}
			} else if (_cityHighlightedIndex>=0) {
				HideCityHighlight();
			}
		}

	#endregion

	#region Internal API
	
		/// <summary>
		/// Returns the overlay base layer (parent gameObject), useful to overlay stuff on the map (like labels). It will be created if it doesn't exist.
		/// </summary>
		public GameObject GetOverlayLayer (bool createIfNotExists) {
			if (overlayLayer != null) {
				return overlayLayer;
			} else if (createIfNotExists) {
				return CreateOverlay ();
			} else {
				return null;
			}
		}


		void SetDestination (Vector2 point, float duration) {
			SetDestination(point, duration, GetZoomLevel());
		}

		void SetDestination (Vector2 point, float duration, float zoomLevel) {
			flyToStartQuaternion = _currentCamera.transform.rotation;
			flyToStartLocation = _currentCamera.transform.position;
			flyToEndQuaternion = transform.rotation;
			flyToEndLocation = transform.TransformPoint(point) - transform.forward * GetZoomLevelDistance(zoomLevel);
			flyToDuration = duration;
			flyToActive = true;
			flyToStartTime = Time.time;
			if (flyToDuration == 0)
				MoveMapToDestination ();
		}


		float GetZoomLevelDistance(float zoomLevel) {
			zoomLevel = Mathf.Clamp01 (zoomLevel);

			float fv = currentCamera.fieldOfView;
			float radAngle = fv * Mathf.Deg2Rad;
			float aspect = currentCamera.aspect; 
			float frustumDistanceH = mapHeight * 0.5f / Mathf.Tan (radAngle * 0.5f);
			float frustumDistanceW = (mapWidth / aspect) * 0.5f / Mathf.Tan (radAngle * 0.5f);
			float distance;
			if (_fitWindowWidth) {
				distance = Mathf.Max (frustumDistanceH, frustumDistanceW);
			} else {
				distance = Mathf.Min (frustumDistanceH, frustumDistanceW);
			}
			return distance * zoomLevel;

		}

		/// <summary>
		/// Used internally to translate the map during FlyTo operations. Use FlyTo method.
		/// </summary>
		void MoveMapToDestination () {
			float delta;
			Quaternion rotation;
			Vector3 destination;
			if (flyToDuration == 0) {
				delta = flyToDuration;
				rotation = flyToEndQuaternion;
				destination = flyToEndLocation;
			} else {
				delta = (Time.time - flyToStartTime);
				float t = delta / flyToDuration;
				float st = Mathf.SmoothStep (0, 1, t);
				rotation = Quaternion.Lerp (flyToStartQuaternion, flyToEndQuaternion, st);
				destination = Vector3.Lerp (flyToStartLocation, flyToEndLocation, st);
			}
			_currentCamera.transform.rotation = rotation;
			_currentCamera.transform.position = destination;
			if (delta >= flyToDuration)
				flyToActive = false;
		}


		Material GetColoredTexturedMaterial(Color color, Texture2D texture) {
			return GetColoredTexturedMaterial(color, texture, true);
		}


		Material GetColoredTexturedMaterial(Color color, Texture2D texture, bool autoChooseTransparentMaterial) {
			if (texture==null && coloredMatCache.ContainsKey(color)) {
				return coloredMatCache[color];
			} else {
				Material customMat;
				if (texture!=null) {
					customMat = Instantiate(texturizedMat);
					customMat.name = texturizedMat.name;
					customMat.mainTexture = texture;
				} else {
					if (color.a<1.0f || !autoChooseTransparentMaterial) {
						customMat = Instantiate (coloredAlphaMat);
					} else {
						customMat = Instantiate (coloredMat);
					}
					customMat.name = coloredMat.name;
					coloredMatCache[color] = customMat;
				}
				customMat.color = color;
				customMat.hideFlags = HideFlags.DontSave;
				return customMat;
			}
		}

		Material GetColoredMarkerMaterial(Color color) {
			if (markerMatCache.ContainsKey(color)) {
				return markerMatCache[color];
			} else {
				Material customMat;
				customMat = Instantiate (markerMat);
				customMat.name = markerMat.name;
				markerMatCache[color] = customMat;
				customMat.color = color;
				customMat.hideFlags = HideFlags.DontSave;
				return customMat;
			}
		}


		void ApplyMaterialToSurface(GameObject obj, Material sharedMaterial) {
			if (obj!=null) {
				Renderer[] rr = obj.GetComponentsInChildren<Renderer>(true);	// surfaces can be saved under parent when Include All Regions is enabled
				for (int k=0;k<rr.Length;k++) {
					if (rr[k].sharedMaterial!=outlineMat) {
						rr[k].sharedMaterial = sharedMaterial;
					}
				}
			}
		}

		void GetPointFromPackedString(string s, out float x, out float y) {
			int j = s.IndexOf(",");
			string sx = s.Substring(0, j);
			string sy = s.Substring(j+1);
			x = float.Parse (sx)/ MAP_PRECISION;
			y = float.Parse (sy)/ MAP_PRECISION;
		}


		/// <summary>
		/// Internal usage.
		/// </summary>
		public int GetUniqueId(List<IExtendableAttribute> list) {
			for (int k=0;k<1000;k++) {
				int rnd = UnityEngine.Random.Range(0, int.MaxValue);
				for (int o=0;o<list.Count;o++) {
					IExtendableAttribute obj= list[o];
					if (obj!=null && obj.uniqueId == rnd) {
						rnd = 0;
						break;
					}
				}
				if (rnd>0) return rnd;
			}
			return 0;
		}


		#endregion

		#region World Gizmos

		void CheckCursorVisibility() {
			if (_showCursor) {
				if (cursorLayerHLine!=null) {
						if ((mouseIsOverUIElement || !mouseIsOver) && cursorLayerHLine.activeSelf && !cursorAlwaysVisible) {	// not over globe?
							cursorLayerHLine.SetActive(false);
						}  else if (!mouseIsOverUIElement && mouseIsOver && !cursorLayerHLine.activeSelf) {	// finally, should be visible?
							cursorLayerHLine.SetActive(true);
					}
				}
				if (cursorLayerVLine!=null) {
					if ((mouseIsOverUIElement || !mouseIsOver) && cursorLayerVLine.activeSelf && !cursorAlwaysVisible) {	// not over globe?
						cursorLayerVLine.SetActive(false);
					}  else if (!mouseIsOverUIElement && mouseIsOver && !cursorLayerVLine.activeSelf) {	// finally, should be visible?
						cursorLayerVLine.SetActive(true);
					}
				}
			}
		}


		void DrawCursor () {

			if (!_showCursor) return;

			// Generate line V **********************
			Vector3[] points = new Vector3[2];
			int[] indices = new int[2];
			indices [0] = 0;
			indices [1] = 1;
			points [0] = Misc.Vector3up * -0.5f;
			points [1] = Misc.Vector3up * 0.5f;

			Transform t = transform.Find ("CursorV");
			if (t != null)
				DestroyImmediate (t.gameObject);
			cursorLayerVLine = new GameObject ("CursorV");
			cursorLayerVLine.hideFlags = HideFlags.DontSave;
			cursorLayerVLine.transform.SetParent (transform, false);
			cursorLayerVLine.transform.localPosition = Misc.Vector3back * 0.00001f; // needed for minimap
			cursorLayerVLine.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			cursorLayerVLine.layer = gameObject.layer;
			cursorLayerVLine.SetActive (_showCursor);

			Mesh mesh = new Mesh ();
			mesh.vertices = points;
			mesh.SetIndices (indices, MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = cursorLayerVLine.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = cursorLayerVLine.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = cursorMatV;


			// Generate line H **********************
			points[0] = Misc.Vector3right * -0.5f;
			points[1] = Misc.Vector3right * 0.5f;

			t = transform.Find ("CursorH");
			if (t != null)
				DestroyImmediate (t.gameObject);
			cursorLayerHLine = new GameObject ("CursorH");
			cursorLayerHLine.hideFlags = HideFlags.DontSave;
			cursorLayerHLine.transform.SetParent (transform, false);
			cursorLayerHLine.transform.localPosition = Misc.Vector3back * 0.00001f; // needed for minimap
			cursorLayerHLine.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			cursorLayerHLine.layer = gameObject.layer;
			cursorLayerHLine.SetActive (_showCursor);
			
			mesh = new Mesh ();
			mesh.vertices = points;
			mesh.SetIndices (indices, MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			mf = cursorLayerHLine.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			mr = cursorLayerHLine.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = cursorMatH;


		}

		void DrawImaginaryLines () {
			DrawLatitudeLines ();
			DrawLongitudeLines ();
		}

		void DrawLatitudeLines () {
			if (!_showLatitudeLines) return;

			// Generate latitude lines
			List<Vector3> points = new List<Vector3> ();
			List<int> indices = new List<int> ();
			float r = 0.5f;
			int idx = -1;

			for (float a =0; a<90; a += _latitudeStepping) {
				for (int h=1; h>=-1; h--) {
					if (h == 0)
						continue;
					float y = h * a/90.0f * r;
					points.Add (new Vector3 (-r, y, 0));
					points.Add (new Vector3 (r, y, 0));
					indices.Add (++idx);
					indices.Add (++idx);
					if (a == 0)
						break;
				}
			}

			Transform t = transform.Find ("LatitudeLines");
			if (t != null)
				DestroyImmediate (t.gameObject);
			latitudeLayer = new GameObject ("LatitudeLines");
			latitudeLayer.hideFlags = HideFlags.DontSave;
			latitudeLayer.transform.SetParent (transform, false);
			latitudeLayer.transform.localPosition = Misc.Vector3zero;
			latitudeLayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			latitudeLayer.layer = gameObject.layer;
			latitudeLayer.SetActive (_showLatitudeLines);
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points.ToArray ();
			mesh.SetIndices (indices.ToArray (), MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = latitudeLayer.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = latitudeLayer.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = imaginaryLinesMat;
			
		}

		void DrawLongitudeLines () {
			if (!_showLongitudeLines) return;

			// Generate longitude lines
			List<Vector3> points = new List<Vector3> ();
			List<int> indices = new List<int> ();
			float r = 0.5f;
			int idx = -1;
			int step = 180 / _longitudeStepping;

			for (float a =0; a<90; a += step) {
				for (int h=1; h>=-1; h--) {
					if (h == 0)
						continue;
					float x = h * a/90.0f * r;
					points.Add (new Vector3 (x, -r, 0));
					points.Add (new Vector3 (x, r, 0));
					indices.Add (++idx);
					indices.Add (++idx);
					if (a == 0)
						break;
				}
			}

			
			Transform t = transform.Find ("LongitudeLines");
			if (t != null)
				DestroyImmediate (t.gameObject);
			longitudeLayer = new GameObject ("LongitudeLines");
			longitudeLayer.hideFlags = HideFlags.DontSave;
			longitudeLayer.transform.SetParent (transform, false);
			longitudeLayer.transform.localPosition = Misc.Vector3zero;
			longitudeLayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			longitudeLayer.layer = gameObject.layer;
			longitudeLayer.SetActive (_showLongitudeLines);
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points.ToArray ();
			mesh.SetIndices (indices.ToArray (), MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = longitudeLayer.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = longitudeLayer.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = imaginaryLinesMat;

		}

		#endregion

		#region Overlay

		public GameObject CreateOverlay () {
#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": CreateOverlay");
#endif

			// 2D labels layer
			Transform t = transform.Find (OVERLAY_BASE);
			if (t == null) {
				overlayLayer = new GameObject(OVERLAY_BASE);
				overlayLayer.hideFlags = HideFlags.DontSave;
				overlayLayer.transform.SetParent (transform, false);
				overlayLayer.transform.localPosition = Misc.Vector3back * 0.002f;
				overlayLayer.transform.localScale = Misc.Vector3one;
				overlayLayer.layer = gameObject.layer;
			} else {
				overlayLayer = t.gameObject;
				overlayLayer.SetActive (true);
			}
			return overlayLayer;
		}

		void UpdateScenicPlusDistance() {
			if (earthMat==null) return;
			float zoomLevel = GetZoomLevel();
			earthMat.SetFloat("_Distance", zoomLevel);
		}

		#endregion

		#region Markers support
		
		void CheckMarkersLayer() {
			if (markersLayer==null) { // try to capture an existing marker layer
				Transform t = transform.Find("Markers");
				if (t!=null) markersLayer = t.gameObject;
			}
			if (markersLayer==null) { // create it otherwise
				markersLayer = new GameObject("Markers");
				markersLayer.transform.SetParent(transform, false);
				markersLayer.layer = transform.gameObject.layer;
			}
		}
		
		
		#endregion


	}

}