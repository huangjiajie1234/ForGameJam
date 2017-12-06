using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit;
using PolygonClipping;

namespace WorldMapStrategyKit_Editor {

	public partial class WMSK_Editor : MonoBehaviour {

		public int GUICountryIndex;
		public string GUICountryName = "";
		public string GUICountryNewName = "";
		public string GUICountryNewContinent = "";
		public int GUICountryTransferToCountryIndex = -1;
		public bool groupByParentAdmin = true;
		public int countryIndex = -1, countryRegionIndex = -1;
		public bool countryChanges; // if there's any pending change to be saved
		public bool countryAttribChanges; // if there's any pending change to be saved

		[SerializeField]
		bool _GUICountryHidden;
		
		public bool GUICountryHidden { get {
				return _GUICountryHidden;
			}
			set {
				if (_GUICountryHidden!=value) {
					_GUICountryHidden = value;
					countryChanges = true;
					if (countryIndex>=0 && _map.countries[countryIndex].hidden!=_GUICountryHidden) {
						_map.countries[countryIndex].hidden = _GUICountryHidden;
						ClearSelection();
						_map.OptimizeFrontiers();
						_map.Redraw();
					}
				}
			}
		}
		// private fields
		int lastCountryCount = -1;
		string[] _countryNames;
				    

		public string[] countryNames {
			get {
				if (map.countries!=null && lastCountryCount != map.countries.Length) {
					countryIndex =-1;
					ReloadCountryNames ();
				}
				return _countryNames;
			}
		}

		
		#region Editor functionality


		public bool CountryRename () {
			if (countryIndex<0) return false;
			string prevName = map.countries[countryIndex].name;
			GUICountryNewName = GUICountryNewName.Trim ();
			if (prevName.Equals(GUICountryNewName)) return false;
			if (map.CountryRename(prevName, GUICountryNewName)) {
				GUICountryName = GUICountryNewName;
				lastCountryCount = -1;
				ReloadCountryNames();
				map.RedrawMapLabels();
				countryChanges = true;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Updates all countries within same continent to new country name
		/// </summary>
		public bool ContinentRename() {
			
			if (countryIndex<0) return false;
			
			string currentContinent = map.countries[countryIndex].continent;
			for (int k=0;k<map.countries.Length;k++) {
				if (map.countries[k].continent.Equals(currentContinent))
					map.countries[k].continent = GUICountryNewContinent;
			}
			countryChanges = true;
			return true;
		}

		public void CountrySanitize() {
			if (countryIndex<0 || countryIndex>=_map.countries.Length) return;
			
			Country country = _map.countries[countryIndex];
			for (int k=0;k<country.regions.Count;k++) {
				Region region = country.regions[k];
				RegionSanitize(region);
			}
			_map.RefreshCountryDefinition(countryIndex, null);
			countryChanges = true;
		}


		public void CountrySelectByCombo (int selection) {
			GUICountryName = "";
			GUICountryIndex = selection;
			if (GetCountryIndexByGUISelection()) {
				if (Application.isPlaying) {
					map.BlinkCountry (countryIndex, Color.black, Color.green, 1.2f, 0.2f);
				}
			}
			CountryRegionSelect ();
		}

		bool GetCountryIndexByGUISelection() {
			if (GUICountryIndex<0 || GUICountryIndex>=countryNames.Length) return false;
			string[] s = countryNames [GUICountryIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				GUICountryName = s [0].Trim ();
				if (int.TryParse (s [1], out countryIndex)) {
					countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
					return true;
				}
			}
			return false;
		}

		public void CountryRegionSelect() {
			if (countryIndex < 0 || countryIndex > map.countries.Length)
				return;

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUICountryName = map.countries[countryIndex].name;
			SyncGUICountrySelection();
			GUICountryNewName = map.countries[countryIndex].name;
			GUICountryNewContinent = map.countries[countryIndex].continent;
			if (editingMode == EDITING_MODE.COUNTRIES)
				CountryHighlightSelection();
			else if (editingMode == EDITING_MODE.PROVINCES) {
				map.HighlightCountryRegion (countryIndex, countryRegionIndex, false, true);
				map.DrawProvinces(countryIndex, false, false);
			}
			lastMountPointCount = -1;
			ClearCitySelection();
			lastCityCount = -1;
			ReloadCityNames();
		}

		public bool CountrySelectByScreenClick(Ray ray) {
			int targetCountryIndex, targetRegionIndex;
			if (map.GetCountryIndex (ray, out targetCountryIndex, out targetRegionIndex)) {
				countryIndex = targetCountryIndex;
				countryRegionIndex = targetRegionIndex;
				CountryRegionSelect();
				return true;
			}
			return false;
		}

		void CountryHighlightSelection() {
			CountryHighlightSelection(null);
		}

		void CountryHighlightSelection(List <Region>filterRegions) {

			if (highlightedRegions==null) highlightedRegions = new List<Region>(); else highlightedRegions.Clear();
			if (countryIndex<0 || countryIndex>=map.countries.Length) return;
			if (countryRegionIndex>=map.countries[countryIndex].regions.Count) countryRegionIndex = map.countries[countryIndex].mainRegionIndex;

			// Colorize neighours
			Color color = new Color(1, 1, 1, 0.4f);
			map.HideCountryRegionHighlights(true);
			Region region = map.countries[countryIndex].regions[countryRegionIndex];
			for (int cr=0;cr<region.neighbours.Count;cr++) {
				Region neighbourRegion = region.neighbours[cr];
				if (filterRegions==null || filterRegions.Contains(neighbourRegion)) {
					int c = map.GetCountryIndex((Country)neighbourRegion.entity);
					map.ToggleCountryRegionSurfaceHighlight(c, neighbourRegion.regionIndex, color, true);
					highlightedRegions.Add (neighbourRegion.entity.regions[neighbourRegion.regionIndex]);
				}
			}
			// Highlights region in edit mode (very slow so commented for now)
//			for (int k=0;k<map.countries[countryIndex].regions.Count;k++) {
//				if (k==countryRegionIndex) {
//		    		map.HighlightCountryRegion (countryIndex, k, false, true);
//				} else {
//					map.ToggleCountryRegionSurfaceHighlight(countryIndex, k, map.fillColor, true);
//				}
//			}
			map.HighlightCountryRegion (countryIndex, countryRegionIndex, false, true);
			highlightedRegions.Add (region);

			shouldHideEditorMesh = true;
	    }

		
		public void ReloadCountryNames () {
			if (map == null || map.countries == null) {
				lastCountryCount = -1;
				return;
			}
			lastCountryCount = map.countries.Length; // check this size, and not result from GetCountryNames
			_countryNames = map.GetCountryNames (groupByParentAdmin);
			lastProvinceCount = -1;
			lastMountPointCount = -1;
			lastCityCount = -1;
			SyncGUICountrySelection();
			CountryRegionSelect(); // refresh selection
		}

		void SyncGUICountrySelection() {
			// recover GUI country index selection
			if (GUICountryName.Length>0) {
				for (int k=0; k<_countryNames.Length; k++) {  // don't use countryNames or the array will be reloaded again if grouped option is enabled causing an infinite loop
					if (_countryNames [k].TrimStart ().StartsWith (GUICountryName)) {
						GUICountryIndex = k;
						countryIndex = map.GetCountryIndex(GUICountryName);
						return;
					}
				}
				SetInfoMsg("Country " + GUICountryName + " not found in this geodata file.");
			}
			GUICountryIndex = -1;
			GUICountryName = "";
			lastMountPointCount = -1;
		}

		
		/// <summary>
		/// Deletes current region of country but not any of its dependencies
		/// </summary>
		public void CountryRegionDelete() {
			if (countryIndex<0 || countryIndex>=map.countries.Length) return;
			map.HideCountryRegionHighlights(true);
			
			if (map.countries[countryIndex].regions.Count>1) {
				map.countries[countryIndex].regions.RemoveAt(countryRegionIndex);
				map.RefreshCountryDefinition(countryIndex, null);
			}
			ClearSelection();
			RedrawFrontiers();
			map.RedrawMapLabels();
			countryChanges = true;
		}



		/// <summary>
		/// Deletes current country.
		/// </summary>
		public void CountryDelete() {
			if (countryIndex<0 || countryIndex>=map.countries.Length) return;
				map.HideCountryRegionHighlights(true);

				mDeleteCountryProvinces();
				DeleteCountryCities();
				DeleteCountryMountPoints();
				List<Country> newAdmins = new List<Country>(map.countries.Length-1);
				for (int k=0;k<map.countries.Length;k++) {
					if (k!=countryIndex) {
						newAdmins.Add (map.countries[k]);
					}
				}
				map.countries = newAdmins.ToArray();
				// Updates country index in provinces
				for (int k=0;k<map.provinces.Length;k++) {
					if (map.provinces[k].countryIndex>countryIndex) {
						map.provinces[k].countryIndex--;
					}
				}
				// Updates country index in cities
				for (int k=0;k<map.cities.Count;k++) {
					if (map.cities[k].countryIndex >countryIndex) {
						map.cities[k].countryIndex--;
					}
				}
				// Updates country index in mount points
				if (map.mountPoints!=null) {
					for (int k=0;k<map.mountPoints.Count;k++) {
						if (map.mountPoints[k].countryIndex >countryIndex) {
							map.mountPoints[k].countryIndex--;
						}
					}
				}

			ClearSelection();
			RedrawFrontiers();
			map.RedrawMapLabels();
			countryChanges = true;
		}

		public void CountryDeleteSameContinent() {
			if (countryIndex<0 || countryIndex>=map.countries.Length) return;

			string continent = map.countries[countryIndex].continent;
			map.CountriesDeleteFromContinent(continent);

			ClearSelection();
			RedrawFrontiers();
			map.RedrawMapLabels();
			countryChanges = true;

			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;

			SyncGUIProvinceSelection();
			provinceChanges = true;

			GUIMountPointName = "";
			SyncGUIMountPointSelection();
			map.DrawMountPoints();
			mountPointChanges = true;

		}


		/// <summary>
		/// Makes one country to annex another
		/// </summary>
		public void CountryTransferTo() {
			if (countryIndex<0 || GUICountryTransferToCountryIndex<0 || GUICountryTransferToCountryIndex>=countryNames.Length) return;
			
			// Get target country
			// recover GUI country index selection
			int targetCountryIndex = -1;
			string[] s = countryNames [GUICountryTransferToCountryIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				if (!int.TryParse (s [1], out targetCountryIndex)) {
					return;
				}
			}
			// Transfer all provinces records to target country
			Country sourceCountry = map.countries[countryIndex];
			Country targetCountry = map.countries[targetCountryIndex];
			if (targetCountry.provinces==null && !map.showProvinces) {
				map.showProvinces = true; // Forces loading of provinces
				map.showProvinces = false;
			}
			List<Province>destProvinces = new List<Province>(targetCountry.provinces);
			for (int k=0;k<sourceCountry.provinces.Length;k++) {
				Province province =sourceCountry.provinces[k];
				province.countryIndex = targetCountryIndex;
				destProvinces.Add (province);
			}
			targetCountry.provinces = destProvinces.ToArray();

			// Add main region of the source country to target if they are joint
			Region sourceRegion = sourceCountry.regions[sourceCountry.mainRegionIndex];
			Region targetRegion = targetCountry.regions[targetCountry.mainRegionIndex];
			
			// Add region to target country's polygon - only if the province is touching or crossing target country frontier
			PolygonClipper pc = new PolygonClipper(targetRegion, sourceRegion);
			if (pc.OverlapsSubjectAndClipping()) {
				pc.Compute(PolygonOp.UNION);
			} else {
				// Add new region to country
				Region newCountryRegion = new Region(targetCountry, targetCountry.regions.Count);
				newCountryRegion.points = new List<Vector2>(sourceRegion.points).ToArray();
				targetCountry.regions.Add (newCountryRegion);
			}

			// Transfer additional regions
			if (sourceCountry.regions.Count>1) {
				List<Region> targetRegions = new List<Region>(targetCountry.regions);
				for (int k=0;k<sourceCountry.regions.Count;k++) {
					if (k!=sourceCountry.mainRegionIndex) {
						targetRegions.Add (sourceCountry.regions[k]);
					}
				}
				targetCountry.regions = targetRegions;
			}

			// Finish operation
			map.HideCountryRegionHighlights(true);
			map.HideProvinceRegionHighlights(true);
			map.CountryDelete(countryIndex, false);
			map.RefreshCountryDefinition(targetCountryIndex, null);
			countryChanges = true;
			provinceChanges = true;
			countryIndex = targetCountryIndex;
			countryRegionIndex = targetCountry.mainRegionIndex;
			CountryRegionSelect();
			map.RedrawMapLabels();
		}

	
		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current country data file (countries10, countries110)
		/// </summary>
		public string GetCountryGeoDataFileName() {
			return map.frontiersDetail == FRONTIERS_DETAIL.Low ? "countries110.txt" : "countries10.txt";
		}
		
		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetCountryGeoData () {
			StringBuilder sb = new StringBuilder ();
			for (int k=0; k<map.countries.Length; k++) {
				Country country = map.countries [k];
				if (k > 0)
					sb.Append ("|");
				sb.Append (country.name);
				sb.Append("$");
				sb.Append (country.continent);
				sb.Append("$");
				for (int r = 0; r<country.regions.Count; r++) {
					if (r > 0)
						sb.Append ("*");
					Region region = country.regions [r];
					for (int p=0; p<region.points.Length; p++) {
						if (p > 0)
							sb.Append (";");
						Vector2 point = region.points [p] * WMSK.MAP_PRECISION;
						sb.Append (point.x.ToString ());
						sb.Append(",");
						sb.Append (point.y.ToString ());
					}
				}
				sb.Append("$");
				sb.Append ((country.hidden ? "1" : "0") );
				sb.Append("$");
				sb.Append (country.uniqueId.ToString() );
			}
			return sb.ToString ();
		}
		
		
		/// <summary>
		/// Exports the geographic data in packed string format with reduced quality.
		/// </summary>
		public string GetCountryGeoDataLowQuality () {
			// step 1: duplicate data
			IAdminEntity[] entities;
			if (editingMode == EDITING_MODE.COUNTRIES) entities = map.countries; else entities = map.provinces;
			List<IAdminEntity> entities1 = new List<IAdminEntity>(entities);
			
			for (int k=0;k<entities1.Count;k++) {
				entities1[k].regions = new List<Region>(entities1[k].regions);
				for (int r=0;r<entities[k].regions.Count;r++) {
					entities1[k].regions[r].points = new List<Vector2>(entities1[k].regions[r].points).ToArray();
				}
			}
			// step 2: ensure near points between neighbours
			float MAX_DIST = 0.00000001f;
			//			int join = 0;
			for (int k=0;k<entities1.Count;k++) {
				for (int r=0;r<entities1[k].regions.Count;r++) {
					Region region1 = entities1[k].regions[r];
					for (int p=0;p<entities1[k].regions[r].points.Length;p++) {
						// Search near points
						for (int k2=0;k2<region1.neighbours.Count;k2++) {
							for (int r2=0;r2<entities1[k2].regions.Count;r2++) {
								Region region2 = entities1[k2].regions[r2];
								for (int p2=0;p2<entities1[k2].regions[r2].points.Length;p2++) {
									float dist = (region1.points[p].x-region2.points[p2].x)*(region1.points[p].x-region2.points[p2].x)+
										(region1.points[p].y-region2.points[p2].y)*(region1.points[p].y-region2.points[p2].y);
									if (dist<MAX_DIST) {
										region2.points[p2] = region1.points[p];
										//										join++;
									}
								}
							}
						}
					}
				}
			}
			//			if (join>0)
			//				Debug.Log (join + " points fused.");
			
			
			// step 2: simplify
			Dictionary<Vector2, bool> frontiersHit = new Dictionary<Vector2, bool>();
			List<IAdminEntity> entities2 = new List<IAdminEntity>(entities1.Count);
			int savings = 0, totalPoints = 0;
			float FACTOR = 1000f;
			for (int k=0; k<entities1.Count; k++) {
				IAdminEntity refEntity = entities1 [k];
				IAdminEntity newEntity;
				if (refEntity is Country) {
					newEntity = new Country(refEntity.name, ((Country)refEntity).continent, map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
				} else {
					newEntity = new Province(refEntity.name, ((Province)refEntity).countryIndex, map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				}
				for (int r=0; r<refEntity.regions.Count; r++) {
					Region region = refEntity.regions [r];
					int numPoints = region.points.Length;
					totalPoints+=numPoints;
					List<Vector2> points = new List<Vector2>(numPoints);
					frontiersHit.Clear();
					
					Vector3[] blockyPoints = new Vector3[numPoints];
					for (int p = 0; p<numPoints; p++) 
						blockyPoints[p] = new Vector2(Mathf.RoundToInt(region.points[p].x * FACTOR) / FACTOR, Mathf.RoundToInt(region.points[p].y * FACTOR) / FACTOR);
					
					points.Add (region.points[0] * WMSK.MAP_PRECISION);
					for (int p = 1; p<numPoints-1; p++) {
						if (blockyPoints[p-1].y == blockyPoints[p].y && blockyPoints[p].y == blockyPoints[p+1].y ||
						    blockyPoints[p-1].x == blockyPoints[p].x && blockyPoints[p].x == blockyPoints[p+1].x) {
							savings++;
							continue;
						}
						if (!frontiersHit.ContainsKey(blockyPoints[p])) { // add neighbour references
							frontiersHit.Add(blockyPoints[p],true);
							points.Add (region.points [p] * WMSK.MAP_PRECISION);
						} else {
							savings++;
						}
					}
					points.Add (region.points[numPoints-1]*  WMSK.MAP_PRECISION);
					if (points.Count>=5) {
						Region newRegion = new Region(newEntity, newEntity.regions.Count);
						newRegion.points = points.ToArray();
						newEntity.regions.Add (newRegion);
					}
				}
				if (newEntity.regions.Count>0) 
					entities2.Add (newEntity);
			}
			
			Debug.Log (savings + " points removed of " + totalPoints + " (" + (((float)savings/totalPoints)*100.0f).ToString("F1") + "%)");
			
			StringBuilder sb = new StringBuilder ();
			for (int k=0; k<entities2.Count; k++) {
				IAdminEntity entity = entities2[k];
				if (k > 0)
					sb.Append ("|");
				sb.Append (entity.name + "$");
				if (entity is Country) {
					sb.Append ( ((Country)entity).continent + "$");
				} else {
					sb.Append ( map.countries[ ((Province)entity).countryIndex].name + "$");
				}
				for (int r = 0; r<entity.regions.Count; r++) {
					if (r > 0)
						sb.Append ("*");
					Region region = entity.regions [r];
					for (int p=0; p<region.points.Length; p++) {
						if (p > 0)
							sb.Append (";");
						Vector2 point = region.points [p];
						sb.Append (point.x.ToString () + ",");
						sb.Append (point.y.ToString ());
					}
				}
			}
			return sb.ToString ();
		}
		

		int GetNearestCountryToShape() {
			int countryIndex = -1;
			float minDist = float.MaxValue;
			Vector2 p = newShape[0];
			for (int k=0;k<map.countries.Length;k++) {
				float dist = (p - map.countries[k].center).sqrMagnitude;
				if (dist<minDist) {
					minDist = dist;
					countryIndex = k;
				}
			}
			return countryIndex;
		}

		/// <summary>
		/// Creates a new country with the current shape
		/// </summary>
		public void CountryCreate() {
			if (newShape.Count<3) return;
			int nearestCountry = GetNearestCountryToShape();
			if (nearestCountry<0) return;

			string continent = map.countries[nearestCountry].continent;
			countryIndex = map.countries.Length;
			countryRegionIndex = 0;
			Country newCountry = new Country("New Country" + (countryIndex+1).ToString(), continent, map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			Region region = new Region(newCountry, 0);
			region.points = newShape.ToArray();
			newCountry.regions.Add (region);
			map.CountryAdd(newCountry);
			map.RefreshCountryDefinition(countryIndex, null);
			lastCountryCount = -1;
			GUICountryName = "";
			ReloadCountryNames();
			countryChanges = true;
			CountryRegionSelect();
			map.RedrawMapLabels();
		}

		/// <summary>
		/// Adds a new region to current country
		/// </summary>
		public void CountryRegionCreate() {
			if (newShape.Count<3 || countryIndex<0) return;

			Country country = map.countries[countryIndex];
			countryRegionIndex = country.regions.Count;
			Region region = new Region(country, countryRegionIndex);
			region.points = newShape.ToArray();
			country.regions.Add (region);
			map.RefreshCountryDefinition(countryIndex, null);
			countryChanges = true;
			CountryRegionSelect();
		}


		#endregion

	}
}
