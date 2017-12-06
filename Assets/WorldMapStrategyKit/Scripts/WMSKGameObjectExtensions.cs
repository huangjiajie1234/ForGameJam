using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit {

	public static class WMSKGameObjectExtensions {

		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, float x, float y) {
			return WMSK_MoveTo(o, new Vector2(x,y), 0, true);
		}
		
		
		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination) {
			return WMSK_MoveTo(o, destination, 0, true);
		}

		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, float x, float y, float duration) {
			return WMSK_MoveTo(o, new Vector2(x,y), duration);
		}

		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination, float duration) {
			return WMSK_MoveTo(o, destination, duration);
		}

		/// <summary>
		/// Smoothly moves this game object to given map position with options.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		/// <param name="scaleOnZoom">If set to <c>true</c> the gameobject will increase/decrease its scale when zoomin in/out.</param>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination, float duration, bool scaleOnZoom) {
			return WMSK_MoveTo(o, destination, duration, 0, HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND, true);
		}


		/// <summary>
		/// Smoothly moves this game object to given map position with options.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		/// <param name="height">Pass a 0 to make this gameobject grounded.</param>
		/// <param name="heightMode">The meaning of height. ABSOLUTE_ALTITUDE will position the object on that altitude (can cross mountains). ABSOLUTE_CLAMPED will position the object always above the ground level. RELATIVE TO GROUND will add the height to the altitude at that position.</param>
		/// <param name="scaleOnZoom">If set to <c>true</c> the gameobject will increase/decrease its scale when zoomin in/out.</param>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination, float duration, float height, HEIGHT_OFFSET_MODE heightMode, bool scaleOnZoom) {
			GameObjectAnimator anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			anim.height = height;
			anim.heightMode = heightMode;
			anim.autoScale = scaleOnZoom;
			anim.MoveTo(destination, duration);
			return anim;
		}

		/// <summary>
		/// Smoothly moves this game object to given map destination along route of points.
		/// </summary>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, List<Vector2>route, float duration) {
			GameObjectAnimator anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			anim.MoveTo(route, duration);
			return anim;
		}

		public static Vector2 WMSK_GetMap2DPosition(this GameObject o) {
			GameObjectAnimator anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			return anim.currentMap2DLocation;
		}

		public static List<Vector2> WMSK_FindRoute(this GameObject o, Vector2 destination) {
			GameObjectAnimator anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			return anim.FindRoute(destination);
		}

	}

}