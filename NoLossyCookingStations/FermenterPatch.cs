using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NoLossyCookingStations
{
	public partial class NoLossyCookingStations
	{
		[HarmonyPatch( typeof( Fermenter ) )]
		private class FermenterPatch
		{
			private static double RateLimitTimeout = 0.0;

			[HarmonyPatch( "Interact" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > InteractTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				// Locate and replace pop with return after the call to Fermenter.AddItem()
				// so the interact animation is not played if adding an item fails

				List< CodeInstruction > instructions = new List< CodeInstruction >( instructionsIn );
				for( int index = 0 ; ( index + 1 ) < instructions.Count ; index++ )
				{
					if( instructions[ index ].opcode == OpCodes.Call && instructions[ index + 1 ].opcode == OpCodes.Pop )
					{
						MethodInfo methodInfo = (MethodInfo)instructions[ index ].operand;
						if( methodInfo.DeclaringType == typeof( Fermenter )
							&& methodInfo.Name.CompareTo( "AddItem" ) == 0
							&& methodInfo.ReturnType == typeof( bool ) )
						{
							instructions[ index + 1 ] = new CodeInstruction( OpCodes.Ret );
							break;
						}
					}
				}

				return instructions;
			}

			[HarmonyPatch( "AddItem" )]
			[HarmonyPrefix]
			private static bool AddItemPrefix(
				ref Fermenter __instance,
				ref ZNetView ___m_nview,
				ref bool __result,
				ref Humanoid user,
				ref ItemDrop.ItemData item )
			{
				bool? ret = Common.SanityCheckInteraction( ___m_nview , ref __result , ref RateLimitTimeout );
				if( ret != null )
					return ret == true;

				__instance.StartCoroutine(
					Common.DelayedInteraction(
						new WeakReference< Humanoid >( user ),
						new WeakReference< Fermenter >( __instance ),
						"AddItem",
						new WeakReference< object >[] {
							new WeakReference< object >( user ),
							new WeakReference< object >( item ) } ) );

				return false;
			}

			[HarmonyPatch( "RPC_AddItem" )]
			[HarmonyPrefix]
			private static bool RPC_AddItemPrefix( ref Fermenter __instance , ref ZNetView ___m_nview , ref string name )
			{
				if( !IsEnabled.Value || !___m_nview.IsOwner() )
					return true;

				bool itemIsAllowed = Traverse.Create( __instance )
					.Method( "IsItemAllowed" , new[] { typeof( string ) } )
					.GetValue< bool >( name );
				string content = Traverse.Create( __instance )
					.Method( "GetContent" )
					.GetValue< string >();

				if( itemIsAllowed && !content.IsNullOrWhiteSpace() )
				{
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab( name );
					Vector3 position = __instance.m_outputPoint.position + Vector3.up;
					Instantiate( itemPrefab , position , Quaternion.identity );

					return false;
				}

				return true;
			}
		}
	}
}
