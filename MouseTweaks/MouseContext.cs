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
			CurrentState = state;
			return state;
		}

		public abstract State Think( VanillaDragState dragState );
		public abstract void End();
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
			System.Console.WriteLine( $"DRAG: Ending BlockingLeftMouseContext" );
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
			System.Console.WriteLine( $"DRAG: Ending BlockingRightMouseContext" );
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
		protected List< InventoryButton > PlayerButtons = null;
		protected List< InventoryButton > ContainerButtons = null;
		protected List< InventoryButton > HighlightedButtons = new List< InventoryButton >();

		protected InventoryGrid PlayerGrid = null;
		protected InventoryGrid ContainerGrid = null;
		protected InventoryButton FirstButton = null;
		protected InventoryButton PreviousButton = null;
		protected InventoryButton CurrentButton = null;

		public InventoryButtonTrackingContext(
			InventoryGui inventoryGui, // Remove?
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
		{
			PlayerGrid = playerGrid;
			PlayerButtons = playerButtons;
			ContainerGrid = containerGrid;
			ContainerButtons = containerButtons;
			FirstButton = firstButton ?? MouseTweaks.InventoryGuiPatch.GetHoveredButton( playerGrid , containerGrid );

			VanillaDragState dragState = new VanillaDragState();
			UpdateInventoryButtons( playerGrid , PlayerButtons , dragState.dragItem );
			UpdateInventoryButtons( containerGrid , ContainerButtons , dragState.dragItem );
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

		public override void End() // TODO: force option? Like the user closed the GUI
		{
			//PlayerButtons.Clear();
			//ContainerButtons.Clear();
			HighlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Normal , false ) );
			HighlightedButtons.Clear();
			// Should this set considerForDrag to false?
		}

		public override State Think( VanillaDragState dragState )
		{
			// TODO: Should we track mouse frames here?
			// TODO: At some point we should track whether items in the inventory changed.
			PreviousButton = CurrentButton;
			CurrentButton = MouseTweaks.InventoryGuiPatch.GetHoveredButton( PlayerGrid , ContainerGrid );
			HighlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Highlighted , true ) );

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
			System.Console.WriteLine( $"DRAG: Ending ShiftLeftDragContext" );

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
			else if( CurrentButton == null || CurrentButton == PreviousButton )
				return SetState( State.NoChange );
			else if( CurrentButton != FirstButton )
				FirstButton.SetSelectionState( InventoryButton.SelectionState.Normal , false ); // Why only on player grid?

			InventoryGrid grid = CurrentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = CurrentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			grid.m_onSelected( grid , item , gridPos , InventoryGrid.Modifier.Move );

			return SetState( State.OK );
		}
	}
	
	public class LeftDragContext : InventoryButtonTrackingContext
	{
		protected List< InventoryButton > SmearedButtons = new List< InventoryButton >();

		public LeftDragContext(
			InventoryGui inventoryGui,
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			InventoryButton firstButton )
			: base( inventoryGui , playerGrid , playerButtons , containerGrid , containerButtons , firstButton )
		{
			FirstButton.considerForDrag = false;
			SmearedButtons.Add( FirstButton );
			// TODO: Start invalid if there's a valid drag?
		}

		// InventoryButtonTrackingContext overrides

		public override void End()
		{
			System.Console.WriteLine( $"DRAG: Ending LeftDragContext" );

			if( CurrentState == State.Done && SmearedButtons.Count > 1 )
			{
				ItemDrop.ItemData splitItem = FirstButton?.curItem;
				if( splitItem != null )
				{
					int amount = Mathf.Max( 1 , Mathf.FloorToInt( splitItem.m_stack / (float)SmearedButtons.Count ) );

					foreach( InventoryButton button in SmearedButtons )
					{
						if( button == FirstButton )
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
			else if( CurrentButton == null || CurrentButton == PreviousButton || CurrentButton == FirstButton || !CurrentButton.considerForDrag )
				return SetState( State.NoChange );

			ItemDrop.ItemData targetItem = CurrentButton.curItem;
			if( targetItem == null || Common.CanStackOnto( FirstButton.curItem , targetItem ) )
			{
				Vector2i gridPos = CurrentButton.gridPos;
				System.Console.WriteLine( $"DRAG: Smear into ({gridPos})" );
				CurrentButton.SetSelectionState( InventoryButton.SelectionState.Highlighted , true );
				CurrentButton.considerForDrag = false;
				HighlightedButtons.Add( CurrentButton );
				SmearedButtons.Add( CurrentButton );
			}
			else
			{
				System.Console.WriteLine( $"DRAG: Cannot stack {FirstButton.curItem} onto {targetItem}" );
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
			System.Console.WriteLine( $"DRAG: Ending RightDragContext" );

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
			else if( CurrentButton == null || CurrentButton == PreviousButton || !CurrentButton.considerForDrag )
				return SetState( State.NoChange );
			else if( !dragState.isValid )
				return SetState( State.Done );

			// Not blocking single drops on the stack represented by the cursor is intentional
			Inventory inv = CurrentButton.grid.GetInventory();
			ItemDrop.ItemData dragItem = dragState.dragItem;
			Vector2i gridPos = CurrentButton.gridPos;
			if( !MouseTweaks.InventoryGuiPatch.AddItem( inv , dragItem , 1 , gridPos.x , gridPos.y ) )
				return SetState( State.NoChange );

			System.Console.WriteLine( $"DRAG: Right, into acceptable slot ({gridPos})" );
			CurrentButton.SetSelectionState( InventoryButton.SelectionState.Highlighted , true );
			CurrentButton.considerForDrag = false;
			HighlightedButtons.Add( CurrentButton );

			// AddItem() modifies the input stack size
			if( dragState.dragItem.m_stack <= 0 )
			{
				Inventory playerInv = PlayerGrid.GetInventory();
				Inventory owningInv = playerInv.ContainsItem( dragItem ) ? playerInv : ContainerGrid.GetInventory();
				owningInv.RemoveItem( dragItem );
			}

			if( !dragState.Decrement() || !VanillaDragState.IsValid() )
				return SetState( State.Done );

			( new VanillaDragState() ).UpdateTooltip();
			return SetState( State.OK );
		}
	}
}
