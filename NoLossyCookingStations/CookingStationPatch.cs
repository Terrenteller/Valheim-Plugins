using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace NoLossyCookingStations
{
	public partial class NoLossyCookingStations
	{
		[HarmonyPatch( typeof( CookingStation ) )]
		private class CookingStationPatch
		{
			// We take a three-prong approach to preventing item loss:
			// 1. Forcefully take network ownership of the cooking station so adding items happens locally
			// 2. Limit the rate at which items can be added
			// 3. Dump overflow back into the world when we're the network owner

			private static double RateLimitTimeout = 0.0;

			[HarmonyPatch( "CookItem" )]
			[HarmonyPrefix]
			private static bool CookItemPrefix(
				ref CookingStation __instance,
				ref ZNetView ___m_nview,
				ref bool __result,
				ref Humanoid user )
			{
				bool? ret = Common.SanityCheckInteraction( ___m_nview , ref __result , ref RateLimitTimeout );
				if( ret != null )
					return ret == true;

				__instance.StartCoroutine(
					Common.DelayedInteraction(
						new WeakReference< Humanoid >( user ),
						new WeakReference< CookingStation >( __instance ),
						"OnInteract",
						new WeakReference< object >[] { new WeakReference< object >( user ) } ) );

				return false;
			}

			[HarmonyPatch( "RPC_AddItem" )]
			[HarmonyPrefix]
			private static bool RPC_AddItemPrefix(
				ref CookingStation __instance,
				ref ZNetView ___m_nview,
				ref long sender,
				ref string itemName )
			{
				// This method is redundant with us taking network ownership but still applies when a peer
				// does not have this plugin and over-fills a cooking station for which we are the network owner

				// Vanilla does not have this ownership check as of writing
				if( !IsEnabled.Value || !___m_nview.IsOwner() )
					return true;

				bool isItemAllowed = Traverse.Create( __instance )
					.Method( "IsItemAllowed" , new[] { typeof( string ) } )
					.GetValue< bool >( itemName );
				int freeSlotIndex = Traverse.Create( __instance )
					.Method( "GetFreeSlot" )
					.GetValue< int >();

				if( !isItemAllowed || freeSlotIndex != -1 )
					return true;

				long derefSender = sender;
				ZNet.PlayerInfo senderInfo = ZNet.instance.GetPlayerList()
					.FirstOrDefault( x => derefSender == x.m_characterID.ID );

				if( senderInfo.m_position != null )
				{
					Traverse.Create( __instance )
						.Method( "SpawnItem" , new[] { typeof( string ) , typeof( int ) , typeof( Vector3 ) } )
						.GetValue( itemName , __instance.m_slots.Length - 1 , senderInfo.m_position );

					return false;
				}

				return true;
			}
		}
	}
}
