using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MouseTweaks
{
	public partial class MouseTweaks
	{
		[HarmonyPatch( typeof( Inventory ) )]
		public class InventoryPatch
		{
			public static void Changed( Inventory inv )
			{
				Traverse.Create( inv )
					.Method( "Changed" )
					.GetValue();
			}
		}

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
				return Common.SwapShiftAndCtrl( instructionsIn );
			}

			/*
			[HarmonyPatch( "UpdateGui" )]
			[HarmonyPrefix]
			private static void UpdateGuiPrefix(
				Player player,
				ItemDrop.ItemData dragItem,
				Inventory ___m_inventory,
				int ___m_width,
				int ___m_height,
				out bool __state )
			{
				__state = ___m_width != ___m_inventory.GetWidth() || ___m_height != ___m_inventory.GetHeight();
			}

			[HarmonyPatch( "UpdateGui" )]
			[HarmonyPostfix]
			private static void UpdateGuiPostfix( InventoryGrid __instance , Player player , ItemDrop.ItemData dragItem , ref bool __state )
			{
				//if( __state )
				//	InventoryGuiPatch.RefreshInventoryButtons( __instance );
			}
			*/
		}
	}
}
