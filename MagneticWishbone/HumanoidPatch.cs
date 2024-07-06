using HarmonyLib;
using UnityEngine;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		[HarmonyPatch( typeof( Humanoid ) )]
		private class HumanoidPatch
		{
			internal static float MaxMagneticRange( Player player )
			{
				float range = Common.DefaultMaxAutoPickupDistance;
				if( !IsEnabled.Value )
					return range;

				foreach( ItemDrop.ItemData itemData in player.GetInventory().GetEquippedItems() )
				{
					if( itemData.m_shared.m_name.Equals( Common.WishboneUnlocalizedName ) )
					{
						int functionalQuality = Mathf.Min( itemData.m_quality , MaxAllowedQuality() );

						// Prevent any quality level from being a downgrade
						if( functionalQuality >= 2 )
							range = Mathf.Max( range , Tier2PickupRadius.Value );
						if( functionalQuality >= 3 )
							range = Mathf.Max( range , Tier3PickupRadius.Value );
						if( functionalQuality >= 4 )
							range = Mathf.Max( range , Tier4PickupRadius.Value );
					}
				}

				return range;
			}

			[HarmonyPatch( "EquipItem" )]
			[HarmonyPostfix]
			private static void EquipItemPostfix( ref bool __result , ref Humanoid __instance , ref ItemDrop.ItemData item , ref bool triggerEquipEffects )
			{
				if( __result
					&& item != null
					&& item.m_equipped
					&& item.m_shared.m_name.Equals( Common.WishboneUnlocalizedName ) )
				{
					Player player = Player.m_localPlayer;
					if( player != null && player == __instance )
						player.m_autoPickupRange = MaxMagneticRange( player );
				}
			}

			[HarmonyPatch( "UnequipItem" )]
			[HarmonyPostfix]
			private static void UnequipItemPostfix( ref Humanoid __instance , ref ItemDrop.ItemData item , ref bool triggerEquipEffects )
			{
				if( item != null // FIXME: How is item ever null?!
					&& !item.m_equipped
					&& item.m_shared.m_name.Equals( Common.WishboneUnlocalizedName ) )
				{
					Player player = Player.m_localPlayer;
					if( player != null && player == __instance )
						player.m_autoPickupRange = MaxMagneticRange( player );
				}
			}
		}
	}
}
