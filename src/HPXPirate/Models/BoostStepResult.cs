namespace HPXPirate.Models;

public sealed class BoostStepResult
{
	public string Label { get; set; } = "";

	public bool Success { get; set; }

	/// <summary>True when the step required no change because the system was already optimal.</summary>
	public bool AlreadyOptimal { get; set; }

	/// <summary>Short status label: "Applied", "Already optimal", "Skipped" or "Failed".</summary>
	public string Status { get; set; } = "";

	public string Detail { get; set; } = "";

}
