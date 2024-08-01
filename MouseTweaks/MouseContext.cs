using System.Collections.Generic;
using UnityEngine;

namespace MouseTweaks
{
	public abstract class IInventoryGuiMouseContext
	{
		public enum State
		{
			OK,
			NoChange,
			Done,
			Invalid
		}

		public virtual State CurrentState { get; protected set; } = State.OK;

		protected virtual State SetState( State state )
		{
			//System.Console.WriteLine( $"CNTX: Setting state to {state}" );
			CurrentState = state;
			return state;
		}

		public abstract State Think( VanillaDragState dragState );
		public abstract void End(); // TODO: Soft VS hard termination bool
	}

	public class BlockingLeftMouseContext : IInventoryGuiMouseContext
	{
		public BlockingLeftMouseContext()
		{
			// TODO: Track modifiers?
		}

		// IMouseContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending BlockingLeftMouseContext" );
		}

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.LeftOnly )
				return SetState( State.NoChange );
			else if( FrameInputs.Current.Left )
				return SetState( State.Invalid );
			else
				return SetState( State.Done );
		}
	}
	
	public class BlockingRightMouseContext : IInventoryGuiMouseContext
	{
		public BlockingRightMouseContext()
		{
			// TODO: Track modifiers?
		}

		// IMouseContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending BlockingRightMouseContext" );
		}

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.RightOnly )
				return SetState( State.NoChange );
			else if( FrameInputs.Current.Right )
				return SetState( State.Invalid );
			else
				return SetState( State.Done );
		}
	}

	public abstract class InventoryButtonTrackingContext : IInventoryGuiMouseContext
	{
		protected List< InventoryButton > playerButtons = null;
		protected List< InventoryButton > containerButtons = null;
		protected List< InventoryButton > highlightedButtons = new List< InventoryButton >();

		protected InventoryGrid playerGrid = null;
		protected InventoryGrid containerGrid = null;
		protected InventoryButton firstButton = null;
		protected InventoryButton previousButton = null;
		protected InventoryButton currentButton = null;

		public InventoryButtonTrackingContext(
			InventoryGui inventoryGui, // Remove?
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
		{
			// containerGrid can be null, but the list from the GUI cannot be
			if( playerGrid == null || playerButtons == null || containerButtons == null )
			{
				CurrentState = State.Invalid;
				return;
			}

			this.playerGrid = playerGrid;
			this.playerButtons = playerButtons;
			this.containerGrid = containerGrid;
			this.containerButtons = containerButtons;
			this.firstButton = firstButton ?? MouseTweaks.InventoryGuiPatch.GetHoveredButton( this.playerGrid , this.containerGrid );
			if( this.firstButton == null )
			{
				System.Console.WriteLine( $"CNTX: firstButton is null" );
				CurrentState = State.Invalid;
				return;
			}

			VanillaDragState dragState = new VanillaDragState();
			UpdateInventoryButtons( playerGrid , this.playerButtons , dragState.dragItem );
			UpdateInventoryButtons( containerGrid , this.containerButtons , dragState.dragItem );
		}

		protected void UpdateInventoryButtons(
			InventoryGrid grid,
			List< InventoryButton > buttons,
			ItemDrop.ItemData dragItem )
		{
			Inventory inv = grid.GetInventory();

			foreach( InventoryButton button in buttons )
			{
				Vector2i gridPos = button.gridPos;
				ItemDrop.ItemData otherItem = inv.GetItemAt( gridPos.x , gridPos.y );
				button.considerForDrag = otherItem == null
					|| dragItem == null
					|| otherItem == dragItem
					|| Common.CanStackOnto( dragItem , otherItem );
				button.representsExistingItem = otherItem != null;
			}
		}

		// IMouseContext overrides

		public override void End()
		{
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Normal , false ) );
			highlightedButtons.Clear();
		}

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;

			// TODO: At some point we should track whether items in the inventory changed.
			previousButton = currentButton;
			currentButton = MouseTweaks.InventoryGuiPatch.GetHoveredButton( playerGrid , containerGrid );
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Highlighted , true ) );

			return State.OK;
		}
	}

	public class ShiftLeftDragContext : InventoryButtonTrackingContext
	{
		public ShiftLeftDragContext(
			InventoryGui inventoryGui,
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
			: base( inventoryGui , playerGrid , playerButtons , containerGrid , containerButtons , firstButton )
		{
			// Nothing to do
		}

		// InventoryButtonTrackingContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending ShiftLeftDragContext" );

			base.End();
			VanillaDragState.ClearDrag();
		}

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.Done );
			else if( !FrameInputs.Current.LeftOnly )
				return SetState( State.Invalid );
			else if( currentButton == null || currentButton == previousButton )
				return SetState( State.NoChange );
			else if( currentButton != firstButton )
				firstButton.SetSelectionState( InventoryButton.SelectionState.Normal , false ); // Why only on player grid?

			InventoryGrid grid = currentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = currentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			grid.m_onSelected( grid , item , gridPos , InventoryGrid.Modifier.Move );

			return SetState( State.OK );
		}
	}

	// TODO: This is identical to ShiftLeftDragContext except for a modifier key
	public class ShiftCtrlLeftDragContext : InventoryButtonTrackingContext
	{
		protected string itemName;

		public ShiftCtrlLeftDragContext(
			InventoryGui inventoryGui,
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
			: base( inventoryGui , playerGrid , playerButtons , containerGrid , containerButtons , firstButton )
		{
			if( CurrentState != State.Invalid )
			{
				itemName = this.firstButton.curItem?.m_shared?.m_name;
				if( itemName == null )
					CurrentState = State.Invalid;
			}
		}

		// InventoryButtonTrackingContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending ShiftCtrlLeftDragContext" );

			base.End();
			VanillaDragState.ClearDrag();
		}

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.Done );
			else if( !FrameInputs.Current.LeftOnly )
				return SetState( State.Invalid );
			else if( currentButton == null || currentButton == previousButton )
				return SetState( State.NoChange );
			else if( currentButton != firstButton )
				firstButton.SetSelectionState( InventoryButton.SelectionState.Normal , false ); // Why only on player grid?

			InventoryGrid grid = currentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = currentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			if( !item.m_shared.m_name.Equals( itemName ) )
				return SetState( State.NoChange );

			grid.m_onSelected( grid , item , gridPos , InventoryGrid.Modifier.Move );
			return SetState( State.OK );
		}
	}
	
	public class LeftDragContext : InventoryButtonTrackingContext
	{
		protected List< InventoryButton > smearedButtons = new List< InventoryButton >();

		public LeftDragContext(
			InventoryGui inventoryGui,
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
			: base( inventoryGui , playerGrid , playerButtons , containerGrid , containerButtons , firstButton )
		{
			if( CurrentState == State.Invalid || VanillaDragState.IsValid() )
			{
				CurrentState = State.Invalid;
				return;
			}

			this.firstButton.considerForDrag = false;
			smearedButtons.Add( this.firstButton );
		}

		// InventoryButtonTrackingContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending LeftDragContext" );

			if( CurrentState == State.Done && smearedButtons.Count > 1 )
			{
				ItemDrop.ItemData splitItem = firstButton?.curItem;
				if( splitItem != null )
				{
					int amount = Mathf.Max( 1 , Mathf.FloorToInt( splitItem.m_stack / (float)smearedButtons.Count ) );

					foreach( InventoryButton button in smearedButtons )
					{
						if( button == firstButton )
							continue;
						else if( splitItem.m_stack < ( 2 * amount ) )
							break;

						Inventory inv = button.grid.GetInventory();
						Vector2i gridPos = button.gridPos;
						MouseTweaks.InventoryGuiPatch.AddItem( inv , splitItem , amount , gridPos.x , gridPos.y );
					}
				}
			}

			base.End();
			VanillaDragState.ClearDrag();
		}

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.Done );
			else if( !FrameInputs.Current.LeftOnly || dragState.isValid )
				return SetState( State.Invalid );
			else if( currentButton == null || currentButton == previousButton || currentButton == firstButton || !currentButton.considerForDrag )
				return SetState( State.NoChange );

			ItemDrop.ItemData targetItem = currentButton.curItem;
			if( targetItem == null || Common.CanStackOnto( firstButton.curItem , targetItem ) )
			{
				Vector2i gridPos = currentButton.gridPos;
				System.Console.WriteLine( $"DRAG: Left, into ({gridPos})" );
				currentButton.SetSelectionState( InventoryButton.SelectionState.Highlighted , true );
				currentButton.considerForDrag = false;
				highlightedButtons.Add( currentButton );
				smearedButtons.Add( currentButton );
			}
			else
			{
				System.Console.WriteLine( $"DRAG: Cannot stack {firstButton.curItem} onto {targetItem}" );
			}

			return SetState( State.OK );
		}
	}

	public class RightDragContext : InventoryButtonTrackingContext
	{
		public RightDragContext(
			InventoryGui inventoryGui,
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
			: base( inventoryGui , playerGrid , playerButtons , containerGrid , containerButtons , firstButton )
		{
			// Nothing to do
		}

		// InventoryButtonTrackingContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"CNTX: Ending RightDragContext" );

			base.End();
		}

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.Invalid || CurrentState == State.Done )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.Done );
			else if( !FrameInputs.Current.RightOnly )
				return SetState( State.Invalid );
			else if( currentButton == null || currentButton == previousButton || !currentButton.considerForDrag )
				return SetState( State.NoChange );
			else if( !dragState.isValid )
				return SetState( State.Done );

			// Not blocking single drops on the stack represented by the cursor is intentional
			Inventory inv = currentButton.grid.GetInventory();
			ItemDrop.ItemData dragItem = dragState.dragItem;
			Vector2i gridPos = currentButton.gridPos;
			if( !MouseTweaks.InventoryGuiPatch.AddItem( inv , dragItem , 1 , gridPos.x , gridPos.y ) )
				return SetState( State.NoChange );

			System.Console.WriteLine( $"DRAG: Right, into ({gridPos})" );
			currentButton.SetSelectionState( InventoryButton.SelectionState.Highlighted , true );
			currentButton.considerForDrag = false;
			highlightedButtons.Add( currentButton );

			// AddItem() modifies the input stack size
			if( dragState.dragItem.m_stack <= 0 )
			{
				Inventory playerInv = playerGrid.GetInventory();
				Inventory owningInv = playerInv.ContainsItem( dragItem ) ? playerInv : containerGrid.GetInventory();
				owningInv.RemoveItem( dragItem );
			}

			if( !dragState.Decrement() || !VanillaDragState.IsValid() )
				return SetState( State.Done );

			( new VanillaDragState() ).UpdateTooltip();
			return SetState( State.OK );
		}
	}
}
