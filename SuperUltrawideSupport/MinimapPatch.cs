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
			private static RectTransform LargeTransform;
			private static float MinX;
			private static float MinY;
			private static float MaxX;
			private static float MaxY;
			private static bool AppliedInverseLerp = false;

			public static void Update( bool userUpdate )
			{
				if( LargeTransform == null || Minimap.instance.m_mode != Minimap.MapMode.Large )
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
				LargeTransform.anchorMin = new Vector2( MinX , MinY );
				LargeTransform.anchorMax = new Vector2( MaxX , MaxY );
				AppliedInverseLerp = false;

				if( IsEnabled.Value && FullSizeMap.Value && !AppliedInverseLerp )
				{
					Lerper.InverseLerp( LargeTransform );
					AppliedInverseLerp = true;
				}
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref Minimap __instance )
			{
				LargeTransform = __instance.m_largeRoot.transform as RectTransform;
				MinX = LargeTransform.anchorMin.x;
				MinY = LargeTransform.anchorMin.y;
				MaxX = LargeTransform.anchorMax.x;
				MaxY = LargeTransform.anchorMax.y;
				AppliedInverseLerp = false;
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				if( LargeTransform != null )
				{
					LargeTransform.anchorMin = new Vector2( MinX , MinY );
					LargeTransform.anchorMax = new Vector2( MaxX , MaxY );
				}

				LargeTransform = null;
				MinX = 0.0f;
				MinY = 0.0f;
				MaxX = 0.0f;
				MaxY = 0.0f;
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
