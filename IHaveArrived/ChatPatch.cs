using HarmonyLib;

namespace IHaveArrived
{
	public partial class IHaveArrived
	{
		[HarmonyPatch( typeof( Chat ) )]
		private class ChatPatch
		{
			public static bool DiscardNextDefaultArrivalMessage = false;

			[HarmonyPrefix]
			[HarmonyPatch( typeof( Chat ) , "SendText" )]
			private static bool SendTextPrefix( Talker.Type type , string text )
			{
				if( DiscardNextDefaultArrivalMessage
					&& type == Talker.Type.Shout
					&& text == Localization.instance.Localize( "$text_player_arrived" ) )
				{
					DiscardNextDefaultArrivalMessage = false;
					return false;
				}

				return true;
			}
		}
	}
}
