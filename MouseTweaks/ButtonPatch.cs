using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MouseTweaks
{
	public partial class MouseTweaks
	{
		internal class RightClickButtonComponent : MonoBehaviour
		{
			public Button.ButtonClickedEvent OnRightClick = new Button.ButtonClickedEvent();
		}

		[HarmonyPatch( typeof( Button ) )]
		private class ButtonPatch
		{
			[HarmonyPatch( "OnPointerClick" )]
			[HarmonyPostfix]
			private static void OnPointerClickPostfix( Button __instance , PointerEventData eventData )
			{
				if( eventData.button == PointerEventData.InputButton.Right && __instance.IsActive() && __instance.IsInteractable() )
				{
					RightClickButtonComponent component = __instance.GetComponent< RightClickButtonComponent >();
					if( component != null )
					{
						System.Console.WriteLine( $"TEST: Activating RightClickButtonComponent" );
						component.OnRightClick.Invoke();
					}
				}
			}
		}
	}
}
