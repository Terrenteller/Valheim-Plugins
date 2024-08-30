using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public partial class SuperUltrawideSupport : BaseUnityPlugin
	{
		[HarmonyPatch( typeof( Barber ) )]
		private class BarberPatch
		{
			[HarmonyPatch( "Interact" )]
			[HarmonyPostfix]
			private static void InteractPostfix( ref Barber __instance )
			{
				// The original Interact() never returns true
				GameObject barberRootPanel = PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject;
				if( barberRootPanel.activeInHierarchy )
				{
					RectTransform rectTransform = Common.FindParentOrSelf( barberRootPanel.transform , "BarberGui" ) as RectTransform;
					if( rectTransform != null )
					{
						Lerper.Register( rectTransform );
						Lerper.Lerp( rectTransform );
					}
				}
			}
		}
	}
}
