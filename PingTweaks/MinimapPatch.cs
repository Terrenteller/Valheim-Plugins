using HarmonyLib;
using System.Collections.Generic;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Minimap ) )]
		private class MinimapPatch
		{
			internal static bool RegeneratePingPins = false;

			[HarmonyPatch( "UpdatePingPins" )]
			[HarmonyPrefix]
			private static void UpdatePingPinsPrefix( ref Minimap __instance , ref List< Minimap.PinData > ___m_pingPins )
			{
				// Minimap.UpdatePingPins() only updates PinData.m_name, not PinData.m_NamePinData.
				// Blow it all away and let the game figure it out.
				if( RegeneratePingPins )
				{
					foreach( Minimap.PinData pingPin in ___m_pingPins )
						__instance.RemovePin( pingPin );

					___m_pingPins.Clear();
				}
			}
		}
	}
}
