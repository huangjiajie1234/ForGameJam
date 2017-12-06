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
using WorldMapStrategyKit.PathFinding;

namespace WorldMapStrategyKit {
	public enum TERRAIN_CAPABILITY {
		Any=1,
		OnlyGround=2,
		OnlyWater=4
	}

	public partial class WMSK : MonoBehaviour {

		byte[] earthRouteMatrix;	// bit 1 for custom elevation, bit 2 for ground without elevation restrictions, bit 3 for water without elevation restrictions
		int[] _customRouteMatrix;	// optional values for custom validation
		float earthRouteMatrixWithElevationMinAltitude, earthRouteMatrixWithElevationMaxAltitude;
		byte computedMatrixBits;

		BitArray earthWaterMask;
		const byte EARTH_WATER_MASK_OCEAN_LEVEL_MAX_ALPHA = 16;	// A lower alpha value in texture means water

		int earthWaterMaskWidth, earthWaterMaskHeight;
		int EARTH_ROUTE_SPACE_WIDTH = 2048;	// both must be power of 2
		int EARTH_ROUTE_SPACE_HEIGHT = 1024;
		PathFinderFast finder;
		int lastMatrix;


		void PathFindingPrewarm() {
			CheckRouteWaterMask();
			ComputeRouteMatrix(TERRAIN_CAPABILITY.OnlyGround, 0, 1.0f);
			ComputeRouteMatrix(TERRAIN_CAPABILITY.OnlyWater, 0, 1.0f);
		}

		// Returns true if water mask buffer has been created; false if it was already created
		bool CheckRouteWaterMask () {

			if (earthWaterMask != null)
				return false;

			// Get water mask info
			Texture2D waterMap;
			if (_earthStyle.isScenicPlus()) {
				waterMap = (Texture2D)earthMat.GetTexture("_TerrestrialMap");
			} else {
				waterMap = Resources.Load<Texture2D> ("WMSK/Textures/EarthScenicPlusMap8k");
			}
			earthWaterMaskHeight = waterMap.height;
			earthWaterMaskWidth = waterMap.width;
			Color32[] colors = waterMap.GetPixels32 ();
			int pixelCount = colors.Length;
			earthWaterMask = new BitArray(pixelCount);
			for (int k=0; k<pixelCount; k++) {
				earthWaterMask [k] = colors [k].a < EARTH_WATER_MASK_OCEAN_LEVEL_MAX_ALPHA;
			}
			return true;
		}

		void ComputeRouteMatrix (TERRAIN_CAPABILITY terrainCapability, float minAltitude, float maxAltitude) {

			bool computeMatrix = false;
			byte thisMatrix = 1;

			// prepare matrix
			if (earthRouteMatrix == null) {
				earthRouteMatrix = new byte[EARTH_ROUTE_SPACE_WIDTH * EARTH_ROUTE_SPACE_HEIGHT];
				computedMatrixBits = 0;
			}

			// prepare water mask data
			bool checkWater = terrainCapability != TERRAIN_CAPABILITY.Any;
			if (checkWater) computeMatrix = CheckRouteWaterMask ();

			// check elevation data if needed
			bool checkElevation = minAltitude > 0f || maxAltitude < 1.0f;
			if (checkElevation) {
				if (viewportElevationPoints == null) {
					Debug.LogError ("Viewport needs to be initialized before calling using Path Finding functions.");
					return;
				}
				if (minAltitude != earthRouteMatrixWithElevationMinAltitude || maxAltitude != earthRouteMatrixWithElevationMaxAltitude)	{
					computeMatrix = true;
					earthRouteMatrixWithElevationMinAltitude = minAltitude;
					earthRouteMatrixWithElevationMaxAltitude = maxAltitude;
				}
			} else {
				if (terrainCapability == TERRAIN_CAPABILITY.OnlyGround) thisMatrix = 2; else thisMatrix = 4;
				if ((computedMatrixBits & thisMatrix) == 0) {
					computeMatrix = true;
					computedMatrixBits |= thisMatrix;	// mark computedMatrixBits
				}
			}

			// Compute route
			if (computeMatrix) {
//			int count = 0;
				int jj_waterMask = 0, kk_waterMask;
				int jj_terrainElevation = 0, kk_terrainElevation;
				bool dry = false;
				float elev = 0;
				for (int j=0; j<EARTH_ROUTE_SPACE_HEIGHT; j++) {
					int jj = j * EARTH_ROUTE_SPACE_WIDTH;
					if (checkWater)
						jj_waterMask = (int)((j * (float)earthWaterMaskHeight / EARTH_ROUTE_SPACE_HEIGHT)) * earthWaterMaskWidth;
					if (checkElevation)
						jj_terrainElevation = ((int)(j * (float)EARTH_ELEVATION_HEIGHT / EARTH_ROUTE_SPACE_HEIGHT)) * EARTH_ELEVATION_WIDTH;
					for (int k=0; k<EARTH_ROUTE_SPACE_WIDTH; k++) {
						bool setBit = false;
						// Check altitude
						if (checkElevation) {
							kk_terrainElevation = (int)(k * (float)EARTH_ELEVATION_WIDTH / EARTH_ROUTE_SPACE_WIDTH);
							elev = viewportElevationPoints [jj_terrainElevation + kk_terrainElevation];
						}
						if (elev >= minAltitude && elev <= maxAltitude) {
							if (checkWater) {
								kk_waterMask = (int)(k * (float)earthWaterMaskWidth / EARTH_ROUTE_SPACE_WIDTH);
								dry = !earthWaterMask [jj_waterMask + kk_waterMask];
							}
							if (terrainCapability == TERRAIN_CAPABILITY.Any ||
								terrainCapability == TERRAIN_CAPABILITY.OnlyGround && dry ||
								terrainCapability == TERRAIN_CAPABILITY.OnlyWater && !dry) {
								setBit = true;
//							if (count<10000 && j>128) {
//								count++;
//								GameObject ng = GameObject.CreatePrimitive(PrimitiveType.Cube);
//								ng.GetComponent<Renderer>().material.color = Color.red;
//								GameObject.Destroy(ng.GetComponent<BoxCollider>());
//								ng.hideFlags = HideFlags.HideInHierarchy;
//								ng.WMSK_MoveTo( new Vector2( (float)k / EARTH_ROUTE_SPACE_WIDTH - 0.5f, (float)j / EARTH_ROUTE_SPACE_HEIGHT - 0.5f), 0);
//							}
							}
						}
						if (setBit) {	// set navigation bit
							earthRouteMatrix [jj + k] |= thisMatrix;
						} else {		// clear navigation bit
							earthRouteMatrix [jj + k] &= (byte)(byte.MaxValue ^ thisMatrix);
						}
					}
				}
			}

			if (finder == null) {
				PathFindingCustomRouteMatrixReset();
				finder = new PathFinderFast (earthRouteMatrix, thisMatrix, EARTH_ROUTE_SPACE_WIDTH, EARTH_ROUTE_SPACE_HEIGHT, _customRouteMatrix);
			} else {
				if (computeMatrix || thisMatrix != lastMatrix) {
					lastMatrix = thisMatrix;
					finder.SetCalcMatrix (earthRouteMatrix, thisMatrix);
				}
			}
		}

		/// <summary>
		/// Used by FindRoute method to satisfy custom positions check
		/// </summary>
		int FindRoutePositionValidator(int location) {
			if (_customRouteMatrix==null) PathFindingCustomRouteMatrixReset();
			int cost = 1;
			if (OnPathFindingCrossPosition!=null)  {
				int y = location / EARTH_ROUTE_SPACE_WIDTH;
				int x = location - y * EARTH_ROUTE_SPACE_WIDTH;
				Vector2 position = PointToPosition(x, y);
				cost = OnPathFindingCrossPosition(position);
			}
			_customRouteMatrix[location] = cost;
			return cost;
		}

		Point PositionToPoint(Vector2 position) {
			int x = (int)((position.x + 0.5f) * EARTH_ROUTE_SPACE_WIDTH);
			int y = (int)((position.y + 0.5f) * EARTH_ROUTE_SPACE_HEIGHT);
			return new Point(x,y);
		}

		Vector2 PointToPosition(int k, int j) {
			float x = (float)k /EARTH_ROUTE_SPACE_WIDTH - 0.5f;
			float y = (float)j /EARTH_ROUTE_SPACE_HEIGHT - 0.5f;
			return new Vector2(x,y);
		}

		int PointToLocation(Point p) {
			return p.Y * EARTH_ROUTE_SPACE_WIDTH + p.X;
		}

	}

}