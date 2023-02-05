using HarmonyLib;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( Pickable ) )]
		class PickablePatch
		{
			[HarmonyPatch( "Interact" )]
			[HarmonyPrefix]
			private static void InteractPrefix( ref Pickable __instance )
			{
				// We can't restore the old value in a postfix because it happens before the RPC completes.
				// This means picking a respawning pickable will make it permanently pickable in tar.
				if( IsEnabled.Value && CanInteractivelyTakeFromTar.Value )
					__instance.m_tarPreventsPicking = false;
			}
		}
	}
}
