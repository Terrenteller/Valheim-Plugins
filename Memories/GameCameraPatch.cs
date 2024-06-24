using HarmonyLib;
using UnityEngine;

namespace Memories
{
	public partial class Memories
	{
		[HarmonyPatch( typeof( GameCamera ) )]
		private class GameCameraPatch
		{
			private const float HalfPI = Mathf.PI / 2.0f;
			private static CameraContext LastCameraContext = CameraContext.Generic;
			private static float FrameInitialCameraDistance = VanillaDefaultZoom;
			private static InterpolationTypeEnum LerpType = InterpolationTypeEnum.Immediate;
			private static float LerpFrom = VanillaDefaultZoom;
			private static float LerpTo = VanillaDefaultZoom;
			private static float LerpElapsed = 0.0f;
			private static float LerpDuration = 0.0f;
			private static float LerpDistanceOverride = VanillaDefaultZoom;
			private static bool IsInterpolating = false;

			public enum CameraContext
			{
				Generic,
				Saddle,
				Ship
			}

			private static CameraContext GetCameraContext( Player player )
			{
				if( player != null )
				{
					if( player.GetControlledShip() != null )
						return CameraContext.Ship;
					else if( player.IsRiding() )
						return CameraContext.Saddle;
				}

				return CameraContext.Generic;
			}

			private static void UpdateCameraCore(
				CameraContext context,
				ref float targetZoom,
				ref float ___m_distance,
				float ___m_minDistance,
				float ___m_maxDistance )
			{
				if( LastCameraContext != context )
				{
					if( InterpolationType.Value != InterpolationTypeEnum.Immediate && InterpolationDuration.Value > 0.0f )
					{
						LerpType = InterpolationType.Value;
						LerpFrom = FrameInitialCameraDistance;
						LerpTo = targetZoom;
						LerpElapsed = 0.0f;
						LerpDuration = Mathf.Max( 0.0f , InterpolationDuration.Value );
						LerpDistanceOverride = FrameInitialCameraDistance;
						IsInterpolating = true;
					}
					else
						___m_distance = Mathf.Clamp( targetZoom , ___m_minDistance , ___m_maxDistance );
				}
				else if( !IsInterpolating && targetZoom != ___m_distance )
					targetZoom = Mathf.Clamp( ___m_distance , ___m_minDistance , ___m_maxDistance );
			}

			[HarmonyPatch( "ApplySettings" )]
			[HarmonyPrefix]
			private static void ApplySettingsPrefix(
				ref float ___m_distance,
				ref float ___m_minDistance,
				ref float ___m_maxDistance,
				ref float ___m_maxDistanceBoat )
			{
				if( !IsEnabled.Value )
					return;

				LastCameraContext = GetCameraContext( Player.m_localPlayer );

				if( LastCameraContext == CameraContext.Saddle )
					___m_distance = Mathf.Clamp( LastSaddleZoom , ___m_minDistance , ___m_maxDistance );
				else if( LastCameraContext == CameraContext.Ship )
					___m_distance = Mathf.Clamp( LastShipZoom , ___m_minDistance , ___m_maxDistanceBoat );
				else
					___m_distance = Mathf.Clamp( LastCameraZoom , ___m_minDistance , ___m_maxDistance );
			}

			[HarmonyPatch( "GetCameraPosition" )]
			[HarmonyPrefix]
			private static void GetCameraPositionPrefix( ref float ___m_distance )
			{
				if( !IsEnabled.Value )
					return;

				if( IsInterpolating )
				{
					// UpdateCamera() clamps m_distance before calling GetCameraPosition().
					// We have to override the distance for the case of zooming in
					// from a distance larger than what is normally allowed.
					___m_distance = LerpDistanceOverride;
				}
				else if( LastCameraContext != GetCameraContext( Player.m_localPlayer ) )
				{
					// This runs before UpdateCameraPostfix(). We need to skip a potential clamp
					// that may cause the camera to be at the wrong position on the first frame.
					___m_distance = FrameInitialCameraDistance;
				}
			}

			[HarmonyPatch( "OnDestroy" )]
			[HarmonyPrefix]
			private static void OnDestroyPrefix( ref float ___m_distance )
			{
				IsInterpolating = false;
				LerpFrom = VanillaDefaultZoom;
				LerpTo = VanillaDefaultZoom;
				LerpElapsed = 0.0f;
				LerpDistanceOverride = VanillaDefaultZoom;
				FrameInitialCameraDistance = VanillaDefaultZoom;
				LastCameraContext = CameraContext.Generic;
			}

			[HarmonyPatch( "UpdateCamera" )]
			[HarmonyPrefix]
			private static void UpdateCameraPrefix( ref float ___m_distance )
			{
				FrameInitialCameraDistance = ___m_distance;
			}
			
			[HarmonyPatch( "UpdateCamera" )]
			[HarmonyPostfix]
			private static void UpdateCameraPostfix(
				ref float dt,
				ref float ___m_distance,
				ref float ___m_minDistance,
				ref float ___m_maxDistance,
				ref float ___m_maxDistanceBoat )
			{
				if( !IsEnabled.Value )
					return;

				if( IsInterpolating )
				{
					LerpElapsed = Mathf.Clamp( LerpElapsed + dt , 0.0f , LerpDuration );
					float completion = LerpElapsed / LerpDuration;

					if( LerpType == InterpolationTypeEnum.Linear )
						___m_distance = Mathf.Lerp( LerpFrom , LerpTo , completion );
					else if( LerpType == InterpolationTypeEnum.Accelerate )
						___m_distance = Mathf.Lerp( LerpFrom , LerpTo , 1.0f - Mathf.Cos( HalfPI * completion ) );
					else if( LerpType == InterpolationTypeEnum.Decelerate )
						___m_distance = Mathf.Lerp( LerpFrom , LerpTo , Mathf.Sin( HalfPI * completion ) );

					LerpDistanceOverride = ___m_distance;
					IsInterpolating = LerpElapsed < LerpDuration;
				}

				CameraContext context = GetCameraContext( Player.m_localPlayer );

				if( context == CameraContext.Saddle )
					UpdateCameraCore( context , ref LastSaddleZoom , ref ___m_distance , ___m_minDistance , ___m_maxDistance );
				else if( context == CameraContext.Ship )
					UpdateCameraCore( context , ref LastShipZoom , ref ___m_distance , ___m_minDistance , ___m_maxDistanceBoat );
				else
					UpdateCameraCore( context , ref LastCameraZoom , ref ___m_distance , ___m_minDistance , ___m_maxDistance );

				LastCameraContext = context;
			}
		}
	}
}
