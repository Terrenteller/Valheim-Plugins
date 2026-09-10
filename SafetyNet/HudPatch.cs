using HarmonyLib;
using UnityEngine;

namespace SafetyNet
{
	public partial class SafetyNet
	{
		[HarmonyPatch( typeof( Hud ) )]
		public class HudPatch
		{
			[HarmonyPatch( "Update" )]
			[HarmonyPostfix]
			private static void UpdatePostfix( Hud __instance )
			{
				KeyCode toggleVisiblityKey = VisiblityToggleKey.Value;
				if( Player.m_localPlayer != null && toggleVisiblityKey != KeyCode.None && ZInput.GetKeyDown( toggleVisiblityKey ) )
					IsEnabled.Value = !IsEnabled.Value;
			}
		}
	}
}
