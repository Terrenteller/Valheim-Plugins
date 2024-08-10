using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( InventoryGrid ) )]
		public class InventoryGridPatch
		{
			public static Vector2i GetButtonPos( InventoryGrid grid , UIInputHandler element )
			{
				return GetButtonPos( grid , element.gameObject );
			}

			public static Vector2i GetButtonPos( InventoryGrid grid , GameObject go )
			{
				return Traverse.Create( grid )
					.Method( "GetButtonPos" , new[] { typeof( GameObject ) } )
					.GetValue< Vector2i >( go );
			}

			public static void OnRightClick( InventoryGrid grid , UIInputHandler element )
			{
				Traverse.Create( grid )
					.Method( "OnRightClick" , new[] { typeof( UIInputHandler ) } )
					.GetValue( element );
			}

			public static int Width( InventoryGrid grid )
			{
				return Traverse.Create( grid )
					.Field( "m_width" )
					.GetValue< int >();
			}

			[HarmonyPatch( "OnLeftClick" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > OnLeftClickTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				return InitialSwapMoveAndSplit ? Common.SwapShiftAndCtrl( instructionsIn ) : instructionsIn;
			}
		}
	}
}
