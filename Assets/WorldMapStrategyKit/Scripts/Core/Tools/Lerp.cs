using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit {
	public static class Lerp {

		public static float EaseIn(float t) {
			t = Mathf.Clamp01(t);
			return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
		}
		
		public static float EaseOut(float t) {
			t = Mathf.Clamp01(t);
			return Mathf.Sin(t * Mathf.PI * 0.5f);
		}
		
		public static float Exponential(float t) {
			t = Mathf.Clamp01(t);
			return t*t;
		}
		
		public static float SmoothStep(float t) {
			return t*t * (3f - 2f*t);
		}
		
		public static float SmootherStep(float t) {
			t = Mathf.Clamp01(t);
			return t*t*t * (t * (6f*t - 15f) + 10f);
		}
	}

}