using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( Hud ) )]
		private class HudPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref Hud __instance )
			{
				// "HUD", the parent of "hudroot", has loading screen stuff we don't want to resize
				RectTransform rectTransform = __instance.transform.Find( "hudroot" ) as RectTransform;
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
				Lerper.Unregister( "hudroot" );
			}
		}
	}
}
