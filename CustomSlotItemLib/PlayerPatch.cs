using HarmonyLib;

namespace CustomSlotItemLib
{
	public partial class CustomSlotItemLib
	{
		[HarmonyPatch( typeof( Player ) )]
		internal class PlayerPatch
		{
			[HarmonyPatch( "Load" )]
			[HarmonyPostfix]
			static void LoadPostfix( ref Player __instance )
			{
				foreach( ItemDrop.ItemData itemData in __instance.GetInventory().GetEquippedItems() )
				{
					string slotName = CustomSlotManager.GetCustomSlotName( itemData );
					if( slotName != null )
						CustomSlotManager.SetSlotItem( __instance , slotName , itemData );
				}
			}
		}
	}
}
