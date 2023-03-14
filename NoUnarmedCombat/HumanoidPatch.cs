using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace NoUnarmedCombat
{
	public partial class NoUnarmedCombat
	{
		[HarmonyPatch( typeof( Humanoid ) )]
		private class HumanoidPatch
		{
			private static bool PerformingSecondaryAttack = false;
			private static bool SheathedBecauseSwimming = false;

			private static bool HumanoidIsLocalPlayer( Humanoid instance )
			{
				return instance.IsPlayer() && instance.GetZDOID().userID == ZDOMan.instance.GetMyID();
			}

			[HarmonyPatch( "GetCurrentBlocker" )]
			[HarmonyPostfix]
			private static void GetCurrentBlockerPostfix( ref Humanoid __instance , ref ItemDrop.ItemData __result )
			{
				if( !IsEnabled.Value || !HumanoidIsLocalPlayer( __instance ) )
					return;

				// Just because we can't punch shouldn't mean we can't block
				__result = __result ?? __instance.m_unarmedWeapon?.m_itemData;
			}

			[HarmonyPatch( "GetCurrentWeapon" )]
			[HarmonyPostfix]
			private static void GetCurrentWeaponPostfix( ref Humanoid __instance , ref ItemDrop.ItemData __result )
			{
				if( !IsEnabled.Value || !HumanoidIsLocalPlayer( __instance ) )
					return;

				// If we allow kicking, we allow all secondary attacks.
				// We don't need to check if the secondary is actually a kick.
				if( PerformingSecondaryAttack && AllowKick.Value )
					return;

				if( __result == __instance.m_unarmedWeapon?.m_itemData )
					__result = null;
			}

			[HarmonyPatch( "StartAttack" )]
			[HarmonyPrefix]
			private static void StartAttackPrefix( ref Humanoid __instance , ref bool secondaryAttack )
			{
				if( !IsEnabled.Value || !HumanoidIsLocalPlayer( __instance ) )
					return;

				PerformingSecondaryAttack = secondaryAttack;
			}

			[HarmonyPatch( "StartAttack" )]
			[HarmonyPostfix]
			private static void StartAttackPostfix(
				ref Humanoid __instance,
				ref ItemDrop.ItemData ___m_hiddenLeftItem,
				ref ItemDrop.ItemData ___m_hiddenRightItem,
				ref ZSyncAnimation ___m_zanim )
			{
				if( !IsEnabled.Value || !HumanoidIsLocalPlayer( __instance ) )
					return;

				if( PerformingSecondaryAttack || __instance.GetCurrentWeapon() != null )
				{
					PerformingSecondaryAttack = false;
					return;
				}

				if( UnsheatheOnPunch.Value && ( ___m_hiddenLeftItem != null || ___m_hiddenRightItem != null ) )
				{
					__instance.ShowHandItems();
				}
				else if( ToolbarEquipOnPunch.Value )
				{
					// Mods which add additional slots may do so as distinct inventories.
					// Items in them may have the same X/Y coordinates as items in the vanilla inventory.
					// Therefore, we cannot call GetAllItems() because we cannot distinguish between inventories.
					List< ItemDrop.ItemData > toolbarItems = new List< ItemDrop.ItemData >();
					__instance.GetInventory().GetBoundItems( toolbarItems );
					IEnumerable< ItemDrop.ItemData > validToolbarItems = toolbarItems
						.OrderBy( x => x.m_gridPos.x )
						.Where( x => x.GetDurabilityPercentage() > 0.0f );

					ItemDrop.ItemData firstWeapon = validToolbarItems.FirstOrDefault( itemData => itemData.IsWeapon() );
					if( firstWeapon != null )
						__instance.EquipItem( firstWeapon );

					ItemDrop.ItemData firstShield = null;
					if( firstWeapon == null || !firstWeapon.IsTwoHanded() )
					{
						firstShield = validToolbarItems.FirstOrDefault( itemData => itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield );
						if( firstShield != null )
							__instance.EquipItem( firstShield );
					}

					if( firstWeapon != null || firstShield != null )
						___m_zanim.SetTrigger( "equip_hip" );
				}

				PlayerPatch.ClearQueuedAttackTimers = true;
			}

			[HarmonyPatch( "UpdateEquipment" )]
			[HarmonyPrefix]
			private static void UpdateEquipmentPrefix(
				ref Humanoid __instance,
				ref ItemDrop.ItemData ___m_leftItem,
				ref ItemDrop.ItemData ___m_rightItem )
			{
				if( !HumanoidIsLocalPlayer( __instance ) )
					return;

				if( __instance.IsSwiming() && !__instance.IsOnGround() )
				{
					SheathedBecauseSwimming |= ___m_leftItem != null || ___m_rightItem != null;
				}
				else if( SheathedBecauseSwimming )
				{
					if( IsEnabled.Value && UnsheatheAfterSwimming.Value )
						__instance.ShowHandItems();

					SheathedBecauseSwimming = false;
				}
			}
		}
	}
}
