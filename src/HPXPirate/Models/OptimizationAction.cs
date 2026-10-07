using System;

namespace HPXPirate.Models;

public sealed class OptimizationAction
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");

	public string Title { get; set; } = "";

	public string Description { get; set; } = "";

	public OptimizationCategory Category { get; set; }

	public ActionRisk Risk { get; set; }

	public OptimizationMode MinimumMode { get; set; }

	public bool Selected { get; set; } = true;

	public bool Applied { get; set; }

	public bool Succeeded { get; set; }

	/// <summary>True when the system was already in the desired state, so nothing was changed.</summary>
	public bool AlreadyOptimal { get; set; }

	/// <summary>Short human-readable status: "Applied", "Already optimal", "Skipped" or "Failed".</summary>
	public string Status { get; set; } = "";

	public string ResultMessage { get; set; } = "";

	public string? PreviousState { get; set; }

	public bool IsReversible { get; set; } = true;

}
