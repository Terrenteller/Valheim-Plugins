using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace PingTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.pingtweaks" , "Ping Tweaks" , "1.0.6" )]
	[BepInProcess( "valheim.exe" )]
	public partial class PingTweaks : BaseUnityPlugin
	{
		public static PingTweaks Instance = null;

		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > SavePinnedPings;
		// 2 - Standard Pings
		public static ConfigEntry< KeyCode > PingBroadcastModifier;
		public static ConfigEntry< Color > PingColor;
		public static ConfigEntry< int > PingDuration;
		// 3 - Persistent Pings
		//public static ConfigEntry< bool > ClearOnArrivalRadius; // TODO: How to manage when starting within?
		public static ConfigEntry< KeyCode > PersistentPingBroadcastModifier;
		public static ConfigEntry< Color > PersistentPingColor;
		private static Coroutine RefreshPersistentPingCoRoutine = null;

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

			SavePinnedPings = Config.Bind(
				"1 - General",
				"SavePinnedPings",
				false,
				"Whether double-clicking on a ping from another player creates a persistent pin. Off by default to not conflict with the cartography table." );

			// Warning: LeftControl as a modifier will cause the player to teleport with cheats
			PingBroadcastModifier = Config.Bind(
				"2 - Standard Pings",
				"PingBroadcastModifier",
				KeyCode.LeftShift,
				"If set and not \"None\", pings will only be sent to other players when the specified key is pressed. If \"None\", pings will always be broadcast." );

			// FIXME: Figure out what controls the map text color
			PingColor = Config.Bind(
				"2 - Standard Pings",
				"PingColor",
				new Color( 0.6f , 0.7f , 1.0f , 1.0f ), // Vanilla default
				"In-world ping text color." );

			PingDuration = Config.Bind(
				"2 - Standard Pings",
				"PingDuration",
				5, // Local testing clocks in at eight seconds, but ILSpy says five seconds
				new ConfigDescription(
					"How long to show pings, in seconds.",
					new AcceptableValueRange< int >( 5 , 60 ) ) );

			// Warning: LeftControl as a modifier will cause the player to teleport with cheats
			PersistentPingBroadcastModifier = Config.Bind(
				"3 - Persistent Pings",
				"PersistentPingBroadcastModifier",
				KeyCode.LeftAlt,
				"If set and not \"None\", pings will be created as persistent when the specified key is pressed. If \"None\", pings will never be persistent." );

			// FIXME: Figure out what controls the map text color
			PersistentPingColor = Config.Bind(
				"3 - Persistent Pings",
				"PersistentPingColor",
				new Color( 1.0f , 0.7f , 0.6f , 1.0f ), // Somewhat opposite of a regular ping
				"In-world persistent ping text color." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();

				RefreshPersistentPingCoRoutine = Instance.StartCoroutine( CoRefreshPersistentPing() );
			}
		}

		private IEnumerator CoRefreshPersistentPing()
		{
			while( true )
			{
				// Needs to be less than Chat.m_worldTextTTL so it appears persistent to other players
				yield return new WaitForSecondsRealtime( 3.0f );

				if( ChatPatch.PersistentPingLocation != null )
					Chat.instance.SendPing( ChatPatch.PersistentPingLocation.GetValueOrDefault() );
			}
		}
	}
}
