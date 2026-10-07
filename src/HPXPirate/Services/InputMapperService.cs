using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using HPXPirate.Interop;

namespace HPXPirate.Services;

public sealed class InputMapperService : IDisposable
{
	private struct MSG
	{
		public nint hwnd;

		public uint message;

		public nint wParam;

		public nint lParam;

		public uint time;

		public InputHook.POINT pt;
	}

	private readonly ILogger _log;

	private nint _keyboardHook = IntPtr.Zero;

	private nint _mouseHook = IntPtr.Zero;

	private InputHook.LowLevelProc? _keyProc;

	private InputHook.LowLevelProc? _mouseProc;

	private Thread? _hookThread;

	private uint _hookThreadId;

	private volatile bool _running;

	private nint _hwnd = IntPtr.Zero;

	private HwndSource? _source;

	private readonly Dictionary<int, string> _hotkeys = new Dictionary<int, string>();

	public bool CaptureEnabled { get; set; }

	public bool HooksActive
	{
		get
		{
			if (_keyboardHook == IntPtr.Zero)
			{
				return _mouseHook != IntPtr.Zero;
			}
			return true;
		}
	}

	public event Action<string>? KeyDown;

	public event Action<string>? KeyUp;

	public event Action<string>? MouseButtonDown;

	public event Action<int, int>? MouseMoved;

	public event Action<int>? HotkeyPressed;

	public InputMapperService(ILogger log)
	{
		_log = log;
	}

	public void AttachToWindow(Window window)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		try
		{
			WindowInteropHelper val = new WindowInteropHelper(window);
			_hwnd = val.Handle;
			_source = HwndSource.FromHwnd((IntPtr)_hwnd);
			HwndSource? source = _source;
			if (source != null)
			{
				source.AddHook(new HwndSourceHook(WndProc));
			}
			_log.Info("Input mapper attached to main window.");
		}
		catch (Exception ex)
		{
			_log.Warn("Could not attach input mapper to window: " + ex.Message);
		}
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == 786)
		{
			int obj = ((IntPtr)wParam).ToInt32();
			this.HotkeyPressed?.Invoke(obj);
			handled = true;
		}
		return IntPtr.Zero;
	}

	public bool RegisterHotkey(string hotkey, int id)
	{
		try
		{
			if (_hwnd == IntPtr.Zero)
			{
				return false;
			}
			var (num, vk) = ParseHotkey(hotkey);
			NativeMethods.UnregisterHotKey(_hwnd, id);
			if (NativeMethods.RegisterHotKey(_hwnd, id, num | 0x4000u, vk))
			{
				_hotkeys[id] = hotkey;
				_log.Info($"Registered hotkey '{hotkey}' (id {id}).");
				return true;
			}
			_log.Warn("Failed to register hotkey '" + hotkey + "' (already in use?).");
			return false;
		}
		catch (Exception ex)
		{
			_log.Warn("RegisterHotkey error: " + ex.Message);
			return false;
		}
	}

	public void UnregisterHotkey(int id)
	{
		try
		{
			if (_hwnd != IntPtr.Zero)
			{
				NativeMethods.UnregisterHotKey(_hwnd, id);
			}
		}
		catch
		{
		}
	}

	public static (uint mods, uint vk) ParseHotkey(string hotkey)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		uint num = 0u;
		uint item = 0u;
		string[] array = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			switch (text.ToUpperInvariant())
			{
			case "CTRL":
			case "CONTROL":
				num |= 2u;
				continue;
			case "ALT":
				num |= 1u;
				continue;
			case "SHIFT":
				num |= 4u;
				continue;
			}
			if (Enum.TryParse<Key>(text, ignoreCase: true, out Key result))
			{
				item = (uint)KeyInterop.VirtualKeyFromKey(result);
			}
		}
		return (mods: num, vk: item);
	}

	public void StartHooks()
	{
		if (!_running)
		{
			_running = true;
			_hookThread = new Thread(HookThreadProc)
			{
				IsBackground = true,
				Name = "HPX-InputHook",
				Priority = ThreadPriority.AboveNormal
			};
			_hookThread.SetApartmentState(ApartmentState.STA);
			_hookThread.Start();
		}
	}

	private void HookThreadProc()
	{
		try
		{
			_hookThreadId = GetCurrentThreadId();
			_keyProc = KeyboardProc;
			_mouseProc = MouseProc;
			nint moduleHandle = InputHook.GetModuleHandle(null);
			_keyboardHook = InputHook.SetWindowsHookEx(13, _keyProc, moduleHandle, 0u);
			_mouseHook = InputHook.SetWindowsHookEx(14, _mouseProc, moduleHandle, 0u);
			if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
			{
				_log.Warn("One or more input hooks failed to install (try running as administrator).");
			}
			else
			{
				_log.Info("Input hooks installed.");
			}
			MSG lpMsg;
			while (_running && GetMessage(out lpMsg, IntPtr.Zero, 0u, 0u) > 0)
			{
				TranslateMessage(ref lpMsg);
				DispatchMessage(ref lpMsg);
			}
		}
		catch (Exception ex)
		{
			_log.Error("Input hook thread error.", ex);
		}
		finally
		{
			if (_keyboardHook != IntPtr.Zero)
			{
				InputHook.UnhookWindowsHookEx(_keyboardHook);
			}
			if (_mouseHook != IntPtr.Zero)
			{
				InputHook.UnhookWindowsHookEx(_mouseHook);
			}
			_keyboardHook = (_mouseHook = IntPtr.Zero);
		}
	}

	private nint KeyboardProc(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0)
		{
			InputHook.KBDLLHOOKSTRUCT kBDLLHOOKSTRUCT = Marshal.PtrToStructure<InputHook.KBDLLHOOKSTRUCT>(lParam);
			int num = ((IntPtr)wParam).ToInt32();
			bool flag = ((num == 256 || num == 260) ? true : false);
			bool flag2 = flag;
			flag = ((num == 257 || num == 261) ? true : false);
			bool flag3 = flag;
			if (flag2 || flag3)
			{
				string obj = KeyName(kBDLLHOOKSTRUCT.vkCode);
				try
				{
					if (flag2)
					{
						this.KeyDown?.Invoke(obj);
					}
					else
					{
						this.KeyUp?.Invoke(obj);
					}
				}
				catch
				{
				}
			}
		}
		return InputHook.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
	}

	private nint MouseProc(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0)
		{
			InputHook.MSLLHOOKSTRUCT mSLLHOOKSTRUCT = Marshal.PtrToStructure<InputHook.MSLLHOOKSTRUCT>(lParam);
			int num = ((IntPtr)wParam).ToInt32();
			try
			{
				switch (num)
				{
				case 512:
					this.MouseMoved?.Invoke(mSLLHOOKSTRUCT.pt.x, mSLLHOOKSTRUCT.pt.y);
					break;
				case 513:
					this.MouseButtonDown?.Invoke("MouseLeft");
					break;
				case 516:
					this.MouseButtonDown?.Invoke("MouseRight");
					break;
				case 519:
					this.MouseButtonDown?.Invoke("MouseMiddle");
					break;
				case 522:
				{
					int num2 = (short)((mSLLHOOKSTRUCT.mouseData >> 16) & 0xFFFF);
					this.MouseButtonDown?.Invoke((num2 > 0) ? "WheelUp" : "WheelDown");
					break;
				}
				}
			}
			catch
			{
			}
		}
		return InputHook.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
	}

	public static string KeyName(uint vk)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Key val = KeyInterop.KeyFromVirtualKey((int)vk);
			return ((int)val == 0) ? $"VK{vk:X2}" : ((object)(Key)(val)).ToString();
		}
		catch
		{
			return $"VK{vk:X2}";
		}
	}

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	private static extern int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll")]
	private static extern bool TranslateMessage(ref MSG lpMsg);

	[DllImport("user32.dll")]
	private static extern nint DispatchMessage(ref MSG lpMsg);

	public void Dispose()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		_running = false;
		try
		{
			foreach (int item in _hotkeys.Keys.ToList())
			{
				UnregisterHotkey(item);
			}
			if (_hookThreadId != 0)
			{
				PostThreadMessage(_hookThreadId, 18u, IntPtr.Zero, IntPtr.Zero);
			}
			_hookThread?.Join(500);
		}
		catch
		{
		}
		HwndSource? source = _source;
		if (source != null)
		{
			source.RemoveHook(new HwndSourceHook(WndProc));
		}
	}

	[DllImport("user32.dll")]
	private static extern bool PostThreadMessage(uint idThread, uint Msg, nint wParam, nint lParam);
}
