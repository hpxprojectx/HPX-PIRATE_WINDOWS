namespace HPXPirate.Models;

public sealed class SystemVerdict
{
	public SystemStatusKind Kind { get; set; }

	public string Title { get; set; } = "Performance Stable";


	public string Detail { get; set; } = "";


	public string Recommendation { get; set; } = "";


	public bool IsHealthy => Kind == SystemStatusKind.Stable;
}
