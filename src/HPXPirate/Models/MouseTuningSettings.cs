namespace HPXPirate.Models;

/// <summary>
/// Persisted "gaming" mouse profile. The live values are always read back from
/// Windows before anything is written, so we never change a setting that is
/// already correct.
/// </summary>
public sealed class MouseTuningSettings
{
	public bool Enabled { get; set; }

	public bool DisableAcceleration { get; set; } = true;

	/// <summary>Pointer speed, 1 (slowest) to 20 (fastest). Default Windows value is 10.</summary>
	public int PointerSpeed { get; set; } = 10;

	/// <summary>Lines scrolled per wheel notch (1-100).</summary>
	public int ScrollLines { get; set; } = 3;

	/// <summary>Double-click time in milliseconds (200-900).</summary>
	public int DoubleClickSpeed { get; set; } = 500;

	public bool SwapButtons { get; set; }

	public bool SnapToDefault { get; set; }

	public bool ClickLock { get; set; }

	/// <summary>Pointer trails length (0 = off, 1-10 = trail length).</summary>
	public int PointerTrails { get; set; }
}
