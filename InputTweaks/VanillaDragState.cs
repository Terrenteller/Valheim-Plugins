using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InputTweaks
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

			// TODO: Multiple states may be new'd up every frame.
			// Expose these more directly. Consider making a mutable version of this class.
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

		public bool Increment( int count = 1 )
		{
			Common.DebugMessage( $"INFO: VanillaDragState.Increment()" );
			if( !isValid )
				return false;

			Traverse.Create( InventoryGui.instance )
				.Field( "m_dragAmount" )
				.SetValue( dragAmount + count );

			return true;
		}
		
		public bool Decrement( int count = 1 )
		{
			Common.DebugMessage( $"INFO: VanillaDragState.Decrement()" );
			if( !isValid )
				return false;

			Traverse.Create( InventoryGui.instance )
				.Field( "m_dragAmount" )
				.SetValue( dragAmount - count );

			return true;
		}

		public void UpdateTooltip()
		{
			Common.DebugMessage( $"INFO: VanillaDragState.UpdateTooltip()" );
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
			// Even for a debug message this gets printed a lot
			//Common.DebugMessage( $"DRAG: VanillaDragState.ClearDrag()" );
			InputTweaks.InventoryGuiPatch.SetupDragItem( InventoryGui.instance , null , null , 1 );
		}

		public static bool IsValid()
		{
			return ( new VanillaDragState() ).isValid;
		}
	}
}
