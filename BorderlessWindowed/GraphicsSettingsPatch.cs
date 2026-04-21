using GUIFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace BorderlessWindowed
{
	public partial class BorderlessWindowed
	{
		[HarmonyPatch( typeof( GraphicsSettings ) )]
		internal class GraphicsSettingsPatch
		{
			private static int LastWindowTypeIndex = -1;
			private static bool SuppressConfigSync = false;
			private static WeakReference< Transform > WeakResolutionsRowTransform = new WeakReference< Transform >( null );
			private static WeakReference< GuiDropdown > WeakWindowTypeDropdown = new WeakReference< GuiDropdown >( null );
			private static List< string > WindowTypeStrings = new List< string >( new[] { "Fullscreen" , "Borderless Windowed" , "Windowed" } );
			private static int WindowTypeFullscreen = 0;
			private static int WindowTypeBorderlessWindowed = 1;
			private static int WindowTypeWindowed = 2;

			internal static void SynchronizeWithConfigEntry( GuiDropdown dropdown )
			{
				if( SuppressConfigSync )
					return;

				if( dropdown != null || ( WeakWindowTypeDropdown.TryGetTarget( out dropdown ) && dropdown != null ) )
				{
					int index = Screen.fullScreen
						? WindowTypeFullscreen
						: ( ShowBorder.Value ? WindowTypeWindowed : WindowTypeBorderlessWindowed );

					dropdown.value = index;
				}
			}

			[HarmonyPatch( "CreateDynamicGraphicsQualitySettings" )]
			[HarmonyPostfix]
			private static void CreateDynamicGraphicsQualitySettingsPostfix(
				ref GameObject ___m_qualityTogglePrefab,
				ref List< QualityToggleData > ___m_qualityToggles,
				ref List< Toggle > ___m_dynamicQualityToggles )
			{
				// Find things. But why is the parent not the "Grid" on "QualityToggles"?
				Transform graphicsTabVerticalLayoutTransform = ___m_qualityToggles.First().m_toggle.transform.parent;
				Transform resolutionsRowTransform = graphicsTabVerticalLayoutTransform.Find( "Resolution" ).Find( "Anchor" );
				Transform fullscreenTransform = resolutionsRowTransform.Find( "Fullscreen" );
				GuiToggle fullscreenToggleComp = fullscreenTransform.GetComponent< GuiToggle >();
				Transform resolutions = resolutionsRowTransform.Find( "Resolutions" );

				// Duplicate the resolutions dropdown
				Transform windowTypeDropdown = Instantiate( resolutions );
				windowTypeDropdown.name = "WindowType";
				// The top-level game object sizes things correctly but the child label is unnecessary
				Destroy( windowTypeDropdown.Find( "Label" ).gameObject );

				// Wire it up
				GuiDropdown windowTypeDropdownComp = windowTypeDropdown.Find( "Resolution" ).GetComponent< GuiDropdown >();
				windowTypeDropdownComp.ClearOptions(); // Toss the dummy 0x0 resolution option
				windowTypeDropdownComp.AddOptions( WindowTypeStrings );
				SynchronizeWithConfigEntry( windowTypeDropdownComp ); // Order is important
				windowTypeDropdownComp.onValueChanged.AddListener( ( index ) =>
				{
					// Update the option we're posing as. Vanilla logic is still good.
					// But first, toggle it to coax the "Test" button into the right state
					// by causing the onValueChanged event to run our ResolutionSettingsChangedPrefix().
					fullscreenToggleComp.isOn = !fullscreenToggleComp.isOn;
					fullscreenToggleComp.isOn = index == 0;
				} );

				// Take some notes
				LastWindowTypeIndex = windowTypeDropdownComp.value;
				WeakResolutionsRowTransform.SetTarget( resolutionsRowTransform );
				WeakWindowTypeDropdown.SetTarget( windowTypeDropdownComp );

				// This implicitly makes windowTypeDropdown the last sibling so it draws over the toggle
				windowTypeDropdown.SetParent( resolutionsRowTransform , false );
				// Position the new dropdown between the resolutions dropdown and the test button.
				// This value is very much a magic number.
				windowTypeDropdown.transform.localPosition += new Vector3( 190.0f , 0.0f , 0.0f );
			}

			[HarmonyPatch( "OnResSwitchOK" )]
			[HarmonyPostfix]
			private static void OnResSwitchOKPostfix()
			{
				if( WeakWindowTypeDropdown.TryGetTarget( out GuiDropdown dropdown ) && dropdown != null )
				{
					LastWindowTypeIndex = dropdown.value;
				}
			}

			[HarmonyPatch( "OnTestResolution" )]
			[HarmonyPostfix]
			private static void OnTestResolutionPostfix()
			{
				if( WeakWindowTypeDropdown.TryGetTarget( out GuiDropdown dropdown ) && dropdown != null )
				{
					SuppressConfigSync = true;
					ShowBorder.Value = dropdown.value != WindowTypeBorderlessWindowed;
					SuppressConfigSync = false;
				}
			}

			[HarmonyPatch( "ResolutionSettingsChanged" )]
			[HarmonyPrefix]
			private static bool ResolutionSettingsChangedPrefix( ref bool __result )
			{
				if( WeakWindowTypeDropdown.TryGetTarget( out GuiDropdown dropdown ) && dropdown != null )
				{
					// This reads poorly. Settings have changed if we're showing the border
					// when the option is to not show the border, or vice versa.
					if( ShowBorder.Value == ( dropdown.value == WindowTypeBorderlessWindowed ) )
					{
						__result = true;
						return false;
					}
				}

				return true;
			}

			[HarmonyPatch( "RevertMode" )]
			[HarmonyPostfix]
			private static void RevertModePostfix( ref GraphicsSettings __instance )
			{
				if( WeakWindowTypeDropdown.TryGetTarget( out GuiDropdown dropdown ) && dropdown != null )
				{
					// Set this first so ResolutionSettingsChangedPrefix() returns the right value
					// when the component's value is set and triggers ResolutionSettingsChanged()
					// via the fullscreen toggle
					SuppressConfigSync = true;
					ShowBorder.Value = LastWindowTypeIndex != WindowTypeBorderlessWindowed;
					SuppressConfigSync = false;
					dropdown.value = LastWindowTypeIndex;

					// Vanilla Bug: the "Test" button will persist after rejecting a change
					// to or from fullscreen because the set and check happen on the same frame.
					// Unity needs more time to react.
				}
			}
		}
	}
}
