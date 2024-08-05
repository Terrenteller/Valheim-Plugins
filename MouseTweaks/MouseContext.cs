using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MouseTweaks
{
	public struct Lockable< T >
	{
		public T value;

		public Lockable( bool locked , T value )
		{
			Locked = locked;
			this.value = value;
		}

		public T Value
		{
			get
			{
				return value;
			}
			set
			{
				if( !Locked )
					this.value = value;
			}
		}

		public bool Locked { get; set; }
	}

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
		protected Func< bool > dynamicRequirement; // Intended for modifier keys, but we can't enforce that

		public BlockingCursorContext( Func< bool > dynamicRequirement )
		{
			this.dynamicRequirement = dynamicRequirement;
		}

		// AbstractInventoryGuiCursorContext overrides

		public override State Think( VanillaDragState dragState )
		{
			if( CurrentState == State.DoneValid || CurrentState == State.DoneInvalid )
				return CurrentState;
			else if( FrameInputs.Current.None )
				return SetState( State.DoneValid );
			else if( CurrentState == State.ActiveInvalid || ( dynamicRequirement != null && !dynamicRequirement.Invoke() ) )
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
			firstButton =  MouseTweaks.InventoryGuiPatch.GetHoveredButton( this.playerGrid , this.containerGrid );
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
			currentButton = MouseTweaks.InventoryGuiPatch.GetHoveredButton( playerGrid , containerGrid );
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Highlighted , true ) );

			return State.ActiveValid;
		}

		public override void End()
		{
			highlightedButtons.ForEach( x => x.SetSelectionState( InventoryButton.SelectionState.Normal , false ) );
			highlightedButtons.Clear();
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
				&& ( MouseTweaks.InitialSwapShiftAndCtrl
					? ( Common.AnyShift() && !Common.AnyControl() )
					: ( Common.AnyControl() && !Common.AnyShift() ) );
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
			return FrameInputs.Current.LeftOnly && Common.AnyShift() && Common.AnyControl();
		}
	}
	
	public class StackSmearContext : InventoryButtonTrackingContext
	{
		protected List< InventoryButton > smearedButtons = new List< InventoryButton >();

		public StackSmearContext(
			InventoryGrid playerGrid,
			List< InventoryButton > playerButtons,
			InventoryGrid containerGrid,
			List< InventoryButton > containerButtons )
			: base( playerGrid , playerButtons , containerGrid , containerButtons )
		{
			if( CurrentState == State.DoneInvalid || VanillaDragState.IsValid() )
			{
				CurrentState = State.DoneInvalid;
				return;
			}

			firstButton.considerForDrag = false;
			smearedButtons.Add( firstButton );
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

			if( CurrentState == State.DoneValid && smearedButtons.Count > 1 )
			{
				ItemDrop.ItemData splitItem = firstButton?.curItem;
				if( splitItem != null )
				{
					// Don't think we should balance existing stack... Might make a good config option though.
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
						if( MouseTweaks.InventoryGuiPatch.AddItem( inv , splitItem , perStack , gridPos.x , gridPos.y ) )
							remainder -= perStack;
					}

					if( MouseTweaks.KeepRemainderOnCursor.Value && remainder > 0 )
					{
						MouseTweaks.InventoryGuiPatch.SetupDragItem(
							InventoryGui.instance,
							splitItem,
							firstButton.grid.GetInventory(),
							remainder );

						clearDrag = false;
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
			return FrameInputs.Current.LeftOnly && !Common.AnyShift() && !Common.AnyControl();
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
			if( !MouseTweaks.InventoryGuiPatch.AddItem( inv , dragItem , 1 , gridPos.x , gridPos.y ) )
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

			if( dragState.Decrement() && VanillaDragState.IsValid() )
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
			return FrameInputs.Current.RightOnly && !Common.AnyShift() && !Common.AnyControl();
		}
	}
}
