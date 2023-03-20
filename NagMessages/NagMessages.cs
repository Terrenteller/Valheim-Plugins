using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NagMessages
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.nagmessages" , "Nag Messages" , "1.0.0" )]
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
		// 3 - Food Effects
		public static ConfigEntry< int > EitrThreshold;
		public static ConfigEntry< int > HealthThreshold;
		public static ConfigEntry< int > StaminaThreshold;
		public static ConfigEntry< int > StomachNagFrequency;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.nagmessages" );

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
				5,
				"Minutes between forsaken power nag messages." );

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

			StaminaThreshold = Config.Bind(
				"3 - Food Effects",
				"StaminaThreshold",
				0,
				"Warn the player when their maximum stamina drops to or below this amount." );

			StomachNagFrequency = Config.Bind(
				"3 - Food Effects",
				"StomachNagFrequency",
				5,
				"Minutes between empty stomach nag messages." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();
			}
		}
	}
}
