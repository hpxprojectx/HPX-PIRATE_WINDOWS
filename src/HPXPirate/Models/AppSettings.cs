namespace HPXPirate.Models;

public sealed class AppSettings
{
	public string Language { get; set; } = "en";


	public string Theme { get; set; } = "Dark";


	public bool StartWithWindows { get; set; }

	public bool MinimizeToTray { get; set; } = true;


	public bool AutoLoadProfiles { get; set; } = true;


	public bool AutoOptimize { get; set; }

	public OptimizationMode DefaultBoostMode { get; set; }

	public int MonitoringIntervalMs { get; set; } = 1000;


	public int IdleMonitoringIntervalMs { get; set; } = 3000;


	public bool ReduceMonitoringWhenIdle { get; set; } = true;


	public bool CreateRestorePoints { get; set; } = true;


	public bool BackupBeforeChanges { get; set; } = true;


	public bool DiagnosticMode { get; set; }

	public bool SafeMode { get; set; }

	public MouseSettings Mouse { get; set; } = new MouseSettings();


	public DragAssistSettings DragAssist { get; set; } = new DragAssistSettings();


	public EasyDragSettings EasyDrag { get; set; } = new EasyDragSettings();


	public CrosshairSettings Crosshair { get; set; } = new CrosshairSettings();


	public OverlaySettings Overlay { get; set; } = new OverlaySettings();


	public MouseTuningSettings MouseTuning { get; set; } = new MouseTuningSettings();


	public KeyboardTuningSettings KeyboardTuning { get; set; } = new KeyboardTuningSettings();


	/// <summary>Target FPS used by the Real-Time FPS Boost view (e.g. 300).</summary>
	public int TargetFps { get; set; } = 300;

}
