namespace HPXPirate.Models;

/// <summary>
/// One mouse / keyboard tuning step. Every step is state-detected first, so it is
/// only reported as "Applied" when a value actually changed. If the system was
/// already in the desired state the step is reported as "Already optimal" and
/// nothing is written — this is what prevents the app from "fixing" healthy settings.
/// </summary>
public sealed class TuningStep
{
	public string Label { get; set; } = "";

	public bool Success { get; set; }

	/// <summary>True when the value was already correct, so nothing was written.</summary>
	public bool AlreadyOptimal { get; set; }

	/// <summary>True when the value was actually changed.</summary>
	public bool Changed { get; set; }

	/// <summary>"Applied", "Already optimal", "Skipped" or "Failed".</summary>
	public string Status { get; set; } = "";

	public string Detail { get; set; } = "";

	public string? PreviousState { get; set; }

	public string? NewState { get; set; }
}
