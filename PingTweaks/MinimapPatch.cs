using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Minimap ) )]
		private class MinimapPatch
		{
			internal static bool RegeneratePingPins = false;

			internal static Minimap.PinData GetClosestPin( Minimap instance , Vector3 pos , float radius , bool mustBeVisible = true )
			{
				return Traverse.Create( instance )
					.Method( "GetClosestPin" , new[] { typeof( Vector3 ) , typeof( float ) , typeof( bool ) } )
					.GetValue< Minimap.PinData >( pos , radius , true );
			}

			private static Chat.WorldTextInstance GetClosestPing( Vector3 pos , float radius )
			{
				List< Chat.WorldTextInstance > worldTexts = new List< Chat.WorldTextInstance >();
				Chat.instance.GetPingWorldTexts( worldTexts );
				Chat.WorldTextInstance bestWorldText = null;
				float bestDistance = float.PositiveInfinity;

				foreach( Chat.WorldTextInstance worldText in worldTexts )
				{
					if( worldText.m_type == Talker.Type.Ping )
					{
						float distance = Utils.DistanceXZ( pos , worldText.m_position );
						if( distance < radius && distance < bestDistance )
						{
							bestWorldText = worldText;
							bestDistance = distance;
						}
					}
				}

				return bestWorldText;
			}

			[HarmonyPatch( "MapPointToWorld" )]
			[HarmonyPostfix]
			private static void MapPointToWorldPostfix( ref Vector3 __result )
			{
				if( IsEnabled.Value )
				{
					// Pins don't get a Y coordinate without this.
					// It's probably OK that other things will now too.
					if( Common.IsLocalPlayerInDungeon() )
						__result.y = Player.m_localPlayer.transform.position.y;
					else
						Heightmap.GetHeight( __result , out __result.y );
				}
			}

			[HarmonyPatch( "OnMapDblClick" )]
			[HarmonyPrefix]
			private static bool OnMapDblClickPrefix(
				ref Minimap __instance,
				ref float ___m_largeZoom,
				ref List< Minimap.PinData > ___m_pins,
				ref Minimap.PinType ___m_selectedType )
			{
				if( !IsEnabled.Value )
					return true;

				Vector3 worldPos = Traverse.Create( __instance )
					.Method( "ScreenToWorldPoint" , new[] { typeof( Vector3 ) } )
					.GetValue< Vector3 >( ZInput.mousePosition );
				float searchRadius = __instance.m_removeRadius * ___m_largeZoom * 2.0f;

				// Vanilla treats a double-click as a single-click first.
				// This may cause an unowned pin to be "claimed" or checked/unchecked.
				// Use the same check Minimap.OnMapLeftClick() does to block the double-click.
				// TODO: If we wanted to be really fancy, we could try to detect this
				// and undo whatever damage the misinterpreted single-click did.
				if( GetClosestPin( __instance , worldPos , searchRadius ) != null )
					return false;

				Chat.WorldTextInstance closestPing = GetClosestPing( worldPos , searchRadius );
				if( closestPing == null || closestPing.m_talkerID == ZNet.GetUID() )
					return true;

				// If the closest ping doesn't have a Y coordinate, it came from a player without this plugin.
				// A postfix of ours happens to do exactly what we want.
				if( closestPing.m_position.y == 0.0f )
					MapPointToWorldPostfix( ref closestPing.m_position );

				// It could be nice to pre-populate the new marker text input with the ping text
				// instead of committing to the received ping text right away
				__instance.AddPin(
					closestPing.m_position,
					___m_selectedType,
					closestPing.m_text,
					SavePinnedPings.Value,
					false,
					closestPing.m_talkerID );

				return false;
			}

			[HarmonyPatch( "UpdatePingPins" )]
			[HarmonyPrefix]
			private static void UpdatePingPinsPrefix( ref Minimap __instance , ref List< Minimap.PinData > ___m_pingPins )
			{
				// Minimap.UpdatePingPins() only updates the unlocalized PinData.m_name,
				// not the localized PinData.m_NamePinData. Start over.
				if( RegeneratePingPins )
				{
					foreach( Minimap.PinData pingPin in ___m_pingPins )
						__instance.RemovePin( pingPin );

					___m_pingPins.Clear();
					RegeneratePingPins = false;
				}
			}

			[HarmonyPatch( "UpdatePingPins" )]
			[HarmonyPostfix]
			private static void UpdatePingPinsPostfix( ref List< Minimap.PinData > ___m_pingPins , ref bool ___m_pinUpdateRequired )
			{
				if( !IsEnabled.Value )
					return;

				// We expect the order to match since they are created in order
				List< Chat.WorldTextInstance > worldTexts = new List< Chat.WorldTextInstance >();
				Chat.instance.GetPingWorldTexts( worldTexts );
				for( int index = 0 ; index < worldTexts.Count ; index++ )
				{
					// Doesn't exist right away
					TMPro.TMP_Text pinText = ___m_pingPins[ index ].m_NamePinData?.PinNameText;
					if( pinText != null )
					{
						Chat.WorldTextInstance worldText = worldTexts[ index ];
						pinText.SetText(
							string.Format(
								"{0}\n{1}\n{2}",
								worldText.m_name,
								worldText.m_text,
								Common.PrettyPrintDistance( Player.m_localPlayer.transform.position , worldText.m_position ) ) );
					}
				}
			}
		}
	}
}
