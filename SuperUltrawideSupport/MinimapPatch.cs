using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( Minimap ) )]
		private class MinimapPatch
		{
			private static RectTransform largeTransform;
			private static float minX;
			private static float minY;
			private static float maxX;
			private static float maxY;
			private static bool AppliedInverseLerp = false;

			public static void Update( bool userUpdate )
			{
				if( largeTransform == null || Minimap.instance.m_mode != Minimap.MapMode.Large )
					return;

				// We only need to cycle the map mode for updates when the big map is open
				// and the user is changing settings that affect the size of the big map
				// because calling Minimap.Update() directly doesn't update pin positions.
				// Programmatic calls may happen too early and bad things will happen.
				if( userUpdate )
				{
					Minimap.instance.SetMapMode( Minimap.MapMode.Small );
					Minimap.instance.SetMapMode( Minimap.MapMode.Large );
				}

				// Undo interpolation as our aspect ratio may have changed
				largeTransform.anchorMin = new Vector2( minX , minY );
				largeTransform.anchorMax = new Vector2( maxX , maxY );
				AppliedInverseLerp = false;

				if( IsEnabled.Value && FullSizeMap.Value && !AppliedInverseLerp )
				{
					Lerper.InverseLerp( largeTransform );
					AppliedInverseLerp = true;
				}
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref Minimap __instance )
			{
				largeTransform = __instance.m_largeRoot.transform as RectTransform;
				minX = largeTransform.anchorMin.x;
				minY = largeTransform.anchorMin.y;
				maxX = largeTransform.anchorMax.x;
				maxY = largeTransform.anchorMax.y;
				AppliedInverseLerp = false;
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				if( largeTransform != null )
				{
					largeTransform.anchorMin = new Vector2( minX , minY );
					largeTransform.anchorMax = new Vector2( maxX , maxY );
				}

				largeTransform = null;
				minX = 0.0f;
				minY = 0.0f;
				maxX = 0.0f;
				maxY = 0.0f;
				AppliedInverseLerp = false;
			}

			[HarmonyPatch( "SetMapMode" )]
			[HarmonyPrefix]
			private static void SetMapModePrefix( ref Minimap __instance , ref Minimap.MapMode mode , out Minimap.MapMode __state )
			{
				__state = __instance.m_mode;
			}

			[HarmonyPatch( "SetMapMode" )]
			[HarmonyPostfix]
			private static void SetMapModePostfix( ref Minimap __instance , ref Minimap.MapMode mode , ref Minimap.MapMode __state )
			{
				if( __instance.m_mode != __state )
					Update( false );
			}
		}
	}
}
