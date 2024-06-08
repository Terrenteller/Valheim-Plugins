using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomSlotItemLib
{
	public partial class CustomSlotItemLib
	{
		[HarmonyPatch( typeof( Humanoid ) )]
		public class HumanoidPatch
		{
			public static HashSet< StatusEffect > GetStatusEffectsFromCustomSlotItems( Humanoid __instance )
			{
				HashSet< StatusEffect > statuses = new HashSet< StatusEffect >();

				foreach( ItemDrop.ItemData itemData in CustomSlotManager.AllSlotItems( __instance ) )
				{
					if( itemData.m_shared.m_equipStatusEffect )
						statuses.Add( itemData.m_shared.m_equipStatusEffect );

					bool hasSetEffect = Traverse.Create( __instance )
						.Method( "HaveSetEffect" , new[] { typeof( ItemDrop.ItemData ) } )
						.GetValue< bool >( itemData );
					if( hasSetEffect )
						statuses.Add( itemData.m_shared.m_setStatusEffect );
				}

				return statuses;
			}

			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			static void AwakePostfix( ref Humanoid __instance )
			{
				CustomSlotManager.Register( __instance );
			}

			[HarmonyPatch( "EquipItem" )]
			[HarmonyPostfix]
			static void EquipItemPostfix( ref bool __result , ref Humanoid __instance , ItemDrop.ItemData item , bool triggerEquipEffects = true )
			{
				if( !__result || !CustomSlotManager.IsCustomSlotItem( item ) )
					return;

				string slotName = CustomSlotManager.GetCustomSlotName( item );
				ItemDrop.ItemData existingSlotItem = CustomSlotManager.GetSlotItem( __instance , slotName );
				if( existingSlotItem != null )
					__instance.UnequipItem( existingSlotItem , triggerEquipEffects );
				CustomSlotManager.SetSlotItem( __instance , slotName , item );

				item.m_equipped = __instance.IsItemEquiped( item );
				Traverse.Create( __instance )
					.Method( "SetupEquipment" )
					.GetValue();

				if( item.m_equipped && triggerEquipEffects )
				{
					Traverse.Create( __instance )
						.Method( "TriggerEquipEffect" , new[] { typeof( ItemDrop.ItemData ) } )
						.GetValue( item );
				}

				__result = true;
			}

			[HarmonyPatch( "GetEquipmentWeight" )]
			[HarmonyPostfix]
			static void GetEquipmentWeightPostfix( ref float __result , ref Humanoid __instance )
			{
				foreach( ItemDrop.ItemData itemData in CustomSlotManager.AllSlotItems( __instance ) )
					__result += itemData.m_shared.m_weight;
			}

			[HarmonyPatch( "GetSetCount" )]
			[HarmonyPostfix]
			static void GetSetCountPostfix( ref int __result , ref Humanoid __instance , string setName )
			{
				foreach( ItemDrop.ItemData itemData in CustomSlotManager.AllSlotItems( __instance ) )
					if( itemData.m_shared.m_setName == setName )
						__result++;
			}

			[HarmonyPatch( "IsItemEquiped" )]
			[HarmonyPostfix]
			static void IsItemEquipedPostfix( ref bool __result , ref Humanoid __instance , ItemDrop.ItemData item )
			{
				if( !CustomSlotManager.IsCustomSlotItem( item ) )
					return;

				string slotName = CustomSlotManager.GetCustomSlotName( item );
				__result |= CustomSlotManager.GetSlotItem( __instance , slotName ) == item;
			}

			[HarmonyPatch( "UnequipAllItems" )]
			[HarmonyPostfix]
			static void UnequipAllItemsPostfix( ref Humanoid __instance )
			{
				foreach( ItemDrop.ItemData itemData in CustomSlotManager.AllSlotItems( __instance ) )
					__instance.UnequipItem( itemData , false );
			}

			[HarmonyPatch( "UnequipItem" )]
			[HarmonyPostfix]
			static void UnequipItemPostfix( ref Humanoid __instance , ItemDrop.ItemData item , bool triggerEquipEffects = true )
			{
				if( item == null )
					return;

				string slotName = CustomSlotManager.GetCustomSlotName( item );
				if( item == CustomSlotManager.GetSlotItem( __instance , slotName ) )
				{
					CustomSlotManager.SetSlotItem( __instance , slotName , null );
					item.m_equipped = __instance.IsItemEquiped( item );
					Traverse.Create( __instance )
						.Method( "UpdateEquipmentStatusEffects" )
						.GetValue();
				}
			}

			[HarmonyPatch( "UpdateEquipmentStatusEffects" )]
			[HarmonyTranspiler]
			static IEnumerable< CodeInstruction > UpdateEquipmentStatusEffectsTranspiler( IEnumerable< CodeInstruction > instructionsIn )
			{
				List< CodeInstruction > instructions = instructionsIn.ToList();
				if( instructions[ 0 ].opcode != OpCodes.Newobj || instructions[ 1 ].opcode != OpCodes.Stloc_0 )
					throw new Exception( "CustomSlotItemLib transpiler injection point not found!" );

				yield return instructions[ 0 ];
				yield return instructions[ 1 ];

				// Add GetStatusEffectsFromCustomSlotItems() results to the set
				yield return new CodeInstruction( OpCodes.Ldloc_0 );
				yield return new CodeInstruction( OpCodes.Ldarg_0 );
				yield return CodeInstruction.Call( typeof( HumanoidPatch ) , nameof( HumanoidPatch.GetStatusEffectsFromCustomSlotItems ) );
				yield return CodeInstruction.Call( typeof( HashSet< StatusEffect > ) , nameof( HashSet< StatusEffect >.UnionWith ) );

				for( int index = 2 ; index < instructions.Count ; index++ )
				{
					CodeInstruction instruction = instructions[ index ];
					yield return instruction;
				}
			}
		}
	}
}
