using HarmonyLib;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Minimap.PinNameData ) )]
		private class MinimapPinNameDataPatch
		{
			[HarmonyPatch( "SetTextAndGameObject" )]
			[HarmonyPostfix]
			private static void SetTextAndGameObjectPostfix( ref Minimap.PinNameData __instance , ref GameObject text )
			{
				if( !IsEnabled.Value
					|| __instance == null
					|| __instance.ParentPin == null
					|| __instance.PinNameText == null
					|| __instance.ParentPin.m_type != Minimap.PinType.Ping )
				{
					return;
				}

				// Prime the text to avoid flickering.
				// Minimap.PinNameData.PinNameText.text is of the form "<name>: <text>" set by Minimap.UpdatePingPins().
				// FIXME: What if a name contains this pattern?
				int splitPos = __instance.PinNameText.text.IndexOf( ": " );
				__instance.PinNameText.text = string.Format(
					"{0}\n{1}\n{2}",
					__instance.PinNameText.text.Substring( 0 , splitPos ),
					__instance.PinNameText.text.Substring( splitPos + 2 ),
					Common.PrettyPrintDistance( Player.m_localPlayer.transform.position , __instance.ParentPin.m_pos ) );
			}
		}
	}
}
