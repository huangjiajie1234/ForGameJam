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

namespace WorldMapStrategyKit {
	public delegate void OnClick (float x, float y);
	public delegate void OnMouseMove (float x, float y);

	public partial class WMSK : MonoBehaviour {
		/// <summary>
		/// Raised when user clicks on the map. Returns x/y local space coordinates (-0.5, 0.5)
		/// </summary>
		public event OnClick OnClick;
		/// <summary>
		/// Occurs when mouse moves over the map.
		/// </summary>
		public event OnMouseMove OnMouseMove;


		#region Public properties

		/// <summary>
		/// Returns true is mouse has entered the Earth's collider.
		/// </summary>
		[NonSerialized]
		public bool
			mouseIsOver;

		/// <summary>
		/// The navigation time in seconds.
		/// </summary>
		[SerializeField]
		[Range(1.0f, 16.0f)]
		float
			_navigationTime = 4.0f;
		
		public float navigationTime {
			get {
				return _navigationTime;
			}
			set {
				if (_navigationTime != value) {
					_navigationTime = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Returns whether a navigation is taking place at this moment.
		/// </summary>
		public bool isFlying { get { return flyToActive; } }

		[SerializeField]
		bool
			_fitWindowWidth = true;
		/// <summary>
		/// Ensure the map is always visible and occupy the entire Window.
		/// </summary>
		public bool fitWindowWidth { 
			get {
				return _fitWindowWidth; 
			}
			set {
				if (value != _fitWindowWidth) {
					_fitWindowWidth = value;
					isDirty = true;
					if (_fitWindowWidth)
						CenterMap ();
					else if (!_fitWindowHeight) 
						maxFrustumDistance = float.MaxValue;
				}
			}
		}

		[SerializeField]
		bool
			_fitWindowHeight = true;
		/// <summary>
		/// Ensure the map is always visible and occupy the entire Window.
		/// </summary>
		public bool fitWindowHeight { 
			get {
				return _fitWindowHeight; 
			}
			set {
				if (value != _fitWindowHeight) {
					_fitWindowHeight = value;
					isDirty = true;
					if (_fitWindowHeight)
						CenterMap ();
					else if (!fitWindowWidth) 
						maxFrustumDistance = float.MaxValue;
				}
			}
		}

		[SerializeField]
		Rect _windowRect = new Rect(-0.5f,-0.5f,1,1);

		public Rect windowRect {
			get {
				return _windowRect;
			}
			set {
				if (value != _windowRect) {
					_windowRect = value;
					isDirty = true;
					CenterMap();
				}
			}
		}

		[SerializeField]
		bool
			_allowUserKeys = false;

		/// <summary>
		/// If user can use WASD keys to drag the map.
		/// </summary>
		public bool	allowUserKeys {
			get { return _allowUserKeys; }
			set {
				_allowUserKeys = value;
				isDirty = true;
			}
		}

		[SerializeField]
		bool
			_dragFlipDirection = false;

		/// <summary>
		/// Whether the direction of the drag should be inverted.
		/// </summary>
		public bool	dragFlipDirection {
			get { return _dragFlipDirection; }
			set {
				_dragFlipDirection = value;
				isDirty = true;
			}
		}

		[SerializeField]
		bool
			_dragConstantSpeed = false;

		/// <summary>
		/// Whether the drag should follow a constant movement, withouth acceleration.
		/// </summary>
		public bool	dragConstantSpeed {
			get { return _dragConstantSpeed; }
			set {
				_dragConstantSpeed = value;
				isDirty = true;
			}
		}

		[SerializeField]
		bool
			_allowUserDrag = true;
		
		public bool	allowUserDrag {
			get { return _allowUserDrag; }
			set {
				_allowUserDrag = value;
				isDirty = true;
			}
		}
		
		[SerializeField]
		bool
			_allowScrollOnScreenEdges = false;
		
		public bool	allowScrollOnScreenEdges {
			get { return _allowScrollOnScreenEdges; }
			set {
				_allowScrollOnScreenEdges = value;
				isDirty = true;
			}
		}
		
		[SerializeField]
		int
			_screenEdgeThickness = 2;
		
		public int	screenEdgeThickness {
			get { return _screenEdgeThickness; }
			set {
				_screenEdgeThickness = value;
				isDirty = true;
			}
		}
		
		[SerializeField]
		bool
			_centerOnRightClick = true;
		
		public bool	centerOnRightClick {
			get { return _centerOnRightClick; }
			set {
				_centerOnRightClick = value;
				isDirty = true;
			}
		}
		
		[SerializeField]
		bool
			_allowUserZoom = true;
		
		public bool allowUserZoom {
			get { return _allowUserZoom; }
			set {
				_allowUserZoom = value;
				isDirty = true;
			}
		}

		
		[SerializeField]
		float _zoomMaxDistance = 10f;
		public float zoomMaxDistance {
			get { return _zoomMaxDistance; }
			set {
				if (value != _zoomMaxDistance) {
					_zoomMaxDistance = value;
					isDirty = true;
				}
			}
		}
		
		[SerializeField]
		float _zoomMinDistance = 0.01f;
		public float zoomMinDistance {
			get { return _zoomMinDistance; }
			set {
				if (value != _zoomMinDistance) {
					_zoomMinDistance = Mathf.Clamp01(value);
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_invertZoomDirection = false;
		
		public bool invertZoomDirection {
			get { return _invertZoomDirection; }
			set {
				if (value != _invertZoomDirection) {
					_invertZoomDirection = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_respectOtherUI = true;
		
		/// <summary>
		/// When enabled, will prevent interaction with the map if pointer is over an UI element
		/// </summary>
		public bool	respectOtherUI {
			get { return _respectOtherUI; }
			set {
				if (value != _respectOtherUI) {
					_respectOtherUI = value;
					isDirty = true;
				}
			}
		}


		[SerializeField]
		[Range(0.1f, 3)]
		float
			_mouseWheelSensitivity = 0.5f;
		
		public float mouseWheelSensitivity {
			get { return _mouseWheelSensitivity; }
			set {
				_mouseWheelSensitivity = value;
				isDirty = true;
			}
		}

		[SerializeField]
		[Range(0.1f, 3)]
		float
			_mouseDragSensitivity = 0.5f;
		
		public float mouseDragSensitivity {
			get { return _mouseDragSensitivity; }
			set {
				_mouseDragSensitivity = value;
				isDirty = true;
			}
		}

	#endregion

	#region Public API area

		/// <summary>
		/// Moves the map in front of the camera so it fits the viewport.
		/// </summary>
		public void CenterMap () {
			if (_renderViewport == null)
				SetupViewport ();

			transform.localScale = new Vector3 (200, 100, 1);
			float fv = currentCamera.fieldOfView;
			float aspect = currentCamera.aspect; 
			float radAngle = fv * Mathf.Deg2Rad;
			float distance, frustumDistanceW, frustumDistanceH;
			if (currentCamera.orthographic) {
				if (_fitWindowHeight) {
					_currentCamera.orthographicSize = mapHeight * 0.5f * _windowRect.height;
					maxFrustumDistance = _currentCamera.orthographicSize;
				} else if (_fitWindowWidth) {
					_currentCamera.orthographicSize = mapWidth * 0.5f * _windowRect.width / aspect;
					maxFrustumDistance = _currentCamera.orthographicSize;
				} else {
					maxFrustumDistance = float.MaxValue;
				}
				distance = 1;

			} else {
				frustumDistanceH = mapHeight * _windowRect.height * 0.5f / Mathf.Tan (radAngle * 0.5f);
				frustumDistanceW = (mapWidth * _windowRect.width / aspect) * 0.5f / Mathf.Tan (radAngle * 0.5f);
				if (_fitWindowHeight) {
					distance = Mathf.Min (frustumDistanceH, frustumDistanceW);
					maxFrustumDistance = distance;
				} else if (_fitWindowWidth) {
					distance = Mathf.Max (frustumDistanceH, frustumDistanceW);
					maxFrustumDistance = distance;
				} else {
					distance = Vector3.Distance (transform.position, currentCamera.transform.position);
					maxFrustumDistance = float.MaxValue;
				}
			}
			transform.rotation = currentCamera.transform.rotation;
			transform.position = currentCamera.transform.position + currentCamera.transform.forward * distance;
		}


		
		/// <summary>
		/// Sets the zoom level
		/// </summary>
		/// <param name="zoomLevel">Value from 0 to 1</param>
		public void SetZoomLevel (float zoomLevel) {
			if (currentCamera.orthographic) {
				float aspect = currentCamera.aspect; 
				float frustumDistanceH;
				if (_fitWindowWidth) {
					frustumDistanceH = mapWidth * 0.5f / aspect;
				} else {
					frustumDistanceH = mapHeight * 0.5f;
				}
				zoomLevel = Mathf.Clamp01 (zoomLevel);
				currentCamera.orthographicSize = Mathf.Max (frustumDistanceH * zoomLevel, 1);
			} else {
				// Takes the distance from the focus point and adjust it according to the zoom level
				Vector3 dest;
				GetLocalHitFromScreenPos (new Vector3 (Screen.width * 0.5f, Screen.height * 0.5f), out dest);
				if (dest != Misc.Vector3zero) {
					dest = transform.TransformPoint (dest);
				} else {
					dest = transform.position;
				}
				float distance = GetZoomLevelDistance (zoomLevel);
				currentCamera.transform.position = dest - (dest - currentCamera.transform.position).normalized * distance;
				float minDistance = 0.01f * transform.localScale.y;
				float camDistance = (dest - currentCamera.transform.position).magnitude;
				// Last distance
				lastDistanceFromCamera = camDistance; 
				if (camDistance < minDistance) {
					currentCamera.transform.position = dest - transform.forward * minDistance;
				}
			}
		}

		/// <summary>
		/// Gets the current zoom level (0..1)
		/// </summary>
		public float GetZoomLevel () {
			float frustumDistanceW, frustumDistanceH;
			float aspect = currentCamera.aspect; 
			if (currentCamera.orthographic) {
				if (_fitWindowWidth) {
					frustumDistanceH = mapWidth * 0.5f / aspect;
				} else {
					frustumDistanceH = mapHeight * 0.5f;
				}
				return currentCamera.orthographicSize / frustumDistanceH;
			}

			float fv = currentCamera.fieldOfView;
			float radAngle = fv * Mathf.Deg2Rad;
			float distance;
			frustumDistanceH = mapHeight * 0.5f / Mathf.Tan (radAngle * 0.5f);
			frustumDistanceW = (mapWidth / aspect) * 0.5f / Mathf.Tan (radAngle * 0.5f);
			if (_fitWindowWidth) {
				distance = Mathf.Max (frustumDistanceH, frustumDistanceW);
			} else {
				distance = Mathf.Min (frustumDistanceH, frustumDistanceW);
			}
			// Takes the distance from the camera to the plane //focus point and adjust it according to the zoom level
			Plane plane = new Plane(transform.forward, transform.position);
			float distanceToCamera = Mathf.Abs (plane.GetDistanceToPoint(_currentCamera.transform.position));
			return distanceToCamera / distance;
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation (Vector2 destination) {
			FlyToLocation (destination, _navigationTime, GetZoomLevel());
		}
		
		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation (Vector2 destination, float duration) {
			FlyToLocation (destination, duration, GetZoomLevel());
		}
		
		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation (float x, float y) {
			FlyToLocation (new Vector2 (x, y), _navigationTime, GetZoomLevel());
		}
		
		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation (float x, float y, float duration) {
			SetDestination (new Vector2 (x, y), duration, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates with target zoom level.
		/// </summary>
		public void FlyToLocation (Vector2 destination, float duration, float zoomLevel) {
			SetDestination (destination, duration, zoomLevel);
		}


		#endregion

	}

}