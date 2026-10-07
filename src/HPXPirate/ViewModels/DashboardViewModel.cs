using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private double _fps;

	private double _avgFps;

	private double _oneLow;

	private double _pointOneLow;

	private double _frameTime;

	private double _cpu;

	private double _gpu;

	private double _ramPercent;

	private double _vramPercent;

	private double _cpuTemp;

	private double _gpuTemp;

	private string _ramText = "-- / -- GB";

	private string _gpuName = "Detecting GPU...";

	private string _statusTitle = "Performance Stable";

	private string _statusDetail = "Waiting for data...";

	private string _statusRecommendation = "";

	private bool _statusHealthy = true;

	private List<double> _fpsSeries = new List<double>();

	private List<double> _frameTimeSeries = new List<double>();

	private List<double> _cpuSeries = new List<double>();

	private List<double> _gpuSeries = new List<double>();

	private bool _isBoosting;

	private string _boostSummary = "Press BOOST NOW to scan and optimise.";

	public double Fps
	{
		get
		{
			return _fps;
		}
		private set
		{
			SetProperty(ref _fps, value, "Fps");
		}
	}

	public double AverageFps
	{
		get
		{
			return _avgFps;
		}
		private set
		{
			SetProperty(ref _avgFps, value, "AverageFps");
		}
	}

	public double OnePercentLow
	{
		get
		{
			return _oneLow;
		}
		private set
		{
			SetProperty(ref _oneLow, value, "OnePercentLow");
		}
	}

	public double PointOnePercentLow
	{
		get
		{
			return _pointOneLow;
		}
		private set
		{
			SetProperty(ref _pointOneLow, value, "PointOnePercentLow");
		}
	}

	public double FrameTime
	{
		get
		{
			return _frameTime;
		}
		private set
		{
			SetProperty(ref _frameTime, value, "FrameTime");
		}
	}

	public double Cpu
	{
		get
		{
			return _cpu;
		}
		private set
		{
			SetProperty(ref _cpu, value, "Cpu");
		}
	}

	public double Gpu
	{
		get
		{
			return _gpu;
		}
		private set
		{
			SetProperty(ref _gpu, value, "Gpu");
		}
	}

	public double RamPercent
	{
		get
		{
			return _ramPercent;
		}
		private set
		{
			SetProperty(ref _ramPercent, value, "RamPercent");
		}
	}

	public double VramPercent
	{
		get
		{
			return _vramPercent;
		}
		private set
		{
			SetProperty(ref _vramPercent, value, "VramPercent");
		}
	}

	public double CpuTemp
	{
		get
		{
			return _cpuTemp;
		}
		private set
		{
			SetProperty(ref _cpuTemp, value, "CpuTemp");
		}
	}

	public double GpuTemp
	{
		get
		{
			return _gpuTemp;
		}
		private set
		{
			SetProperty(ref _gpuTemp, value, "GpuTemp");
		}
	}

	public string RamText
	{
		get
		{
			return _ramText;
		}
		private set
		{
			SetProperty(ref _ramText, value, "RamText");
		}
	}

	public string GpuName
	{
		get
		{
			return _gpuName;
		}
		private set
		{
			SetProperty(ref _gpuName, value, "GpuName");
		}
	}

	public string FpsDisplay
	{
		get
		{
			if (!(Fps > 0.0))
			{
				return "--";
			}
			return Fps.ToString("0");
		}
	}

	public string FrameTimeDisplay
	{
		get
		{
			if (!(FrameTime > 0.0))
			{
				return "-- ms";
			}
			return FrameTime.ToString("0.0") + " ms";
		}
	}

	public string StatusTitle
	{
		get
		{
			return _statusTitle;
		}
		private set
		{
			SetProperty(ref _statusTitle, value, "StatusTitle");
		}
	}

	public string StatusDetail
	{
		get
		{
			return _statusDetail;
		}
		private set
		{
			SetProperty(ref _statusDetail, value, "StatusDetail");
		}
	}

	public string StatusRecommendation
	{
		get
		{
			return _statusRecommendation;
		}
		private set
		{
			SetProperty(ref _statusRecommendation, value, "StatusRecommendation");
		}
	}

	public bool StatusHealthy
	{
		get
		{
			return _statusHealthy;
		}
		private set
		{
			SetProperty(ref _statusHealthy, value, "StatusHealthy");
		}
	}

	public List<double> FpsSeries
	{
		get
		{
			return _fpsSeries;
		}
		private set
		{
			SetProperty(ref _fpsSeries, value, "FpsSeries");
		}
	}

	public List<double> FrameTimeSeries
	{
		get
		{
			return _frameTimeSeries;
		}
		private set
		{
			SetProperty(ref _frameTimeSeries, value, "FrameTimeSeries");
		}
	}

	public List<double> CpuSeries
	{
		get
		{
			return _cpuSeries;
		}
		private set
		{
			SetProperty(ref _cpuSeries, value, "CpuSeries");
		}
	}

	public List<double> GpuSeries
	{
		get
		{
			return _gpuSeries;
		}
		private set
		{
			SetProperty(ref _gpuSeries, value, "GpuSeries");
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
		}
	}

	public string BoostSummary
	{
		get
		{
			return _boostSummary;
		}
		private set
		{
			SetProperty(ref _boostSummary, value, "BoostSummary");
		}
	}

	public ObservableCollection<BoostStepResult> BoostSteps { get; } = new ObservableCollection<BoostStepResult>();


	public ICommand BoostCommand { get; }

	public DashboardViewModel(AppHost host)
	{
		_host = host;
		BoostCommand = new RelayCommand(async delegate
		{
			await RunBoostAsync();
		}, () => !IsBoosting);
		_host.Monitor.MetricsUpdated += OnMetrics;
		_host.Monitor.VerdictUpdated += OnVerdict;
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
			Fps = i.Fps;
			AverageFps = i.AverageFps;
			OnePercentLow = i.OnePercentLow;
			PointOnePercentLow = i.PointOnePercentLow;
			FrameTime = i.FrameTimeMs;
			Cpu = i.CpuUsage;
			Gpu = i.GpuUsage;
			RamPercent = i.RamUsagePercent;
			VramPercent = i.VramUsagePercent;
			CpuTemp = i.CpuTemperature;
			GpuTemp = i.GpuTemperature;
			RamText = $"{i.RamUsedMb / 1024.0:0.0} / {i.RamTotalMb / 1024.0:0.0} GB";
			if (!string.IsNullOrWhiteSpace(i.GpuName))
			{
				GpuName = i.GpuName;
			}
			OnPropertyChanged("FpsDisplay");
			OnPropertyChanged("FrameTimeDisplay");
			FpsSeries = (from x in _host.Monitor.FpsHistory.Snapshot(TimeSpan.FromSeconds(60.0))
				select x.v).ToList();
			FrameTimeSeries = (from x in _host.Monitor.FrameTimeHistory.Snapshot(TimeSpan.FromSeconds(60.0))
				select x.v).ToList();
			CpuSeries = (from x in _host.Monitor.CpuHistory.Snapshot(TimeSpan.FromSeconds(60.0))
				select x.v).ToList();
			GpuSeries = (from x in _host.Monitor.GpuHistory.Snapshot(TimeSpan.FromSeconds(60.0))
				select x.v).ToList();
		});
	}

	private void OnVerdict(SystemVerdict v)
	{
		SystemVerdict v2 = v;
		Application current = Application.Current;
		if (current != null)
		{
			((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
			{
				StatusTitle = v2.Title;
				StatusDetail = v2.Detail;
				StatusRecommendation = v2.Recommendation;
				StatusHealthy = v2.IsHealthy;
			});
		}
	}

	private async Task RunBoostAsync()
	{
		IsBoosting = true;
		BoostSteps.Clear();
		BoostSummary = "Scanning system...";
		OptimizationMode mode = _host.AppSettings.DefaultBoostMode;
		List<OptimizationAction> plan = _host.Optimizer.BuildPlan(mode);
		BoostResult result = await Task.Run(() => _host.Optimizer.ApplyPlan(plan, mode, _host.AppSettings.CreateRestorePoints));
		Application current = Application.Current;
		if (current == null)
		{
			return;
		}
		((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
		{
			foreach (BoostStepResult step in result.Steps)
			{
				BoostSteps.Add(step);
			}
			BoostSummary = result.Summary;
			IsBoosting = false;
		});
	}

	public void Detach()
	{
		_host.Monitor.MetricsUpdated -= OnMetrics;
		_host.Monitor.VerdictUpdated -= OnVerdict;
	}
}
