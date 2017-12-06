using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit;
using WorldMapStrategyKit.PathFinding;

namespace WorldMapStrategyKit_Editor {
	[CustomEditor(typeof(WMSK))]
	public class WMSKInspector : Editor {
		WMSK _map;
		Texture2D _headerTexture, _blackTexture;
		string[] earthStyleOptions, frontiersDetailOptions; //, renderViewportQualityOptions;
		GUIStyle blackStyle, sectionHeaderStyle;
		bool expandWindowSection, expandViewportSection, expandEarthSection, expandCitiesSection, expandCountriesSection, expandProvincesSection, expandInteractionSection, expandPathFindingSection, expandCustomAttributes, expandGrid, expandMiscellanea;
		string[] pathFindingHeuristicOptions;
		int[] pathFindingHeuristicValues;

		void OnEnable () {

			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f) : new Color (0.7f, 0.7f, 0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			_map = (WMSK)target;
			_headerTexture = Resources.Load<Texture2D> ("WMSK/EditorHeader");
			if (_map.countries == null) {
				_map.Init ();
			}
			earthStyleOptions = new string[] {
				"Natural", "Alternate Style 1", "Alternate Style 2", "Alternate Style 3", "Solid Color", "Natural (HighRes)", "Natural (Scenic)", "Natural (Scenic Plus)", "Natural (Scenic Plus Alt 1)"
			};
			frontiersDetailOptions = new string[] {
				"Low",
				"High"
			};
//			renderViewportQualityOptions = new string[] {
//				"Low",
//				"Medium",
//				"High"
//			};
			pathFindingHeuristicOptions = new string[] { "Manhattan", "MaxDXDY" }; //, "DiagonalShortCut", "Euclidean", "EuclideanNoSQR", "Custom" };
			pathFindingHeuristicValues = new int[] { (int)HeuristicFormula.Manhattan, (int)HeuristicFormula.MaxDXDY }; //, (int)HeuristicFormula.DiagonalShortCut, (int)HeuristicFormula.Euclidean, (int)HeuristicFormula.EuclideanNoSQR, (int)HeuristicFormula.Custom1 };

			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;

			// Restore folding sections statte
			expandWindowSection = EditorPrefs.GetBool("expandWindowSection", false);
			expandViewportSection = EditorPrefs.GetBool("expandViewportSection", false);
			expandEarthSection = EditorPrefs.GetBool("expandEarthSection", false);
			expandCitiesSection = EditorPrefs.GetBool("expandCitiesSection", false);
			expandCountriesSection = EditorPrefs.GetBool("expandCountriesSection", false);
			expandProvincesSection = EditorPrefs.GetBool("expandProvincesSection", false);
			expandInteractionSection = EditorPrefs.GetBool("expandInteractionSection", false);
			expandPathFindingSection = EditorPrefs.GetBool("expandPathFindingSection", false);
			expandCustomAttributes = EditorPrefs.GetBool("expandCustomAttributes", false);
			expandGrid = EditorPrefs.GetBool("expandGrid", false);
			expandMiscellanea = EditorPrefs.GetBool("expandMiscellanea", false);
		}

		void OnDestroy() {
			// Restore folding sections statte
			EditorPrefs.SetBool("expandWindowSection", expandWindowSection);
			EditorPrefs.SetBool("expandViewportSection", expandViewportSection);
			EditorPrefs.SetBool("expandEarthSection", expandEarthSection);
			EditorPrefs.SetBool("expandCitiesSection", expandCitiesSection);
			EditorPrefs.SetBool("expandCountriesSection", expandCountriesSection);
			EditorPrefs.SetBool("expandProvincesSection", expandProvincesSection);
			EditorPrefs.SetBool("expandInteractionSection", expandInteractionSection);
			EditorPrefs.SetBool("expandPathFindingSection", expandPathFindingSection);
			EditorPrefs.SetBool("expandCustomAttributes", expandCustomAttributes);
			EditorPrefs.SetBool("expandGrid", expandGrid);
			EditorPrefs.SetBool("expandMiscellanea", expandMiscellanea);
		}

		public override void OnInspectorGUI () {
			_map.isDirty = false;

			if (sectionHeaderStyle==null) {
				sectionHeaderStyle = new GUIStyle(EditorStyles.foldout);
			}
			sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
			sectionHeaderStyle.margin = new RectOffset(12,0,0,0);
			sectionHeaderStyle.fontStyle = FontStyle.Bold;

			EditorGUILayout.Separator ();
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;  
			GUILayout.Label (_headerTexture, GUILayout.ExpandWidth (true));
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;  
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandWindowSection = EditorGUILayout.Foldout(expandWindowSection, "Window settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();

			if (expandWindowSection) {
				EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Fit Window Width", GUILayout.Width (120));
			_map.fitWindowWidth = EditorGUILayout.Toggle (_map.fitWindowWidth);
			GUILayout.Label ("Fit Window Height");
			_map.fitWindowHeight = EditorGUILayout.Toggle (_map.fitWindowHeight);
			if (GUILayout.Button ("Center Map")) {
				_map.CenterMap ();
			}
			EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				float left, top, width, height;
				EditorGUILayout.LabelField("Left", GUILayout.Width(45));
				left = EditorGUILayout.FloatField(_map.windowRect.x, GUILayout.Width(40));
				EditorGUILayout.LabelField("Bottom", GUILayout.Width(45));
				top = EditorGUILayout.FloatField(_map.windowRect.y, GUILayout.Width(40));
				if (GUILayout.Button ("Clear Constraints", GUILayout.Width(120))) {
					_map.windowRect = new Rect(-0.5f,-0.5f,1,1);
					_map.isDirty = true;
					EditorGUIUtility.ExitGUI();
				}
				if (GUILayout.Button ("?", GUILayout.Width(20))) {
					EditorUtility.DisplayDialog("Window Constraints", "Set rectangular coordinates for the map constraints (-0.5f=left/bottom, 0.5f=top/right). Note that window constraints only work when Fit To Window Width and/or Fit To Window Height are checked.", "Ok");
					EditorGUIUtility.ExitGUI();
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField("Width", GUILayout.Width(45));
				width = EditorGUILayout.FloatField(_map.windowRect.width, GUILayout.Width(40));
				EditorGUILayout.LabelField("Height", GUILayout.Width(45));
				height = EditorGUILayout.FloatField(_map.windowRect.height, GUILayout.Width(40));
				_map.windowRect = new Rect(left, top, width, height);

				if (GUILayout.Button ("Set Current Rect", GUILayout.Width(120))) {
					_map.windowRect = _map.renderViewportRect;
					_map.isDirty = true;
					EditorGUIUtility.ExitGUI();
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField("Zoom Level", GUILayout.Width(85));
				float zoomLevel = _map.GetZoomLevel();
				EditorGUILayout.LabelField(zoomLevel.ToString("F7"), GUILayout.Width(90));

				if (GUILayout.Button ("Copy to ClipBoard", GUILayout.Width(120))) {
					EditorGUIUtility.systemCopyBuffer = "map.windowRect = new Rect(" + left.ToString("F7") + "f, " + top.ToString("F7") + "f, " + width.ToString("F7") + "f, " + height.ToString("F7") + "f);\nmap.SetZoomLevel(" + zoomLevel.ToString("F7") + "f);";
					EditorUtility.DisplayDialog("", "Window rect and zoom level sample code copied to clipboard.", "Ok");
				}
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandViewportSection = EditorGUILayout.Foldout(expandViewportSection, "Viewport settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandViewportSection) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Render Viewport", GUILayout.Width (120));
				_map.renderViewport = (GameObject)EditorGUILayout.ObjectField (_map.renderViewport, typeof(GameObject), true );
				
				if (GUILayout.Button("?", GUILayout.Width(24))) {
					EditorUtility.DisplayDialog("Render Viewport Help", "Render Viewport allows to display the map onto a Viewport GameObject, cropping the map according to the size of the viewport.\n\nTo use this feature drag a Viewport prefab into the scene and assign the viewport gameobject created to this property.", "Ok");
				}
				EditorGUILayout.EndHorizontal ();
				
				if (_map.renderViewport!=_map.gameObject) {
					//				EditorGUILayout.BeginHorizontal ();
					//				GUILayout.Label ("  Render Quality", GUILayout.Width (120));
					//				_map.renderViewportQuality = (VIEWPORT_QUALITY)EditorGUILayout.Popup ((int)_map.renderViewportQuality, renderViewportQualityOptions);
					//				EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Ground Elevation", GUILayout.Width (120));
					_map.earthElevation = EditorGUILayout.Slider (_map.earthElevation, 0, 2.0f);
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("Units Scale Multiplier", "Scale multiplier applied to all game objects put on top of the viewport."), GUILayout.Width (120));
					_map.renderViewportGOAutoScaleMultiplier = EditorGUILayout.Slider (_map.renderViewportGOAutoScaleMultiplier, 0.1f, 100f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("   Minimum Scale", "Scale multiplier applied to all game objects put on top of the viewport."), GUILayout.Width (120));
					_map.renderViewportGOAutoScaleMin = EditorGUILayout.Slider (_map.renderViewportGOAutoScaleMin, 0.1f, 10f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("   Maximum Scale", "Scale multiplier applied to all game objects put on top of the viewport."), GUILayout.Width (120));
					_map.renderViewportGOAutoScaleMax = EditorGUILayout.Slider (_map.renderViewportGOAutoScaleMax, 0.1f, 10f);
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Cloud Layer", GUILayout.Width (120));
					_map.earthCloudLayer = EditorGUILayout.Toggle (_map.earthCloudLayer, GUILayout.Width(40.0f));
					if (_map.earthCloudLayer) {
						GUILayout.Label ("Speed");
						_map.earthCloudLayerSpeed = EditorGUILayout.Slider (_map.earthCloudLayerSpeed, -5f, 5f);
						EditorGUILayout.EndHorizontal ();				
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Clouds Elevation", GUILayout.Width (120));
						_map.earthCloudLayerElevation = -EditorGUILayout.Slider (-_map.earthCloudLayerElevation, 0.1f,30.0f);
						EditorGUILayout.EndHorizontal ();				
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Clouds Alpha", GUILayout.Width (120));
						_map.earthCloudLayerAlpha = EditorGUILayout.Slider (_map.earthCloudLayerAlpha, 0f, 1f);
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Shadows Strength", GUILayout.Width (120));
						_map.earthCloudLayerShadowStrength = EditorGUILayout.Slider (_map.earthCloudLayerShadowStrength, 0f, 1f);
					}
					EditorGUILayout.EndHorizontal ();				
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Fog of War", GUILayout.Width (120));
					_map.fogOfWarLayer = EditorGUILayout.Toggle (_map.fogOfWarLayer, GUILayout.Width(40.0f));
					if (_map.fogOfWarLayer) {
						GUILayout.Label ("Color");
						_map.fogOfWarColor = EditorGUILayout.ColorField (_map.fogOfWarColor, GUILayout.Width (50));
					}
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("Sun", "Assign a Game Object (usually a Directional Light that acts as the Sun) to automatically synchronize the light direction with the time of day parameter below."), GUILayout.Width (130));
					_map.sun = (GameObject)EditorGUILayout.ObjectField (_map.sun, typeof(GameObject), true );
					EditorGUILayout.EndHorizontal ();
					if (_map.sun!=null) {
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Time Of Day", GUILayout.Width (120));
						_map.timeOfDay = EditorGUILayout.Slider (_map.timeOfDay, 0f, 24f);
						EditorGUILayout.EndHorizontal ();
					}

				}
			}
			
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandEarthSection = EditorGUILayout.Foldout(expandEarthSection, "Earth settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();

			if (expandEarthSection) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Show Earth", GUILayout.Width (120));
			_map.showEarth = EditorGUILayout.Toggle (_map.showEarth, GUILayout.Width(40));

			if (_map.showEarth) {
				GUILayout.Label ("Style");
				_map.earthStyle = (EARTH_STYLE)EditorGUILayout.Popup ((int)_map.earthStyle, earthStyleOptions);

				if (_map.earthStyle == EARTH_STYLE.SolidColor) {
					GUILayout.Label ("Color");
					_map.earthColor = EditorGUILayout.ColorField (_map.earthColor, GUILayout.Width (50));
				}

				if (_map.earthStyle.isScenicPlus()) {
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Water Color", GUILayout.Width (120));
					_map.waterColor = EditorGUILayout.ColorField (_map.waterColor, GUILayout.Width (50));
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Latitude Lines", GUILayout.Width (120));
			_map.showLatitudeLines = EditorGUILayout.Toggle (_map.showLatitudeLines, GUILayout.Width(40));
			GUILayout.Label ("Stepping");
			_map.latitudeStepping = EditorGUILayout.IntSlider (_map.latitudeStepping, 5, 45);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Longitude Lines", GUILayout.Width (120));
			_map.showLongitudeLines = EditorGUILayout.Toggle (_map.showLongitudeLines, GUILayout.Width(40));
			GUILayout.Label ("Stepping");
			_map.longitudeStepping = EditorGUILayout.IntSlider (_map.longitudeStepping, 5, 45);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Color", GUILayout.Width (120));
				_map.imaginaryLinesColor = EditorGUILayout.ColorField (_map.imaginaryLinesColor, GUILayout.Width (50));
			EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandCitiesSection = EditorGUILayout.Foldout(expandCitiesSection, "Cities settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandCitiesSection) {

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Cities", GUILayout.Width (120));
			_map.showCities = EditorGUILayout.Toggle (_map.showCities);
			EditorGUILayout.EndHorizontal ();

			if (_map.showCities) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Cities Color", GUILayout.Width (120));
				_map.citiesColor = EditorGUILayout.ColorField (_map.citiesColor, GUILayout.Width (40));
				GUILayout.Label ("Region Cap.");
				_map.citiesRegionCapitalColor = EditorGUILayout.ColorField (_map.citiesRegionCapitalColor, GUILayout.Width (40));
				GUILayout.Label ("Capital");
				_map.citiesCountryCapitalColor = EditorGUILayout.ColorField (_map.citiesCountryCapitalColor, GUILayout.Width (40));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Icon Size", GUILayout.Width(120));
				_map.cityIconSize = EditorGUILayout.Slider (_map.cityIconSize, 0.1f, 5.0f);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Min Population (K)", GUILayout.Width (120));
				_map.minPopulation = EditorGUILayout.IntSlider (_map.minPopulation, 0, 3000);
				GUILayout.Label (_map.numCitiesDrawn + "/" + _map.cities.Count);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Always Visible:", GUILayout.Width (120));
				int cityClassFilter = 0;
				bool cityBit;
				cityBit = EditorGUILayout.Toggle ((_map.cityClassAlwaysShow & WMSK.CITY_CLASS_FILTER_REGION_CAPITAL_CITY)!=0, GUILayout.Width(20));
				GUILayout.Label("Region Capitals");
				if (cityBit) cityClassFilter += WMSK.CITY_CLASS_FILTER_REGION_CAPITAL_CITY;
				cityBit = EditorGUILayout.Toggle ((_map.cityClassAlwaysShow & WMSK.CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY)!=0, GUILayout.Width(20));
				GUILayout.Label("Country Capitals");
				if (cityBit) cityClassFilter += WMSK.CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY;
				_map.cityClassAlwaysShow = cityClassFilter;
				EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Normal City Icon", GUILayout.Width (120));
					_map.citySpot = (GameObject)EditorGUILayout.ObjectField (_map.citySpot, typeof(GameObject), false );
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Region Capital Icon", GUILayout.Width (120));
					_map.citySpotCapitalRegion = (GameObject)EditorGUILayout.ObjectField (_map.citySpotCapitalRegion, typeof(GameObject), false );
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Country Capital Icon", GUILayout.Width (120));
					_map.citySpotCapitalCountry = (GameObject)EditorGUILayout.ObjectField (_map.citySpotCapitalCountry, typeof(GameObject), false );
					EditorGUILayout.EndHorizontal ();


			}
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandCountriesSection = EditorGUILayout.Foldout(expandCountriesSection, "Countries settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandCountriesSection) {

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Countries", GUILayout.Width (120));
			_map.showFrontiers = EditorGUILayout.Toggle (_map.showFrontiers);
			EditorGUILayout.EndHorizontal ();

			if (_map.showFrontiers) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Frontiers Detail", GUILayout.Width (120));
				_map.frontiersDetail = (FRONTIERS_DETAIL)EditorGUILayout.Popup ((int)_map.frontiersDetail, frontiersDetailOptions);

				GUILayout.Label (_map.countries.Length.ToString ());

				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Frontiers Color", GUILayout.Width (120));
				_map.frontiersColor = EditorGUILayout.ColorField (_map.frontiersColor); //, GUILayout.Width (50));

				GUILayout.Label ("Outer Color", GUILayout.Width (120));
				_map.frontiersColorOuter = EditorGUILayout.ColorField (_map.frontiersColorOuter, GUILayout.Width (50));

				EditorGUILayout.EndHorizontal ();

			}

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Country Highlight", GUILayout.Width (120));
			_map.enableCountryHighlight = EditorGUILayout.Toggle (_map.enableCountryHighlight);

			if (_map.enableCountryHighlight) {
				GUILayout.Label ("Highlight Color", GUILayout.Width (120));
				_map.fillColor = EditorGUILayout.ColorField (_map.fillColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Draw Outline", GUILayout.Width (120));
				_map.showOutline = EditorGUILayout.Toggle (_map.showOutline);
				if (_map.showOutline) {
					GUILayout.Label ("Outline Color", GUILayout.Width (120));
					_map.outlineColor = EditorGUILayout.ColorField (_map.outlineColor, GUILayout.Width (50));
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Include All Regions", GUILayout.Width (120));
				_map.highlightAllCountryRegions = EditorGUILayout.Toggle (_map.highlightAllCountryRegions);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Country Names", GUILayout.Width (120));
			_map.showCountryNames = EditorGUILayout.Toggle (_map.showCountryNames);
			EditorGUILayout.EndHorizontal ();

			if (_map.showCountryNames) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Relative Size", GUILayout.Width (120));
				_map.countryLabelsSize = EditorGUILayout.Slider (_map.countryLabelsSize, 0.01f, 0.9f);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Minimum Size", GUILayout.Width (120));
				_map.countryLabelsAbsoluteMinimumSize = EditorGUILayout.Slider (_map.countryLabelsAbsoluteMinimumSize, 0.01f, 2.5f);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Font", GUILayout.Width (120));
				_map.countryLabelsFont = (Font)EditorGUILayout.ObjectField (_map.countryLabelsFont, typeof(Font), false);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Labels Color", GUILayout.Width (120));
				_map.countryLabelsColor = EditorGUILayout.ColorField (_map.countryLabelsColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Draw Shadow", GUILayout.Width (120));
				_map.showLabelsShadow = EditorGUILayout.Toggle (_map.showLabelsShadow);
				if (_map.showLabelsShadow) {
					GUILayout.Label ("Shadow Color", GUILayout.Width (120));
					_map.countryLabelsShadowColor = EditorGUILayout.ColorField (_map.countryLabelsShadowColor, GUILayout.Width (50));
				}
				EditorGUILayout.EndHorizontal ();
			}
			}
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandProvincesSection = EditorGUILayout.Foldout(expandProvincesSection, "Provinces settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandProvincesSection) {
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Provinces", GUILayout.Width (120));
			_map.showProvinces = EditorGUILayout.Toggle (_map.showProvinces);
			if (_map.showProvinces) {
				GUILayout.Label ("Draw All Provinces", GUILayout.Width (120));
				_map.drawAllProvinces = EditorGUILayout.Toggle (_map.drawAllProvinces);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Borders Color", GUILayout.Width (120));
				_map.provincesColor = EditorGUILayout.ColorField (_map.provincesColor, GUILayout.Width (50));
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Province Highlight", GUILayout.Width (120));
			_map.enableProvinceHighlight = EditorGUILayout.Toggle (_map.enableProvinceHighlight);
			if (_map.enableProvinceHighlight) {
				GUILayout.Label ("Highlight Color", GUILayout.Width (120));
				_map.provincesFillColor = EditorGUILayout.ColorField (_map.provincesFillColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Include All Regions", GUILayout.Width (120));
					_map.highlightAllProvinceRegions = EditorGUILayout.Toggle (_map.highlightAllProvinceRegions);
				}
			EditorGUILayout.EndHorizontal ();

			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandInteractionSection = EditorGUILayout.Foldout(expandInteractionSection, "Interaction settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandInteractionSection) {

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Cursor", GUILayout.Width (120));
			_map.showCursor = EditorGUILayout.Toggle (_map.showCursor);

			if (_map.showCursor) {
				GUILayout.Label ("Cursor Color", GUILayout.Width (120));
				_map.cursorColor = EditorGUILayout.ColorField (_map.cursorColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Follow Mouse", GUILayout.Width (120));
				_map.cursorFollowMouse = EditorGUILayout.Toggle (_map.cursorFollowMouse);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Always Visible", GUILayout.Width (120));
				_map.cursorAlwaysVisible = EditorGUILayout.Toggle (_map.cursorAlwaysVisible, GUILayout.Width(40));
				GUILayout.Label ("Respect Other UI", GUILayout.Width (120));
				_map.respectOtherUI = EditorGUILayout.Toggle (_map.respectOtherUI);
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Allow User Drag", GUILayout.Width (120));
			_map.allowUserDrag = EditorGUILayout.Toggle (_map.allowUserDrag, GUILayout.Width (30));
			if (_map.allowUserDrag) {
				GUILayout.Label ("Speed");
				_map.mouseDragSensitivity = EditorGUILayout.Slider (_map.mouseDragSensitivity, 0.1f, 3);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Right Click Centers", GUILayout.Width (120));
				_map.centerOnRightClick = EditorGUILayout.Toggle (_map.centerOnRightClick, GUILayout.Width (30));
				GUILayout.Label ("Constant Drag Speed", GUILayout.Width (120));
				_map.dragConstantSpeed = EditorGUILayout.Toggle (_map.dragConstantSpeed,  GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Allow Keys (WASD)", GUILayout.Width (120));
				_map.allowUserKeys = EditorGUILayout.Toggle (_map.allowUserKeys, GUILayout.Width (30));
				if (_map.allowUserKeys) {
					GUILayout.Label ("Flip Direction", GUILayout.Width (120));
					_map.dragFlipDirection = EditorGUILayout.Toggle (_map.dragFlipDirection,  GUILayout.Width (50));
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Screen Edge Scroll", GUILayout.Width (120));
				_map.allowScrollOnScreenEdges = EditorGUILayout.Toggle (_map.allowScrollOnScreenEdges, GUILayout.Width (30));
				if (_map.allowScrollOnScreenEdges) {
					GUILayout.Label ("Edge Thickness");
					_map.screenEdgeThickness = EditorGUILayout.IntSlider (_map.screenEdgeThickness, 1, 10);
				}
			}
			EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Allow User Zoom", GUILayout.Width (120));
				_map.allowUserZoom = EditorGUILayout.Toggle (_map.allowUserZoom);
				if (_map.allowUserZoom) {
					GUILayout.Label ("Speed");
					_map.mouseWheelSensitivity = EditorGUILayout.Slider (_map.mouseWheelSensitivity, 0.1f, 3);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Invert Direction", GUILayout.Width (120));
					_map.invertZoomDirection = EditorGUILayout.Toggle (_map.invertZoomDirection);
					GUILayout.Label (new GUIContent("Distance Min", "0 = default min distance"));
					_map.zoomMinDistance = EditorGUILayout.FloatField(_map.zoomMinDistance, GUILayout.Width(50));
					GUILayout.Label (new GUIContent("Max", "10m = default max distance"));
					_map.zoomMaxDistance = EditorGUILayout.FloatField(_map.zoomMaxDistance, GUILayout.Width(50));
				}
				EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Navigation Time", GUILayout.Width (120));
			_map.navigationTime = EditorGUILayout.Slider (_map.navigationTime, 0, 10);
			EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();
			
			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandPathFindingSection = EditorGUILayout.Foldout(expandPathFindingSection, "Path finding settings", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandPathFindingSection) {
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Heuristic", GUILayout.Width (120));
				_map.pathFindingHeuristicFormula = (HeuristicFormula) EditorGUILayout.IntPopup ((int)_map.pathFindingHeuristicFormula, pathFindingHeuristicOptions, pathFindingHeuristicValues);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Default Max Cost", GUILayout.Width (120));
				_map.pathFindingMaxCost = EditorGUILayout.IntField(_map.pathFindingMaxCost, GUILayout.Width(80));
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();
			
			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandCustomAttributes = EditorGUILayout.Foldout(expandCustomAttributes, "Custom Attributes", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandCustomAttributes) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Country Attribute File", GUILayout.Width (120));
				_map.countryAttributeFile = EditorGUILayout.TextField(_map.countryAttributeFile);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Province Attribute File", GUILayout.Width (120));
				_map.provinceAttributeFile = EditorGUILayout.TextField(_map.provinceAttributeFile);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("City Attribute File", GUILayout.Width (120));
				_map.cityAttributeFile = EditorGUILayout.TextField(_map.cityAttributeFile);
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandGrid = EditorGUILayout.Foldout(expandGrid, "Grid", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();
			
			if (expandGrid) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Show Grid", GUILayout.Width(120));
				_map.showGrid = EditorGUILayout.Toggle (_map.showGrid);
				EditorGUILayout.EndHorizontal();
				if (_map.showGrid) {
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Columns", GUILayout.Width(120));
					_map.gridColumns = EditorGUILayout.IntSlider (_map.gridColumns, 32, 512);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Rows", GUILayout.Width(120));
					_map.gridRows = EditorGUILayout.IntSlider (_map.gridRows, 16, 256);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label ("Color", GUILayout.Width (120));
					_map.gridColor = EditorGUILayout.ColorField (_map.gridColor, GUILayout.Width (50));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label ("Enable Highlight", GUILayout.Width (120));
					_map.enableCellHighlight = EditorGUILayout.Toggle (_map.enableCellHighlight);
					if (_map.enableCellHighlight) {
						GUILayout.Label ("Highlight Color", GUILayout.Width (120));
						_map.cellHighlightColor = EditorGUILayout.ColorField (_map.cellHighlightColor, GUILayout.Width (50));
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Highlight Fade", GUILayout.Width(120));
						_map.highlightFadeAmount = EditorGUILayout.Slider (_map.highlightFadeAmount, 0.0f, 1.0f);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Visible Distance Min", GUILayout.Width (120));
					_map.gridMinDistance = EditorGUILayout.FloatField(_map.gridMinDistance, GUILayout.Width(50));
					GUILayout.Label (new GUIContent("Max", "10m = default max distance"), GUILayout.Width(60));
					_map.gridMaxDistance = EditorGUILayout.FloatField(_map.gridMaxDistance, GUILayout.Width(50));
					EditorGUILayout.EndHorizontal();

				}
			}
			
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			expandMiscellanea = EditorGUILayout.Foldout(expandMiscellanea, "Miscellanea", sectionHeaderStyle);
			EditorGUILayout.EndHorizontal ();

			if (expandMiscellanea) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (new GUIContent("Prewarm At Start", "Precomputes big country surfaces and path finding matrices during initialization to allow smoother performance during play."), GUILayout.Width (120));
				_map.prewarm = EditorGUILayout.Toggle(_map.prewarm, GUILayout.Width(40));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (new GUIContent("Geodata Folder Prefix", "Path after any Resources folder where geodata files reside."), GUILayout.Width (120));
				_map.geodataResourcesPath = EditorGUILayout.TextField(_map.geodataResourcesPath);
				EditorGUILayout.EndHorizontal ();

			}

			EditorGUILayout.EndVertical (); 

			// Extra components opener
			EditorGUILayout.Separator ();
			float buttonWidth = Screen.width * 0.4f;
			if (_map.gameObject.activeInHierarchy) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();

				if (_map.gameObject.GetComponent<WMSK_Calculator> () == null) {
					if (GUILayout.Button ("Open Calculator", GUILayout.Width (buttonWidth))) {
						_map.gameObject.AddComponent<WMSK_Calculator> ();
					}
				} else {
					if (GUILayout.Button ("Hide Calculator", GUILayout.Width (buttonWidth))) {
						DestroyImmediate (_map.gameObject.GetComponent<WMSK_Calculator> ());
						EditorGUIUtility.ExitGUI ();
					}
				}

				if (_map.gameObject.GetComponent<WMSK_Ticker> () == null) {
					if (GUILayout.Button ("Open Ticker", GUILayout.Width (buttonWidth))) {
						_map.gameObject.AddComponent<WMSK_Ticker> ();
					}
				} else {
					if (GUILayout.Button ("Hide Ticker", GUILayout.Width (buttonWidth))) {
						DestroyImmediate (_map.gameObject.GetComponent<WMSK_Ticker> ());
						EditorGUIUtility.ExitGUI ();
					}
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (_map.gameObject.GetComponent<WMSK_Editor> () == null) {
					if (GUILayout.Button ("Open Editor", GUILayout.Width (buttonWidth))) {
						// Unity 5.3.1 prevents raycasting in the scene view if rigidbody is present
						Rigidbody rb = _map.gameObject.GetComponent<Rigidbody>();
						if (rb!=null) DestroyImmediate(rb);
						_map.gameObject.AddComponent<WMSK_Editor> ();
					}
				} else {
					if (GUILayout.Button ("Hide Editor", GUILayout.Width (buttonWidth))) {
						_map.HideProvinces();
						_map.HideCountrySurfaces();
						_map.HideProvinceSurfaces();
						_map.Redraw();
						DestroyImmediate (_map.gameObject.GetComponent<WMSK_Editor> ());
						EditorGUIUtility.ExitGUI ();
					}
				}

				if (_map.gameObject.GetComponent<WMSK_Decorator> () == null) {
					if (GUILayout.Button ("Open Decorator", GUILayout.Width (buttonWidth))) {
						_map.gameObject.AddComponent<WMSK_Decorator> ();
					}
				} else {
					if (GUILayout.Button ("Hide Decorator", GUILayout.Width (buttonWidth))) {
						DestroyImmediate (_map.gameObject.GetComponent<WMSK_Decorator> ());
						EditorGUIUtility.ExitGUI ();
					}
				}

				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("About", GUILayout.Width(buttonWidth * 2.0f))) {
				WMSKAbout.ShowAboutWindow();
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal ();

 
			if (_map.isDirty) {
				EditorUtility.SetDirty (target);
			}
		}

		Texture2D MakeTex (int width, int height, Color col) {
			Color[] pix = new Color[width * height];
			
			for (int i = 0; i < pix.Length; i++)
				pix [i] = col;
			
			Texture2D result = new Texture2D (width, height);
			result.SetPixels (pix);
			result.Apply ();
			
			return result;
		}



	}

}