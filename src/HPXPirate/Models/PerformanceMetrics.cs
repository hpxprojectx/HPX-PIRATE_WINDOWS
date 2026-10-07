using System;

namespace HPXPirate.Models;

public sealed class PerformanceMetrics
{
	public DateTime Timestamp { get; set; } = DateTime.Now;


	public double Fps { get; set; }

	public double AverageFps { get; set; }

	public double OnePercentLow { get; set; }

	public double PointOnePercentLow { get; set; }

	public double FrameTimeMs { get; set; }

	public double MinFps { get; set; }

	public double MaxFps { get; set; }

	public double CpuUsage { get; set; }

	public double CpuTemperature { get; set; }

	public int CpuCoreCount { get; set; }

	public double GpuUsage { get; set; }

	public double GpuTemperature { get; set; }

	public double GpuMemoryUsedMb { get; set; }

	public double GpuMemoryTotalMb { get; set; }

	public string GpuName { get; set; } = "";


	public double RamUsedMb { get; set; }

	public double RamTotalMb { get; set; }

	public double RamUsagePercent
	{
		get
		{
			if (!(RamTotalMb > 0.0))
			{
				return 0.0;
			}
			return RamUsedMb / RamTotalMb * 100.0;
		}
	}

	public double DiskUsagePercent { get; set; }

	public double NetworkKbps { get; set; }

	public double VramUsagePercent
	{
		get
		{
			if (!(GpuMemoryTotalMb > 0.0))
			{
				return 0.0;
			}
			return GpuMemoryUsedMb / GpuMemoryTotalMb * 100.0;
		}
	}
}
