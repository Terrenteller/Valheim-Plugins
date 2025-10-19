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
			private static string TransformPath = null;

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref StoreGui __instance )
			{
				RectTransform rectTransform = Common.FindChildOfParent( __instance.transform , "Store" , "Store_Screen" );
				if( rectTransform != null )
				{
					TransformPath = AspectLerper.AbsoluteTransformPath( rectTransform );
					Lerper.RegisterLerpAndUpdate( rectTransform );
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				Lerper.Unregister( TransformPath );
			}
		}
	}
}
