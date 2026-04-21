using GUIFramework;
using HarmonyLib;
using System;
namespace BorderlessWindowed
{
	public partial class BorderlessWindowed
	{
		[HarmonyPatch( typeof( GraphicsSettingsManager ) )]
		private class GraphicsSettingsManagerPatch
		{
			[HarmonyPatch( "ApplyTargetResolutionSetting" )]
			[HarmonyPostfix]
			private static void ApplyTargetResolutionSettingPostfix()
			{
				Instance.StartCoroutine( Instance.CoUpdateBorder() );
			}
		}
	}
}
