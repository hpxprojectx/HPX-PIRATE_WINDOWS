using System;
using System.Timers;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class MonitorService : IDisposable
{
	private readonly ILogger _log;

	private readonly HardwareMonitorService _hardware;

	private readonly FpsMonitorService _fps;

	private readonly BottleneckAnalyzer _analyzer;

	private readonly ProcessManagerService _processes;

	private readonly SettingsService _settings;

	private readonly FpsAnalyzer _benchmark = new FpsAnalyzer();

	private System.Timers.Timer? _timer;

	private readonly object _gate = new object();

	private DateTime _lastTick = DateTime.Now;

	private bool _gameRunning;

	public PerformanceMetrics Current { get; private set; } = new PerformanceMetrics();


	public SystemVerdict Verdict { get; private set; } = new SystemVerdict();


	public FpsAnalyzer Benchmark => _benchmark;

	public HistoryBuffer FpsHistory { get; } = new HistoryBuffer(TimeSpan.FromMinutes(5.0));


	public HistoryBuffer FrameTimeHistory { get; } = new HistoryBuffer(TimeSpan.FromMinutes(5.0));


	public HistoryBuffer CpuHistory { get; } = new HistoryBuffer(TimeSpan.FromMinutes(5.0));


	public HistoryBuffer GpuHistory { get; } = new HistoryBuffer(TimeSpan.FromMinutes(5.0));


	public HistoryBuffer RamHistory { get; } = new HistoryBuffer(TimeSpan.FromMinutes(5.0));


	public bool IsRunning => _timer?.Enabled ?? false;

	public bool GameRunning => _gameRunning;

	public event Action<PerformanceMetrics>? MetricsUpdated;

	public event Action<SystemVerdict>? VerdictUpdated;

	public MonitorService(ILogger log, HardwareMonitorService hardware, FpsMonitorService fps, BottleneckAnalyzer analyzer, ProcessManagerService processes, SettingsService settings)
	{
		_log = log;
		_hardware = hardware;
		_fps = fps;
		_analyzer = analyzer;
		_processes = processes;
		_settings = settings;
	}

	public void Start()
	{
		if (_timer != null)
		{
			_timer.Enabled = true;
			return;
		}
		_timer = new System.Timers.Timer(_settings.Current.MonitoringIntervalMs)
		{
			AutoReset = true
		};
		_timer.Elapsed += OnTick;
		_timer.Enabled = true;
		_fps.Start();
		_log.Info("Monitor started.");
	}

	public void Stop()
	{
		if (_timer != null)
		{
			_timer.Enabled = false;
		}
		_log.Info("Monitor stopped.");
	}

	private void OnTick(object? sender, ElapsedEventArgs e)
	{
		lock (_gate)
		{
			try
			{
				DateTime now = DateTime.Now;
				double num = (now - _lastTick).TotalSeconds;
				if (num <= 0.0)
				{
					num = 1.0;
				}
				_lastTick = now;
				_fps.Tick(num);
				HardwareSample hardwareSample = _hardware.Sample();
				ProcessInfoModel processInfoModel = _processes.DetectRunningGame();
				_gameRunning = processInfoModel != null;
				PerformanceMetrics performanceMetrics2 = (Current = new PerformanceMetrics
				{
					Timestamp = now,
					Fps = _fps.CurrentFps,
					FrameTimeMs = _fps.CurrentFrameTimeMs,
					AverageFps = _fps.AverageFps,
					OnePercentLow = _fps.OnePercentLow,
					PointOnePercentLow = _fps.PointOnePercentLow,
					MinFps = _fps.MinFps,
					MaxFps = _fps.MaxFps,
					CpuUsage = hardwareSample.CpuUsage,
					CpuTemperature = hardwareSample.CpuTemperature,
					CpuCoreCount = hardwareSample.CpuCores,
					GpuUsage = hardwareSample.GpuUsage,
					GpuTemperature = hardwareSample.GpuTemperature,
					GpuMemoryUsedMb = hardwareSample.GpuMemUsedMb,
					GpuMemoryTotalMb = hardwareSample.GpuMemTotalMb,
					GpuName = hardwareSample.GpuName,
					RamUsedMb = hardwareSample.RamUsedMb,
					RamTotalMb = hardwareSample.RamTotalMb,
					DiskUsagePercent = hardwareSample.DiskUsagePercent,
					NetworkKbps = hardwareSample.NetworkKbps
				});
				FpsHistory.Add(now, performanceMetrics2.Fps);
				FrameTimeHistory.Add(now, performanceMetrics2.FrameTimeMs);
				CpuHistory.Add(now, performanceMetrics2.CpuUsage);
				GpuHistory.Add(now, performanceMetrics2.GpuUsage);
				RamHistory.Add(now, performanceMetrics2.RamUsagePercent);
				if (performanceMetrics2.FrameTimeMs > 0.0)
				{
					_analyzer.Feed(performanceMetrics2.FrameTimeMs);
				}
				if (_benchmark.IsRunning)
				{
					_benchmark.Add(performanceMetrics2.Fps, performanceMetrics2.FrameTimeMs);
				}
				SystemVerdict obj = (Verdict = _analyzer.Analyze(performanceMetrics2));
				this.MetricsUpdated?.Invoke(performanceMetrics2);
				this.VerdictUpdated?.Invoke(obj);
				AdaptInterval();
			}
			catch (Exception ex)
			{
				_log.Error("Monitor tick failed.", ex);
			}
		}
	}

	private void AdaptInterval()
	{
		if (_timer != null)
		{
			int num = ((_gameRunning || !_settings.Current.ReduceMonitoringWhenIdle) ? _settings.Current.MonitoringIntervalMs : _settings.Current.IdleMonitoringIntervalMs);
			if ((int)_timer.Interval != num)
			{
				_timer.Interval = num;
			}
		}
	}

	public void ApplyIntervalSettings()
	{
		if (_timer != null)
		{
			_timer.Interval = _settings.Current.MonitoringIntervalMs;
		}
	}

	public void Dispose()
	{
		try
		{
			_timer?.Dispose();
		}
		catch
		{
		}
		_fps.Dispose();
		_hardware.Dispose();
	}
}
