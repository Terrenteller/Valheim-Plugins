namespace TarTweaks
{
	public partial class TarTweaks
	{
		public static StatusEffect GetStatusEffectByName( SEMan manager , string name )
		{
			foreach( StatusEffect effect in manager.GetStatusEffects() )
				if( effect.name.CompareTo( name ) == 0 )
					return effect;

			return null;
		}
	}
}
