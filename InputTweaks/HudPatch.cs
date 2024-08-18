using HarmonyLib;
using UnityEngine;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( Hud ) )]
		public class HudPatch
		{
			[HarmonyPatch( "Update" )]
			[HarmonyPostfix]
			private static void UpdatePostfix( Hud __instance )
			{
				KeyCode toggleHudKey = ToggleHudKey.Value;
				if( Player.m_localPlayer != null && toggleHudKey != KeyCode.None && ZInput.GetKeyDown( toggleHudKey ) )
					__instance.m_userHidden = !__instance.m_userHidden;
			}
		}
	}
}
