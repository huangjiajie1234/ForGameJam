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

	public partial class WMSK : MonoBehaviour {

		// Materials and resources
		Material gridMat, hudMatCell;

		// Cell mesh data
		const string CELLS_LAYER_NAME = "Grid";
		const float GRID_ENCLOSING_THRESHOLD = 0.3f; // size of the enclosing viewport rect
		Vector3[][] cellMeshBorders;
		int[][] cellMeshIndices;
		bool recreateCells;

		// Common territory & cell structures
		List<Vector3> gridPoints;
		int[] hexIndices = new int[] { 0, 1, 5, 1, 2, 5, 5, 2, 4, 2, 3, 4 };

		// Placeholders and layers
		GameObject _gridSurfacesLayer;
		GameObject gridSurfacesLayer { get { if (_gridSurfacesLayer==null) CreateGridSurfacesLayer(); return _gridSurfacesLayer; } }
		GameObject cellLayer;
		Rect gridRect;

		// Caches
		Dictionary<int, GameObject>gridSurfaces;
		Dictionary<Cell, int>_cellLookup;
		int lastCellLookupCount = -1;
		bool refreshMesh = false;
		
		// Cell highlighting
		GameObject cellHighlightedObj;
		Cell _cellHighlighted;
		int _cellHighlightedIndex = -1;
		float highlightFadeStart;
		int _cellLastClickedIndex = -1;
		

		Dictionary<Cell, int>cellLookup {
			get {
				if (_cellLookup != null && cells.Length == lastCellLookupCount)
					return _cellLookup;
				if (_cellLookup == null) {
					_cellLookup = new Dictionary<Cell,int> ();
				} else {
					_cellLookup.Clear ();
				}
				for (int k=0; k<cells.Length; k++) {
					if (cells[k]!=null) _cellLookup.Add (cells[k], k);
				}
				lastCellLookupCount = cells.Length;
				return _cellLookup;
			}
		}

		
		#region Initialization

		void CreateGridSurfacesLayer() {
			Transform t = transform.Find ("GridSurfaces");
			if (t != null) {
				DestroyImmediate (t.gameObject);
			}
			_gridSurfacesLayer = new GameObject ("GridSurfaces");
			_gridSurfacesLayer.transform.SetParent (transform, false);
			_gridSurfacesLayer.transform.localPosition = Vector3.zero; // Vector3.back * 0.01f;
			_gridSurfacesLayer.layer = gameObject.layer;
		}
		
		void DestroyGridSurfaces() {
			HideCellHighlight();
			if (gridSurfaces!=null) gridSurfaces.Clear();
			if (_gridSurfacesLayer!=null) DestroyImmediate(_gridSurfacesLayer);
		}
		
		#endregion
		
		#region Map generation


		void CreateCells () {

			int newLength = _gridRows * _gridColumns;
			if (cells==null || cells.Length!=newLength) {
				cells = new Cell[newLength];
			}
			lastCellLookupCount = -1;
			
			float qx = (_gridColumns+0.25f) * 3 / 4;
			int qy = _gridRows;
			int qx2 = _gridColumns;
			
			float stepX = 1f / qx;
			float stepY = 1f / qy;
			
			float halfStepX = stepX*0.5f;
			float halfStepY = stepY*0.5f;
			float halfStepX2 = stepX*0.25f;
			
			CellSegment [,,] sides = new CellSegment[qx2,qy,6]; // 0 = left-up, 1 = top, 2 = right-up, 3 = right-down, 4 = down, 5 = left-down
			for (int j=0;j<qy;j++) {
				int jj = j * _gridColumns;
				for (int k=0;k<qx2;k++) {
					Vector2 center = new Vector2((float)k/qx-0.5f+halfStepX,(float)j/qy-0.5f+halfStepY);
					center.x -= k *  halfStepX2; //halfStepX/2;
					Cell cell = new Cell(j,k, center);
					
					float offsetY = (k % 2 == 0) ? 0: -halfStepY;
					
					CellSegment leftUp =  (k>0 && offsetY<0) ? sides[k-1, j, 3].swapped: new CellSegment(center + new Vector2(-halfStepX, offsetY), center + new Vector2(-halfStepX2, halfStepY + offsetY));
					sides[k, j, 0] = leftUp;
					
					CellSegment top = new CellSegment(center + new Vector2(-halfStepX2, halfStepY + offsetY), center + new Vector2(halfStepX2, halfStepY + offsetY));
					sides[k, j, 1] = top;
					
					CellSegment rightUp = new CellSegment(center + new Vector2(halfStepX2, halfStepY + offsetY), center + new Vector2(halfStepX, offsetY));
					sides[k, j, 2] = rightUp;
					
					CellSegment rightDown = (j > 0 && k<qx2-1 && offsetY<0) ? sides[k+1,j-1,0].swapped: new CellSegment(center + new Vector2(halfStepX, offsetY), center + new Vector2(halfStepX2, -halfStepY + offsetY));
					sides[k, j, 3] = rightDown;
					
					CellSegment bottom = j>0 ? sides[k, j-1, 1].swapped: new CellSegment(center + new Vector2(halfStepX2, -halfStepY + offsetY), center + new Vector2(-halfStepX2, -halfStepY +offsetY));
					sides[k, j, 4] = bottom;
					
					CellSegment leftDown;
					if (offsetY<0 && j>0) {
						leftDown = sides[k-1, j-1, 2].swapped;
					} else if (offsetY==0 && k>0) {
						leftDown = sides[k-1, j, 2].swapped;
					} else {
						leftDown = new CellSegment(center + new Vector2(-halfStepX2, -halfStepY+offsetY), center + new Vector2(-halfStepX, offsetY));
					}
					sides[k, j, 5] = leftDown;
					
					if (j>0 || offsetY == 0) { //k % 2 == 0) {
						cell.center += Vector2.up * (float)offsetY;
						cell.segments.Add (leftUp);
						cell.segments.Add (top);
						cell.segments.Add (rightUp);
						cell.segments.Add (rightDown);
						cell.segments.Add (bottom);
						cell.segments.Add (leftDown);
						if (j==1) {
							bottom.isRepeated = false;
						} else if (j==0) {
							leftDown.isRepeated = false;
						}
						cell.rect2D = new Rect(leftUp.start.x, bottom.start.y, rightUp.end.x - leftUp.start.x, top.start.y - bottom.start.y);
						cells[jj + k] = cell;
					}
				}
			}
		}

		

	
		void GenerateCellsMesh () {
			if (gridPoints == null) {
				gridPoints = new List<Vector3> (_gridRows * _gridColumns * 6 + 2000);
			} else {
				gridPoints.Clear ();
			}

			int y0 = (int)((gridRect.yMin + 0.5f) * _gridRows);
			int y1 = (int)((gridRect.yMax + 0.5f) * _gridRows);
			for (int y=y0;y<=y1;y++) {
				if (y<0 || y>=_gridRows) continue;
				int yy = y * _gridColumns;
				int x0 = (int)((gridRect.xMin + 0.5f) * _gridColumns);
				int x1 = (int)((gridRect.xMax + 0.5f) * _gridColumns);
				for (int x=x0;x<=x1;x++) {
					if (x<0 || x>=_gridColumns) continue;
					Cell cell = cells [yy+x];
					if (cell!=null) {
						for (int i = 0; i<6; i++) {
							CellSegment s = cell.segments[i];
							if (!s.isRepeated) {
								gridPoints.Add (s.start);
								gridPoints.Add (s.end);
							}
						}
					}
				}
			}

			int meshGroups = (gridPoints.Count / 65000) + 1;
			int meshIndex = -1;
			if (cellMeshIndices==null || cellMeshIndices.GetUpperBound(0)!=meshGroups-1) {
				cellMeshIndices = new int[meshGroups][];
				cellMeshBorders = new Vector3[meshGroups][];
			}
			if (gridPoints.Count==0) {
				cellMeshBorders [0] = new Vector3[0];
				cellMeshIndices [0] = new int[0];
			} else {
				for (int k=0; k<gridPoints.Count; k+=65000) {
					int max = Mathf.Min (gridPoints.Count - k, 65000); 
					++meshIndex;
					if (cellMeshBorders[meshIndex]==null || cellMeshBorders[0].GetUpperBound(0)!=max-1) {
						cellMeshBorders [meshIndex] = new Vector3[max];
						cellMeshIndices [meshIndex] = new int[max];
					}
					for (int j=0; j<max; j++) {
						cellMeshBorders [meshIndex] [j] = gridPoints [j+k];
						cellMeshIndices [meshIndex] [j] = j;
					}
				}
			}

			refreshMesh = false; // mesh creation finished at this point
		}

		
		#endregion
		
		#region Drawing stuff

		
		public void GenerateGrid() {
			recreateCells = true;
			DrawGrid();
		}


		/// <summary>
		/// Determines if grid needs to be generated again, based on current viewport position
		/// </summary>
		public void CheckGridRect() {
			ComputeViewportRect();

			// Check rect size thresholds
			bool validGrid = true;
			float dx = renderViewportRect.width;
			float dy = renderViewportRect.height;
			if (dx>gridRect.width || dy>gridRect.height) {
				validGrid = false;
			} else if (dx<gridRect.width * GRID_ENCLOSING_THRESHOLD || dy<gridRect.height * GRID_ENCLOSING_THRESHOLD) {
				validGrid = false;
			} else {
				// if current viewport rect is inside grid rect and viewport size is between 0.8 and 1 from grid size then we're ok and exit.
				Vector2 p0 = new Vector2(_renderViewportRect.xMin, _renderViewportRect.yMax);
				Vector2 p1 = new Vector2(_renderViewportRect.xMax, _renderViewportRect.yMin);
				if (!gridRect.Contains(p0) || !gridRect.Contains(p1))
					validGrid = false;
			}
			if (validGrid) {
				AdjustsGridAlpha();
				return;
			}

			refreshMesh = true;
			CheckCells();
			DrawCellBorders();
		}
		
		public void DrawGrid() {

			if (!gameObject.activeInHierarchy) return;

			// Initialize surface cache
			if (gridSurfaces != null) {
				List<GameObject> cached = new List<GameObject> (gridSurfaces.Values);
				for (int k=0; k<cached.Count; k++)
					if (cached [k] != null)
						DestroyImmediate (cached [k]);
			}
			DestroyGridSurfaces();
			if (!_showGrid) return;

			gridSurfaces = new Dictionary<int, GameObject> ();

			refreshMesh = true;
			gridRect = new Rect(-1000,-1000,1,1);

			CheckCells ();
			if (_showGrid) {
				DrawCellBorders();
				DrawColorizedCells();
			}
			recreateCells = false;
		}
		
		
		void CheckCells() {
			if (!_showGrid && !_enableCellHighlight) return;
			if (cells==null || recreateCells) {
				CreateCells();
				refreshMesh = true;
			}
			if (refreshMesh) {
				float f =  GRID_ENCLOSING_THRESHOLD + (1f - GRID_ENCLOSING_THRESHOLD) * 0.5f;
				float gridWidth = renderViewportRect.width / f;
				float gridHeight = renderViewportRect.height / f;
				gridRect = new Rect(_renderViewportRect.center.x - gridWidth * 0.5f, _renderViewportRect.center.y - gridHeight * 0.5f, gridWidth, gridHeight);
				GenerateCellsMesh();
			}
			
		}
		
		void DrawCellBorders () {
			
			if (cellLayer != null) {
				DestroyImmediate (cellLayer);
			} else {
				Transform t = transform.Find(CELLS_LAYER_NAME);
				if (t!=null) DestroyImmediate(t.gameObject);
			}
			if (cells.Length==0) return;
			
			cellLayer = new GameObject (CELLS_LAYER_NAME);
			cellLayer.hideFlags = HideFlags.DontSave;
			cellLayer.transform.SetParent (transform, false);
			cellLayer.transform.localPosition = Vector3.back * 0.001f;
			int layer = transform.gameObject.layer;
			cellLayer.layer = layer;
			
			for (int k=0; k<cellMeshBorders.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.hideFlags = HideFlags.DontSave;
				flayer.layer = layer;
				flayer.transform.SetParent (cellLayer.transform, false);
				flayer.transform.localPosition = Vector3.zero;
				flayer.transform.localRotation = Quaternion.Euler (Vector3.zero);
				
				Mesh mesh = new Mesh ();
				mesh.vertices = cellMeshBorders [k];
				mesh.SetIndices (cellMeshIndices [k], MeshTopology.Lines, 0);
				
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;
				
				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;

				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.useLightProbes = false;
				mr.sharedMaterial = gridMat;
			}
			AdjustsGridAlpha();
		}

		// Adjusts alpha according to minimum and maximum distance
		void AdjustsGridAlpha() {
			float gridAlpha;
			if (lastDistanceFromCamera<_gridMinDistance) {
				gridAlpha = 1f - (_gridMinDistance - lastDistanceFromCamera) / (_gridMinDistance * 0.2f);
			} else if (lastDistanceFromCamera>_gridMaxDistance) {
				gridAlpha = 1f - (lastDistanceFromCamera - _gridMaxDistance) / (_gridMaxDistance * 0.5f);
			} else {
				gridAlpha = 1f;
			}
			gridAlpha = Mathf.Clamp01 (_gridColor.a * gridAlpha);
			if (gridAlpha!=gridMat.color.a) {
				gridMat.color = new Color(_gridColor.r, _gridColor.g, _gridColor.b, gridAlpha);
			}
			cellLayer.SetActive(_showGrid && gridAlpha>0);
		}
		
		
		void DrawColorizedCells() {
			int cellsCount = cells.Length;
			for (int k=0;k<cellsCount;k++) {
				Cell cell = cells[k];
				if (cell==null) continue;
				if (cell.customMaterial!=null) { // && cell.visible) {
					ToggleCellSurface(k, true, cell.customMaterial.color, false, (Texture2D)cell.customMaterial.mainTexture, cell.customTextureScale, cell.customTextureOffset, cell.customTextureRotation);
				}
			}
		}

		GameObject GenerateCellSurface (int cellIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			if (cellIndex<0 || cellIndex>=cells.Length) return null;
			int cacheIndex = GetCacheIndexForCell (cellIndex); 
			return GenerateCellSurface(cells[cellIndex], cacheIndex, material,   textureScale, textureOffset, textureRotation);
		}
		
		GameObject GenerateCellSurface (Cell cell, int cacheIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			string cacheIndexSTR = cacheIndex.ToString();
			// Deletes potential residual surface
			Transform t = gridSurfacesLayer.transform.Find(cacheIndexSTR);
			if (t!=null) DestroyImmediate(t.gameObject);
			Rect rect = cell.rect2D;
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, cell.points.ToArray(), hexIndices, material, rect, textureScale, textureOffset, textureRotation);									
			surf.transform.SetParent (gridSurfacesLayer.transform, false);
			surf.transform.localPosition = Vector3.zero;
			surf.layer = gameObject.layer;
			if (gridSurfaces.ContainsKey(cacheIndex)) gridSurfaces.Remove(cacheIndex);
			gridSurfaces.Add (cacheIndex, surf);
			return surf;
		}


		#endregion

		#region Highlighting
		
		void GridCheckMousePos () {
			if (!Application.isPlaying || !_showGrid)
				return;
			
			if (!lastMouseMapHitPosGood) {
				HideCellHighlight ();
				return;
			}
			
			// verify if last highlited cell remains active
			if (_cellHighlightedIndex >= 0) {
				if (_cellHighlighted.ContainsPoint (lastMouseMapHitPos.x, lastMouseMapHitPos.y)) { 
					return;
				}
			}
			int newCellHighlightedIndex = GetCellIndex(lastMouseMapHitPos);
			if (newCellHighlightedIndex >= 0) {
				HighlightCell (newCellHighlightedIndex, false);
			} else {
				HideCellHighlight ();
			}
		}
		
		
		void GridUpdateHighlightFade() {
			if (_highlightFadeAmount==0) return;
			
			if (cellHighlightedObj!=null) {
				float newAlpha = 1.0f - Mathf.PingPong(Time.time - highlightFadeStart, _highlightFadeAmount);
				Material mat = cellHighlightedObj.GetComponent<Renderer>().sharedMaterial;
				Color color = mat.color;
				Color newColor = new Color(color.r, color.g, color.b, newAlpha);
				mat.color = newColor;
			}
			
		}

		
		#endregion
		
		
		#region Geometric functions

		Vector3 GetWorldSpacePosition(Vector2 localPosition) {
			return transform.TransformPoint(localPosition);
		}
		
		
		#endregion
		
	
		
		#region Cell stuff
		
		int GetCacheIndexForCell (int cellIndex) {
			return cellIndex; // * 1000 + regionIndex;
		}
		
		/// <summary>
		/// Highlights the cell region specified. Returns the generated highlight surface gameObject.
		/// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a territory region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		void HighlightCell (int cellIndex, bool refreshGeometry) {
			if (cellHighlightedObj!=null) HideCellHighlight();
			if (cellIndex<0 || cellIndex>=cells.Length) return;

			if (!cells[cellIndex].isFading && _enableCellHighlight) {
			int cacheIndex = GetCacheIndexForCell (cellIndex); 
			bool existsInCache = gridSurfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = gridSurfaces [cacheIndex];
				gridSurfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			if (existsInCache) {
				cellHighlightedObj = gridSurfaces [cacheIndex];
				if (cellHighlightedObj!=null) {
					cellHighlightedObj.SetActive (true);
					cellHighlightedObj.GetComponent<Renderer> ().sharedMaterial = hudMatCell;
				} else {
					gridSurfaces.Remove(cacheIndex);
				}
			} else {
				cellHighlightedObj = GenerateCellSurface (cellIndex, hudMatCell, Vector2.one, Vector2.zero, 0);
			}
				highlightFadeStart = Time.time;
			}

			_cellHighlighted = cells[cellIndex];
			_cellHighlightedIndex = cellIndex;
			
			if (OnCellEnter!=null) OnCellEnter(_cellHighlightedIndex);

		}
		
		void HideCellHighlight () {
			if (cellHighlighted == null)
				return;
			if (cellHighlightedObj != null) {
				if (!cellHighlighted.isFading) {
					if (cellHighlighted.customMaterial!=null) {
						ApplyMaterialToSurface (cellHighlightedObj, cellHighlighted.customMaterial);
					} else {
						cellHighlightedObj.SetActive (false);
					}
				}
				cellHighlightedObj = null;
			}
			if (OnCellExit!=null) OnCellExit(_cellHighlightedIndex);
			_cellHighlighted = null;
			_cellHighlightedIndex = -1;
		}
		


		List<int> GetCellsWithinRect(Rect rect2D) {
			int r0 = (int)((rect2D.yMin + 0.5f) * _gridRows);
			int r1 = (int)((rect2D.yMax + 0.5f) * _gridRows);
			int c0 = (int)((rect2D.xMin + 0.5f) * _gridColumns);
			int c1 = (int)((rect2D.xMax + 0.5f) * _gridColumns);
			List<int>indices = new List<int>();
			for (int r=r0;r<=r1;r++) {
				int rr = r * _gridColumns;
				for (int c=c0;c<=c1;c++) {
					int cellIndex = rr + c;
					Cell cell = cells [cellIndex];
					if (cell!=null) indices.Add (cellIndex);
				}
			}
			return indices;
		}

		#endregion

	}

}