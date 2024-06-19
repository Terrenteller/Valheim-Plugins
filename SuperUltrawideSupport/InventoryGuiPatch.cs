using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( InventoryGui ) )]
		private class InventoryGuiPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref InventoryGui __instance )
			{
				RectTransform rectTransform = Common.FindParentOrSelf( __instance.m_inventoryRoot , "Inventory_screen" ) as RectTransform;
				if( rectTransform != null )
				{
					Lerper.Register( rectTransform );
					Lerper.Update();
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix( ref InventoryGui __instance )
			{
				Lerper.Unregister( "Inventory_screen" );
			}
		}
	}
}
