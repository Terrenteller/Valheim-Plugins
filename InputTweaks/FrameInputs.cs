using UnityEngine;

namespace InputTweaks
{
	public class FrameInputs
	{
		// An enum makes bitwise manipulation worse than it already is
		public const int LeftButton = 0;
		public const int RightButton = 1;
		public const int AllButtons = 2;

		public enum ButtonDelta
		{
			None,
			Pressed,
			Held,
			Released
		}

		// These couple other classes too closely to this one.
		// Tracking successive clicks makes it worse. Can we factor these out?
		public static FrameInputs Prior = null;
		public static FrameInputs Current = new FrameInputs( true );

		private static double LeftDownLast;
		private static Vector2i SuccessiveClicksFirstPos;
		private static int SuccessiveClicks;

		private readonly int mouseButtons;
		private readonly ButtonDelta[] buttonDeltas = new ButtonDelta[ AllButtons ];
		public readonly Vector2i mousePos;
		public readonly bool isDoubleClick;
		public readonly int successiveClicks;

		public bool Left => Test( LeftButton );
		public bool Right => Test( RightButton );

		public bool LeftOnly => Only( LeftButton );
		public bool RightOnly => Only( RightButton );

		public bool None => mouseButtons == 0;
		public bool Any => mouseButtons > 0;
		public bool Exclusive => ( mouseButtons & ( mouseButtons - 1 ) ) == 0;

		public ButtonDelta LeftDelta => buttonDeltas[ 0 ];
		public ButtonDelta RightDelta => buttonDeltas[ 1 ];

		protected FrameInputs( bool skipInit )
		{
			if( skipInit )
				return;

			for( int mouseButton = 0 ; mouseButton < AllButtons ; mouseButton++ )
			{
				mouseButtons |= ZInput.GetMouseButton( mouseButton ) ? ( 1 << mouseButton ) : 0;
				buttonDeltas[ mouseButton ] = Test( mouseButton )
					? ( Prior.Test( mouseButton ) ? ButtonDelta.Held : ButtonDelta.Pressed )
					: ( Prior.Test( mouseButton ) ? ButtonDelta.Released : ButtonDelta.None );
			}

			mousePos = new Vector2i( Input.mousePosition );
			double now = Time.timeAsDouble;
			double sinceLastPress = now - LeftDownLast;

			if( LeftDelta == ButtonDelta.Pressed )
			{
				LeftDownLast = now;

				if( SuccessiveClicks == 0 )
				{
					SuccessiveClicksFirstPos = mousePos;
					SuccessiveClicks++;
				}
				else if( ( mousePos - SuccessiveClicksFirstPos ).Magnitude() < InputTweaks.SuccessiveClickRadius.Value ) // No MagnitudeSquared()?
				{
					SuccessiveClicks++;
					Common.DebugMessage( $"DBLC: SuccessiveClicks {SuccessiveClicks}" );
				}
				else
				{
					SuccessiveClicksFirstPos = mousePos;
					SuccessiveClicks = 1;
					Common.DebugMessage( $"DBLC: SuccessiveClicks reset (distance)" );
				}
			}
			else if( LeftDelta == ButtonDelta.None && sinceLastPress > InputTweaks.SuccessiveClickWindow.Value && SuccessiveClicks > 0 )
			{
				SuccessiveClicks = 0;
				Common.DebugMessage( $"DBLC: SuccessiveClicks reset (time)" );
			}

			successiveClicks = SuccessiveClicks;
			isDoubleClick = successiveClicks == 2;
		}

		public bool Test( int mouseButton )
		{
			return ( mouseButtons & ( 1 << mouseButton ) ) > 0;
		}

		public bool Only( int mouseButton )
		{
			int buttonFlag = 1 << mouseButton;
			return ( mouseButtons & buttonFlag ) == buttonFlag;
		}

		public ButtonDelta Delta( int mouseButton )
		{
			return Current.Test( mouseButton )
				? ( Prior.Test( mouseButton ) ? ButtonDelta.Held : ButtonDelta.Pressed )
				: ( Prior.Test( mouseButton ) ? ButtonDelta.Released : ButtonDelta.None );
		}

		// Statics

		public static void Update()
		{
			Prior = Current;
			Current = new FrameInputs( false );
		}
	}
}
