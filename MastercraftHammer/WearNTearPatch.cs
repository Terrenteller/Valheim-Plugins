using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MastercraftHammer
{
	public partial class MastercraftHammer
	{
		[HarmonyPatch( typeof( WearNTear ) )]
		private class WearNTearPatch
		{
			internal static List< uint > WornNTorn = new List< uint >();

			private static readonly float[] DamageStateMultipliers = { 0.0f , 0.25f , 0.75f , 1.0f };

			private static bool DoesMaterialPassFilter( WearNTear.MaterialType materialType )
			{
				return ( materialType == WearNTear.MaterialType.HardWood && IncludeHardWood.Value )
					|| ( materialType == WearNTear.MaterialType.Iron && IncludeIron.Value )
					|| ( materialType == WearNTear.MaterialType.Marble && IncludeMarble.Value )
					|| ( materialType == WearNTear.MaterialType.Stone && IncludeStone.Value )
					|| ( materialType == WearNTear.MaterialType.Wood && IncludeWood.Value );
			}

			private static float? GetRandomHealth( WearNTear instance , bool forPlacement )
			{
				int[] stateWeights =
				{
					!forPlacement && AllowDestruction.Value ? DestructionRatio.Value : 0,
					AllowBroken.Value ? BrokenRatio.Value : 0,
					AllowWorn.Value ? WornRatio.Value : 0,
					AllowNew.Value ? NewRatio.Value : 0
				};

				int maxWeight = 0;
				foreach( int weight in stateWeights )
					maxWeight += weight;

				if( maxWeight == 0 )
					return null;

				int randomWeight = Random.Range( 0 , maxWeight );
				int weightLimit = 0;

				for( int index = 0 ; index < stateWeights.Length ; index++ )
				{
					weightLimit += stateWeights[ index ];
					if( randomWeight < weightLimit )
						return Mathf.Floor( instance.m_health * DamageStateMultipliers[ index ] );
				}

				return null;
			}

			private static void RandomizeHealth( WearNTear instance , ZNetView netView , bool forPlacement )
			{
				ZDO zdo = netView.GetZDO();
				if( !DoesMaterialPassFilter( instance.m_materialType )
					|| ( EnableSingleTouch.Value && WornNTorn.Contains( zdo.m_uid.ID ) ) )
				{
					return;
				}

				float health = GetRandomHealth( instance , forPlacement ) ?? instance.m_health;
				if( health == 0.0f )
				{
					instance.Remove();
					return;
				}

				if( EnableSingleTouch.Value )
					WornNTorn.Add( zdo.m_uid.ID );

				if( health != zdo.GetFloat( "health" , instance.m_health ) )
				{
					zdo.Set( "health" , health );
					netView.InvokeRPC( ZNetView.Everybody , "WNTHealthChanged" , health );
				}
			}

			[HarmonyPatch( "OnPlaced" )]
			[HarmonyPostfix]
			private static void OnPlacedPostfix( ref WearNTear __instance , ref ZNetView ___m_nview )
			{
				if( IsEnabled.Value && RandomizeStateOnPlacement.Value )
					RandomizeHealth( __instance , ___m_nview , true );
			}
			
			[HarmonyPatch( "Remove" )]
			[HarmonyPrefix]
			private static void RemovePrefix( ref WearNTear __instance , ref ZNetView ___m_nview , out uint __state )
			{
				__state = ___m_nview.GetZDO().m_uid.ID;
			}
			
			[HarmonyPatch( "Remove" )]
			[HarmonyPostfix]
			private static void RemovePostfix( ref WearNTear __instance , ref ZNetView ___m_nview , ref uint __state )
			{
				WornNTorn.Remove( __state );
			}
			
			[HarmonyPatch( "Repair" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > RepairTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				// Pave over the health check and only do it in a prefix when the plugin is NOT enabled

				List< CodeInstruction > instructions = new List< CodeInstruction >( instructionsIn );
				int lastRetIndex = -1;

				for( int index = 0 ; ( index + 3 ) < instructions.Count ; index++ )
				{
					if( lastRetIndex != -1
						&& instructions[ index + 0 ].opcode == OpCodes.Ldfld
						&& instructions[ index + 1 ].opcode == OpCodes.Blt_Un // ILSpy says Blt_Un_S
						&& instructions[ index + 2 ].opcode == OpCodes.Ldc_I4_0
						&& instructions[ index + 3 ].opcode == OpCodes.Ret )
					{
						FieldInfo fieldInfo = (FieldInfo)instructions[ index ].operand;
						if( fieldInfo.Name == "m_health" )
							for( int nopIndex = lastRetIndex + 1 ; nopIndex <= ( index + 3 ) ; nopIndex++ )
								instructions[ nopIndex ].opcode = OpCodes.Nop;
					}
					else if( instructions[ index ].opcode == OpCodes.Ret )
						lastRetIndex = index;
				}

				return instructions;
			}

			[HarmonyPatch( "Repair" )]
			[HarmonyPrefix]
			private static bool RepairPrefix( ref WearNTear __instance , ref ZNetView ___m_nview , ref bool __result )
			{
				if( !IsEnabled.Value
					&& ___m_nview.IsValid()
					&& ___m_nview.GetZDO().GetFloat( "health" , __instance.m_health ) >= __instance.m_health )
				{
					__result = false;
					return false;
				}

				return true;
			}

			[HarmonyPatch( "RPC_Repair" )]
			[HarmonyPrefix]
			private static bool RPC_RepairPrefix( ref WearNTear __instance , ref ZNetView ___m_nview , ref long sender )
			{
				if( IsEnabled.Value
					&& RandomizeStateOnRepair.Value
					&& sender == ZDOMan.GetSessionID()
					&& ___m_nview.IsValid()
					&& ___m_nview.IsOwner()
					&& ZNet.instance
					&& ZNet.instance.IsServer()
					&& ZNet.instance.GetPeerConnections() == 0
					&& Player.m_localPlayer )
				{
					RandomizeHealth( __instance , ___m_nview , false );
					return false;
				}

				return true;
			}
		}
	}
}
