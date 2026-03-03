using HarmonyLib;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Game ) )]
		private class GamePatch
		{
			[HarmonyPatch( "Logout" )]
			[HarmonyPrefix]
			private static void LogoutPostfix( ref bool ___m_firstSpawn )
			{
				ChatPatch.PersistentPingLocation = null;
			}
		}
	}
}
