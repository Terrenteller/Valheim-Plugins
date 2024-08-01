﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace MouseTweaks
{
	public class Common
	{
		public const int LeftControlInt = (int)KeyCode.LeftControl;
		public const int RightControlInt = (int)KeyCode.RightControl;
		public const int LeftShiftInt = (int)KeyCode.LeftShift;
		public const int RightShiftInt = (int)KeyCode.RightShift;

		public static bool AnyControl()
		{
			return ZInput.GetKey( KeyCode.LeftControl ) || ZInput.GetKey( KeyCode.RightControl );
		}
		
		public static bool AnyShift()
		{
			return ZInput.GetKey( KeyCode.LeftShift ) || ZInput.GetKey( KeyCode.RightShift );
		}

		public static bool CanStackOnto( ItemDrop.ItemData item , ItemDrop.ItemData target )
		{
			return item != null
				&& target != null
				&& item != target
				&& item.m_shared.m_name == target.m_shared.m_name
				&& target.m_shared.m_maxStackSize > 1
				&& target.m_stack < target.m_shared.m_maxStackSize
				&& item.m_quality == target.m_quality;
		}

		public static List< ItemDrop.ItemData > EquivalentStackables( ItemDrop.ItemData item , InventoryGrid grid )
		{
			int width = MouseTweaks.InventoryGridPatch.Width( grid );
			Inventory inv = grid.GetInventory();

			return inv.GetAllItems()
			   .Where( x => CanStackOnto( x , item ) )
			   .OrderBy( x => x.m_stack )
			   .ThenBy( x => x.m_gridPos.x + ( x.m_gridPos.y * width ) )
			   .ToList();
		}

		public static ItemDrop.ItemData FindFirstSimilarItemInInventory( ItemDrop.ItemData item , Inventory inv )
		{
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

		public static ItemDrop.ItemData FindPartialStack( ItemDrop.ItemData item , Inventory inv , bool canReturnSelf )
		{
			if( item == null )
				return null;
			else if( canReturnSelf && item.m_stack < item.m_shared.m_maxStackSize )
				return item;

			foreach( ItemDrop.ItemData invItem in inv.GetAllItems() )
			{
				if( invItem != item
					&& invItem.m_shared.m_name.Equals( item.m_shared.m_name )
					&& invItem.m_stack < invItem.m_shared.m_maxStackSize )
				{
					return invItem;
				}
			}

			return null;
		}

		public static bool IsCursorOver( GameObject go )
		{
			RectTransform rectTransform = go.transform as RectTransform;
			return rectTransform != null
				? RectTransformUtility.RectangleContainsScreenPoint( rectTransform , Input.mousePosition )
				: false;
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
					// Swap InventoryGrid.Modifier.Split and InventoryGrid.Modifier.Move
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
