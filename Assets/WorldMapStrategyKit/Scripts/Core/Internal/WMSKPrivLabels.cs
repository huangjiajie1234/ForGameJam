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

		GameObject textRoot;
		Font labelsFont;

		#region Map Labels

		
		//		void ReloadFont() {
		//			if (labelsFont==null) {
		//				Debug.Log ("instantiating font lato...");
		//				labelsFont = GameObject.Instantiate (Resources.Load <Font> ("WMSK/Font/Lato"));
		//				Debug.Log ("ok");
		//				labelsFont.hideFlags = HideFlags.DontSave;
		//			}
		//			Debug.Log ("instantiating material font...");
		//			Material fontMaterial = Instantiate (Resources.Load<Material>("WMSK/Materials/Font")); // this material is linked to a shader that has into account zbuffer
		//			Debug.Log ("ok");
		//			fontMaterial.mainTexture = labelsFont.material.mainTexture;
		//			fontMaterial.hideFlags = HideFlags.DontSave;
		//			labelsFont.material = fontMaterial;
		//			labelsShadowMaterial = GameObject.Instantiate (fontMaterial);
		//			labelsShadowMaterial.hideFlags = HideFlags.DontSave;
		//			labelsShadowMaterial.renderQueue--;
		//		}
		
		void ReloadFont() {
			if (_countryLabelsFont==null) {
				labelsFont = Instantiate (Resources.Load <Font> ("WMSK/Font/Lato"));
			} else {
				labelsFont = Instantiate(_countryLabelsFont);
			}
			labelsFont.hideFlags = HideFlags.DontSave;
			
			Material fontMaterial = Instantiate (Resources.Load<Material>("WMSK/Materials/Font")); // this material is linked to a shader that has into account zbuffer
			if (labelsFont.material!=null) {
				fontMaterial.mainTexture = labelsFont.material.mainTexture;
			}
			fontMaterial.hideFlags = HideFlags.DontSave;
			labelsFont.material = fontMaterial;
			labelsShadowMaterial = GameObject.Instantiate (fontMaterial);
			labelsShadowMaterial.hideFlags = HideFlags.DontSave;
			labelsShadowMaterial.renderQueue--;
		}

		/// <summary>
		/// Draws the map labels. Note that it will update cached textmesh objects if labels are already drawn.
		/// </summary>
		void DrawMapLabels () { 
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": Draw map labels");
			#endif
			
			if (!_showCountryNames || !gameObject.activeInHierarchy) return;
			
			// Set colors
			labelsFont.material.color = _countryLabelsColor;
			labelsShadowMaterial.color = _countryLabelsShadowColor;
			
			// Create texts
			DestroyMapLabels();
			GameObject overlay = GetOverlayLayer (true);
			Transform t = overlay.transform.Find (OVERLAY_TEXT_ROOT);
			if (t == null) {
				textRoot = new GameObject (OVERLAY_TEXT_ROOT);
				textRoot.hideFlags = HideFlags.DontSave;
				textRoot.layer = overlay.layer;
			} else {
				textRoot = t.gameObject;
			}
			
			List<MeshRect> meshRects = new List<MeshRect> ();
			float mw = mapWidth;
			float mh = mapHeight;
			for (int countryIndex=0; countryIndex<countries.Length; countryIndex++) {
				Country country = countries [countryIndex];
				if (country.hidden || !country.labelVisible) continue;
				
				Vector2 center = new Vector2(country.center.x * mapWidth, country.center.y * mh) + country.labelOffset;
				Region region = country.regions [country.mainRegionIndex];
				
				float zoomFactor = transform.localScale.x / 200.0f;
				// Special countries adjustements
				if (_frontiersDetail == FRONTIERS_DETAIL.Low) {
					switch (countryIndex) {
					case 6: // Antartica
						center.y += 3f * zoomFactor;
						break;
					case 65: // Greenland
						center.y -= 3f * zoomFactor;
						break;
					case 22: // Brazil
						center.y += 4f * zoomFactor;
						center.x += 1.0f * zoomFactor;
						break;
					case 73: // India
						center.x -= 2f * zoomFactor;
						break;
					case 168: // USA
						center.x -= 1f * zoomFactor;
						break;
					case 27: // Canada
						center.x -= 3f * zoomFactor;
						break;
					case 30: // China
						center.x -= 1f * zoomFactor;
						center.y -= 1f * zoomFactor;
						break;
					}
				} else {
					switch (countryIndex) {
					case 114: // Russia
						center.x -= 4f * zoomFactor;
						center.y += 1f * zoomFactor;
						break;
					case 92: // Canada
						center.x -= 7f * zoomFactor;
						break;
					case 185: // USA
						center.x -= 2f * zoomFactor;
						break;
					case 37: // Antartica
						center.y += 2f * zoomFactor;
						break;
					case 84: // Brazil
						center.y += 4f * zoomFactor;
						center.x += 2f * zoomFactor;
						break;
					case 95: // China
						center.x -= 3f * zoomFactor;
						break;
					}
				}
				
				// Adjusts country name length
				string countryName = country.customLabel != null ? country.customLabel : country.name.ToUpper();
				bool introducedCarriageReturn = false;
				if (countryName.Length > 15) {
					int spaceIndex = countryName.IndexOf (' ', countryName.Length / 2);
					if (spaceIndex >= 0) {
						countryName = countryName.Substring (0, spaceIndex) + "\n" + countryName.Substring (spaceIndex + 1);
						introducedCarriageReturn = true;
					}
				}
				
				// add caption
				GameObject textObj;
				TextMesh tm;
				if (country.labelGameObject == null) {
					Color labelColor = country.labelColorOverride ? country.labelColor : _countryLabelsColor;
					Font customFont = country.labelFontOverride ?? labelsFont;
					Material customLabelShadowMaterial = country.labelFontShadowMaterial ?? labelsShadowMaterial;
					tm = Drawing.CreateText (countryName, null, center, customFont, labelColor, _showLabelsShadow, customLabelShadowMaterial, _countryLabelsShadowColor);
					textObj = tm.gameObject;
					country.labelGameObject = tm;
					Bounds bounds = textObj.GetComponent<Renderer> ().bounds;
					country.labelMeshWidth = bounds.size.x;
					country.labelMeshHeight = bounds.size.y;
					country.labelMeshCenter = center;
					textObj.transform.SetParent(textRoot.transform, false);
					textObj.transform.localPosition = center;
					textObj.layer = textRoot.gameObject.layer;
					if (_showLabelsShadow) {
						country.labelShadowGameObject = textObj.transform.Find("shadow").GetComponent<TextMesh>();
						country.labelShadowGameObject.gameObject.layer = textObj.layer;
					}
				} else {
					tm = country.labelGameObject;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
				}
				
				float meshWidth = country.labelMeshWidth;
				float meshHeight = country.labelMeshHeight;
				
				// adjusts caption
				Rect rect = new Rect(region.rect2D.xMin * mw, region.rect2D.yMin * mh, region.rect2D.width * mw, region.rect2D.height * mh);
				float absoluteHeight;
				if (country.labelRotation>0) {
					textObj.transform.localRotation = Quaternion.Euler (0, 0, country.labelRotation);
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				} else if (rect.height > rect.width * 1.45f) {
					float angle;
					if (rect.height > rect.width * 1.5f) {
						angle = 90;
					} else {
						angle = Mathf.Atan2 (rect.height, rect.width) * Mathf.Rad2Deg;
					}
					textObj.transform.localRotation = Quaternion.Euler (0, 0, angle);
					absoluteHeight = Mathf.Min (rect.width * _countryLabelsSize, rect.height);
				} else {
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				}
				
				// adjusts scale to fit width in rect
				float adjustedMeshHeight = introducedCarriageReturn ? meshHeight * 0.5f : meshHeight;
				float scale = absoluteHeight / adjustedMeshHeight;
				if (country.labelFontSizeOverride) {
					scale = country.labelFontSize;
				} else {
					float desiredWidth = meshWidth * scale;
					if (desiredWidth > rect.width) {
						scale = rect.width / meshWidth;
					}
					if (adjustedMeshHeight * scale < _countryLabelsAbsoluteMinimumSize) {
						scale = _countryLabelsAbsoluteMinimumSize / adjustedMeshHeight;
					}
				}
				
				// stretchs out the caption
				float displayedMeshWidth = meshWidth * scale;
				float displayedMeshHeight = meshHeight * scale;
				string wideName;
				int times = Mathf.FloorToInt (rect.width * 0.45f / (meshWidth * scale));
				if (times > 10)
					times = 10;
				if (times > 0) {
					StringBuilder sb = new StringBuilder ();
					string spaces = new string (' ', times * 2);
					for (int c=0; c<countryName.Length; c++) {
						sb.Append (countryName [c]);
						if (c < countryName.Length - 1) {
							sb.Append (spaces);
						}
					}
					wideName = sb.ToString ();
				} else {
					wideName = countryName;
				}
				
				if (tm.text.Length != wideName.Length) {
					tm.text = wideName;
					displayedMeshWidth = textObj.GetComponent<Renderer> ().bounds.size.x * scale;
					displayedMeshHeight = textObj.GetComponent<Renderer> ().bounds.size.y * scale;
					if (_showLabelsShadow) {
						textObj.transform.Find ("shadow").GetComponent<TextMesh> ().text = wideName;
					}
				}
				
				// apply scale
				textObj.transform.localScale = new Vector3 (scale, scale, 1);
				
				// Save mesh rect for overlapping checking
				if (country.labelOffset == Misc.Vector2zero) {
					MeshRect mr = new MeshRect (countryIndex, new Rect (center.x - displayedMeshWidth * 0.5f, center.y - displayedMeshHeight * 0.5f, displayedMeshWidth, displayedMeshHeight));
					meshRects.Add (mr);
				}
			}
			
			// Simple-fast overlapping checking
			int cont = 0;
			bool needsResort = true;
			
			while (needsResort && ++cont<10) {
				meshRects.Sort (overlapComparer);
				
				for (int c=1; c<meshRects.Count; c++) {
					Rect thisMeshRect = meshRects [c].rect;
					for (int prevc=c-1; prevc>=0; prevc--) {
						Rect otherMeshRect = meshRects [prevc].rect;
						if (thisMeshRect.Overlaps (otherMeshRect)) {
							needsResort = true;
							int thisCountryIndex = meshRects [c].countryIndex;
							Country country = countries [thisCountryIndex];
							GameObject thisLabel = country.labelGameObject.gameObject;
							
							// displaces this label
							float offsety = (thisMeshRect.yMax - otherMeshRect.yMin);
							offsety = Mathf.Min (country.regions[country.mainRegionIndex].rect2D.height * mh * 0.35f, offsety);
							thisLabel.transform.localPosition = new Vector3 (country.labelMeshCenter.x, country.labelMeshCenter.y - offsety, thisLabel.transform.localPosition.z);
							thisMeshRect = new Rect (thisLabel.transform.localPosition.x - thisMeshRect.width * 0.5f,
							                         thisLabel.transform.localPosition.y - thisMeshRect.height * 0.5f,
							                         thisMeshRect.width, thisMeshRect.height);
							meshRects [c].rect = thisMeshRect;
						}
					}
				}
			}
			
			// Adjusts parent
			textRoot.transform.SetParent (overlay.transform, false);
			textRoot.transform.localPosition = new Vector3 (0, 0, -0.001f);
			textRoot.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			textRoot.transform.localScale = new Vector3(1.0f/mw, 1.0f/mh,1);
		}
		
		int overlapComparer (MeshRect r1, MeshRect r2) {
			return (r2.rect.center.y).CompareTo (r1.rect.center.y);
		}
		
		class MeshRect {
			public int countryIndex;
			public Rect rect;
			
			public MeshRect (int countryIndex, Rect rect) {
				this.countryIndex = countryIndex;
				this.rect = rect;
			}
		}
		
		void DestroyMapLabels () {
			#if TRACE_CTL			
			Debug.Log ("CTL " + DateTime.Now + ": destroy labels");
			#endif
			if (countries != null) {
				for (int k=0; k<countries.Length; k++) {
					if (countries [k].labelGameObject != null) {
						DestroyImmediate (countries [k].labelGameObject);
						countries [k].labelGameObject = null;
					}
				}
			}
			if (textRoot!=null) DestroyImmediate(textRoot);
			// Security check: if there're still gameObjects under TextRoot, also delete it
			if (overlayLayer != null) {
				Transform t = overlayLayer.transform.Find (OVERLAY_TEXT_ROOT);
				if (t != null && t.childCount > 0) {
					DestroyImmediate (t.gameObject);
				}
			}
		}


		void FadeCountryLabels() {

			// Automatically fades in/out country labels based on their screen size

			float y0 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0,0,0)).y;
			float y1 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0,1.0f,0)).y;
			float th = y1 - y0;

			float maxAlpha = _countryLabelsColor.a;
			float maxAlphaShadow = _countryLabelsShadowColor.a;
			float labelFadeMinSize = 0.018f;
			float labelFadeMaxSize = 0.2f;
			float labelFadeMinFallOff = 0.005f;
			float labelFadeMaxFallOff = 0.5f;
			float mh = mapHeight;
			for (int k=0;k<countries.Length;k++) {
				Country country = countries[k];
				TextMesh tm = country.labelGameObject;
				if (tm!=null) {
					// Fade label
					float labelSize = (country.labelMeshHeight + country.labelMeshWidth) * 0.5f;
					float screenHeight = labelSize * tm.transform.localScale.y  * th / mh;
					float ad;
					if (screenHeight<labelFadeMinSize) {
						ad = Mathf.Lerp(1.0f, 0, (labelFadeMinSize - screenHeight) / labelFadeMinFallOff);
					} else if (screenHeight>labelFadeMaxSize) {
						ad = Mathf.Lerp (1.0f, 0, (screenHeight - labelFadeMaxSize) / labelFadeMaxFallOff);
					} else {
						ad = 1.0f;
					}
					float newAlpha = ad * maxAlpha;
					if (tm.color.a != newAlpha) {
						tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, newAlpha);
					}
					// Fade label shadow
					TextMesh tmShadow = country.labelShadowGameObject;
					if (tmShadow!=null) {
						newAlpha = ad * maxAlphaShadow;
						if ( tmShadow.color.a != newAlpha) {
							tmShadow.color = new Color(tmShadow.color.r, tmShadow.color.g, tmShadow.color.b, maxAlphaShadow * ad);
						}
					}
				}
			}
		}

		#endregion


	}

}