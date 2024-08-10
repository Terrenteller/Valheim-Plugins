using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InputTweaks
{
	// What if we have a context wrapper (nested contexts?) that deals with input validity?
	// When the input, major or minor, goes invalid, it finalizes the wrapped context
	// and blocks/no-ops until the major input becomes invalid.
	// This way, we can create the outer context all the same, but create an inner context with more control.
	public class ContextState
	{
		public bool Active { get; protected set; } = true;
		public bool Valid { get; protected set; } = true;
		//public Lockable< bool > Valid2 = new Lockable< bool >( false , true );

		public ContextState SetActive( bool value )
		{
			if( Active && !value )
			{
				Common.DebugMessage( $"CNTX: Changing state from Active to Inactive" );
				Active = value;
			}

			return this;
		}

		public ContextState SetValid( bool value )
		{
			if( Valid && !value )
			{
				Common.DebugMessage( $"CNTX: Changing state from Valid to Invalid" );
				Valid = value;
			}

			return this;
		}
	}

	public abstract class AbstractInventoryGuiCursorContext
	{
		public enum State
		{
			ActiveValid,
			ActiveInvalid,
			DoneValid,
			DoneInvalid
		}

		public virtual State CurrentState { get; protected set; } = State.ActiveValid;

		protected virtual bool UserInputSatisfied()
		{
			return true;
		}

		// Make the state internal and only return a boolean?
		public abstract State Think( VanillaDragState dragState );

		protected virtual State SetState( State state )
		{
			if( CurrentState != state )
			{
				Common.DebugMessage( $"CNTX: Changing state from {CurrentState} to {state}" );
				CurrentState = state;
			}

			return state;
		}

		public abstract void End();
	}

	public class BlockingCursorContext : AbstractInventoryGuiCursorContext
	{
		protected Func< bool > blockRequirement; // Intended for modifier keys, but we can't enforce that

		public BlockingCursorContext( Func< bool > blockRequirement )
		{
			this.blockRequirement = blockRequirement;
		}

		// AbstractInventoryGuiCursorContext overrides

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( CurrentState == State.ActiveInvalid || ( blockRequirement != null && !blockRequirement.Invoke() ) )
				return SetState( State.ActiveInvalid );

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending BlockingCursorContext" );
		}
	}
	
	public abstract class InventoryButtonTrackingContext : AbstractInventoryGuiCursorContext
	{
		protected InventoryGrid playerGrid = null;
		protected List< InventoryButton > playerButtons = null;
		protected InventoryGrid containerGrid = null;
		protected List< InventoryButton > containerButtons = null;
		protected InventoryButton firstButton = null;
		protected InventoryButton previousButton = null;
		protected InventoryButton currentButton = null;
		protected List< InventoryButton > highlightedButtons = new List< InventoryButton >();

		public InventoryButtonTrackingContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
		{
			// containerGrid can be null, but the list from the GUI cannot be
			if( playerGrid == null || playerButtons == null || containerButtons == null )
			{
				Common.DebugMessage( $"CNTX: One or more null arguments" );
				CurrentState = State.DoneInvalid;
				return;
			}

			this.playerGrid = playerGrid;
			this.playerButtons = playerButtons;
			this.containerGrid = containerGrid;
			this.containerButtons = containerButtons;
			firstButton =  InputTweaks.InventoryGuiPatch.GetHoveredButton( this.playerGrid , this.containerGrid );
			if( firstButton == null )
			{
				Common.DebugMessage( $"CNTX: Started on a null button" );
				CurrentState = State.DoneInvalid;
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

		// AbstractInventoryGuiCursorContext overrides

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;

			// Do we need to track whether items in the inventory changed?
			previousButton = currentButton;
			currentButton = InputTweaks.InventoryGuiPatch.GetHoveredButton( playerGrid , containerGrid );
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Highlighted , true ) );

			return State.ActiveValid;
		}

		public override void End()
		{
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Normal , false ) );
			highlightedButtons.Clear();
		}
	}

	public class MultiClickContext : InventoryButtonTrackingContext
	{
		protected bool collected = false;

		public MultiClickContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			if( CurrentState == State.ActiveValid && firstButton == null )
				SetState( State.ActiveInvalid );
		}
		
		protected bool CollectFromInto( InventoryGrid grid , ItemDrop.ItemData item )
		{
			List< ItemDrop.ItemData > equivalentStackables = Common.EquivalentStackables( item , grid ).ToList();
			Common.DebugMessage( $"DBLC: Collecting {item} from {equivalentStackables.Count} equivalent stacks" );
			Inventory inv = grid.GetInventory();

			foreach( ItemDrop.ItemData equivalent in equivalentStackables )
			{
				int transfer = Mathf.Min( item.m_shared.m_maxStackSize - item.m_stack , equivalent.m_stack );
				Common.DebugMessage( $"DBLC: Moving {transfer} item(s)" );
				if( inv.RemoveItem( equivalent , transfer ) )
					item.m_stack += transfer; // Implementation Detail: We expect the caller to update weights
			}

			return item.m_stack < item.m_shared.m_maxStackSize;
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.successiveClicks == 0 )
				return SetState( CurrentState == State.ActiveValid ? State.DoneValid : State.DoneInvalid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() || currentButton != firstButton )
				return SetState( State.ActiveInvalid );
			else if( collected )
				return SetState( State.ActiveValid );

			InventoryGrid grid = currentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = currentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			if( item == null )
			{
				return SetState( State.DoneInvalid );
			}
			else if( item.m_stack < item.m_shared.m_maxStackSize )
			{
				// Prioritize the grid the item is in
				InventoryGrid otherGrid = grid == playerGrid ? containerGrid : playerGrid;
				if( CollectFromInto( grid , item ) )
					CollectFromInto( otherGrid , item );

				// FIXME: CollectFromInto() doesn't perfectly update weights because the item and grid args may not align
				InputTweaks.InventoryPatch.Changed( inv );
				InputTweaks.InventoryPatch.Changed( otherGrid.GetInventory() );
			}

			// Pick the item back up since we trigger on a double-click that put the item down
			InputTweaks.InventoryGuiPatch.SetupDragItem( InventoryGui.instance , item , inv , item.m_stack );
			collected = true;

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending MultiClickContext" );
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.successiveClicks > 0 && Common.CheckModifier( InputTweaks.ModifierKeyEnum.None );
		}
	}

	public class StackMoveContext : InventoryButtonTrackingContext
	{
		public StackMoveContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			// Nothing to do
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( dragState.isValid )
				return SetState( State.DoneInvalid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() )
				return SetState( State.ActiveInvalid );
			else if( currentButton == null || currentButton == previousButton )
				return SetState( State.ActiveValid );
			else if( currentButton != firstButton )
				firstButton.SetSelectionState( InventoryButton.SelectionState.Normal , false ); // Why only on player grid?

			InventoryGrid grid = currentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = currentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			grid.m_onSelected( grid , item , gridPos , InventoryGrid.Modifier.Move );

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending StackMoveContext" );

			base.End();
			VanillaDragState.ClearDrag();
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.LeftOnly
				&& Common.CheckModifier( InputTweaks.ModifierKeyEnum.Move )
				&& !Common.CheckModifier( InputTweaks.ModifierKeyEnum.Split );
		}
	}

	public class FilteredStackMoveContext : InventoryButtonTrackingContext
	{
		protected string itemName;

		public FilteredStackMoveContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			if( CurrentState != State.DoneValid || CurrentState != State.DoneInvalid )
			{
				itemName = firstButton.curItem?.m_shared?.m_name;
				if( itemName == null )
					SetState( State.DoneInvalid );
			}
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( dragState.isValid )
				return SetState( State.DoneInvalid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() )
				return SetState( State.ActiveInvalid );
			else if( currentButton == null || currentButton == previousButton )
				return SetState( State.ActiveValid );
			else if( currentButton != firstButton )
				firstButton.SetSelectionState( InventoryButton.SelectionState.Normal , false ); // Why only on player grid?

			InventoryGrid grid = currentButton.grid;
			Inventory inv = grid.GetInventory();
			Vector2i gridPos = currentButton.gridPos;
			ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
			if( item != null && item.m_shared.m_name.Equals( itemName ) )
				grid.m_onSelected( grid , item , gridPos , InventoryGrid.Modifier.Move );

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending FilteredStackMoveContext" );

			base.End();
			VanillaDragState.ClearDrag();
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.LeftOnly && Common.AnyShift() && Common.AnyCtrl();
		}
	}
	
	public class StackSmearContext : InventoryButtonTrackingContext
	{
		protected List< InventoryButton > smearedButtons = new List< InventoryButton >();
		protected Inventory swappedInv;
		protected Vector2i swappedPos;

		public StackSmearContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons,
			Inventory swappedInv,
			Vector2i swappedPos )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			if( CurrentState == State.DoneInvalid || VanillaDragState.IsValid() )
			{
				CurrentState = State.DoneInvalid;
				return;
			}

			firstButton.considerForDrag = false;
			smearedButtons.Add( firstButton );
			this.swappedInv = swappedInv;
			this.swappedPos = swappedPos;

			if( ( firstButton.curItem?.m_stack ?? 0 ) <= 1 )
				CurrentState = State.DoneValid;
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( dragState.isValid )
				return SetState( State.DoneInvalid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() )
				return SetState( State.ActiveInvalid );
			else if( currentButton == null || currentButton == previousButton || currentButton == firstButton || !currentButton.considerForDrag )
				return SetState( State.ActiveValid );

			ItemDrop.ItemData targetItem = currentButton.curItem;
			if( targetItem == null || Common.CanStackOnto( firstButton.curItem , targetItem ) )
			{
				Vector2i gridPos = currentButton.gridPos;
				Common.DebugMessage( $"CNTX: Left, into ({gridPos})" );
				currentButton.SetSelectionState( InventoryButton.SelectionState.Highlighted , true );
				currentButton.considerForDrag = false;
				highlightedButtons.Add( currentButton );
				smearedButtons.Add( currentButton );
			}
			else
			{
				Common.DebugMessage( $"CNTX: Cannot stack {firstButton.curItem} onto {targetItem}" );
			}

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending StackSmearContext" );

			bool clearDrag = true;

			if( CurrentState == State.DoneValid )
			{
				if( smearedButtons.Count == 1 )
				{
					// TODO: This does not belong here. It has nothing to do with a smear.
					// We do not have a context framework refined enough to smoothly transition from one to another,
					// much less one that doesn't require contexts to know about each other.
					// Ideally, selected items go into a transient inventory slot. How do other inventory plugins do this?
					if( InputTweaks.SelectSwappedItem.Value && swappedInv != null && firstButton.gridPos != swappedPos )
					{
						// Because this is the wrong place for this logic, we have to check whether a partial stack
						// on the cursor was merely added to our stack. We only swap different or unstackable items.
						ItemDrop.ItemData smearItem = firstButton.curItem;
						ItemDrop.ItemData swappedItem = swappedInv.GetItemAt( swappedPos.x , swappedPos.y );
						if( swappedItem != null && !Common.CanStackOnto( smearItem , swappedItem ) )
						{
							Common.DebugMessage( $"INFO: Selecting swapped item" );

							InputTweaks.InventoryGuiPatch.SetupDragItem(
								InventoryGui.instance,
								swappedItem,
								swappedInv,
								swappedItem.m_stack );
							
							clearDrag = false;
						}
					}
				}
				else
				{
					ItemDrop.ItemData splitItem = firstButton?.curItem;
					if( splitItem != null )
					{
						// Don't think we should balance existing stacks... Might make a good config option though.
						// What to do with the remainder though?
						/*
						int total = splitItem.m_stack;
						List< InventoryButton > matchingButtons = new List< InventoryButton >();
						matchingButtons.Add( firstButton );

						foreach( InventoryButton smearedButton in smearedButtons )
						{
							ItemDrop.ItemData buttonItem = smearedButton.curItem;
							if( buttonItem == null || Common.ItemsAreSimilarButDistinct( splitItem , buttonItem ) )
							{
								total += buttonItem.m_stack;
								matchingButtons.Add( smearedButton );
							}
						}

						int balancedValue = Mathf.Max( 1 , Mathf.FloorToInt( total / (float)matchingButtons.Count ) );
						foreach( InventoryButton matchingButton in matchingButtons )
						{
							Inventory inv = matchingButton.grid.GetInventory();
							Vector2i gridPos = matchingButton.gridPos;

							// TODO
						}
						*/

						int perStack = Mathf.Max( 1 , Mathf.FloorToInt( splitItem.m_stack / (float)smearedButtons.Count ) );
						int remainder = splitItem.m_stack - perStack; // Subtract the source stack's share

						foreach( InventoryButton button in smearedButtons )
						{
							if( button == firstButton )
								continue;
							else if( splitItem.m_stack < ( 2 * perStack ) )
								break;

							Inventory inv = button.grid.GetInventory();
							Vector2i gridPos = button.gridPos;
							if( InputTweaks.InventoryGuiPatch.AddItem( inv , splitItem , perStack , gridPos.x , gridPos.y ) )
								remainder -= perStack;
						}

						if( InputTweaks.KeepRemainderOnCursor.Value && remainder > 0 )
						{
							InputTweaks.InventoryGuiPatch.SetupDragItem(
								InventoryGui.instance,
								splitItem,
								firstButton.grid.GetInventory(),
								remainder );

							clearDrag = false;
						}
					}
				}
			}

			base.End();
			if( clearDrag )
				VanillaDragState.ClearDrag();
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.LeftOnly && Common.CheckModifier( InputTweaks.ModifierKeyEnum.None );
		}
	}
	
	public class StackCollectContext : InventoryButtonTrackingContext
	{
		public StackCollectContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			if( CurrentState == State.DoneInvalid || !VanillaDragState.IsValid() )
				CurrentState = State.DoneInvalid;
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( !dragState.isValid )
				return SetState( State.DoneInvalid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() )
				return SetState( State.ActiveInvalid );
			else if( currentButton == null || currentButton == previousButton || currentButton == firstButton )
				return SetState( State.ActiveValid );

			ItemDrop.ItemData sourceItem = currentButton.curItem;
			ItemDrop.ItemData targetItem = firstButton.curItem;
			if( Common.CanStackOnto( sourceItem , targetItem ) )
			{
				Common.DebugMessage( $"CNTX: Left, from ({currentButton.gridPos})" );

				Inventory sourceInv = currentButton.grid.GetInventory();
				Inventory targetInv = firstButton.grid.GetInventory();
				int transfer = Mathf.Min( targetItem.m_shared.m_maxStackSize - targetItem.m_stack , sourceItem.m_stack );
				Common.DebugMessage( $"CNTX: Moving {transfer} item(s)" );
				if( sourceInv.RemoveItem( sourceItem , transfer ) )
				{
					targetItem.m_stack += transfer;
					if( dragState.Increment( transfer ) )
					{
						dragState = new VanillaDragState();
						dragState.UpdateTooltip();
						UITooltip.HideTooltip(); // This is the item description tooltip, which overlaps
					}

					if( targetInv != sourceInv )
						InputTweaks.InventoryPatch.Changed( targetInv );
				}
			}
			else
			{
				Common.DebugMessage( $"CNTX: Cannot stack {sourceItem} onto {targetItem}" );
			}

			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending StackCollectContext" );

			base.End();
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.LeftOnly && Common.CheckModifier( InputTweaks.ModifierKeyEnum.None );
		}
	}

	public class SingleSmearContext : InventoryButtonTrackingContext
	{
		public SingleSmearContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			// Nothing to do
		}

		// InventoryButtonTrackingContext overrides

		public override State Think( VanillaDragState dragState )
		{
			base.Think( dragState );

			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None || !dragState.isValid )
				return SetState( State.DoneValid );
			else if( CurrentState == State.ActiveInvalid || !UserInputSatisfied() )
				return SetState( State.ActiveInvalid );
			else if( currentButton == null || currentButton == previousButton || !currentButton.considerForDrag )
				return SetState( State.ActiveValid );

			// Not blocking single drops on the stack represented by the cursor is intentional
			Inventory inv = currentButton.grid.GetInventory();
			ItemDrop.ItemData dragItem = dragState.dragItem;
			Vector2i gridPos = currentButton.gridPos;
			if( !InputTweaks.InventoryGuiPatch.AddItem( inv , dragItem , 1 , gridPos.x , gridPos.y ) )
				return SetState( State.ActiveValid );

			Common.DebugMessage( $"CNTX: Right, into ({gridPos})" );
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

			if( dragState.Decrement() )
				( new VanillaDragState() ).UpdateTooltip();

			// We're not really done until buttons are released 
			return SetState( State.ActiveValid );
		}

		public override void End()
		{
			Common.DebugMessage( $"CNTX: Ending SingleSmearContext" );

			base.End();
		}

		// AbstractInventoryGuiCursorContext overrides

		protected override bool UserInputSatisfied()
		{
			return FrameInputs.Current.RightOnly && Common.CheckModifier( InputTweaks.ModifierKeyEnum.None );
		}
	}
}
