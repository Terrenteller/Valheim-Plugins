using HarmonyLib;

namespace CustomSlotItemLib
{
	public partial class CustomSlotItemLib
	{
		[HarmonyPatch( typeof( ItemDrop.ItemData ) )]
		[HarmonyPriority( Priority.High )]
		private class ItemDropItemDataPatch
		{
			[HarmonyPatch( "IsEquipable" )]
			[HarmonyPostfix]
			static void IsEquipablePostfix( ref bool __result , ref ItemDrop.ItemData __instance )
			{
				__result |= CustomSlotManager.IsCustomSlotItem( __instance );
			}
		}
	}
}
