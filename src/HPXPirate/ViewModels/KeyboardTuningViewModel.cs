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
/// View model for the Keyboard Tuning page. Wraps <see cref="KeyboardTuningService"/>,
/// which reads the live Windows keyboard state first and only writes a value when it
/// actually differs. Accessibility toggles (Sticky/Filter/Toggle keys) are only ever
/// turned off, never on.
/// </summary>
public sealed class KeyboardTuningViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private KeyboardLiveState _live = new KeyboardLiveState();

	private string _status = "Read your live Windows keyboard settings, then apply only what needs changing.";

	private string _summary = "";

	private bool _busy;

	private KeyboardTuningSettings S => _host.AppSettings.KeyboardTuning;

	public ObservableCollection<TuningStep> Steps { get; } = new ObservableCollection<TuningStep>();

	/// <summary>Live values read straight from the OS (never modified by reading).</summary>
	public KeyboardLiveState Live
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

	public int RepeatDelay
	{
		get
		{
			return S.RepeatDelay;
		}
		set
		{
			S.RepeatDelay = value;
			OnPropertyChanged("RepeatDelay");
		}
	}

	public int RepeatRate
	{
		get
		{
			return S.RepeatRate;
		}
		set
		{
			S.RepeatRate = value;
			OnPropertyChanged("RepeatRate");
		}
	}

	public bool DisableStickyKeys
	{
		get
		{
			return S.DisableStickyKeys;
		}
		set
		{
			S.DisableStickyKeys = value;
			OnPropertyChanged("DisableStickyKeys");
		}
	}

	public bool DisableFilterKeys
	{
		get
		{
			return S.DisableFilterKeys;
		}
		set
		{
			S.DisableFilterKeys = value;
			OnPropertyChanged("DisableFilterKeys");
		}
	}

	public bool DisableToggleKeys
	{
		get
		{
			return S.DisableToggleKeys;
		}
		set
		{
			S.DisableToggleKeys = value;
			OnPropertyChanged("DisableToggleKeys");
		}
	}

	public ICommand ApplyCommand { get; }

	public ICommand RevertCommand { get; }

	public ICommand RestoreDefaultsCommand { get; }

	public ICommand RefreshCommand { get; }

	public KeyboardTuningViewModel(AppHost host)
	{
		_host = host;
		ApplyCommand = new RelayCommand(async delegate
		{
			await ApplyAsync();
		}, () => !Busy);
		RevertCommand = new RelayCommand(async delegate
		{
			await RevertAsync();
		}, () => !Busy && _host.KeyboardTuning.LastSnapshot != null);
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
			Live = _host.KeyboardTuning.ReadLive();
		}
		catch (Exception ex)
		{
			Status = "Could not read keyboard settings: " + ex.Message;
		}
	}

	private async Task ApplyAsync()
	{
		Busy = true;
		Status = "Applying keyboard profile...";
		S.Enabled = true;
		KeyboardTuningSettings settings = S;
		List<TuningStep> steps = await Task.Run(() => _host.KeyboardTuning.Apply(settings));
		_host.Settings.Save();
		Publish(steps);
		RefreshLive();
		Busy = false;
	}

	private async Task RevertAsync()
	{
		Busy = true;
		Status = "Reverting keyboard settings to their previous values...";
		List<TuningStep> steps = await Task.Run(() => _host.KeyboardTuning.Revert());
		_host.Settings.Save();
		Publish(steps);
		RefreshLive();
		Busy = false;
	}

	private async Task RestoreDefaultsAsync()
	{
		Busy = true;
		Status = "Restoring Windows default keyboard settings...";
		List<TuningStep> steps = await Task.Run(() => _host.KeyboardTuning.RestoreDefaults());
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
			? (head + " Nothing needed changing \u2014 your keyboard settings were already ideal.")
			: (head + " Only settings that actually needed a change were modified."));
		Status = Summary;
	}
}
