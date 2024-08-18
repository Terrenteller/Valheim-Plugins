using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace InputTweaks
{
	public class Common
	{
		public const int LeftControlInt = (int)KeyCode.LeftControl;
		public const int RightControlInt = (int)KeyCode.RightControl;
		public const int LeftShiftInt = (int)KeyCode.LeftShift;
		public const int RightShiftInt = (int)KeyCode.RightShift;

		public static bool AnyAlt()
		{
			return ZInput.GetKey( KeyCode.LeftAlt ) || ZInput.GetKey( KeyCode.RightAlt );
		}

		public static bool AnyCommand()
		{
			return ZInput.GetKey( KeyCode.LeftCommand ) || ZInput.GetKey( KeyCode.RightCommand );
		}
		
		public static bool AnyCtrl()
		{
			return ZInput.GetKey( KeyCode.LeftControl ) || ZInput.GetKey( KeyCode.RightControl );
		}
		
		public static bool AnyShift()
		{
			return ZInput.GetKey( KeyCode.LeftShift ) || ZInput.GetKey( KeyCode.RightShift );
		}

		public static bool CanStackOnto( ItemDrop.ItemData item , ItemDrop.ItemData target )
		{
			return ItemsAreSimilarButDistinct( item , target , true )
				&& target.m_shared.m_maxStackSize > 1
				&& target.m_stack < target.m_shared.m_maxStackSize;
		}

		public static bool CheckModifier( InputTweaks.ModifierKeyEnum modifier )
		{
			switch( modifier )
			{
				case InputTweaks.ModifierKeyEnum.Alt:
					return AnyAlt();
				case InputTweaks.ModifierKeyEnum.Command:
					return AnyCommand();
				case InputTweaks.ModifierKeyEnum.Ctrl:
					return AnyCtrl();
				case InputTweaks.ModifierKeyEnum.Move:
					return InputTweaks.InitialSwapMoveAndSplit ? AnyShift() : AnyCtrl();
				case InputTweaks.ModifierKeyEnum.Split:
					return InputTweaks.InitialSwapMoveAndSplit ? AnyCtrl() : AnyShift();
				case InputTweaks.ModifierKeyEnum.Shift:
					return AnyShift();
			}

			return !AnyAlt() && !AnyCommand() && !AnyCtrl() && !AnyShift();
		}

		public static void DebugMessage( string message )
		{
			if( InputTweaks.DebugMessages.Value )
				Debug.Log( message );
		}

		public static List< ItemDrop.ItemData > EquivalentStackables( ItemDrop.ItemData item , InventoryGrid grid )
		{
			int width = InputTweaks.InventoryGridPatch.Width( grid );
			Inventory inv = grid.GetInventory();

			return inv.GetAllItems()
			   .Where( x => CanStackOnto( x , item ) )
			   .OrderBy( x => x.m_stack )
			   .ThenBy( x => x.m_gridPos.x + ( x.m_gridPos.y * width ) )
			   .ToList();
		}

		public static ItemDrop.ItemData FindFirstSimilarItemInInventory( ItemDrop.ItemData item , Inventory inv )
		{
			// Some vanilla similarity checks check ItemDrop.ItemData.m_worldLevel. What is that?
			foreach( ItemDrop.ItemData invItem in inv.GetAllItems() )
				if( item != invItem && item.m_shared.m_name.Equals( invItem.m_shared.m_name ) && item.m_quality == invItem.m_quality )
					return invItem;

			// Try again without the quality check if the item is not stackable
			if( item.m_shared.m_maxStackSize == 1 )
				foreach( ItemDrop.ItemData invItem in inv.GetAllItems() )
					if( item != invItem && item.m_shared.m_name.Equals( invItem.m_shared.m_name ) )
						return invItem;

			return null;
		}

		public static ItemDrop.ItemData FindLargestPartialStack( ItemDrop.ItemData item , InventoryGrid grid )
		{
			if( item.m_shared.m_maxStackSize == 1 )
				return null;

			int width = InputTweaks.InventoryGridPatch.Width( grid );
			Inventory inv = grid.GetInventory();

			return inv.GetAllItems()
			   .Where( x => ItemsAreSimilarButDistinct( item , x , true ) )
			   .OrderByDescending( x => x.m_stack )
			   .ThenBy( x => x.m_gridPos.x + ( x.m_gridPos.y * width ) )
			   .FirstOrDefault( x => x.m_stack < x.m_shared.m_maxStackSize );
		}

		public static ItemDrop.ItemData FindSmallestPartialStack( ItemDrop.ItemData item , InventoryGrid grid )
		{
			if( item.m_shared.m_maxStackSize == 1 )
				return null;

			int width = InputTweaks.InventoryGridPatch.Width( grid );
			Inventory inv = grid.GetInventory();

			return inv.GetAllItems()
			   .Where( x => ItemsAreSimilarButDistinct( item , x , true ) )
			   .OrderBy( x => x.m_stack )
			   .ThenBy( x => x.m_gridPos.x + ( x.m_gridPos.y * width ) )
			   .FirstOrDefault( x => x.m_stack < x.m_shared.m_maxStackSize );
		}

		public static bool IsCursorOver( GameObject go )
		{
			return IsCursorOver( go?.transform as RectTransform );
		}
		
		public static bool IsCursorOver( RectTransform rectTransform )
		{
			return rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint( rectTransform , Input.mousePosition );
		}

		public static bool ItemsAreSimilarButDistinct( ItemDrop.ItemData item , ItemDrop.ItemData other , bool considerQuality )
		{
			return item != null
				&& other != null
				&& item != other
				&& item.m_shared.m_name == other.m_shared.m_name
				&& ( !considerQuality || item.m_quality == other.m_quality );
		}
		
		public static IEnumerable< CodeInstruction > SwapShiftAndCtrl( IEnumerable< CodeInstruction > instructionsIn )
		{
			foreach( CodeInstruction instruction in instructionsIn )
			{
				if( instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == LeftShiftInt )
					instruction.operand = LeftControlInt;
				else if( instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == RightShiftInt )
					instruction.operand = RightControlInt;
				else if( instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == LeftControlInt )
					instruction.operand = LeftShiftInt;
				else if( instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == RightControlInt )
					instruction.operand = RightShiftInt;

				yield return instruction;
			}
		}

		public static List< CodeInstruction > SwapMoveAndSplitInSwitch( IEnumerable< CodeInstruction > instructionsIn )
		{
			List< CodeInstruction > instructions = new List< CodeInstruction >( instructionsIn );
			for( int index = 0 ; ( index + 2 ) < instructions.Count ; index++ )
			{
				CodeInstruction instruction0 = instructions[ index ];
				CodeInstruction instruction2 = instructions[ index + 2 ];

				// A weird approach, but this doesn't throw unmarked label argument exceptions
				if( instruction0.opcode == OpCodes.Ldarg_S && instruction2.opcode == OpCodes.Bne_Un )
				{
					// Swap InventoryGrid.Modifier.Split (1) and InventoryGrid.Modifier.Move (2)
					CodeInstruction instruction1 = instructions[ index + 1 ];
					if( instruction1.opcode == OpCodes.Ldc_I4_1 )
						instruction1.opcode = OpCodes.Ldc_I4_2;
					else if( instruction1.opcode == OpCodes.Ldc_I4_2 )
						instruction1.opcode = OpCodes.Ldc_I4_1;
				}
			}

			return instructions;
		}
	}
}
