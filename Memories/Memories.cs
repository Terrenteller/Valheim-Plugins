using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace Memories
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.memories" , "Memories" , "1.1.0" )]
	[BepInProcess( "valheim.exe" )]
	public partial class Memories : BaseUnityPlugin
	{
		public const float VanillaDefaultZoom = 4.0f;

		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< float > CameraZoom;
		public static float LastCameraZoom;
		public static ConfigEntry< float > ShipZoom;
		public static float LastShipZoom;
		public static ConfigEntry< float > SaddleZoom;
		public static float LastSaddleZoom;
		// 2 - Interpolation
		public static ConfigEntry< float > InterpolationDuration;
		public static ConfigEntry< InterpolationTypeEnum > InterpolationType;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.memories" );

		public enum InterpolationTypeEnum
		{
			Immediate,
			Linear,
			Accelerate,
			Decelerate,
		}

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

			CameraZoom = Config.Bind(
				"1 - General",
				"CameraZoom",
				VanillaDefaultZoom,
				"Normal camera distance. This value is not updated in real-time to avoid frivolous disk I/O." );

			SaddleZoom = Config.Bind(
				"1 - General",
				"SaddleCameraZoom",
				VanillaDefaultZoom,
				"Saddle camera distance. This value is not updated in real-time to avoid frivolous disk I/O." );

			ShipZoom = Config.Bind(
				"1 - General",
				"ShipCameraZoom",
				VanillaDefaultZoom,
				"Ship camera distance. This value is not updated in real-time to avoid frivolous disk I/O." );

			InterpolationDuration = Config.Bind(
				"2 - Interpolation",
				"InterpolationDuration",
				1.5f,
				"How long it takes the camera to reposition, in seconds." );

			InterpolationType = Config.Bind(
				"2 - Interpolation",
				"InterpolationType",
				InterpolationTypeEnum.Decelerate,
				"How the camera moves when repositioning." );

			if( LoadOnStart.Value )
			{
				Harmony.PatchAll();

				LastCameraZoom = CameraZoom.Value;
				LastSaddleZoom = SaddleZoom.Value;
				LastShipZoom = ShipZoom.Value;
				Config.SettingChanged += Config_SettingChanged;
			}
		}

		private void OnDestroy()
		{
			Config.SettingChanged -= Config_SettingChanged;
			CameraZoom.Value = LastCameraZoom;
			SaddleZoom.Value = LastSaddleZoom;
			ShipZoom.Value = LastShipZoom;

			Harmony.UnpatchSelf();
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == InterpolationDuration )
			{
				if( InterpolationDuration.Value < 0.0f )
					InterpolationDuration.Value = 0.0f;
				else if( InterpolationDuration.Value > 5.0f )
					InterpolationDuration.Value = 5.0f;
			}
			else if( e.ChangedSetting == CameraZoom )
			{
				CameraZoom.Value = LastCameraZoom;
			}
			else if( e.ChangedSetting == SaddleZoom )
			{
				SaddleZoom.Value = LastSaddleZoom;
			}
			else if( e.ChangedSetting == ShipZoom )
			{
				ShipZoom.Value = LastShipZoom;
			}
		}
	}
}
