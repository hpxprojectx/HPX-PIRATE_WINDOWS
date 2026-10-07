namespace HPXPirate.Models;

/// <summary>
/// Snapshot of the live Windows keyboard settings, read straight from the OS.
/// </summary>
public sealed class KeyboardLiveState
{
	/// <summary>Repeat delay 0 (shortest) to 3 (longest). Windows default is 1.</summary>
	public int RepeatDelay { get; set; } = 1;

	/// <summary>Repeat rate 0 (slowest) to 31 (fastest). Windows default is 31.</summary>
	public int RepeatRate { get; set; } = 31;

	public bool StickyKeysOn { get; set; }

	public bool FilterKeysOn { get; set; }

	public bool ToggleKeysOn { get; set; }

	public string RepeatDelayText => $"{RepeatDelay} / 3";

	public string RepeatRateText => $"{RepeatRate} / 31";

	public string StickyKeysText => StickyKeysOn ? "On" : "Off";

	public string FilterKeysText => FilterKeysOn ? "On" : "Off";

	public string ToggleKeysText => ToggleKeysOn ? "On" : "Off";
}
