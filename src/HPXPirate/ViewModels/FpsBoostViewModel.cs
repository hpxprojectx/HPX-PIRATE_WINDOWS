using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

/// <summary>
/// View model for the Real-Time FPS Boost page. The live FPS numbers come from the
/// ETW (DxgKrnl Present) monitor \u2014 the same frames the GPU actually presents \u2014 so
/// nothing here is simulated or faked. The boost itself applies only real, reversible
/// Windows changes, and only where a change is actually needed.
/// </summary>
public sealed class FpsBoostViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private double _liveFps;

	private double _liveAverageFps;

	private double _liveFrameTime;

	private double _liveOneLow;

	private double _fpsBefore;

	private double _fpsAfter;

	private string _summary = "";

	private string _note = "";

	private string _status = "Start your game, then press BOOST NOW. FPS is read live from the GPU's Present events (ETW).";

	private bool _isBoosting;

	private int _measureSeconds = 3;

	public ObservableCollection<FpsBoostStep> Steps { get; } = new ObservableCollection<FpsBoostStep>();

	public int TargetFps
	{
		get
		{
			return _host.AppSettings.TargetFps;
		}
		set
		{
			_host.AppSettings.TargetFps = value;
			OnPropertyChanged("TargetFps");
		}
	}

	/// <summary>How long each measurement window runs (seconds).</summary>
	public int MeasureSeconds
	{
		get
		{
			return _measureSeconds;
		}
		set
		{
			SetProperty(ref _measureSeconds, value, "MeasureSeconds");
		}
	}

	public double LiveFps
	{
		get
		{
			return _liveFps;
		}
		private set
		{
			SetProperty(ref _liveFps, value, "LiveFps");
			OnPropertyChanged("LiveFpsText");
		}
	}

	public double LiveAverageFps
	{
		get
		{
			return _liveAverageFps;
		}
		private set
		{
			SetProperty(ref _liveAverageFps, value, "LiveAverageFps");
			OnPropertyChanged("LiveAverageFpsText");
		}
	}

	public double LiveFrameTime
	{
		get
		{
			return _liveFrameTime;
		}
		private set
		{
			SetProperty(ref _liveFrameTime, value, "LiveFrameTime");
			OnPropertyChanged("LiveFrameTimeText");
		}
	}

	public double LiveOnePercentLow
	{
		get
		{
			return _liveOneLow;
		}
		private set
		{
			SetProperty(ref _liveOneLow, value, "LiveOnePercentLow");
			OnPropertyChanged("LiveOnePercentLowText");
		}
	}

	public string LiveFpsText => LiveFps > 0.0 ? LiveFps.ToString("0") : "--";

	public string LiveAverageFpsText => LiveAverageFps > 0.0 ? LiveAverageFps.ToString("0") : "--";

	public string LiveFrameTimeText => LiveFrameTime > 0.0 ? (LiveFrameTime.ToString("0.0") + " ms") : "-- ms";

	public string LiveOnePercentLowText => LiveOnePercentLow > 0.0 ? LiveOnePercentLow.ToString("0") : "--";

	public double FpsBefore
	{
		get
		{
			return _fpsBefore;
		}
		private set
		{
			SetProperty(ref _fpsBefore, value, "FpsBefore");
			OnPropertyChanged("FpsBeforeText");
			OnPropertyChanged("DeltaText");
		}
	}

	public double FpsAfter
	{
		get
		{
			return _fpsAfter;
		}
		private set
		{
			SetProperty(ref _fpsAfter, value, "FpsAfter");
			OnPropertyChanged("FpsAfterText");
			OnPropertyChanged("DeltaText");
		}
	}

	public string FpsBeforeText => FpsBefore > 0.0 ? FpsBefore.ToString("0") : "--";

	public string FpsAfterText => FpsAfter > 0.0 ? FpsAfter.ToString("0") : "--";

	public string DeltaText
	{
		get
		{
			if (FpsBefore <= 0.0 || FpsAfter <= 0.0)
			{
				return "--";
			}
			double delta = FpsAfter - FpsBefore;
			string sign = ((delta >= 0.0) ? "+" : "-");
			return $"{sign}{Math.Abs(delta):0} FPS";
		}
	}

	public string MonitorSource => _host.FpsBoost.SourceName;

	public bool MonitorAvailable => _host.FpsBoost.IsMonitorAvailable;

	public string MonitorStatusText => MonitorAvailable
		? ("Live source: " + MonitorSource)
		: "FPS monitor idle \u2014 start a game to see live FPS.";

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

	public string Note
	{
		get
		{
			return _note;
		}
		private set
		{
			SetProperty(ref _note, value, "Note");
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

	public bool IsBoosting
	{
		get
		{
			return _isBoosting;
		}
		private set
		{
			SetProperty(ref _isBoosting, value, "IsBoosting");
			((RelayCommand)BoostCommand).RaiseCanExecuteChanged();
			((RelayCommand)RevertCommand).RaiseCanExecuteChanged();
		}
	}

	public ICommand BoostCommand { get; }

	public ICommand RevertCommand { get; }

	public ICommand ResetHistoryCommand { get; }

	public FpsBoostViewModel(AppHost host)
	{
		_host = host;
		BoostCommand = new RelayCommand(async delegate
		{
			await BoostAsync();
		}, () => !IsBoosting);
		RevertCommand = new RelayCommand(async delegate
		{
			await RevertAsync();
		}, () => !IsBoosting && _host.FpsBoost.Changes.Any((ChangeRecord c) => !c.Reverted));
		ResetHistoryCommand = new RelayCommand(() =>
		{
			_host.FpsBoost.ResetFpsHistory();
			Status = "FPS history cleared \u2014 fresh measurement will start now.";
		});
		_host.Monitor.MetricsUpdated += OnMetrics;
	}

	private void OnMetrics(PerformanceMetrics m)
	{
		PerformanceMetrics i = m;
		Application current = Application.Current;
		if (current == null)
		{
			return;
		}
		((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
		{
			LiveFps = i.Fps;
			LiveAverageFps = i.AverageFps;
			LiveFrameTime = i.FrameTimeMs;
			LiveOnePercentLow = i.OnePercentLow;
			OnPropertyChanged("MonitorSource");
			OnPropertyChanged("MonitorAvailable");
			OnPropertyChanged("MonitorStatusText");
		});
	}

	private async Task BoostAsync()
	{
		IsBoosting = true;
		Steps.Clear();
		Summary = "";
		Note = "";
		int seconds = Math.Clamp(MeasureSeconds, 1, 10);
		Status = $"Measuring baseline FPS for {seconds}s (real-time, ETW / DxgKrnl)...";
		_host.FpsBoost.ResetFpsHistory();
		await Task.Delay(seconds * 1000);
		FpsBefore = _host.FpsBoost.CurrentFps;

		int target = TargetFps;
		bool restore = _host.AppSettings.CreateRestorePoints;
		FpsBoostResult result = await Task.Run(() => _host.FpsBoost.Apply(target, restore));

		Status = $"Boost applied. Re-measuring FPS for {seconds}s...";
		_host.FpsBoost.ResetFpsHistory();
		await Task.Delay(seconds * 1000);
		_host.FpsBoost.CompleteMeasurement(result);

		foreach (FpsBoostStep step in result.Steps)
		{
			Steps.Add(step);
		}
		FpsBefore = result.FpsBefore;
		FpsAfter = result.FpsAfter;
		Summary = result.Summary;
		Note = result.Note;
		Status = (result.Success ? "Boost complete." : "Boost finished with some steps needing attention.");
		((RelayCommand)RevertCommand).RaiseCanExecuteChanged();
		IsBoosting = false;
	}

	private async Task RevertAsync()
	{
		IsBoosting = true;
		Status = "Reverting FPS boost changes...";
		int count = await Task.Run(() => _host.FpsBoost.RollbackAll());
		Status = ((count > 0) ? $"Reverted {count} change(s)." : "Nothing to revert.");
		((RelayCommand)RevertCommand).RaiseCanExecuteChanged();
		IsBoosting = false;
	}
}
