using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace TarTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs
	[BepInPlugin( "com.riintouge.tartweaks" , "Tar Tweaks" , "1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class TarTweaks : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - Items
		public static ConfigEntry< bool > CanAutomaticallyTakeFromTar;
		public static ConfigEntry< bool > CanInteractivelyTakeFromTar;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.tartweaks" );

		// TODO / Considerations:
		// - Server sync
		// - Players need to be able to climb out of tar pits
		// - Stamina drain when moving in tar
		// - Higher stamina drain when swimming in tar
		// - Drowning in tar inflicts poison
		// - Optional stamina requirement when only interactive pickup is enabled
		// - Sap collector collects tar
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

			CanAutomaticallyTakeFromTar = Config.Bind(
				"1 - Items",
				"CanAutomaticallyTakeFromTar",
				false,
				"Allow the player to automatically take items stuck in tar." );

			CanInteractivelyTakeFromTar = Config.Bind(
				"1 - Items",
				"CanInteractivelyTakeFromTar",
				true,
				"Allow the player to interactively take items stuck in tar." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
