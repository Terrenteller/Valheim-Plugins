using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace IHaveArrived
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.ihavearrived" , "I Have Arrived" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class IHaveArrived : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > AnnounceArrival;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.ihavearrived" );

		private void Awake()
		{
			IsEnabled = Config.Bind(
				"0 - Core",
				"Enable",
				true,
				"Whether this plugin has any effect when loaded." );

			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Whether this plugin loads on game start." );

			AnnounceArrival = Config.Bind(
				"1 - General",
				"AnnounceArrival",
				true,
				"Whether the player shouts a very very Swedish sentence upon joining a world. When false, the player will not announce their arrival." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
