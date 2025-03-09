using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NoLossyCookingStations
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.nolossycookingstations" , "No Lossy Cooking Stations" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class NoLossyCookingStations : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.nolossycookingstations" );

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

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
