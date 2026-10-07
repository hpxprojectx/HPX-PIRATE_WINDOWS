namespace HPXPirate.Models;

public sealed class BenchmarkResult
{
	public string Label { get; set; } = "";


	public double AverageFps { get; set; }

	public double MinFps { get; set; }

	public double MaxFps { get; set; }

	public double OnePercentLow { get; set; }

	public double PointOnePercentLow { get; set; }

	public double AverageFrameTimeMs { get; set; }

	public int FrameTimeSpikes { get; set; }

	public double DurationSeconds { get; set; }

	public int SampleCount { get; set; }
}
