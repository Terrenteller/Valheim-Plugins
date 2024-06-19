using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( StoreGui ) )]
		private class StoreGuiPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref StoreGui __instance )
			{
				RectTransform rectTransform = Common.FindParentOrSelf( __instance.transform , "Store_Screen" ) as RectTransform;
				if( rectTransform != null )
				{
					Lerper.Register( rectTransform );
					Lerper.Lerp( rectTransform );
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				Lerper.Unregister( "Store_Screen" );
			}
		}
	}
}
