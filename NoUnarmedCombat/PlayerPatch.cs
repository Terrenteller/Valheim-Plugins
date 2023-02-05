using HarmonyLib;

namespace NoUnarmedCombat
{
	public partial class NoUnarmedCombat
	{
		[HarmonyPatch( typeof( Player ) )]
		class PlayerPatch
		{
			// If we unsheathe a bow instead of punching without clearing these timers:
			// 1. We cannot sheathe it the same way. The character will immediately equip it again.
			// 2. Switching to a melee weapon will cause the player to perform an attack unexpectedly.
			internal static bool ClearQueuedAttackTimers = false;

			[HarmonyPatch( "PlayerAttackInput" )]
			[HarmonyPostfix]
			static void PlayerAttackInputPostfix(
				ref float ___m_queuedAttackTimer,
				ref float ___m_queuedSecondAttackTimer )
			{
				if( ClearQueuedAttackTimers )
				{
					___m_queuedAttackTimer = 0.0f;
					___m_queuedSecondAttackTimer = 0.0f;
				}
			}
		}
	}
}
