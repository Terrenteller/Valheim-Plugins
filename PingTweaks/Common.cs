using System;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		internal class Common
		{
			// TODO: Can we locate the dungeon entrance and put the ping below it? Something about a ZoneSystem?
			public const float DungeonFakeDepth = 30.0f;
			public const float DungeonMinHeight = 4500.0f; // Closer to 5 km, really. Safeguard against unusually tall/deep dungeons.
			public const float EstimatedSeaLevel = 30.0f;

			public static bool CheckBroadcastKeybind()
			{
				return PingBroadcastModifier.Value == KeyCode.None || Input.GetKey( PingBroadcastModifier.Value );
			}

			public static bool CheckPersistentKeybind()
			{
				return PersistentPingBroadcastModifier.Value != KeyCode.None && Input.GetKey( PersistentPingBroadcastModifier.Value );
			}

			public static Color CopyColor( Color color )
			{
				return new Color( color.r , color.g , color.b , color.a );
			}

			public static bool IsLocalPlayerInDungeon()
			{
				return Player.m_localPlayer.transform.position.y >= DungeonMinHeight;
			}

			public static string PrettyPrintDistance( Vector3 from , Vector3 to )
			{
				return string.Format(
					System.Globalization.CultureInfo.CurrentCulture,
					"({0}m)",
					Math.Round( ( to - from ).magnitude , 1 ) );
			}

			public static string PrettyPrintPingText( Chat.WorldTextInstance worldText )
			{
				return string.Format(
					"{0}\n{1}\n{2}",
					worldText.m_name,
					worldText.m_text,
					Player.m_localPlayer != null
						? PrettyPrintDistance( Player.m_localPlayer.transform.position , worldText.m_position )
						: "" );
			}
		}
	}
}
