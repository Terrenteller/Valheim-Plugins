using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace PettingZoo
{
	public partial class PettingZoo
	{
		[HarmonyPatch( typeof( AnimalAI ) )]
		private class AnimalAIPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( AnimalAI __instance )
			{
				if( !IsEnabled.Value || __instance.gameObject.GetComponent< Petable >() )
					return;

				// Mooch data from a stable source
				GameObject halstein = PrefabManager.Instance.GetPrefab( "Halstein" );
				Petable petableHalstein = halstein?.GetComponent< Petable >();
				if( !petableHalstein )
					return;

				Character character = __instance.gameObject.GetComponent< Character >();
				if( !character )
					return;

				Petable petable = __instance.gameObject.AddComponent< Petable >();
				petable.m_name = character.GetHoverName();
				petable.m_petEffect = petableHalstein.m_petEffect;
				petable.m_randomPetTexts = petableHalstein.m_randomPetTexts;
			}

			[HarmonyPatch( "OnDamaged" )]
			[HarmonyPostfix]
			private static void OnDamagedPostfix( AnimalAI __instance, Character attacker )
			{
				if( !IsEnabled.Value )
					return;

				GrudgeHelper grudgeHelper = new GrudgeHelper( __instance.gameObject.GetComponent< Character >() );
				grudgeHelper.AddAttacker( attacker.GetZDOID() );
			}
		}
	}
}
