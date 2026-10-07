using System;
using System.Collections.Generic;
using System.Linq;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class FpsAnalyzer
{
	private readonly List<double> _fps = new List<double>();

	private readonly List<double> _frameTimes = new List<double>();

	private DateTime _start;

	private bool _running;

	public bool IsRunning => _running;

	public TimeSpan Elapsed
	{
		get
		{
			if (!_running)
			{
				return TimeSpan.Zero;
			}
			return DateTime.Now - _start;
		}
	}

	public void Start()
	{
		_fps.Clear();
		_frameTimes.Clear();
		_start = DateTime.Now;
		_running = true;
	}

	public void Add(double fps, double frameTimeMs)
	{
		if (_running)
		{
			if (fps > 0.0)
			{
				_fps.Add(fps);
			}
			if (frameTimeMs > 0.0)
			{
				_frameTimes.Add(frameTimeMs);
			}
		}
	}

	public BenchmarkResult Stop(string label)
	{
		_running = false;
		return Build(label);
	}

	private BenchmarkResult Build(string label)
	{
		BenchmarkResult result = new BenchmarkResult
		{
			Label = label,
			DurationSeconds = (DateTime.Now - _start).TotalSeconds,
			SampleCount = _fps.Count
		};
		if (_fps.Count == 0)
		{
			return result;
		}
		result.AverageFps = _fps.Average();
		result.MinFps = _fps.Min();
		result.MaxFps = _fps.Max();
		result.AverageFrameTimeMs = ((_frameTimes.Count > 0) ? _frameTimes.Average() : 0.0);
		double[] slowestFirst = _frameTimes.OrderByDescending((double x) => x).ToArray();
		result.OnePercentLow = AvgFpsOfSlowest(slowestFirst, 0.01);
		result.PointOnePercentLow = AvgFpsOfSlowest(slowestFirst, 0.001);
		if (result.AverageFrameTimeMs > 0.0)
		{
			result.FrameTimeSpikes = _frameTimes.Count((double ft) => ft > result.AverageFrameTimeMs * 1.5);
		}
		return result;
	}

	private static double AvgFpsOfSlowest(double[] slowestFirst, double fraction)
	{
		if (slowestFirst.Length == 0)
		{
			return 0.0;
		}
		int count = Math.Max(1, (int)Math.Ceiling((double)slowestFirst.Length * fraction));
		double num = slowestFirst.Take(count).Average();
		if (!(num > 0.0))
		{
			return 0.0;
		}
		return 1000.0 / num;
	}
}
