#if PrototypeToggle

using HarmonyLib;

namespace BorderlessWindowed
{
	public partial class BorderlessWindowed
	{
		[HarmonyPatch( typeof( GraphicsSettingsState ) )]
		private class GraphicsSettingsStatePatch
		{
			[HarmonyPatch( "SetValue" , new[] { typeof( GraphicsSettingBool ) , typeof( bool ) } )]
			[HarmonyPrefix]
			private static bool SetValuePrefix( GraphicsSettingBool setting , bool value )
			{
				if( setting == GraphicsSettingsPatchPrototype.BorderlessWindowed )
				{
					ShowBorder.Value = !value;
					return false;
				}

				return true;
			}

			[HarmonyPatch( "GetValue" , new[] { typeof( GraphicsSettingBool ) } )]
			[HarmonyPrefix]
			private static bool GetValuePrefix( ref bool __result , GraphicsSettingBool setting )
			{
				if( setting == GraphicsSettingsPatchPrototype.BorderlessWindowed )
				{
					// Too much bother to modify or monitor the GraphicsSettingsState in question.
					// Just use what we have.
					__result = !ShowBorder.Value;
					return false;
				}

				return true;
			}
		}
	}
}

#endif
