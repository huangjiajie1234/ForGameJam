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

	
	/* Event definitions */
	public delegate void OnCellEnter(int cellIndex);
	public delegate void OnCellExit(int cellIndex);
	public delegate void OnCellClick(int cellIndex);
	

	public partial class WMSK : MonoBehaviour
	{

		#region Public properties

		[SerializeField]
		bool
			_showGrid = false;
		
		/// <summary>
		/// Toggle grid on/off.
		/// </summary>
		public bool showGrid { 
			get {
				return _showGrid; 
			}
			set {
				if (value != _showGrid) {
					_showGrid = value;
					isDirty = true;

					if (cellLayer != null) {
						CheckGridRect();
						cellLayer.SetActive (_showGrid);
					} else if (_showGrid) {
						DrawGrid ();
					}
				}
			}
		}

		/// <summary>
		/// Enable/disable cell highlight when grid is visible and mouse is over.
		/// </summary>
		[SerializeField]
		bool
			_enableCellHighlight = true;
		
		public bool enableCellHighlight {
			get {
				return _enableCellHighlight;
			}
			set {
				if (_enableCellHighlight != value) {
					_enableCellHighlight = value;
					isDirty = true;
					if (_enableCellHighlight) {
						enableCountryHighlight = false;
						enableProvinceHighlight = false;
						showLatitudeLines = false;
						showLongitudeLines = false;
					} else {
						HideCellHighlight();
					}
				}
			}
		}

		
		[SerializeField]
		int _gridRows = 32;
		/// <summary>
		/// Returns the number of rows for box and hexagonal grid topologies
		/// </summary>
		public int gridRows { 
			get {
				return _gridRows;
			}
			set {
				if (value != _gridRows) {
					_gridRows = value;
					isDirty = true;
					GenerateGrid();
				}
			}
			
		}
		
		[SerializeField]
		int _gridColumns = 64;
		/// <summary>
		/// Returns the number of columns for box and hexagonal grid topologies
		/// </summary>
		public int gridColumns { 
			get {
				return _gridColumns;
			}
			set {
				if (value != _gridColumns) {
					_gridColumns = value;
					isDirty = true;
					GenerateGrid();
				}
			}
		}

		/// <summary>
		/// Complete array of cells.
		/// </summary>
		[NonSerialized]
		public Cell[] cells;
		
		
		[SerializeField]
		float _highlightFadeAmount = 0.5f;

		/// <summary>
		/// Amount of fading ping-poing effect for highlighted cell
		/// </summary>
		public float highlightFadeAmount {
			get {
				return _highlightFadeAmount;
			}
			set {
				if (_highlightFadeAmount!=value) {
					_highlightFadeAmount = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		Color
			_gridColor = new Color (0.486f, 0.490f, 0.529f, 1.0f);
		
		/// <summary>
		/// Cells border color
		/// </summary>
		public Color gridColor {
			get {
				return _gridColor;
			}
			set {
				if (value != _gridColor) {
					_gridColor = value;
					isDirty = true;
					if (gridMat != null && _gridColor != gridMat.color) {
						gridMat.color = _gridColor;
					}
				}
			}
		}

		
		[SerializeField]
		Color
			_cellHighlightColor = new Color (1, 0, 0, 0.7f);
		
		/// <summary>
		/// Fill color to use when the mouse hovers a cell's region.
		/// </summary>
		public Color cellHighlightColor {
			get {
				return _cellHighlightColor;
			}
			set {
				if (value != _cellHighlightColor) {
					_cellHighlightColor = value;
					isDirty = true;
					if (hudMatCell != null && _cellHighlightColor != hudMatCell.color) {
						hudMatCell.color = _cellHighlightColor;
					}
				}
			}
		}

		
		[SerializeField]
		float _gridMaxDistance = 1000f;

		/// <summary>
		/// Maximum distance from grid where it's visible
		/// </summary>
		public float gridMaxDistance {
			get { return _gridMaxDistance; }
			set {
				if (value != _gridMaxDistance) {
					_gridMaxDistance = value;
					isDirty = true;
					CheckGridRect();
				}
			}
		}
		
		[SerializeField]
		float _gridMinDistance = 0.01f;
		/// <summary>
		/// Minimum distance from grid where it's visible
		/// </summary>
		public float gridMinDistance {
			get { return _gridMinDistance; }
			set {
				if (value != _gridMinDistance) {
					_gridMinDistance = value;
					isDirty = true;
					CheckGridRect();
				}
			}
		}


	#endregion

	#region Public API area
	
		public event OnCellEnter OnCellEnter;
		public event OnCellExit OnCellExit;
		public event OnCellClick OnCellClick;
		
		/// <summary>
		/// Returns Cell under mouse position or null if none.
		/// </summary>
		public Cell cellHighlighted { get { return _cellHighlighted; } }
		
		/// <summary>
		/// Returns current highlighted cell index.
		/// </summary>
		public int cellHighlightedIndex { get { return _cellHighlightedIndex; } }
		
		/// <summary>
		/// Returns Cell index which has been clicked
		/// </summary>
		public int cellLastClickedIndex { get { return _cellLastClickedIndex; } }
		

		
		/// <summary>
		/// Returns the_numCellsrovince in the cells array by its reference.
		/// </summary>
		public int GetCellIndex (Cell cell) {
			//			string searchToken = cell.territoryIndex + "|" + cell.name;
			if (cellLookup.ContainsKey(cell)) 
				return _cellLookup[cell];
			else
				return -1;
		}
		
		/// <summary>
		/// Colorizes specified cell by index.
		/// </summary>
		public void ToggleCellSurface (int cellIndex, bool visible, Color color, bool refreshGeometry) {
			ToggleCellSurface(cellIndex, visible, color, refreshGeometry, null, Vector2.one, Vector2.zero, 0);
		}
		
		/// <summary>
		/// Colorizes or texture specified cell by index.
		/// </summary>
		public void ToggleCellSurface (int cellIndex, bool visible, Color color,  bool refreshGeometry, Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			if (cellIndex<0 || cellIndex>=cells.Length) return;
			if (cells[cellIndex]==null) return;
			if (!visible) {
				HideCellSurface (cellIndex);
				return;
			}
			int cacheIndex = GetCacheIndexForCell (cellIndex); 
			bool existsInCache = gridSurfaces.ContainsKey (cacheIndex);
			if (existsInCache && gridSurfaces[cacheIndex]==null) {
				gridSurfaces.Remove(cacheIndex);
				existsInCache = false;
			}
			if (refreshGeometry && existsInCache) {
				GameObject obj = gridSurfaces [cacheIndex];
				gridSurfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			GameObject surf = null;
			if (existsInCache) 
				surf = gridSurfaces [cacheIndex];
			
			// Should the surface be recreated?
			Material surfMaterial;
			Cell cell = cells[cellIndex];
			if (surf != null) {
				surfMaterial = surf.GetComponent<Renderer> ().sharedMaterial;
				if (texture != null && (cell.customMaterial == null || textureScale != cell.customTextureScale || textureOffset != cell.customTextureOffset || 
				                        textureRotation != cell.customTextureRotation || !cell.customMaterial.name.Equals (texturizedMat.name))) {
					gridSurfaces.Remove (cacheIndex);
					DestroyImmediate (surf);
					surf = null;
				}
			}
			// If it exists, activate and check proper material, if not create surface
			bool isHighlighted = cellHighlightedIndex == cellIndex;
			if (surf != null) {
				if (!surf.activeSelf)
					surf.SetActive (true);
				// Check if material is ok
				surfMaterial = surf.GetComponent<Renderer> ().sharedMaterial;
				if ((texture == null && !surfMaterial.name.Equals (coloredMat.name)) || (texture != null && !surfMaterial.name.Equals (texturizedMat.name)) 
				    || (surfMaterial.color != color && !isHighlighted) || (texture != null && cell.customMaterial.mainTexture != texture)) {
					Material goodMaterial = GetColoredTexturedMaterial (color, texture, false);
					cell.customMaterial = goodMaterial;
					ApplyMaterialToSurface (surf, goodMaterial);
				}
			} else {
				surfMaterial = GetColoredTexturedMaterial (color, texture, false);
				surf = GenerateCellSurface (cellIndex, surfMaterial, textureScale, textureOffset, textureRotation);
				cell.customMaterial = surfMaterial;
				cell.customTextureOffset = textureOffset;
				cell.customTextureRotation = textureRotation;
				cell.customTextureScale = textureScale;
			}
			// If it was highlighted, highlight it again
			if (cell.customMaterial != null && isHighlighted && cell.customMaterial.color != hudMatCell.color) {
				Material clonedMat = Instantiate (cell.customMaterial);
				clonedMat.hideFlags = HideFlags.DontSave;
				clonedMat.name = cell.customMaterial.name;
				clonedMat.color = hudMatCell.color;
				surf.GetComponent<Renderer> ().sharedMaterial = clonedMat;
				cellHighlightedObj = surf;
			}
		}


		/// <summary>
		/// Uncolorize/hide all cells.
		/// </summary>
		public void HideCellSurfaces() {
			for (int k=0;k<cells.Length;k++) {
				HideCellSurface(k);
			}
		}
		
		/// <summary>
		/// Uncolorize/hide specified cell by index in the cells collection.
		/// </summary>
		public void HideCellSurface (int cellIndex) {
			if (_cellHighlightedIndex != cellIndex) {
				int cacheIndex = GetCacheIndexForCell (cellIndex);
				if (gridSurfaces.ContainsKey (cacheIndex)) {
					if (gridSurfaces[cacheIndex] == null) {
						gridSurfaces.Remove(cacheIndex);
					} else {
						gridSurfaces [cacheIndex].SetActive (false);
					}
				}
			}
			if (cells[cellIndex]!=null) {
				cells [cellIndex].customMaterial = null;
			}
		}
		
		
		/// <summary>
		/// Colors a cell and fades it out during "duration" in seconds.
		/// </summary>
		public void CellFadeOut(int cellIndex, Color color, float duration) {
			if (cellIndex == _cellHighlightedIndex) {
				cells[cellIndex].isFading = true;
				HideCellHighlight();
			}
			ToggleCellSurface(cellIndex, true, color, false);
			int cacheIndex = GetCacheIndexForCell (cellIndex);
			if (gridSurfaces.ContainsKey (cacheIndex)) {
				GameObject cellSurface = gridSurfaces[cacheIndex];
				SurfaceFader fader = cellSurface.AddComponent<SurfaceFader>();
				fader.duration = duration;
				fader.fadeEntity = cells[cellIndex];
			}
		}

		/// <summary>
		/// Gets the cell's center position in world space.
		/// </summary>
		public Vector3 GetCellWorldPosition(int cellIndex) {
			Vector2 cellGridCenter = cells[cellIndex].center;
			return GetWorldSpacePosition(cellGridCenter);
		}

		/// <summary>
		/// Returns the world space position of the vertex
		/// </summary>
		public Vector3 GetCellVertexWorldPosition(int cellIndex, int vertexIndex) {
			Vector2 localPosition = cells[cellIndex].points[vertexIndex];
			return GetWorldSpacePosition(localPosition);
		}


		/// <summary>
		/// Returns the cell object under position in local coordinates
		/// </summary>
		public Cell GetCell(Vector2 localPosition) {
			int cellIndex = GetCellIndex(localPosition);
			if (cellIndex>=0) return cells[cellIndex];
			return null;
		}

		/// <summary>
		/// Returns the cell at the specified row and column. This is a shortcut to accessing the cells array using row * gridColumns + column as the index.
		/// </summary>
		public Cell GetCell(int row, int column) {
			int cellIndex = row * _gridColumns + column;
			if (cellIndex<0 || cellIndex>=cells.Length) return null;
			return cells[cellIndex];
		}

		/// <summary>
		/// Returns the cell index under position in local coordinates
		/// </summary>
		public int GetCellIndex(Vector2 localPosition) {
			int row = (int)((localPosition.y + 0.5f) * _gridRows);
			int col = (int)((localPosition.x + 0.5f) * _gridColumns);
			for (int r=row-1;r<=row+1;r++) {
				if (r<0 || r>=_gridRows) continue;
				int rr = r * _gridColumns;
				for (int c=col-1;c<=col+1;c++) {
					if (c<0 || c>=_gridColumns) continue;
					int cellIndex = rr + c;
					Cell cell = cells [cellIndex];
					if (cell!=null && cell.ContainsPoint (localPosition.x, localPosition.y)) {
						return cellIndex;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns a list of cells indices whose center belongs to a country regions.
		/// </summary>
		public List<int>GetCellsInCountry(int countryIndex) {
			List<int> allCells = new List<int>();
			Country country = countries[countryIndex];
			for (int k=0;k<country.regions.Count;k++) {
				List<int> candidateCells = GetCellsWithinRect(country.regions[k].rect2D);
				for (int c=0;c<candidateCells.Count;c++) {
					int cellCountry = GetCellCountryIndex(candidateCells[c]);
					if (cellCountry == countryIndex) {
						allCells.Add (candidateCells[c]);
					}
				}
			}
			return allCells;
		}


		/// <summary>
		/// Returns a list of cells whose center belongs to a country region.
		/// </summary>
		public List<int>GetCellsInProvince(int provinceIndex) {
			List<int> allCells = new List<int>();
			Province province = provinces[provinceIndex];
			for (int k=0;k<province.regions.Count;k++) {
				List<int> candidateCells = GetCellsWithinRect(province.regions[k].rect2D);
				for (int c=0;c<candidateCells.Count;c++) {
					int cellProvince = GetCellProvinceIndex(candidateCells[c]);
					if (cellProvince == provinceIndex) {
						allCells.Add (candidateCells[c]);
					}
				}
			}
			return allCells;
		}

		/// <summary>
		/// Returns the country index to which the cell belongs.
		/// </summary>
		/// <returns>The cell country index.</returns>
		public int GetCellCountryIndex(int cellIndex) {
			Cell cell = cells[cellIndex];
			if (cell==null) return -1;
			Dictionary<int, int>countryCount = new Dictionary<int, int>();
			int countryMax = -1, countryMaxCount = 0;
			int pointCount = cell.points.Count;
			for (int k=-1;k<pointCount;k++) {
				int countryIndex;
				if (k==-1) {
					countryIndex = GetCountryIndex(cell.center);
				} else {
					countryIndex = GetCountryIndex(cell.points[k]);
				}
				if (countryIndex == -1) continue;
				int count;
				if (countryCount.ContainsKey(countryIndex)) {
					count = countryCount[countryIndex] + 1;
					countryCount[countryIndex] = count;
				} else {
					count = 1;
					countryCount.Add (countryIndex, count);
				}
				if (count>countryMaxCount) {
					countryMaxCount = count;
					countryMax = countryIndex;
				}
			}
			return countryMax;
		}

		
		/// <summary>
		/// Returns the province index to which the cell belongs.
		/// </summary>
		public int GetCellProvinceIndex(int cellIndex) {
			Cell cell = cells[cellIndex];
			if (cell==null) return -1;
			Dictionary<int, int>provinceCount = new Dictionary<int, int>();
			int provinceMax = -1, provinceMaxCount = 0;
			int pointCount = cell.points.Count;
			for (int k=-1;k<pointCount;k++) {
				int provinceIndex;
				if (k==-1) {
					provinceIndex = GetProvinceIndex(cell.center);
				} else {
					provinceIndex = GetProvinceIndex(cell.points[k]);
				}
				if (provinceIndex == -1) continue;
				int count;
				if (provinceCount.ContainsKey(provinceIndex)) {
					count = provinceCount[provinceIndex] + 1;
					provinceCount[provinceIndex] = count;
				} else {
					count = 1;
					provinceCount.Add (provinceIndex, count);
				}
				if (count>provinceMaxCount) {
					provinceMaxCount = count;
					provinceMax = provinceIndex;
				}
			}
			return provinceMax;
		}

		#endregion

	}

}