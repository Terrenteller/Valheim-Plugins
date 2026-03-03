using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( ZRoutedRpc ) )]
		private class ZRoutedRpcPatch
		{
			[HarmonyPatch( "InvokeRoutedRPC" , new[] { typeof( long ) , typeof( string ) , typeof( object[] ) } )]
			[HarmonyPrefix]
			private static bool InvokeRoutedRPCPrefix( ref string methodName , ref object[] parameters )
			{
				if( !IsEnabled.Value || methodName.CompareTo( "ChatMessage" ) != 0 )
					return true;

				int? maybeType = parameters[ 1 ] as int?;
				if( !maybeType.HasValue || (Talker.Type)maybeType.Value != Talker.Type.Ping )
					return true;

				Vector3? maybePosition = parameters[ 0 ] as Vector3?;
				if( maybePosition == null || !maybePosition.HasValue )
					return true;

				Vector3 position = maybePosition.Value;
				float originalY = position.y;
				UserInfo userInfo = parameters[ 2 ] as UserInfo;
				string text = parameters[ 3 ] as string;

				Minimap minimap = Minimap.instance;
				float zoomFactor = Traverse.Create( minimap )
					.Field( "m_largeZoom" )
					.GetValue< float >();
				float radius = minimap.m_removeRadius * zoomFactor * 2.0f;
				Minimap.PinData pinData = MinimapPatch.GetClosestPin( minimap , position , radius , false );
				if( pinData != null )
				{
					position = pinData.m_pos;

					// Old pins won't have a Y coordinate
					if( position.y == 0.0f )
						position.y = originalY;

					// Pins don't have to have text
					if( !pinData.m_name.IsNullOrWhiteSpace() )
					{
						parameters[ 3 ] = pinData.m_name; // Unlocalized for broadcast
						text = pinData.m_NamePinData.PinNameText.text; // Localized for non-broadcast
					}
				}

				if( !ChatPatch.BroadcastPing && userInfo != null )
				{
					Chat.instance.OnNewChatMessage( null , ZNet.GetUID() , position , Talker.Type.Ping , userInfo , text ?? "PING" );
					return false; // No need to send anything because we just sent it to ourself
				}

				// Need to send the modified position!
				parameters[ 0 ] = position;
				return true;
			}
		}
	}
}
