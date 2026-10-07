using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class CrosshairViewModel : ViewModelBase
{
	private readonly AppHost _host;

	public const int ToggleHotkeyId = 16385;

	private string _status = "";

	private CrosshairSettings C => _host.AppSettings.Crosshair;

	public string Status
	{
		get
		{
			return _status;
		}
		private set
		{
			SetProperty(ref _status, value, "Status");
		}
	}

	public bool IsOverlayVisible => _host.Crosshair.IsVisible;

	public bool Enabled
	{
		get
		{
			return C.Enabled;
		}
		set
		{
			C.Enabled = value;
			OnPropertyChanged("Enabled");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public string Shape
	{
		get
		{
			return C.Shape;
		}
		set
		{
			C.Shape = value;
			OnPropertyChanged("Shape");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public double Size
	{
		get
		{
			return C.Size;
		}
		set
		{
			C.Size = value;
			OnPropertyChanged("Size");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public double Thickness
	{
		get
		{
			return C.Thickness;
		}
		set
		{
			C.Thickness = value;
			OnPropertyChanged("Thickness");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public double Gap
	{
		get
		{
			return C.Gap;
		}
		set
		{
			C.Gap = value;
			OnPropertyChanged("Gap");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public double Opacity
	{
		get
		{
			return C.Opacity;
		}
		set
		{
			C.Opacity = value;
			OnPropertyChanged("Opacity");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public string Color
	{
		get
		{
			return C.Color;
		}
		set
		{
			C.Color = value;
			OnPropertyChanged("Color");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public bool CenterDot
	{
		get
		{
			return C.CenterDot;
		}
		set
		{
			C.CenterDot = value;
			OnPropertyChanged("CenterDot");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public bool Outline
	{
		get
		{
			return C.Outline;
		}
		set
		{
			C.Outline = value;
			OnPropertyChanged("Outline");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public string ToggleHotkey
	{
		get
		{
			return C.ToggleHotkey;
		}
		set
		{
			C.ToggleHotkey = value;
			OnPropertyChanged("ToggleHotkey");
		}
	}

	public double OffsetX
	{
		get
		{
			return C.OffsetX;
		}
		set
		{
			C.OffsetX = value;
			OnPropertyChanged("OffsetX");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public double OffsetY
	{
		get
		{
			return C.OffsetY;
		}
		set
		{
			C.OffsetY = value;
			OnPropertyChanged("OffsetY");
			_host.Crosshair.ApplySettings(C);
		}
	}

	public string[] Shapes { get; } = new string[4] { "Cross", "Dot", "Circle", "CrossDot" };


	public string[] Colors { get; } = new string[7] { "#FF2BD6", "#2BE7FF", "#35E08A", "#FFB020", "#FF4D6D", "#FFFFFF", "#000000" };


	public ICommand ShowCommand { get; }

	public ICommand HideCommand { get; }

	public ICommand ToggleCommand { get; }

	public ICommand SaveCommand { get; }

	public ICommand ApplyHotkeyCommand { get; }

	public CrosshairViewModel(AppHost host)
	{
		_host = host;
		ShowCommand = new RelayCommand((Action)delegate
		{
			_host.Crosshair.ApplySettings(C);
			_host.Crosshair.Show();
			OnPropertyChanged("IsOverlayVisible");
		}, (Func<bool>?)null);
		HideCommand = new RelayCommand((Action)delegate
		{
			_host.Crosshair.Hide();
			OnPropertyChanged("IsOverlayVisible");
		}, (Func<bool>?)null);
		ToggleCommand = new RelayCommand((Action)delegate
		{
			_host.Crosshair.ApplySettings(C);
			_host.Crosshair.Toggle();
			OnPropertyChanged("IsOverlayVisible");
		}, (Func<bool>?)null);
		SaveCommand = new RelayCommand((Action)delegate
		{
			_host.Settings.Save();
			_host.Crosshair.ApplySettings(C);
			Status = "Crosshair saved.";
		}, (Func<bool>?)null);
		ApplyHotkeyCommand = new RelayCommand(ApplyHotkey);
	}

	private void ApplyHotkey()
	{
		_host.Input.UnregisterHotkey(16385);
		if (_host.Input.RegisterHotkey(ToggleHotkey, 16385))
		{
			Status = "Toggle hotkey '" + ToggleHotkey + "' registered.";
		}
		else
		{
			Status = "Could not register '" + ToggleHotkey + "' (in use?).";
		}
	}

	public void OnHotkey(int id)
	{
		if (id != 16385)
		{
			return;
		}
		Application current = Application.Current;
		if (current != null)
		{
			((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
			{
				_host.Crosshair.ApplySettings(C);
				_host.Crosshair.Toggle();
				OnPropertyChanged("IsOverlayVisible");
			});
		}
	}
}
