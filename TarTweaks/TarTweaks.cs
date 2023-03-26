using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace TarTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.tartweaks" , "Tar Tweaks" , "1.0.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class TarTweaks : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - Items
		public static ConfigEntry< bool > CanInteractivelyTakeFromTar;
		// 2 - Characters
		public static ConfigEntry< bool > ApplyMovementPenalties;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.tartweaks" );

		private void Awake()
		{
			IsEnabled = Config.Bind(
				"0 - Core",
				"Enable",
				true,
				"Determines if this mod has any effect when loaded." );

			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Determines if this mod loads on game start." );

			CanInteractivelyTakeFromTar = Config.Bind(
				"1 - Items",
				"CanInteractivelyTakeFromTar",
				true,
				"Allow the player to interactively take items stuck in tar." );

			ApplyMovementPenalties = Config.Bind(
				"2 - Characters",
				"ApplyMovementPenalties",
				true,
				"Penalizes character movement (not just players) when tarred and in tar." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
