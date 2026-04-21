// This adds an immediate toggle option to the graphics settings page.
// The dropdown makes more UX sense because the window styles are mutually exclusive.
// This code may be useful in the future, possibly as reference material
// for a redesigned settings UI around the official 1.0 release.
#if PrototypeToggle

using GUIFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace BorderlessWindowed
{
	public partial class BorderlessWindowed
	{
		[HarmonyPatch( typeof( GraphicsSettings ) )]
		internal class GraphicsSettingsPatchPrototype
		{
			public static GraphicsSettingBool BorderlessWindowed { get { return (GraphicsSettingBool)( 1 << BorderlessWindowedIndex.Value ); } }

			private static WeakReference< GuiToggle > WeakFullscreenToggle = new WeakReference< GuiToggle >( null );

			private static Lazy< int > BorderlessWindowedIndex = new Lazy< int >( () =>
			{
				int offset = 31; // Try to be the last
				if( Enum.IsDefined( typeof( GraphicsSettingBool ) , (GraphicsSettingBool)( 1 << offset ) ) )
				{
					// Last bit is taken. Look for the last unused bit.
					while( offset > 0 )
					{
						offset -= 1;
						if( !Enum.IsDefined( typeof( GraphicsSettingBool ) , (GraphicsSettingBool)( 1 << offset ) ) )
							return offset;
					}
				}
				else
				{
					// Last bit is not taken. Look for the first unused bit after the last used bit.
					while( offset > 0 )
					{
						offset -= 1;
						if( Enum.IsDefined( typeof( GraphicsSettingBool ) , (GraphicsSettingBool)( 1 << offset ) ) )
							return offset + 1;
					}
				}

				throw new IndexOutOfRangeException( "All GraphicsSettingBool enum values are used. Please report this to the plugin developer." );
			} );

			internal static void SynchronizeWithConfigEntry( GuiToggle toggle , bool notify )
			{
				if( toggle != null || ( WeakFullscreenToggle.TryGetTarget( out toggle ) && toggle != null ) )
					toggle.isOn = !ShowBorder.Value;
			}

			[HarmonyPatch( "CreateDynamicGraphicsQualitySettings" )]
			[HarmonyPostfix]
			private static void CreateDynamicGraphicsQualitySettingsPostfix(
				ref GameObject ___m_qualityTogglePrefab,
				ref List< QualityToggleData > ___m_qualityToggles,
				ref List< Toggle > ___m_dynamicQualityToggles )
			{
				// Why is the parent not the "Grid" on "QualityToggles"?
				Transform graphicsTabVerticalLayoutTransform = ___m_qualityToggles.First().m_toggle.transform.parent;

				// The effect of removing the spacers is subtle, but lifts the last (new) row
				// of toggle options out of the way of dialog buttons
				for( int index = 0 ; index < graphicsTabVerticalLayoutTransform.childCount ; index++ )
				{
					Transform child = graphicsTabVerticalLayoutTransform.GetChild( index );
					if( child.name == "Space" )
						child.gameObject.SetActive( false );
				}

				// Taken from Valheim.SettingsGui.GraphicsSettings.CreateDynamicGraphicsQualitySettings()
				GameObject prefab = Instantiate( ___m_qualityTogglePrefab , ___m_qualityTogglePrefab.transform.parent );
				prefab.transform.SetSiblingIndex( ___m_qualityTogglePrefab.transform.GetSiblingIndex() );
				GuiToggle guiToggle = prefab.GetComponentInChildren< GuiToggle >();
				guiToggle.transform.Find( "Label" ).GetComponent< TMP_Text >().text = "Borderless windowed";
				prefab.SetActive( true );
				___m_dynamicQualityToggles.Add( guiToggle );
				___m_qualityToggles.Add( new QualityToggleData( BorderlessWindowed , guiToggle ) );
			}

			[HarmonyPatch( "ModifySetting" , new[] { typeof( GraphicsSettingBool ) , typeof( bool ) } )]
			[HarmonyPrefix]
			private static bool ModifySettingPrefix( GraphicsSettingBool setting , bool value )
			{
				if( setting == BorderlessWindowed )
				{
					ShowBorder.Value = !value;
					return false;
				}

				return true;
			}
		}
	}
}

#endif
