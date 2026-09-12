using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace IHaveArrived
{
	public partial class IHaveArrived
	{
		[HarmonyPatch( typeof( Game ) )]
		private class GamePatch
		{
			private static Random Rand = new Random();
			private static bool FirstSpawnPending = false;

			// Adapted from the original plugin in case ellipsization becomes preferable.
			// Right now, we simply ignore long lines.
			/*
			private static string ShortenLengthyAnnouncement( string announcement )
			{
				if( announcement.IsNullOrWhiteSpace() || announcement.Length <= 150 )
					return announcement;

				string left = announcement.Substring( 0 , 100 );
				string right = announcement.Substring( announcement.Length - 25 );
				return left + "..." + right;
			}
			*/

			[HarmonyPrefix]
			[HarmonyPatch( typeof( Game ) , "UpdateRespawn" )]
			[HarmonyPriority( Priority.First )]
			private static void UpdateRespawnPrefix( ref bool ___m_firstSpawn )
			{
				FirstSpawnPending = false;

				if( IsEnabled.Value && ___m_firstSpawn )
				{
					FirstSpawnPending = true;
					ChatPatch.DiscardNextDefaultArrivalMessage = true;
				}
			}

			[HarmonyPostfix]
			[HarmonyPatch( typeof( Game ) , "UpdateRespawn" )]
			private static void UpdateRespawnPostfix()
			{
				if( FirstSpawnPending && Player.m_localPlayer != null )
				{
					FirstSpawnPending = false;
					if( AnnounceArrival.Value )
					{
						List< string > announcements = Common.LoadAnnouncements();
						string announcement = announcements != null && announcements.Count > 0
							? announcements[ Rand.Next( 0 , announcements.Count - 1 ) ]
							: Localization.instance.Localize( "$text_player_arrived" );
						Chat.instance.SendText( Talker.Type.Shout , announcement );
					}
				}
			}
		}
	}
}
