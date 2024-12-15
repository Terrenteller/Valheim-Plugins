using HarmonyLib;
using System;
using UnityEngine;

namespace NoLossyCookingStations
{
	public partial class NoLossyCookingStations
	{
		// Everything that processes items over time that is not the oven or fermenter appears to be a Smelter
		[HarmonyPatch( typeof( Smelter ) )]
		private class SmelterPatch
		{
			private static double RateLimitTimeout = 0.0;

			[HarmonyPatch( "OnAddFuel" )]
			[HarmonyPrefix]
			private static bool OnAddFuelPrefix(
				ref Smelter __instance,
				ref ZNetView ___m_nview,
				ref bool __result,
				ref Switch sw,
				ref Humanoid user,
				ref ItemDrop.ItemData item )
			{
				bool? ret = Common.SanityCheckInteraction( ___m_nview , ref __result , ref RateLimitTimeout );
				if( ret != null )
					return ret == true;

				__instance.StartCoroutine(
					Common.DelayedInteraction(
						new WeakReference< Humanoid >( user ),
						new WeakReference< Smelter >( __instance ),
						"OnAddFuel",
						new WeakReference< object >[] {
							new WeakReference< object >( sw ),
							new WeakReference< object >( user ),
							new WeakReference< object >( item ) } ) );

				return false;
			}

			[HarmonyPatch( "OnAddOre" )]
			[HarmonyPrefix]
			private static bool OnAddOrePrefix(
				ref Smelter __instance,
				ref ZNetView ___m_nview,
				ref bool __result,
				ref Switch sw,
				ref Humanoid user,
				ref ItemDrop.ItemData item )
			{
				bool? ret = Common.SanityCheckInteraction( ___m_nview , ref __result , ref RateLimitTimeout );
				if( ret != null )
					return ret == true;

				__instance.StartCoroutine(
					Common.DelayedInteraction(
						new WeakReference< Humanoid >( user ),
						new WeakReference< Smelter >( __instance ),
						"OnAddOre",
						new WeakReference< object >[] {
							new WeakReference< object >( sw ),
							new WeakReference< object >( user ),
							new WeakReference< object >( item ) } ) );

				return false;
			}

			[HarmonyPatch( "RPC_AddFuel" )]
			[HarmonyPrefix]
			private static bool RPC_AddFuelPrefix(
				ref Smelter __instance,
				ref ZNetView ___m_nview,
				ref int ___m_maxFuel,
				ref ItemDrop ___m_fuelItem,
				ref long sender )
			{
				if( !IsEnabled.Value || !___m_nview.IsOwner() )
					return true;

				float fuel = Traverse.Create( __instance )
					.Method( "GetFuel" )
					.GetValue< float >();

				if( fuel > (float)( ___m_maxFuel - 1 ) )
				{
					// ___m_fuelItem.m_itemData.m_dropPrefab is invalid for some reason
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab( ___m_fuelItem.m_itemData.m_shared );
					Vector3 position = __instance.m_outputPoint.position + Vector3.up;
					Instantiate( itemPrefab , position , Quaternion.identity );

					return false;
				}

				return true;
			}

			[HarmonyPatch( "RPC_AddOre" )]
			[HarmonyPrefix]
			private static bool RPC_AddOrePrefix(
				ref Smelter __instance,
				ref ZNetView ___m_nview,
				ref int ___m_maxOre,
				ref long sender,
				ref string name )
			{
				if( !IsEnabled.Value || !___m_nview.IsOwner() )
					return true;

				float queueSize = Traverse.Create( __instance )
					.Method( "GetQueueSize" )
					.GetValue< int >();

				if( queueSize > ___m_maxOre )
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
