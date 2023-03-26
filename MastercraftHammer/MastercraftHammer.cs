using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MastercraftHammer
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.mastercrafthammer" , "Mastercraft Hammer" , "1.0.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class MastercraftHammer : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > RandomizeStateOnPlacement;
		public static ConfigEntry< bool > RandomizeStateOnRepair;
		// 2 - Filter
		public static ConfigEntry< bool > IncludeHardWood;
		public static ConfigEntry< bool > IncludeIron;
		public static ConfigEntry< bool > IncludeMarble;
		public static ConfigEntry< bool > IncludeStone;
		public static ConfigEntry< bool > IncludeWood;
		// 3 - Single Touch
		public static ConfigEntry< bool > EnableSingleTouch;
		public static ConfigEntry< bool > ResetSingleTouch;
		// 4 - New
		public static ConfigEntry< bool > AllowNew;
		public static ConfigEntry< int > NewRatio;
		// 5 - Worn
		public static ConfigEntry< bool > AllowWorn;
		public static ConfigEntry< int > WornRatio;
		// 6 - Broken
		public static ConfigEntry< bool > AllowBroken;
		public static ConfigEntry< int > BrokenRatio;
		// 7 - Destruction
		public static ConfigEntry< bool > AllowDestruction;
		public static ConfigEntry< int > DestructionRatio;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.mastercrafthammer" );

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

			RandomizeStateOnPlacement = Config.Bind(
				"1 - General",
				"RandomizeOnPlacement",
				false,
				"Affect objects newly placed into the world." );

			// This can destroy entire buildings with a single click and possibly crash the game
			// when used with an area repair mod! It should never work in multiplayer!
			RandomizeStateOnRepair = Config.Bind(
				"1 - General",
				"RandomizeOnRepair",
				false,
				"Affect objects undergoing repairs. Only works in singleplayer!" );

			IncludeHardWood = Config.Bind(
				"2 - Filter",
				"IncludeHardWood",
				true,
				"Affect hardwood objects." );

			IncludeIron = Config.Bind(
				"2 - Filter",
				"IncludeIron",
				true,
				"Affect iron objects." );

			IncludeMarble = Config.Bind(
				"2 - Filter",
				"IncludeMarble",
				true,
				"Affect marble objects." );

			IncludeStone = Config.Bind(
				"2 - Filter",
				"IncludeStone",
				true,
				"Affect stone objects." );

			IncludeWood = Config.Bind(
				"2 - Filter",
				"IncludeWood",
				true,
				"Affect wooden objects." );

			EnableSingleTouch = Config.Bind(
				"3 - Single Touch",
				"EnableSingleTouch",
				false,
				"Prevent objects from being affected more than once." );

			ResetSingleTouch = Config.Bind(
				"3 - Single Touch",
				"ResetSingleTouch",
				false,
				"Toggle to true to reset the list of single-touched objects." );

			AllowNew = Config.Bind(
				"4 - New",
				"AllowNew",
				true,
				"Whether objects may be fully restored." );

			NewRatio = Config.Bind(
				"4 - New",
				"NewRatio",
				1,
				"The Value:X chance, where X is the sum of allowed ratios, the object will be fully restored." );

			AllowWorn = Config.Bind(
				"5 - Worn",
				"AllowWorn",
				true,
				"Whether objects may be made to appear worn." );

			WornRatio = Config.Bind(
				"5 - Worn",
				"WornRatio",
				1,
				"The Value:X chance, where X is the sum of allowed ratios, the object will be made to appear worn." );

			AllowBroken = Config.Bind(
				"6 - Broken",
				"AllowBroken",
				true,
				"Whether objects may be made to appear broken." );

			BrokenRatio = Config.Bind(
				"6 - Broken",
				"BrokenRatio",
				1,
				"The Value:X chance, where X is the sum of allowed ratios, the object will be made to appear broken." );

			AllowDestruction = Config.Bind(
				"7 - Destruction",
				"AllowDestruction",
				false,
				"Whether objects may be destroyed completely. Use with caution! There is no undo." );

			DestructionRatio = Config.Bind(
				"7 - Destruction",
				"DestructionRatio",
				0,
				"The Value:X chance, where X is the sum of allowed ratios, the object will be destroyed." );

			if( LoadOnStart.Value )
			{
				Harmony.PatchAll();

				AllowDestruction.Value = false;
				DestructionRatio.Value = 0;
				ResetSingleTouch.Value = false;

				Config.SettingChanged += Config_SettingChanged;
			}
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == ResetSingleTouch && ResetSingleTouch.Value )
			{
				WearNTearPatch.WornNTorn.Clear();
				StartCoroutine( "DelayResetResetSingleTouch" );
			}
		}

		private IEnumerator DelayResetResetSingleTouch()
		{
			yield return new WaitForSecondsRealtime( 0.1f );

			ResetSingleTouch.Value = false;
		}
	}
}
