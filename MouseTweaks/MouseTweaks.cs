using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MouseTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.mousetweaks" , "Mouse Tweaks" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class MouseTweaks : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		//public static ConfigEntry< InsertionPriorityEnum > InsertionPriority;
		public static ConfigEntry< WheelActionEnum > PlayerWheelAction;
		public static ConfigEntry< WheelActionEnum > ContainerWheelAction;
		// 2 - Keybinds
		public static ConfigEntry< KeyCode > DropOneKey;
		public static ConfigEntry< bool > DropOneRightClick;

		public enum WheelActionEnum
		{
			PullUpPushDown,
			PushUpPullDown
		}
		
		/*
		public enum InsertionPriorityEnum
		{
			Default,
			TopLeft,
			BottomLeft
		}
		*/

		private readonly Harmony Harmony = new Harmony( "com.riintouge.mousetweaks" );

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

			// FIXME: Doesn't seem to work
			/*
			InsertionPriority = Config.Bind(
				"1 - General",
				"InsertionPriority",
				InsertionPriorityEnum.Default,
				"TODO" );
			*/
			
			PlayerWheelAction = Config.Bind(
				"1 - General",
				"PlayerWheelAction",
				WheelActionEnum.PullUpPushDown,
				"How items will be moved when the mouse wheel is turned." );

			ContainerWheelAction = Config.Bind(
				"1 - General",
				"ContainerWheelAction",
				WheelActionEnum.PushUpPullDown,
				"How items will be moved when the mouse wheel is turned." );

			DropOneKey = Config.Bind(
				"2 - Keybinds",
				"DropOneKey",
				KeyCode.Q,
				"Drop one item from the stack below the cursor when this key is pressed. Conflicts with auto-run by default." );

			DropOneRightClick = Config.Bind(
				"2 - Keybinds",
				"DropOneRightClick",
				true,
				"Whether right-clicking with a stack outside of the inventory drops a single item from the stack." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
