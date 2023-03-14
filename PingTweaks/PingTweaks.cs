using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PingTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.pingtweaks" , "Ping Tweaks" , "1.0.1" )]
	[BepInProcess( "valheim.exe" )]
	public partial class PingTweaks : BaseUnityPlugin
    {
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< KeyCode > PingBroadcastModifier;
		public static ConfigEntry< bool > ShowMapMarkerTextWhenPinged;
		public static ConfigEntry< bool > SuppressChatBoxOnPing;
		public static ConfigEntry< bool > SuppressIncomingPings;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.pingtweaks" );

		private void Awake()
		{
			IsEnabled = Config.Bind(
				"0 - Core",
				"Enable",
				true,
				"Whether this mod has any effect when loaded." );

			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Whether this mod loads on game start." );

			// Note that LeftControl as a modifier will cause the player to teleport under specific circumstances
			PingBroadcastModifier = Config.Bind(
				"1 - General",
				"PingBroadcastModifier",
				KeyCode.LeftShift,
				"If set and not \"None\", pings will only be sent to other players when the specified key is pressed." );

			ShowMapMarkerTextWhenPinged = Config.Bind(
				"1 - General",
				"ShowMapMarkerTextWhenPinged",
				true,
				"If true, pinging map markers will show the marker text to any player with this mod and option enabled." );

			SuppressChatBoxOnPing = Config.Bind(
				"1 - General",
				"SuppressChatBoxOnPing",
				true,
				"If true, pings will not cause the chat box to appear." );

			SuppressIncomingPings = Config.Bind(
				"1 - General",
				"SuppressIncomingPings",
				false,
				"If true, pings from other players will not be shown." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
