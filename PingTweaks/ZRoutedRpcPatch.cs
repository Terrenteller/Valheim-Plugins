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

				int? typeRaw = parameters[ 1 ] as int?;
				if( !typeRaw.HasValue || (Talker.Type)typeRaw.Value != Talker.Type.Ping )
					return true;

				Vector3? maybePosition = parameters[ 0 ] as Vector3?;
				if( maybePosition == null || !maybePosition.HasValue )
					return true;

				Vector3 position = maybePosition.Value;
				float originalY = position.y;
				UserInfo userInfo = parameters[ 2 ] as UserInfo;
				string text = parameters[ 3 ] as string;
				string senderAccountId = parameters[ 4 ] as string;

				Minimap minimap = Minimap.instance;
				float zoomFactor = Traverse.Create( minimap )
					.Field( "m_largeZoom" )
					.GetValue< float >();
				float radius = minimap.m_removeRadius * zoomFactor * 2.0f;
				Minimap.PinData pinData = MinimapPatch.GetClosestPin( minimap , position , radius , true );
				if( pinData != null )
				{
					position = pinData.m_pos;

					// Old pins won't have a Y coordinate
					if( position.y == 0.0f )
						position.y = originalY;

					// Pins don't have to have text
					if( !pinData.m_name.IsNullOrWhiteSpace() )
					{
						parameters[ 3 ] = pinData.m_name; // Unlocalized
						text = pinData.m_NamePinData.PinNameText.text; // Localized
					}
				}

				if( PingBroadcastModifier.Value != KeyCode.None
					&& !Input.GetKey( PingBroadcastModifier.Value )
					&& userInfo != null
					&& text != null
					&& senderAccountId != null )
				{
					Chat.instance.OnNewChatMessage(
						null,
						ZNet.GetUID(),
						position,
						Talker.Type.Ping,
						userInfo,
						text,
						senderAccountId );

					// No need to send anything, even to ourself
					return false;
				}

				// Need to send the modified position!
				parameters[ 0 ] = position;
				return true;
			}
		}
	}
}
