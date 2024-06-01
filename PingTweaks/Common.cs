using System;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		internal class Common
		{
			public const float DungeonFakeDepth = 30.0f;
			public const float DungeonMinHeight = 4500.0f; // Closer to 5 km, really

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
		}
	}
}
