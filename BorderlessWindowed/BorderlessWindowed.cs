using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Runtime.InteropServices;

namespace BorderlessWindowed
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.borderlesswindowed" , "Borderless Windowed" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class BorderlessWindowed : BaseUnityPlugin
	{
		public static BorderlessWindowed Instance = null;

		// 0 - Core
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< bool > ShowBorder;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.borderlesswindowed" );
		private readonly BorderHelper BorderHelper = new BorderHelper();

		private void Awake()
		{
			if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
				return;

			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Whether this plugin loads on game start." );

			ShowBorder = Config.Bind(
				"1 - General",
				"ShowBorder",
				true,
				"Whether the border should be shown on the game window." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();
				Config.SettingChanged += Config_SettingChanged;
			}
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == ShowBorder )
				Instance.StartCoroutine( Instance.CoUpdateBorder() );
		}

		internal IEnumerator CoUpdateBorder()
		{
			return BorderHelper.UpdateBorder( ShowBorder.Value );
		}
	}
}
