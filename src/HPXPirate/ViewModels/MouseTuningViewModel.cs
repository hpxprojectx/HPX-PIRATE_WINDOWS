using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

/// <summary>
/// View model for the Mouse Tuning page. Wraps <see cref="MouseTuningService"/>,
/// which reads the live Windows mouse state first and only writes a value when it
/// actually differs. That is what stops the app from "fixing" a setting that was
/// never broken: healthy values are reported as "Already optimal" and left alone.
/// </summary>
public sealed class MouseTuningViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private MouseLiveState _live = new MouseLiveState();

	private string _status = "Read your live Windows mouse settings, then apply only what needs changing.";

	private string _summary = "";

	private bool _busy;

	private MouseTuningSettings S => _host.AppSettings.MouseTuning;

	public ObservableCollection<TuningStep> Steps { get; } = new ObservableCollection<TuningStep>();

	/// <summary>Live values read straight from the OS (never modified by reading).</summary>
	public MouseLiveState Live
	{
		get
		{
			return _live;
		}
		private set
		{
			_live = value;
			OnPropertyChanged("Live");
		}
	}

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

	public string Summary
	{
		get
		{
			return _summary;
		}
		private set
		{
			SetProperty(ref _summary, value, "Summary");
		}
	}

	public bool Busy
	{
		get
		{
			return _busy;
		}
		private set
		{
			SetProperty(ref _busy, value, "Busy");
			((RelayCommand)ApplyCommand).RaiseCanExecuteChanged();
			((RelayCommand)RevertCommand).RaiseCanExecuteChanged();
			((RelayCommand)RestoreDefaultsCommand).RaiseCanExecuteChanged();
		}
	}

	public bool Enabled
	{
		get
		{
			return S.Enabled;
		}
		set
		{
			S.Enabled = value;
			OnPropertyChanged("Enabled");
		}
	}

	public bool DisableAcceleration
	{
		get
		{
			return S.DisableAcceleration;
		}
		set
		{
			S.DisableAcceleration = value;
			OnPropertyChanged("DisableAcceleration");
		}
	}

	public int PointerSpeed
	{
		get
		{
			return S.PointerSpeed;
		}
		set
		{
			S.PointerSpeed = value;
			OnPropertyChanged("PointerSpeed");
		}
	}

	public int ScrollLines
	{
		get
		{
			return S.ScrollLines;
		}
		set
		{
			S.ScrollLines = value;
			OnPropertyChanged("ScrollLines");
		}
	}

	public int DoubleClickSpeed
	{
		get
		{
			return S.DoubleClickSpeed;
		}
		set
		{
			S.DoubleClickSpeed = value;
			OnPropertyChanged("DoubleClickSpeed");
		}
	}

	public bool SwapButtons
	{
		get
		{
			return S.SwapButtons;
		}
		set
		{
			S.SwapButtons = value;
			OnPropertyChanged("SwapButtons");
		}
	}

	public bool SnapToDefault
	{
		get
		{
			return S.SnapToDefault;
		}
		set
		{
			S.SnapToDefault = value;
			OnPropertyChanged("SnapToDefault");
		}
	}

	public bool ClickLock
	{
		get
		{
			return S.ClickLock;
		}
		set
		{
			S.ClickLock = value;
			OnPropertyChanged("ClickLock");
		}
	}

	public int PointerTrails
	{
		get
		{
			return S.PointerTrails;
		}
		set
		{
			S.PointerTrails = value;
			OnPropertyChanged("PointerTrails");
		}
	}

	public ICommand ApplyCommand { get; }

	public ICommand RevertCommand { get; }

	public ICommand RestoreDefaultsCommand { get; }

	public ICommand RefreshCommand { get; }

	public MouseTuningViewModel(AppHost host)
	{
		_host = host;
		ApplyCommand = new RelayCommand(async delegate
		{
			await ApplyAsync();
		}, () => !Busy);
		RevertCommand = new RelayCommand(async delegate
		{
			await RevertAsync();
		}, () => !Busy && _host.MouseTuning.LastSnapshot != null);
		RestoreDefaultsCommand = new RelayCommand(async delegate
		{
			await RestoreDefaultsAsync();
		}, () => !Busy);
		RefreshCommand = new RelayCommand(RefreshLive);
		RefreshLive();
	}

	private void RefreshLive()
	{
		try
		{
			Live = _host.MouseTuning.ReadLive();
		}
		catch (Exception ex)
		{
			Status = "Could not read mouse settings: " + ex.Message;
		}
	}

	private async Task ApplyAsync()
	{
		Busy = true;
		Status = "Applying mouse profile...";
		S.Enabled = true;
		MouseTuningSettings settings = S;
		List<TuningStep> steps = await Task.Run(() => _host.MouseTuning.Apply(settings));
		_host.Settings.Save();
		Publish(steps);
		RefreshLive();
		Busy = false;
	}

	private async Task RevertAsync()
	{
		Busy = true;
		Status = "Reverting mouse settings to their previous values...";
		List<TuningStep> steps = await Task.Run(() => _host.MouseTuning.Revert());
		_host.Settings.Save();
		Publish(steps);
		RefreshLive();
		Busy = false;
	}

	private async Task RestoreDefaultsAsync()
	{
		Busy = true;
		Status = "Restoring Windows default mouse settings...";
		List<TuningStep> steps = await Task.Run(() => _host.MouseTuning.RestoreDefaults());
		_host.Settings.Save();
		Publish(steps);
		RefreshLive();
		Busy = false;
	}

	private void Publish(List<TuningStep> steps)
	{
		Steps.Clear();
		foreach (TuningStep step in steps)
		{
			Steps.Add(step);
		}
		int applied = steps.Count((TuningStep s) => s.Changed);
		int already = steps.Count((TuningStep s) => s.AlreadyOptimal);
		int failed = steps.Count((TuningStep s) => !s.Success);
		int skipped = steps.Count((TuningStep s) => s.Status == "Skipped");
		List<string> parts = new List<string> { $"{applied} applied" };
		if (already > 0)
		{
			parts.Add($"{already} already optimal");
		}
		if (skipped > 0)
		{
			parts.Add($"{skipped} skipped");
		}
		if (failed > 0)
		{
			parts.Add($"{failed} failed");
		}
		string head = string.Join(", ", parts) + ".";
		Summary = ((applied == 0 && failed == 0)
			? (head + " Nothing needed changing \u2014 your mouse settings were already ideal.")
			: (head + " Only settings that actually needed a change were modified."));
		Status = Summary;
	}
}
