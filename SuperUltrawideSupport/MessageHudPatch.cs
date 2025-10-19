using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( MessageHud ) )]
		private class MessageHudPatch
		{
			private static HashSet< string > TransformPaths = new HashSet< string >();

			private static void Reset()
			{
				foreach( string transformPath in TransformPaths )
					Lerper.Unregister( transformPath );

				TransformPaths.Clear();
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref MessageHud __instance )
			{
				{
					Transform transform = __instance.transform;
					RectTransform rectTransform = Common.FindParentOrSelf( transform , "HudMessage" ) as RectTransform;
					if( rectTransform != null )
					{
						TransformPaths.Add( AspectLerper.AbsoluteTransformPath( rectTransform ) );
						Lerper.RegisterLerpAndUpdate( rectTransform );
					}
				}

				{
					Transform transform = __instance.m_messageText.gameObject.transform;
					RectTransform rectTransform = Common.FindChildOfParent( transform , "root" , "TopLeftMessage" );
					if( rectTransform != null )
					{
						TransformPaths.Add( AspectLerper.AbsoluteTransformPath( rectTransform ) );
						Lerper.RegisterLerpAndUpdate( rectTransform );
					}
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				Reset();
			}
		}
	}
}
