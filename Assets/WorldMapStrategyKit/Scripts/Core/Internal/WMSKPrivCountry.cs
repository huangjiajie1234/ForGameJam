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
using Poly2Tri;

namespace WorldMapStrategyKit {

	public partial class WMSK : MonoBehaviour {

		const string COUNTRY_OUTLINE_GAMEOBJECT_NAME = "countryOutline";
		const string COUNTRY_ATTRIB_DEFAULT_FILENAME = "countriesAttrib";

		/// <summary>
		/// Country look up dictionary. Used internally for fast searching of country names.
		/// </summary>
		Dictionary<string, int>_countryLookup;
		int lastCountryLookupCount = -1;

		Dictionary<string, int>countryLookup {
			get {
				if (countries==null) return _countryLookup;
				if (_countryLookup != null && countries.Length == lastCountryLookupCount)
					return _countryLookup;
				if (_countryLookup == null) {
					_countryLookup = new Dictionary<string,int> ();
				} else {
					_countryLookup.Clear ();
				}
				if (countries!=null) {
					for (int k=0; k<countries.Length; k++)
						_countryLookup.Add (countries [k].name, k);
				}
				lastCountryLookupCount = _countryLookup.Count;
				return _countryLookup;
			}
		}

		// resources
		Material frontiersMat, hudMatCountry;

		// gameObjects
		GameObject countryRegionHighlightedObj;
		GameObject frontiersLayer;

		// caché and gameObject lifetime control
		Vector3[][] frontiers;
		int[][] frontiersIndices;


		void ReadCountriesPackedString () {
			lastCountryLookupCount = -1;
			string frontiersFileName = _geodataResourcesPath + (_frontiersDetail == FRONTIERS_DETAIL.Low ? "/countries110" : "/countries10");
			TextAsset ta = Resources.Load<TextAsset> (frontiersFileName);
			string s = ta.text;
			string[] countryList = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int countryCount = countryList.Length;
			countries = new Country[countryCount];
			for (int k=0; k<countryCount; k++) {
				string[] countryInfo = countryList [k].Split (new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
				string name = countryInfo [0];
				string continent = countryInfo [1];
				int uniqueId;
				if (countryInfo.Length>=5) {
					uniqueId = int.Parse(countryInfo[4]);
				} else {
					uniqueId = GetUniqueId(new List<IExtendableAttribute>(countries));
				}
				Country country = new Country (name, continent, uniqueId);
				string[] regions = countryInfo [2].Split (new char[] {'*'}, StringSplitOptions.RemoveEmptyEntries);
				int regionCount = regions.Length;
				country.regions = new List<Region> ();
				float maxVol = 0;
				Vector2 minCountry = Misc.Vector2one * 10;
				Vector2 maxCountry = -minCountry;
				for (int r=0; r<regionCount; r++) {
					string[] coordinates = regions [r].Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					int coorCount = coordinates.Length;
					if (coorCount<3) continue;
					Vector2 min = Misc.Vector2one * 10;
					Vector2 max = -min;
					Region countryRegion = new Region (country, country.regions.Count);
					countryRegion.points = new Vector2[coorCount];
					for (int c=0; c<coorCount; c++) {
						float x, y;
						GetPointFromPackedString(coordinates[c], out x, out y);
						if (x < min.x)
							min.x = x;
						if (x > max.x)
							max.x = x;
						if (y < min.y)
							min.y = y;
						if (y > max.y)
							max.y = y;
						Vector2 point = new Vector2 (x, y);
						countryRegion.points [c] = point;
					}
					Vector3 normRegionCenter = (min + max) * 0.5f;
					countryRegion.center = normRegionCenter; 

					// Calculate country bounding rect
					if (min.x<minCountry.x) minCountry.x = min.x;
					if (min.y<minCountry.y) minCountry.y = min.y;
					if (max.x>maxCountry.x) maxCountry.x = max.x;
					if (max.y>maxCountry.y) maxCountry.y = max.y;
					countryRegion.rect2D = new Rect (min.x, min.y,  Math.Abs (max.x - min.x), Mathf.Abs (max.y - min.y));
					float vol = (max - min).sqrMagnitude;
					if (vol > maxVol) {
						maxVol = vol;
						country.mainRegionIndex = country.regions.Count;
						country.center = countryRegion.center;
					}
					country.regions.Add (countryRegion);
				}
				// hidden
				if (countryInfo.Length>=4) {
					int hidden = 0;
					if (int.TryParse(countryInfo[3], out hidden)) {
						country.hidden = hidden>0;
					}
				}
				country.regionsRect2D = new Rect (minCountry.x, minCountry.y,  Math.Abs (maxCountry.x - minCountry.x), Mathf.Abs (maxCountry.y - minCountry.y));
				countries[k] = country;
			}

			ReloadCountryAttributes();
		}

		void ReloadCountryAttributes() {
			TextAsset ta = Resources.Load<TextAsset> (_geodataResourcesPath + "/" + _countryAttributeFile);
			if (ta==null) return;
			SetCountriesXMLAttributes(ta.text);
		}

		/// <summary>
		/// Computes surfaces for big countries
		/// </summary>
		void CountriesPrewarmBigSurfaces() {
			for (int k=0;k<countries.Length;k++) {
				int points = countries[k].regions[countries[k].mainRegionIndex].points.Length;
				if (points>6000) {
					ToggleCountrySurface(k, true, Misc.ColorClear);
					ToggleCountrySurface(k, false, Misc.ColorClear);
				}
			}
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of countries array
		/// </summary>
		public void RefreshCountryDefinition (int countryIndex, List<Region>filterRegions)
		{
			lastCountryLookupCount = -1;
			if (countryIndex >= 0 && countryIndex < countries.Length) {
				float maxVol = 0;
				Country country = countries [countryIndex];
				int regionCount = country.regions.Count;
				Vector2 minCountry = Misc.Vector2one * 10;
				Vector2 maxCountry = -minCountry;
				for (int r=0; r<regionCount; r++) {
					Region countryRegion = country.regions [r];
					countryRegion.entity = country;	// just in case one country has been deleted
					countryRegion.regionIndex = r;				// just in case a region has been deleted
					int coorCount = countryRegion.points.Length;
					Vector2 min = Misc.Vector2one * 10;
					Vector2 max = -min;
					for (int c=0; c<coorCount; c++) {
						float x = countryRegion.points [c].x;
						float y = countryRegion.points [c].y;
						if (x < min.x)
							min.x = x;
						if (x > max.x)
							max.x = x;
						if (y < min.y)
							min.y = y;
						if (y > max.y)
							max.y = y;
					}
					Vector3 normRegionCenter = (min + max) * 0.5f;
					countryRegion.center = normRegionCenter; 

					// Calculate country bounding rect
					if (min.x<minCountry.x) minCountry.x = min.x;
					if (min.y<minCountry.y) minCountry.y = min.y;
					if (max.x>maxCountry.x) maxCountry.x = max.x;
					if (max.y>maxCountry.y) maxCountry.y = max.y;

					// Calculate bounding rect
					countryRegion.rect2D = new Rect (min.x, min.y,  Math.Abs (max.x - min.x), Mathf.Abs (max.y - min.y));
					float vol = (max - min).sqrMagnitude;
					if (vol > maxVol) {
						maxVol = vol;
						country.mainRegionIndex = r;
						country.center = countryRegion.center;
					}
				}
				country.regionsRect2D = new Rect (minCountry.x, minCountry.y,  Math.Abs (maxCountry.x - minCountry.x), Mathf.Abs (maxCountry.y - minCountry.y));
			}
			OptimizeFrontiers (filterRegions);
			DrawFrontiers ();
		}

		/// <summary>
		/// Prepare and cache meshes for frontiers. Used internally by extra components (decorator). This is called just after loading data or when hidding a country.
		/// </summary>
		public void OptimizeFrontiers () {
			OptimizeFrontiers(null);
		}

		void OptimizeFrontiers (List<Region>filterRegions) {
			if (frontiersPoints==null) {
				frontiersPoints = new List<Vector3>(1000000); // needed for high-def resolution map
			} else {
				frontiersPoints.Clear();
			}
			if (frontiersCacheHit==null) {
				frontiersCacheHit = new Dictionary<double, Region>(500000); // needed for high-resolution map
			} else {
				frontiersCacheHit.Clear();
			}

			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				for (int r=0; r<country.regions.Count; r++) {
					Region region = country.regions [r];
					if (filterRegions==null || filterRegions.Contains(region)) {
						region.entity = country;
						region.regionIndex = r;
						region.neighbours.Clear();
					}
				}
			}

			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				if (country.hidden) continue;
				for (int r=0; r<country.regions.Count; r++) {
					Region region = country.regions [r];
					if (filterRegions==null || filterRegions.Contains(region)) {
					int numPoints = region.points.Length - 1;
					for (int i = 0; i<numPoints; i++) {
						double v = (region.points [i].x + region.points [i + 1].x) + MAP_PRECISION * (region.points [i].y + region.points [i + 1].y);
						if (frontiersCacheHit.ContainsKey(v)) { // add neighbour references
							Region neighbour = frontiersCacheHit[v];
							if (neighbour!=region) {
								if (!region.neighbours.Contains(neighbour)) {
									region.neighbours.Add(neighbour);
									neighbour.neighbours.Add (region);
								}
							}
						} else {
							frontiersCacheHit.Add(v,region);
							frontiersPoints.Add (region.points [i]);
							frontiersPoints.Add (region.points [i + 1]);
						}
					}
					// Close the polygon
					frontiersPoints.Add (region.points [numPoints]);
					frontiersPoints.Add (region.points [0]);
				}
				}
			}

			int meshGroups = (frontiersPoints.Count / 65000) + 1;
			int meshIndex = -1;
			frontiersIndices = new int[meshGroups][];
			frontiers = new Vector3[meshGroups][];
			for (int k=0; k<frontiersPoints.Count; k+=65000) {
				int max = Mathf.Min (frontiersPoints.Count - k, 65000); 
				frontiers [++meshIndex] = new Vector3[max];
				frontiersIndices [meshIndex] = new int[max];
				for (int j=k; j<k+max; j++) {
					frontiers [meshIndex] [j - k] = frontiersPoints [j];
					frontiersIndices [meshIndex] [j - k] = j - k;
				}
			}
		}

	#region Drawing stuff

		int GetCacheIndexForCountryRegion (int countryIndex, int regionIndex) {
			if (highlightAllCountryRegions) regionIndex = 9999;
			return countryIndex * 1000 + regionIndex;
		}


		void DrawFrontiers () {

			if (!gameObject.activeInHierarchy)
				return;
			if (!_showFrontiers) return;

			// Create frontiers layer
			Transform t = transform.Find ("Frontiers");
			if (t != null) {
				DestroyImmediate (t.gameObject);
			}

			if (frontiers==null) {
				OptimizeFrontiers();	// lazy optimization
			}

			frontiersLayer = new GameObject ("Frontiers");
			frontiersLayer.hideFlags = HideFlags.DontSave;
			frontiersLayer.transform.SetParent (transform, false);
			frontiersLayer.transform.localPosition = Misc.Vector3zero;
			frontiersLayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			frontiersLayer.layer = gameObject.layer;

			for (int k=0; k<frontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				flayer.transform.SetParent (frontiersLayer.transform, false);
				flayer.transform.localPosition = Misc.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
				flayer.layer = frontiersLayer.layer;

				Mesh mesh = new Mesh ();
				mesh.vertices = frontiers [k];
				mesh.SetIndices (frontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;

				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;

				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.useLightProbes = false;
				mr.sharedMaterial = frontiersMat;
			}

			// Toggle frontiers visibility layer according to settings
			frontiersLayer.SetActive (_showFrontiers);
		}

	#endregion

	#region Country highlighting

		void HideCountryRegionHighlight () {
			HideProvinceRegionHighlight();
			HideCityHighlight();
			if (_countryRegionHighlighted==null)
				return;
			if (countryRegionHighlightedObj != null) {
				if (_countryRegionHighlighted!=null && _countryRegionHighlighted.customMaterial!=null) {
					ApplyMaterialToSurface (countryRegionHighlightedObj, _countryRegionHighlighted.customMaterial);
				} else {
					countryRegionHighlightedObj.SetActive (false);
				}
				countryRegionHighlightedObj = null;
			}

			// Raise exit event
			if (OnCountryExit!=null && _countryHighlightedIndex>=0) OnCountryExit(_countryHighlightedIndex, _countryRegionHighlightedIndex);

			_countryHighlighted = null;
			_countryHighlightedIndex = -1;
			_countryRegionHighlighted = null;
			_countryRegionHighlightedIndex = -1;
		}

		/// <summary>
		/// Disables all country regions highlights. This doesn't remove custom materials.
		/// </summary>
		public void HideCountryRegionHighlights (bool destroyCachedSurfaces) {
			HideCountryRegionHighlight();
			if (countries==null) return;
			for (int c=0;c<countries.Length;c++) {
				Country country = countries[c];
				for (int cr=0;cr<country.regions.Count;cr++) {
					Region region = country.regions[cr];
					int cacheIndex = GetCacheIndexForCountryRegion(c, cr);
					if (surfaces.ContainsKey(cacheIndex)) {
						GameObject surf = surfaces[cacheIndex];
						if (surf==null) {
							surfaces.Remove(cacheIndex);
						} else {
							if (destroyCachedSurfaces) {
								surfaces.Remove(cacheIndex);
								DestroyImmediate(surf);
							} else {
								if (region.customMaterial==null) {
									surf.SetActive(false);
								} else {
									ApplyMaterialToSurface (surf, region.customMaterial);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Highlights the country region specified. Returns the generated highlight surface gameObject.
		/// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a country region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		public GameObject HighlightCountryRegion (int countryIndex, int regionIndex, bool refreshGeometry, bool drawOutline)
		{
			if (_countryHighlightedIndex == countryIndex && _countryRegionHighlightedIndex == regionIndex && !refreshGeometry)
				return countryRegionHighlightedObj;
			if (countryRegionHighlightedObj != null)
				HideCountryRegionHighlight ();
			if (countryIndex < 0 || countryIndex >= countries.Length || regionIndex < 0 || regionIndex >= countries [countryIndex].regions.Count)
				return null;

			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = surfaces [cacheIndex];
				surfaces.Remove (cacheIndex);
				DestroyImmediate (obj);
				existsInCache = false;
			}

			if (_enableCountryHighlight) {

				if (existsInCache) {
					countryRegionHighlightedObj = surfaces [cacheIndex];
					if (countryRegionHighlightedObj == null) {
						surfaces.Remove (cacheIndex);
					} else {
						if (!countryRegionHighlightedObj.activeSelf)
							countryRegionHighlightedObj.SetActive (true);
						Renderer[] rr = countryRegionHighlightedObj.GetComponentsInChildren<Renderer> (true);
						for (int k=0;k<rr.Length;k++) {
							if (rr[k].sharedMaterial!=hudMatCountry && rr[k].sharedMaterial!=outlineMat)
								rr[k].sharedMaterial = hudMatCountry;
						}
					}
				} else {
					countryRegionHighlightedObj = GenerateCountryRegionSurface (countryIndex, regionIndex, hudMatCountry, Misc.Vector2one, Misc.Vector2zero, 0, drawOutline);
					// Add rest of regions?
					if (_highlightAllCountryRegions) {
						Country country = countries[countryIndex];
						for (int r=0;r<country.regions.Count;r++) {
							if (r!=regionIndex) {
								Region otherRegion = country.regions[r];
								// Triangulate to get the polygon vertex indices
//								int[] otherSurfIndices = Triangulator.GetPoints (otherRegion.points);
//								GameObject otherSurf = Drawing.CreateSurface (countryRegionHighlightedObj.name, otherRegion.points, otherSurfIndices, hudMatCountry);									
								Polygon poly = new Polygon(otherRegion.points);
								P2T.Triangulate(poly);
								GameObject otherSurf = Drawing.CreateSurface (countryRegionHighlightedObj.name, poly, hudMatCountry, otherRegion.rect2D, Misc.Vector2zero, Misc.Vector2zero, 0);									
								otherSurf.transform.SetParent (countryRegionHighlightedObj.transform, false);
								otherSurf.transform.localPosition = Misc.Vector3zero;
								otherSurf.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
								otherSurf.layer = gameObject.layer;
								if (drawOutline) {
									DrawCountryRegionOutline(otherRegion, otherSurf);
								}
							}
						}
					}
				}
			}

			_countryHighlightedIndex = countryIndex;
			_countryRegionHighlighted = countries [countryIndex].regions [regionIndex];
			_countryRegionHighlightedIndex = regionIndex;
			_countryHighlighted = countries [countryIndex];
			return countryRegionHighlightedObj;
		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, bool drawOutline) {
			return GenerateCountryRegionSurface(countryIndex, regionIndex, material, Misc.Vector2one, Misc.Vector2zero, 0, drawOutline);
		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool drawOutline) {
			if (countryIndex<0 || countryIndex>=countries.Length) return null;
			Country country = countries[countryIndex];
			Region region = country.regions [regionIndex];
			if (region.points.Length<3) {
				return null;
			}

//			// Triangulate to get the polygon vertex indices
//			int[] surfIndices = Triangulator.GetPoints (region.points);

			// Triangulate to get the polygon vertex indices
			//			int[] surfIndices = Triangulator.GetPoints (region.points);
			Polygon poly = new Polygon(region.points);
			P2T.Triangulate(poly);
			//			int numPoints = poly.Triangles.Count*3;
			//			Vector3[] revisedSurfPoints = new Vector3[numPoints];
			//			for (int k=0;k<poly.Triangles.Count;k++) {
			//				DelaunayTriangle dt = poly.Triangles[k];
			//				revisedSurfPoints[k*3] = new Vector3(dt.Points[0].Xf, dt.Points[0].Yf, 0);
			//				revisedSurfPoints[k*3+2] = new Vector3(dt.Points[1].Xf, dt.Points[1].Yf, 0);
			//				revisedSurfPoints[k*3+1] = new Vector3(dt.Points[2].Xf, dt.Points[2].Yf, 0);
			//			}

			// Prepare surface cache entry and deletes older surface if exists
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			string cacheIndexSTR = cacheIndex.ToString();
			Transform t = surfacesLayer.transform.Find(cacheIndexSTR);
			if (t!=null) DestroyImmediate(t.gameObject); // Deletes potential residual surface
			
			// Creates surface mesh
			//			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, region.points, surfIndices, material, region.rect2D, textureScale, textureOffset, textureRotation);									
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, poly, material, region.rect2D, textureScale, textureOffset, textureRotation);			
			surf.transform.SetParent (surfacesLayer.transform, false);
			surf.transform.localPosition = Misc.Vector3zero;
			surf.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			surf.layer = gameObject.layer;
			if (surfaces.ContainsKey(cacheIndex)) surfaces.Remove(cacheIndex);
			surfaces.Add (cacheIndex, surf);

			// Draw polygon outline
			if (drawOutline) {
				DrawCountryRegionOutline(region, surf);
			}
			return surf;
		}

		GameObject DrawCountryRegionOutline(Region region, GameObject surf) {
			if (surf==null) return null;
			int[] indices = new int[region.points.Length+1];
			for (int k=0; k<indices.Length; k++) {
				indices [k] = k;
			}
			indices[indices.Length-1] = 0; 
			Transform t = surf.transform.Find(COUNTRY_OUTLINE_GAMEOBJECT_NAME);
			if (t!=null) DestroyImmediate(t.gameObject);
			GameObject boldFrontiers = new GameObject (COUNTRY_OUTLINE_GAMEOBJECT_NAME);
			boldFrontiers.hideFlags = HideFlags.DontSave;
			boldFrontiers.transform.SetParent (surf.transform, false);
			boldFrontiers.transform.localPosition = Misc.Vector3zero;
			boldFrontiers.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			boldFrontiers.layer = surf.layer;
			
			Mesh mesh = new Mesh ();
			Vector3[] points = new Vector3[region.points.Length];
			for (int k=0;k<region.points.Length;k++) points[k] = region.points[k];
			mesh.vertices = points; 
			mesh.SetIndices (indices, MeshTopology.LineStrip, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = boldFrontiers.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = boldFrontiers.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = outlineMat;

			return boldFrontiers;
		}


	#endregion

	}

}