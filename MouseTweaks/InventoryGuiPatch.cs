using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MouseTweaks
{
	public partial class MouseTweaks
	{
		[HarmonyPatch( typeof( InventoryGui ) )]
		public class InventoryGuiPatch
		{
			// Used to skip a very specific sequence that causes RMB to reset the drag
			private static bool IgnoreUpdateItemDragRightMouseReset = false;
			private static IInventoryGuiMouseContext MouseContext = null;
			private static MousePressContextEnum MousePressContext;
			private static VanillaDragState LastDragState = null;
			private static VanillaDragState CurrentDragState = new VanillaDragState();
			private static List< InventoryButton > PlayerButtons = new List< InventoryButton >();
			private static List< InventoryButton > ContainerButtons = new List< InventoryButton >();
			private static InventoryButton CurrentButton = null;
			private static bool DroppedOne = false;

			private enum MousePressContextEnum
			{
				None,
				Invalid,
				Select,
				Left,
				// LeftSameStackMove? (ctrl drag)
				LeftStackMove,
				LeftSplit,
				Right,
				RightSplitDialog,
				RightSplitImmediate
			}

			#region Helpers
			
			private static void CollectInventoryButtons( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( PlayerButtons.Count == 0 )
				{
					CollectInventoryButtons( playerGrid , PlayerButtons );
					System.Console.WriteLine( $"INFO: Indexed {PlayerButtons.Count} player buttons" );
				}

				if( ContainerButtons.Count == 0 )
				{
					CollectInventoryButtons( containerGrid , ContainerButtons );
					System.Console.WriteLine( $"INFO: Indexed {ContainerButtons.Count} container buttons" );
				}
			}

			private static void CollectInventoryButtons( InventoryGrid grid , List< InventoryButton > buttons )
			{
				buttons.Clear();
				if( grid == null )
					return;

				Inventory inv = grid.GetInventory();
				UIInputHandler[] handlers = grid.GetComponentsInChildren< UIInputHandler >();
				buttons.Resize( handlers.Length );
				int width = InventoryGridPatch.Width( grid );

				// TODO: This news up a bunch of stuff every time. Use a pool.
				foreach( UIInputHandler button in handlers )
				{
					Vector2i gridPos = InventoryGridPatch.GetButtonPos( grid , button );
					ItemDrop.ItemData otherItem = inv.GetItemAt( gridPos.x , gridPos.y );
					int gridIndex = gridPos.x + ( gridPos.y * width );
					buttons[ gridIndex ] = new InventoryButton
					{
						button = button,
						grid = grid,
						gridPos = gridPos,
						gridIndex = gridIndex, // FIXME: This doesn't appear to be the actual index...
						considerForDrag = true,
						representsExistingItem = otherItem != null
					};
				}
			}

			internal static InventoryButton GetHoveredButton( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( playerGrid != null && playerGrid.gameObject.activeInHierarchy && Common.IsCursorOver( playerGrid.gameObject ) )
					return PlayerButtons.Where( x => x != null && Common.IsCursorOver( x.button.gameObject ) ).FirstOrDefault();
				else if( containerGrid != null && containerGrid.gameObject.activeInHierarchy && Common.IsCursorOver( containerGrid.gameObject ) )
					return ContainerButtons.Where( x => x != null && Common.IsCursorOver( x.button.gameObject ) ).FirstOrDefault();

				return null;
			}

			#endregion

			#region MouseWheel

			private static void UpdateMouseWheel( InventoryGui instance , InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( MouseContext != null || MousePressContext != MousePressContextEnum.None )
					return;

				Player player = Player.m_localPlayer;
				float scroll = ZInput.GetMouseScrollWheel();
				if( player == null || scroll == 0.0f || CurrentButton == null || CurrentDragState.isValid )
					return;

				Inventory inv = CurrentButton.grid.GetInventory();
				Vector2i gridPos = CurrentButton.gridPos;
				ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
				if( item == null || item.m_stack < 1 )
					return;

				bool itemInContainer = CurrentButton.grid == containerGrid;
				bool wheelUp = scroll > 0.0f;
				bool pull = itemInContainer
					? ( ContainerWheelAction.Value == WheelActionEnum.PullUpPushDown ) == wheelUp
					: ( PlayerWheelAction.Value == WheelActionEnum.PullUpPushDown ) == wheelUp;

				if( itemInContainer )
					MoveOne( player , pull , item , containerGrid , playerGrid );
				else if( instance.IsContainerOpen() )
					MoveOne( player , pull , item , playerGrid , containerGrid );
				else
					MoveOne( player , pull , item , playerGrid , null );
			}

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
							System.Console.WriteLine( $"PULL: Doesn't make sense" );
							return;
						}
						else if( !owningInv.HaveEmptySlot() )
						{
							System.Console.WriteLine( $"PULL: No empty slots" );
							return;
						}
					}
					else if( item.m_stack == item.m_shared.m_maxStackSize )
					{
						if( otherInv == null )
						{
							System.Console.WriteLine( $"PULL: Doesn't make sense" );
							return;
						}
						else if( !owningInv.HaveEmptySlot() && Common.FindPartialStack( item , owningInv , false ) == null )
						{
							System.Console.WriteLine( $"PULL: No space" );
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
							System.Console.WriteLine( $"PULL: No similar item" );
						}
						else if( otherItem == item ) // Actual programming error
						{
							System.Console.WriteLine( $"PULL: Found self" );
							return;
						}
						else if( otherItem.m_shared.m_maxStackSize == 1 )
						{
							System.Console.WriteLine( $"PULL: Single" );
							owningInv.MoveItemToThis( otherInv , otherItem );
							return;
						}
						else if( otherInv.RemoveOneItem( otherItem ) )
						{
							System.Console.WriteLine( $"PULL: One item" );
							item.m_stack++;
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
							System.Console.WriteLine( $"PULL: No similar item" );
							return;
						}
						else if( otherItem == item ) // Actual programming error
						{
							System.Console.WriteLine( $"PULL: Found self" );
							return;
						}
						else if( owningInv.RemoveOneItem( otherItem ) )
						{
							System.Console.WriteLine( $"PULL: One item" );
							item.m_stack++;
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
							System.Console.WriteLine( $"PUSH: Single or last" );
							player.RemoveEquipAction( item );
							player.UnequipItem( item );
							otherInv.MoveItemToThis( owningInv , item ); // ???: WHAT IF IT FAILS?
							UITooltip.HideTooltip();
							return;
						}

						if( item.m_stack == item.m_shared.m_maxStackSize )
							item = Common.FindPartialStack( item , owningInv , false ) ?? item;

						if( otherInv.AddItem( item.m_dropPrefab , 1 ) )
						{
							System.Console.WriteLine( $"PUSH: One item" );
							owningInv.RemoveOneItem( item );
							return;
						}

						// TODO: Should we try to push to the same inventory if these fail?
					}
					else // Push to self
					{
						ItemDrop.ItemData otherItem = Common.FindPartialStack( item , owningInv , false );
						if( otherItem != null )
						{
							System.Console.WriteLine( $"PUSH: One item to sibling" );
							otherItem.m_stack++;
							owningInv.RemoveOneItem( item );
							if( item.m_stack < 1 )
								UITooltip.HideTooltip();

							return;
						}
						else if( item.m_stack == 1 ) // Only invalid if there's not a partial stack
						{
							System.Console.WriteLine( $"PUSH: Doesn't make sense" );
							return;
						}

						Vector2i emptySlot = FindEmptySlot( owningInv , TopFirst( owningInv , item ) );
						if( emptySlot.x < 0 || emptySlot.y < 0 )
						{
							System.Console.WriteLine( $"PUSH: No empty slot" );
							return;
						}
						else
						{
							System.Console.WriteLine( $"PUSH: One item to empty slot" );
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
				if( !( FrameInputs.Current.isDoubleClick && LastDragState.isValid && !CurrentDragState.isValid && CurrentButton != null ) )
					return;

				InventoryGrid grid = CurrentButton.grid;
				Inventory inv = grid.GetInventory();
				Vector2i gridPos = CurrentButton.gridPos;
				ItemDrop.ItemData item = inv.GetItemAt( gridPos.x , gridPos.y );
				if( item == null || item.m_stack >= item.m_shared.m_maxStackSize )
					return;

				// Prioritize the grid the item is in
				if( CollectFromInto( grid , item ) )
					CollectFromInto( grid == playerGrid ? containerGrid : playerGrid , item );

				// Pick the item back up
				SetupDragItem( instance , item , inv , item.m_stack );
			}

			private static bool CollectFromInto( InventoryGrid grid , ItemDrop.ItemData item )
			{
				List< ItemDrop.ItemData > equivalentStackables = Common.EquivalentStackables( item , grid ).ToList();
				System.Console.WriteLine( $"DBLC: Collecting {item} from {equivalentStackables.Count} equivalent stacks" );
				Inventory inv = grid.GetInventory();

				foreach( ItemDrop.ItemData equivalent in equivalentStackables )
				{
					int transfer = Mathf.Min( item.m_shared.m_maxStackSize - item.m_stack , equivalent.m_stack );
					System.Console.WriteLine( $"DBLC: Moving {transfer}" );
					if( inv.RemoveItem( equivalent , transfer ) )
						item.m_stack += transfer;
				}

				return item.m_stack < item.m_shared.m_maxStackSize;
			}

			#endregion

			#region Drag
			
			private static void UpdateDrag( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( FrameInputs.Current.successiveClicks < FrameInputs.Prior.successiveClicks )
				{
					System.Console.WriteLine( $"DRAG: Ending drag because clicking stopped" );
					EndDrag( false );
					return;
				}
				else if( FrameInputs.Current.successiveClicks > 1 )
				{
					// Don't end the drag on successive clicks to not interfere with a double-click
					if( MousePressContext != MousePressContextEnum.None )
					{
						System.Console.WriteLine( $"DRAG: Setting Invalid" );
						MousePressContext = MousePressContextEnum.Invalid;
					}

					return;
				}

				// TODO: If Done or Invalid, should the context be the thing that finalizes itself?
				if( MouseContext is BlockingLeftMouseContext || MouseContext is BlockingRightMouseContext )
				{
					switch( MouseContext.Think( CurrentDragState ) )
					{
						case IInventoryGuiMouseContext.State.NoChange:
						case IInventoryGuiMouseContext.State.OK:
							return;
						case IInventoryGuiMouseContext.State.Done:
							MouseContext.End();
							EndDrag( false );
							MouseContext = null;
							return;
						case IInventoryGuiMouseContext.State.Invalid:
							// We need to block somehow when a context becomes invalid until it can be freed
							MouseContext.End();
							EndDrag( true );
							MouseContext = null;
							return;
					}
				}
				else if( MouseContext is InventoryButtonTrackingContext )
				{
					switch( MouseContext.Think( CurrentDragState ) )
					{
						case IInventoryGuiMouseContext.State.NoChange:
						case IInventoryGuiMouseContext.State.OK:
							return;
						case IInventoryGuiMouseContext.State.Done:
							MouseContext.End();
							EndDrag( true );
							MouseContext = null;
							return;
						case IInventoryGuiMouseContext.State.Invalid:
							//MouseContext.End(); // Do not call... FIXME: Check for invalid state
							EndDrag( true );
							MouseContext = null;
							return;
					}
				}

				MousePressContext = MousePressContextEnum.None;
			}

			private static void EndDrag( bool clearDrag )
			{
				System.Console.WriteLine( $"DRAG: Ending {MousePressContext}" );

				PlayerButtons.ForEach( x => x.considerForDrag = false );
				ContainerButtons.ForEach( x => x.considerForDrag = false );
				MousePressContext = MousePressContextEnum.None;
				//MouseContext = null;

				if( clearDrag )
					VanillaDragState.ClearDrag();
			}

			#endregion

			#region DropOne

			public static void UpdateDropOne( InventoryGrid m_playerGrid , InventoryGrid m_containerGrid )
			{
				if( !IsEnabled.Value
					|| MouseContext != null
					|| MousePressContext != MousePressContextEnum.None
					|| CurrentDragState.isValid
					|| FrameInputs.Current.Any )
				{
					return;
				}
				else if( !ZInput.GetKey( DropOneKey.Value ) )
				{
					DroppedOne = false;
					return;
				}
				else if( DroppedOne )
					return;

				DroppedOne = true; // Even if the actual drop fails
				InventoryButton button = GetHoveredButton( m_playerGrid , m_containerGrid );
				if( button != null )
					DropOne( button.grid.GetInventory() , button.curItem );
			}

			public static bool DropOne( Inventory inv , ItemDrop.ItemData item )
			{
				if( !IsEnabled.Value || MouseContext != null || MousePressContext != MousePressContextEnum.None )
				{
					return false;
				}
				else if( Player.m_localPlayer.DropItem( inv , item , 1 ) )
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
				___m_dropButton.gameObject.AddComponent< RightClickButtonComponent >().OnRightClick.AddListener( delegate
				{
					if( !IsEnabled.Value || !DropOneRightClick.Value )
						return;

					// FIXME: Does not work if we drop outside the confines of the following RectTransform with SuperUltrawideSupport
					// _GameMain / LoadingGUI / PixelFix / Scaled 3D Viewport / IngameGui / Inventory_screen / root / dropButton
					// Should we track "invalid" right clicks as drop single? That doesn't solve the root problem.
					// TODO: If we wanted to be really cool, raytrace clicks to see if we interact with item stands.
					System.Console.WriteLine( $"INFO: Trying to drop single item" );
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
				EndDrag( true );
				PlayerButtons.Clear();
				ContainerButtons.Clear();
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
					System.Console.WriteLine( $"INFO: Setting Right" );
					CollectInventoryButtons( ___m_playerGrid , ___m_containerGrid );
					MousePressContext = MousePressContextEnum.Right;
					MouseContext = new RightDragContext( __instance , ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons , null );
					return false; // Block all right-click logic while an item is on the cursor
				}
				else if( item == null || item.m_shared.m_maxStackSize == 1 )
				{
					return true; // No special handling
				}
				else if( Common.AnyShift() )
				{
					int amount = Mathf.CeilToInt( item.m_stack / 2.0f );
					SetupDragItem( __instance , item , grid.GetInventory() , amount );

					System.Console.WriteLine( $"INFO: Setting RightSplitImmediate" );
					MousePressContext = MousePressContextEnum.RightSplitImmediate;
					MouseContext = new BlockingRightMouseContext();
					return false;
				}
				else if( Common.AnyControl() )
				{
					if( item.m_stack == 1 )
					{
						// Nothing to split. Mirrors vanilla behaviour.
						SetupDragItem( __instance , item , grid.GetInventory() , 1 );
						MousePressContext = MousePressContextEnum.RightSplitImmediate;
						MouseContext = new BlockingRightMouseContext();
						return false;
					}

					int amount = Mathf.CeilToInt( item.m_stack / 2.0f );
					__instance.m_splitSlider.value = amount;

					Traverse.Create( __instance )
						.Method( "ShowSplitDialog" , new[] { typeof( ItemDrop.ItemData ) , typeof( Inventory ) } )
						.GetValue( item , grid.GetInventory() );
					Traverse.Create( __instance )
						.Method( "OnSplitSliderChanged" , new[] { typeof( float ) } )
						.GetValue( amount );

					System.Console.WriteLine( $"INFO: Setting RightSplitDialog" );
					MousePressContext = MousePressContextEnum.RightSplitDialog;
					MouseContext = new BlockingRightMouseContext();
					return false;
				}

				return true; // No special handling
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
				if( FrameInputs.Current.successiveClicks > 0 )
				{
					// TODO: Should we start a double-click context here?
					System.Console.WriteLine( $"INFO: Excessive clicking" );
					return true;
				}
				else if( MousePressContext == MousePressContextEnum.LeftStackMove || MouseContext is ShiftLeftDragContext )
				{
					// FIXME: We shouldn't know this context is special
					System.Console.WriteLine( $"INFO: Allowing LeftMove" );
					return true;
				}
				else if( MouseContext != null )
				{
					return false;
				}
				else if( MousePressContext != MousePressContextEnum.None )
				{
					// TODO: Determine if this should come first...
					// TODO: This should bail if we have some other active context.
					System.Console.WriteLine( $"INFO: Setting Invalid (was {MousePressContext})" );
					MousePressContext = MousePressContextEnum.Invalid;
					return true;
				}
				else if( VanillaDragState.IsValid() )
				{
					// Allow the item to be put into the slot and invalidate the vanilla drag.
					// This may fail, so we check again in the postfix.
					System.Console.WriteLine( $"INFO: Setting Left" );
					MousePressContext = MousePressContextEnum.Left;
					__state = true;
					return true;
				}
				else if( Common.AnyShift() )
				{
					// We'll call this method shortly and indirectly from our drag logic
					System.Console.WriteLine( $"INFO: Setting LeftStackMove" );
					CollectInventoryButtons( ___m_playerGrid , ___m_containerGrid );
					MousePressContext = MousePressContextEnum.LeftStackMove;
					MouseContext = new ShiftLeftDragContext( null , ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons , null );
					return false;
				}
				else if( Common.AnyControl() )
				{
					System.Console.WriteLine( $"INFO: Setting LeftSplit" );
					MousePressContext = MousePressContextEnum.LeftSplit;
					MouseContext = new BlockingLeftMouseContext();
					return true;
				}

				System.Console.WriteLine( $"INFO: Setting Select" );
				MousePressContext = MousePressContextEnum.Select;
				MouseContext = new BlockingLeftMouseContext();
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
				if( !IsEnabled.Value || !__state )
				{
					return;
				}
				else if( VanillaDragState.IsValid() )
				{
					// We were not able to put down the entire stack for some reason
					System.Console.WriteLine( $"INFO: Setting Left - SYKE" );
					MousePressContext = MousePressContextEnum.None;
					return;
				}

				CollectInventoryButtons( ___m_playerGrid , ___m_containerGrid );
				MouseContext = new LeftDragContext( null , ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons , null );
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
				return Common.SwapShiftAndCtrl( instructionsIn );
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

				UpdateMouseWheel( __instance , ___m_playerGrid , ___m_containerGrid );
				UpdateDoubleClick( __instance , ___m_playerGrid , ___m_containerGrid );
				UpdateDrag( ___m_playerGrid , ___m_containerGrid );
				UpdateDropOne( ___m_playerGrid , ___m_containerGrid );

				IgnoreUpdateItemDragRightMouseReset = CurrentDragState.isValid;
			}
		}
	}
}
