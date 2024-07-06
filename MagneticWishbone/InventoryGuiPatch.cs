using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		[HarmonyPatch( typeof( InventoryGui ) )]
		private class InventoryGuiPatch
		{
			[HarmonyPatch( "AddRecipeToList" )]
			[HarmonyPrefix]
			private static bool AddRecipeToListPrefix(
				ref InventoryGui __instance,
				ref Player player,
				ref Recipe recipe,
				ref ItemDrop.ItemData item,
				ref bool canCraft )
			{
				// A recipe produces as many entries in the crafting GUI as there are applicable items.
				// Filter out entries that don't apply based on the crafting station.
				if( IsEnabled.Value && !canCraft && recipe is CustomRecipe )
				{
					CustomRecipe customRecipe = (CustomRecipe)recipe;
					CraftingStation currentStation = player.GetCurrentCraftingStation();

					if( ( __instance.InCraftTab() && !customRecipe.IsCraftableAt( currentStation ) )
						|| ( !__instance.InCraftTab() && !customRecipe.IsUpgradableAt( item.m_quality , currentStation ) ) )
					{
						return false;
					}
				}

				return true;
			}

			[HarmonyPatch( "UpdateRecipeList" )]
			[HarmonyPrefix]
			private static void UpdateRecipeListPrefix( ref InventoryGui __instance , ref List< Recipe > recipes )
			{
				Player player = Player.m_localPlayer;
				if( !IsEnabled.Value || player == null )
					return;

				// This is placeholder logic for iterating over a larger collection of custom recipes
				IEnumerable< CustomRecipe > customRecipes = new[] { MagneticWishboneRecipe.Instance.Value };
				List< ItemDrop.ItemData > inventory = player.GetInventory().GetAllItems().Where( x => x != null ).ToList();
				CraftingStation currentStation = player.GetCurrentCraftingStation();

				foreach( CustomRecipe customRecipe in customRecipes.Where( x => x.m_enabled ) )
				{
					if( __instance.InCraftTab() )
					{
						if( customRecipe.IsCraftableAt( currentStation ) )
							recipes.Add( customRecipe );

						continue;
					}

					foreach( ItemDrop.ItemData item in inventory )
					{
						if( customRecipe.AppliesTo( item ) && customRecipe.IsUpgradableAt( item.m_quality , currentStation ) )
						{
							recipes.Add( customRecipe );
							break;
						}
					}
				}
			}
		}
	}
}
