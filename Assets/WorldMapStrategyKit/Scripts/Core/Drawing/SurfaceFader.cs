using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit {
	public class SurfaceFader : MonoBehaviour {

		public float duration;
		Material fadeMaterial;
		float highlightFadeStart;
		public IFader fadeEntity;

		void Start () {
			GenerateMaterial ();
			highlightFadeStart = Time.time;
			if (fadeEntity!=null) fadeEntity.isFading = true;
		}
	
		// Update is called once per frame
		void Update () {
			float elapsed = Time.time - highlightFadeStart;
			if (elapsed > duration) {
				if (fadeEntity!=null) {
					fadeEntity.isFading = false;
					fadeEntity.customMaterial = null;
				}
				Destroy (this);
			}
			float newAlpha = Mathf.Clamp01 (1.0f - elapsed / duration);
			Color color = fadeMaterial.color;
			Color newColor = new Color(color.r, color.g, color.b, newAlpha);
			fadeMaterial.color = newColor;
		}

		void GenerateMaterial () {
			fadeMaterial = Instantiate (GetComponent<Renderer> ().sharedMaterial);
			fadeMaterial.hideFlags = HideFlags.DontSave;
			GetComponent<Renderer> ().sharedMaterial = fadeMaterial;
		}
	}

}