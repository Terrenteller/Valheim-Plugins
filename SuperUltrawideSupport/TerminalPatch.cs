using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( Terminal ) )]
		private class TerminalPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( ref Terminal __instance )
			{
				// This has the name "Image" on the start menu and "Chat_box" when joined into a world
				RectTransform rectTransform = __instance.m_chatWindow;
				if( rectTransform != null )
				{
					Lerper.Register( rectTransform );
					Lerper.Lerp( rectTransform );
				}
			}
		}
	}
}
