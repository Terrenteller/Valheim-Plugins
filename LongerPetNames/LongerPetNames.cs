using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace LongerPetNames
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.longerpetnames" , "Longer Pet Names" , "1.0.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class LongerPetNames : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > LoadOnStart;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.longerpetnames" );

		private void Awake()
		{
			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Determines if this mod loads on game start." );

			if( LoadOnStart.Value )
				Harmony.PatchAll();
		}
	}
}
