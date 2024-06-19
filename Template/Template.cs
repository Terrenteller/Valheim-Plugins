using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

// This is a template project for copying or prototyping. To copy:
// 1. Duplicate the top-level "Template" directory outside of Visual Studio
// 2. Generate a new GUID in .../Properties/AssemblyInfo.cs
// 3. Add the new project to the solution
// 4. Replace all instances of "Template" with a real name, minding capitalization and spaces
// 5. Add a listing to the main README

namespace Template
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.template" , "Template" , "1.0.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class Template : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.template" );

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

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
