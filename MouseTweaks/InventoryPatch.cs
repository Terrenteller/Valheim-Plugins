using HarmonyLib;

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
	}
}
