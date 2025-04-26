using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Chat ) )]
		private class ChatPatch
		{
			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPrefix]
			private static void AddInworldTextPrefix(
				ref Vector3 position,
				ref string text,
				ref Talker.Type type,
				out string __state )
			{
				// Grab the text before the real method modifies it
				__state = text;

				if( !IsEnabled.Value || type != Talker.Type.Ping )
					return;

				float playerY = Player.m_localPlayer.transform.position.y;

				if( position.y == 0.0f )
				{
					// Put height-agnostic pings at the player's height for convenience.
					// They come from old markers or players without this plugin.
					position.y = playerY;
				}
				else if( position.y >= Common.DungeonMinHeight )
				{
					// Put the ping underground if we're not in a dungeon.
					// If we are, there's nothing to do.
					if( !Common.IsLocalPlayerInDungeon() )
						position.y = playerY - Common.DungeonFakeDepth;
				}
				else if( Common.IsLocalPlayerInDungeon() )
				{
					// The position is on the surface which is actually below us.
					// Add some height to make it look like we're underground.
					// FIXME: This is far from perfect as the difference is relative.
					// We may be able to get the ceiling of the dungeon from ZoneSystem
					// and use that in place of the player's elevation.
					// Same for the reverse case.
					position.y = playerY + Common.DungeonFakeDepth;
				}
			}

			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPostfix]
			private static void AddInworldTextPostfix(
				ref Chat __instance,
				ref long senderID,
				ref Talker.Type type,
				ref string __state )
			{
				if( type != Talker.Type.Ping || !IsEnabled.Value || __state.IsNullOrWhiteSpace() )
					return;

				Chat.WorldTextInstance worldText = Traverse.Create( __instance )
					.Method( "FindExistingWorldText" , new[] { typeof( long ) } )
					.GetValue< Chat.WorldTextInstance >( senderID );
				if( worldText == null )
					return;

				worldText.m_text = Localization.instance.Localize( __state ).ToUpperInvariant();
				Traverse.Create( __instance )
					.Method( "UpdateWorldTextField" , new[] { typeof( Chat.WorldTextInstance ) } )
					.GetValue( worldText );

				MinimapPatch.RegeneratePingPins = true;
			}

			[HarmonyPatch( "SendPing" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > SendPingTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				// Pave over the overwrite of the original Y coordinate with the player's Y coordinate

				List< CodeInstruction > instructions = new List< CodeInstruction >( instructionsIn );
				for( int index = 0 ; ( index + 2 ) < instructions.Count ; index++ )
				{
					if( instructions[ index + 0 ].opcode == OpCodes.Ldarg_1
						&& instructions[ index + 1 ].opcode == OpCodes.Stloc_1
						&& instructions[ index + 2 ].opcode == OpCodes.Ldloca_S )
					{
						for( int nopIndex = index + 2 ; nopIndex <= ( index + 7 ) ; nopIndex++ )
							instructions[ nopIndex ].opcode = OpCodes.Nop;
					}
				}

				return instructions;
			}

			[HarmonyPatch( "UpdateWorldTextField" )]
			[HarmonyPrefix]
			private static void UpdateWorldTextFieldPrefix( ref Chat __instance , ref Chat.WorldTextInstance wt )
			{
				// No need to set all the time in UpdateWorldTextsPostfix()
				if( IsEnabled.Value && wt.m_type == Talker.Type.Ping )
				{
					wt.m_textMeshField.color = Common.CopyColor( PingColor.Value );
					wt.m_timer = __instance.m_worldTextTTL - (float)PingDuration.Value;
				}
			}

			[HarmonyPatch( "UpdateWorldTexts" )]
			[HarmonyPrefix]
			private static void UpdateWorldTextsPrefix( ref float dt , ref List< Chat.WorldTextInstance > ___m_worldTexts )
			{
				// Stop pings from slowly moving upwards and causing the distance to change
				if( IsEnabled.Value )
					foreach( Chat.WorldTextInstance worldText in ___m_worldTexts )
						if( worldText.m_type == Talker.Type.Ping )
							worldText.m_position.y -= dt * 0.15f; // Taken from the original method
			}

			[HarmonyPatch( "UpdateWorldTexts" )]
			[HarmonyPostfix]
			private static void UpdateWorldTextsPostfix( ref float dt , ref List< Chat.WorldTextInstance > ___m_worldTexts )
			{
				if( !IsEnabled.Value )
					return;

				foreach( Chat.WorldTextInstance worldText in ___m_worldTexts )
				{
					// Limit to pings for consistency
					if( worldText.m_type != Talker.Type.Ping )
						continue;

					worldText.m_textMeshField.SetText(
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
