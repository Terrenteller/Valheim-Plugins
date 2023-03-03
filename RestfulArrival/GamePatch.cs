using HarmonyLib;
using UnityEngine;

namespace RestfulArrival
{
	public partial class RestfulArrival
	{
		[HarmonyPatch( typeof( Game ) )]
		private class GamePatch
		{
			private static bool FirstSpawnPending = false;
			internal static double TimeOfLastFirstSpawn = 0.0;

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
					TimeOfLastFirstSpawn = Time.timeAsDouble;
				}
			}
		}
	}
}
