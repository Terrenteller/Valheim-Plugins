using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace NoLossyCookingStations
{
	public partial class NoLossyCookingStations : BaseUnityPlugin
	{
		internal class Common
		{
			private const double NetworkClaimDelay = 0.5;
			private const double NetworkOwnershipDelay = 0.1;
			private const double DelayFudgeFactor = 0.01;

			public static bool? SanityCheckInteraction(
				ZNetView netView,
				ref bool result,
				ref double rateLimitTimeout )
			{
				if( !IsEnabled.Value )
				{
					return true;
				}
				else if( Time.timeAsDouble < rateLimitTimeout )
				{
					result = false;
					return false;
				}
				else if( !netView.HasOwner() || netView.IsOwner() )
				{
					rateLimitTimeout = Time.timeAsDouble + NetworkOwnershipDelay;
					return true;
				}

				rateLimitTimeout = Time.timeAsDouble + NetworkClaimDelay;
				long previousOwner = netView.GetZDO().m_uid.ID;
				netView.ClaimOwnership(); // Look at me. I am the owner now.
				ZDOMan.instance.ForceSendZDO( previousOwner , netView.GetZDO().m_uid );

				result = false;
				return null;
			}

			public static IEnumerator DelayedInteraction< T >(
				WeakReference< Humanoid > userReference,
				WeakReference< T > interactableReference,
				string methodName,
				params WeakReference< object >[] argReferences )
				where T : MonoBehaviour
			{
				yield return new WaitForSecondsRealtime( (float)( NetworkClaimDelay + DelayFudgeFactor ) );

				Humanoid user = null;
				if( interactableReference.TryGetTarget( out T interactable )
					&& interactable != null
					&& ( userReference == null || userReference.TryGetTarget( out user ) ) )
				{
					object[] args = new object[ argReferences.Length ];
					for( int index = 0 ; index < argReferences.Length ; index++ )
						argReferences[ index ].TryGetTarget( out args[ index ] );

					// We assume what we're calling returns a boolean based on success
					bool result = Traverse.Create( interactable )
						.Method( methodName , args )
						.GetValue< bool >( args );

					if( result && user != null )
					{
						Traverse.Create( user )
							.Field( "m_zanim" )
							.GetValue< ZSyncAnimation >()
							?.SetTrigger( "interact" );
					}
				}
			}
		}
	}
}
