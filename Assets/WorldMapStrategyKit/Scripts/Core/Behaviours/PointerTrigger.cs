using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit {
	public class PointerTrigger : MonoBehaviour {

		public WMSK map;
	
		void Awake () {
			if (GetComponent<MeshCollider>()==null) gameObject.AddComponent<MeshCollider>();
		}
		
		void OnMouseEnter () {
			if (map!=null)
				map.OnMouseEnter();
		}
		
		void OnMouseExit () {
			if (map!=null)
				map.OnMouseExit();
		}

	}
}
