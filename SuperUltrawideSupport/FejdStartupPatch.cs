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
			private static HashSet< string > TransformPaths = new HashSet< string >();

			private static void Reset()
			{
				foreach( string transformPath in TransformPaths )
					Lerper.Unregister( transformPath );

				TransformPaths.Clear();
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
							TransformPaths.Add( child.name );
							Lerper.Register( child );
							Lerper.Lerp( child );
						}
					}
				}

				if( TransformPaths.Count > 0 )
					Lerper.Update();
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
