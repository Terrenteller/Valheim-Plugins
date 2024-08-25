using System;
using System.Collections;
using System.Text;
using System.Threading;
using UnityEngine;
using static BorderlessWindowed.NativeMethods;

namespace BorderlessWindowed
{
	internal class BorderHelper
	{
		private const uint GameWindowBorderFlags = WS_BORDER | WS_DLGFRAME | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
		private static System.Object UpdateBorderLock = new System.Object();

		// The game resolution does not include the borders.
		// Determining these values on the fly is too much trouble.
		// Cache them at the first opportunity so toggling the border is stable.
		private WindowRect? outerBorderExtra = null;
		private WindowRect? innerBorderExtra = null;

		public IEnumerator UpdateBorder( bool enable )
		{
			if( Monitor.TryEnter( UpdateBorderLock ) )
			{
				try
				{
					yield return UpdateBorderCore( enable );
				}
				finally
				{
					Monitor.Exit( UpdateBorderLock );
				}
			}

			yield break;
		}

		private IEnumerator UpdateBorderCore( bool enable )
		{
			// HACK: The game window persists one pixel too far to the right when the border is disabled
			bool gameIsStarting = Console.instance == null;

			// Skip the first frame for Unity to stabilize
			yield return null;

			if( Screen.fullScreen )
				yield break;

			IntPtr hWnd = FindGameWindowHandle();
			if( hWnd == IntPtr.Zero )
				yield break;

			bool hasBorder = GameWindowHasBorder( hWnd );
			if( enable == hasBorder )
				yield break;

			RectTransform rootRect = GameObject.Find( "GuiRoot" )?.transform as RectTransform;
			if( rootRect == null )
				yield break;

			DebugMessage( $">>> BEGIN" );
			DebugMessage( $"SetGameWindowBorder({enable})" );

			// Grab these before we start messing with the window
			int desiredWidth = Screen.width;
			int desiredHeight = Screen.height;
			DebugMessage( $"Scene extents: {Screen.width}x{Screen.height}" );

			WindowRect outerInitial;
			WindowRect innerInitial;
			GetGameWindowRects( hWnd , out outerInitial , out innerInitial );
			DebugMessage( $"Outer, initial: ({outerInitial.Left},{outerInitial.Top}) to ({outerInitial.Right},{outerInitial.Bottom})" );
			DebugMessage( $"Inner, initial: ({innerInitial.Left},{innerInitial.Top}) to ({innerInitial.Right},{innerInitial.Bottom})" );

			if( outerBorderExtra == null && innerBorderExtra == null )
			{
				// The game is expected to start with a border to derive values from
				if( !hasBorder )
					yield break;

				// Default initialization is to make C# happy about the assignment at the end of this block
				WindowRect outerExtra = default( WindowRect );
				outerExtra.Left = ( ( outerInitial.Right - outerInitial.Left ) - desiredWidth ) / 2;
				// Top seems to be stable...
				outerExtra.Right = outerExtra.Left;
				outerExtra.Bottom = ( outerInitial.Bottom - outerInitial.Top ) - desiredHeight;
				DebugMessage( $"OuterBorderExtra: LR {outerExtra.Left},{outerExtra.Right} to TB {outerExtra.Top},{outerExtra.Bottom}" );
				outerBorderExtra = outerExtra;

				// Default initialization is to make C# happy about the assignment at the end of this block
				WindowRect innerExtra = default( WindowRect );
				innerExtra.Left = ( ( innerInitial.Right - innerInitial.Left ) - desiredWidth ) / 2;
				// Top seems to be stable...
				innerExtra.Right = innerExtra.Left;
				innerExtra.Bottom = ( innerInitial.Bottom - innerInitial.Top ) - desiredHeight;
				DebugMessage( $"InnerBorderExtra: LR {innerExtra.Left},{innerExtra.Right} to TB {innerExtra.Top},{innerExtra.Bottom}" );
				innerBorderExtra = innerExtra;
			}

			SetGameWindowBorder( hWnd , enable );
			yield return null;

#if !PACKAGE
			WindowRect outerAfter;
			WindowRect innerAfter;
			GetGameWindowRects( hWnd , out outerAfter , out innerAfter );
			DebugMessage( $"Outer, after border change: ({outerAfter.Left},{outerAfter.Top}) to ({outerAfter.Right},{outerAfter.Bottom})" );
			DebugMessage( $"Inner, after border change: ({innerAfter.Left},{innerAfter.Top}) to ({innerAfter.Right},{innerAfter.Bottom})" );
#endif

			int targetWidth = desiredWidth;
			int targetHeight = desiredHeight;
			int left = innerInitial.Left;
			int top = innerInitial.Top;

			if( enable )
			{
				// Adding the border causes the border to expand into content space
				WindowRect extra = outerBorderExtra.Value;
				targetWidth = desiredWidth + extra.Left + extra.Right;
				targetHeight = desiredHeight + extra.Top + extra.Bottom;
				left -= extra.Left;
				top -= extra.Top;
			}
			else
			{
				// Removing the border causes contents to expand into border space
				WindowRect extra = innerBorderExtra.Value;
				left += extra.Left;
				top += extra.Top;

				if( gameIsStarting )
					left -= 1;
			}

			UInt32 setWindowPosFlags = SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_NOZORDER;
			DebugMessage( $"SetWindowPos( hWnd , IntPtr.Zero , {left} , {top} , {targetWidth} , {targetHeight} , setWindowPosFlags );" );
			SetWindowPos( hWnd , IntPtr.Zero , left , top , targetWidth , targetHeight , setWindowPosFlags );
			DebugMessage( $"<<< END\n" );
		}

		// Statics

		private static void DebugMessage( string message )
		{
#if !PACKAGE
			Debug.Log( message );
#endif
		}

		private static IntPtr FindGameWindowHandle()
		{
			IntPtr gameWindowHandle = IntPtr.Zero;

			EnumDesktopWindowsDelegate filter = delegate( IntPtr hWnd , int lParam )
			{
				StringBuilder titleBuilder = new StringBuilder( 255 );
				GetWindowText( hWnd , titleBuilder , titleBuilder.Capacity + 1 );
				string windowTitle = titleBuilder.ToString();
				if( IsWindowVisible( hWnd ) && windowTitle == "Valheim" )
				{
					gameWindowHandle = hWnd;
					return false;
				}

				return true;
			};
			EnumDesktopWindows( IntPtr.Zero , filter , IntPtr.Zero );

			return gameWindowHandle;
		}

		private static bool GameWindowHasBorder( IntPtr hWnd )
		{
			// Should the inner and outer borders be compared too?
			return hWnd != IntPtr.Zero && ( GetWindowLong( hWnd , GWL_STYLE ) & GameWindowBorderFlags ) != 0;
		}

		private static unsafe void GetGameWindowRects( IntPtr hWnd , out WindowRect outer , out WindowRect inner )
		{
			// This is the outer extents of the thick border.
			// It may render the drop shadow and function as the resize grip.
			WindowRect tempOuter;
			GetWindowRect( hWnd , out tempOuter );
			outer = tempOuter;

			// This is the ever-present, single pixel border
			WindowRect tempInner;
			DwmGetWindowAttribute( hWnd , DWMWA_EXTENDED_FRAME_BOUNDS , &tempInner , sizeof( WindowRect ) );
			inner = tempInner;
		}

		private static void SetGameWindowBorder( IntPtr hWnd , bool enable )
		{
			if( hWnd == IntPtr.Zero )
				return;

			UInt32 style = GetWindowLong( hWnd , GWL_STYLE );
			bool isEnabled = ( style & GameWindowBorderFlags ) != 0;
			if( enable == isEnabled )
				return;

			style = enable ? ( style |= GameWindowBorderFlags ) : ( style &= ~GameWindowBorderFlags );
			SetWindowLong( hWnd , GWL_STYLE , style );
		}
	}
}
