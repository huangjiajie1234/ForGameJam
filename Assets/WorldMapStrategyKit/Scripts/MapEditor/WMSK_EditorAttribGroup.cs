using UnityEngine;
using System.Collections;
using WorldMapStrategyKit;

namespace WorldMapStrategyKit_Editor
{
	public class WMSK_EditorAttribGroup
	{
		public IExtendableAttribute itemGroup;
		public string newTagKey;
		public string newTagValue;
		public bool foldOut;

		public void SetItemGroup(IExtendableAttribute item) {
			itemGroup = item;
			newTagKey = "";
			newTagValue = "";
		}

	}

}
