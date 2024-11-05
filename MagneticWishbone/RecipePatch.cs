using HarmonyLib;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		[HarmonyPatch( typeof( Recipe ) )]
		private class RecipePatch
		{
			[HarmonyPatch( "GetRequiredStation" )]
			[HarmonyPrefix]
			private static bool GetRequiredStationPrefix( ref CraftingStation __result , ref Recipe __instance , ref int quality )
			{
				if( __instance is CustomRecipe )
				{
					__result = ( (CustomRecipe)__instance ).GetCustomRequiredStation( quality );
					return false;
				}

				return true;
			}

			[HarmonyPatch( "GetRequiredStationLevel" )]
			[HarmonyPrefix]
			private static bool GetRequiredStationLevelPrefix( ref int __result , ref Recipe __instance , ref int quality )
			{
				if( __instance is CustomRecipe )
				{
					__result = ( (CustomRecipe)__instance ).GetCustomRequiredStationLevel( quality );
					return false;
				}

				return true;
			}
		}
	}
}
