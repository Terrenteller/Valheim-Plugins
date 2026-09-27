using HarmonyLib;

namespace PettingZoo
{
	public partial class PettingZoo
	{
		[HarmonyPatch( typeof( Petable ) )]
		private class PetablePatch
		{
			[HarmonyPatch( "Interact" )]
			[HarmonyPrefix]
			private static bool InteractPrefix( Petable __instance , Humanoid user )
			{
				if( !IsEnabled.Value )
					return true;

				Character character = __instance.gameObject.GetComponent< Character >();
				if( !character )
					return true;

				GrudgeHelper grudgeHelper = new GrudgeHelper( __instance.gameObject.GetComponent< Character >() );
				if( !grudgeHelper.IsAttacker( user.GetZDOID() ) )
					return true;

				AnimalAI animalAI = __instance.gameObject.GetComponent< AnimalAI >();
				if( !animalAI )
					return true;

				Traverse.Create( animalAI )
					.Method( "OnDamaged" , new[] { typeof( float ) , typeof( Character ) } )
					.GetValue( 0 , user );

				return false;
			}
		}
	}
}
