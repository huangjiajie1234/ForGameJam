using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {

	public class LineMarkerAnimator : MonoBehaviour {

		const int MINIMUM_POINTS = 64; // increase to improve line resolution // TODO: was 64

		public WMSK map;

		/// <summary>
		/// The list of map points to be traversed by the line.
		/// </summary>
		public Vector2[] path;

		/// <summary>
		/// The color of the line.
		/// </summary>
		public Color color;

		/// <summary>
		/// Line width (default: 0.01f)
		/// </summary>
		public float lineWidth = 0.01f;

		/// <summary>
		/// Arc of the line. A value of zero means the line will be drawn flat, on the ground.
		/// </summary>
		public float arcElevation;

		/// <summary>
		/// The duration of the drawing of the line. Zero means instant drawing.
		/// </summary>
		public float drawingDuration;

		/// <summary>
		/// The line material. If not supplied it will use the default lineMarkerMaterial.
		/// </summary>
		public Material lineMaterial;

		/// <summary>
		/// Specifies the duration in seconds for the line before it fades out
		/// </summary>
		public float autoFadeAfter = 0;

		/// <summary>
		/// The duration of the fade out.
		/// </summary>
		public float fadeOutDuration = 1.0f;

		/// <summary>
		/// 0 for continuous line.
		/// </summary>
		public float dashInterval;

		/// <summary>
		/// Duratino of a cycle in seconds. 0.1f can be a good value. 0 = no animation.
		/// </summary>
		public float dashAnimationDuration = 0f;


		/* Internal fields */
		float startTime, startAutoFadeTime;
		List<Vector3> vertices;
		LineRenderer lr;
		LineRenderer2 lrd; // for dashed lines
		Color colorTransparent;
		int numPoints;
		bool usesViewport;
		List<Vector3> newLinePositions;
		bool isFading;

		// Use this for initialization
		void Start () {

			startAutoFadeTime = float.MaxValue;
			colorTransparent = new Color(color.r, color.g, color.b, 0);

			// Compute path points on viewport or on 2D map
			usesViewport = map.renderViewportIsEnabled && arcElevation > 0;

			if (dashInterval>0) {
				SetupDashedLine();
			} else {
				SetupLine();
			}

			Update ();
		}

		
		// Update is called once per frame
		void Update () {
			UpdateLine();
			if (Time.time >= startAutoFadeTime) {
				UpdateFade ();
			}
		}

		
		void UpdateLine () {
			float t;
			if (drawingDuration == 0) 
				t = 1.0f;
			else
				t = (Time.time - startTime) / drawingDuration;
			if (t >= 1.0f) {
				t = 1.0f;
				if (autoFadeAfter == 0) {
					if (!usesViewport && dashAnimationDuration==0 && !isFading) enabled = false;	// disable this behaviour
				} else if (!isFading) {
					startAutoFadeTime = Time.time;
					isFading = true;
				}
			}
			
			if (dashInterval>0) {
				UpdateDashedLine(t);
			} else {
				UpdateContinousLine(t);
			}
		}

		void UpdateFade () {
			float t = Time.time - startAutoFadeTime;
			if (t < autoFadeAfter)
				return;
			
			t = (t - autoFadeAfter) / fadeOutDuration;
			if (t >= 1.0f) {
				t = 1.0f;
				Destroy (gameObject);
			}
			
			Color fadeColor = Color.Lerp (color, colorTransparent, t);
			lineMaterial.color = fadeColor;

		}
		
		/// <summary>
		/// Fades out current line.
		/// </summary>
		public void FadeOut(float duration) {
			startAutoFadeTime = Time.time;
			fadeOutDuration = duration;
			isFading = true;
			enabled = true;
		}


		#region Continous line

		void SetupLine() {
			// Create the line mesh
			numPoints = Mathf.Max (MINIMUM_POINTS, path.Length-1);
			startTime = Time.time;
			lr = transform.GetComponent<LineRenderer> ();
			if (lr == null) {
				lr = gameObject.AddComponent<LineRenderer> ();
			}
			lr.useWorldSpace = usesViewport;
			lr.SetWidth (lineWidth, lineWidth);
			lineMaterial = Instantiate (lineMaterial);
			lineMaterial.color = color;
			lr.material = lineMaterial; // needs to instantiate to preserve individual color so can't use sharedMaterial
			lr.SetColors (color, color);
		}

		void CreateLineVertices() {

			float elevationStart = 0, elevationEnd = 0;
			if (usesViewport) {
				lineWidth *= 6.0f;
				elevationStart = map.ComputeEarthHeight(path[0], false);
				elevationEnd = map.ComputeEarthHeight(path[path.Length-1], false);
			}

			if (vertices == null) {
				vertices = new List<Vector3>(numPoints+1);
			} else {
				vertices.Clear();
			}
			for (int s=0; s<=numPoints; s++) {
				float t = (float)s / numPoints;
				int index = (int)( (path.Length-1) * t);
				int findex =  Mathf.Min (index +1, path.Length-1);
				float t0 = t * (path.Length-1);
				t0 -= (int)t0;
				Vector3 mapPos = Vector2.Lerp(path[index], path[findex], t0);
				if (usesViewport) {
					if (map.renderViewportRect.Contains(mapPos)) {
						float elevation = Mathf.Lerp (elevationStart, elevationEnd, t);
						elevation += arcElevation > 0 ? Mathf.Sin (t * Mathf.PI) * arcElevation: 0;
						mapPos = map.Map2DToWorldPosition(mapPos, elevation, HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED, false);
						vertices.Add (mapPos);
					}
				} else {
					vertices.Add (mapPos);
				}
			}
		}

		void UpdateContinousLine(float t) {
			CreateLineVertices();

			float vertexIndex = 1 + (vertices.Count - 2) * t;
			int currentVertex = (int)(vertexIndex);
			lr.SetVertexCount (currentVertex + 1);
			if (currentVertex>=0) {
				for (int k=0; k<currentVertex; k++) {
					lr.SetPosition (k, vertices [k]);
				}
				// adjust last segment
				Vector3 nextVertex = vertices [currentVertex];
				float subt = vertexIndex - currentVertex;
				Vector3 progress = Vector3.Lerp (vertices [currentVertex], nextVertex, subt);
				lr.SetPosition (currentVertex, progress);
			}
		}

		#endregion

		#region Dashed line

		void SetupDashedLine() {
			// Create the line mesh
			startTime = Time.time;
			lrd = transform.GetComponent<LineRenderer2> ();
			if (lrd == null) {
				lrd = gameObject.AddComponent<LineRenderer2> ();
			}
			lrd.useWorldSpace = usesViewport; // needed since thickness should be independent of parent scale
			if (!usesViewport) lineWidth /= map.transform.localScale.x;
			lrd.SetWidth (lineWidth, lineWidth);
			lineMaterial = Instantiate (lineMaterial);
			lineMaterial.color = color;
			lrd.material = lineMaterial; // needs to instantiate to preserve individual color so can't use sharedMaterial
			lrd.SetColors (color, color);
		}

		void CreateDashedLineVertices() {

			// Prepare elevation range
			float elevationStart = 0, elevationEnd = 0;
			if (usesViewport) {
				lineWidth *= 6.0f;
				elevationStart = map.ComputeEarthHeight(path[0], false);
				elevationEnd = map.ComputeEarthHeight(path[path.Length-1], false);
			}

			// Calculate total line distance
			float totalDistance = 0;
			Vector2 prev = Misc.Vector2zero;
			for (int s=0;s<path.Length;s++) {
				Vector2 current = path[s];
				if (s>0) totalDistance += Vector2.Distance(current, prev);
				prev = current;
			}

			// Dash animation?
			float startingDistance = 0;
			float step = dashInterval * 2f;
			if (dashAnimationDuration>0) {
				float tt = Time.time;
				float ett = tt / dashAnimationDuration;
				float elapsed = ett - (int)ett;
				float et = elapsed * step;
				startingDistance = -step + et;
			}

			// Compute dash segments
			if (vertices==null) {
				vertices = new List<Vector3>(100);
			} else {
				vertices.Clear();
			}
//			int prevIndex=0;
			int pair = 0;
			Vector2 mapPos;
			for (float distanceAcum = startingDistance; distanceAcum < totalDistance + step; distanceAcum += dashInterval, pair++) {
				float t0 = Mathf.Clamp01(distanceAcum / totalDistance);
				int index = (int)( (path.Length-1) * t0);
				int findex =  Mathf.Min (index +1, path.Length-1);

//				if (prevIndex<index && distanceAcum < totalDistance) {
//					// add dash corner
//					mapPos = path[index];
//					if (usesViewport) {
//						if (map.renderViewportRect.Contains(mapPos)) {
//							if (vertices.Count>0 || (pair % 2 == 0)) {
//								float elevation = Mathf.Lerp (elevationStart, elevationEnd, t0);
//								elevation += arcElevation > 0 ? Mathf.Sin (t0 * Mathf.PI) * arcElevation: 0;
//								Vector3 sPos = map.Map2DToWorldPosition(mapPos, elevation, HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED, false);
//								vertices.Add (sPos);
//								vertices.Add (sPos);
//							}
//						}
//					} else {
//						vertices.Add (mapPos);
//						vertices.Add (mapPos);
//					}
//				}
				float t = t0 * (path.Length-1);
				t -= (int)t;
				mapPos = Vector2.Lerp(path[index], path[findex], t);

				if (usesViewport) {
					if (map.renderViewportRect.Contains(mapPos)) {
						if (vertices.Count>0 || (pair % 2 == 0)) {
							float elevation = Mathf.Lerp (elevationStart, elevationEnd, t0);
							elevation += arcElevation > 0 ? Mathf.Sin (t0 * Mathf.PI) * arcElevation: 0;
							Vector3 sPos = map.Map2DToWorldPosition(mapPos, elevation, HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED, false);
							vertices.Add (sPos);
						}
					}
				} else {
					vertices.Add (mapPos);
				}
//				prevIndex = index;
			}
		}


		void UpdateDashedLine(float t) {
			// pass current vertices
			CreateDashedLineVertices();

			float vertexIndex = 1 + (vertices.Count - 2) * t;
			int currentVertex = (int)(vertexIndex);
			lrd.SetVertexCount (currentVertex + 1);
			if (currentVertex>=0) {
				for (int k=0; k<currentVertex; k++) {
					lrd.SetPosition (k, vertices [k]);
				}
				// adjust last segment
				Vector3 nextVertex = vertices [currentVertex];
				float subt = vertexIndex - currentVertex;
				Vector3 progress = Vector3.Lerp (vertices [currentVertex], nextVertex, subt);
				lrd.SetPosition (currentVertex, progress);
			}
		}

		#endregion

	}
}