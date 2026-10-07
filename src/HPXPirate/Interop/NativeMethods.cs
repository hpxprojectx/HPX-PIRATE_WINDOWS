using System;
using System.Runtime.InteropServices;

namespace HPXPirate.Interop;

internal static class NativeMethods
{
	public struct POINT
	{
		public int X;

		public int Y;
	}

	public struct MEMORYSTATUSEX
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct MINMAXINFO
	{
		public POINT ptReserved;

		public POINT ptMaxSize;

		public POINT ptMaxPosition;

		public POINT ptMinTrackSize;

		public POINT ptMaxTrackSize;
	}

	public struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	public const int WM_HOTKEY = 786;

	public const int GWL_EXSTYLE = -20;

	public const int WS_EX_LAYERED = 524288;

	public const int WS_EX_TRANSPARENT = 32;

	public const int WS_EX_TOOLWINDOW = 128;

	public const int WS_EX_NOACTIVATE = 134217728;

	public const uint MOD_ALT = 1u;

	public const uint MOD_CONTROL = 2u;

	public const uint MOD_SHIFT = 4u;

	public const uint MOD_NOREPEAT = 16384u;

	public const int SM_CXSCREEN = 0;

	public const int SM_CYSCREEN = 1;

	public const uint IDLE_PRIORITY_CLASS = 64u;

	public const uint BELOW_NORMAL_PRIORITY_CLASS = 16384u;

	public const uint NORMAL_PRIORITY_CLASS = 32u;

	public const uint ABOVE_NORMAL_PRIORITY_CLASS = 32768u;

	public const uint HIGH_PRIORITY_CLASS = 128u;

	public const uint REALTIME_PRIORITY_CLASS = 256u;

	public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	public const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

	public const int WM_GETMINMAXINFO = 36;

	public const uint MONITOR_DEFAULTTONEAREST = 2u;

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool UnregisterHotKey(nint hWnd, int id);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	public static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	public static extern short GetAsyncKeyState(int vKey);

	[DllImport("user32.dll")]
	public static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("kernel32.dll")]
	public static extern nint GetCurrentProcess();

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetPriorityClass(nint hProcess, uint dwPriorityClass);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

	[DllImport("dwmapi.dll")]
	public static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

	[DllImport("shcore.dll")]
	public static extern int SetProcessDpiAwareness(int value);

	[DllImport("user32.dll")]
	public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll")]
	public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

	// ------------------------------------------------------------------
	// Mouse / keyboard tuning via SystemParametersInfo.
	// Constants verified against Microsoft Learn (winuser.h).
	// ------------------------------------------------------------------
	public const int SM_SWAPBUTTON = 23;

	public const uint SPI_GETMOUSE = 0x0003u;

	public const uint SPI_SETMOUSE = 0x0004u;

	public const uint SPI_GETKEYBOARDSPEED = 0x000Au;

	public const uint SPI_SETKEYBOARDSPEED = 0x000Bu;

	public const uint SPI_GETKEYBOARDDELAY = 0x0016u;

	public const uint SPI_SETKEYBOARDDELAY = 0x0017u;

	public const uint SPI_SETDOUBLECLICKTIME = 0x0020u;

	public const uint SPI_SETMOUSEBUTTONSWAP = 0x0021u;

	public const uint SPI_GETFILTERKEYS = 0x0032u;

	public const uint SPI_SETFILTERKEYS = 0x0033u;

	public const uint SPI_GETTOGGLEKEYS = 0x0034u;

	public const uint SPI_SETTOGGLEKEYS = 0x0035u;

	public const uint SPI_GETSTICKYKEYS = 0x003Au;

	public const uint SPI_SETSTICKYKEYS = 0x003Bu;

	public const uint SPI_SETMOUSETRAILS = 0x005Du;

	public const uint SPI_GETMOUSETRAILS = 0x005Eu;

	public const uint SPI_GETSNAPTODEFBUTTON = 0x005Fu;

	public const uint SPI_SETSNAPTODEFBUTTON = 0x0060u;

	public const uint SPI_GETWHEELSCROLLLINES = 0x0068u;

	public const uint SPI_SETWHEELSCROLLLINES = 0x0069u;

	public const uint SPI_GETMOUSESPEED = 0x0070u;

	public const uint SPI_SETMOUSESPEED = 0x0071u;

	public const uint SPI_GETMOUSECLICKLOCK = 0x101Eu;

	public const uint SPI_SETMOUSECLICKLOCK = 0x101Fu;

	public const uint SPIF_UPDATEINIFILE = 0x01u;

	public const uint SPIF_SENDCHANGE = 0x02u;

	public const uint SKF_STICKYKEYSON = 0x00000001u;

	public const uint TKF_TOGGLEKEYSON = 0x00000001u;

	public const uint FKF_FILTERKEYSON = 0x00000001u;

	[StructLayout(LayoutKind.Sequential)]
	public struct FILTERKEYS
	{
		public uint cbSize;

		public uint dwFlags;

		public uint iWaitMSec;

		public uint iDelayMSec;

		public uint iRepeatMSec;

		public uint iBounceMSec;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct STICKYKEYS
	{
		public uint cbSize;

		public uint dwFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TOGGLEKEYS
	{
		public uint cbSize;

		public uint dwFlags;
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);

	[DllImport("user32.dll")]
	public static extern uint GetDoubleClickTime();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref FILTERKEYS pvParam, uint fWinIni);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref STICKYKEYS pvParam, uint fWinIni);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref TOGGLEKEYS pvParam, uint fWinIni);
}
