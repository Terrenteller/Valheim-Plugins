using HarmonyLib;
using UnityEngine;

namespace RestfulArrival
{
	public partial class RestfulArrival
	{
		[HarmonyPatch( typeof( SE_Cozy ) )]
		private class SE_CozyPatch
		{
			[HarmonyPatch( "UpdateStatusEffect" )]
			[HarmonyPrefix]
			private static void UpdateStatusEffectPrefix( ref SE_Cozy __instance )
			{
				if( IsEnabled.Value && ( Time.timeAsDouble - GamePatch.TimeOfLastFirstSpawn ) < 3.0 )
				{
					// Calculate early so the rested message is correct
					Traverse.Create( Player.m_localPlayer )
						.Field( "m_comfortLevel" )
						.SetValue( SE_Rested.CalculateComfortLevel( Player.m_localPlayer ) );

					__instance.m_delay = 0.0f;
				}
			}
		}
	}
}
