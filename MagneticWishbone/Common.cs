using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MagneticWishbone
{
	public class Common
	{
		// Vanilla defaults as of writing
		public const string WishbonePrefabName = "Wishbone";
		public const string WishboneUnlocalizedName = "$item_wishbone";
		public const string NoneCraftingStationName = "NoneCraftingStation";
		public const float DefaultMaxAutoPickupDistance = 2.0f;
		public const float DefaultMaxInteractDistance = 5.0f;
		public const float MaxAutoPickupDistance = 50.0f; 

		public static InvalidatableLazy< CraftingStation > NoneCraftingStation = new InvalidatableLazy< CraftingStation >( () =>
		{
			CraftingStation craftingStation = ( new GameObject() ).AddComponent< CraftingStation >();
			craftingStation.name = NoneCraftingStationName;
			craftingStation.m_name = NoneCraftingStationName;
			craftingStation.m_discoverRange = 0.0f;
			return craftingStation;
		} );

		// Such as:
		// $piece_artisanstation
		// $piece_blackforge
		// $piece_cauldron
		// $piece_forge
		// $piece_magetable
		// $piece_stonecutter
		// $piece_workbench
		public static CraftingStation FindCraftingStationByUnlocalizedName( string unlocalizedName )
		{
			if( !unlocalizedName.IsNullOrWhiteSpace() )
			{
				List< CraftingStation > craftingStations = Traverse.Create( typeof( CraftingStation ) )
					.Field( "m_allStations" )
					.GetValue() as List< CraftingStation >;

				foreach( CraftingStation station in craftingStations )
					if( station.m_name.Equals( unlocalizedName ) )
						return station;
			}

			// null is not reliably invalid given Unity's insane operator== hijack.
			// You MUST compare by unlocalized names!
			return NoneCraftingStation.Value;
		}
	}
}
