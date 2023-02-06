﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NoUnarmedCombat
{
	// Keep the version up-to-date with AssemblyInfo.cs and manifest.json!
	[BepInPlugin( "com.riintouge.nounarmedcombat" , "No Unarmed Combat" , "1.0.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class NoUnarmedCombat : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > AllowKick;
		public static ConfigEntry< bool > ToolbarEquipOnPunch;
		public static ConfigEntry< bool > UnsheatheAfterSwimming;
		public static ConfigEntry< bool > UnsheatheOnPunch;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.nounarmedcombat" );

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

			// Kicking by accident is rarely a problem so we provide a separate toggle
			AllowKick = Config.Bind(
				"1 - General",
				"AllowKick",
				false,
				"Allow the player to kick." );

			ToolbarEquipOnPunch = Config.Bind(
				"1 - General",
				"ToolbarEquipOnPunch",
				true,
				"Instead of punching and if nothing is sheathed, equip gear from the toolbar, left to right." );

			// Disabled by default because it may bring the player to a complete stop.
			// Exiting liquids can be difficult enough already.
			UnsheatheAfterSwimming = Config.Bind(
				"1 - General",
				"UnsheatheAfterSwimming",
				false,
				"Unsheathe equipment when exiting water that was put away upon swimming." );

			UnsheatheOnPunch = Config.Bind(
				"1 - General",
				"UnsheatheOnPunch",
				true,
				"Instead of punching, equip gear from the player's back. Takes precedence over the toolbar." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
