using HarmonyLib;

namespace TarTweaks
{
	public partial class TarTweaks
	{
		[HarmonyPatch( typeof( ItemDrop ) )]
		class ItemDropPatch
		{
			private static bool SuppressInTar = false;
			private static bool WasInTar = false;
			private const float TakeFromTarStamina = 3.0f;

			[HarmonyPatch( "InTar" )]
			[HarmonyPostfix]
			private static bool InTarPostfix( bool result )
			{
				if( IsEnabled.Value && SuppressInTar && result )
				{
					WasInTar = true;
					return false;
				}

				return result;
			}

			[HarmonyPatch( "Interact" )]
			[HarmonyPrefix]
			private static void InteractPrefix( ref Humanoid character )
			{
				SuppressInTar = IsEnabled.Value
					&& CanInteractivelyTakeFromTar.Value
					&& character.HaveStamina( TakeFromTarStamina );
			}

			[HarmonyPatch( "Interact" )]
			[HarmonyPostfix]
			private static bool InteractPostfix( bool result , ref Humanoid character )
			{
				if( IsEnabled.Value && CanInteractivelyTakeFromTar.Value && WasInTar && result )
					character.UseStamina( TakeFromTarStamina );

				SuppressInTar = false;
				WasInTar = false;
				return result;
			}
		}
	}
}
