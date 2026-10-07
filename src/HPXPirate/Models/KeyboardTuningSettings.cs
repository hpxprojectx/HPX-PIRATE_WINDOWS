namespace HPXPirate.Models;

/// <summary>
/// Persisted "gaming" keyboard profile. Live values are read back from Windows
/// before writing so already-correct settings are left untouched.
/// </summary>
public sealed class KeyboardTuningSettings
{
	public bool Enabled { get; set; }

	/// <summary>Key repeat delay, 0 (shortest) to 3 (longest). Default Windows value is 1.</summary>
	public int RepeatDelay { get; set; }

	/// <summary>Key repeat rate, 0 (slowest) to 31 (fastest). Default Windows value is 31.</summary>
	public int RepeatRate { get; set; } = 31;

	public bool DisableStickyKeys { get; set; } = true;

	public bool DisableFilterKeys { get; set; } = true;

	public bool DisableToggleKeys { get; set; } = true;
}
