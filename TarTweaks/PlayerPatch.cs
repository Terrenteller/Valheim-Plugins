using HarmonyLib;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( Player ) )]
		class PlayerPatch
		{
			[HarmonyPatch( "AutoPickup" )]
			[HarmonyPrefix]
			private static void AutoPickupPrefix()
			{
				ItemDropPatch.SuppressInTar = CanAutomaticallyTakeFromTar.Value;
			}

			[HarmonyPatch( "AutoPickup" )]
			[HarmonyPostfix]
			private static void AutoPickupPostfix()
			{
				ItemDropPatch.SuppressInTar = false;
			}

			[HarmonyPatch( "Dodge" )]
			[HarmonyPrefix]
			private static bool DodgePrefix( ref Player __instance )
			{
				return !CharacterPatch.ShouldApplyTarMovementPenalty( __instance , out _ );
			}
		}
	}
}
