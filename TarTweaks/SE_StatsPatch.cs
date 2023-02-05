﻿using HarmonyLib;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( SE_Stats ) )]
		private class SE_StatsPatch
		{
			[HarmonyPatch( "ModifySpeed" )]
			[HarmonyPostfix]
			private static void ModifySpeedPostfix( ref SE_Stats __instance , ref Character ___m_character , ref float speed )
			{
				if( IsEnabled.Value && __instance.name.CompareTo( "Tared" ) == 0 )
					CharacterPatch.ModifySpeedForTar( ___m_character , ref speed );
			}
		}
	}
}
