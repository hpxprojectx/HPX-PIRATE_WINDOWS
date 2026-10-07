using System;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class AppHost : IDisposable
{
	private bool _initialized;

	public LoggingService Logger { get; private set; }

	public SettingsService Settings { get; private set; }

	public ProfileManager Profiles { get; private set; }

	public HardwareMonitorService Hardware { get; private set; }

	public FpsMonitorService Fps { get; private set; }

	public BottleneckAnalyzer Analyzer { get; private set; }

	public ProcessManagerService Processes { get; private set; }

	public RestorePointService Restore { get; private set; }

	public SystemOptimizerService Optimizer { get; private set; }

	public StartupManagerService Startup { get; private set; }

	public InputMapperService Input { get; private set; }

	public CrosshairOverlayService Crosshair { get; private set; }

	public EasyDragEngine EasyDrag { get; private set; }

	public DragAssistEngine DragAssist { get; private set; }

	public MonitorService Monitor { get; private set; }

	public MouseTuningService MouseTuning { get; private set; }

	public KeyboardTuningService KeyboardTuning { get; private set; }

	public FpsBoostService FpsBoost { get; private set; }

	public AppSettings AppSettings => Settings.Current;

	public void Initialize()
	{
		if (!_initialized)
		{
			AppPaths.EnsureCreated();
			Logger = new LoggingService(AppPaths.Logs);
			Settings = new SettingsService(Logger);
			Profiles = new ProfileManager(Logger);
			Profiles.LoadAll();
			Hardware = new HardwareMonitorService(Logger);
			Hardware.Initialize();
			Fps = new FpsMonitorService(Logger);
			Analyzer = new BottleneckAnalyzer();
			Processes = new ProcessManagerService(Logger);
			Restore = new RestorePointService(Logger);
			Optimizer = new SystemOptimizerService(Logger, Restore, Processes);
			Optimizer.LoadChanges();
			Startup = new StartupManagerService(Logger);
			Input = new InputMapperService(Logger);
			Crosshair = new CrosshairOverlayService(Logger);
			EasyDrag = new EasyDragEngine();
			DragAssist = new DragAssistEngine();
			Monitor = new MonitorService(Logger, Hardware, Fps, Analyzer, Processes, Settings);
			MouseTuning = new MouseTuningService(Logger);
			KeyboardTuning = new KeyboardTuningService(Logger);
			FpsBoost = new FpsBoostService(Logger, Fps, Processes, Restore);
			FpsBoost.LoadChanges();
			_initialized = true;
			Logger.Info("AppHost initialised.");
		}
	}

	public void Shutdown()
	{
		Logger?.Info("HPX PIRATE shutting down.");
		try
		{
			Monitor?.Dispose();
		}
		catch
		{
		}
		try
		{
			Crosshair?.Dispose();
		}
		catch
		{
		}
		try
		{
			Input?.Dispose();
		}
		catch
		{
		}
		Settings?.Save();
	}

	public void Dispose()
	{
		Shutdown();
	}
}
