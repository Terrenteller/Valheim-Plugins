using HarmonyLib;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InputTweaks
{
	public partial class InputTweaks
	{
		[HarmonyPatch( typeof( ScrollRect ) )]
		private class ScrollRectPatch
		{
			public static bool BlockContainerButtonScrolling = false;
			public static WeakReference< ScrollRect > ContainerScrollRect = new WeakReference< ScrollRect >( null );

			[HarmonyPatch( "OnScroll" )]
			[HarmonyPrefix]
			private static bool OnScrollPrefix( ScrollRect __instance , PointerEventData data )
			{
				return !( BlockContainerButtonScrolling
					&& ContainerScrollRect.TryGetTarget( out ScrollRect containerScrollRect )
					&& containerScrollRect == __instance );
			}
		}
	}
}
