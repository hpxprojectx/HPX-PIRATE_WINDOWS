using System;
using System.Collections.Generic;

namespace HPXPirate.Models;

/// <summary>
/// One real, reversible FPS-boost step. Every step is state-detected so it is
/// only reported as "Applied" when something actually changed.
/// </summary>
public sealed class FpsBoostStep
{
	public string Label { get; set; } = "";

	public bool Success { get; set; }

	public bool AlreadyOptimal { get; set; }

	public bool Reversible { get; set; } = true;

	/// <summary>"Applied", "Already optimal", "Skipped" or "Failed".</summary>
	public string Status { get; set; } = "";

	public string Detail { get; set; } = "";

	public string? PreviousState { get; set; }
}

/// <summary>
/// Result of a real-time FPS boost run. The before/after FPS values come from the
/// ETW (DxgKrnl Present) monitor, i.e. the same numbers the game actually renders.
/// </summary>
public sealed class FpsBoostResult
{
	public DateTime Timestamp { get; set; } = DateTime.Now;

	public bool Success { get; set; }

	public List<FpsBoostStep> Steps { get; set; } = new List<FpsBoostStep>();

	public string Summary { get; set; } = "";

	public int AppliedCount { get; set; }

	public int AlreadyOptimalCount { get; set; }

	public int SkippedCount { get; set; }

	public int FailedCount { get; set; }

	public string GameName { get; set; } = "";

	public int TargetFps { get; set; } = 300;

	public double FpsBefore { get; set; }

	public double FpsAfter { get; set; }

	public string Note { get; set; } = "";
}
