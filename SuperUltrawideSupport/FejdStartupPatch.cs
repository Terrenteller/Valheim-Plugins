using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( FejdStartup ) )]
		private class FejdStartupPatch
		{
			private static HashSet< string > TransformNames = new HashSet< string >();

			// TODO: Should we apply this pattern elsewhere?
			private static void Reset()
			{
				foreach( string name in TransformNames )
					Lerper.Unregister( name );

				TransformNames.Clear();
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref FejdStartup __instance )
			{
				Reset();

				// The amount of hardcoded text here is unfortunate
				Transform startScreen = Common.FindParentOrSelf( __instance.m_creditsList , "StartGui" );
				if( startScreen != null )
				{
					for( int index = 0 ; index < startScreen.childCount ; index++ )
					{
						RectTransform child = startScreen.GetChild( index ) as RectTransform;
						if( child != null
							&& child.name != "BLACK"
							&& child.name != "Loading"
							&& child.name != "PleaseWait"
							&& child.name != "Scaled 3D Viewport" )
						{
							TransformNames.Add( child.name );
							Lerper.Register( child );
							Lerper.Lerp( child );
						}
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
