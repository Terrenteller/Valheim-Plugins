using HarmonyLib;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( Inventory ) )]
		public class InventoryPatch
		{
			public static void Changed( Inventory inv , bool success = false , bool cheatedStateChanged = false )
			{
				if( inv != null )
				{
					Traverse.Create( inv )
						.Method( "Changed" , new[] { typeof( bool ) , typeof( bool ) } )
						.GetValue( success , cheatedStateChanged );
				}
			}
		}
	}
}
