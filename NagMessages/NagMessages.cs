using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

namespace NagMessages
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.nagmessages" , "Nag Messages" , "1.0.1" )]
	[BepInProcess( "valheim.exe" )]
	public partial class NagMessages : BaseUnityPlugin
	{
		public static NagMessages Instance = null;

		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > QueueCenterMessages;
		// 2 - Forsaken Powers
		public static ConfigEntry< bool > AllowBonemass;
		public static ConfigEntry< bool > AllowEikthyr;
		public static ConfigEntry< bool > AllowModer;
		public static ConfigEntry< bool > AllowTheElder;
		public static ConfigEntry< bool > AllowTheQueen;
		public static ConfigEntry< bool > AllowYagluth;
		public static ConfigEntry< int > PowerNagFrequency;
		private static int LastPowerNagFrequency;
		// 3 - Food Effects
		public static ConfigEntry< int > EitrThreshold;
		public static ConfigEntry< int > HealthThreshold;
		public static ConfigEntry< int > HungerNagFrequency;
		private static int LastHungerNagFrequency;
		public static ConfigEntry< int > StaminaThreshold;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.nagmessages" );

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

			// We need messages to be queued to operate smoothly, but this tweak is not specific to us.
			// Allow our tweak to be toggled off so we don't break someone else's fix.
			QueueCenterMessages = Config.Bind(
				"1 - General",
				"QueueCenterMessages",
				true,
				"Whether front-and-center messages will be queued for display. Provided for compatibility." );

			AllowBonemass = Config.Bind(
				"2 - Forsaken Powers",
				"AllowBonemass",
				true,
				"If false, periodically nag the player to switch powers." );

			AllowEikthyr = Config.Bind(
				"2 - Forsaken Powers",
				"AllowEikthyr",
				true,
				"If false, periodically nag the player to switch powers." );

			AllowModer = Config.Bind(
				"2 - Forsaken Powers",
				"AllowModer",
				true,
				"If false, periodically nag the player to switch powers." );

			AllowTheElder = Config.Bind(
				"2 - Forsaken Powers",
				"AllowTheElder",
				true,
				"If false, periodically nag the player to switch powers." );

			AllowTheQueen = Config.Bind(
				"2 - Forsaken Powers",
				"AllowTheQueen",
				true,
				"If false, periodically nag the player to switch powers." );

			AllowYagluth = Config.Bind(
				"2 - Forsaken Powers",
				"AllowYagluth",
				true,
				"If false, periodically nag the player to switch powers." );

			PowerNagFrequency = Config.Bind(
				"2 - Forsaken Powers",
				"PowerNagFrequency",
				3,
				"Minimum time in minutes between Forsaken Power nag messages." );

			// This one is particularly useful because the amount of eitr required to cast fireball
			// is more than a single eitr food can provide a few minutes before the food wears off
			EitrThreshold = Config.Bind(
				"3 - Food Effects",
				"EitrThreshold",
				35,
				"Warn the player when their maximum eitr drops to or below this amount." );

			HealthThreshold = Config.Bind(
				"3 - Food Effects",
				"HealthThreshold",
				0,
				"Warn the player when their maximum health drops to or below this amount." );

			// Is there a sound we could play too? What about for the power?
			HungerNagFrequency = Config.Bind(
				"3 - Food Effects",
				"HungerNagFrequency",
				3,
				"Minimum time in minutes between hunger nag messages." );

			StaminaThreshold = Config.Bind(
				"3 - Food Effects",
				"StaminaThreshold",
				0,
				"Warn the player when their maximum stamina drops to or below this amount." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();

				LastHungerNagFrequency = HungerNagFrequency.Value;
				Config.SettingChanged += Config_SettingChanged;
			}
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == AllowBonemass
				|| e.ChangedSetting == AllowEikthyr
				|| e.ChangedSetting == AllowModer
				|| e.ChangedSetting == AllowTheElder
				|| e.ChangedSetting == AllowTheQueen
				|| e.ChangedSetting == AllowYagluth )
			{
				if( !(bool)e.ChangedSetting.BoxedValue )
					NagAboutPower();
			}
			else if( e.ChangedSetting == PowerNagFrequency )
			{
				int changeInMinutes = PowerNagFrequency.Value - LastPowerNagFrequency;
				double changeInSeconds = changeInMinutes * 60.0;

				// We only need to schedule a nag when the delay shrinks.
				// If the delay grows, a pending nag will cause a new one
				// to be scheduled in the right amount of time.
				if( changeInMinutes > 0 )
				{
					MinTimeOfNextPowerNag += changeInSeconds;
				}
				else if( changeInMinutes < 0 )
				{
					double now = Time.timeAsDouble;
					changeInSeconds = Math.Abs( changeInSeconds );

					if( ( MinTimeOfNextPowerNag - changeInSeconds ) < now )
						MinTimeOfNextPowerNag = now + ( PowerNagFrequency.Value * 60.0 );
					else
						MinTimeOfNextPowerNag -= changeInSeconds;

					NagAboutPower();
				}

				LastPowerNagFrequency = PowerNagFrequency.Value;
			}
			else if( e.ChangedSetting == HungerNagFrequency )
			{
				int changeInMinutes = HungerNagFrequency.Value - LastHungerNagFrequency;
				double changeInSeconds = changeInMinutes * 60.0;

				// We only need to schedule a nag when the delay shrinks.
				// If the delay grows, a pending nag will cause a new one
				// to be scheduled in the right amount of time.
				if( changeInMinutes > 0 )
				{
					MinTimeOfNextHungerNag += changeInSeconds;
				}
				else if( changeInMinutes < 0 )
				{
					double now = Time.timeAsDouble;
					changeInSeconds = Math.Abs( changeInSeconds );

					if( ( MinTimeOfNextHungerNag - changeInSeconds ) < now )
						MinTimeOfNextHungerNag = now + ( HungerNagFrequency.Value * 60.0 );
					else
						MinTimeOfNextHungerNag -= changeInSeconds;

					NagAboutHunger();
				}

				LastHungerNagFrequency = HungerNagFrequency.Value;
			}
		}
	}
}
