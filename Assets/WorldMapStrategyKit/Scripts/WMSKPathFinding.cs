// World Strategy Kit for Unity - Main Script
// Copyright (C) Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WorldMapStrategyKit.PathFinding;

namespace WorldMapStrategyKit
{
	public delegate int OnPathFindingCrossPosition (Vector2 position);

	public partial class WMSK : MonoBehaviour
	{

		/// <summary>
		/// Fired when path finding algorithmn evaluates a cell. Return the increased cost for that map position.
		/// </summary>
		public event OnPathFindingCrossPosition OnPathFindingCrossPosition;

		#region Public properties

		[SerializeField]
		HeuristicFormula
			_pathFindingHeuristicFormula = HeuristicFormula.MaxDXDY;

		/// <summary>
		/// The path finding heuristic formula to estimate distance from current position to destination
		/// </summary>
		public HeuristicFormula pathFindingHeuristicFormula {
			get { return _pathFindingHeuristicFormula; }
			set {
				if (value != _pathFindingHeuristicFormula) {
					_pathFindingHeuristicFormula = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		int
			_pathFindingMaxCost = 2000;
		
		/// <summary>
		/// The maximum search cost of the path finding execution.
		/// </summary>
		public int pathFindingMaxCost {
			get { return _pathFindingMaxCost; }
			set {
				if (value != _pathFindingMaxCost) {
					_pathFindingMaxCost = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_pathFindingEnableCustomRouteMatrix = false;

		/// <summary>
		/// Enables user-defined location crossing costs for path finding engine.
		/// </summary>
		public bool pathFindingEnableCustomRouteMatrix {
			get { return _pathFindingEnableCustomRouteMatrix; }
			set { _pathFindingEnableCustomRouteMatrix = value;	}
		}

		/// <summary>
		/// Returns a copy of the current custom route matrix or set it.
		/// </summary>
		public int[] pathFindingCustomRouteMatrix {
			get { 
				int[] copy = new int[_customRouteMatrix.Length];
				Array.Copy(_customRouteMatrix, copy, _customRouteMatrix.Length);
				return copy;
			}
			set {
				_customRouteMatrix = value;
				if (finder!=null) finder.SetCustomRouteMatrix(_customRouteMatrix);
			}
		}

		#endregion

		#region Path finding APIs

		/// <summary>
		/// Returns an optimal route from startPosition to endPosition with options.
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="startPosition">Start position in map coordinates (-0.5...0.5)</param>
		/// <param name="endPosition">End position in map coordinates (-0.5...0.5)</param>
		/// <param name="terrainCapability">Type of terrain that the unit can pass through</param>
		/// <param name="minAltitude">Minimum altitude (0..1)</param>
		/// <param name="maxAltitude">Maximum altutude (0..1)</param>
		/// <param name="maxSearchCost">Maximum search cost for the path finding algorithm. A value of 0 will use the global default defined by pathFindingMaxCost</param>
		public List<Vector2> FindRoute (Vector2 startPosition, Vector2 endPosition, TERRAIN_CAPABILITY terrainCapability, float minAltitude, float maxAltitude, int maxSearchCost)
		{
			
			ComputeRouteMatrix (terrainCapability, minAltitude, maxAltitude);

			Point startingPoint = new Point ((int)((startPosition.x + 0.5f) * EARTH_ROUTE_SPACE_WIDTH), 
			                                (int)((startPosition.y + 0.5f) * EARTH_ROUTE_SPACE_HEIGHT));
			Point endingPoint = new Point ((int)((endPosition.x + 0.5f + 0.5f / EARTH_ROUTE_SPACE_WIDTH) * EARTH_ROUTE_SPACE_WIDTH), 
			                              (int)((endPosition.y + 0.5f + 0.5f / EARTH_ROUTE_SPACE_HEIGHT) * EARTH_ROUTE_SPACE_HEIGHT));
			List<Vector2> routePoints = null;
			
			// Minimum distance for routing?
			if (Mathf.Abs (endingPoint.X - startingPoint.X) > 0 || Mathf.Abs (endingPoint.Y - startingPoint.Y) > 0) {
				finder.Formula = _pathFindingHeuristicFormula;
				finder.SearchLimit = maxSearchCost == 0 ? _pathFindingMaxCost : maxSearchCost;
				if (_pathFindingEnableCustomRouteMatrix) {
					finder.OnCellCross = FindRoutePositionValidator;
				} else {
					finder.OnCellCross = null;
				}
				List<PathFinderNode> route = finder.FindPath (startingPoint, endingPoint);
				if (route != null) {
					routePoints = new List<Vector2> (route.Count);
					routePoints.Add (startPosition);
					for (int r=route.Count-1; r>=0; r--) {
						float x = (float)route [r].X / EARTH_ROUTE_SPACE_WIDTH - 0.5f;
						float y = (float)route [r].Y / EARTH_ROUTE_SPACE_HEIGHT - 0.5f;
						Vector2 stepPos = new Vector2 (x, y);
						
						// due to grid effect the first step may be farther than the current position, so we skip it in that case.
						if (r == route.Count - 1 && (endPosition - startPosition).sqrMagnitude < (endPosition - stepPos).sqrMagnitude)
							continue;
						
						routePoints.Add (stepPos);
						
						// Visualize route
						//						GameObject ng = GameObject.CreatePrimitive(PrimitiveType.Cube);
						//						GameObject.Destroy(ng.GetComponent<BoxCollider>());
						//						ng.GetComponent<Renderer>().material.color = Color.green;
						//						ng.hideFlags = HideFlags.HideInHierarchy;
						//						ng.WMSK_MoveTo( stepPos, 0 );
					}
				} else {
					return null;	// no route available
				}
			}
			
			// Add final step if it's appropiate
			bool hasWater = ContainsWater (endPosition);
			if (terrainCapability == TERRAIN_CAPABILITY.Any ||
				(terrainCapability == TERRAIN_CAPABILITY.OnlyWater && hasWater) ||
				(terrainCapability == TERRAIN_CAPABILITY.OnlyGround && !hasWater)) {
				if (routePoints == null) {
					routePoints = new List<Vector2> ();
					routePoints.Add (startPosition);
					routePoints.Add (endPosition);
				} else {
					routePoints [routePoints.Count - 1] = endPosition;
				}
			}
			return routePoints;
		}

		/// <summary>
		/// Resets the custom route matrix. Use this custom route matrix to set location customized costs.
		/// </summary>
		public void PathFindingCustomRouteMatrixReset ()
		{
			if (_customRouteMatrix == null)
				_customRouteMatrix = new int[EARTH_ROUTE_SPACE_WIDTH * EARTH_ROUTE_SPACE_HEIGHT];
			for (int k=0; k<_customRouteMatrix.Length; k++) {
				_customRouteMatrix [k] = -1;
			}
		}

		/// <summary>
		/// Sets the movement cost for a given map position.
		/// </summary>
		public void PathFindingCustomRouteMatrixSet (Vector2 position, int cost)
		{
			PathFindingCustomRouteMatrixSet (new List<Vector2> () { position }, cost);
		}

		/// <summary>
		/// Sets the movement cost for a list of map positions.
		/// </summary>
		public void PathFindingCustomRouteMatrixSet (List<Vector2> positions, int cost)
		{
			for (int p=0; p<positions.Count; p++) {
				Point point = PositionToPoint (positions [p]);
				if (point.X < 0 || point.X >= EARTH_ROUTE_SPACE_WIDTH || point.Y < 0 || point.Y >= EARTH_ROUTE_SPACE_HEIGHT)
					return;
				int location = PointToLocation (point);
				_customRouteMatrix [location] = cost;
			}
		}

		
		/// <summary>
		/// Sets the movement cost for a region of the map.
		/// </summary>

		public void PathFindingCustomRouteMatrixSet (Region region, int cost)
		{
			if (region.pathFindingPositions == null) {
				Rect rect = region.rect2D;
				Point start = PositionToPoint (new Vector2 (rect.xMin, rect.yMin));
				Point end = PositionToPoint (new Vector2 (rect.xMax, rect.yMax));
				region.pathFindingPositions = new List<int> ();
				for (int j=start.Y; j<=end.Y; j++) {
					float y = (float)j / EARTH_ROUTE_SPACE_HEIGHT - 0.5f;
					int yy = j * EARTH_ROUTE_SPACE_WIDTH;
					for (int k=start.X; k<=end.X; k++) {
						int pos = yy + k;
						if (_customRouteMatrix [pos] != cost) {
							float x = (float)k / EARTH_ROUTE_SPACE_WIDTH - 0.5f;
							Vector2 position = new Vector2 (x, y);
							if (region.ContainsPoint (position)) {
								_customRouteMatrix [pos] = cost;
								region.pathFindingPositions.Add (pos);
							}
						}
					}
				}
			} else {
				int maxk = region.pathFindingPositions.Count;
				for (int k=0; k<maxk; k++) {
					int position = region.pathFindingPositions [k];
					_customRouteMatrix [position] = cost;
				}
			}
		}

		/// <summary>
		/// Sets the movement cost for a country.
		/// </summary>
		public void PathFindingCustomRouteMatrixSet (Country country, int cost)
		{
			for (int r=0; r<country.regions.Count; r++) {
				PathFindingCustomRouteMatrixSet (country.regions [r], cost);
			}
		}

		/// <summary>
		/// Sets the movement cost for a province.
		/// </summary>
		public void PathFindingCustomRouteMatrixSet (Province province, int cost)
		{
			for (int r=0; r<province.regions.Count; r++) {
				PathFindingCustomRouteMatrixSet (province.regions [r], cost);
			}
		}

		/// <summary>
		/// Returns the indices of the provinces the path is crossing.
		/// </summary>
		public List<int>PathFindingGetProvincesInPath(List<Vector2>path) {
			List<int> provincesIndices = new List<int>();
			for (int k=0;k<path.Count;k++) {
				int provinceIndex = GetProvinceIndex(path[k]);
				if (provinceIndex>=0 && !provincesIndices.Contains(provinceIndex)) provincesIndices.Add (provinceIndex);
			}
			return provincesIndices;
		}
		
		/// <summary>
		/// Returns the indices of the provinces the path is crossing.
		/// </summary>
		public List<int>PathFindingGetCountriesInPath(List<Vector2>path) {
			List<int> countriesIndices = new List<int>();
			for (int k=0;k<path.Count;k++) {
				int countryIndex = GetCountryIndex(path[k]);
				if (countryIndex>=0 && !countriesIndices.Contains(countryIndex)) countriesIndices.Add (countryIndex);
			}
			return countriesIndices;
		}

		/// <summary>
		/// Returns the indices of the cities the path is crossing.
		/// </summary>
		public List<int>PathFindingGetCitiesInPath(List<Vector2>path) {
			List<int> citiesIndices = new List<int>();
			for (int k=0;k<path.Count;k++) {
				int countryIndex = GetCountryIndex(path[k]);
				int cityIndex = GetCityNearPoint(path[k], countryIndex);
				if (cityIndex>=0 && !citiesIndices.Contains(cityIndex)) citiesIndices.Add (cityIndex);
			}
			return citiesIndices;
		}

		/// <summary>
		/// Returns the indices of the mount points the path is crossing.
		/// </summary>
		public List<int>PathFindingGetMountPointsInPath(List<Vector2>path) {
			List<int> mountPointsIndices = new List<int>();
			for (int k=0;k<path.Count;k++) {
				int mountPointIndex = GetMountPointNearPoint(path[k]);
				if (mountPointIndex>=0 && !mountPointsIndices.Contains(mountPointIndex)) mountPointsIndices.Add (mountPointIndex);
			}
			return mountPointsIndices;
		}


		#endregion

	}

}