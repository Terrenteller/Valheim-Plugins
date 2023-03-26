using HarmonyLib;

namespace NoUnarmedCombat
{
	public partial class NoUnarmedCombat
	{
		[HarmonyPatch( typeof( Player ) )]
		private class PlayerPatch
		{
			// If we unsheathe a bow instead of punching without messing with these timers:
			// 1. We cannot sheathe it the same way. The character will immediately equip it again.
			// 2. Switching to a melee weapon will cause the player to perform an attack unexpectedly.
			internal static bool RestoreQueuedAttackTimers = false;
			private static float LastAttackTimer = 0.0f;
			private static float LastSecondaryAttackTimer = 0.0f;

			[HarmonyPatch( "PlayerAttackInput" )]
			[HarmonyPrefix]
			private static void PlayerAttackInputPrefix(
				ref float ___m_queuedAttackTimer,
				ref float ___m_queuedSecondAttackTimer )
			{
				LastAttackTimer = ___m_queuedAttackTimer;
				LastSecondaryAttackTimer = ___m_queuedSecondAttackTimer;
			}

			[HarmonyPatch( "PlayerAttackInput" )]
			[HarmonyPostfix]
			private static void PlayerAttackInputPostfix(
				ref float ___m_queuedAttackTimer,
				ref float ___m_queuedSecondAttackTimer )
			{
				if( RestoreQueuedAttackTimers )
				{
					___m_queuedAttackTimer = LastAttackTimer;
					___m_queuedSecondAttackTimer = LastSecondaryAttackTimer;
				}
			}
		}
	}
}
