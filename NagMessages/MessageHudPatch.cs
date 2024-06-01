using BepInEx;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NagMessages
{
	public partial class NagMessages
	{
		[HarmonyPatch( typeof( MessageHud ) )]
		private class MessageHudPatch
		{
			// Vanilla does not queue or stack multiple front-and-center messages
			// that will most likely happen upon joining a world, so we must do it

			internal const double MessageTTL = 4.0; // Taken from MessageHud.ShowMessage()
			private static string CurrentMessage = null;
			private static double MinimumTimeOfNextMessage = 0.0;
			private static readonly LinkedList< string > PendingMessages = new LinkedList< string >();

			[HarmonyPatch( "ShowMessage" )]
			[HarmonyPrefix]
			private static bool ShowMessagePrefix( ref MessageHud.MessageType type , ref string text )
			{
				// We don't check if we're enabled right away so pending messages will get shown as intended
				if( type != MessageHud.MessageType.Center )
				{
					return true;
				}
				else if( text.IsNullOrWhiteSpace() )
				{
					if( PendingMessages.Count > 0 )
					{
						text = PendingMessages.First();
						PendingMessages.RemoveFirst();
					}

					CurrentMessage = text;
					return true;
				}

				double now = Time.timeAsDouble;
				if( !IsEnabled.Value || !QueueCenterMessages.Value || now >= MinimumTimeOfNextMessage )
				{
					MinimumTimeOfNextMessage = now + MessageTTL;
					CurrentMessage = text;
					return true;
				}
				else if( text.CompareTo( CurrentMessage ) == 0 )
				{
					// Refreshing the message is easy but delaying coroutines is too much of a headache
					return false;
				}
				else if( !PendingMessages.Contains( text ) )
				{
					PendingMessages.AddLast( text );
					Instance.StartCoroutine( "ShowNextMessage" , (float)( MinimumTimeOfNextMessage - now ) );
					MinimumTimeOfNextMessage += MessageTTL;
				}

				return false;
			}
		}

		public IEnumerator ShowNextMessage( float seconds )
		{
			yield return new WaitForSecondsRealtime( seconds );

			if( Player.m_localPlayer && MessageHud.instance )
				MessageHud.instance.ShowMessage( MessageHud.MessageType.Center , null );
		}
	}
}
