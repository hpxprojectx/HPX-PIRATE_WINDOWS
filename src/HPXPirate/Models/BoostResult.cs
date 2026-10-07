using System;
using System.Collections.Generic;

namespace HPXPirate.Models;

public sealed class BoostResult
{
	public DateTime Timestamp { get; set; } = DateTime.Now;

	public OptimizationMode Mode { get; set; }

	public bool Success { get; set; }

	public List<BoostStepResult> Steps { get; set; } = new List<BoostStepResult>();

	public string Summary { get; set; } = "";

	public string? RestorePointId { get; set; }

	/// <summary>Number of actions that actually changed system state.</summary>
	public int AppliedCount { get; set; }

	/// <summary>Number of actions that were already in the desired state (nothing changed).</summary>
	public int AlreadyOptimalCount { get; set; }

	/// <summary>Number of actions that were skipped because they were not applicable.</summary>
	public int SkippedCount { get; set; }

	/// <summary>Number of actions that failed.</summary>
	public int FailedCount { get; set; }

}
