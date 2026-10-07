using System;

namespace HPXPirate.Models;

public sealed class ChangeRecord
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");


	public DateTime Timestamp { get; set; } = DateTime.Now;


	public string Category { get; set; } = "";


	public string Description { get; set; } = "";


	public string? PreviousState { get; set; }

	public bool Reverted { get; set; }
}
