using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace CustomSlotItemLib
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.customslotitemlib" , "CustomSlotItemLib" , "1.1.1" )]
	[BepInProcess( "valheim.exe" )]
	public partial class CustomSlotItemLib : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< string > ItemSlotPairs;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.customslotitemlib" );

		private void Awake()
		{
			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Whether this plugin loads on game start." );

			// The original WishboneSlot and WisplightSlot plugins used these slot strings
			ItemSlotPairs = Config.Bind(
				"1 - General",
				"ItemSlotPairs",
				"Demister,wisplight;Wishbone,wishbone",
				"\"ItemName1,SlotName;...;ItemNameN,SlotName\"\nMultiple items can go in the same slot (not all at once), but an item cannot go in multiple slots.\nThe game must be restarted for changes to take effect." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
