using HarmonyLib;
using System;

namespace MagneticWishbone
{
	public partial class MagneticWishbone
	{
		[HarmonyPatch( typeof( Piece.Requirement ) )]
		private class PieceRequirementPatch
		{
			[HarmonyPatch( "GetAmount" )]
			[HarmonyPrefix]
			private static bool GetAmountPrefix( ref int __result , ref Piece.Requirement __instance , ref int qualityLevel )
			{
				if( __instance is CustomRequirement )
				{
					__result = ( (CustomRequirement)__instance ).GetCustomAmount( qualityLevel );
					return false;
				}

				return true;
			}
		}
	}
}
