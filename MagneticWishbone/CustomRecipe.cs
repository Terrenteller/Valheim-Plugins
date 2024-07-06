using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;

namespace MagneticWishbone
{
	// Vanilla has a very crude and very formulaic crafting system. Our recipe breaks the formula in several ways.
	// We could elaborate and split this out into a separate library, but we only have one hyper-specific use case.
	// The actual devs should be responsible for a more robust crafting system. Our effort could be obsoleted at any time.
	public abstract class CustomRecipe : Recipe
	{
		public virtual bool IsCraftable()
		{
			// Basic implementation. Derived classes should be more explicit.
			return IsCraftableAt( GetCustomRequiredStation( 1 ) );
		}

		public abstract bool IsCraftableAt( CraftingStation station );

		public virtual bool IsUpgradable( int currentQuality )
		{
			// Basic implementation. Derived classes should be more explicit.
			return IsUpgradableAt( currentQuality , GetCustomRequiredStation( currentQuality ) );
		}

		public abstract bool IsUpgradableAt( int currentQuality , CraftingStation station );

		public virtual bool AppliesTo( ItemDrop itemDrop )
		{
			return AppliesTo( itemDrop.m_itemData );
		}

		public virtual bool AppliesTo( ItemDrop.ItemData itemData )
		{
			return AppliesTo( itemData.m_shared.m_name );
		}

		public virtual bool AppliesTo( string unlocalizedItemNameOrPrefabName )
		{
			return m_item.m_itemData.m_shared.m_name.Equals( unlocalizedItemNameOrPrefabName )
				|| m_item.name.Equals( unlocalizedItemNameOrPrefabName );
		}

		public virtual bool RegisterWithVNEI()
		{
			if( Chainloader.PluginInfos.ContainsKey( "com.maxsch.valheim.vnei" ) )
			{
				RegisterWithVNEICore();
				return true;
			}

			return false;
		}

		protected virtual void RegisterWithVNEICore()
		{
			for( int quality = 1 ; quality <= m_item.m_itemData.m_shared.m_maxQuality ; quality++ )
				if( quality == 1 ? IsCraftable() : IsUpgradable( quality - 1 ) )
					VNEI.Logic.Indexing.AddRecipeToItems( (VNEI.Logic.RecipeInfo)RegisterWithVNEICore( quality ) );
		}

		// Object, so the vtable can be constructed when VNEI is not present
		protected virtual Object RegisterWithVNEICore( int quality )
		{
			// Props to the VNEI devs for having the foresight to make a distinction between quality and everything else
			VNEI.Logic.RecipeInfo recipeInfo = new VNEI.Logic.RecipeInfo( this , quality );
			recipeInfo.AddStation( GetCustomRequiredStation( quality ).m_name , GetCustomRequiredStationLevel( quality ) );
			return recipeInfo;
		}

		// Recipe pseudo-overrides

		public virtual int GetCustomAmount( int quality , out int need , out ItemDrop.ItemData singleReqItem )
		{
			return GetAmount( quality , out need , out singleReqItem );
		}

		public virtual CraftingStation GetCustomRequiredStation( int quality )
		{
			return GetRequiredStation( quality );
		}

		public virtual int GetCustomRequiredStationLevel( int quality )
		{
			return GetRequiredStationLevel( quality );
		}

		// Statics

		public static Tuple< string , int > ParseItemCountPair( string rawPair )
		{
			if( rawPair == null )
				throw new ArgumentNullException( "rawPair" );

			int count = 0;
			string[] keyValue = rawPair.Split( ',' );
			if( keyValue.Length < 2 )
				throw new ArgumentException( "Item count does not name an item!" );
			else if( keyValue.Length > 2 )
				throw new ArgumentException( "Item count lists more than an item and a count!" );
			else if( keyValue[ 0 ].IsNullOrWhiteSpace() )
				throw new ArgumentException( "Item name is null or whitespace!" );
			else if( keyValue[ 1 ].IsNullOrWhiteSpace() )
				throw new ArgumentException( "Count is null or whitespace!" );
			else if( !int.TryParse( keyValue[ 1 ] , out count ) )
				throw new NullReferenceException( $"Cannot parse \"{keyValue[ 1 ]}\" as an integer!" );

			return new Tuple< string , int >( keyValue[ 0 ] , count );
		}

		public static Dictionary< string , int > ParseRequirementString( string requirementsString )
		{
			if( requirementsString == null )
				return null;

			Dictionary< string , int > requirements = new Dictionary< string , int >();
			if( requirementsString.IsNullOrWhiteSpace() ) // Can only be empty or whitespace at this point
				return requirements;

			try
			{
				foreach( string pair in requirementsString.Split( ';' ) )
				{
					// TODO: Add warnings for zero and duplicate items?
					Tuple< string , int > itemCount = ParseItemCountPair( pair );
					if( itemCount.Item2 <= 0 )
						continue;

					if( requirements.ContainsKey( itemCount.Item1 ) )
						requirements[ itemCount.Item1 ] += itemCount.Item2;
					else
						requirements.Add( itemCount.Item1 , itemCount.Item2 );
				}
			}
			catch( Exception e )
			{
				System.Console.WriteLine( e );
				return null;
			}

			return requirements;
		}

		public static List< T > CreateRequirements< T >( string requirementsString ) where T : CustomRequirement , new()
		{
			Dictionary< string , int > requirementsDict = ParseRequirementString( requirementsString );
			if( requirementsDict == null )
				return null;

			List< T > requirements = new List< T >();
			foreach( string prefabName in requirementsDict.Keys )
			{
				requirements.Add( new T
				{
					m_resItem = ObjectDB.instance.GetItemPrefab( prefabName ).GetComponent< ItemDrop >(),
					m_amount = requirementsDict[ prefabName ],
					m_extraAmountOnlyOneIngredient = 0,
					m_amountPerLevel = 0,
					m_recover = true
				} );
			}

			return requirements;
		}
	}
}
