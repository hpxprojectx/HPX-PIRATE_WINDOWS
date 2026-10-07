namespace HPXPirate.Models;

public sealed class OverlaySettings
{
	public bool FpsCounter { get; set; } = true;


	public bool FrameTimeGraph { get; set; } = true;


	public bool PerformanceOverlay { get; set; }

	public bool ScreenRegionMarker { get; set; }

	public bool ColorAccessibilityFilter { get; set; }

	public string AccessibilityMode { get; set; } = "None";

}
