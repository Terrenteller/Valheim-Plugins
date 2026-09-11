using HarmonyLib;

namespace NagMessages
{
	public partial class NagMessages
	{
		[HarmonyPatch( typeof( Game ) )]
		private class GamePatch
		{
			private static bool FirstSpawnPending = false;

			[HarmonyPatch( "FixedUpdate" )]
			[HarmonyPrefix]
			[HarmonyPriority( Priority.First + 1 )]
			private static void FixedUpdatePrefix( ref bool ___m_firstSpawn )
			{
				// Capture this value before other plugins mess with it
				if( ___m_firstSpawn )
					FirstSpawnPending = true;
			}

			[HarmonyPatch( "UpdateRespawn" )]
			[HarmonyPostfix]
			private static void UpdateRespawnPostfix()
			{
				if( FirstSpawnPending && Player.m_localPlayer )
				{
					FirstSpawnPending = false;

					// Forsaken Powers are status effects like any other
					// and are not retained upon switching worlds
					Instance.NagAboutPower( MessageHudPatch.MessageTTL * 2.0 , true );
					Instance.NagAboutHunger( MessageHudPatch.MessageTTL * 3.0 , true );
				}
			}
		}
	}
}
