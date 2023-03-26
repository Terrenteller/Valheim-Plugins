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
			private static bool InStartAttack = false;
			private static bool PerformingSecondaryAttack = false;
			private static bool SheathedBecauseSwimming = false;
			private static bool SkipNextEquipAttempt = false;

			private static ItemDrop.ItemData[] GetToolbarWeaponAndShield( Humanoid humanoid )
			{
				// Mods which add additional slots may do so as distinct inventories.
				// Items in them may have the same X/Y coordinates as items in the vanilla inventory.
				// Therefore, we cannot call GetAllItems() because we cannot distinguish between inventories.
				List< ItemDrop.ItemData > toolbarItems = new List< ItemDrop.ItemData>();
				humanoid.GetInventory().GetBoundItems( toolbarItems );
				IEnumerable< ItemDrop.ItemData > validToolbarItems = toolbarItems
					.OrderBy( x => x.m_gridPos.x )
					.Where( x => x.GetDurabilityPercentage() > 0.0f );

				ItemDrop.ItemData weapon = validToolbarItems.FirstOrDefault( itemData => itemData.IsWeapon() );
				ItemDrop.ItemData[] weaponAndShield = new ItemDrop.ItemData[ 2 ];
				weaponAndShield[ 0 ] = weapon;
				weaponAndShield[ 1 ] = weapon == null || !weapon.IsTwoHanded()
					? validToolbarItems.FirstOrDefault( itemData => itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield )
					: null;

				return weaponAndShield;
			}

			private static bool HumanoidIsLocalPlayer( Humanoid humanoid )
			{
				return humanoid.IsPlayer() && humanoid.GetZDOID().userID == ZDOMan.instance.GetMyID();
			}

			private static bool ItemIsWeaponOrShield( ItemDrop.ItemData itemData )
			{
				return itemData != null
					&& ( itemData.IsWeapon() || itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield );
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
			private static ItemDrop.ItemData GetCurrentWeaponPostfix(
				ItemDrop.ItemData result,
				ref Humanoid __instance,
				ref ItemDrop.ItemData ___m_hiddenLeftItem,
				ref ItemDrop.ItemData ___m_hiddenRightItem )
			{
				if( InStartAttack
					&& IsEnabled.Value
					&& result == __instance.m_unarmedWeapon?.m_itemData
					&& HumanoidIsLocalPlayer( __instance )
					&& ( !PerformingSecondaryAttack || !AllowKick.Value ) )
				{
					if( UnsheatheOnPunch.Value && ( ___m_hiddenLeftItem != null || ___m_hiddenRightItem != null ) )
						return null;

					if( ToolbarEquipOnPunch.Value )
					{
						ItemDrop.ItemData[] weaponAndShield = GetToolbarWeaponAndShield( __instance );
						if( weaponAndShield[ 0 ] != null )
							return null;
					}

					if( !FallbackFisticuffs.Value )
					{
						SkipNextEquipAttempt = true;
						return null;
					}
				}

				return result;
			}

			[HarmonyPatch( "StartAttack" )]
			[HarmonyPrefix]
			private static void StartAttackPrefix( ref Humanoid __instance , ref bool secondaryAttack )
			{
				if( !IsEnabled.Value || !HumanoidIsLocalPlayer( __instance ) )
					return;

				InStartAttack = true;
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
				bool skipEquip = !IsEnabled.Value
					|| !HumanoidIsLocalPlayer( __instance )
					|| __instance.GetCurrentWeapon() != null
					|| !InStartAttack
					|| SkipNextEquipAttempt;

				InStartAttack = false;
				PerformingSecondaryAttack = false;
				SkipNextEquipAttempt = false;

				if( skipEquip )
					return;

				// We can't reliably equip from the toolbar while unsheathing in the same pass
				// because not all equipment correctly reports two-handed-ness, like hammers.
				// The double-edged sword here, logically, is this method gets called A LOT.
				// If the first pass unsheathed equipment, the second pass will respect what
				// the player is holding. A shield only may cause us to look for a weapon
				// on the toolbar while a hammer will count as a weapon so we will never get here.
				if( UnsheatheOnPunch.Value && ( ___m_hiddenLeftItem != null || ___m_hiddenRightItem != null ) )
				{
					__instance.ShowHandItems();
				}
				else if( ToolbarEquipOnPunch.Value )
				{
					ItemDrop.ItemData[] weaponAndShield = GetToolbarWeaponAndShield( __instance );
					ItemDrop.ItemData weapon = weaponAndShield[ 0 ];
					ItemDrop.ItemData shield = weaponAndShield[ 1 ];
					ItemDrop.ItemData rightItem = __instance.GetRightItem();
					ItemDrop.ItemData leftItem = __instance.GetLeftItem();
					bool equipmentChanged = false;

					if( rightItem != null && rightItem.IsWeapon() )
					{
						weapon = __instance.GetRightItem();
					}
					else if( leftItem != null && leftItem.IsWeapon() )
					{
						weapon = __instance.GetLeftItem();
					}
					else if( weapon != null )
					{
						__instance.EquipItem( weapon );
						equipmentChanged = true;
					}

					if( ( weapon == null || !weapon.IsTwoHanded() )
						&& shield != null
						&& !ItemIsWeaponOrShield( __instance.GetLeftItem() ) )
					{
						__instance.EquipItem( shield );
						equipmentChanged = true;
					}

					if( equipmentChanged )
						___m_zanim.SetTrigger( "equip_hip" );
				}
				else
					return;

				PlayerPatch.RestoreQueuedAttackTimers = true;
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
