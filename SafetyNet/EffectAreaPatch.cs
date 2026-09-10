using HarmonyLib;

namespace SafetyNet
{
	public partial class SafetyNet
	{
		[HarmonyPatch( typeof( EffectArea ) )]
		public class EffectAreaPatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			private static void AwakePostfix( Hud __instance )
			{
				// Does this apply to generated objects like fuling bonfires?
				foreach( EffectArea effectArea in __instance.gameObject.GetComponentsInChildren< EffectArea >() )
					if( effectArea.m_type == EffectArea.Type.PlayerBase || effectArea.m_type == EffectArea.Type.NoMonsters )
						SafetyNetBehaviour.CreateOnFor( effectArea.gameObject , effectArea );
			}
		}
	}
}
