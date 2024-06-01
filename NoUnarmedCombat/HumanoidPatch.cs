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

			private static ItemDrop.ItemData[] GetToolbarWeaponsAndShield( Humanoid humanoid )
			{
				// Mods which add additional slots may do so as distinct inventories.
				// Items in them may have the same X/Y coordinates as items in the vanilla inventory.
				// Therefore, we cannot call GetAllItems() because we cannot distinguish between inventories.
				List< ItemDrop.ItemData > toolbarItems = new List< ItemDrop.ItemData >();
				humanoid.GetInventory().GetBoundItems( toolbarItems );
				List< ItemDrop.ItemData > validToolbarItems = toolbarItems
					.OrderBy( x => x.m_gridPos.x )
					.Where( x => x.GetDurabilityPercentage() > 0.0f )
					.ToList();

				ItemDrop.ItemData[] weaponsAndShield = new ItemDrop.ItemData[ 3 ];
				weaponsAndShield[ 0 ] = validToolbarItems.FirstOrDefault(
					itemData => itemData.IsWeapon() && itemData.IsTwoHanded() );
				weaponsAndShield[ 1 ] = validToolbarItems.FirstOrDefault(
					itemData => itemData.IsWeapon() && !itemData.IsTwoHanded() );
				weaponsAndShield[ 2 ] = validToolbarItems.FirstOrDefault(
					itemData => ItemIsShield( itemData ) );

				return weaponsAndShield;
			}

			private static bool HumanoidIsLocalPlayer( Humanoid humanoid )
			{
				return humanoid.IsPlayer() && humanoid.GetZDOID().UserID == ZDOMan.GetSessionID();
			}

			private static bool ItemIsShield( ItemDrop.ItemData itemData )
			{
				return itemData?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
			}
			
			private static void ShowHandItems( ref Humanoid __instance )
			{
				// Protected for some reason when it wasn't before
				Traverse.Create( __instance )
					.Method( "ShowHandItems" )
					.GetValue();
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
						ItemDrop.ItemData[] weaponAndShield = GetToolbarWeaponsAndShield( __instance );
						if( weaponAndShield[ 0 ] != null || weaponAndShield[ 1 ] != null )
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
			private static bool StartAttackPrefix( ref Humanoid __instance , ref bool secondaryAttack )
			{
				if( !IsEnabled.Value )
					return true;

				// Rarely, the player may unsheathe or attack when panning around the minimap.
				// This may be related to something which causes the minimap to close when an enemy
				// spawns nearby or gets too close to the player. More testing is needed.
				if( HumanoidIsLocalPlayer( __instance ) && Minimap.IsOpen() )
					return false;

				InStartAttack = true;
				PerformingSecondaryAttack = secondaryAttack;

				return true;
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

				ItemDrop.ItemData activeMainHandItem = __instance.RightItem;
				ItemDrop.ItemData activeOffHandItem = __instance.LeftItem;
				if( activeMainHandItem != null || activeOffHandItem != null )
					return;

				// BEWARE: Not all equipment correctly reports two-handed-ness, like hammers.
				// Equipping some items may unexpectedly unequip others.

				ItemDrop.ItemData chosenMainHandItem = null;
				ItemDrop.ItemData chosenOffHandItem = null;

				if( UnsheatheOnPunch.Value && ( ___m_hiddenRightItem != null || ___m_hiddenLeftItem != null ) )
				{
					if( !ToolbarEquipOnPunch.Value
						|| ( ___m_hiddenLeftItem != null && ___m_hiddenRightItem != null ) )
					{
						ShowHandItems( ref __instance );
						return;
					}

					// Not technically active, but unsheathing has priority
					activeMainHandItem = ___m_hiddenRightItem;
					activeOffHandItem = ___m_hiddenLeftItem;
					chosenMainHandItem = ___m_hiddenRightItem;
					chosenOffHandItem = ___m_hiddenLeftItem;
				}
				
				if( ToolbarEquipOnPunch.Value && ( chosenMainHandItem == null || chosenOffHandItem == null ) )
				{
					ItemDrop.ItemData[] weaponsAndShield = GetToolbarWeaponsAndShield( __instance );
					ItemDrop.ItemData toolbarWeaponTwoHanded = weaponsAndShield[ 0 ];
					ItemDrop.ItemData toolbarWeaponOneHanded = weaponsAndShield[ 1 ];
					ItemDrop.ItemData toolbarShield = weaponsAndShield[ 2 ];

					if( chosenMainHandItem == null && chosenOffHandItem == null )
					{
						if( toolbarWeaponOneHanded != null && toolbarWeaponTwoHanded != null )
						{
							if( toolbarWeaponOneHanded.m_gridPos.x < toolbarWeaponTwoHanded.m_gridPos.x )
							{
								chosenMainHandItem = toolbarWeaponOneHanded;
								chosenOffHandItem = toolbarShield;
							}
							else
								chosenMainHandItem = toolbarWeaponTwoHanded;
						}
						else if( toolbarWeaponTwoHanded != null )
						{
							chosenMainHandItem = toolbarWeaponTwoHanded;
						}
						else
						{
							chosenMainHandItem = toolbarWeaponOneHanded;
							chosenOffHandItem = toolbarShield;
						}
					}
					else if( chosenMainHandItem != null && chosenOffHandItem == null )
					{
						if( chosenMainHandItem.IsTwoHanded() )
						{
							// No-op
						}
						else
						{
							chosenOffHandItem = toolbarShield;
						}
					}
					else if( chosenMainHandItem == null && chosenOffHandItem != null )
					{
						if( chosenOffHandItem.IsTwoHanded() )
						{
							// No-op
						}
						else if( chosenOffHandItem.IsWeapon() )
						{
							// No-op
						}
						else if( ItemIsShield( chosenOffHandItem ) )
						{
							if( toolbarWeaponOneHanded != null )
								chosenMainHandItem = toolbarWeaponOneHanded;
						}
					}
				}
				else
					return;

				// Yet another balancing act. The active items are assumed to be compatible.

				if( activeMainHandItem != null )
				{
					__instance.EquipItem( activeMainHandItem );
				}
				else if( chosenMainHandItem != null && !__instance.IsItemEquiped( chosenMainHandItem ) )
				{
					__instance.EquipItem( chosenMainHandItem );
					activeMainHandItem = chosenMainHandItem;
				}

				if( activeOffHandItem != null )
				{
					__instance.EquipItem( activeOffHandItem );
				}
				else if( chosenOffHandItem != null && !__instance.IsItemEquiped( chosenOffHandItem ) )
				{
					__instance.EquipItem( chosenOffHandItem );

					if( activeMainHandItem != null && !__instance.IsItemEquiped( activeMainHandItem ) )
						__instance.EquipItem( activeMainHandItem );
					else
						activeOffHandItem = chosenOffHandItem;
				}

				if( activeMainHandItem != null || activeOffHandItem != null )
					___m_zanim.SetTrigger( "equip_hip" );

				// FIXME: It's possible that us equipping stuff in the postfix is the reason
				// why players may attack after unsheathing/equipping equipment.
				// We should probably do this in the prefix.
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

				if( __instance.IsSwimming() && !__instance.IsOnGround() )
				{
					SheathedBecauseSwimming |= ___m_leftItem != null || ___m_rightItem != null;
				}
				else if( SheathedBecauseSwimming )
				{
					if( IsEnabled.Value && UnsheatheAfterSwimming.Value )
						ShowHandItems( ref __instance );

					SheathedBecauseSwimming = false;
				}
			}
		}
	}
}
