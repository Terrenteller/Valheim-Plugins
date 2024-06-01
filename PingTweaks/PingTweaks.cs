using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PingTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.pingtweaks" , "Ping Tweaks" , "1.0.3" )]
	[BepInProcess( "valheim.exe" )]
	public partial class PingTweaks : BaseUnityPlugin
    {
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< KeyCode > PingBroadcastModifier;
		public static ConfigEntry< Color > PingColor;
		public static ConfigEntry< bool > SavePinnedPings;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.pingtweaks" );

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

			// LeftControl as a modifier will cause the player to teleport with cheats
			PingBroadcastModifier = Config.Bind(
				"1 - General",
				"PingBroadcastModifier",
				KeyCode.LeftShift,
				"If set and not \"None\", pings will only be sent to other players when the specified key is pressed." );

			// FIXME: Why doesn't this work on the minimap?
			PingColor = Config.Bind(
				"1 - General",
				"PingColor",
				new Color( 0.6f , 0.7f , 1.0f , 1.0f ), // Vanilla default
				"In-world ping text color." );

			SavePinnedPings = Config.Bind(
				"1 - General",
				"SavePinnedPings",
				false,
				"Whether double-clicking on a ping from another player creates a persistent pin. Off by default to not conflict with the cartography table." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
