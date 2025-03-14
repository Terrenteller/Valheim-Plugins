InputTweaks is a Valheim clone of Mouse Wheelie, a re-make of InventoryTweaks, and a dash of default Minecraft controls.

Enabled by default (or hardcoded):
- Move and split actions are swapped so that [SHIFT] is move and [CTRL] is split
- [SHIFT] + [LMB] moves/drops items under the cursor
	- [CTRL], in addition, will filter to items similar to the first (on drag)
- [CTRL] + [LMB|RMB] opens the split dialog for the item under the cursor
- [LMB] drag will collect/divide similar items from/into applicable stacks/slots
- [RMB] drag will move one item from the cursor into applicable slots
- [LMB] double-click on an item will collect similar items from other stacks
- [RMB] with an item on the cursor outside of the inventory will drop a single item
- [Q] will drop a single item from the stack beneath the cursor
	- This conflicts with auto-run out-of-the-box
	- [SHIFT], in addition, will drop the entire stack
- Mouse scroll will pull/push items under the cursor and increment/decrement the split dialog
- [LMB] will put/take items on/from item/armor stands
- [LMB] will open/close containers
- [F3] will toggle the HUD
	- [CTRL] + [F3] is the game default and is not changed
	- [F1] would conflict with configuration managers

Disabled by default:
- Swapped items stay selected (à la Minecraft)
- Stack splits round down
- Unbalanced stack smear remainders stay selected

Other plugins that deal with the inventory and modifier keys may require configuration to be compatible with InputTweaks, such as SmartContainers' route-on-move feature. Please check configuration options if you encounter input conflicts.

## Changelog

1.1.2

- Update for 0.220.3 (Bog Witch)
- Player characters no longer block world interactions
- Allow items in the world to be picked up from the inventory GUI
- Play interaction animations when appropriate
- Default HUD toggle key to F3 to align with the vanilla default

1.1.1

- Add the ability to interact with armor stands, item stands, and containers from the inventory GUI
- Add a customizable HUD toggle key
- Make mouse wheel behaviour more consistent when full stacks are involved
- Fix single smear drags not starting on their own full stack

1.1.0

- Initial release
