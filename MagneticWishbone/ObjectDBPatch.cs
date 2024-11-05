using HarmonyLib;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		[HarmonyPatch( typeof( ObjectDB ) )]
		private class ObjectDBPatch
		{
			public static void UpdateWishboneMaxQuality()
			{
				var sharedData = ObjectDB.instance
					?.GetItemPrefab( Common.WishbonePrefabName )
					?.GetComponent< ItemDrop >()
					?.m_itemData
					?.m_shared;
				if( sharedData != null )
					sharedData.m_maxQuality = MaxAllowedQuality();
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPrefix]
			private static void AwakePrefix()
			{
				MagneticWishboneRecipe.Instance.Value = null;
			}

			[HarmonyPatch( "UpdateRegisters" )]
			[HarmonyPostfix]
			private static void UpdateRegistersPostfix()
			{
				// This needs to be done ASAP. It's unclear how "shared" ItemDrop.ItemData.SharedData is,
				// or even what "shared" truly means. If the recipe tries to set this at a later time,
				// it will not apply to inventory items. This prevents quality values from being shown
				// and may cause the crafting GUI to say the item is at max quality when it is not.
				UpdateWishboneMaxQuality();
			}
		}
	}
}
