using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class MonitorViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private TimeSpan _window = TimeSpan.FromSeconds(60.0);

	private string _windowLabel = "1 minute";

	private List<double> _fps = new List<double>();

	private List<double> _cpu = new List<double>();

	private List<double> _gpu = new List<double>();

	private List<double> _ram = new List<double>();

	private List<double> _frameTime = new List<double>();

	private string _cpuText = "--";

	private string _gpuText = "--";

	private string _ramText = "--";

	private string _fpsText = "--";

	private string _ftText = "--";

	private bool _benchmarkRunning;

	private BenchmarkResult? _before;

	private BenchmarkResult? _after;

	public string[] Windows { get; } = new string[4] { "10 seconds", "30 seconds", "1 minute", "5 minutes" };


	public string WindowLabel
	{
		get
		{
			return _windowLabel;
		}
		set
		{
			if (SetProperty(ref _windowLabel, value, "WindowLabel"))
			{
				_window = value switch
				{
					"10 seconds" => TimeSpan.FromSeconds(10.0), 
					"30 seconds" => TimeSpan.FromSeconds(30.0), 
					"5 minutes" => TimeSpan.FromMinutes(5.0), 
					_ => TimeSpan.FromMinutes(1.0), 
				};
			}
		}
	}

	public List<double> FpsSeries
	{
		get
		{
			return _fps;
		}
		private set
		{
			SetProperty(ref _fps, value, "FpsSeries");
		}
	}

	public List<double> CpuSeries
	{
		get
		{
			return _cpu;
		}
		private set
		{
			SetProperty(ref _cpu, value, "CpuSeries");
		}
	}

	public List<double> GpuSeries
	{
		get
		{
			return _gpu;
		}
		private set
		{
			SetProperty(ref _gpu, value, "GpuSeries");
		}
	}

	public List<double> RamSeries
	{
		get
		{
			return _ram;
		}
		private set
		{
			SetProperty(ref _ram, value, "RamSeries");
		}
	}

	public List<double> FrameTimeSeries
	{
		get
		{
			return _frameTime;
		}
		private set
		{
			SetProperty(ref _frameTime, value, "FrameTimeSeries");
		}
	}

	public string CpuText
	{
		get
		{
			return _cpuText;
		}
		private set
		{
			SetProperty(ref _cpuText, value, "CpuText");
		}
	}

	public string GpuText
	{
		get
		{
			return _gpuText;
		}
		private set
		{
			SetProperty(ref _gpuText, value, "GpuText");
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

	public string FpsText
	{
		get
		{
			return _fpsText;
		}
		private set
		{
			SetProperty(ref _fpsText, value, "FpsText");
		}
	}

	public string FrameTimeText
	{
		get
		{
			return _ftText;
		}
		private set
		{
			SetProperty(ref _ftText, value, "FrameTimeText");
		}
	}

	public ObservableCollection<ProcessInfoModel> Processes { get; } = new ObservableCollection<ProcessInfoModel>();


	public bool BenchmarkRunning
	{
		get
		{
			return _benchmarkRunning;
		}
		private set
		{
			SetProperty(ref _benchmarkRunning, value, "BenchmarkRunning");
			((RelayCommand)StartBenchmarkCommand).RaiseCanExecuteChanged();
			((RelayCommand)StopBenchmarkCommand).RaiseCanExecuteChanged();
		}
	}

	public BenchmarkResult? BeforeResult
	{
		get
		{
			return _before;
		}
		private set
		{
			SetProperty(ref _before, value, "BeforeResult");
		}
	}

	public BenchmarkResult? AfterResult
	{
		get
		{
			return _after;
		}
		private set
		{
			SetProperty(ref _after, value, "AfterResult");
		}
	}

	public ICommand RefreshProcessesCommand { get; }

	public ICommand StartBenchmarkCommand { get; }

	public ICommand StopBenchmarkCommand { get; }

	public MonitorViewModel(AppHost host)
	{
		_host = host;
		RefreshProcessesCommand = new RelayCommand(RefreshProcesses);
		StartBenchmarkCommand = new RelayCommand(StartBenchmark, () => !BenchmarkRunning);
		StopBenchmarkCommand = new RelayCommand(StopBenchmark, () => BenchmarkRunning);
		_host.Monitor.MetricsUpdated += OnMetrics;
		RefreshProcesses();
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
			CpuText = i.CpuUsage.ToString("0") + " %";
			GpuText = i.GpuUsage.ToString("0") + " %";
			RamText = $"{i.RamUsedMb / 1024.0:0.0} / {i.RamTotalMb / 1024.0:0.0} GB";
			FpsText = ((i.Fps > 0.0) ? i.Fps.ToString("0") : "--");
			FrameTimeText = ((i.FrameTimeMs > 0.0) ? (i.FrameTimeMs.ToString("0.0") + " ms") : "--");
			FpsSeries = (from x in _host.Monitor.FpsHistory.Snapshot(_window)
				select x.v).ToList();
			CpuSeries = (from x in _host.Monitor.CpuHistory.Snapshot(_window)
				select x.v).ToList();
			GpuSeries = (from x in _host.Monitor.GpuHistory.Snapshot(_window)
				select x.v).ToList();
			RamSeries = (from x in _host.Monitor.RamHistory.Snapshot(_window)
				select x.v).ToList();
			FrameTimeSeries = (from x in _host.Monitor.FrameTimeHistory.Snapshot(_window)
				select x.v).ToList();
		});
	}

	private void RefreshProcesses()
	{
		List<ProcessInfoModel> topProcesses = _host.Processes.GetTopProcesses();
		Processes.Clear();
		foreach (ProcessInfoModel item in topProcesses)
		{
			Processes.Add(item);
		}
	}

	private void StartBenchmark()
	{
		_host.Monitor.Benchmark.Start();
		BenchmarkRunning = true;
	}

	private void StopBenchmark()
	{
		BenchmarkResult benchmarkResult = _host.Monitor.Benchmark.Stop("Benchmark");
		if (BeforeResult == null)
		{
			BeforeResult = benchmarkResult;
		}
		else
		{
			AfterResult = benchmarkResult;
		}
		BenchmarkRunning = false;
	}
}
