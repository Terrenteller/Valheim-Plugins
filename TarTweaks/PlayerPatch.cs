using HarmonyLib;
using UnityEngine;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( Player ) )]
		class PlayerPatch
		{
			private const string PoisonEffectName = "Poison";
			private static int PoisonEffectNameHash = PoisonEffectName.GetHashCode();

			[HarmonyPatch( "Dodge" )]
			[HarmonyPrefix]
			private static bool DodgePrefix( ref Player __instance )
			{
				return !CharacterPatch.ShouldApplyTarMovementPenalty( __instance , out _ );
			}

			[HarmonyPatch( "OnSwimming" )]
			[HarmonyPostfix]
			private static void OnSwimmingPostfix( ref Player __instance , ref float ___m_drownDamageTimer )
			{
				if( CharacterPatch.ShouldApplyTarMovementPenalty( __instance , out _ )
					&& !__instance.HaveStamina()
					&& ___m_drownDamageTimer == 0.0f )
				{
					SEMan statusManager = __instance.GetSEMan();
					SE_Poison poisonStatus = statusManager.GetStatusEffect( PoisonEffectNameHash ) as SE_Poison;
					if( !poisonStatus )
					{
						poisonStatus = statusManager.AddStatusEffect( PoisonEffectNameHash ) as SE_Poison;

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
