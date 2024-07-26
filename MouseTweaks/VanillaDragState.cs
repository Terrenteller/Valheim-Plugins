﻿using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MouseTweaks
{
	public class VanillaDragState
	{
		public readonly GameObject dragObject;
		public readonly Inventory dragInventory;
		public readonly ItemDrop.ItemData dragItem;
		public readonly int dragAmount;
		public readonly bool isValid;

		public VanillaDragState()
		{
			InventoryGui inventoryGui = InventoryGui.instance;
			dragObject = Traverse.Create( inventoryGui )
				.Field( "m_dragGo" )
				.GetValue< GameObject >();

			if( dragObject == null )
			{
				dragInventory = null;
				dragItem = null;
				dragAmount = 0;

				return;
			}

			dragInventory = Traverse.Create( inventoryGui )
				.Field( "m_dragInventory" )
				.GetValue< Inventory >();
			dragItem = Traverse.Create( inventoryGui )
				.Field( "m_dragItem" )
				.GetValue< ItemDrop.ItemData >();
			dragAmount = Traverse.Create( inventoryGui )
				.Field( "m_dragAmount" )
				.GetValue< int >();

			isValid = dragObject != null
				&& dragInventory != null
				&& dragItem != null
				&& dragAmount > 0
				&& dragInventory.ContainsItem( dragItem );
		}
		
		protected VanillaDragState(
			GameObject dragObject,
			Inventory dragInventory,
			ItemDrop.ItemData dragItem,
			int dragAmount,
			bool isValid )
		{
			this.dragObject = dragObject;
			this.dragInventory = dragInventory;
			this.dragItem = dragItem;
			this.dragAmount = dragAmount;
			this.isValid = isValid;
		}

		public VanillaDragState Clone()
		{
			return new VanillaDragState( dragObject , dragInventory , dragItem , dragAmount , isValid );
		}

		public bool Decrement()
		{
			System.Console.WriteLine( $"TEST: VanillaDragState.Decrement()" );
			if( !isValid )
				return false;

			Traverse.Create( InventoryGui.instance )
				.Field( "m_dragAmount" )
				.SetValue( dragAmount - 1 );

			return true;
		}

		public void UpdateTooltip()
		{
			System.Console.WriteLine( $"TEST: VanillaDragState.UpdateTooltip()" );
			if( !isValid )
				return;

			// Taken from InventoryGui.UpdateItemDrag()
			dragObject.transform
				.Find( "icon" )
				.GetComponent< Image >()
				.sprite = dragItem.GetIcon();
			dragObject.transform
				.Find( "name" )
				.GetComponent< TMP_Text >()
				.text = dragItem.m_shared.m_name;
			dragObject.transform
				.Find( "amount" )
				.GetComponent< TMP_Text >()
				.text = dragAmount > 1 ? dragAmount.ToString() : "";
		}

		// Statics

		public static void ClearDrag()
		{
			System.Console.WriteLine( $"TEST: VanillaDragState.ClearDrag()" );
			MouseTweaks.InventoryGuiPatch.SetupDragItem( InventoryGui.instance , null , null , 1 );
		}
		
		public static void ClearDragIfInvalid( VanillaDragState dragState = null )
		{
			System.Console.WriteLine( $"TEST: VanillaDragState.ClearDragIfInvalid()" );
			if( !( dragState ?? new VanillaDragState() ).isValid )
				ClearDrag();
		}
		
		public static bool IsValid()
		{
			return ( new VanillaDragState() ).isValid;
		}
	}
}