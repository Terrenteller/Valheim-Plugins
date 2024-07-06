using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( MessageHud ) )]
		private class MessageHudPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref MessageHud __instance )
			{
				{
					Transform transform = __instance.transform;
					RectTransform rectTransform = Common.FindParentOrSelf( transform , "HudMessage" ) as RectTransform;
					if( rectTransform != null )
					{
						Lerper.Register( rectTransform );
						Lerper.Lerp( rectTransform );
					}
				}

				{
					Transform transform = __instance.m_messageText.gameObject.transform;
					RectTransform rectTransform = Common.FindParentOrSelf( transform , "TopLeftMessage" ) as RectTransform;
					if( rectTransform != null )
					{
						Lerper.Register( rectTransform );
						Lerper.Lerp( rectTransform );
					}
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				Lerper.Unregister( "HudMessage" );
				Lerper.Unregister( "TopLeftMessage" );
			}
		}
	}
}
