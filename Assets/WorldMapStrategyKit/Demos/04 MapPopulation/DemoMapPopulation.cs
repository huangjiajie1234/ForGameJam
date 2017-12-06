using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit;

namespace WorldMapStrategyKitDemo {
	public class DemoMapPopulation : MonoBehaviour {

		enum UNIT_TYPE {
			TANK = 1,
			SHIP = 2
		}

		WMSK map;
		GUIStyle labelStyle, labelStyleShadow, buttonStyle;
		bool enableAddTowerOnClick, enableClickToMoveTank, enableClickToMoveShip;
		GameObjectAnimator tank, ship;
		List<GameObjectAnimator> units;

		void Start () {
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;
			labelStyleShadow = new GUIStyle (labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle (labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;

			// setup GUI resizer - only for the demo
			GUIResizer.Init (800, 500); 

			map.OnClick += (float x, float y) => { 
				if (enableAddTowerOnClick)
					AddTowerAtPosition (x, y);
				else if (enableClickToMoveTank) {
					MoveTankWithPathFinding (new Vector2 (x, y));
				} else if (enableClickToMoveShip) {
					// as ship has terrainCapability set to Water we just need to call one function to make it go over there along an optimal path
					ship.MoveTo (new Vector2 (x, y), 0.1f);
				}
			};

			map.CenterMap ();
		}
	
		
		// Executes on each frame - move ships and tanks around
		void Update () {
			
			Vector2 destination;
			if (units != null) {
				for (int k=0; k<units.Count; k++) {
					if (!units [k].isMoving) {
						if (units [k].type == (int)UNIT_TYPE.TANK) {
							destination = GetRandomCityPosition ();
						} else {	// it's a ship
							destination = GetRandomWaterPosition ();
						}
						units [k].MoveTo (destination, 0.1f);
					}
				}
			}
		}


		/// <summary>
		/// UI Buttons
		/// </summary>
		void OnGUI () {
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize ();

			GUI.Box (new Rect (0, 0, 160, 160), "");

			bool prev = enableAddTowerOnClick;
			enableAddTowerOnClick = GUI.Toggle (new Rect (10, 20, 150, 30), enableAddTowerOnClick, "Enable Tower On Click");
			if (enableAddTowerOnClick && prev != enableAddTowerOnClick) {
				enableClickToMoveTank = false;
				enableClickToMoveShip = false;
			}

			prev = enableClickToMoveTank;
			enableClickToMoveTank = GUI.Toggle (new Rect (180, 20, 200, 30), enableClickToMoveTank, "Enable Move Tank On Click");
			if (enableClickToMoveTank && prev != enableClickToMoveTank) {
				enableAddTowerOnClick = false;
				enableClickToMoveShip = false;
			}

			prev = enableClickToMoveShip;
			enableClickToMoveShip = GUI.Toggle (new Rect (390, 20, 200, 30), enableClickToMoveShip, "Enable Move Ship On Click");
			if (enableClickToMoveShip && prev != enableClickToMoveShip) {
				enableAddTowerOnClick = false;
				enableClickToMoveTank = false;
			}

			// buttons background color
			GUI.backgroundColor = new Color (0.1f, 0.1f, 0.3f, 0.95f);

			if (GUI.Button (new Rect (10, 50, 150, 30), "  Add Random Tower", buttonStyle)) {
				AddRandomTower ();
			}

			if (GUI.Button (new Rect (10, 90, 150, 30), "  Add Random Sprite", buttonStyle)) {
				AddRandomSprite ();
			}

			if (GUI.Button (new Rect (10, 130, 150, 30), "  Drop Tank on Paris", buttonStyle)) {
				DropTankOnCity ();
			}

			if (GUI.Button (new Rect (10, 170, 150, 30), "  Move Tank & Follow", buttonStyle)) {
				MoveTankAndFollow ();
			}

			if (GUI.Button (new Rect (10, 210, 150, 30), "  Launch Ship", buttonStyle)) {
				LaunchShip ();
			}

			if (GUI.Button (new Rect (GUIResizer.authoredScreenWidth - 190, 50, 180, 30), "  Mass Create", buttonStyle)) {
				MassCreate ();
			}

			if (GUI.Button (new Rect (GUIResizer.authoredScreenWidth - 190, 90, 180, 30), "  Find Coast", buttonStyle)) {
				FindCoast ();
			}

			if (GUI.Button (new Rect (GUIResizer.authoredScreenWidth - 190, 130, 180, 30), "  Add Sphere", buttonStyle)) {
				AddSphere ();
			}


		}

		/// <summary>
		/// Creates a tower instance and adds it to the map at a random city
		/// </summary>
		void AddRandomTower () {

			// Get a random big city
			int cityIndex = -1;
			do {
				cityIndex = Random.Range (0, map.cities.Count);
			} while (map.cities[cityIndex].population<10000);

			// Get city location
			Vector2 cityPosition = map.cities [cityIndex].unity2DLocation;

			// Create tower and add it to the map
			AddTowerAtPosition (cityPosition.x, cityPosition.y);

			// Fly to the location with provided zoom level
			map.FlyToLocation (cityPosition, 2.0f, 0.1f);
		}


		/// <summary>
		/// Creates a sprite instance and adds it to the map at a random city
		/// </summary>
		void AddRandomSprite () {
			
			// Get a random big city
			int cityIndex = -1;
			do {
				cityIndex = Random.Range (0, map.cities.Count);
			} while (map.cities[cityIndex].population<10000);
			
			// Get city location
			Vector2 cityPosition = map.cities [cityIndex].unity2DLocation;

			AddRandomSpriteAtPosition(cityPosition);

			// Fly to the location with provided zoom level
			map.FlyToLocation (cityPosition, 2.0f, 0.1f);
		}

		void AddRandomSpriteAtPosition(Vector2 position) {

			// Instantiate the sprite, face it to up and position it into the map
			GameObject star = Instantiate (Resources.Load<GameObject> ("StarSprite/StarSprite"));
			star.transform.localRotation = Quaternion.Euler (90, 0, 0);
			star.transform.localScale = Misc.Vector3one * 2;
			star.WMSK_MoveTo (position, 0f, 0.2f, HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND, true);
		}


		/// <summary>
		/// Creates a tower instance and adds it to given coordinates
		/// </summary>
		void AddTowerAtPosition (float x, float y) {
			// Instantiate game object and position it instantly over the city
			GameObject tower = Instantiate (Resources.Load<GameObject> ("Tower/Tower"));
			tower.WMSK_MoveTo (x, y);
		}

		/// <summary>
		/// Creates a tank instance and adds it to specified city
		/// </summary>
		void DropTankOnCity () {

			// Get a random big city
			int cityIndex = map.GetCityIndex ("Paris", "France");

			// Get city location
			Vector2 cityPosition = map.cities [cityIndex].unity2DLocation;

			if (tank != null)
				DestroyImmediate (tank.gameObject);
			tank = DropTankOnPosition (cityPosition);

			// Zoom into tank
			map.FlyToLocation (cityPosition, 2.0f, 0.15f);

			// Enable move on click in this demo
			enableAddTowerOnClick = false;
			enableClickToMoveShip = false;
			enableClickToMoveTank = true;
		}

		// Create tank instance and add it to the map
		GameObjectAnimator DropTankOnPosition (Vector2 mapPosition) {
			GameObject tankGO = Instantiate (Resources.Load<GameObject> ("Tank/CompleteTank"));
			tank = tankGO.WMSK_MoveTo (mapPosition);
			tank.type = (int)UNIT_TYPE.TANK;
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			return tank;
		}

		/// <summary>
		/// Checks if tank is near Paris. Then moves it to Moscow. Otherwise, moves it back to Paris.
		/// </summary>
		void MoveTankAndFollow () {
			string destinationCity, destinationCountry;
			if (tank == null)
				DropTankOnCity ();

			// Gets position of Paris in map
			Vector2 parisPosition = map.GetCity ("Paris", "France").unity2DLocation;

			// Is the tank nearby (less than 50 km)? Then set destination to Moscow, otherwize Paris again
			if (map.calc.Distance (tank.currentMap2DLocation, parisPosition) < 50000) {
				destinationCity = "Moscow";
				destinationCountry = "Russia";
			} else {
				destinationCity = "Paris";
				destinationCountry = "France";
			}

			// Get position of destination
			Vector2 destination = map.GetCity (destinationCity, destinationCountry).unity2DLocation;

			// For this movement, we will move the tank following a straight line
			tank.terrainCapability = TERRAIN_CAPABILITY.Any;

			// Move the tank to the new position with smooth ease
			tank.easeType = EASE_TYPE.SmoothStep;

			// Use a close zoom during follow - either current zoom level or 0.1f maximum so tank is watched closely
			tank.follow = true;
			tank.followZoomLevel = Mathf.Min (0.1f, map.GetZoomLevel ());

			// Move it!
			tank.MoveTo (destination, 4.0f);

			// Finally, signal me when tank stops
			tank.OnMoveEnd += (thisTank) => Debug.Log ("Tank has stopped at " + thisTank.currentMap2DLocation + " location.");
		}

		/// <summary>
		/// Moves the tank with path finding.
		/// </summary>
		void MoveTankWithPathFinding (Vector2 destination) {
			// Ensure tank is limited terrain, avoid water
			if (tank == null) {
				DropTankOnCity ();
				return;
			}
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			tank.MoveTo (destination, 0.1f);
		}

		/// <summary>
		/// Creates ship. Main function called from button UI.
		/// </summary>
		void LaunchShip () {

			// Get a coastal city and a water entrypoint
			int cityIndex = Random.Range (0, map.cities.Count);
			Vector2 cityPosition;
			Vector2 waterPosition = Misc.Vector2zero;
			int safeAbort = 0;
			do {
				cityIndex++;
				if (cityIndex >= map.cities.Count)
					cityIndex = 0;
				cityPosition = map.cities [cityIndex].unity2DLocation;
				if (safeAbort++ > 8000)
					break;
			} while (!map.ContainsWater(cityPosition, 0.0001f, out waterPosition));

			if (safeAbort > 8000)
				return;

			// Create ship
			if (ship != null)
				DestroyImmediate (ship.gameObject);
			ship = DropShipOnPosition (waterPosition);

			// Fly to the location of ship with provided zoom level
			map.FlyToLocation (waterPosition, 2.0f, 0.1f);

			// Enable move on click in this demo
			enableAddTowerOnClick = false;
			enableClickToMoveTank = false;
			enableClickToMoveShip = true;
		}

		/// <summary>
		/// Creates a new ship on position.
		/// </summary>
		GameObjectAnimator DropShipOnPosition (Vector2 position) {

			// Create ship
			GameObject shipGO = Instantiate (Resources.Load<GameObject> ("Ship/VikingShip"));
			ship = shipGO.WMSK_MoveTo (position);
			ship.type = (int)UNIT_TYPE.SHIP;
			ship.terrainCapability = TERRAIN_CAPABILITY.OnlyWater;
			ship.autoRotation = true;
			return ship;
		}

		/// <summary>
		/// Returns a random position on water.
		/// </summary>
		Vector2 GetRandomWaterPosition () {
			
			// Get a coastal city and a water entrypoint
			Vector2 waterPosition = Misc.Vector2zero;
			int safeAbort = 0;
			do {
				waterPosition = new Vector2 (Random.value - 0.5f, Random.value - 0.5f);
				if (safeAbort++ > 10)
					break;
			} while (!map.ContainsWater(waterPosition));
			return waterPosition;
		}

		Vector2 GetRandomCityPosition () {
			int cityIndex = Random.Range (0, map.cities.Count);
			return map.cities [cityIndex].unity2DLocation;
		}

		/// <summary>
		/// Creates lots of ships and tanks. Called from main UI button.
		/// </summary>
		void MassCreate () {

			int numberOfUnits = 50;
			units = new List<GameObjectAnimator> ();
			// add tanks
			for (int k=0; k<numberOfUnits; k++) {
				Vector2 cityPosition = GetRandomCityPosition ();
				GameObjectAnimator newTank = DropTankOnPosition (cityPosition);
				newTank.gameObject.hideFlags = HideFlags.HideInHierarchy; // don't show in hierarchy to avoid clutter
				units.Add (newTank);
			} 
			// add ships
			for (int k=0; k<numberOfUnits; k++) {
				Vector2 waterPosition = GetRandomWaterPosition ();
				GameObjectAnimator newShip = DropShipOnPosition (waterPosition);
				newShip.gameObject.hideFlags = HideFlags.HideInHierarchy; // don't show in hierarchy to avoid clutter
				units.Add (newShip);
			} 
		}

		/// <summary>
		/// Locates coastal points for a sample country and add custom sprites over that line
		/// </summary>
		void FindCoast() {

			int franceIndex = map.GetCountryIndex("France");
			List<Vector2> points = map.GetCountryCoastalPoints(franceIndex);
			points.ForEach( (point) => AddRandomSpriteAtPosition(point));
			if (points.Count>0) map.FlyToLocation(points[0], 2, 0.2f);
		}


		/// <summary>
		/// This function adds a standard sphere primitive to the map. The difference here is that the pivot of the sphere is centered in the sphere. So we make use of pivotY property to specify it and
		/// this way the positioning over the terrain will work. Otherwise, the sphere will be cut by the terrain (the center of the sphere will be on the ground - and we want the sphere on top of the terrain).
		/// </summary>
		void AddSphere() {

			GameObject sphere = Instantiate(Resources.Load<GameObject> ("Sphere/Sphere"));
			Vector2 position = map.GetCity("Lhasa", "China").unity2DLocation;
			GameObjectAnimator anim = sphere.WMSK_MoveTo(position, 0, 0, HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND, true);
			anim.pivotY = 0.5f;
			map.FlyToLocation(position, 2f, 0.1f);

		}

	}

}

