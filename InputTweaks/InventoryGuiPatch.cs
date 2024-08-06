using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( InventoryGui ) )]
		public class InventoryGuiPatch
		{
			// Used to skip a very specific sequence that causes RMB to reset the drag
			private static bool IgnoreUpdateItemDragRightMouseReset = false;
			// Hide() is called at a bad time by a code path ILSpy is not revealing.
			// This causes UpdatePostfix() to cache soon-to-be stale buttons immediately after HidePostfix()
			// clears them, preventing fresh buttons from being cached when the GUI is shown next.
			private static bool SkipNextUpdate = false;
			private static bool SingleDropCoolDown = false;
			private static AbstractInventoryGuiCursorContext MouseContext = null;
			private static VanillaDragState LastDragState = null;
			private static VanillaDragState CurrentDragState = new VanillaDragState();
			private static List< InventoryButton > PlayerButtons = new List< InventoryButton >();
			private static List< InventoryButton > ContainerButtons = new List< InventoryButton >();
			private static InventoryButton CurrentButton = null;

			#region Helpers

			private static void CollectInventoryButtons( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( PlayerButtons.Count == 0 && ContainerButtons.Count == 0 )
				{
					Common.DebugMessage( $"INFO: Attempting to index PlayerButtons" );
					CollectInventoryButtons( playerGrid , PlayerButtons );
					Common.DebugMessage( $"INFO: Indexed {PlayerButtons.Count} player buttons" );

					Common.DebugMessage( $"INFO: Attempting to index ContainerButtons" );
					CollectInventoryButtons( containerGrid , ContainerButtons );
					Common.DebugMessage( $"INFO: Indexed {ContainerButtons.Count} container buttons" );
				}
			}

			private static void CollectInventoryButtons( InventoryGrid grid , List< InventoryButton > buttons )
			{
				buttons.Clear();

				Inventory inv = grid?.GetInventory();
				if( inv == null )
					return;

				int width = inv.GetWidth();
				int height = inv.GetHeight();
				if( width <= 0 || height <= 0 )
					return;

				// EVIL: If the object argument is not specified, it news ONE up, and sets ALL new indices to it
				buttons.Resize( width * height , null );
				foreach( UIInputHandler inputHandler in grid.GetComponentsInChildren< UIInputHandler >() )
				{
					Vector2i gridPos = InventoryGridPatch.GetButtonPos( grid , inputHandler );
					if( gridPos.x == -1 || gridPos.y == -1 )
						continue; // UnityEngine.Object.Destroy() does not destroy on the same frame

					int gridIndex = gridPos.x + ( gridPos.y * width );
					buttons[ gridIndex ] = new InventoryButton
					{
						inputHandler = inputHandler,
						grid = grid,
						gridPos = gridPos,
						considerForDrag = true,
						representsExistingItem = inv.GetItemAt( gridPos.x , gridPos.y ) != null
					};
				}
			}

			internal static InventoryButton GetHoveredButton( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( playerGrid != null && playerGrid.gameObject.activeInHierarchy && Common.IsCursorOver( playerGrid.gameObject ) )
					return PlayerButtons.Where( x => Common.IsCursorOver( x.inputHandler.gameObject ) ).FirstOrDefault();
				else if( containerGrid != null && containerGrid.gameObject.activeInHierarchy && Common.IsCursorOver( containerGrid.gameObject ) )
					return ContainerButtons.Where( x => Common.IsCursorOver( x.inputHandler.gameObject ) ).FirstOrDefault();

				return null;
			}

			#endregion

			#region MouseWheel

			private static void UpdateMouseWheel( InventoryGui gui , InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( MouseContext != null || CurrentDragState.isValid || FrameInputs.Current.Any )
					return;

				Player player = Player.m_localPlayer;
				float scroll = ZInput.GetMouseScrollWheel();
				if( player == null || scroll == 0.0f )
					return;

				GameObject splitPanel = gui.m_splitPanel.gameObject;
				if( splitPanel.activeInHierarchy )
				{
					// The split panel is the full size of the screen
					if( Common.IsCursorOver( gui.m_splitPanel.Find( "win_bkg" ) as RectTransform ) )
					{
						Slider splitSlider = gui.m_splitSlider;
						splitSlider.value = Mathf.Clamp(
							splitSlider.value + ( scroll > 0.0f ? 1.0f : -1.0f ),
							splitSlider.minValue,
							splitSlider.maxValue );

						Traverse.Create( gui )
							.Method( "OnSplitSliderChanged" , new[] { typeof( float ) } )
							.GetValue( Mathf.Clamp( splitSlider.value , splitSlider.minValue , splitSlider.maxValue ) );
					}

					return;
				}
				else if( CurrentButton == null )
					return;

				Inventory inv = CurrentButton.grid.GetInventory();
				Vector2i gridPos = CurrentButton.gridPos;
				ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
				if( item == null || item.m_stack < 1 )
					return;

				bool itemInContainer = CurrentButton.grid == containerGrid;
				bool wheelUp = scroll > 0.0f;
				WheelActionEnum action = ( itemInContainer ? ContainerWheelAction : PlayerWheelAction ).Value;
				if( action == WheelActionEnum.None )
					return;

				bool pull = ( action == WheelActionEnum.PullUpPushDown ) == wheelUp;

				if( itemInContainer )
					MoveOne( player , pull , item , containerGrid , playerGrid );
				else if( gui.IsContainerOpen() )
					MoveOne( player , pull , item , playerGrid , containerGrid );
				else
					MoveOne( player , pull , item , playerGrid , null );
			}

			// Consider adding multipliers based on modifier keys like Endless Sky.
			// 2x and 5x are probably fine. We're working with considerably fewer items.
			private static void MoveOne( Player player , bool pull , ItemDrop.ItemData item , InventoryGrid owningGrid , InventoryGrid otherGrid )
			{
				if( otherGrid == owningGrid )
					otherGrid = null;

				Inventory owningInv = owningGrid.GetInventory();
				Inventory otherInv = otherGrid?.GetInventory();

				if( pull )
				{
					if( item.m_shared.m_maxStackSize == 1 )
					{
						if( otherInv == null )
						{
							Common.DebugMessage( $"PULL: Doesn't make sense" );
							return;
						}
						else if( !owningInv.HaveEmptySlot() )
						{
							Common.DebugMessage( $"PULL: No empty slots" );
							return;
						}
					}
					else if( item.m_stack == item.m_shared.m_maxStackSize )
					{
						if( otherInv == null )
						{
							Common.DebugMessage( $"PULL: Doesn't make sense" );
							return;
						}
						else if( !owningInv.HaveEmptySlot() && Common.FindPartialStack( item , owningInv ) == null )
						{
							Common.DebugMessage( $"PULL: No space" );
							return;
						}
					}

					// These are annoyingly similar
					if( otherInv != null )
					{
						ItemDrop.ItemData otherItem = item.m_shared.m_maxStackSize > 1
							? Common.EquivalentStackables( item , otherGrid ).FirstOrDefault()
							: Common.FindFirstSimilarItemInInventory( item , otherInv );

						if( otherItem == null || otherItem.m_stack < 1 )
						{
							Common.DebugMessage( $"PULL: No similar item" );
						}
						else if( otherItem == item ) // Actual programming error
						{
							Common.DebugMessage( $"PULL: Found self" );
							return;
						}
						else if( otherItem.m_shared.m_maxStackSize == 1 )
						{
							Common.DebugMessage( $"PULL: Single" );
							owningInv.MoveItemToThis( otherInv , otherItem );
							return;
						}
						else if( otherInv.RemoveOneItem( otherItem ) )
						{
							Common.DebugMessage( $"PULL: One item" );
							item.m_stack++;
							InventoryPatch.Changed( owningInv );
							return;
						}
					}

					{
						// How much do we want to simplify this?
						ItemDrop.ItemData otherItem = Common.EquivalentStackables( item , owningGrid ).FirstOrDefault();
						//if( otherItem != null && otherItem.m_stack > 0 && otherItem != item && owningInv.RemoveOneItem( otherItem ) )
						//	item.m_stack++;

						if( otherItem == null || otherItem.m_stack < 1 )
						{
							Common.DebugMessage( $"PULL: No similar item" );
							return;
						}
						else if( otherItem == item ) // Actual programming error
						{
							Common.DebugMessage( $"PULL: Found self" );
							return;
						}
						else if( owningInv.RemoveOneItem( otherItem ) )
						{
							Common.DebugMessage( $"PULL: One item" );
							item.m_stack++;
							InventoryPatch.Changed( otherInv );
							return;
						}
					}
				}
				else // Push
				{
					if( otherInv != null )
					{
						if( item.m_stack == 1 )
						{
							Common.DebugMessage( $"PUSH: Single or last" );
							player.RemoveEquipAction( item );
							player.UnequipItem( item );
							otherInv.MoveItemToThis( owningInv , item ); // ???: WHAT IF IT FAILS?
							UITooltip.HideTooltip();
							return;
						}

						if( item.m_stack == item.m_shared.m_maxStackSize )
							item = Common.FindPartialStack( item , owningInv ) ?? item;

						if( otherInv.AddItem( item.m_dropPrefab , 1 ) )
						{
							Common.DebugMessage( $"PUSH: One item" );
							owningInv.RemoveOneItem( item );
							return;
						}

						// TODO: Should we try to push to the same inventory if these fail?
					}
					else // Push to self
					{
						ItemDrop.ItemData otherItem = Common.FindPartialStack( item , owningInv );
						if( otherItem != null )
						{
							Common.DebugMessage( $"PUSH: One item to sibling" );
							otherItem.m_stack++;
							InventoryPatch.Changed( otherInv );
							owningInv.RemoveOneItem( item ); // TODO: What if this fails?
							if( item.m_stack < 1 )
								UITooltip.HideTooltip();

							return;
						}
						else if( item.m_stack == 1 ) // Only invalid if there's not a partial stack
						{
							Common.DebugMessage( $"PUSH: Doesn't make sense" );
							return;
						}

						Vector2i emptySlot = FindEmptySlot( owningInv , TopFirst( owningInv , item ) );
						if( emptySlot.x < 0 || emptySlot.y < 0 )
						{
							Common.DebugMessage( $"PUSH: No empty slot" );
							return;
						}
						else
						{
							Common.DebugMessage( $"PUSH: One item to empty slot" );
							AddItem( owningInv , item , 1 , emptySlot.x , emptySlot.y ); // Does subtraction
							return;
						}
					}
				}
			}

			#endregion

			#region DoubleClick

			private static void UpdateDoubleClick( InventoryGui instance , InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( !AllowDoubleClickCollect.Value
					|| !FrameInputs.Current.isDoubleClick
					|| !LastDragState.isValid
					|| CurrentDragState.isValid
					|| CurrentButton == null )
				{
					return;
				}

				InventoryGrid grid = CurrentButton.grid;
				Inventory inv = grid.GetInventory();
				Vector2i gridPos = CurrentButton.gridPos;
				ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
				if( item == null || item.m_stack >= item.m_shared.m_maxStackSize )
					return;

				// Prioritize the grid the item is in
				InventoryGrid otherGrid = grid == playerGrid ? containerGrid : playerGrid;
				if( CollectFromInto( grid , item ) )
					CollectFromInto( otherGrid , item );

				// FIXME: CollectFromInto() doesn't perfectly update weights because the item and grid args may not align
				InventoryPatch.Changed( inv );
				InventoryPatch.Changed( otherGrid.GetInventory() );

				// Pick the item back up since we trigger on a double-click that put the item down
				SetupDragItem( instance , item , inv , item.m_stack );
			}

			private static bool CollectFromInto( InventoryGrid grid , ItemDrop.ItemData item )
			{
				List< ItemDrop.ItemData > equivalentStackables = Common.EquivalentStackables( item , grid ).ToList();
				Common.DebugMessage( $"DBLC: Collecting {item} from {equivalentStackables.Count} equivalent stacks" );
				Inventory inv = grid.GetInventory();

				foreach( ItemDrop.ItemData equivalent in equivalentStackables )
				{
					int transfer = Mathf.Min( item.m_shared.m_maxStackSize - item.m_stack , equivalent.m_stack );
					Common.DebugMessage( $"DBLC: Moving {transfer}" );
					if( inv.RemoveItem( equivalent , transfer ) )
						item.m_stack += transfer; // Implementation Detail: We expect the caller to update weights
				}

				return item.m_stack < item.m_shared.m_maxStackSize;
			}

			#endregion

			#region Context
			
			private static void UpdateContext( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				// Consider making a successive click context
				if( FrameInputs.Current.successiveClicks < FrameInputs.Prior.successiveClicks )
				{
					Common.DebugMessage( $"CNTX: Ending drag because clicking stopped" );
					EndDrag( false );
					return;
				}
				else if( FrameInputs.Current.successiveClicks > 1 )
				{
					// Don't end the drag on successive clicks to not interfere with a double-click
					return;
				}
				else if( MouseContext != null )
				{
					switch( MouseContext.Think( CurrentDragState ) )
					{
						case AbstractInventoryGuiCursorContext.State.DoneValid:
						case AbstractInventoryGuiCursorContext.State.DoneInvalid:
						{
							// Lock the context until all mouse buttons are released. Contexts ought to do this instead.
							if( FrameInputs.Current.None )
								EndDrag( true );

							return;
						}
					}
				}
			}

			private static void EndDrag( bool clearContext )
			{
				PlayerButtons.ForEach( x => x.considerForDrag = false );
				ContainerButtons.ForEach( x => x.considerForDrag = false );

				if( clearContext )
				{
					MouseContext?.End();
					MouseContext = null;
				}
			}

			#endregion

			#region SingleDrop

			public static void UpdateSingleDrop( InventoryGrid m_playerGrid , InventoryGrid m_containerGrid )
			{
				if( MouseContext != null || CurrentDragState.isValid || FrameInputs.Current.Any )
				{
					return;
				}
				else if( !ZInput.GetKey( SingleDropKey.Value ) )
				{
					SingleDropCoolDown = false;
					return;
				}
				else if( SingleDropCoolDown )
					return;

				SingleDropCoolDown = true; // Try only once per press regardless of the outcome
				InventoryButton button = GetHoveredButton( m_playerGrid , m_containerGrid );
				if( button != null )
					DropOne( button.grid.GetInventory() , button.curItem );
			}

			public static bool DropOne( Inventory inv , ItemDrop.ItemData item )
			{
				if( MouseContext == null && Player.m_localPlayer.DropItem( inv , item , 1 ) )
				{
					Traverse.Create( InventoryGui.instance )
						.Field( "m_moveItemEffects" )
						.GetValue< EffectList >()
						.Create( InventoryGui.instance.transform.position , Quaternion.identity );

					return true;
				}

				return false;
			}

			#endregion

			#region Traverse

			// IMPORTANT IMPLEMENTATION DETAIL:
			// 1. This copies item into inventory
			// 2. This subtracts amount from item
			// 3. It does not remove item from the original inventory if the stack is empty
			public static bool AddItem( Inventory inventory , ItemDrop.ItemData item , int amount , int x , int y )
			{
				return Traverse.Create( inventory )
					.Method( "AddItem" , new[] { typeof( ItemDrop.ItemData ) , typeof( int ) , typeof( int ) , typeof( int ) } )
					.GetValue< bool >( item , amount , x , y );
			}

			public static Vector2i FindEmptySlot( Inventory inventory , bool topFirst )
			{
				return Traverse.Create( inventory )
					.Method( "FindEmptySlot" , new[] { typeof( bool ) } )
					.GetValue< Vector2i >( topFirst );
			}

			public static void SetupDragItem( InventoryGui instance , ItemDrop.ItemData item , Inventory inventory , int amount )
			{
				Traverse.Create( instance )
					.Method( "SetupDragItem" , new[] { typeof( ItemDrop.ItemData ) , typeof( Inventory ) , typeof( int ) } )
					.GetValue( item , inventory , amount );
			}

			public static bool TopFirst( Inventory inventory , ItemDrop.ItemData item )
			{
				// This probably shouldn't go here...
				// FIXME: Nor does it seem to work
				//switch( InsertionPriority.Value )
				//{
				//	case InsertionPriorityEnum.TopLeft:
				//		return true;
				//	case InsertionPriorityEnum.BottomLeft:
				//		return false;
				//}

				return Traverse.Create( inventory )
					.Method( "TopFirst" , new[] { typeof( ItemDrop.ItemData ) } )
					.GetValue< bool >( item );
			}
		
			public static void UpdateCraftingPanel( InventoryGui instance , bool focusView = false )
			{
				Traverse.Create( instance )
					.Method( "UpdateCraftingPanel" , new[] { typeof( bool ) } )
					.GetValue( focusView );
			}

			#endregion

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( Button ___m_dropButton )
			{
				___m_dropButton.gameObject.AddComponent< ButtonRightClickComponent >().OnRightClick.AddListener( delegate
				{
					if( !IsEnabled.Value || !AllowRightClickDrop.Value )
						return;

					// FIXME: Does not work if we drop outside the confines of the following RectTransform with SuperUltrawideSupport
					// _GameMain / LoadingGUI / PixelFix / Scaled 3D Viewport / IngameGui / Inventory_screen / root / dropButton
					// Should we track "invalid" right clicks as drop single? That doesn't solve the root problem.
					// Can we attach the right click listener to another HUD object?
					// TODO: If we wanted to be really cool, raytrace clicks to see if we interact with item stands.
					Common.DebugMessage( $"INFO: Trying to drop single item" );
					VanillaDragState dragState = new VanillaDragState();
					if( dragState.isValid && DropOne( dragState.dragInventory , dragState.dragItem ) && dragState.Decrement() )
					{
						dragState = new VanillaDragState();
						if( dragState.isValid )
							dragState.UpdateTooltip();
						else
							VanillaDragState.ClearDrag();
					}
				} );
			}

			[HarmonyPatch( "CloseContainer" )]
			[HarmonyPostfix]
			private static void CloseContainerPostfix()
			{
				// The player's inventory will remain open if the player moves far enough away from an open container
				MouseContext?.End();
				EndDrag( true );
				ContainerButtons.Clear();
			}

			[HarmonyPatch( "Hide" )]
			[HarmonyPostfix]
			private static void HidePostfix()
			{
				MouseContext?.End();
				VanillaDragState.ClearDrag();
				EndDrag( true );
				OnDestroyPrefix();
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix()
			{
				IgnoreUpdateItemDragRightMouseReset = false;
				SkipNextUpdate = true;
				SingleDropCoolDown = false;
				MouseContext = null;
				PlayerButtons.Clear();
				ContainerButtons.Clear();
				CurrentButton = null;
			}

			[HarmonyPatch( "OnRightClickItem" )]
			[HarmonyPrefix]
			private static bool OnRightClickItemPrefix(
				InventoryGui __instance,
				InventoryGrid grid,
				ItemDrop.ItemData item,
				Vector2i pos,
				InventoryGrid ___m_playerGrid,
				InventoryGrid ___m_containerGrid )
			{
				if( !IsEnabled.Value )
				{
					return true;
				}
				else if( VanillaDragState.IsValid() )
				{
					Common.DebugMessage( $"INFO: Start SingleSmearContext" );
					MouseContext = new SingleSmearContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
					return false; // Block all right-click logic while an item is on the cursor
				}
				else if( item == null || item.m_shared.m_maxStackSize == 1 )
				{
					// Fall-through. We can't defer to vanilla if InitialSwapShiftAndCtrl is false because we need to track contexts.
				}
				else if( InitialSwapShiftAndCtrl ? Common.AnyShift() : Common.AnyControl() )
				{
					int amount = Mathf.CeilToInt( item.m_stack / 2.0f );
					SetupDragItem( __instance , item , grid.GetInventory() , amount );

					Common.DebugMessage( $"INFO: Start BlockingMouseContext (RMB, split, immediate)" );
					MouseContext = new BlockingCursorContext( () => InitialSwapShiftAndCtrl ? Common.AnyShift() : Common.AnyControl() );
					return false;
				}
				else if( InitialSwapShiftAndCtrl ? Common.AnyControl() : Common.AnyShift() )
				{
					if( item.m_stack == 1 )
					{
						// Nothing to split. Do what vanilla does.
						SetupDragItem( __instance , item , grid.GetInventory() , 1 );
					}
					else
					{
						int amount = Mathf.CeilToInt( item.m_stack / 2.0f );
						__instance.m_splitSlider.value = amount;

						Traverse.Create( __instance )
							.Method( "ShowSplitDialog" , new[] { typeof( ItemDrop.ItemData ) , typeof( Inventory ) } )
							.GetValue( item , grid.GetInventory() );
						Traverse.Create( __instance )
							.Method( "OnSplitSliderChanged" , new[] { typeof( float ) } )
							.GetValue( amount );
					}

					Common.DebugMessage( $"INFO: Start BlockingMouseContext (RMB, split, dialog)" );
					MouseContext = new BlockingCursorContext( () => InitialSwapShiftAndCtrl ? Common.AnyControl() : Common.AnyShift() );
					return false;
				}

				Common.DebugMessage( $"CNTX: Start BlockingMouseContext (RMB)" );
				MouseContext = new BlockingCursorContext( null );
				return true;
			}

			[HarmonyPatch( "OnSelectedItem" )]
			[HarmonyPrefix]
			private static bool OnSelectedItemPrefix(
				InventoryGrid grid,
				ItemDrop.ItemData item,
				Vector2i pos,
				InventoryGrid.Modifier mod,
				InventoryGrid ___m_playerGrid,
				InventoryGrid ___m_containerGrid,
				out bool __state )
			{
				__state = false;

				if( !IsEnabled.Value )
				{
					return true;
				}
				else if( MouseContext != null )
				{
					if( MouseContext.CurrentState == AbstractInventoryGuiCursorContext.State.ActiveValid )
					{
						Common.DebugMessage( $"CNTX: Selecting something for {MouseContext.GetType()}" );
						return true;
					}

					return false;
				}
				else if( FrameInputs.Current.successiveClicks > 0 )
				{
					// Clicks lag behind by one here when this runs outside of our updates.
					// TODO: Should we start a double-click context here?
					Common.DebugMessage( $"DBLC: Excessive clicking" );
					return true;
				}
				else if( VanillaDragState.IsValid() )
				{
					// Allow the item to be put into the slot and invalidate the vanilla drag.
					// This can fail, so we check again in the postfix.
					__state = true;
					return true;
				}

				bool move = InitialSwapShiftAndCtrl ? Common.AnyShift() : Common.AnyControl();
				bool split = InitialSwapShiftAndCtrl ? Common.AnyControl() : Common.AnyShift();
				if( move && split )
				{
					Common.DebugMessage( $"CNTX: Start FilteredStackMoveContext" );
					MouseContext = new FilteredStackMoveContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
					return false;
				}
				else if( move )
				{
					Common.DebugMessage( $"CNTX: Start StackMoveContext" );
					MouseContext = new StackMoveContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
					return false;
				}
				else if( split )
				{
					Common.DebugMessage( $"CNTX: Start BlockingMouseContext (LMB, split)" );
					MouseContext = new BlockingCursorContext( () => InitialSwapShiftAndCtrl ? Common.AnyControl() : Common.AnyShift() );
					return true;
				}

				Common.DebugMessage( $"CNTX: Start BlockingMouseContext (LMB)" );
				MouseContext = new BlockingCursorContext( null );
				return true;
			}

			[HarmonyPatch( "OnSelectedItem" )]
			[HarmonyPostfix]
			private static void OnSelectedItemPostfix(
				InventoryGrid grid,
				ItemDrop.ItemData item,
				Vector2i pos,
				InventoryGrid.Modifier mod,
				InventoryGrid ___m_playerGrid,
				InventoryGrid ___m_containerGrid,
				ref bool __state )
			{
				// Because we require the item to be put down, we can't start a smear on an existing item.
				// But we can. But it would require some finagling and re-working of StackSmearContext.End().
				// TODO: Should we pick the item back up here and only actually put it down on button release?
				// TODO: Should we add long-press contexts? Like, long [LMB] would collect into the pressed item?
				// Along with another comment about nested contexts, what about contexts that can graduate into others?
				if( IsEnabled.Value && __state && !VanillaDragState.IsValid() )
				{
					InventoryButton button = GetHoveredButton( ___m_playerGrid , ___m_containerGrid );
					ItemDrop.ItemData hoveredItem = button?.curItem;
					if( hoveredItem == null || hoveredItem.m_stack <= 1 )
					{
						Common.DebugMessage( $"CNTX: Cannot smear a null, empty, or single item" );
						return;
					}

					Common.DebugMessage( $"CNTX: Start StackSmearContext" );
					MouseContext = new StackSmearContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
				}
			}

			[HarmonyPatch( "SetupDragItem" )]
			[HarmonyPrefix]
			private static bool SetupDragItemPrefix( ItemDrop.ItemData item , Inventory inventory , int amount )
			{
				if( IgnoreUpdateItemDragRightMouseReset )
				{
					IgnoreUpdateItemDragRightMouseReset = false;
					return !FrameInputs.Current.Right;
				}

				return true;
			}
			
			[HarmonyPatch( "ShowSplitDialog" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > ShowSplitDialogTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				return InitialSwapShiftAndCtrl ? Common.SwapShiftAndCtrl( instructionsIn ) : instructionsIn;
			}

			[HarmonyPatch( "Update" )]
			[HarmonyPostfix]
			private static void UpdatePostfix( int ___m_hiddenFrames , InventoryGrid ___m_playerGrid , InventoryGrid ___m_containerGrid )
			{
				if( ___m_hiddenFrames == 0 && !SkipNextUpdate )
					CollectInventoryButtons( ___m_playerGrid , ___m_containerGrid );

				SkipNextUpdate = false;
			}
			
			[HarmonyPatch( "UpdateItemDrag" )]
			[HarmonyPrefix]
			private static void UpdateItemDragPrefix(
				InventoryGui __instance,
				InventoryGrid ___m_playerGrid,
				InventoryGrid ___m_containerGrid )
			{
				// Tracking states even if the plugin is disabled is more correct, but the user is not expected
				// to perform frame-perfect actions to end up in an invalid state with stale information
				if( !IsEnabled.Value || ( PlayerButtons.Count == 0 && ContainerButtons.Count == 0 ) )
					return;

				FrameInputs.Update();
				LastDragState = CurrentDragState;
				CurrentDragState = new VanillaDragState();
				CurrentButton = GetHoveredButton( ___m_playerGrid , ___m_containerGrid );

				// Order is mildly important here to keep multiple things from happening on the same frame
				UpdateContext( ___m_playerGrid , ___m_containerGrid );
				UpdateDoubleClick( __instance , ___m_playerGrid , ___m_containerGrid );
				UpdateSingleDrop( ___m_playerGrid , ___m_containerGrid );
				UpdateMouseWheel( __instance , ___m_playerGrid , ___m_containerGrid );

				IgnoreUpdateItemDragRightMouseReset = CurrentDragState.isValid;
			}
		}
	}
}
