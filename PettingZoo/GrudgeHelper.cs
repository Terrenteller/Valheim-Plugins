using HarmonyLib;
using System;
using System.Collections.Generic;

namespace PettingZoo
{
	internal class GrudgeHelper
	{
		public static readonly int GrudgesKeyHash = "pettingzoo_grudges".GetStableHashCode();

		public ZDO zdo = null;
		public List< string > attackers = null;

		public GrudgeHelper( object znetViewHolder )
		{
			try
			{
				zdo = Traverse.Create( znetViewHolder )
					.Field( "m_nview" )
					.GetValue< ZNetView >()
					.GetZDO();
			}
			catch( Exception )
			{
				// We should probably report this
			}

			attackers = zdo != null
				? new List< string >( zdo.GetString( GrudgesKeyHash ).Split( ';' ) )
				: new List< string >();
		}

		public void AddAttacker( ZDOID zdoID )
		{
			if( zdo != null && !IsAttacker( zdoID ) )
			{
				attackers.Add( zdoID.ToString() );
				zdo.Set( GrudgesKeyHash , string.Join( ";" , attackers ) );
			}
		}

		public bool IsAttacker( ZDOID zdoID )
		{
			return attackers.Contains( zdoID.ToString() );
		}
	}
}
