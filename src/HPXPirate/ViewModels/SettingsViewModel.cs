using System;
using System.Windows;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private string _status = "";

	private AppSettings S => _host.AppSettings;

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

	public bool StartWithWindows
	{
		get
		{
			return S.StartWithWindows;
		}
		set
		{
			S.StartWithWindows = value;
			_host.Startup.SetEnabled(value);
			OnPropertyChanged("StartWithWindows");
		}
	}

	public bool MinimizeToTray
	{
		get
		{
			return S.MinimizeToTray;
		}
		set
		{
			S.MinimizeToTray = value;
			OnPropertyChanged("MinimizeToTray");
		}
	}

	public string Language
	{
		get
		{
			return S.Language;
		}
		set
		{
			S.Language = value;
			OnPropertyChanged("Language");
		}
	}

	public string Theme
	{
		get
		{
			return S.Theme;
		}
		set
		{
			S.Theme = value;
			OnPropertyChanged("Theme");
		}
	}

	public OptimizationMode DefaultBoostMode
	{
		get
		{
			return S.DefaultBoostMode;
		}
		set
		{
			S.DefaultBoostMode = value;
			OnPropertyChanged("DefaultBoostMode");
		}
	}

	public bool AutoOptimize
	{
		get
		{
			return S.AutoOptimize;
		}
		set
		{
			S.AutoOptimize = value;
			OnPropertyChanged("AutoOptimize");
		}
	}

	public int MonitoringIntervalMs
	{
		get
		{
			return S.MonitoringIntervalMs;
		}
		set
		{
			S.MonitoringIntervalMs = value;
			_host.Monitor.ApplyIntervalSettings();
			OnPropertyChanged("MonitoringIntervalMs");
		}
	}

	public int IdleMonitoringIntervalMs
	{
		get
		{
			return S.IdleMonitoringIntervalMs;
		}
		set
		{
			S.IdleMonitoringIntervalMs = value;
			OnPropertyChanged("IdleMonitoringIntervalMs");
		}
	}

	public bool ReduceMonitoringWhenIdle
	{
		get
		{
			return S.ReduceMonitoringWhenIdle;
		}
		set
		{
			S.ReduceMonitoringWhenIdle = value;
			OnPropertyChanged("ReduceMonitoringWhenIdle");
		}
	}

	public bool AutoLoadProfiles
	{
		get
		{
			return S.AutoLoadProfiles;
		}
		set
		{
			S.AutoLoadProfiles = value;
			OnPropertyChanged("AutoLoadProfiles");
		}
	}

	public bool CreateRestorePoints
	{
		get
		{
			return S.CreateRestorePoints;
		}
		set
		{
			S.CreateRestorePoints = value;
			OnPropertyChanged("CreateRestorePoints");
		}
	}

	public bool BackupBeforeChanges
	{
		get
		{
			return S.BackupBeforeChanges;
		}
		set
		{
			S.BackupBeforeChanges = value;
			OnPropertyChanged("BackupBeforeChanges");
		}
	}

	public bool SafeMode
	{
		get
		{
			return S.SafeMode;
		}
		set
		{
			S.SafeMode = value;
			OnPropertyChanged("SafeMode");
		}
	}

	public bool DiagnosticMode
	{
		get
		{
			return S.DiagnosticMode;
		}
		set
		{
			S.DiagnosticMode = value;
			OnPropertyChanged("DiagnosticMode");
		}
	}

	public string[] Languages { get; } = new string[2] { "en", "id" };


	public string[] Themes { get; } = new string[3] { "Dark", "Midnight", "Neon" };


	public OptimizationMode[] Modes { get; } = new OptimizationMode[4]
	{
		OptimizationMode.Balanced,
		OptimizationMode.Performance,
		OptimizationMode.Extreme,
		OptimizationMode.Custom
	};


	public ICommand SaveCommand { get; }

	public ICommand ResetCommand { get; }

	public ICommand RestorePointCommand { get; }

	public SettingsViewModel(AppHost host)
	{
		_host = host;
		SaveCommand = new RelayCommand(Save);
		ResetCommand = new RelayCommand(ResetAll);
		RestorePointCommand = new RelayCommand((Action)delegate
		{
			_host.Restore.TryCreateRestorePoint("HPX PIRATE manual restore point", out string message);
			Status = message;
		}, (Func<bool>?)null);
	}

	private void Save()
	{
		_host.Settings.Save();
		Status = "Settings saved.";
	}

	private void ResetAll()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		if ((int)MessageBox.Show("Reset ALL HPX PIRATE settings to defaults?\nThis does not touch Windows settings or your profiles.", "Reset Settings", (MessageBoxButton)4, (MessageBoxImage)48) == 6)
		{
			_host.Settings.ResetToDefaults();
			OnPropertyChanged(string.Empty);
			Status = "Settings reset to defaults.";
		}
	}
}
