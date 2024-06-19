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
				// The original method never returns true
				if( PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.activeInHierarchy )
				{
					Transform transform = PlayerCustomizaton.m_barberInstance.m_rootPanel.gameObject.transform;
					RectTransform rectTransform = Common.FindParentOrSelf( transform , "BarberGui" ) as RectTransform;
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
