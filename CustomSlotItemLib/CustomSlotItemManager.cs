using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomSlotItemLib
{
	public static class CustomSlotManager
	{
		private static readonly Dictionary< Humanoid , Dictionary< string , ItemDrop.ItemData > > customSlotItemData =
			new Dictionary< Humanoid , Dictionary< string , ItemDrop.ItemData > >();

		public static void Register( Humanoid humanoid )
		{
			customSlotItemData[ humanoid ] = new Dictionary< string , ItemDrop.ItemData >();
		}

		public static void Unregister( Humanoid humanoid )
		{
			customSlotItemData.Remove( humanoid );
		}

		public static bool IsCustomSlotItem( ItemDrop.ItemData item )
		{
			return GetCustomSlotName( item ) != null;
		}

		public static string GetCustomSlotName( ItemDrop.ItemData item )
		{
			return item?.m_dropPrefab?.GetComponent< CustomSlotData >()?.slotName;
		}

		private static Dictionary< string , ItemDrop.ItemData > GetCustomSlots( Humanoid humanoid )
		{
			return humanoid != null && customSlotItemData.ContainsKey( humanoid )
				? customSlotItemData[ humanoid ]
				: null;
		}

		public static bool DoesSlotExist( Humanoid humanoid , string slotName )
		{
			return GetCustomSlots( humanoid )?.ContainsKey( slotName ) ?? false;
		}

		public static bool IsSlotOccupied( Humanoid humanoid , string slotName )
		{
			return GetSlotItem( humanoid , slotName ) != null;
		}

		public static ItemDrop.ItemData GetSlotItem( Humanoid humanoid , string slotName )
		{
			var slots = slotName != null ? GetCustomSlots( humanoid ) : null;
			return slots != null && slots.ContainsKey( slotName ) ? slots[ slotName ] : null;
		}

		public static void SetSlotItem( Humanoid humanoid , string slotName , ItemDrop.ItemData item )
		{
			if( humanoid == null || slotName == null )
				return;

			// Should we warn about an already occupied slot when item is not null?
			customSlotItemData[ humanoid ][ slotName ] = item;
		}

		public static IEnumerable< ItemDrop.ItemData > AllSlotItems( Humanoid humanoid )
		{
			if( humanoid == null || !customSlotItemData.ContainsKey( humanoid ) )
				return Enumerable.Empty< ItemDrop.ItemData > ();

			return customSlotItemData[ humanoid ].Values.Where( x => x != null ).ToList();
		}

		public static void ApplyCustomSlotItem( GameObject gameObject , string slotName )
		{
			// It's probably a really bad idea to throw here
			if( gameObject == null )
				throw new ArgumentNullException( "gameObject" );
			else if( slotName == null )
				throw new ArgumentNullException( "slotName" );

			CustomSlotData customSlotData = gameObject.GetComponent< CustomSlotData >();
			if( customSlotData != null )
			{
				if( customSlotData.slotName != slotName )
					throw new InvalidOperationException( $"GameObject \"{gameObject.name}\" already has component CustomSlotData! (\"{customSlotData.slotName}\" != \"{slotName}\")" );
				else
					return;
			}
			else if( gameObject.GetComponent< ItemDrop >() == null )
				throw new InvalidOperationException( $"GameObject \"{gameObject.name}\" does not have component ItemDrop!" );

			//System.Console.WriteLine( $"Applying custom slot \"{slotName}\" to \"{gameObject.name}\"" );
			gameObject.AddComponent< CustomSlotData >().slotName = slotName;
			// This prevents equip conflicts with items of the original type
			gameObject.GetComponent< ItemDrop >().m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.None;
		}
	}
}
