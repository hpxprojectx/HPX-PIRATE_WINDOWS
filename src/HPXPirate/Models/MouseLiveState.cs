namespace HPXPirate.Models;

/// <summary>
/// Snapshot of the live Windows mouse settings, read straight from the OS.
/// Used purely for display and for detecting whether a change is actually needed.
/// </summary>
public sealed class MouseLiveState
{
	/// <summary>True when "Enhance pointer precision" (mouse acceleration) is on.</summary>
	public bool AccelerationEnabled { get; set; }

	public int PointerSpeed { get; set; } = 10;

	public int ScrollLines { get; set; } = 3;

	public int DoubleClickMs { get; set; } = 500;

	public bool ButtonsSwapped { get; set; }

	public bool SnapToDefault { get; set; }

	public bool ClickLock { get; set; }

	public int PointerTrails { get; set; }

	public string AccelerationText => AccelerationEnabled ? "On (not ideal)" : "Off (good)";

	public string PointerSpeedText => $"{PointerSpeed} / 20";

	public string ScrollLinesText => ScrollLines <= 0 ? "One page" : $"{ScrollLines} line(s)";

	public string DoubleClickText => $"{DoubleClickMs} ms";

	public string ButtonsSwappedText => ButtonsSwapped ? "Swapped" : "Right-handed";

	public string SnapText => SnapToDefault ? "On" : "Off";

	public string ClickLockText => ClickLock ? "On" : "Off";

	public string TrailsText => PointerTrails <= 0 ? "Off" : $"{PointerTrails}";
}
