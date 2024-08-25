using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessWindowed
{
	public class NativeMethods
	{
		// DwmGetWindowAttribute

		[DllImport( "dwmapi.dll" )]
		public static unsafe extern Int32 DwmGetWindowAttribute( IntPtr hwnd , Int32 dwAttribute , void* pvAttribute , int cbAttribute );

		public const Int32 DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		// EnumDesktopWindows

		[DllImport( "user32.dll" , SetLastError = true )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern bool EnumDesktopWindows( IntPtr hDesktop , EnumDesktopWindowsDelegate lpfn , IntPtr lParam );

		public delegate bool EnumDesktopWindowsDelegate( IntPtr hwnd , Int32 lParam );

		// IsWindowVisible

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern bool IsWindowVisible( IntPtr hWnd );

		public const Int32 SW_MAXIMIZE = 3;

		// GetWindowLong, SetWindowLong

		[DllImport( "user32.dll" , SetLastError = true )]
		public static extern UInt32 GetWindowLong( IntPtr hWnd , Int32 nIndex );

		[DllImport( "user32.dll" )]
		public static extern Int32 SetWindowLong( IntPtr hWnd , Int32 nIndex , UInt32 dwNewLong );

		public const Int32 GWL_STYLE = -16;

		public const UInt32 WS_MAXIMIZEBOX = 0x00010000;
		public const UInt32 WS_MINIMIZEBOX = 0x00020000;
		public const UInt32 WS_THICKFRAME = 0x00040000;
		public const UInt32 WS_SYSMENU = 0x00080000;
		public const UInt32 WS_DLGFRAME = 0x00400000;
		public const UInt32 WS_BORDER = 0x00800000;

		// SetWindowPos

		[DllImport( "user32.dll" , SetLastError = true )]
		public static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			Int32 x,
			Int32 y,
			Int32 cx, // width
			Int32 cy, // height
			UInt32 flags );

		public const UInt32 SWP_ASYNCWINDOWPOS = 0x4000;
		public const UInt32 SWP_DEFERERASE = 0x2000;
		public const UInt32 SWP_DRAWFRAME = 0x0020;
		public const UInt32 SWP_FRAMECHANGED = 0x0020;
		public const UInt32 SWP_HIDEWINDOW = 0x0080;
		public const UInt32 SWP_NOACTIVATE = 0x0010;
		public const UInt32 SWP_NOCOPYBITS = 0x0100;
		public const UInt32 SWP_NOMOVE = 0x0002;
		public const UInt32 SWP_NOOWNERZORDER = 0x0200;
		public const UInt32 SWP_NOREDRAW = 0x0008;
		public const UInt32 SWP_NOREPOSITION = 0x0200;
		public const UInt32 SWP_NOSENDCHANGING = 0x0400;
		public const UInt32 SWP_NOSIZE = 0x0001;
		public const UInt32 SWP_NOZORDER = 0x0004;
		public const UInt32 SWP_SHOWWINDOW = 0x0040;

		// GetWindowRect

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern bool GetWindowRect( IntPtr hWnd , out WindowRect lpRect );

		// GetWindowText

		[DllImport( "user32.dll" , SetLastError = true , CharSet = CharSet.Unicode )]
		public static extern Int32 GetWindowText(
		   IntPtr hWnd,
		   [Out] StringBuilder lpString,
		   Int32 nMaxCount );

		// Common structures

		[StructLayout( LayoutKind.Sequential )]
		public struct WindowRect
		{
			public Int32 Left;
			public Int32 Top;
			public Int32 Right;
			public Int32 Bottom;
		}
	}
}
