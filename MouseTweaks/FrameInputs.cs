using UnityEngine;

namespace MouseTweaks
{
	public class FrameInputs
	{
		// An enum makes bitwise manipulation worse than it already is
		public const int LeftButton = 0;
		public const int RightButton = 1;
		public const int AllButtons = 2;

		// Arbitrary values. What are good defaults? Make config options?
		private const double SuccessiveClickWindow = 0.25f;
		private const int SuccessiveClickRadius = 3;

		public enum ButtonDelta
		{
			None,
			Pressed,
			Held,
			Released
		}

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
				else if( ( mousePos - SuccessiveClicksFirstPos ).Magnitude() < SuccessiveClickRadius ) // No MagnitudeSquared()?
				{
					SuccessiveClicks++;
					System.Console.WriteLine( $"DBLC: SuccessiveClicks {SuccessiveClicks}" );
				}
				else
				{
					SuccessiveClicksFirstPos = mousePos;
					SuccessiveClicks = 1;
					System.Console.WriteLine( $"DBLC: SuccessiveClicks reset (distance)" );
				}

				// Don't increment if we're outside the double click radius, but don't reset either.
				// If this class tracked the validity of actions, leaving the radius should invalid it.
				// Determining whether the doodad frobulated changed is more difficult.
				// FIXME: Or maybe we should reset...
			}
			else if( LeftDelta == ButtonDelta.None && sinceLastPress > SuccessiveClickWindow && SuccessiveClicks > 0 )
			{
				SuccessiveClicks = 0;
				System.Console.WriteLine( $"DBLC: SuccessiveClicks reset (time)" );
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
