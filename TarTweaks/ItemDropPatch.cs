using HarmonyLib;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( ItemDrop ) )]
		class ItemDropPatch
		{
			internal static bool SuppressInTar = false;

			[HarmonyPatch( "InTar" )]
			[HarmonyPrefix]
			private static bool InTarPrefix( ref bool __result )
			{
				if( IsEnabled.Value && SuppressInTar )
				{
					__result = false;
					return false;
				}

				return true;
			}

			[HarmonyPatch( "Interact" )]
			[HarmonyPrefix]
			private static void InteractPrefix()
			{
				SuppressInTar = CanInteractivelyTakeFromTar.Value;
			}

			[HarmonyPatch( "Interact" )]
			[HarmonyPostfix]
			private static void InteractPostfix()
			{
				SuppressInTar = false;
			}
		}
	}
}
