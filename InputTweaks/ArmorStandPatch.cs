using HarmonyLib;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( ArmorStand ) )]
		public class ArmorStandPatch
		{
			public static bool CanAttach( ArmorStand armorStand , ArmorStand.ArmorStandSlot slot , ItemDrop.ItemData item )
			{
				return Traverse.Create( armorStand )
					.Method( "CanAttach" , new[] { typeof( ArmorStand.ArmorStandSlot ) , typeof( ItemDrop.ItemData ) } )
					.GetValue< bool >( slot , item );
			}

			public static void RPC_DropItem( ArmorStand armorStand , int index )
			{
				Traverse.Create( armorStand )
					.Field( "m_nview" )
					.GetValue< ZNetView >()
					.InvokeRPC( ZNetView.Everybody , "RPC_DropItem" , index );
			}
		}
	}
}
