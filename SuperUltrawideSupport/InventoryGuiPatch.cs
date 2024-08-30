using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( InventoryGui ) )]
		private class InventoryGuiPatch
		{
			private static HashSet< string > TransformNames = new HashSet< string >();

			private static void Reset()
			{
				foreach( string name in TransformNames )
					Lerper.Unregister( name );

				TransformNames.Clear();
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref InventoryGui __instance )
			{
				Reset();

				for( int index = 0 ; index < __instance.m_inventoryRoot.childCount ; index++ )
				{
					// It would be much nicer to do what we did before,
					// but AspectLerper wasn't designed to single out specific children
					RectTransform child = __instance.m_inventoryRoot.GetChild( index ) as RectTransform;
					if( child != null && child.name != "dropButton" )
					{
						TransformNames.Add( child.name );
						Lerper.Register( child );
						Lerper.Lerp( child );
					}
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix( ref InventoryGui __instance )
			{
				Reset();
			}
		}
	}
}
