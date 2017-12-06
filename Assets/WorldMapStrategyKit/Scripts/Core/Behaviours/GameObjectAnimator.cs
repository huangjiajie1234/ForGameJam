using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {

	public enum EASE_TYPE {
		Linear = 0,
		EaseOut = 1,
		EaseIn = 2,
		Exponential = 3,
		SmoothStep = 4,
		SmootherStep = 5
	}

	
	public delegate void OnMoveStart(GameObjectAnimator anim);
	public delegate void OnMoveEnd(GameObjectAnimator anim);

	public class GameObjectAnimator : MonoBehaviour, IExtendableAttribute {
	
		const float SPEED_FACTOR = 1f/2f; // all durations are multiplied by this factor. Useful to adjust global speed movement of units.

		/// <summary>
		/// User defined value. Could be the unit or resource type.
		/// </summary>
		public int type;

		/// <summary>
		/// User-defined value for unit grouping. Some methogs accepts this value, like ToggleGroupVisibility method.
		/// </summary>
		public int group;

		/// <summary>
		/// User-defined value to specify to which player belongs this unit.
		/// </summary>
		public int player;

		/// <summary>
		/// Returns true if the gameobject is visible inside the viewport.
		/// </summary>
		public bool isVisibleInViewport;

		/// <summary>
		/// Current destination if it's moving.
		/// </summary>
		public Vector2 destination;

		/// <summary>
		/// Duration of the translation if it's moving.
		/// </summary>
		public float duration;

		/// <summary>
		/// Ease type for the movement. Default = Linear.
		/// </summary>
		public EASE_TYPE easeType = EASE_TYPE.Linear;

		/// <summary>
		/// If the gameobject is moving across the map.
		/// </summary>
		public bool isMoving;

		/// <summary>
		/// Current map local coordinate of the gameobject (x,y in the range of -0.5...0.5)
		/// </summary>
		public Vector2 currentMap2DLocation;

		/// <summary>
		/// Height from ground of the gameobject. A zero means object is grounded.
		/// </summary>
		public float height;

		/// <summary>
		/// Height mode. See HEIGHT_OFFSET_MODE possible values.
		/// </summary>
		public HEIGHT_OFFSET_MODE heightMode = HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED;

		/// <summary>
		/// Whether the gameobject will scale automatically when user zooms in/out into the viewport
		/// </summary>
		public bool autoScale = true;

		/// <summary>
		/// If the camera should focus and follow this game object while moving
		/// </summary>
		public bool follow = false;

		/// <summary>
		/// The follow zoom level of the camera (0..1).
		/// </summary>
		public float followZoomLevel = 0.1f;

		/// <summary>
		/// When the game object moves, it will rotate around the map Y axis towards the movement direction effectively adapting to the terrain contour. Use this only for moveable units.
		/// </summary>
		public bool autoRotation = false;

		/// <summary>
		/// If set to true, the game object will maintain it's current rotation when moved over the viewport. Note that autoRotation overrides this property, so if you set autoRotation = true, preserveOriginalRotation will be ignored.
		/// </summary>
		public bool preserveOriginalRotation = false;

		/// <summary>
		/// The auto-rotation speed.
		/// </summary>
		public float rotationSpeed = 0.1f;

		/// <summary>
		/// Type of terrain the unit can pass through.
		/// </summary>
		public TERRAIN_CAPABILITY terrainCapability = TERRAIN_CAPABILITY.Any;

		/// <summary>
		/// Minimun altitude this unit move over (0..1).
		/// </summary>
		public float minAltitude = 0.0f;

		/// <summary>
		/// Maximum altitude this unit move over (0..1).
		/// </summary>
		public float maxAltitude = 1.0f;

		/// <summary>
		/// The max search cost for the path finding algorithm. A value of zero will use the global default max defined by pathFindingMaxCost
		/// </summary>
		public int maxSearchCost = 0;

		/// <summary>
		/// A user-defined unique identifier useful to get a quick reference to this GO with VGOGet method.
		/// </summary>
		public int uniqueId { get; set; }
		
		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		public JSONObject attrib { get; set; }

		/// <summary>
		/// Specifies the Y coordinate of the pivot. If pivot is at bottom of the game object, you don't have to change this value (0). But if the pivot is at center, set this value to 0.5f. If pivot is at top, set this value to 1f.
		/// </summary>
		public float pivotY;


		/// <summary>
		/// Fired when this GO starts moving
		/// </summary>
		public event OnMoveStart OnMoveStart;

		/// Fired when this GO ends movement
		public event OnMoveEnd OnMoveEnd;


		#region internal fields

		float startingTime;		// moment when move is issued
		float stepTime;			// moment of current step animation (1- rotating, 2- translating)
		Vector2 startingMap2DLocation, endingMap2DLocation;
		Vector3 startingScale;
		Quaternion targetDirection;
		Vector3 destinationDirection;
		WMSK map;
		List<Vector2> route;
		int routeNextDestinationIndex;

		#endregion


		void Awake() {
			map = WMSK.instance;
			attrib = new JSONObject();
		}

		/// <summary>
		/// Initiates a movement for this Gameobject.
		/// </summary>
		/// <returns><c>true</c>, if move is possible, <c>false</c> otherwise.</returns>
		public bool MoveTo(Vector2 destination, float duration) {
			if (terrainCapability!= TERRAIN_CAPABILITY.Any) {
				// Get a route
				List<Vector2> route = FindRoute(destination);
				if (route==null) return false;
				return MoveTo (route, duration);
			} else {
				ClearOptions();
				this.endingMap2DLocation = destination;
				StartCoroutine(StartMove(destination, duration));
			}
			return true;
		}

		/// <summary>
		/// Initiates a movement for this Gameobject along a predefined route.
		/// </summary>
		public bool MoveTo(List<Vector2> route, float stepDuration) {
			ClearOptions();
			this.route = route;
			if (route==null) return false;
			routeNextDestinationIndex = 1;
			this.destination = route.Count>1 ? route[1]: route[0];
			this.endingMap2DLocation = route[route.Count-1];
			StartCoroutine(StartMove(destination, stepDuration * route.Count * SPEED_FACTOR));
			return true;
		}

		/// <summary>
		/// Returns a potential movement path from current position to destination with unit terrain capabilities
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="destination">Destination.</param>
		public List<Vector2> FindRoute(Vector2 destination) {
			List<Vector2> route = map.FindRoute(currentMap2DLocation, destination, terrainCapability, minAltitude, maxAltitude, maxSearchCost);
			return route;
		}

		void ClearOptions() {
			route = null;
		}

		/// <summary>
		/// Initiates a movement for this Gameobject.
		/// </summary>
		IEnumerator StartMove(Vector2 destination, float duration) {
			yield return new WaitForEndOfFrame();

			if (map==null) {
				Awake ();
			}

			this.duration = duration;
			this.destination = destination;
			this.startingTime = Time.time;
			this.stepTime = this.startingTime;
			isMoving = true;

			if (map.VGOIsRegistered(this)) {
				this.startingMap2DLocation = currentMap2DLocation;
			} else {
				// Register the gameobject for terrain updates
				map.VGORegisterGameObject(this);
				this.startingMap2DLocation = map.WorldToMap2DPosition(transform.position);
				this.startingScale = transform.localScale;

				FixedUpdate();
				
				// Reset current object visibility
				UpdateVisibility (true);
			}

			SetupDirection(destination);

			if (OnMoveStart!=null) OnMoveStart(this);
		}

		void SetupDirection(Vector3 nextStop) {
			if (autoRotation) {
				Vector3 worldPosDestination = map.Map2DToWorldPosition(nextStop, height, heightMode, false);
				destinationDirection = worldPosDestination - transform.position;
				if (destinationDirection == Misc.Vector3zero) {
					worldPosDestination = map.Map2DToWorldPosition(endingMap2DLocation, height, heightMode, false);
					destinationDirection = worldPosDestination - transform.position;
				}
			}
		}

		// Updates game object position
		void FixedUpdate () {
			if (!isMoving) return;

			MoveGameObject();

			UpdateTransformAndVisibility();

			// Follow object?
			if (follow) {
				float t = Lerp.EaseOut(Time.time - startingTime);
				float zoomLevel = Mathf.Lerp (map.GetZoomLevel(), followZoomLevel, t);
				Vector2 loc = Vector2.Lerp (map.transform.InverseTransformPoint(map.currentCamera.transform.position), currentMap2DLocation, t);
				map.FlyToLocation(loc, 0, zoomLevel);
			}
		}

		void MoveGameObject() {
			bool canMove = true;
			float elapsed = Time.time - stepTime;
			
			// Lerp translate
			float t = 0;
			if (elapsed>=duration || duration<=0) {
				t = 1.0f;
				isMoving = false;
				follow = false;
				if (OnMoveEnd!=null) OnMoveEnd(this);
			} else {
				
				// Check if GO needs to rotate towards destination first
				if (autoRotation && isVisibleInViewport) {
					Vector3 projDest = Vector3.ProjectOnPlane(destinationDirection, transform.up);
					Vector3 projForward = Vector3.ProjectOnPlane(transform.forward, transform.up);
					float angle = Vector3.Angle (projDest, projForward);
					if (angle>45.0f) {	// prevents movement until rotation has finished
						stepTime += Time.deltaTime;
						canMove = false;
					}
				}
				if (elapsed>0) t = GetLerpT(elapsed/duration);
			}
			
			// Update position and visibility
			if (route!=null) {
				int index = (int)( (route.Count-1) * t);
				int findex =  Mathf.Min (index +1, route.Count-1);
				if (routeNextDestinationIndex!=findex) {
					routeNextDestinationIndex = findex;
					SetupDirection(route[findex]);
				}
				if (canMove) {
					t *= (route.Count-1);
					t -= (int)t;
					currentMap2DLocation = Vector2.Lerp(route[index], route[findex], t);
				}
			} else { 
				if (canMove) {
					currentMap2DLocation = Vector2.Lerp(startingMap2DLocation, destination, t);
				}
			}
		}

		float GetLerpT(float t) {
			switch(easeType) {
			case EASE_TYPE.EaseIn:
				return Lerp.EaseIn(t);
			case EASE_TYPE.EaseOut:
				return Lerp.EaseOut(t);
			case EASE_TYPE.Exponential:
				return Lerp.Exponential(t);
			case EASE_TYPE.SmoothStep:
				return Lerp.SmoothStep(t);
			case EASE_TYPE.SmootherStep:
				return Lerp.SmootherStep(t);
			}
			return t;
		}

		/// <summary>
		/// Updates GO's position, rotation, scale and visibility according to its current map position and direction
		/// </summary>
		public void UpdateTransformAndVisibility() {

			// Adjust scale
			if (autoScale) {
				float desiredScale = map.renderViewportScaleFactor * map.renderViewportGOAutoScaleMultiplier;
				transform.localScale = this.startingScale * Mathf.Clamp(desiredScale, map.renderViewportGOAutoScaleMin, map.renderViewportGOAutoScaleMax);
			}	

			// Adjust world position having into account terrain elevation
			float h = height + transform.localScale.y * pivotY;
			Vector3 worldPos = map.Map2DToWorldPosition(currentMap2DLocation, h, heightMode, false);
			transform.position = worldPos;

			// if gameobject is not inside the viewport area, hide it's renderers
			UpdateVisibility(false);

			// Make it climb up/down the slope
			if (isVisibleInViewport) {
				// if not autorotates according to terrain, align it with the renderViewport rotation
				if (!autoRotation && !preserveOriginalRotation) {
					Vector3 rva = map.renderViewport.transform.rotation.eulerAngles;
					transform.rotation = Quaternion.Euler(new Vector3(rva.x + 180.0f, rva.y, rva.z));
				// otherwise, calculate the normal and align to it
				} else if (isMoving && autoRotation) {
					Vector3 normal;
					if (map.RenderViewportGetNormal(worldPos, out normal)) {
						Quaternion currentRotation = transform.rotation;
						// Orient to slope
						transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(90,0,0);
						// Head to target
						if (destinationDirection!=Misc.Vector3zero) {
							Quaternion destRotation = Quaternion.LookRotation(destinationDirection, normal);
						transform.rotation = Quaternion.Slerp(currentRotation, destRotation, rotationSpeed);
						}
					}
				}
			}

		}

		public void UpdateVisibility(bool forceRefreshVisibility) {
			bool shouldBeVisible = map.renderViewportRect.Contains(currentMap2DLocation) && gameObject.activeSelf;
			if (shouldBeVisible!=isVisibleInViewport || forceRefreshVisibility) {
				isVisibleInViewport = shouldBeVisible;
				Renderer[] rr = GetComponentsInChildren<Renderer>();
				for (int rrk=0;rrk<rr.Length;rrk++) rr[rrk].enabled = isVisibleInViewport;
			}
		}

	}

}