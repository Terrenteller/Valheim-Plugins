using HarmonyLib;
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
			//private const float MinimumSpeedInTar = 0.2f;
			//private const float QuarterPi = (float)Math.PI / 4.0f; // Where is Mathf.PI?

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
				if( !IsEnabled.Value || !ApplyMovementPenalties.Value || character.m_tolerateTar )
				{
					percentInTar = 0.0f;
					return false;
				}

				StatusEffect tarEffect = GetStatusEffectByName( character.GetSEMan() , SE_StatsPatch.TarEffectName );
				float percentTarredEffect = 0.0f;
				if( tarEffect != null )
				{
					float tarEffectFactor = tarEffect.GetRemaningTime() / ( tarEffect.GetDuration() + tarEffect.GetRemaningTime() );
					percentTarredEffect = PercentInLiquidWhenSwimming * tarEffectFactor;
				}

				bool inTar = Traverse.Create( character )
					.Method( "InTar" )
					.GetValue< bool >();
				float percentTarredWorld = inTar ? PercentSubmerged( character ) : 0.0f;

				percentInTar = Mathf.Clamp01( Mathf.Max( percentTarredEffect , percentTarredWorld ) );
				return percentInTar > TarForgivenessThreshold;
			}

			// FIXME: This needs to account for movement on the ground normal
			// to give players the ability to escape tar pits without mining their way out.
			// Speed is so impacted even stairs and ladders are not viable.
			// Also, if the player is too slow, they stop rotating to face the direction in which they move.
			internal static void ModifySpeedForTar( Character character , ref float speed )
			{
				/*
				float percentInTar = 0.0f;
				if( speed <= MinimumSpeedInTar || !ShouldApplyTarMovementPenalty( character , out percentInTar ) )
					return;

				Vector3 moveDir = Traverse.Create( character ).Field( "m_moveDir" ).GetValue< Vector3 >();
				if( moveDir.magnitude <= 0.1f )
					return;

				float tarSpeedReduction = speed * Mathf.Sin( percentInTar * QuarterPi );
				Debug.Log( $"{speed} - {tarSpeedReduction} = {speed - tarSpeedReduction}" );
				speed -= tarSpeedReduction;

				if( character.IsOnGround() )
				{
					float slideAngle = Traverse.Create( character ).Method( "GetSlideAngle" ).GetValue< float >();
					Vector3 lastGroundNormal = character.IsOnGround()
						? Traverse.Create( character ).Field( "m_lastGroundNormal" ).GetValue< Vector3 >()
						: Vector3.up;
					Vector3 moveDirOnGround = Vector3.ProjectOnPlane( moveDir , lastGroundNormal );
					float upwardsSlope = 90.0f - Math.Min( 90.0f , Vector3.Angle( moveDirOnGround , Vector3.up ) );
					speed += upwardsSlope * 0.01f;
				}

				// Never let the speed reach zero. This is a game, not an extinction event.
				speed = Mathf.Max( speed , MinimumSpeedInTar );
				Debug.Log( $"Final speed: {speed}" );
				*/
			}

			[HarmonyPatch( "ApplyLiquidResistance" )]
			[HarmonyPostfix]
			private static void ApplyLiquidResistancePostfix( ref Character __instance , ref float speed )
			{
				StatusEffect tarEffect = GetStatusEffectByName( __instance.GetSEMan() , SE_StatsPatch.TarEffectName );
				if( tarEffect != null )
					return; // SE_StatsPatch.ModifySpeedPostfix() handles this case

				ModifySpeedForTar( __instance , ref speed );
			}

			[HarmonyPatch( "CheckRun" )]
			[HarmonyPrefix]
			private static bool CheckRunPrefix( ref Character __instance , ref bool __result )
			{
				// In vanilla, running in tar imposes a massive speed penalty compared to jogging, walking,
				// and even crouching for some reason. Character.ApplyLiquidResistance()
				// is ignored when tarred as well. Fortunately, we don't need to make sense of this
				// because we don't allow the player to run in tar because it's tar.
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
