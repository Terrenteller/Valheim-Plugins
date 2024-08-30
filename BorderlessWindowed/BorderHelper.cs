using System;
using System.Collections;
using System.Linq;
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
		// The game is expected to start with a border to derive the outer and one of the inner thickness from.
		private WindowRect? outerBorderThickness = null;
		private WindowRect? innerWindowedBorderThickness = null;
		private WindowRect? innerMaximizedBorderThickness = null;

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

			DebugMessage( $">>> BEGIN" );
			DebugMessage( $"SetGameWindowBorder({enable})" );

			// Grab these before we start messing with the window
			bool isMaximized = IsZoomed( hWnd );
			bool isResolutionPreset = Screen.resolutions.Any( x => Screen.width == x.width && Screen.height == x.height );
			int sceneWidth = Screen.width;
			int sceneHeight = Screen.height;
			DebugMessage( $"Scene extents, {( isMaximized ? "maximized" : "windowed" )}: {Screen.width}x{Screen.height} ({( isResolutionPreset ? "" : "not " )}a preset)" );

			WindowRect outerInitial;
			WindowRect innerInitial;
			GetGameWindowRects( hWnd , out outerInitial , out innerInitial );
			DebugMessage( $"Outer, initial: ({outerInitial.Left},{outerInitial.Top}) to ({outerInitial.Right},{outerInitial.Bottom})" );
			DebugMessage( $"Inner, initial: ({innerInitial.Left},{innerInitial.Top}) to ({innerInitial.Right},{innerInitial.Bottom})" );

			if( outerBorderThickness == null )
			{
				if( !hasBorder )
					yield break;

				outerBorderThickness = ComputeBorderThickness( outerInitial , sceneWidth , sceneHeight );
			}

			if( !isMaximized && innerWindowedBorderThickness == null )
			{
				if( !hasBorder )
					yield break;

				innerWindowedBorderThickness = ComputeBorderThickness( innerInitial , sceneWidth , sceneHeight );
			}

			if( isMaximized && innerMaximizedBorderThickness == null )
			{
				if( !hasBorder )
					yield break;

				innerMaximizedBorderThickness = ComputeBorderThickness( innerInitial , sceneWidth , sceneHeight );
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

			int left = innerInitial.Left;
			int top = innerInitial.Top;
			int width = sceneWidth;
			int height = sceneHeight;

			if( enable )
			{
				// Adding the border causes the border to expand into content space
				WindowRect outerThickness = outerBorderThickness.Value;
				left -= outerThickness.Left;
				top -= outerThickness.Top;
				width = sceneWidth + outerThickness.Left + outerThickness.Right;

				if( isResolutionPreset )
				{
					height += outerThickness.Top + outerThickness.Bottom;
				}
				else if( !isMaximized )
				{
					WindowRect innerThickness = innerWindowedBorderThickness.Value;
					height += ( outerThickness.Top - innerThickness.Top ) + ( outerThickness.Bottom - innerThickness.Bottom ); 
				}
			}
			else
			{
				// Removing the border causes the content to expand into border space
				WindowRect innerThickness = isMaximized ? innerMaximizedBorderThickness.Value : innerWindowedBorderThickness.Value;
				left += innerThickness.Left;
				top += innerThickness.Top;

				// The game always starts with a border. If it exits without a border, it ends up offset slightly.
				// Further complicating things, a maximized window is persisted as if it was a normal window.
				// Fortunately, the size persisted without a border ends up being what we want.
				// FIXME: If the user disables the border while the game is not running, we won't get the right height.
				if( gameIsStarting )
					left -= 1;
				else if( !isResolutionPreset )
					height += innerThickness.Top + innerThickness.Bottom;

				// FIXME: The window height ends up one too short if we:
				// 1. Maximize the game
				// 2. Toggle the border off
				// 3. Quit the game
				// 4. Start the game
				// 5. Toggle the border on
				// The workaround is to maximize the window like it was supposed to be in the first place
			}

			UInt32 setWindowPosFlags = SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_NOZORDER;
			DebugMessage( $"SetWindowPos( ... , {left} , {top} , {width} , {height} , ... );" );
			if( height == sceneHeight && width != sceneWidth )
			{
				// HACK: Prod the scene to resize. Only height changes work.
				SetWindowPos( hWnd , IntPtr.Zero , left , top , width , height - 1 , setWindowPosFlags );
			}
			SetWindowPos( hWnd , IntPtr.Zero , left , top , width , height , setWindowPosFlags );
			yield return null;
			DebugMessage( $"<<< END\n" );
		}

		// Statics

		private WindowRect ComputeBorderThickness( WindowRect outer , int width , int height )
		{
			// Default initialization is to make C# happy about the assignment at the end of this block.
			WindowRect thickness = default( WindowRect );
			thickness.Left = ( ( outer.Right - outer.Left ) - width ) / 2;
			thickness.Right = thickness.Left;
			// The height difference ends up entirely as bottom thickness.
			// This causes the window to move and down unpleasantly, but does keep the top consistent.
			thickness.Bottom = ( outer.Bottom - outer.Top ) - height;
			DebugMessage( $"Computed border thickness: LR {thickness.Left},{thickness.Right} to TB {thickness.Top},{thickness.Bottom}" );
			return thickness;
		}

		private static void DebugMessage( string message )
		{
#if !PACKAGE
			UnityEngine.Debug.Log( message );
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
