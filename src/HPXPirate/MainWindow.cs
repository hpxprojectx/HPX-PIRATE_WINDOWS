using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;
using HPXPirate.Interop;
using HPXPirate.Services;
using HPXPirate.Views;

namespace HPXPirate;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly AppHost _host = App.Host;

	private readonly Dictionary<string, System.Windows.Controls.UserControl> _views = new Dictionary<string, System.Windows.Controls.UserControl>();

	private NotifyIcon? _tray;

	private bool _reallyClose;

	private static readonly Dictionary<string, string> SectionTitles = new Dictionary<string, string>
	{
		["dashboard"] = "Dashboard",
		["performance"] = "Performance Engine",
		["profiles"] = "Game Profiles",
		["mapper"] = "Control Mapper",
		["dragassist"] = "Drag Assist",
		["easydrag"] = "Easy Drag Lab",
		["crosshair"] = "Crosshair",
		["monitor"] = "Monitor",
		["optimizer"] = "System Optimizer",
		["mousetuning"] = "Mouse Tuning",
		["keyboardtuning"] = "Keyboard Tuning",
		["fpsboost"] = "FPS Boost",
		["settings"] = "Settings"
	};






	public MainWindow()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		InitializeComponent();
		((Window)this).SourceInitialized += OnSourceInitialized;
		((FrameworkElement)this).Loaded += new RoutedEventHandler(OnLoaded);
		((Window)this).StateChanged += OnStateChanged;
		((Window)this).Closing += OnClosing;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_host.Input.AttachToWindow((Window)(object)this);
			_host.Input.StartHooks();
			_host.Input.HotkeyPressed += OnGlobalHotkey;
			_host.Input.RegisterHotkey(_host.AppSettings.Crosshair.ToggleHotkey, 16385);
			_host.Monitor.Start();
			InitTray();
			ShowView("dashboard");
		}
		catch (Exception ex)
		{
			_host.Logger.Error("MainWindow initialisation error.", ex);
		}
	}

	private void OnGlobalHotkey(int id)
	{
		if (id == 16385)
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				_host.Crosshair.ApplySettings(_host.AppSettings.Crosshair);
				_host.Crosshair.Toggle();
			});
		}
	}

	private void Nav_Checked(object sender, RoutedEventArgs e)
	{
		System.Windows.Controls.RadioButton val = (System.Windows.Controls.RadioButton)((sender is System.Windows.Controls.RadioButton) ? sender : null);
		if (val != null && ((FrameworkElement)val).Tag is string key)
		{
			ShowView(key);
		}
	}

	private void ShowView(string key)
	{
		if (!_views.TryGetValue(key, out System.Windows.Controls.UserControl value))
		{
			value = (System.Windows.Controls.UserControl)(key switch
			{
				"performance" => new PerformanceView(), 
				"profiles" => new GameProfilesView(), 
				"mapper" => new ControlMapperView(), 
				"dragassist" => new DragAssistView(), 
				"easydrag" => new EasyDragLabView(), 
				"crosshair" => new CrosshairView(), 
				"monitor" => new MonitorView(), 
				"optimizer" => new SystemOptimizerView(), 
				"mousetuning" => new MouseTuningView(), 
				"keyboardtuning" => new KeyboardTuningView(), 
				"fpsboost" => new FpsBoostView(), 
				"settings" => new SettingsView(), 
				_ => new DashboardView(), 
			});
			_views[key] = value;
		}
		ContentHost.Content = value;
		BreadcrumbText.Text = (SectionTitles.TryGetValue(key, out string value2) ? value2 : "Dashboard");
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		if (e.ClickCount == 2)
		{
			ToggleMaximise();
		}
		else if ((int)e.ButtonState == 1)
		{
			try
			{
				((Window)this).DragMove();
			}
			catch
			{
			}
		}
	}

	private void Minimise_Click(object sender, RoutedEventArgs e)
	{
		((Window)this).WindowState = (WindowState)1;
	}

	private void Maximise_Click(object sender, RoutedEventArgs e)
	{
		ToggleMaximise();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		((Window)this).Close();
	}

	private void ToggleMaximise()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		((Window)this).WindowState = (WindowState)(((int)((Window)this).WindowState != 2) ? 2 : 0);
	}

	private void OnSourceInitialized(object? sender, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		try
		{
			nint handle = new WindowInteropHelper((Window)(object)this).Handle;
			int attrValue = 1;
			try
			{
				NativeMethods.DwmSetWindowAttribute(handle, 20, ref attrValue, 4);
			}
			catch
			{
			}
			try
			{
				NativeMethods.DwmSetWindowAttribute(handle, 19, ref attrValue, 4);
			}
			catch
			{
			}
			HwndSource obj3 = HwndSource.FromHwnd((IntPtr)handle);
			if (obj3 != null)
			{
				obj3.AddHook(new HwndSourceHook(WndProc));
			}
		}
		catch (Exception ex)
		{
			_host.Logger.Warn("Window chrome init failed: " + ex.Message);
		}
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == 36)
		{
			try
			{
				NativeMethods.MINMAXINFO structure = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
				nint num = NativeMethods.MonitorFromWindow(hwnd, 2u);
				if (num != IntPtr.Zero)
				{
					NativeMethods.MONITORINFO mONITORINFO = default(NativeMethods.MONITORINFO);
					mONITORINFO.cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>();
					NativeMethods.MONITORINFO lpmi = mONITORINFO;
					if (NativeMethods.GetMonitorInfo(num, ref lpmi))
					{
						structure.ptMaxPosition.X = lpmi.rcWork.Left - lpmi.rcMonitor.Left;
						structure.ptMaxPosition.Y = lpmi.rcWork.Top - lpmi.rcMonitor.Top;
						structure.ptMaxSize.X = lpmi.rcWork.Right - lpmi.rcWork.Left;
						structure.ptMaxSize.Y = lpmi.rcWork.Bottom - lpmi.rcWork.Top;
						structure.ptMaxTrackSize.X = structure.ptMaxSize.X;
						structure.ptMaxTrackSize.Y = structure.ptMaxSize.Y;
						Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
						handled = true;
					}
				}
			}
			catch
			{
			}
		}
		return IntPtr.Zero;
	}

	private void InitTray()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		try
		{
			Icon val = null;
			try
			{
				StreamResourceInfo resourceStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/hpx.ico"));
				Stream stream = ((resourceStream != null) ? resourceStream.Stream : null);
				if (stream != null)
				{
					val = new Icon(stream);
				}
			}
			catch
			{
			}
			_tray = new NotifyIcon
			{
				Text = "HPX PIRATE",
				Visible = true,
				Icon = (val ?? SystemIcons.Application)
			};
			_tray.DoubleClick += delegate
			{
				RestoreFromTray();
			};
			ContextMenuStrip val2 = new ContextMenuStrip();
			((ToolStrip)val2).Items.Add("Open HPX PIRATE", (System.Drawing.Image)null, (EventHandler)delegate
			{
				RestoreFromTray();
			});
			((ToolStrip)val2).Items.Add("Toggle Crosshair", (System.Drawing.Image)null, (EventHandler)delegate
			{
				_host.Crosshair.ApplySettings(_host.AppSettings.Crosshair);
				_host.Crosshair.Toggle();
			});
			((ToolStrip)val2).Items.Add((ToolStripItem)new ToolStripSeparator());
			((ToolStrip)val2).Items.Add("Exit", (System.Drawing.Image)null, (EventHandler)delegate
			{
				_reallyClose = true;
				((Window)this).Close();
			});
			_tray.ContextMenuStrip = val2;
		}
		catch (Exception ex)
		{
			_host.Logger.Warn("Tray icon unavailable: " + ex.Message);
		}
	}

	private void RestoreFromTray()
	{
		((Window)this).Show();
		((Window)this).WindowState = (WindowState)0;
		((Window)this).Activate();
	}

	private void OnStateChanged(object? sender, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((Window)this).WindowState == 1 && _host.AppSettings.MinimizeToTray)
		{
			try
			{
				((Window)this).Hide();
			}
			catch
			{
			}
		}
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (!_reallyClose && _host.AppSettings.MinimizeToTray && !_host.AppSettings.SafeMode)
		{
			e.Cancel = true;
			try
			{
				((Window)this).Hide();
				return;
			}
			catch
			{
				return;
			}
		}
		try
		{
			((Component)(object)_tray)?.Dispose();
			_tray = null;
		}
		catch
		{
		}
	}

}
