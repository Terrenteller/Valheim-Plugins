using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NagMessages
{
	// TODO: Clean up the outrageous redundancy in this project
	public partial class NagMessages
	{
		private readonly int BonemassPowerNameHash = "GP_Bonemass".GetHashCode();
		private readonly int EikthyrPowerNameHash = "GP_Eikthyr".GetHashCode();
		private readonly int ModerPowerNameHash = "GP_Moder".GetHashCode();
		private readonly int TheElderPowerNameHash = "GP_TheElder".GetHashCode();
		private readonly int TheQueenPowerNameHash = "GP_Queen".GetHashCode();
		private readonly int YagluthPowerNameHash = "GP_Yagluth".GetHashCode();

		private static List< Coroutine > PowerNagCoroutines = new List< Coroutine >();
		internal static double MinTimeOfNextPowerNag = 0.0;
		private static List< Coroutine > HungerNagCoroutines = new List< Coroutine >();
		internal static double MinTimeOfNextHungerNag = 0.0;

		private class NagArgs
		{
			public double delay;
			public bool force;
			public Coroutine self;
		}

		public void NagAboutPower()
		{
			NagAboutPower( PowerNagFrequency.Value * 60.0f , false );
		}

		public void NagAboutPower( double delay , bool force )
		{
			NagArgs args = new NagArgs { delay = delay , force = force };
			Coroutine coroutine = Instance.StartCoroutine( CoNagAboutPower( args ) );
			args.self = coroutine;
			PowerNagCoroutines.Add( coroutine );
		}

		private IEnumerator CoNagAboutPower( NagArgs args )
		{
			// Brief delay so the caller can set args.coroutine
			yield return new WaitForSecondsRealtime( 1.0f );

			if( args.delay > 1.0f )
				yield return new WaitForSecondsRealtime( (float)args.delay );

			Instance.NagAboutPower( args );
		}

		private void NagAboutPower( NagArgs args )
		{
			Player player = Player.m_localPlayer;
			if( !IsEnabled.Value || !player )
				return;

			double now = Time.timeAsDouble;
			if( !args.force && now < MinTimeOfNextPowerNag )
			{
				Instance.StopCoroutine( args.self );
				PowerNagCoroutines.Remove( args.self );
				if( PowerNagCoroutines.Count == 0 )
					NagAboutPower( MinTimeOfNextPowerNag - now , false );

				return;
			}

			int powerNameHash = player.GetGuardianPowerName().GetHashCode();
			if( ( !AllowBonemass.Value && powerNameHash == BonemassPowerNameHash )
				|| ( !AllowEikthyr.Value && powerNameHash == EikthyrPowerNameHash )
				|| ( !AllowModer.Value && powerNameHash == ModerPowerNameHash )
				|| ( !AllowTheElder.Value && powerNameHash == TheElderPowerNameHash )
				|| ( !AllowTheQueen.Value && powerNameHash == TheQueenPowerNameHash )
				|| ( !AllowYagluth.Value && powerNameHash == YagluthPowerNameHash ) )
			{
				// TODO: Don't nag if the world does not have any of the preferred powers unlocked
				player.Message( MessageHud.MessageType.Center , "Change your forsaken power!" );
				MinTimeOfNextPowerNag = now + ( PowerNagFrequency.Value * 60.0f );

				foreach( Coroutine coroutine in PowerNagCoroutines )
					Instance.StopCoroutine( coroutine );
				PowerNagCoroutines.Clear();

				NagAboutPower();
			}
		}
		
		public void NagAboutHunger()
		{
			NagAboutHunger( HungerNagFrequency.Value * 60.0f , false );
		}
		
		public void NagAboutHunger( double delay , bool force )
		{
			NagArgs args = new NagArgs { delay = delay , force = force };
			Coroutine coroutine = Instance.StartCoroutine( CoNagAboutHunger( args ) );
			args.self = coroutine;
			HungerNagCoroutines.Add( coroutine );
		}

		private IEnumerator CoNagAboutHunger( NagArgs args )
		{
			// Brief delay so the caller can set args.coroutine
			yield return new WaitForSecondsRealtime( 1.0f );

			if( args.delay > 1.0f )
				yield return new WaitForSecondsRealtime( (float)args.delay );

			Instance.NagAboutHunger( args );
		}

		private void NagAboutHunger( NagArgs args )
		{
			Player player = Player.m_localPlayer;
			if( !IsEnabled.Value || !player )
				return;

			double now = Time.timeAsDouble;
			if( !args.force && now < MinTimeOfNextHungerNag )
			{
				Instance.StopCoroutine( args.self );
				HungerNagCoroutines.Remove( args.self );
				if( HungerNagCoroutines.Count == 0 )
					NagAboutHunger( MinTimeOfNextHungerNag - now , false );

				return;
			}

			if( player.GetFoods().Count == 0 )
			{
				player.Message( MessageHud.MessageType.Center , "Your stomach is growling" );
				MinTimeOfNextHungerNag = now + ( HungerNagFrequency.Value * 60.0f );

				foreach( Coroutine coroutine in HungerNagCoroutines )
					Instance.StopCoroutine( coroutine );
				HungerNagCoroutines.Clear();

				NagAboutHunger();
			}
		}
	}
}
