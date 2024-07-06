using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		internal class MagneticWishboneRecipe : CustomRecipe
		{
			public static InvalidatableLazy< MagneticWishboneRecipe > Instance = new InvalidatableLazy< MagneticWishboneRecipe >( () =>
			{
				return (MagneticWishboneRecipe)ScriptableObject.CreateInstance( typeof( MagneticWishboneRecipe ) );
			} );

			protected MagneticWishboneRecipe()
			{
				m_item = ObjectDB.instance.GetItemPrefab( Common.WishbonePrefabName ).GetComponent< ItemDrop >();
				m_amount = 1;
				m_enabled = true;
				m_qualityResultAmountMultiplier = 1f;
				m_craftingStation = Common.NoneCraftingStation.Value;
				m_repairStation = Common.NoneCraftingStation.Value;
				m_minStationLevel = 1;
				m_requireOnlyOneIngredient = false;

				try
				{
					List< Piece.Requirement > reqs = new List< Piece.Requirement >();
					if( AllowTier4.Value || AllowTier3.Value || AllowTier2.Value )
					{
						var levelReqs = CreateRequirements< CustomRequirement >( Tier2Ingredients.Value );
						levelReqs.ForEach( x => x.m_applicableLevel = 2 );
						reqs.AddRange( levelReqs );
					}
					if( AllowTier4.Value || AllowTier3.Value )
					{
						var levelReqs = CreateRequirements< CustomRequirement >( Tier3Ingredients.Value );
						levelReqs.ForEach( x => x.m_applicableLevel = 3 );
						reqs.AddRange( levelReqs );
					}
					if( AllowTier4.Value )
					{
						var levelReqs = CreateRequirements< CustomRequirement >( Tier4Ingredients.Value );
						levelReqs.ForEach( x => x.m_applicableLevel = 4 );
						reqs.AddRange( levelReqs );
					}
					m_resources = reqs.ToArray();
				}
				catch( Exception e )
				{
					m_enabled = false;
					System.Console.WriteLine( e );
				}

				RegisterWithVNEI();
			}

			// CustomRecipe overrides

			public override bool IsCraftable()
			{
				return false;
			}

			public override bool IsCraftableAt( CraftingStation station )
			{
				return false;
			}

			public override bool IsUpgradable( int currentQuality )
			{
				return currentQuality < MaxAllowedQuality();
			}

			public override bool IsUpgradableAt( int currentQuality , CraftingStation station )
			{
				CraftingStation requiredStation = GetCustomRequiredStation( currentQuality + 1 );

				if( station?.m_name == null && requiredStation?.m_name == null )
					return true;
				else if( station?.m_name == null || requiredStation?.m_name == null )
					return false;
				else if( !requiredStation.m_name.Equals( station.m_name ) )
					return false;
				else if( GetCustomRequiredStationLevel( currentQuality + 1 ) > station.GetLevel() )
					return false;

				return true;
			}

			// Recipe pseudo-overrides

			public override CraftingStation GetCustomRequiredStation( int quality )
			{
				switch( quality )
				{
					case 2:
					{
						return AllowTier2.Value
							? Common.FindCraftingStationByUnlocalizedName( Tier2CraftingStation.Value.Split( ',' )[ 0 ] )
							: Common.NoneCraftingStation.Value;
					}
					case 3:
					{
						return AllowTier3.Value
							? Common.FindCraftingStationByUnlocalizedName( Tier3CraftingStation.Value.Split( ',' )[ 0 ] )
							: Common.NoneCraftingStation.Value;
					}
					case 4:
					{
						return AllowTier4.Value
							? Common.FindCraftingStationByUnlocalizedName( Tier4CraftingStation.Value.Split( ',' )[ 0 ] )
							: Common.NoneCraftingStation.Value;
					}
				}

				return Common.NoneCraftingStation.Value;
			}

			public override int GetCustomRequiredStationLevel( int quality )
			{
				try
				{
					switch( quality )
					{
						case 2:
							return AllowTier2.Value ? int.Parse( Tier2CraftingStation.Value.Split( ',' )[ 1 ] ) : 999;
						case 3:
							return AllowTier3.Value ? int.Parse( Tier3CraftingStation.Value.Split( ',' )[ 1 ] ) : 999;
						case 4:
							return AllowTier4.Value ? int.Parse( Tier4CraftingStation.Value.Split( ',' )[ 1 ] ) : 999;
					}
				}
				catch( Exception )
				{
					// The above block will throw on bad formatting. Act like the recipe doesn't apply.
				}

				return 999;
			}
		}
	}
}
