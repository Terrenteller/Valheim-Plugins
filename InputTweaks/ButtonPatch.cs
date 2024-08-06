using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		internal class ButtonRightClickComponent : MonoBehaviour
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
					ButtonRightClickComponent component = __instance.GetComponent< ButtonRightClickComponent >();
					if( component != null )
					{
						Common.DebugMessage( $"RCLK: Activating RightClickButtonComponent" );
						component.OnRightClick.Invoke();
					}
				}
			}
		}
	}
}
