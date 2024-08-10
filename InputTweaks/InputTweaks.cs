using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace InputTweaks
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.inputtweaks" , "Input Tweaks" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class InputTweaks : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > DebugMessages;
		//public static ConfigEntry< InsertionPriorityEnum > InsertionPriority; // FIXME
		public static ConfigEntry< bool > SelectSwappedItem;
		public static ConfigEntry< bool > SplitRoundsUp;
		public static ConfigEntry< bool > SwapMoveAndSplit;
		public static bool InitialSwapMoveAndSplit; // Transpiled code, "live" code, and descriptions need to stay in sync
		// 2 - Cursor Context
		// Why disable any of these? A better option is to change the required input.
		//public static ConfigEntry< bool > AllowFilteredStackMove;
		//public static ConfigEntry< bool > AllowImmediateSplit;
		//public static ConfigEntry< bool > AllowSingleSmear;
		//public static ConfigEntry< bool > AllowStackMove;
		//public static ConfigEntry< bool > AllowStackSmear;
		public static ConfigEntry< bool > KeepRemainderOnCursor;
		// 3 - Double-click
		public static ConfigEntry< bool > AllowDoubleClickCollect;
		public static ConfigEntry< int > SuccessiveClickRadius;
		public static ConfigEntry< float > SuccessiveClickWindow;
		// 4 - Drop Shortcuts
		public static ConfigEntry< bool > AllowRightClickDrop;
		public static ConfigEntry< KeyCode > SingleDropKey;
		public static ConfigEntry< ModifierKeyEnum > StackDropModifier;
		// 5 - Mouse Wheel
		public static ConfigEntry< WheelActionEnum > ContainerWheelAction;
		public static ConfigEntry< WheelActionEnum > PlayerWheelAction;

		public enum WheelActionEnum
		{
			None,
			PullUpPushDown,
			PushUpPullDown
		}
		
		public enum ModifierKeyEnum
		{
			None,
			Alt,
			Command,
			Ctrl,
			Move,
			Shift,
			Split
		}
		
		/*
		public enum InsertionPriorityEnum
		{
			Default,
			TopLeft,
			BottomLeft
		}
		*/

		private readonly Harmony Harmony = new Harmony( "com.riintouge.inputtweaks" );

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

			DebugMessages = Config.Bind(
				"1 - General",
				"DebugMessages",
				false,
				"Whether to print plugin-specific trace/debug/warning/error messages." );

			SelectSwappedItem = Config.Bind(
				"1 - General",
				"SelectSwappedItem",
				false,
				"Whether selecting a second item with an item already selected will automatically select the second item after the swap. This mimics Minecraft's transient cursor item slot." );
			
			SplitRoundsUp = Config.Bind(
				"1 - General",
				"SplitRoundsUp",
				true,
				"Whether stack splits round up when the stack has an odd number of items." );

			SwapMoveAndSplit = Config.Bind(
				"1 - General",
				"SwapMoveAndSplit",
				true,
				"Whether [SHIFT] as split and [CTRL] as move swap behavior. Changes will not apply until the game is restarted." );
			InitialSwapMoveAndSplit = SwapMoveAndSplit.Value;

			/*
			AllowFilteredStackMove = Config.Bind(
				"2 - Cursor Context",
				"AllowFilteredStackMove",
				true,
				"Whether dragging with [CTRL] + [SHIFT] + [LMB} moves stacks similar to the initial stack." );

			AllowImmediateSplit = Config.Bind(
				"2 - Cursor Context",
				"AllowImmediateSplit",
				true,
				$"Whether [{( InitialSwapMoveAndSplit ? "SHIFT" : "CTRL" )}] + [RMB] immediately splits the stack, rounding up." );

			AllowSingleSmear = Config.Bind(
				"2 - Cursor Context",
				"AllowSingleSmear",
				true,
				"Whether dragging with [RMB] puts a single item into applicable slots." );

			AllowStackMove = Config.Bind(
				"2 - Cursor Context",
				"AllowStackMove",
				true,
				$"Whether dragging with [{( InitialSwapMoveAndSplit ? "SHIFT" : "CTRL" )}] + [LMB] moves stacks." );

			AllowStackSmear = Config.Bind(
				"2 - Cursor Context",
				"AllowStackSmear",
				true,
				"Whether dragging with [LMB] will divide the stack into applicable slots." );
			*/

			KeepRemainderOnCursor = Config.Bind(
				"2 - Cursor Context",
				"KeepRemainderOnCursor",
				false,
				"Whether an unbalanced stack smear leaves the remainder on the cursor for further action." );

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

			// FIXME: Disabling this doesn't restore vanilla behaviour because we don't remove the listener (or component)
			AllowRightClickDrop = Config.Bind(
				"4 - Drop Shortcuts",
				"AllowRightClickDrop",
				true,
				"Whether right-clicking with a stack outside of the inventory drops a single item from the stack." );

			SingleDropKey = Config.Bind(
				"4 - Drop Shortcuts",
				"SingleDropKey",
				KeyCode.Q, // Conflicts OOTB, but "None" is the first Configuration Manager list item and there's a "Reset" button
				"Drop one item from the stack below the cursor when this key is pressed. Conflicts with auto-run by default." );

			StackDropModifier = Config.Bind(
				"4 - Drop Shortcuts",
				"StackDropModifier",
				ModifierKeyEnum.Move,
				"Drop the entire stack below the cursor when this modifier is pressed when SingleDropKey is pressed." );

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
				InitialSwapMoveAndSplit = SwapMoveAndSplit.Value;
				Harmony.PatchAll();
			}
		}
	}
}
