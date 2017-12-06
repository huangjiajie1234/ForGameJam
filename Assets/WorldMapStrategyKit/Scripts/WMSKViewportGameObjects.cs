// World Strategy Kit for Unity - Main Script
// Copyright (C) Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{

	public partial class WMSK : MonoBehaviour
	{


	#region Public properties


	#endregion

		#region Viewport GameObject (VGOs) APIs

		/// <summary>
		/// Registers the game object in the viewport collection. It's position will be monitored by the viewport updates.
		/// </summary>
		internal void VGORegisterGameObject(GameObjectAnimator o) {
			if (vgos==null) return;
			if (o.uniqueId==0) o.uniqueId = vgos.Count+1;
			if (vgos.ContainsKey(o.uniqueId)) vgos[o.uniqueId] = o; else vgos.Add(o.uniqueId, o);
		}
		
		/// <summary>
		/// Returns true if the game object is already registered in the viewport collection.
		/// </summary>
		/// <returns><c>true</c>, if viewport is registered was rendered, <c>false</c> otherwise.</returns>
		internal bool VGOIsRegistered(GameObjectAnimator o) {
			if (vgos==null) return false;
			return vgos.ContainsKey(o.uniqueId);
		}

		/// <summary>
		/// Toggles the visibility of a group of GameObjects in the viewport.
		/// </summary>
		public void VGOToggleGroupVisibility(int group, bool isVisible) {
			if (vgos==null) return;
			foreach (KeyValuePair<int, GameObjectAnimator> keyValue in vgos) {
				GameObjectAnimator go = keyValue.Value;
				if (go!=null && go.group==group) {
					GameObject o = go.gameObject;
					if (o.activeSelf!=isVisible) {
						o.SetActive(isVisible);
						go.UpdateVisibility(true);
					}
				}
			}
		}

		/// <summary>
		/// Returns the registered Game Object with a given unique identifier
		/// </summary>
		public GameObjectAnimator VGOGet(int uniqueId) {
			if (vgos.ContainsKey(uniqueId)) {
				return vgos[uniqueId];
			}
			return null;
		}

		#endregion

	}

}