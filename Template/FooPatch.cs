using HarmonyLib;
using System.Collections.Generic;

namespace Template
{
	public partial class Template
	{
		/*
		[HarmonyPatch( typeof( Foo ) )]
		private class FooPatch
		{
			[HarmonyPatch( "Bar" )]
			[HarmonyPrefix]
			private static void BarPrefix()
			{
				// TODO: Check IsEnabled.Value and other pre-conditions before proceeding
			}

			[HarmonyPatch( "Bar" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > BarPatch( IEnumerable< CodeInstruction > instructionsIn )
			{
				foreach( CodeInstruction instruction in instructionsIn )
					yield return instruction;
			}

			[HarmonyPatch( "Bar" )]
			[HarmonyPostfix]
			private static void BarPostfix( ref bool __state )
			{
				// TODO: Check IsEnabled.Value and other pre-conditions before proceeding
			}
		}
		*/
	}
}
