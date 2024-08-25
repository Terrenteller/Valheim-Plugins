using HarmonyLib;
using Valheim.SettingsGui;

namespace BorderlessWindowed
{
	public partial class BorderlessWindowed
	{
		[HarmonyPatch( typeof( GraphicsModeManager ) )]
		private class GraphicsModeManagerPatch
		{
			[HarmonyPatch( "ApplyMode" )]
			[HarmonyPostfix]
			private static void ApplyModePostfix( bool __result , GraphicsQualityMode mode )
			{
				if( __result )
					Instance.StartCoroutine( Instance.CoUpdateBorder() );
			}
		}
	}
}
