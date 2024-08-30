using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace SuperUltrawideSupport
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.superultrawidesupport" , "Super Ultrawide Support" , "1.1.2" )]
	[BepInProcess( "valheim.exe" )]
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		public static SuperUltrawideSupport Instance = null;

		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< string > AspectRatio;
		internal static AspectLerper Lerper = null;
		private string LastAspectRatio = "16:9";
		private static int LastAspectWidth = 16;
		private static int LastAspectHeight = 9;
		private static Coroutine ScreenSizeDetectorCoroutine = null;
		private static int LastScreenWidth = -1;
		private static int LastScreenHeight = -1;
		public static ConfigEntry< bool > FullSizeMap;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.superultrawidesupport" );

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

			// TODO: We could try to guess the size of the middle (or single) monitor based on height
			AspectRatio = Config.Bind(
				"1 - General",
				"AspectRatio",
				LastAspectRatio,
				$"The desired aspect ratio in whole numbers, like \"{LastAspectRatio}\"." );

			FullSizeMap = Config.Bind(
				"1 - General",
				"FullSizeMap",
				true,
				"Whether the map appears at full size regardless of the specified aspect ratio." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();

				UpdateAspectRatio( AspectRatio.Value );
				Config.SettingChanged += Config_SettingChanged;
				ScreenSizeDetectorCoroutine = Instance.StartCoroutine( CoCheckForScreenResize() );
			}
		}

		private void OnDestroy()
		{
			if( ScreenSizeDetectorCoroutine != null )
				Instance.StopCoroutine( ScreenSizeDetectorCoroutine );

			Harmony.UnpatchSelf();
		}

		private IEnumerator CoCheckForScreenResize()
		{
			while( true )
			{
				yield return new WaitForSecondsRealtime( 3.0f );

				if( LastScreenWidth == -1 || LastScreenHeight == -1 )
				{
					LastScreenWidth = Screen.width;
					LastScreenHeight = Screen.height;
				}
				else if( LastScreenWidth != Screen.width || LastScreenHeight != Screen.height )
				{
					LastScreenWidth = Screen.width;
					LastScreenHeight = Screen.height;

					Lerper.Update( LastScreenWidth , LastScreenHeight , LastAspectWidth , LastAspectHeight );
					MinimapPatch.Update( true );
				}
			}
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == IsEnabled )
			{
				Lerper.Update( IsEnabled.Value );
				MinimapPatch.Update( true );
			}
			else if( e.ChangedSetting == AspectRatio )
			{
				UpdateAspectRatio( AspectRatio.Value );
			}
			else if( e.ChangedSetting == FullSizeMap )
			{
				MinimapPatch.Update( true );
			}
		}

		private void UpdateAspectRatio( string value )
		{
			try
			{
				int width = 0;
				int height = 0;

				if( value == string.Empty )
				{
					width = Screen.width;
					height = Screen.height;
				}
				else
				{
					string[] aspectRatio = value.Split( ':' );
					width = int.Parse( aspectRatio[ 0 ] );
					height = int.Parse( aspectRatio[ 1 ] );
				}

				if( width > 0 && height > 0 )
				{
					LastAspectRatio = $"{width}:{height}";
					LastAspectWidth = width;
					LastAspectHeight = height;

					if( Lerper == null )
					{
						Lerper = new AspectLerper( Screen.width , Screen.height , LastAspectWidth , LastAspectHeight );
						Lerper.Update( IsEnabled.Value );
					}
					else
						Lerper.Update( Screen.width , Screen.height , LastAspectWidth , LastAspectHeight );

					MinimapPatch.Update( true );
				}
			}
			catch( Exception )
			{
				AspectRatio.Value = AspectRatio.Value != LastAspectRatio ? LastAspectRatio : string.Empty;
			}
		}
	}
}
