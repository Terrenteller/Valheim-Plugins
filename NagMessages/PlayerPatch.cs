using HarmonyLib;
using System.Collections.Generic;

namespace NagMessages
{
	public partial class NagMessages
	{
		[HarmonyPatch( typeof( Player ) )]
		private class PlayerPatch
		{
			[HarmonyPatch( "ActivateGuardianPower" )]
			[HarmonyPrefix]
			private static void ActivateGuardianPowerPrefix( ref Player __instance , out float __state )
			{
				__state = __instance.m_guardianPowerCooldown;
			}

			[HarmonyPatch( "ActivateGuardianPower" )]
			[HarmonyPostfix]
			private static void ActivateGuardianPowerPostfix(
				ref Player __instance,
				ref StatusEffect ___m_guardianSE,
				ref float __state )
			{
				if( __instance.m_guardianPowerCooldown != __state && ___m_guardianSE )
				{
					StatusEffect power = __instance.GetSEMan().GetStatusEffect( ___m_guardianSE.NameHash() );
					if( power )
						Instance.NagAboutPower( power.GetRemaningTime() , true );
				}
			}

			[HarmonyPatch( "SetGuardianPower" )]
			[HarmonyPostfix]
			private static void SetGuardianPowerPostfix()
			{
				Instance.NagAboutPower();
			}

			[HarmonyPatch( "SetMaxEitr" )]
			[HarmonyPrefix]
			private static void SetMaxEitrPrefix( ref Player __instance , float eitr )
			{
				float maxEitr = __instance.GetMaxEitr();
				if( IsEnabled.Value
					&& eitr < maxEitr
					&& eitr <= EitrThreshold.Value
					&& maxEitr > EitrThreshold.Value )
				{
					__instance.Message( MessageHud.MessageType.Center , "Your maximum eitr is getting too low!" );
				}
			}

			[HarmonyPatch( "SetMaxHealth" )]
			[HarmonyPrefix]
			private static void SetMaxHealthPrefix( ref Player __instance , float health )
			{
				float maxHealth = __instance.GetMaxHealth();
				if( IsEnabled.Value
					&& health < maxHealth
					&& health <= HealthThreshold.Value
					&& maxHealth > HealthThreshold.Value )
				{
					__instance.Message( MessageHud.MessageType.Center , "Your maximum health is getting too low!" );
				}
			}

			[HarmonyPatch( "SetMaxStamina" )]
			[HarmonyPrefix]
			private static void SetMaxStaminaPrefix( ref Player __instance , float stamina )
			{
				float maxStamina = __instance.GetMaxStamina();
				if( IsEnabled.Value
					&& stamina < maxStamina
					&& stamina <= StaminaThreshold.Value
					&& maxStamina > StaminaThreshold.Value )
				{
					__instance.Message( MessageHud.MessageType.Center , "Your maximum stamina is getting too low!" );
				}
			}

			[HarmonyPatch( "UpdateFood" )]
			[HarmonyPrefix]
			private static void UpdateFoodPrefix( ref List< Player.Food > ___m_foods , out int __state )
			{
				__state = ___m_foods.Count;
			}

			[HarmonyPatch( "UpdateFood" )]
			[HarmonyPostfix]
			private static void UpdateFoodPostfix( ref List< Player.Food > ___m_foods , ref int __state )
			{
				if( ___m_foods.Count == 0 && __state > 0 )
					Instance.NagAboutHunger();
			}
		}
	}
}
