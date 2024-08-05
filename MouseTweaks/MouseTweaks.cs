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
		public static ConfigEntry< bool > DebugMessages;
		//public static ConfigEntry< InsertionPriorityEnum > InsertionPriority;
		public static ConfigEntry< bool > SwapShiftAndCtrl; // SwapMoveAndSplit instead?
		public static bool InitialSwapShiftAndCtrl; // Transpiled code, "live" code, and descriptions need to stay in sync
		// 2 - Cursor Context
		public static ConfigEntry< bool > AllowFilteredStackMove;
		public static ConfigEntry< bool > AllowImmediateSplit;
		public static ConfigEntry< bool > AllowSingleSmear;
		public static ConfigEntry< bool > AllowStackMove;
		public static ConfigEntry< bool > AllowStackSmear;
		public static ConfigEntry< bool > KeepRemainderOnCursor;
		// 3 - Double-click
		public static ConfigEntry< bool > AllowDoubleClickCollect;
		public static ConfigEntry< int > SuccessiveClickRadius;
		public static ConfigEntry< float > SuccessiveClickWindow;
		// 4 - Single Drop
		public static ConfigEntry< bool > AllowRightClickDrop;
		public static ConfigEntry< KeyCode > SingleDropKey;
		// 5 - Mouse Wheel
		public static ConfigEntry< WheelActionEnum > ContainerWheelAction;
		public static ConfigEntry< WheelActionEnum > PlayerWheelAction;

		public enum WheelActionEnum
		{
			None,
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

			DebugMessages = Config.Bind(
				"1 - General",
				"DebugMessages",
				false,
				"Whether to print plugin-specific trace/debug/warning/error messages." );

			SwapShiftAndCtrl = Config.Bind(
				"1 - General",
				"SwapShiftAndCtrl",
				true,
				"Whether [SHIFT] (split) and [CTRL] (move) swap behavior. Changes will not apply until the game is restarted." );
			InitialSwapShiftAndCtrl = SwapShiftAndCtrl.Value;

			AllowFilteredStackMove = Config.Bind(
				"2 - Cursor Context",
				"AllowFilteredStackMove",
				true,
				"Whether dragging with [CTRL] + [SHIFT] + [LMB} moves stacks similar to the initial stack." );

			AllowImmediateSplit = Config.Bind(
				"2 - Cursor Context",
				"AllowImmediateSplit",
				true,
				$"Whether [{( InitialSwapShiftAndCtrl ? "SHIFT" : "CTRL" )}] + [RMB] immediately splits the stack, rounding up." );

			AllowSingleSmear = Config.Bind(
				"2 - Cursor Context",
				"AllowSingleSmear",
				true,
				"Whether dragging with [RMB] puts a single item into applicable slots." );

			AllowStackMove = Config.Bind(
				"2 - Cursor Context",
				"AllowStackMove",
				true ,
				$"Whether dragging with [{( InitialSwapShiftAndCtrl ? "SHIFT" : "CTRL" )}] + [LMB] moves stacks." );

			AllowStackSmear = Config.Bind(
				"2 - Cursor Context",
				"AllowStackSmear",
				true,
				"Whether dragging with [LMB] will divide the stack into applicable slots." );

			KeepRemainderOnCursor = Config.Bind(
				"2 - Cursor Context",
				"KeepRemainderOnCursor",
				false,
				"Whether an uneven stack smear leaves the remainder on the cursor for further action." );

			AllowDoubleClickCollect = Config.Bind(
				"3 - Double-click",
				"AllowDoubleClickCollect",
				true,
				"Whether double-clicking on a partial stack will collect similar items from other stacks." );

			SuccessiveClickRadius = Config.Bind(
				"3 - Double-click",
				"SuccessiveClickRadius",
				3,
				new ConfigDescription(
					"The range, in pixels, in which a click may count as a successor to the initial click in the series.",
					new AcceptableValueRange< int >( 0 , 10 ) ) );

			SuccessiveClickWindow = Config.Bind(
				"3 - Double-click",
				"SuccessiveClickWindow",
				0.25f,
				new ConfigDescription(
					"The interval, in seconds, in which a click may count as the successor to the previous click in the series.",
					new AcceptableValueRange< float >( 0.05f , 1.0f ) ) ); // 0.05f so configuration managers don't treat it as a percentage

			AllowRightClickDrop = Config.Bind(
				"4 - Single Drop",
				"AllowRightClickDrop",
				true,
				"Whether right-clicking with a stack outside of the inventory drops a single item from the stack." );

			SingleDropKey = Config.Bind(
				"4 - Single Drop",
				"SingleDropKey",
				KeyCode.Q, // Fortunately, "None" is the first Configuration Manager list item and there's a "Reset" button
				"Drop one item from the stack below the cursor when this key is pressed. Conflicts with auto-run by default." );

			// Unfortunate this is first alphabetically and second visually
			ContainerWheelAction = Config.Bind(
				"5 - Mouse Wheel",
				"ContainerWheelAction",
				WheelActionEnum.PushUpPullDown,
				"How items in a container will be moved when the mouse wheel is turned." );
			
			PlayerWheelAction = Config.Bind(
				"5 - Mouse Wheel",
				"PlayerWheelAction",
				WheelActionEnum.PullUpPushDown,
				"How items in the player's inventory will be moved when the mouse wheel is turned." );

			if( LoadOnStart.Value )
			{
				InitialSwapShiftAndCtrl = SwapShiftAndCtrl.Value;
				Harmony.PatchAll();
			}
		}
	}
}
