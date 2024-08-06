using HarmonyLib;

namespace InputTweaks
{
	public partial class InputTweaks
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
	}
}
