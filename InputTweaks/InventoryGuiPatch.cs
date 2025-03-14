using HarmonyLib;
using System;
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
			private static VanillaDragState CurrentDragState = new VanillaDragState();
			private static List< InventoryButton > PlayerButtons = new List< InventoryButton >();
			private static bool ForceContainerButtonUpdate = false;
			private static List< InventoryButton > ContainerButtons = new List< InventoryButton >();
			private static InventoryButton CurrentButton = null;
			private static Component LastWorldComponent = null;
			private static Component CurrentWorldComponent = null;
			private static GameObject WorldInteractionPreviewDragObject = null;

			#region Helpers

			private static void CollectInventoryButtons( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				// Valid player buttons prevent unnecessary attempts at caching container buttons.
				// With the addition of "world interactions", containers can be opened and closed at any time.
				bool standardInitialization = PlayerButtons.Count == 0 && ContainerButtons.Count == 0;

				if( standardInitialization )
				{
					Common.DebugMessage( $"INFO: Attempting to index PlayerButtons" );
					CollectInventoryButtons( playerGrid , PlayerButtons );
					Common.DebugMessage( $"INFO: Indexed {PlayerButtons.Count} player buttons" );
				}

				if( standardInitialization || ForceContainerButtonUpdate )
				{
					Common.DebugMessage( $"INFO: Attempting to index ContainerButtons" );
					CollectInventoryButtons( containerGrid , ContainerButtons );
					Common.DebugMessage( $"INFO: Indexed {ContainerButtons.Count} container buttons" );

					ForceContainerButtonUpdate = false;
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

			#region Context

			private static void UpdateContext( InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( MouseContext == null )
					return;

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
				{
					ItemDrop.ItemData item = button.curItem;
					PlayerDropFromInv(
						button.grid.GetInventory(),
						item,
						Common.CheckModifier( StackDropModifier.Value ) ? item.m_stack : 1 );
				}
			}

			private static bool PlayerDropFromInv( Inventory inv , ItemDrop.ItemData item , int count )
			{
				if( MouseContext == null && Player.m_localPlayer.DropItem( inv , item , count ) )
				{
					// Why are we doing this?
					Traverse.Create( InventoryGui.instance )
						.Field( "m_moveItemEffects" )
						.GetValue< EffectList >()
						.Create( InventoryGui.instance.transform.position , Quaternion.identity );

					return true;
				}

				return false;
			}

			#endregion

			#region MouseWheel

			private static void UpdateMouseWheel( InventoryGui inventoryGui , InventoryGrid playerGrid , InventoryGrid containerGrid )
			{
				if( MouseContext != null || CurrentDragState.isValid || FrameInputs.Current.Any )
					return;

				Player player = Player.m_localPlayer;
				float scroll = ZInput.GetMouseScrollWheel();
				if( player == null || scroll == 0.0f )
					return;

				GameObject splitPanel = inventoryGui.m_splitPanel.gameObject;
				if( splitPanel.activeInHierarchy )
				{
					// The split panel is the full size of the screen
					if( Common.IsCursorOver( inventoryGui.m_splitPanel.Find( "win_bkg" ) as RectTransform ) )
					{
						Slider splitSlider = inventoryGui.m_splitSlider;
						splitSlider.value = Mathf.Clamp(
							splitSlider.value + ( scroll > 0.0f ? 1.0f : -1.0f ),
							splitSlider.minValue,
							splitSlider.maxValue );

						Traverse.Create( inventoryGui )
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
				WheelActionEnum action = ( itemInContainer ? ContainerWheelAction : PlayerWheelAction ).Value;
				if( action == WheelActionEnum.None )
					return;

				bool pull = ( action == WheelActionEnum.PullUpPushDown ) == ( scroll > 0.0f );

				if( itemInContainer )
					MoveOne( player , pull , item , containerGrid , playerGrid );
				else if( inventoryGui.IsContainerOpen() )
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

						ItemDrop.ItemData partialItem = Common.FindLargestPartialStack( item , owningGrid );
						if( partialItem == null )
						{
							if( !owningInv.HaveEmptySlot() )
							{
								Common.DebugMessage( $"PULL: No space" );
								return;
							}
						}
						else
							item = partialItem;
					}

					// These are annoyingly similar
					if( otherInv != null )
					{
						ItemDrop.ItemData otherItem = item.m_shared.m_maxStackSize > 1
							? Common.EquivalentStackables( item , otherGrid ).FirstOrDefault()
							: Common.FindFirstSimilarItemInInventory( item , otherInv );

						if( otherItem == null && item.m_stack == item.m_shared.m_maxStackSize && item.m_shared.m_maxStackSize > 1 )
						{
							ItemDrop.ItemData otherPartialItem = Common.FindSmallestPartialStack( item , otherGrid );
							if( otherPartialItem != null && owningInv.AddItem( item.m_dropPrefab , 1 ) )
							{
								Common.DebugMessage( $"PULL: One item" );
								otherInv.RemoveOneItem( otherPartialItem );
								return;
							}
						}
						else if( otherItem == null || otherItem.m_stack < 1 )
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
							item = Common.FindSmallestPartialStack( item , owningGrid ) ?? item;

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
						ItemDrop.ItemData otherItem = Common.FindLargestPartialStack( item , owningGrid );
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

			#region WorldInteractions

			private static void UpdateWorldInteractionDragPreview( InventoryGui inventoryGui , Button dropButton )
			{
				// Cycle regardless so we don't hold stale data
				LastWorldComponent = CurrentWorldComponent;
				CurrentWorldComponent = null;

				VanillaDragState dragState = new VanillaDragState();
				if( dragState.dragObject != null && dragState.dragObject != WorldInteractionPreviewDragObject )
					return;

				// Interactables underneath GUI components should not be considered
				Transform dropButtonTransform = dropButton.transform;
				Transform rootTransform = dropButtonTransform.parent.transform;
				for( int index = 0 ; index < rootTransform.childCount ; index++ )
				{
					RectTransform child = rootTransform.GetChild( index ) as RectTransform;
					// TODO: What were we doing here? Was this a workaround for the SuperUltrawideSupport bug?
					//if( child == dropButtonTransform )
					//{
					//	if( !child.gameObject.activeInHierarchy || !Common.IsCursorOver( child ) )
					//	{
					//		ClearWorldInteractionDragPreview();
					//		return;
					//	}
					//}
					//else if( child.gameObject.activeInHierarchy && Common.IsCursorOver( child ) )
					if( child != dropButtonTransform && child.gameObject.activeInHierarchy && Common.IsCursorOver( child ) )
					{
						ClearWorldInteractionDragPreview();
						return;
					}
				}

				CurrentWorldComponent = RaycastComponentForWorldInteraction( out RaycastHit hit );
				if( CurrentWorldComponent != LastWorldComponent )
					VanillaDragState.ClearDrag();

				string prefabName = GetHoverPrefabName( CurrentWorldComponent , hit );
				if( prefabName == null )
				{
					ClearWorldInteractionDragPreview();
					return;
				}

				ItemDrop.ItemData item = ObjectDB.instance
					.GetItemPrefab( prefabName )
					?.GetComponent< ItemDrop >()
					?.m_itemData;

				if( item != null )
					SetWorldInteractionDragPreview( inventoryGui , item );
				else
					ClearWorldInteractionDragPreview();
			}

			private static Component RaycastComponentForWorldInteraction( out RaycastHit hit )
			{
				Ray cursorRay = GameCamera.instance.GetComponent< Camera >().ScreenPointToRay( Input.mousePosition );
				Player player = Player.m_localPlayer;
				int interactMask = Traverse.Create( player )
					.Field( "m_interactMask" )
					.GetValue< int >();

				// Ignore players, especially the player's own character. May ignore NPCs, too.
				if( !Physics.Raycast( cursorRay , out hit , 50.0f , interactMask & ~( 1 << 9 ) ) )
					return null;
				else if( Vector3.Distance( hit.point , player.m_eye.position ) > player.m_maxInteractDistance )
					return null;

				return hit.collider.GetComponentInParent< Component >();
			}

			private static string GetHoverPrefabName( Component component , RaycastHit hit )
			{
				if( component == null )
					return null;

				if( InteractWithArmorStands.Value )
				{
					ArmorStand armorStand = component.GetComponentInParent< ArmorStand >();
					if( armorStand != null )
					{
						ItemDrop.ItemData.ItemType searchItemType = ItemTypeForArmorStandPoint( hit );

						for( int index = 0 ; index < armorStand.m_slots.Count ; index++ )
						{
							ArmorStand.ArmorStandSlot slot = armorStand.m_slots[ index ];
							if( slot.m_supportedTypes.Count != 0 && !slot.m_supportedTypes.Contains( searchItemType ) )
								continue;
							else if( !armorStand.HaveAttachment( index ) )
								break;

							return Traverse.Create( armorStand )
								.Field( "m_nview" )
								.GetValue< ZNetView >()
								.GetZDO()
								.GetString( index + "_item" );
						}

						return null;
					}
				}

				if( InteractWithItemStands.Value )
				{
					ItemStand itemStand = component.GetComponentInParent< ItemStand >();
					if( itemStand != null )
					{
						return Traverse.Create( itemStand )
							.Field( "m_visualName" )
							.GetValue< string >();
					}
				}

				if( InteractWithWorldItems.Value )
				{
					ItemDrop item = component.GetComponentInParent< ItemDrop >();
					if( item != null )
						return item.m_itemData.m_dropPrefab.name;
				}

				return null;
			}

			private static void SetWorldInteractionDragPreview( InventoryGui inventoryGui , ItemDrop.ItemData item )
			{
				// We can't cache a hover-drag object because drag objects get destroyed when replaced
				// and calling SetupDragItem() every frame spams a clicking noise (m_moveItemEffects).
				VanillaDragState vanillaDragState = new VanillaDragState();
				if( vanillaDragState.dragObject != null && vanillaDragState.dragObject == WorldInteractionPreviewDragObject )
				{
					if( Common.ItemsAreSimilarButDistinct( vanillaDragState.dragItem , item , true ) )
						return;

					Traverse inventoryGuiTraversal = Traverse.Create( inventoryGui );
					inventoryGuiTraversal.Field( "m_dragInventory" ).SetValue( null );
					inventoryGuiTraversal.Field( "m_dragItem" ).SetValue( item );
					inventoryGuiTraversal.Field( "m_dragAmount" ).SetValue( 0 );
				}
				else
				{
					SetupDragItem( inventoryGui , item , null , 0 );
					WorldInteractionPreviewDragObject = ( new VanillaDragState() ).dragObject;
				}
			}

			private static void ClearWorldInteractionDragPreview()
			{
				if( WorldInteractionPreviewDragObject != null && ( new VanillaDragState() ).dragObject == WorldInteractionPreviewDragObject )
				{
					WorldInteractionPreviewDragObject = null;
					VanillaDragState.ClearDrag();
				}
			}

			private static void InteractWithArmorStand( RaycastHit hit , ArmorStand armorStand )
			{
				Player player = Player.m_localPlayer;
				VanillaDragState dragState = new VanillaDragState();

				if( dragState.isValid )
				{
					for( int index = 0 ; index < armorStand.m_slots.Count ; index++ )
					{
						ItemDrop.ItemData item = dragState.dragItem;
						ArmorStand.ArmorStandSlot slot = armorStand.m_slots[ index ];

						if( armorStand.HaveAttachment( index ) )
						{
							if( ArmorStandPatch.CanAttach( armorStand , slot , item ) )
							{
								ArmorStandPatch.RPC_DropItem( armorStand , index );
								DoInteractAnimation( Player.m_localPlayer , armorStand.gameObject );
								return;
							}
						}
						else if( slot.m_switch.UseItem( player , dragState.dragItem ) )
						{
							// Checking the pending item and slot is insufficient for some reason
							VanillaDragState.ClearDrag();
							DoInteractAnimation( Player.m_localPlayer , armorStand.gameObject );
							return;
						}
					}
				}
				else
				{
					ItemDrop.ItemData.ItemType itemType = ItemTypeForArmorStandPoint( hit );

					for( int index = 0 ; index < armorStand.m_slots.Count ; index++ )
					{
						ArmorStand.ArmorStandSlot slot = armorStand.m_slots[ index ];
						if( slot.m_supportedTypes.Count != 0 && !slot.m_supportedTypes.Contains( itemType ) )
							continue;

						if( armorStand.HaveAttachment( index ) )
						{
							ArmorStandPatch.RPC_DropItem( armorStand , index );
							DoInteractAnimation( Player.m_localPlayer , armorStand.gameObject );
						}

						return;
					}
				}
			}

			private static ItemDrop.ItemData.ItemType ItemTypeForArmorStandPoint( RaycastHit hit )
			{
				Bounds bounds = hit.collider.bounds;
				float colliderHeight = bounds.max.y - bounds.min.y;
				float collisionHeight = hit.point.y - bounds.min.y;
				float normalizedHeight = collisionHeight / colliderHeight;
				// TODO: Revisit this someday
				//Vector3 heightAdjustedHitPoint = hit.point;
				//heightAdjustedHitPoint.y = bounds.center.y;
				//float distanceFromAxis = ( bounds.center - heightAdjustedHitPoint ).magnitude;

				// TODO: Turn side-selection into a plugin option
				//if( distanceFromAxis < 0.24f ) // Reasonable value determined by experimentation
				{
					// Prefabs don't have a collider, bounds, or anything obvious that we can test,
					// so we estimate the item based on its position in a way that makes sufficient sense.
					// Even if we could, it would make it difficult to get items from the back of the stand.
					const float oneSixtheenth = 1.0f / 16.0f;

					if( normalizedHeight >= ( 15 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.Helmet;
					else if( normalizedHeight >= ( 14 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.Shoulder;
					else if( normalizedHeight >= ( 12 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.Chest;
					else if( normalizedHeight >= ( 10 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.Utility; // What, if not the belt?
					else if( normalizedHeight >= ( 8 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.OneHandedWeapon; // Matches the "weapon" slot
					else if( normalizedHeight >= ( 6 * oneSixtheenth ) )
						return ItemDrop.ItemData.ItemType.Shield;
					else
						return ItemDrop.ItemData.ItemType.Legs;
				}

				//return normalizedHeight >= 0.5f
				//	? ItemDrop.ItemData.ItemType.OneHandedWeapon
				//	: ItemDrop.ItemData.ItemType.Shield;
			}

			private static void InteractWithContainer( Container container , InventoryGui inventoryGui )
			{
				Container currentContainer = Traverse.Create( inventoryGui )
					.Field( "m_currentContainer" )
					.GetValue< Container >();
				Traverse.Create( inventoryGui )
					.Method( "CloseContainer" )
					.GetValue();

				if( container != currentContainer )
					container.Interact( Player.m_localPlayer , false , false );

				DoInteractAnimation( Player.m_localPlayer , container.gameObject );
			}

			private static void InteractWithItemStand( ItemStand itemStand )
			{
				Player player = Player.m_localPlayer;

				if( itemStand.HaveAttachment() )
				{
					if( itemStand.Interact( player , false , false ) )
						DoInteractAnimation( Player.m_localPlayer , itemStand.gameObject );

					return;
				}

				if( !PrivateArea.CheckAccess( itemStand.transform.position ) )
					return;

				VanillaDragState dragState = new VanillaDragState();
				if( !dragState.isValid )
					return;

				// ItemStand.UseItem() only returns false if the stand already has an item attached.
				// Otherwise, the item stand will remove the item from the player when the RPC runs.
				// We explicitly check for access first because we're bypassing ItemStand.Interact().
				itemStand.UseItem( player , dragState.dragItem );
				ItemDrop.ItemData queuedItem = Traverse.Create( itemStand )
					.Field( "m_queuedItem" )
					.GetValue< ItemDrop.ItemData >();

				if( dragState.dragItem == queuedItem && ( new VanillaDragState() ).Decrement() )
				{
					// Move into decrement/increment? This is duplicated code.
					dragState = new VanillaDragState();
					if( dragState.isValid )
						dragState.UpdateTooltip();
					else
						VanillaDragState.ClearDrag();

					DoInteractAnimation( Player.m_localPlayer , itemStand.gameObject );
				}
			}

			#endregion

			#region Traverse

			// IMPORTANT IMPLEMENTATION DETAILS:
			// 1. This copies item into inventory
			// 2. This subtracts amount from item
			// 3. It does not remove item from the original inventory if the stack is empty
			public static bool AddItem( Inventory inventory , ItemDrop.ItemData item , int amount , int x , int y )
			{
				return Traverse.Create( inventory )
					.Method( "AddItem" , new[] { typeof( ItemDrop.ItemData ) , typeof( int ) , typeof( int ) , typeof( int ) } )
					.GetValue< bool >( item , amount , x , y );
			}

			public static void DoInteractAnimation( Player player , GameObject gameObject )
			{
				Traverse.Create( player )
					.Method( "DoInteractAnimation" , new[] { typeof( GameObject ) } )
					.GetValue( gameObject );
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
					if( !IsEnabled.Value )
					{
						return;
					}
					else if( !AllowRightClickDrop.Value )
					{
						VanillaDragState.ClearDrag();
						return;
					}

					VanillaDragState dragState = new VanillaDragState();
					if( dragState.isValid && PlayerDropFromInv( dragState.dragInventory , dragState.dragItem , 1 ) && dragState.Decrement() )
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

			[HarmonyPatch( "OnDropOutside" )]
			[HarmonyPrefix]
			private static bool OnDropOutsidePrefix( InventoryGui __instance )
			{
				Component component = RaycastComponentForWorldInteraction( out RaycastHit hit );
				if( component == null )
					return true;

				if( InteractWithArmorStands.Value )
				{
					ArmorStand armorStand = component.GetComponentInParent< ArmorStand >();
					if( armorStand != null )
					{
						Common.DebugMessage( $"INTR: Hit ArmorStand" );
						InteractWithArmorStand( hit , armorStand );
						return false;
					}
				}

				if( InteractWithContainers.Value )
				{
					Container container = component.GetComponentInParent< Container >();
					if( container != null )
					{
						Common.DebugMessage( $"INTR: Hit Container" );
						InteractWithContainer( container , __instance );
						return false;
					}
				}

				if( InteractWithItemStands.Value )
				{
					ItemStand itemStand = component.GetComponentInParent< ItemStand >();
					if( itemStand != null )
					{
						Common.DebugMessage( $"INTR: Hit ItemStand" );
						InteractWithItemStand( itemStand );
						return false;
					}
				}

				if( InteractWithWorldItems.Value )
				{
					ItemDrop item = component.GetComponentInParent< ItemDrop >();
					if( item != null )
					{
						Common.DebugMessage( $"INTR: Hit ItemDrop" );
						Traverse.Create( Player.m_localPlayer )
							.Method( "Interact" , new[] { typeof( GameObject ) , typeof( bool ) , typeof( bool ) } )
							.GetValue( item.gameObject , false , false );
						return false;
					}
				}

				return true;
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
					return true;

				FrameInputs.Update();

				if( VanillaDragState.IsValid() )
				{
					Common.DebugMessage( $"INFO: Start SingleSmearContext" );
					MouseContext = new SingleSmearContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
					return false; // Block all right-click logic while an item is on the cursor
				}
				else if( item == null || item.m_shared.m_maxStackSize == 1 )
				{
					// Fall-through
				}
				else if( Common.CheckModifier( ModifierKeyEnum.Move ) )
				{
					int amount = SplitRoundsUp.Value
						? Mathf.CeilToInt( item.m_stack / 2.0f )
						: Mathf.Max( 1 , Mathf.FloorToInt( item.m_stack / 2.0f ) );
					SetupDragItem( __instance , item , grid.GetInventory() , amount );

					Common.DebugMessage( $"INFO: Start BlockingMouseContext (RMB, split, immediate)" );
					MouseContext = new BlockingCursorContext( () => Common.CheckModifier( ModifierKeyEnum.Move ) );
					return false;
				}
				else if( Common.CheckModifier( ModifierKeyEnum.Split ) )
				{
					// Consider pushing stuff like this into a one-shot context to keep business logic out of this method
					if( item.m_stack == 1 )
					{
						// Nothing to split. Do what vanilla does.
						SetupDragItem( __instance , item , grid.GetInventory() , 1 );
					}
					else
					{
						Traverse.Create( __instance )
							.Method( "ShowSplitDialog" , new[] { typeof( ItemDrop.ItemData ) , typeof( Inventory ) } )
							.GetValue( item , grid.GetInventory() );

						int amount = SplitRoundsUp.Value ? Mathf.CeilToInt( item.m_stack / 2.0f ) : Mathf.FloorToInt( item.m_stack / 2.0f );
						__instance.m_splitSlider.value = amount;

						Traverse.Create( __instance )
							.Method( "OnSplitSliderChanged" , new[] { typeof( float ) } )
							.GetValue( amount );
					}

					Common.DebugMessage( $"INFO: Start BlockingMouseContext (RMB, split, dialog)" );
					MouseContext = new BlockingCursorContext( () => Common.CheckModifier( ModifierKeyEnum.Split ) );
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
				out Tuple< Inventory , Vector2i > __state )
			{
				__state = null;

				if( !IsEnabled.Value )
					return true;

				FrameInputs.Update();

				if( MouseContext != null )
				{
					if( MouseContext.CurrentState == AbstractInventoryGuiCursorContext.State.ActiveValid )
					{
						Common.DebugMessage( $"CNTX: Selecting something for {MouseContext.GetType()}" );
						return true;
					}

					return false;
				}
				else if( FrameInputs.Current.successiveClicks > 1 )
				{
					if( AllowDoubleClickCollect.Value )
					{
						Common.DebugMessage( $"CNTX: Start MultiClickContext" );
						MouseContext = new MultiClickContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
						return true;
					}
					else
					{
						Common.DebugMessage( $"CNTX: Start BlockingMouseContext (LMB, collect)" );
						MouseContext = new BlockingCursorContext( null );
						return true;
					}
				}

				VanillaDragState dragState = new VanillaDragState();
				if( dragState.isValid )
				{
					// Allow the item to be put into the slot and invalidate the vanilla drag.
					// This can fail, so we have to handle this case in the postfix.
					__state = new Tuple< Inventory , Vector2i >( dragState.dragInventory , dragState.dragItem.m_gridPos );
					return true;
				}

				bool move = Common.CheckModifier( ModifierKeyEnum.Move );
				bool split = Common.CheckModifier( ModifierKeyEnum.Split );
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
					MouseContext = new BlockingCursorContext( () => Common.CheckModifier( ModifierKeyEnum.Split ) );
					return true;
				}

				Common.DebugMessage( $"CNTX: Start BlockingMouseContext (LMB)" );
				MouseContext = new BlockingCursorContext( null );
				return true;
			}

			[HarmonyPatch( "OnSelectedItem" )]
			[HarmonyPostfix]
			private static void OnSelectedItemPostfix(
				InventoryGui __instance,
				InventoryGrid grid,
				ItemDrop.ItemData item,
				Vector2i pos,
				InventoryGrid.Modifier mod,
				InventoryGrid ___m_playerGrid,
				InventoryGrid ___m_containerGrid,
				ref Tuple< Inventory , Vector2i > __state )
			{
				if( !IsEnabled.Value )
					return;

				if( MouseContext is BlockingCursorContext )
				{
					if( Common.CheckModifier( ModifierKeyEnum.Split ) )
					{
						if( !SplitRoundsUp.Value )
						{
							int amount = Mathf.Max( 1 , Mathf.FloorToInt( item.m_stack / 2.0f ) );
							__instance.m_splitSlider.value = amount;

							Traverse.Create( __instance )
								.Method( "OnSplitSliderChanged" , new[] { typeof( float ) } )
								.GetValue( amount );
						}
					}
					else if( Common.CheckModifier( ModifierKeyEnum.None ) )
					{
						Common.DebugMessage( $"CNTX: Start StackCollectContext" );
						MouseContext = new StackCollectContext( ___m_playerGrid , PlayerButtons , ___m_containerGrid , ContainerButtons );
					}
				}
				else if( __state != null )
				{
					if( VanillaDragState.IsValid() )
						return;

					// Because we require the item to be put down, we can't start a smear on an existing item.
					// But we can. But it would require some finagling and re-working of StackSmearContext.End().
					// TODO: Should we pick the item back up here and only actually put it down on button release?
					// TODO: Should we add long-press contexts? Like, long [LMB] would collect into the pressed item?
					// Along with another comment about nested contexts, what about contexts that graduate/decay into others?

					InventoryButton button = GetHoveredButton( ___m_playerGrid , ___m_containerGrid );
					ItemDrop.ItemData hoveredItem = button?.curItem;
					if( hoveredItem == null || hoveredItem.m_stack <= 0 )
					{
						Common.DebugMessage( $"CNTX: Cannot smear a null or empty item" );
						return;
					}

					Common.DebugMessage( $"CNTX: Start StackSmearContext" );
					MouseContext = new StackSmearContext(
						___m_playerGrid,
						PlayerButtons,
						___m_containerGrid,
						ContainerButtons,
						__state.Item1,
						__state.Item2 );
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

			[HarmonyPatch( "Show" )]
			[HarmonyPostfix]
			private static void ShowPostfix( Container container , int activeGroup = 1 )
			{
				ForceContainerButtonUpdate = true;
			}

			[HarmonyPatch( "ShowSplitDialog" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > ShowSplitDialogTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				return InitialSwapMoveAndSplit ? Common.SwapShiftAndCtrl( instructionsIn ) : instructionsIn;
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
				InventoryGrid ___m_containerGrid,
				Button ___m_dropButton )
			{
				// Tracking states even if the plugin is disabled is more correct, but the user is not expected
				// to perform frame-perfect actions to end up in an invalid state with stale information
				if( !IsEnabled.Value || ( PlayerButtons.Count == 0 && ContainerButtons.Count == 0 ) )
					return;

				FrameInputs.Update();
				CurrentDragState = new VanillaDragState();
				CurrentButton = GetHoveredButton( ___m_playerGrid , ___m_containerGrid );

				// Order is mildly important here to keep multiple things from happening on the same frame
				UpdateContext( ___m_playerGrid , ___m_containerGrid );
				UpdateSingleDrop( ___m_playerGrid , ___m_containerGrid );
				UpdateMouseWheel( __instance , ___m_playerGrid , ___m_containerGrid );
				UpdateWorldInteractionDragPreview( __instance , ___m_dropButton );

				// FIXME: It's probably not safe to cache the stateful version of VanillaDragState
				IgnoreUpdateItemDragRightMouseReset = CurrentDragState.isValid;
			}
		}
	}
}
