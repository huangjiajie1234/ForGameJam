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

	public partial class WMSK : MonoBehaviour
	{
		const float EARTH_RADIUS_KM = 6371f;

		#region Public properties

		[SerializeField]
		bool
			_showCursor = true;
		
		/// <summary>
		/// Toggle cursor lines visibility.
		/// </summary>
		public bool showCursor { 
			get {
				return _showCursor; 
			}
			set {
				if (value != _showCursor) {
					_showCursor = value;
					isDirty = true;

					if (cursorLayerVLine != null) {
						cursorLayerVLine.SetActive (_showCursor);
					}
					if (cursorLayerHLine != null) {
						cursorLayerHLine.SetActive (_showCursor);
					}
				}
			}
		}

		/// <summary>
		/// Cursor lines color.
		/// </summary>
		[SerializeField]
		Color
			_cursorColor = new Color (0.56f, 0.47f, 0.68f);
		
		public Color cursorColor {
			get {
				if (cursorMatH != null) {
					return cursorMatH.color;
				} else {
					return _cursorColor;
				}
			}
			set {
				if (value != _cursorColor) {
					_cursorColor = value;
					isDirty = true;
					
					if (cursorMatH != null && _cursorColor != cursorMatH.color) {
						cursorMatH.color = _cursorColor;
					}
					if (cursorMatV != null && _cursorColor != cursorMatV.color) {
						cursorMatV.color = _cursorColor;
					}
				}
			}
		}

		[SerializeField]
		bool
			_cursorFollowMouse = true;
		
		/// <summary>
		/// Makes the cursor follow the mouse when it's over the World.
		/// </summary>
		public bool cursorFollowMouse { 
			get {
				return _cursorFollowMouse; 
			}
			set {
				if (value != _cursorFollowMouse) {
					_cursorFollowMouse = value;
					isDirty = true;
				}
			}
		}

		Vector3
			_cursorLocation;

		public Vector3
			cursorLocation {
			get {
				return _cursorLocation;
			}
			set {
				if (_cursorLocation != value) {
					_cursorLocation = value;
					if (cursorLayerVLine != null) {
						Vector3 pos = cursorLayerVLine.transform.localPosition;
						cursorLayerVLine.transform.localPosition = new Vector3(_cursorLocation.x, 0, pos.z);
					}
					if (cursorLayerHLine != null) {
						Vector3 pos = cursorLayerHLine.transform.localPosition;
						cursorLayerHLine.transform.localPosition = new Vector3(0, _cursorLocation.y, pos.z);
					}
				}
			}
		}

		
		/// <summary>
		/// If set to false, cursor will be hidden when mouse if not over the map.
		/// </summary>
		[SerializeField]
		bool
			_cursorAllwaysVisible = true;
		
		public bool cursorAlwaysVisible {
			get {
				return _cursorAllwaysVisible;
			}
			set {
				if (value != _cursorAllwaysVisible) {
					_cursorAllwaysVisible = value;
					isDirty = true;
					CheckCursorVisibility();
				}
			}
		}


		[SerializeField]
		bool
			_showLatitudeLines = true;
		
		/// <summary>
		/// Toggle latitude lines visibility.
		/// </summary>
		public bool showLatitudeLines { 
			get {
				return _showLatitudeLines; 
			}
			set {
				if (value != _showLatitudeLines) {
					_showLatitudeLines = value;
					isDirty = true;
					
					if (latitudeLayer != null) {
						latitudeLayer.SetActive (_showLatitudeLines);
					} else if (_showLatitudeLines) {
						DrawLatitudeLines();
					}
					if (_showLatitudeLines) {
						showGrid = false;
					}
				}
			}
		}

		[SerializeField]
		[Range(5.0f, 45.0f)]
		int
			_latitudeStepping = 15;
		/// <summary>
		/// Specify latitude lines separation.
		/// </summary>
		public int latitudeStepping { 
			get {
				return _latitudeStepping; 
			}
			set {
				if (value != _latitudeStepping) {
					_latitudeStepping = value;
					isDirty = true;

					if (gameObject.activeInHierarchy)
						DrawLatitudeLines ();
				}
			}
		}

		[SerializeField]
		bool
			_showLongitudeLines = true;
		
		/// <summary>
		/// Toggle longitude lines visibility.
		/// </summary>
		public bool showLongitudeLines { 
			get {
				return _showLongitudeLines; 
			}
			set {
				if (value != _showLongitudeLines) {
					_showLongitudeLines = value;
					isDirty = true;
					
					if (longitudeLayer != null) {
						longitudeLayer.SetActive (_showLongitudeLines);
					} else if (_showLongitudeLines) {
						DrawLongitudeLines();
					}
					if (_showLongitudeLines) {
						showGrid = false;
					}
				}
			}
		}
		
		[SerializeField]
		[Range(5.0f, 45.0f)]
		int
			_longitudeStepping = 15;
		/// <summary>
		/// Specify longitude lines separation.
		/// </summary>
		public int longitudeStepping { 
			get {
				return _longitudeStepping; 
			}
			set {
				if (value != _longitudeStepping) {
					_longitudeStepping = value;
					isDirty = true;

					if (gameObject.activeInHierarchy)
						DrawLongitudeLines ();
				}
			}
		}

		/// <summary>
		/// Color for imaginary lines (longitude and latitude).
		/// </summary>
		[SerializeField]
		Color
			_imaginaryLinesColor = new Color (0.16f, 0.33f, 0.498f);
		
		public Color imaginaryLinesColor {
			get {
				if (imaginaryLinesMat != null) {
					return imaginaryLinesMat.color;
				} else {
					return _imaginaryLinesColor;
				}
			}
			set {
				if (value != _imaginaryLinesColor) {
					_imaginaryLinesColor = value;
					isDirty = true;
					
					if (imaginaryLinesMat != null && _imaginaryLinesColor != imaginaryLinesMat.color) {
						imaginaryLinesMat.color = _imaginaryLinesColor;
					}
				}
			}
		}

	#endregion

	#region Public API area
	
		/// <summary>
		/// Adds a custom marker (gameobject) to the globe on specified location and with custom scale.
		/// </summary>
		public void AddMarker (GameObject marker, Vector3 planeLocation, float markerScale)
		{
			// Try to get the height of the object
			float height = 0;
			if (marker.GetComponent<MeshFilter> () != null)
				height = marker.GetComponent<MeshFilter> ().sharedMesh.bounds.size.y;
			else if (marker.GetComponent<Collider> () != null) 
				height = marker.GetComponent<Collider> ().bounds.size.y;
			
			float h = height * markerScale / planeLocation.magnitude; // lift the marker so it appears on the surface of the globe
			
			CheckMarkersLayer ();
			
			marker.transform.SetParent (markersLayer.transform, false);
			marker.transform.localPosition = planeLocation + Misc.Vector3back * (0.001f + h * 0.5f);
			marker.layer = gameObject.layer;

			// apply custom scale
			float prop = mapWidth / mapHeight;
			marker.transform.localScale = new Vector3 (markerScale, prop * markerScale, markerScale);
		}
		
		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="start">starting location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="end">end location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine (Vector2 start, Vector2 end, Color color, float arcElevation, float lineWidth)
		{
			Vector2[] path = new Vector2[] { start, end };
			LineMarkerAnimator lma = AddLine(path, markerMat, arcElevation, lineWidth);
			lma.color = color;
			return lma;
		}
	
		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="Color">line color</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine (Vector2[] points, Color color, float arcElevation, float lineWidth)
		{
			LineMarkerAnimator lma = AddLine (points, markerMat, arcElevation, lineWidth);
			lma.color = color;
			return lma;
		}
	
		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="start">starting location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="end">end location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="lineMaterial">line material</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine (Vector2 start, Vector2 end, Material lineMaterial, float arcElevation, float lineWidth)
		{
			Vector2[] path = new Vector2[] { start, end };
			return AddLine(path, lineMaterial, arcElevation, lineWidth);
		}

		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="lineMaterial">line material</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine (Vector2[] points, Material lineMaterial, float arcElevation, float lineWidth) {
			CheckMarkersLayer ();
			GameObject newLine = new GameObject ("MarkerLine");
			newLine.layer = gameObject.layer;
			bool usesRenderViewport = renderViewportIsEnabled && arcElevation>0;
			if (!usesRenderViewport) newLine.transform.SetParent (markersLayer.transform, false);
			LineMarkerAnimator lma = newLine.AddComponent<LineMarkerAnimator> ();
			lma.map = this;
			lma.path = points;
			lma.color = Color.white;
			lma.arcElevation = arcElevation;
			if (usesRenderViewport) lineWidth *= _renderViewportScaleFactor * 90.0f;
			lma.lineWidth = lineWidth;
			lma.lineMaterial = lineMaterial;
			return lma;
		}

		/// <summary>
		/// Adds a custom marker (polygon) to the globe on specified location and with custom size in km.
		/// </summary>
		/// <param name="position">Position for the center of the circle.</param>
		/// <param name="kmRadius">Radius in KM.</param>
		/// <param name="ringWidthStart">Ring inner limit (0..1). Pass 0 to draw a full circle.</param>
		/// <param name="ringWidthEnd">Ring outer limit (0..1). Pass 1 to draw a full circle.</param>
		/// <param name="color">Color</param>
		public GameObject AddCircle(Vector2 position, float kmRadius, float ringWidthStart, float ringWidthEnd, Color color) {
			CheckMarkersLayer();
			float rw = 2.0f * Mathf.PI * EARTH_RADIUS_KM;
			float w = kmRadius / rw;
			w *= mapWidth;
			float h = w * 2f;
			GameObject marker = Drawing.DrawCircle("MarkerCircle", position, w,h, 0, Mathf.PI*2.0f, ringWidthStart, ringWidthEnd, 64, GetColoredMarkerMaterial(color));
			if (marker!=null) {
				marker.transform.SetParent(markersLayer.transform, false);
				marker.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
				marker.layer = markersLayer.layer;
			}
			return marker;
		}


		/// <summary>
		/// Deletes all custom markers and lines
		/// </summary>
		public void ClearMarkers ()
		{
			if (markersLayer == null)
				return;
			Destroy (markersLayer);
		}
		
		
		/// <summary>
		/// Removes all marker lines.
		/// </summary>
		public void ClearLineMarkers ()
		{
			if (markersLayer == null)
				return;
			LineRenderer[] t = markersLayer.transform.GetComponentsInChildren<LineRenderer> ();
			for (int k=0; k<t.Length; k++)
				Destroy (t [k].gameObject);
		}
	
		#endregion

	}

}