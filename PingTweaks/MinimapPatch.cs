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

			private static Vector3 ScreenToWorldPoint( Minimap instance )
			{
				return Traverse.Create( instance )
					.Method( "ScreenToWorldPoint" , new[] { typeof( Vector3 ) } )
					.GetValue< Vector3 >( ZInput.mousePosition );
			}

			internal static Minimap.PinData GetClosestPin( Minimap instance , Vector3 pos , float radius , bool mustBeVisible = true )
			{
				return Traverse.Create( instance )
					.Method( "GetClosestPin" , new[] { typeof( Vector3 ) , typeof( float ) , typeof( bool ) } )
					.GetValue< Minimap.PinData >( pos , radius , mustBeVisible );
			}

			private static Chat.WorldTextInstance GetClosestPing( Vector3 pos , float radius , long uid = -1 )
			{
				List< Chat.WorldTextInstance > worldTexts = new List< Chat.WorldTextInstance >();
				Chat.instance.GetPingWorldTexts( worldTexts );
				Chat.WorldTextInstance bestWorldText = null;
				float bestDistance = float.PositiveInfinity;

				foreach( Chat.WorldTextInstance worldText in worldTexts )
				{
					if( worldText.m_type == Talker.Type.Ping && ( uid == -1 || uid == worldText.m_talkerID ) )
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
					// Pins don't get a meaningful Y coordinate without this.
					// It's probably OK that other things will now too.
					if( Common.IsLocalPlayerInDungeon() )
					{
						__result.y = Player.m_localPlayer.transform.position.y;
					}
					else
					{
						// Clamp to sea level like ChatPatch.AddInworldTextPrefix()
						Heightmap.GetHeight( __result , out __result.y );
						__result.y = Mathf.Max( Common.EstimatedSeaLevel , __result.y );
					}
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

				Vector3 worldPos = ScreenToWorldPoint( __instance );
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
				Vector3 pinPos = closestPing.m_position;
				if( pinPos.y == 0.0f )
					MapPointToWorldPostfix( ref pinPos );

				// It could be nice to pre-populate the new marker text input with the ping text
				// instead of committing to the received ping text right away
				__instance.AddPin(
					pinPos,
					___m_selectedType,
					closestPing.m_text,
					SavePinnedPings.Value,
					false,
					closestPing.m_talkerID );

				return false;
			}

			[HarmonyPatch( "OnMapMiddleClick" )]
			[HarmonyPrefix]
			private static bool OnMapMiddleClickPrefix(
				ref Minimap __instance,
				ref UIInputHandler handler,
				ref float ___m_largeZoom )
			{
				Vector3 worldPos = ScreenToWorldPoint( __instance );
				float searchRadius = __instance.m_removeRadius * ___m_largeZoom * 2.0f;
				Chat.WorldTextInstance existingPing = GetClosestPing( worldPos , searchRadius , ZNet.GetUID() );
				if( existingPing != null )
				{
					ChatPatch.BroadcastPing = false;
					ChatPatch.CreatePersistentPing = false;
					ChatPatch.PersistentPingLocation = null;
					existingPing.m_timer = Mathf.Infinity;
					return false;
				}

				ChatPatch.BroadcastPing = Common.CheckBroadcastKeybind();
				ChatPatch.CreatePersistentPing = Common.CheckPersistentKeybind();
				ChatPatch.PersistentPingLocation = null; // Unset early due to paranoia
				// Don't unset ChatPatch.CreatePersistentPing and ChatPatch.BroadcastPing in a postfix.
				// These values are valid until the next ping.

				return true;
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
					Minimap.PinData pinData = ___m_pingPins[ index ];
					TMPro.TMP_Text pinText = pinData.m_NamePinData?.PinNameText; // Doesn't exist right away
					if( pinText != null )
					{
						Chat.WorldTextInstance worldText = worldTexts[ index ];
						pinText.SetText( Common.PrettyPrintPingText( worldText ) );

						// FIXME: What really controls the color of the ping text on the map?
						// pinData.m_iconElement.color as set by Minimap.UpdatePins() has no effect here.
						// pinData.m_NamePinData.PinNameText.color is already set via the world text instance in ChatPatch.
						//Color color = ChatPatch.CreatePersistentPing
						//	&& ChatPatch.PersistentPingLocation != null
						//	&& worldText.m_position == ChatPatch.PersistentPingLocation.GetValueOrDefault()
						//	&& worldText.m_talkerID == ZNet.GetUID()
						//	? Common.CopyColor( PersistentPingColor.Value )
						//	: Common.CopyColor( PingColor.Value );
						//pinData.m_iconElement.color = color;
						//if( pinData.m_NamePinData != null && pinData.m_NamePinData.PinNameText.color != color )
						//	pinData.m_NamePinData.PinNameText.color = color;
					}
				}
			}
		}
	}
}
