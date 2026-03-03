using HarmonyLib;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Player ) )]
		private class PlayerPatch
		{
			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPostfix( ref Player ___m_localPlayer )
			{
				if( ___m_localPlayer == null )
					ChatPatch.PersistentPingLocation = null;
			}
		}
	}
}
