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
			public static bool BroadcastPing = false;
			public static bool CreatePersistentPing = false;
			public static Vector3? PersistentPingLocation = null;
			//private static Material PingTextMaterial = null; // See below

			public static Chat.WorldTextInstance FindExistingWorldText( Chat instance , long senderID )
			{
				return Traverse.Create( instance )
					.Method( "FindExistingWorldText" , new[] { typeof( long ) } )
					.GetValue< Chat.WorldTextInstance >( senderID );
			}

			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPrefix]
			private static void AddInworldTextPrefix(
				ref Chat __instance,
				ref long senderID,
				ref Vector3 position,
				ref string text,
				ref Talker.Type type,
				out string __state )
			{
				// Grab the text before the real method clobbers it
				__state = text.IsNullOrWhiteSpace() ? "PING" : text;

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
				else if( position.y < Common.EstimatedSeaLevel )
				{
					// Point is below sea level.
					// Show as slightly above to not break zero checks.
					// Order matters here to not conflict with fake dungeon depth.
					position.y = Common.EstimatedSeaLevel;
				}

				if( CreatePersistentPing && senderID == ZNet.GetUID() )
					PersistentPingLocation = position + Vector3.zero;
			}

			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPostfix]
			private static void AddInworldTextPostfix(
				ref Chat __instance,
				ref long senderID,
				ref Talker.Type type,
				ref string __state )
			{
				if( !IsEnabled.Value || type != Talker.Type.Ping )
					return;

				Chat.WorldTextInstance worldText = FindExistingWorldText( __instance , senderID );
				if( worldText == null )
					return;

				worldText.m_text = Localization.instance.Localize( __state ).ToUpperInvariant();
				MinimapPatch.RegeneratePingPins = true;

				// TODO: Consider improving the visibility of text against similar backgrounds
				//worldText.m_textMeshField.fontStyle = TMPro.FontStyles.Bold; // Does the font not have a bold style?
				//worldText.m_textMeshField.fontSize += 2; // Determine a good size, if different, and set it exactly
				//System.Console.WriteLine( $"AddInworldTextPostfix() worldText.m_textMeshField.fontSize is {worldText.m_textMeshField.fontSize}" );

				//if( PingTextMaterial == null )
				//{
				//	// FIXME: This helps, but the outline expand inwards too much and takes over the text.
				//	// Would it look better with a bold font? Negative outline widths do not work.
				//	// Also needs to be done regardless of __state.
				//	// https://stackoverflow.com/a/79833376
				//	PingTextMaterial = worldText.m_textMeshField.fontMaterial;
				//	PingTextMaterial.EnableKeyword( "OUTLINE_ON" );
				//	PingTextMaterial.SetFloat( "_OutlineWidth" , 0.2f );
				//	PingTextMaterial.SetColor( "_OutlineColor" , Color.black );
				//}

				// This gets runs often enough to raise an optimization eyebrow. Can we improve that?
				//worldText.m_textMeshField.fontMaterial = PingTextMaterial;
				//worldText.m_textMeshField.UpdateMeshPadding();
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
				if( !IsEnabled.Value || wt.m_type != Talker.Type.Ping )
					return;

				// These values get overwritten somehow if set in AddInworldTextPostfix()
				if( PersistentPingLocation != null
					&& wt.m_position == PersistentPingLocation.GetValueOrDefault()
					&& wt.m_talkerID == ZNet.GetUID() )
				{
					wt.m_textMeshField.color = Common.CopyColor( PersistentPingColor.Value );
					wt.m_timer = Mathf.NegativeInfinity;
				}
				else
				{
					wt.m_textMeshField.color = Common.CopyColor( PingColor.Value );
					wt.m_timer = __instance.m_worldTextTTL - (float)PingDuration.Value;
				}
			}

			[HarmonyPatch( "UpdateWorldTexts" )]
			[HarmonyPostfix]
			private static void UpdateWorldTextsPostfix( ref float dt , ref List< Chat.WorldTextInstance > ___m_worldTexts )
			{
				if( !IsEnabled.Value )
					return;

				foreach( Chat.WorldTextInstance worldText in ___m_worldTexts )
				{
					if( worldText.m_type == Talker.Type.Ping )
					{
						// Cancel out pings slowly moving upwards and causing the distance to change
						worldText.m_position.y -= dt * 0.15f;
						worldText.m_textMeshField.SetText( Common.PrettyPrintPingText( worldText ) );
					}
				}
			}
		}
	}
}
