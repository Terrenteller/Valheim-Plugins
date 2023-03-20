using System.Collections;
using UnityEngine;

namespace NagMessages
{
	public partial class NagMessages
	{
		private const string BonemassPowerName = "GP_Bonemass";
		private const string EikthyrPowerName = "GP_Eikthyr";
		private const string ModerPowerName = "GP_Moder";
		private const string TheElderPowerName = "GP_TheElder";
		private const string TheQueenPowerName = "GP_Queen";
		private const string YagluthPowerName = "GP_Yagluth";
		private const float DelayFudgeFactor = 0.01f;
		private static double LastPowerNagTime = 0.0;
		private static double LastStomachNagTime = 0.0;

		public void Nag( bool force , bool aboutPower )
		{
			Player player = Player.m_localPlayer;
			if( !IsEnabled.Value || !player )
				return;

			double now = Time.timeAsDouble;
			if( aboutPower )
			{
				if( !force )
				{
					double secondsSinceLastNag = now - LastPowerNagTime;
					double nagFrequencyInSeconds = PowerNagFrequency.Value * 60.0;
					if( now < PlayerPatch.ForsakenPowerTimeout || secondsSinceLastNag < nagFrequencyInSeconds )
						return;
				}

				string powerName = player.GetGuardianPowerName();
				if( ( !AllowBonemass.Value && powerName.CompareTo( BonemassPowerName ) == 0 )
					|| ( !AllowEikthyr.Value && powerName.CompareTo( EikthyrPowerName ) == 0 )
					|| ( !AllowModer.Value && powerName.CompareTo( ModerPowerName ) == 0 )
					|| ( !AllowTheElder.Value && powerName.CompareTo( TheElderPowerName ) == 0 )
					|| ( !AllowTheQueen.Value && powerName.CompareTo( TheQueenPowerName ) == 0 )
					|| ( !AllowYagluth.Value && powerName.CompareTo( YagluthPowerName ) == 0 ) )
				{
					player.Message( MessageHud.MessageType.Center , "Change your forsaken power!" );
					LastPowerNagTime = now;
					Instance.StartCoroutine( "NagAboutPowerIn" , PowerNagFrequency.Value * 60.0f );
				}
			}
			else
			{
				if( !force )
				{
					double secondsSinceLastNag = now - LastStomachNagTime;
					double nagFrequencyInSeconds = StomachNagFrequency.Value * 60.0;
					if( secondsSinceLastNag < nagFrequencyInSeconds )
						return;
				}

				if( player.GetFoods().Count == 0 )
				{
					player.Message( MessageHud.MessageType.Center , "Your stomach is growling" );
					LastStomachNagTime = now;
					Instance.StartCoroutine( "NagAboutEatingIn" , StomachNagFrequency.Value * 60.0f );
				}
			}
		}

		public IEnumerator NagAboutPowerIn( float seconds )
		{
			if( seconds > 1.0f )
			{
				yield return new WaitForSecondsRealtime( seconds + DelayFudgeFactor );

				Instance.Nag( false , true );
			}
		}

		public IEnumerator ForceNagAboutPowerIn( float seconds )
		{
			if( seconds > 1.0f )
			{
				yield return new WaitForSecondsRealtime( seconds + DelayFudgeFactor );

				Instance.Nag( true , true );
			}
		}

		public IEnumerator NagAboutEatingIn( float seconds )
		{
			if( seconds > 1.0f )
			{
				yield return new WaitForSecondsRealtime( seconds + DelayFudgeFactor );

				Instance.Nag( false , false );
			}
		}
	}
}
