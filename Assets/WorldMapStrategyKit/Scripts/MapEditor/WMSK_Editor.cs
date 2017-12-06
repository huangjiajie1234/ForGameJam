using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit;

namespace WorldMapStrategyKit_Editor {

	public enum OPERATION_MODE {
		SELECTION = 0,
		RESHAPE = 1,
		CREATE = 2,
		UNDO = 3,
		CONFIRM = 4
	}

	public enum RESHAPE_REGION_TOOL {
		POINT = 0,
		CIRCLE = 1,
		SPLITV = 2,
		SPLITH = 3,
		MAGNET = 4,
		SMOOTH = 5,
		ERASER = 6,
		DELETE = 7
	}

	public enum RESHAPE_CITY_TOOL {
		MOVE = 0,
		DELETE = 1
	}

	public enum RESHAPE_MOUNT_POINT_TOOL {
		MOVE = 0,
		DELETE = 1
	}

	public enum CREATE_TOOL {
		CITY = 0,
		COUNTRY = 1,
		COUNTRY_REGION = 2,
		PROVINCE = 3,
		PROVINCE_REGION = 4,
		MOUNT_POINT = 5
	}


	public enum EDITING_MODE {
		COUNTRIES,
		PROVINCES
	}

	public enum EDITING_COUNTRY_FILE {
		COUNTRY_HIGHDEF=0,
		COUNTRY_LOWDEF=1
	}

	public static class ReshapeToolExtensons {
		public static bool hasCircle(this RESHAPE_REGION_TOOL r) {
			return r== RESHAPE_REGION_TOOL.CIRCLE || r == RESHAPE_REGION_TOOL.MAGNET || r == RESHAPE_REGION_TOOL.ERASER;
		}
	}

	[RequireComponent(typeof(WMSK))]
	[ExecuteInEditMode]
	public partial class WMSK_Editor : MonoBehaviour {

		public int entityIndex { get { if (editingMode == EDITING_MODE.PROVINCES) return provinceIndex; else return countryIndex; } }
		public int regionIndex { get { if (editingMode == EDITING_MODE.PROVINCES) return provinceRegionIndex; else return countryRegionIndex; } }
		public OPERATION_MODE operationMode;
		public RESHAPE_REGION_TOOL reshapeRegionMode;
		public RESHAPE_CITY_TOOL reshapeCityMode;
		public RESHAPE_MOUNT_POINT_TOOL reshapeMountPointMode;
		public CREATE_TOOL createMode;
		public Vector3 cursor;
		public bool circleMoveConstant, circleCurrentRegionOnly;
		public float reshapeCircleWidth = 0.01f;
		public bool shouldHideEditorMesh;
		public bool magnetAgressiveMode = false;

		public string infoMsg = "";
		public DateTime infoMsgStartTime;
		public EDITING_MODE editingMode;
		public EDITING_COUNTRY_FILE editingCountryFile;

		[NonSerialized]
		public List<Region> highlightedRegions;

		List<List<Region>> _undoRegionsList;
		List<List<Region>> undoRegionsList { get { if (_undoRegionsList==null) _undoRegionsList = new List<List<Region>>(); return _undoRegionsList; } }
		public int undoRegionsDummyFlag;

		List<List<City>> _undoCitiesList;
		List<List<City>> undoCitiesList { get { if (_undoCitiesList==null) _undoCitiesList = new List<List<City>>(); return _undoCitiesList; } }
		public int undoCitiesDummyFlag;

		List<List<MountPoint>> _undoMountPointsList;
		List<List<MountPoint>> undoMountPointsList { get { if (_undoMountPointsList==null) _undoMountPointsList = new List<List<MountPoint>>(); return _undoMountPointsList; } }
		public int undoMountPointsDummyFlag;

		public IAdminEntity[] entities { get {
				if (editingMode == EDITING_MODE.PROVINCES) return map.provinces; else return map.countries;
			}
		}

		public List<Vector2>newShape;


		float[] gaussian = { 0.153170f, 0.144893f, 0.122649f, 0.092902f, 0.062970f };
		WMSK _map;
		
		[SerializeField]
		int lastMinPopulation;
		
		void OnEnable() {
			lastMinPopulation = map.minPopulation;
			map.minPopulation = 0;
		}
		
		void OnDisable() {
			if (_map!=null) {
				if (_map.minPopulation == 0) _map.minPopulation = lastMinPopulation;
			}
		}
		
		#region Editor functionality
		
		
		/// <summary>
		/// Accesor to the World Map Globe core API
		/// </summary>
		public WMSK map { get {
				if (_map==null) _map = GetComponent<WMSK> ();
				return _map; } }

		public void ClearSelection() {
			map.HideCountryRegionHighlights(true);
			highlightedRegions = null;
			countryIndex = -1;
			countryRegionIndex = -1;
			GUICountryName = "";
			GUICountryNewName = "";
			GUICountryIndex = -1;
			ClearProvinceSelection();
			ClearCitySelection();
			ClearMountPointSelection();
		}

		/// <summary>
		/// Removes special characters from string.
		/// </summary>
		string DataEscape(string s) {
			s = s.Replace("$", "");
			s = s.Replace("|", "");
			return s;
		}

		/// <summary>
		/// Redraws all frontiers and highlights current selected regions.
		/// </summary>
		public void RedrawFrontiers() {
			RedrawFrontiers(highlightedRegions, true);
		}

		/// <summary>
		/// Redraws the frontiers and highlights specified regions filtered by provided list of regions. Also highlights current selected country/province.
		/// </summary>
		/// <param name="filterRegions">Regions.</param>
		/// <param name="highlightSelected">Pass false to just redraw borders.</param>
		public void RedrawFrontiers (List <Region> filterRegions, bool highlightSelected) {
			map.RefreshCountryDefinition (countryIndex, filterRegions);
			if (highlightSelected) {
				map.HideProvinces();
				CountryHighlightSelection(filterRegions);
			}
			if (editingMode == EDITING_MODE.PROVINCES) {
				map.RefreshProvinceDefinition (provinceIndex);
				if (highlightSelected) {
					ProvinceHighlightSelection();
				}
			}
		}

		public void DiscardChanges() {
			ClearSelection();
			map.ReloadData();
			map.Redraw();
			RedrawFrontiers();
			cityChanges = false;
			countryChanges = false;
			provinceChanges = false;
		}

		/// <summary>
		/// Moves any point inside circle.
		/// </summary>
		/// <returns>Returns a list with changed regions</returns>
		public List<Region> MoveCircle (Vector3 position, Vector2 dragAmount, float circleSize) {
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return null;

			float circleSizeSqr = circleSize * circleSize;
			List<Region> regions = new List<Region>(100); 
			// Current region
			Region currentRegion = entities [entityIndex].regions [regionIndex];
			regions.Add (currentRegion);
			// Current region's neighbours
			if (!circleCurrentRegionOnly) {
				for (int r=0; r<currentRegion.neighbours.Count; r++) {
					Region region = currentRegion.neighbours[r];
					if (!regions.Contains(region)) regions.Add (region);
				}
				// If we're editing provinces, check if country points can be moved as well
				if (editingMode == EDITING_MODE.PROVINCES) {
					// Moves current country
					for (int cr=0;cr<map.countries[countryIndex].regions.Count;cr++) {
						Region countryRegion = map.countries[countryIndex].regions[cr];
						if (!regions.Contains(countryRegion)) regions.Add (countryRegion);
						// Moves neighbours
						for (int r=0; r<countryRegion.neighbours.Count; r++) {
							Region region = countryRegion.neighbours[r];
							if (!regions.Contains(region)) regions.Add (region);
						}
					}
				}
			}
			// Execute move operation on each point
			List<Region> affectedRegions = new List<Region>(regions.Count);
			for (int r=0;r<regions.Count;r++) {
				Region region = regions[r];
				bool regionAffected = false;
				for (int p=0; p<region.points.Length; p++) {
					Vector2 rp = region.points[p];
					float dist = (rp.x-position.x)*(rp.x-position.x)*4.0f + (rp.y-position.y)* (rp.y-position.y);
					if (dist < circleSizeSqr) {
						if (circleMoveConstant) {
							region.points [p] += dragAmount;
						} else {
							region.points [p] += dragAmount  - dragAmount* (dist / circleSizeSqr);
						}
						regionAffected = true;
					}
				}
				if (regionAffected)
					affectedRegions.Add (region);
			}
			return affectedRegions;
		}


		/// <summary>
		/// Moves a single point.
		/// </summary>
		/// <returns>Returns a list of affected regions</returns>
		public List<Region> MovePoint(Vector3 position, Vector3 dragAmount) {
			return MoveCircle(position, dragAmount, 0.0001f);
		}

		/// <summary>
		/// Moves points of other regions towards current frontier
		/// </summary>
		public bool Magnet (Vector3 position, float circleSize)
		{
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return false;

			Region currentRegion = entities [entityIndex].regions [regionIndex];
			float circleSizeSqr = circleSize * circleSize;

			Dictionary<Vector3, bool>attractorsUse = new Dictionary<Vector3, bool>();
			// Attract points of other regions/countries
			List<Region> regions = new List<Region>();
			for (int c=0; c<entities.Length; c++) {
				IAdminEntity entity = entities [c];
				if (entity.regions==null) continue;
				for (int r=0; r<entity.regions.Count; r++) {
					if (c!=entityIndex || r!=regionIndex) {
						regions.Add (entities[c].regions[r]);
					}
				}
			}
			if (editingMode == EDITING_MODE.PROVINCES) {
				// Also add regions of current country and neighbours
				for (int r=0;r<map.countries[countryIndex].regions.Count;r++) {
					Region region = map.countries[countryIndex].regions[r];
					regions.Add (region);
					for (int n=0;n<region.neighbours.Count;n++) {
						Region nregion = region.neighbours[n];
						if (!regions.Contains(nregion)) regions.Add (nregion);
					}
				}
			}

			bool changes = false;
			Vector3 goodAttractor = Misc.Vector3zero;

			for (int r=0; r<regions.Count; r++) {
				Region region = regions [r];
				bool changesInThisRegion = false;
				for (int p=0; p<region.points.Length; p++) {
					Vector3 rp = region.points [p];
					float dist = (rp.x - position.x) * (rp.x - position.x) * 4.0f + (rp.y - position.y) * (rp.y - position.y);
					if (dist < circleSizeSqr) {
						float minDist = float.MaxValue;
						int nearest = -1;
						for (int a=0; a<currentRegion.points.Length; a++) {
							Vector3 attractor = currentRegion.points [a];
							dist = (rp.x - attractor.x) * (rp.x - attractor.x) * 4.0f + (rp.y - attractor.y) * (rp.y - attractor.y);
							if (dist < circleSizeSqr && dist < minDist) {
								minDist = dist;
								nearest = a;
								goodAttractor = attractor;
							}
						}
						if (nearest >= 0) {
							changes = true;
							// Check if this attractor is being used by other point
							bool used = attractorsUse.ContainsKey (goodAttractor);
							if (!used || magnetAgressiveMode) {
								region.points [p] = goodAttractor;
								if (!used)
									attractorsUse.Add (goodAttractor, true);
								changesInThisRegion = true;
							}
						}
					}
				}
				if (changesInThisRegion) {
					// Remove duplicate points in this region
					Dictionary<Vector2, bool> repeated = new Dictionary<Vector2, bool> ();
					for (int k=0; k<region.points.Length; k++)
						if (!repeated.ContainsKey (region.points [k]))
							repeated.Add (region.points [k], true);
					region.points = new List<Vector2> (repeated.Keys).ToArray ();
				}
			}
			return changes;
		}


		
		/// <summary>
		/// Erase points inside circle.
		/// </summary>
		public bool Erase (Vector3 position, float circleSize) {
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return false;

			if (circleCurrentRegionOnly) {
				Region currentRegion = entities [entityIndex].regions [regionIndex];
				return EraseFromRegion(currentRegion, position, circleSize);
			} else {
				return EraseFromAnyRegion(position, circleSize);
			}
		}
		
		bool EraseFromRegion(Region region, Vector3 position, float circleSize) {

			float circleSizeSqr = circleSize * circleSize;
			
			// Erase points inside the circle
			bool changes = false;
			List<Vector2> temp = new List<Vector2> (region.points.Length);
			for (int p=0; p<region.points.Length; p++) {
				Vector3 rp = region.points [p];
				float dist = (rp.x - position.x) * (rp.x - position.x) * 4.0f + (rp.y - position.y) * (rp.y - position.y);
				if (dist > circleSizeSqr) {
					temp.Add (rp);
				} else {
					changes = true;
				}
			}
			if (changes) {
				Vector2[] newPoints = temp.ToArray();
				if (newPoints.Length>=3) {
					region.points = newPoints;
				} else {
					SetInfoMsg("Minimum of 3 points is required. To remove the region use the DELETE button.");
					return false;
				}
//					// Remove region from entity
//					if (region.entity.regions.Contains(region)) {
//						region.entity.regions.Remove(region);
//					}
//					SetInfoMsg("Region removed from entity!");
//				}
				if (region.entity is Country){
					countryChanges = true;
				} else {
					provinceChanges = true;
				}
			}
			
			return changes;
		}
		
		bool EraseFromAnyRegion(Vector3 position, float circleSize) {
			
			// Try to delete from any region of any country
			bool changes = false;
			for (int c=0;c<_map.countries.Length;c++) {
				Country country = _map.countries[c];
				for (int cr=0;cr<country.regions.Count;cr++) {
					Region region = country.regions[cr];
					if (EraseFromRegion(region, position, circleSize)) changes = true;
				}
			}
			
			// Try to delete from any region of any province
			if (editingMode == EDITING_MODE.PROVINCES && _map.provinces!=null) {
				for (int p=0;p<_map.provinces.Length;p++) {
					Province province = _map.provinces[p];
					for (int pr=0;pr<province.regions.Count;pr++) {
						Region region = province.regions[pr];
						if (EraseFromRegion(region, position, circleSize)) changes = true;
					}
				}
			}
			return changes;
		}


		public void UndoRegionsPush(List<Region> regions) {
			UndoRegionsInsertAtCurrentPos(regions);
			undoRegionsDummyFlag++;
			if (editingMode == EDITING_MODE.COUNTRIES) {
				countryChanges = true;
			} else
				provinceChanges = true;
		}

		public void UndoRegionsInsertAtCurrentPos(List<Region> regions) {
			if (regions==null) return;
			List<Region> clonedRegions = new List<Region>();
			for (int k=0;k<regions.Count;k++) {
				clonedRegions.Add (regions[k].Clone());
			}
			if (undoRegionsDummyFlag>undoRegionsList.Count) undoRegionsDummyFlag = undoRegionsList.Count;
			undoRegionsList.Insert(undoRegionsDummyFlag, clonedRegions);
		}

		public void UndoCitiesPush() {
			UndoCitiesInsertAtCurrentPos();
			undoCitiesDummyFlag++;
		}

		public void UndoCitiesInsertAtCurrentPos() {
			List<City> cities = new List<City>(map.cities.Count);
			for (int k=0;k<map.cities.Count;k++) cities.Add (map.cities[k].Clone());
			if (undoCitiesDummyFlag>undoCitiesList.Count) undoCitiesDummyFlag = undoCitiesList.Count;
			undoCitiesList.Insert(undoCitiesDummyFlag, cities);
		}

		public void UndoMountPointsPush() {
			UndoMountPointsInsertAtCurrentPos();
			undoMountPointsDummyFlag++;
		}
		
		public void UndoMountPointsInsertAtCurrentPos() {
			if (map.mountPoints==null) map.mountPoints = new List<MountPoint>();
			List<MountPoint> mountPoints = new List<MountPoint>(map.mountPoints.Count);
			for (int k=0;k<map.mountPoints.Count;k++) mountPoints.Add (map.mountPoints[k].Clone());
			if (undoMountPointsDummyFlag>undoMountPointsList.Count) undoMountPointsDummyFlag = undoMountPointsList.Count;
			undoMountPointsList.Insert(undoMountPointsDummyFlag, mountPoints);
		}

		public void UndoHandle() {
			if (undoRegionsList!=null && undoRegionsList.Count>=2) {
				if (undoRegionsDummyFlag >= undoRegionsList.Count) {
					undoRegionsDummyFlag = undoRegionsList.Count-2;
				}
				List<Region> savedRegions = undoRegionsList[undoRegionsDummyFlag];
				RestoreRegions(savedRegions);
			}
			if (undoCitiesList!=null && undoCitiesList.Count>=2) {
				if (undoCitiesDummyFlag >= undoCitiesList.Count) {
					undoCitiesDummyFlag = undoCitiesList.Count-2;
				}
				List<City> savedCities = undoCitiesList[undoCitiesDummyFlag];
				RestoreCities(savedCities);
			}
			if (undoMountPointsList!=null && undoMountPointsList.Count>=2) {
				if (undoMountPointsDummyFlag >= undoMountPointsList.Count) {
					undoMountPointsDummyFlag = undoMountPointsList.Count-2;
				}
				List<MountPoint> savedMountPoints = undoMountPointsList[undoMountPointsDummyFlag];
				RestoreMountPoints(savedMountPoints);
			}
		}

		
		void RestoreRegions(List<Region> savedRegions) {
			for (int k=0;k<savedRegions.Count;k++) {
				IAdminEntity entity = savedRegions[k].entity;
				int regionIndex = savedRegions[k].regionIndex;
				entity.regions[regionIndex] = savedRegions[k];
			}
			RedrawFrontiers();
		}

		void RestoreCities(List<City> savedCities) {
			map.cities = savedCities;
			lastCityCount = -1;
			ReloadCityNames();
			map.DrawCities();
		}

		void RestoreMountPoints(List<MountPoint> savedMountPoints) {
			map.mountPoints = savedMountPoints;
			lastMountPointCount = -1;
			ReloadMountPointNames();
			map.DrawMountPoints();
		}

		void EntityAdd (IAdminEntity newEntity) {
			if (newEntity is Country) {
				map.CountryAdd((Country)newEntity);
			} else {
				map.ProvinceAdd((Province)newEntity);
			}
		}
		


		public void SplitHorizontally() {
			if (entityIndex<0 || entityIndex>=entities.Length) return;

			IAdminEntity currentEntity = entities[entityIndex];
			Region currentRegion = currentEntity.regions[regionIndex];
			Vector2 center = currentRegion.center;
			List<Vector2>half1 = new List<Vector2>();
			List<Vector2>half2 = new List<Vector2>();
			int prevSide = 0;
			for (int k=0;k<currentRegion.points.Length;k++) {
				Vector3 p = currentRegion.points[k];
				if (p.y>currentRegion.center.y) {
					half1.Add (p);
					if (prevSide == -1) half2.Add (p);
					prevSide = 1;
				}
				if (p.y<=currentRegion.center.y) {
					half2.Add (p);
					if (prevSide == 1) half1.Add (p);
					prevSide = -1;
				}
			}
			// Setup new entity
			IAdminEntity newEntity;
			if (currentEntity is Country) {
				newEntity = new Country("New " + currentEntity.name, ((Country)currentEntity).continent, map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			} else {
				newEntity = new Province("New " + currentEntity.name, ((Province)currentEntity).countryIndex, map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				newEntity.regions = new List<Region>();
			}
			EntityAdd(newEntity);

			// Update polygons
			Region newRegion = new Region(newEntity, 0);
			if (entities[countryIndex].center.y>center.y) {
				currentRegion.points = half1.ToArray();
				newRegion.points = half2.ToArray();
			} else {
				currentRegion.points = half2.ToArray();
				newRegion.points = half1.ToArray();
			}
			newEntity.regions.Add (newRegion);

			// Refresh old entity and selects the new
			if (currentEntity is Country) {
				map.RefreshCountryDefinition(countryIndex, highlightedRegions);
				countryIndex = map.countries.Length-1;
				countryRegionIndex = 0;
			} else {
				map.RefreshProvinceDefinition(provinceIndex);
				provinceIndex = map.provinces.Length-1;
				provinceRegionIndex = 0;
			}

			// Refresh lines
			highlightedRegions.Add (newRegion);
			RedrawFrontiers();
			map.RedrawMapLabels();
		}

		public void SplitVertically () {
			if (entityIndex<0 || entityIndex>=entities.Length) return;

			IAdminEntity currentEntity = entities[entityIndex];
			Region currentRegion = currentEntity.regions[regionIndex];
			Vector2 center = currentRegion.center;
			List<Vector2>half1 = new List<Vector2>();
			List<Vector2>half2 = new List<Vector2>();
			int prevSide = 0;
			for (int k=0;k<currentRegion.points.Length;k++) {
				Vector3 p = currentRegion.points[k];
				if (p.x>currentRegion.center.x) {
					half1.Add (p);
					if (prevSide == -1) half2.Add (p);
					prevSide = 1;
				}
				if (p.x<=currentRegion.center.x) {
					half2.Add (p);
					if (prevSide == 1) half1.Add (p);
					prevSide = -1;
				}
			}

			// Setup new entity
			IAdminEntity newEntity;
			if (currentEntity is Country) {
				newEntity = new Country("New " + currentEntity.name, ((Country)currentEntity).continent, map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			} else {
				newEntity = new Province("New " + currentEntity.name, ((Province)currentEntity).countryIndex, map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				newEntity.regions = new List<Region>();
			}
			EntityAdd(newEntity);
			
			// Update polygons
			Region newRegion = new Region(newEntity, 0);
			if (entities[countryIndex].center.x>center.x) {
				currentRegion.points = half1.ToArray();
				newRegion.points = half2.ToArray();
			} else {
				currentRegion.points = half2.ToArray();
				newRegion.points = half1.ToArray();
			}
			newEntity.regions.Add (newRegion);
			
			// Refresh old entity and selects the new
			if (currentEntity is Country) {
				map.RefreshCountryDefinition(countryIndex, highlightedRegions);
				countryIndex = map.countries.Length-1;
				countryRegionIndex = 0;
			} else {
				map.RefreshProvinceDefinition(provinceIndex);
				provinceIndex = map.provinces.Length-1;
				provinceRegionIndex = 0;
			}
			
			// Refresh lines
			highlightedRegions.Add (newRegion);
			RedrawFrontiers();
			map.RedrawMapLabels();
		}

	
		/// <summary>
		/// Adds the new point to currently selected region.
		/// </summary>
		public void AddPoint(Vector2 newPoint) {
			if (entities==null || entityIndex<0 || entityIndex>=entities.Length || regionIndex<0 || entities[entityIndex].regions == null || regionIndex>=entities[entityIndex].regions.Count) return;
			List<Region> affectedRegions = new List<Region>();
			Region region =  entities[entityIndex].regions[regionIndex];
			float minDist = float.MaxValue;
			int nearest = -1, previous = -1;
			int max = region.points.Length;
			for (int p=0; p<max; p++) {
				int q = p == 0 ? max-1: p-1;
				Vector3 rp = (region.points [p] + region.points[q]) * 0.5f;
				float dist = (rp.x - newPoint.x) * (rp.x - newPoint.x)*4  + (rp.y - newPoint.y) * (rp.y - newPoint.y);
				if (dist < minDist) {
					// Get nearest point
					minDist = dist;
					nearest = p;
					previous = q;
				}
			}

			if (nearest>=0) {
				Vector2 pointToInsert = (region.points[nearest] + region.points[previous]) * 0.5f;

				// Check if nearest and previous exists in any neighbour
				int nearest2 = -1, previous2 = -1;
				for (int n=0;n<region.neighbours.Count;n++) {
					Region nregion = region.neighbours[n];
					for (int p=0;p<nregion.points.Length;p++) {
						if (nregion.points[p] == region.points[nearest]) {
							nearest2 = p;
						}
						if (nregion.points[p] == region.points[previous]) {
							previous2 = p;
						}
					}
					if (nearest2>=0 && previous2>=0) {
						nregion.points = InsertPoint(nregion.points, previous2, pointToInsert);
						affectedRegions.Add (nregion);
						break;
					}
				}

				// Insert the point in the current region (must be done after inserting in the neighbour so nearest/previous don't unsync)
				region.points= InsertPoint(region.points, nearest, pointToInsert);
				affectedRegions.Add (region);
			} 
		}

		Vector2[] InsertPoint(Vector2[] pointArray, int index, Vector2 pointToInsert) {
			List<Vector2> temp = new List<Vector2>(pointArray.Length+1);
			for (int k=0;k<pointArray.Length;k++) {
				if (k==index) temp.Add (pointToInsert);
				temp.Add (pointArray[k]);
			}
			return temp.ToArray();
		}


		public void SetInfoMsg (string msg) {
			this.infoMsg = msg;
			infoMsgStartTime = DateTime.Now;
		}

		#endregion


		#region Misc options


		public delegate bool MotionVectorProgress(float percentage, string text);
		public delegate void MotionVectorFinished(Texture2D texture, bool cancelled);
		bool cancelled;

		IEnumerator BlurPass(Color[] colors, int width, int height, int incx, int incy, MotionVectorProgress progress, string progressText) {

			for (int y=0;y<height;y++) {
				if (y % 100 == 0) {
					if (progress!=null) {
						if (progress((float) y/height, progressText)) {
							cancelled = true;
							break;
						}
					}
					yield return null;
				}
				for (int x=0;x<width;x++) {
					int pixelPos = y * width + x;
					float denom = gaussian[0];
					float[] sum = new float[] {colors[pixelPos].g * denom, colors[pixelPos].b * denom, colors[pixelPos].a * denom};
					for (int k=1;k<5;k++) {
						int y0 = y + incy * k;
						if (y0>=0 && y0<height) {
							int x0 = x + incx * k;
							if (x0>=0 && x0<width) {
								int pixelPos0 = y0 * width + x0;
								float g = gaussian[k];
								denom += g;
								Color color = colors[pixelPos0];
								sum[0] += color.g * g;
								sum[1] += color.b * g;
								sum[2] += color.a * g;
							}
						}
					}
					colors[pixelPos].g = sum[0] / denom;
					colors[pixelPos].b = sum[1] / denom;
					colors[pixelPos].a = sum[2] / denom;
				}
			}
		}


		/// <summary>
		/// Updates provided texture to reflect water animation vectors
		/// Red channel: elevation
		/// Green channel: water flow motion vector (x)
		/// Blue channel: water flow motion vector (y)
		/// Alpha channel: foam intensity
		/// </summary>
		public void GenerateWaterMotionVectors(Texture2D tex, MotionVectorProgress progress, MotionVectorFinished finish) {
			StartCoroutine(mGenerateWaterMotionVectors(tex, progress, finish));
		}

		IEnumerator mGenerateWaterMotionVectors(Texture2D tex, MotionVectorProgress progress, MotionVectorFinished finish) {
			Color[] colors = tex.GetPixels();
			int colorBufferSize = colors.Length;

			int width = tex.width;
			int height = tex.height;

			// Per pixel, calculate foam intensity and motion vector
			// For motion vector, we calculate a weighted average of vectors surrounding the pixel for a custom sized kernel size
			// For foam intensity, we take the weighted average
			int hks = 2; // Half of kernel size

			Vector2[,] kernelWeight = new Vector2[hks*2, hks*2];
			for (int y=-hks;y<hks;y++) {
				for (int x=-hks;x<hks;x++) {
					Vector2 v = new Vector2(x,y);
					v.Normalize();
					kernelWeight[y + hks, x + hks] = -v;
				}
			}
					
			cancelled = false;
			for (int j=0;j<height;j++) {
				if (j % 100 == 0) {
					if (progress!=null) {
						if (progress((float) j/height, "Pass 1/4")) {
							cancelled = true;
							break;
						}
					}
					yield return null;
				}
				for (int k=0;k<width;k++) {
					int currentPixel = j * width + k;
					float sumElev = 0;
					// Compute weighter vectors
					Vector2 avgVector = Misc.Vector2zero;
					for (int y=-hks;y<hks;y++) {
						int pixelPos = currentPixel + y * width - hks;
						if (pixelPos>=0 && pixelPos<colorBufferSize) {
							for (int x=0;x<hks*2 && pixelPos<colorBufferSize;x++, pixelPos++) {
								if (pixelPos!=currentPixel) {
									float elev = colors[pixelPos].r;
									avgVector += kernelWeight[y + hks,x] * elev;
									sumElev += elev;
								}
							}
						}
					}
					if (sumElev>0) avgVector /= sumElev;
					colors[currentPixel].g = avgVector.x;
					colors[currentPixel].b = avgVector.y;
					colors[currentPixel].a = sumElev / (hks*hks*4);
				}
			}

			// apply blur
			if (progress!=null) progress(1, ""); // hide progress bar
			if (!cancelled) {
				yield return StartCoroutine(BlurPass(colors, width, height, 1, 0, progress, "Pass 2/4"));
				if (progress!=null) progress(1, ""); // hide progress bar
			}
			if (!cancelled) {
				yield return StartCoroutine(BlurPass(colors, width, height, 0, 1, progress, "Pass 3/4"));
				if (progress!=null) progress(1, ""); // hide progress bar
			}

			// clamp colors
			if (!cancelled) {
				for (int j=0;j<colorBufferSize;j++) {
					if (j % 100000 == 0) {
						if (progress!=null) {
							if (progress((float)j/colorBufferSize, "Pass 4/4")) {
								cancelled = true;
								break;
							}
						}
						yield return null;
					}
					colors[j].g = (colors[j].g * 0.5f) + 0.5f;
					colors[j].b = (colors[j].b * 0.5f) + 0.5f;
				}
				if (progress!=null) progress(1, ""); // hide progress bar
			}

			if (!cancelled) {
				tex.SetPixels(colors);
			}
			if (finish!=null) finish(tex, cancelled);
		}

		#endregion


		#region Common internal functions

		public void RegionSanitize(Region region) {
			//			UpdateLatLonFromPoints(region); // reduces precision
			List<Vector2> newPoints = new List<Vector2>(region.points);
			// removes points which are too near from others
			for (int k=0;k<newPoints.Count;k++) {
				Vector3 p0 = newPoints[k];
				for (int j=k+1;j<newPoints.Count;j++) {
					Vector3 p1 = newPoints[j];
					float distance = (p1-p0).sqrMagnitude;
					if (distance<0.00000000001f) {
						newPoints.RemoveAt(j);
						j--;
					}
				}
			}
			// remove crossing segments
			region.points = PolygonSanitizer.RemoveCrossingSegments(newPoints).ToArray();
		}


		#endregion

	}
}
