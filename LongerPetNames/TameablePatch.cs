using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LongerPetNames
{
	public partial class LongerPetNames
	{
		[HarmonyPatch( typeof( Tameable ) )]
		private class TameablePatch
		{
			[HarmonyPatch( "SetName" )]
			[HarmonyTranspiler]
			private static IEnumerable< CodeInstruction > SetNamePatch( IEnumerable< CodeInstruction > instructionsIn )
			{
				foreach( CodeInstruction instruction in instructionsIn )
				{
					if( instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 10 )
						instruction.operand = 100;

					yield return instruction;
				}
			}
		}
	}
}
