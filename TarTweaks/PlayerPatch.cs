using HarmonyLib;
using UnityEngine;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( Player ) )]
		class PlayerPatch
		{
			[HarmonyPatch( "Dodge" )]
			[HarmonyPrefix]
			private static bool DodgePrefix( ref Player __instance )
			{
				return !CharacterPatch.ShouldApplyTarMovementPenalty( __instance , out _ );
			}

			[HarmonyPatch( "OnSwiming" )]
			[HarmonyPostfix]
			private static void OnSwimingPostfix( ref Player __instance , ref float ___m_drownDamageTimer )
			{
				if( CharacterPatch.ShouldApplyTarMovementPenalty( __instance , out _ )
					&& !__instance.HaveStamina()
					&& ___m_drownDamageTimer == 0.0f )
				{
					SEMan statusManager = __instance.GetSEMan();
					SE_Poison poisonStatus = statusManager.GetStatusEffect( "Poison" ) as SE_Poison;
					if( !poisonStatus )
					{
						poisonStatus = statusManager.AddStatusEffect( "Poison" ) as SE_Poison;

						// Delay the on-set of poison so it doesn't overlap with drowning hitsplats
						Traverse.Create( poisonStatus )
							.Field( "m_timer" )
							.SetValue( 0.5f );
					}

					poisonStatus.m_ttl = 5.0f;
					Traverse.Create( poisonStatus )
						.Field( "m_damagePerHit" )
						.SetValue( Mathf.Ceil( __instance.GetMaxHealth() / 20f ) );

					poisonStatus.ResetTime();
				}
			}
		}
	}
}
