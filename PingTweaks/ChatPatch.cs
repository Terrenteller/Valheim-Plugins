using BepInEx;
using HarmonyLib;

namespace PingTweaks
{
	public partial class PingTweaks
	{
		[HarmonyPatch( typeof( Chat ) )]
		private class ChatPatch
		{
			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPrefix]
			private static void AddInworldTextPrefix( ref string text , out string __state )
			{
				// Grab the text before the real method modifies it
				__state = text;
			}

			[HarmonyPatch( "AddInworldText" )]
			[HarmonyPostfix]
			private static void AddInworldTextPostfix(
				ref Chat __instance,
				ref long senderID,
				ref Talker.Type type,
				ref string __state )
			{
				if( type != Talker.Type.Ping
					|| !IsEnabled.Value
					|| !ShowMapMarkerTextWhenPinged.Value
					|| __state.IsNullOrWhiteSpace() )
				{
					return;
				}

				Chat.WorldTextInstance worldTextInstance = Traverse.Create( __instance )
					.Method( "FindExistingWorldText" , new[] { typeof( long ) } )
					.GetValue< Chat.WorldTextInstance >( senderID );

				if( worldTextInstance == null )
					return;

				worldTextInstance.m_text = Localization.instance.Localize( __state ).ToUpperInvariant();
				Traverse.Create( __instance )
					.Method( "UpdateWorldTextField" , new[] { typeof( Chat.WorldTextInstance ) } )
					.GetValue( worldTextInstance );
			}
			
			[HarmonyPatch( "OnNewChatMessage" )]
			[HarmonyPrefix]
			private static bool OnNewChatMessagePrefix(
				ref long senderID,
				ref Talker.Type type,
				ref float ___m_hideTimer,
				out float __state )
			{
				__state = ___m_hideTimer;

				return !( IsEnabled.Value
					&& SuppressIncomingPings.Value
					&& type == Talker.Type.Ping
					&& senderID != ZNet.instance.GetUID() );
			}

			[HarmonyPatch( "OnNewChatMessage" )]
			[HarmonyPostfix]
			private static void OnNewChatMessagePostfix(
				ref Talker.Type type,
				ref float ___m_hideTimer,
				ref float __state )
			{
				if( IsEnabled.Value && SuppressChatBoxOnPing.Value && type == Talker.Type.Ping )
					___m_hideTimer = __state;
			}
		}
	}
}
