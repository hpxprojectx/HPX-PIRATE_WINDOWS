using System;
using System.Collections.Generic;
using System.Linq;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class BottleneckAnalyzer
{
	private readonly Queue<double> _frameTimes = new Queue<double>();

	private const int FrameCap = 120;

	public void Feed(double frameTimeMs)
	{
		if (!(frameTimeMs <= 0.0))
		{
			_frameTimes.Enqueue(frameTimeMs);
			while (_frameTimes.Count > 120)
			{
				_frameTimes.Dequeue();
			}
		}
	}

	public SystemVerdict Analyze(PerformanceMetrics m)
	{
		double num = 0.0;
		if (_frameTimes.Count > 20)
		{
			double[] source = _frameTimes.ToArray();
			double mean = source.Average();
			if (mean > 0.0)
			{
				num = Math.Sqrt(source.Select((double x) => (x - mean) * (x - mean)).Average()) / mean;
			}
		}
		if (m.RamUsagePercent >= 90.0)
		{
			return new SystemVerdict
			{
				Kind = SystemStatusKind.MemoryPressure,
				Title = "MEMORY PRESSURE DETECTED",
				Detail = $"RAM is {m.RamUsagePercent:0}% full ({m.RamUsedMb / 1024.0:0.0} / {m.RamTotalMb / 1024.0:0.0} GB).",
				Recommendation = "Close unused background apps or browser tabs. HPX PIRATE can free safe background load via BOOST NOW."
			};
		}
		if (m.CpuUsage >= 90.0 && m.GpuUsage < 75.0 && m.GpuUsage > 0.0)
		{
			return new SystemVerdict
			{
				Kind = SystemStatusKind.CpuBottleneck,
				Title = "CPU BOTTLENECK DETECTED",
				Detail = $"CPU {m.CpuUsage:0}% vs GPU {m.GpuUsage:0}%.",
				Recommendation = "Reduce background CPU load, raise the game process priority, and close CPU-heavy apps."
			};
		}
		if (m.GpuUsage >= 95.0)
		{
			return new SystemVerdict
			{
				Kind = SystemStatusKind.GpuBottleneck,
				Title = "GPU BOTTLENECK DETECTED",
				Detail = $"GPU is at {m.GpuUsage:0}% — the GPU is the limiting factor.",
				Recommendation = "Lower in-game graphics settings, resolution or render scale. This is expected at high settings."
			};
		}
		if (num > 0.35 && m.Fps > 0.0)
		{
			return new SystemVerdict
			{
				Kind = SystemStatusKind.FrameTimeInstability,
				Title = "FRAME-TIME INSTABILITY DETECTED",
				Detail = $"Frame-time variation is {num * 100.0:0}% (jittery).",
				Recommendation = "Enable a performance mode, reduce background load, and check for thermal throttling."
			};
		}
		if (m.CpuUsage > 60.0 && m.GpuUsage > 0.0 && m.GpuUsage < 60.0)
		{
			return new SystemVerdict
			{
				Kind = SystemStatusKind.BackgroundPressure,
				Title = "BACKGROUND PROCESS PRESSURE",
				Detail = $"CPU {m.CpuUsage:0}% with GPU headroom suggests background activity.",
				Recommendation = "Use BOOST NOW to trim safe background processes."
			};
		}
		return new SystemVerdict
		{
			Kind = SystemStatusKind.Stable,
			Title = "Performance Stable",
			Detail = ((m.Fps > 0.0) ? $"FPS {m.Fps:0} · Frame time {m.FrameTimeMs:0.0} ms · CPU {m.CpuUsage:0}% · GPU {m.GpuUsage:0}%" : "No active game detected. Monitoring system only."),
			Recommendation = "No action needed."
		};
	}

	public void Reset()
	{
		_frameTimes.Clear();
	}
}
