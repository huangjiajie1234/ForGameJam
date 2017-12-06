using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Poly2Tri;

namespace WorldMapStrategyKit {
	public static class Drawing {

		static Dictionary<Vector3, int>hit;

		/// <summary>
		/// Rotates one point around another
		/// </summary>
		/// <param name="pointToRotate">The point to rotate.</param>
		/// <param name="centerPoint">The centre point of rotation.</param>
		/// <param name="angleInDegrees">The rotation angle in degrees.</param>
		/// <returns>Rotated point</returns>
		static Vector2 RotatePoint (Vector2 pointToRotate, Vector2 centerPoint, float angleInDegrees) {
			float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
			float cosTheta = Mathf.Cos (angleInRadians);
			float sinTheta = Mathf.Sin (angleInRadians);
			return new Vector2 (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x,
			                   sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y);
		}

		public static GameObject CreateSurface (string name, Vector3[] surfPoints, int[] indices, Material material) {
			Rect dummyRect = new Rect ();
			return CreateSurface (name, surfPoints, indices, material, dummyRect, Misc.Vector2one, Misc.Vector2zero, 0);
		}

		public static GameObject CreateSurface (string name, Vector3[] points, int[] indices, Material material, Rect rect, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			
			GameObject hexa = new GameObject (name, typeof(MeshRenderer), typeof(MeshFilter));
			hexa.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			Mesh mesh = new Mesh ();
			mesh.hideFlags = HideFlags.DontSave;
			mesh.vertices = points;
			mesh.triangles = indices;
			// uv mapping
			if (material.mainTexture != null) {
				Vector2[] uv = new Vector2[points.Length];
				for (int k=0; k<uv.Length; k++) {
					Vector2 coor = points [k];
					coor.x /= textureScale.x;
					coor.y /= textureScale.y;
					if (textureRotation != 0) 
						coor = RotatePoint (coor, Misc.Vector2zero, textureRotation);
					coor += textureOffset;
					Vector2 normCoor = new Vector2 ((coor.x - rect.xMin) / rect.width, (coor.y - rect.yMax) / rect.height);
					uv [k] = normCoor;
				}
				mesh.uv = uv;
			}
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			;
			
			MeshFilter meshFilter = hexa.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
			
			hexa.GetComponent<Renderer> ().sharedMaterial = material;
			return hexa;
			
		}

		public static GameObject CreateSurface (string name, Polygon poly, Material material, Rect rect, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			
			GameObject hexa = new GameObject (name, typeof(MeshRenderer), typeof(MeshFilter));
			hexa.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

//			revisedSurfPoints[k*3] = new Vector3(dt.Points[0].Xf, dt.Points[0].Yf, 0);
//			revisedSurfPoints[k*3+2] = new Vector3(dt.Points[1].Xf, dt.Points[1].Yf, 0);
//			revisedSurfPoints[k*3+1] = new Vector3(dt.Points[2].Xf, dt.Points[2].Yf, 0);

			int triCount = poly.Triangles.Count;
			List<Vector3> newPoints = new List<Vector3> (triCount*3);
			int[] triNew = new int[triCount*3];
			int newPointsCount = -1;
			if (hit == null)
				hit = new Dictionary<Vector3, int> (2000);
			else
				hit.Clear ();
			for (int k=0; k<triCount; k++) {
				DelaunayTriangle dt = poly.Triangles[k];
				Vector3 p =  new Vector3(dt.Points[0].Xf, dt.Points[0].Yf, 0);
				if (hit.ContainsKey (p)) {
					triNew [k*3] = hit [p];
				} else {
					newPoints.Add (p);
					hit.Add (p, ++newPointsCount);
					triNew [k*3] = newPointsCount;
				}
				p =  new Vector3(dt.Points[2].Xf, dt.Points[2].Yf, 0);
				if (hit.ContainsKey (p)) {
					triNew [k*3+1] = hit [p];
				} else {
					newPoints.Add (p);
					hit.Add (p, ++newPointsCount);
					triNew [k*3+1] = newPointsCount;
				}
				p =  new Vector3(dt.Points[1].Xf, dt.Points[1].Yf, 0);
				if (hit.ContainsKey (p)) {
					triNew [k*3+2] = hit [p];
				} else {
					newPoints.Add (p);
					hit.Add (p, ++newPointsCount);
					triNew [k*3+2] = newPointsCount;
				}
			}

			Mesh mesh = new Mesh ();
			mesh.hideFlags = HideFlags.DontSave;
			mesh.vertices = newPoints.ToArray();
			// uv mapping
			if (material.mainTexture != null) {
				Vector2[] uv = new Vector2[newPoints.Count];
				for (int k=0; k<uv.Length; k++) {
					Vector2 coor = newPoints [k];
					coor.x /= textureScale.x;
					coor.y /= textureScale.y;
					if (textureRotation != 0) 
						coor = RotatePoint (coor, Misc.Vector2zero, textureRotation);
					coor += textureOffset;
					Vector2 normCoor = new Vector2 ((coor.x - rect.xMin) / rect.width, (coor.y - rect.yMax) / rect.height);
					uv [k] = normCoor;
				}
				mesh.uv = uv;
			}
			mesh.triangles = triNew;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			;
			
			MeshFilter meshFilter = hexa.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
			
			hexa.GetComponent<Renderer> ().sharedMaterial = material;
			return hexa;
			
		}

		public static TextMesh CreateText (string text, GameObject parent, Vector2 center, Font labelFont, Color textColor, bool showShadow, Material shadowMaterial, Color shadowColor) {
			// create base text
			GameObject textObj = new GameObject (text);
			textObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			if (parent != null) {
				textObj.transform.SetParent (parent.transform, false);
			}
			textObj.transform.localPosition = new Vector3 (center.x, center.y, 0);
			TextMesh tm = textObj.AddComponent<TextMesh> ();
			tm.font = labelFont;
			textObj.GetComponent<Renderer> ().sharedMaterial = tm.font.material;
			tm.alignment = TextAlignment.Center;
			tm.anchor = TextAnchor.MiddleCenter;
			tm.color = textColor;
			tm.text = text;

			// add shadow
			if (showShadow) {
				GameObject shadow = GameObject.Instantiate (textObj);
				shadow.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				shadow.name = "shadow";
				shadow.transform.SetParent (textObj.transform, false);
				shadow.transform.localScale = Misc.Vector3one;
				shadow.transform.localPosition = new Vector3 (Mathf.Max (center.x / 100.0f, 1), Mathf.Min (center.y / 100.0f, -1), 0);
				shadow.GetComponent<Renderer> ().sharedMaterial = shadowMaterial;
				shadow.GetComponent<TextMesh> ().color = shadowColor;
			}
			return tm;
		}

		/// <summary>
		/// Draws a dashed line.
		/// </summary>
		/// <param name="points">Sequence of pair of points.</param>
		public static void DrawDashedLine(GameObject parent, List<Vector3> points, float thickness, Material sharedMaterial, ref Vector3[] meshPoints, ref int[] triPoints, ref Vector2[] uv) {
			MeshFilter meshFilter = parent.AddComponent<MeshFilter> ();
			UpdateDashedLine(meshFilter, points, thickness, ref meshPoints, ref triPoints, ref uv);
			Renderer renderer = parent.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = sharedMaterial;
		}

		public static void UpdateDashedLine(MeshFilter meshFilter, List<Vector3> points, float thickness, ref Vector3[] meshPoints, ref int[] triPoints, ref Vector2[] uv) {

			int max = (points.Count/2)*2;
			int numPoints = 8 * 3 * max / 2;
			Mesh mesh = meshFilter.sharedMesh;
			bool reassignMesh = false;
			if (mesh==null || meshPoints==null || mesh.vertexCount != numPoints) {
				meshPoints = new Vector3[numPoints];
				triPoints = new int[numPoints];
				uv = new Vector2[numPoints];
				reassignMesh = true;
			}

			int mp=0;
			thickness *= 0.5f;
			float y0 = 0f; //Mathf.Sin (0.0f * Mathf.Deg2Rad);
			float x0 = 1f; //Mathf.Cos (0.0f * Mathf.Deg2Rad);
			float y1 = 0.8660254f; //Mathf.Sin (120.0f * Mathf.Deg2Rad);
			float x1 = -0.5f; //Mathf.Cos (120.0f * Mathf.Deg2Rad);
			float y2 = -0.8660254f; //Mathf.Sin (240.0f * Mathf.Deg2Rad);
			float x2 = -0.5f; //Mathf.Cos (240.0f * Mathf.Deg2Rad);
			Vector3 up = WMSK.instance.currentCamera.transform.forward; // upQuaternion * v01;

//			Quaternion upQuaternion = Quaternion.Euler(-90, 0, 0);

			for (int p=0;p<max;p+=2) {
				Vector3 p0 = points[p];
				Vector3 p1 = points[p+1];
				
				Vector3 v01 = (p1-p0).normalized;
				Vector3 right = Vector3.Cross (up, v01);
				
				// Front triangle
				meshPoints[mp+ 0] = p0 + (up * y0 + right * x0).normalized * thickness;
				meshPoints[mp+ 1] = p0 + (up * y2 + right * x2).normalized * thickness;
				meshPoints[mp+ 2] = p0 + (up * y1 + right * x1).normalized * thickness;
				triPoints [mp+ 0] = mp+0;
				triPoints [mp+ 1] = mp+1;
				triPoints [mp+ 2] = mp+2;
				uv        [mp+ 0] = Misc.Vector2zero; // new Vector2(0,0);
				uv        [mp+ 1] = Misc.Vector2right; // new Vector2(1,0);
				uv        [mp+ 2] = Misc.Vector2up; // new Vector2(0,1);
				
				// Back triangle
				meshPoints[mp+ 3] = p1 + (up * y0 + right * x0).normalized * thickness;
				triPoints [mp+ 3] = mp+3;
				uv        [mp+ 3] = Misc.Vector2zero; //new Vector2(0,0);
				meshPoints[mp+ 4] = p1 + (up * y1 + right * x1).normalized * thickness;
				triPoints [mp+ 4] = mp+4;
				uv        [mp+ 4] = Misc.Vector2one; //new Vector2(1,1);
				meshPoints[mp+ 5] = p1 + (up * y2 + right * x2).normalized * thickness;
				triPoints [mp+ 5] = mp+5;
				uv        [mp+ 5] = Misc.Vector2right; // new Vector2(1,0);
				
				// One side
				meshPoints[mp+ 6] = meshPoints[mp+0];
				triPoints [mp+ 6] = mp+6;
				uv        [mp+ 6] = Misc.Vector2up; // new Vector2(0,1);
				meshPoints[mp+ 7] = meshPoints[mp+3];
				triPoints [mp+ 7] = mp+7;
				uv        [mp+ 7] = Misc.Vector2one; // new Vector2(1,1);
				meshPoints[mp+ 8] = meshPoints[mp+1];
				triPoints [mp+ 8] = mp+8;
				uv        [mp+ 8] = Misc.Vector2zero; //new Vector2(0,0);
				
				meshPoints[mp+ 9] = meshPoints[mp+1];
				triPoints [mp+ 9] = mp+9;
				uv        [mp+ 9] = Misc.Vector2zero; //new Vector2(0,0);
				meshPoints[mp+10] = meshPoints[mp+3];
				triPoints [mp+10] = mp+10;
				uv        [mp+10] = Misc.Vector2one; //new Vector2(1,1);
				meshPoints[mp+11] = meshPoints[mp+5];
				triPoints [mp+11] = mp+11;
				uv        [mp+11] = Misc.Vector2zero; //new Vector2(0,0);
				
				// Second side
				meshPoints[mp+12] = meshPoints[mp+1];
				triPoints [mp+12] = mp+12;
				uv        [mp+12] = Misc.Vector2zero; //new Vector2(0,0);
				meshPoints[mp+13] = meshPoints[mp+5];
				triPoints [mp+13] = mp+13;
				uv        [mp+13] = Misc.Vector2right; // new Vector2(1,0);
				meshPoints[mp+14] = meshPoints[mp+2];
				triPoints [mp+14] = mp+14;
				uv        [mp+14] = Misc.Vector2up; // new Vector2(0,1);
				
				meshPoints[mp+15] = meshPoints[mp+2];
				triPoints [mp+15] = mp+15;
				uv        [mp+15] = Misc.Vector2up; // new Vector2(0,1);
				meshPoints[mp+16] = meshPoints[mp+5];
				triPoints [mp+16] = mp+16;
				uv        [mp+16] = Misc.Vector2right; // new Vector2(1,0);
				meshPoints[mp+17] = meshPoints[mp+4];
				triPoints [mp+17] = mp+17;
				uv        [mp+17] = Misc.Vector2up; // new Vector2(0,1);
				
				// Third side
				meshPoints[mp+18] = meshPoints[mp+0];
				triPoints [mp+18] = mp+18;
				uv        [mp+18] = Misc.Vector2right; // new Vector2(1,0);
				meshPoints[mp+19] = meshPoints[mp+4];
				triPoints [mp+19] = mp+19;
				uv        [mp+19] = Misc.Vector2up; // new Vector2(0,1);
				meshPoints[mp+20] = meshPoints[mp+3];
				triPoints [mp+20] = mp+20;
				uv        [mp+20] = Misc.Vector2zero; //new Vector2(0,0);
				
				meshPoints[mp+21] = meshPoints[mp+0];
				triPoints [mp+21] = mp+21;
				uv        [mp+21] = Misc.Vector2zero; //new Vector2(0,0);
				meshPoints[mp+22] = meshPoints[mp+2];
				triPoints [mp+22] = mp+22;
				uv        [mp+22] = Misc.Vector2one; //new Vector2(1,1);
				meshPoints[mp+23] = meshPoints[mp+4];
				triPoints [mp+23] = mp+23;
				uv        [mp+23] = Misc.Vector2up; // new Vector2(0,1);
				
				mp += 24;
			}

			if (mesh==null) {
				mesh = new Mesh();
				mesh.hideFlags = HideFlags.DontSave;
			}
			if (reassignMesh) mesh.Clear();
			mesh.vertices = meshPoints;
			mesh.uv = uv;
			mesh.triangles = triPoints;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			if (reassignMesh) meshFilter.sharedMesh = mesh;
		}


		/// <summary>
		/// Creates a 2D pie
		/// </summary>
		public static GameObject DrawCircle (string name, Vector3 localPosition, float width, float height, float angleStart, float angleEnd, float ringWidthMin, float ringWidthMax, int numSteps, Material material) {
			
			GameObject hexa = new GameObject (name, typeof(MeshRenderer), typeof(MeshFilter));
			hexa.isStatic = true;
			
			// create the points - start with a circle
			numSteps = Mathf.FloorToInt (32.0f * (angleEnd - angleStart) / (2 * Mathf.PI));
			numSteps = Mathf.Clamp (numSteps, 12, 32);
			
			// if ringWidthMin == 0 we only need one triangle per step
			int numPoints = ringWidthMin == 0 ? numSteps * 3 : numSteps * 6;
			Vector3[] points = new Vector3[numPoints];
			Vector2[] uv = new Vector2[numPoints];
			int pointIndex = -1;
			
			width *= 0.5f;
			height *= 0.5f;
			
			float angleStep = (angleEnd - angleStart) / numSteps;
			float px, py;
			for (int stepIndex = 0; stepIndex < numSteps; stepIndex++) {
				float angle0 = angleStart + stepIndex * angleStep;
				float angle1 = angle0 + angleStep;
				
				// first triangle
				// 1
				px = Mathf.Cos (angle0) * (ringWidthMax * width);
				py = Mathf.Sin (angle0) * (ringWidthMax * height);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (1, 1);
				// 2
				px = Mathf.Cos (angle0) * (ringWidthMin * width);
				py = Mathf.Sin (angle0) * (ringWidthMin * height);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (1, 0);
				// 3
				px = Mathf.Cos (angle1) * (ringWidthMax * width);
				py = Mathf.Sin (angle1) * (ringWidthMax * height);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (0, 1);
				
				// second triangle
				if (ringWidthMin != 0) {
					// 1
					points [++pointIndex] = points [pointIndex - 2];
					uv [pointIndex] = new Vector2 (1, 0);
					// 2
					px = Mathf.Cos (angle1) * (ringWidthMin * width);
					py = Mathf.Sin (angle1) * (ringWidthMin * height);
					points [++pointIndex] = new Vector3 (px, py, 0);
					uv [pointIndex] = new Vector2 (0, 0);
					// 3
					points [++pointIndex] = points [pointIndex - 3];
					uv [pointIndex] = new Vector2 (0, 1);
				}
			}
			
			// triangles
			int[] triPoints = new int[numPoints];
			for (int p=0; p<numPoints; p++) {
				triPoints [p] = p;
			}
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points;
			mesh.triangles = triPoints;
			mesh.uv = uv;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			;
			
			//			TangentSolver (mesh);
			
			MeshFilter meshFilter = hexa.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
			hexa.GetComponent<Renderer> ().sharedMaterial = material;
			
			hexa.transform.localPosition = localPosition;
			hexa.transform.localScale = Misc.Vector3one;
			
			return hexa;
			
		}



	}


}



