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
			private static void FixedUpdatePrefix( ref bool ___m_firstSpawn )
			{
				// Other mods which do or change things on first spawn may screw with Game.m_firstSpawn.
				// Capture the value sooner so we have a higher chance of working correctly.
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
