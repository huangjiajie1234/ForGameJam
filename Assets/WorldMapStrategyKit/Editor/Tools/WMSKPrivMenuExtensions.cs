using UnityEngine;
using UnityEditor;
using System.Collections;

namespace WorldMapStrategyKit {

	public static class WMSKMenuExtensions
	{
		[MenuItem("GameObject/Create Other/WMSK Viewport")]
		static void CreateWMSKViewport()
		{
			GameObject viewport = GameObject.Instantiate(Resources.Load<GameObject>("WMSK/Prefabs/Viewport"));
			viewport.name = "Viewport";
			if (!WMSK.instanceExists) {
				GameObject wmsk = GameObject.Instantiate(Resources.Load<GameObject>("WMSK/Prefabs/WorldMapStrategyKit"));
				wmsk.name = "WorldMapStrategyKit";
			}
			WMSK.instance.renderViewport = viewport;
		}
	}
}
