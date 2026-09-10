using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace SafetyNet
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.safetynet" , "Safety Net" , "1.0.1" )]
	[BepInProcess( "valheim.exe" )]
	public partial class SafetyNet : BaseUnityPlugin
	{
		public static SafetyNet Instance = null;
		//public static WireframeMeshFactory WireframeMeshFactory = new WireframeMeshFactory();

		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		public static ConfigEntry< bool > LoadOnStart;
		// 1 - General
		public static ConfigEntry< int > LatitudeFidelity;
		public static ConfigEntry< int > LongitudeFidelity;
		public static ConfigEntry< int > MaximumObjects;
		public static ConfigEntry< float > UpdateFrequencyInSeconds;
		public static ConfigEntry< KeyCode > VisiblityToggleKey;
		private static Coroutine UpdateRenderCoroutine = null;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.safetynet" );

		private void Awake()
		{
			IsEnabled = Config.Bind(
				"0 - Core",
				"Enable",
				false, // This plugin is visual clutter when not wanted
				"Whether this plugin has any effect when loaded." );

			LoadOnStart = Config.Bind(
				"0 - Core",
				"LoadOnStart",
				true,
				"Whether this plugin loads on game start." );

			LatitudeFidelity = Config.Bind(
				"1 - General",
				"LatitudeFidelity",
				2,
				new ConfigDescription(
					"The number of additional latitudes per hemisphere.",
					new AcceptableValueRange< int >( 0 , 20 ) ) );

			LongitudeFidelity = Config.Bind(
				"1 - General",
				"LongitudeFidelity",
				2,
				new ConfigDescription(
					"The number of additional longitudes per sphere quadrant.",
					new AcceptableValueRange< int >( 0 , 20 ) ) );

			MaximumObjects = Config.Bind(
				"1 - General",
				"MaximumObjects",
				3,
				new ConfigDescription(
					"The maximum number of objects for which to show safety visuals.",
					new AcceptableValueRange< int >( 1 , 20 ) ) );

			UpdateFrequencyInSeconds = Config.Bind(
				"1 - General",
				"UpdateFrequencyInSeconds",
				1000.0f,
				new ConfigDescription(
					"How often to recalculate visibility when enabled, in seconds.",
					new AcceptableValueRange< float >( 0.1f , 5.0f ) ) );

			VisiblityToggleKey = Config.Bind(
				"1 - General",
				"VisiblityToggleKey",
				KeyCode.F7, // Same as Minecraft light level mods and plugins
				"The key to toggle wireframe visibility (effectively toggling IsEnabled)." );

			if( LoadOnStart.Value )
			{
				Instance = this;
				Harmony.PatchAll();

				Config.SettingChanged += Config_SettingChanged;
				UpdateRenderCoroutine = Instance.StartCoroutine( CoUpdateNetRendering() );
			}
		}

		private void OnDestroy()
		{
			if( UpdateRenderCoroutine != null )
				Instance.StopCoroutine( UpdateRenderCoroutine );

			Harmony.UnpatchSelf();
		}

		private void Config_SettingChanged( object sender , SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == LatitudeFidelity || e.ChangedSetting == LongitudeFidelity )
			{
				SafetyNetBehaviour.WireframeMeshFactory.latitudeFidelity = LatitudeFidelity.Value;
				SafetyNetBehaviour.WireframeMeshFactory.longitudeFidelity = LongitudeFidelity.Value;
				SafetyNetBehaviour.UpdateMeshes();
			}
		}

		private IEnumerator CoUpdateNetRendering()
		{
			while( true )
			{
				if( !IsEnabled.Value )
				{
					// If the update is slow, we could be slow to turn back on.
					// Check more often so the player isn't left waiting.
					// TODO: Be smarter with the coroutine. Add some state for safe toggling.
					yield return new WaitForSecondsRealtime( 0.5f );
					continue;
				}

				int enabledNets = 0;
				foreach( SafetyNetBehaviour safetyNet in SafetyNetBehaviour.UpdateRenderConsiderationsAndSort() )
				{
					bool enable = enabledNets < MaximumObjects.Value && safetyNet.considerForRender;
					safetyNet.meshRenderer.enabled = enable;
					if( enable )
						enabledNets++;

					CircleProjector circleProjector = safetyNet.projector;
					if( circleProjector )
					{
						circleProjector.enabled = enable;
						circleProjector.gameObject.SetActive( enable );
					}
				}

				yield return new WaitForSecondsRealtime( UpdateFrequencyInSeconds.Value );
			}
		}
	}
}
