using HarmonyLib;
using System;
using UnityEngine;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( Character ) )]
		private class CharacterPatch
		{
			private const float TarForgivenessThreshold = 0.05f;
			private const float PercentInLiquidWhenSwimming = 0.85f;
			private const float MinimumSpeedInTar = 0.2f;
			private const float QuarterPi = (float)Math.PI / 4.0f; // Where is Mathf.PI?

			internal static float PercentSubmerged( Character character )
			{
				float characterHeight = character.GetCollider().height;
				float depthInLiquid = Mathf.Max( 0f , character.GetLiquidLevel() - character.transform.position.y );

				return characterHeight != 0.0f
					? Mathf.Clamp01( depthInLiquid / characterHeight )
					: ( depthInLiquid > 0.0f ? 1.0f : 0.0f );
			}

			internal static bool ShouldApplyTarMovementPenalty( Character character , out float percentInTar )
			{
				if( !IsEnabled.Value || character.m_tolerateTar )
				{
					percentInTar = 0.0f;
					return false;
				}

				StatusEffect tarEffect = character.GetSEMan().GetStatusEffect( "Tared" );
				float percentTarredEffect = 0.0f;
				if( tarEffect != null )
				{
					float tarEffectFactor = tarEffect.GetRemaningTime() / ( tarEffect.GetDuration() + tarEffect.GetRemaningTime() );
					percentTarredEffect = PercentInLiquidWhenSwimming * tarEffectFactor;
				}
				float percentTarredWorld = character.InTar() ? PercentSubmerged( character ) : 0.0f;

				percentInTar = Mathf.Max( percentTarredEffect , percentTarredWorld );
				return percentInTar > TarForgivenessThreshold;
			}

			// FIXME: This needs to account for movement on the ground normal
			// to give players the ability to escape tar pits without mining their way out.
			// Speed is so impacted even stairs and ladders are not viable.
			// Arguably, sticky tar makes it easier to scale knee-high mountains.
			internal static void ModifySpeedForTar( Character character , ref float speed )
			{
				if( speed > MinimumSpeedInTar && ShouldApplyTarMovementPenalty( character , out float percentInTar ) )
				{
					speed -= ( speed * Mathf.Sin( percentInTar * QuarterPi ) );
					speed = Mathf.Max( speed , MinimumSpeedInTar );
				}
			}

			[HarmonyPatch( "ApplyLiquidResistance" )]
			[HarmonyPostfix]
			private static void ApplyLiquidResistancePostfix( ref Character __instance , ref float speed )
			{
				StatusEffect tarEffect = __instance.GetSEMan().GetStatusEffect( "Tared" );
				if( tarEffect != null )
					return; // SE_StatsPatch.ModifySpeedPostfix() handles this case

				ModifySpeedForTar( __instance , ref speed );
			}

			[HarmonyPatch( "CheckRun" )]
			[HarmonyPrefix]
			private static bool CheckRunPrefix( ref Character __instance , ref bool __result )
			{
				// In vanilla, running in tar imposes a massive speed penalty compared to jogging, walking,
				// and even crouching for some reason. Alternatively, Character.ApplyLiquidResistance()
				// is ignored when tarred. Fortunately, we don't need to make sense of this because
				// we don't allow the player to run in tar because it's tar.
				if( ShouldApplyTarMovementPenalty( __instance , out _ ) )
				{
					__result = false;
					return false;
				}

				return true;
			}

			[HarmonyPatch( "Jump" )]
			[HarmonyPrefix]
			private static bool JumpPrefix( ref Character __instance )
			{
				return !ShouldApplyTarMovementPenalty( __instance , out _ );
			}
		}
	}
}
