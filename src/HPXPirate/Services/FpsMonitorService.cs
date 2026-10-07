using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace HPXPirate.Services;

public sealed class FpsMonitorService : IDisposable
{
	private static readonly Guid DxgKrnlProvider = new Guid("802ec45a-1e99-4b83-9920-87c98277ba9d");

	private readonly ILogger _log;

	private TraceEventSession? _session;

	private Thread? _thread;

	private readonly ConcurrentDictionary<int, long> _counts = new ConcurrentDictionary<int, long>();

	private readonly Queue<double> _frameTimes = new Queue<double>();

	private const int HistoryCap = 1800;

	private double _lastFps;

	private double _lastFrameTimeMs;

	private int _lastPid = -1;

	public bool IsAvailable { get; private set; }

	public int TargetPid { get; private set; } = -1;


	public string SourceName { get; private set; } = "Unavailable";


	public double CurrentFps => _lastFps;

	public double CurrentFrameTimeMs => _lastFrameTimeMs;

	public double AverageFps
	{
		get
		{
			if (_frameTimes.Count == 0)
			{
				return 0.0;
			}
			double num = _frameTimes.Average();
			if (!(num > 0.0))
			{
				return 0.0;
			}
			return 1000.0 / num;
		}
	}

	public double MinFps
	{
		get
		{
			if (_frameTimes.Count == 0)
			{
				return 0.0;
			}
			double num = _frameTimes.Max();
			if (!(num > 0.0))
			{
				return 0.0;
			}
			return 1000.0 / num;
		}
	}

	public double MaxFps
	{
		get
		{
			if (_frameTimes.Count == 0)
			{
				return 0.0;
			}
			double num = _frameTimes.Min();
			if (!(num > 0.0))
			{
				return 0.0;
			}
			return 1000.0 / num;
		}
	}

	public double OnePercentLow => PercentileLowFps(0.01);

	public double PointOnePercentLow => PercentileLowFps(0.001);

	public FpsMonitorService(ILogger log)
	{
		_log = log;
	}

	public bool Start()
	{
		if (IsAvailable)
		{
			return true;
		}
		try
		{
			_session = new TraceEventSession($"HPXPirate-FPS-{Environment.ProcessId}")
			{
				StopOnDispose = true
			};
			_session.EnableProvider(DxgKrnlProvider, TraceEventLevel.Informational, ulong.MaxValue);
			_session.Source.AllEvents += OnEvent;
			_thread = new Thread((ThreadStart)delegate
			{
				try
				{
					_session.Source.Process();
				}
				catch (Exception ex2)
				{
					_log.Warn("ETW loop ended: " + ex2.Message);
				}
			})
			{
				IsBackground = true,
				Name = "HPX-ETW",
				Priority = ThreadPriority.BelowNormal
			};
			_thread.Start();
			IsAvailable = true;
			SourceName = "ETW (DxgKrnl Present)";
			_log.Info("ETW FPS capture started.");
			return true;
		}
		catch (Exception ex)
		{
			IsAvailable = false;
			SourceName = "Unavailable";
			_log.Warn("ETW FPS capture unavailable (run as administrator for game FPS): " + ex.Message);
			return false;
		}
	}

	private void OnEvent(TraceEvent data)
	{
		try
		{
			string eventName = data.EventName;
			if (eventName != null && eventName.Contains("Present", StringComparison.OrdinalIgnoreCase))
			{
				_counts.AddOrUpdate(data.ProcessID, 1L, (int _, long v) => v + 1);
			}
		}
		catch
		{
		}
	}

	public void Tick(double intervalSeconds)
	{
		int num = -1;
		long num2 = 0L;
		foreach (KeyValuePair<int, long> count in _counts)
		{
			if (count.Value > num2)
			{
				num2 = count.Value;
				num = count.Key;
			}
		}
		_counts.Clear();
		if (num > 0 && num2 > 0)
		{
			_lastPid = num;
			TargetPid = num;
			_lastFps = ((intervalSeconds > 0.0) ? ((double)num2 / intervalSeconds) : ((double)num2));
			_lastFrameTimeMs = ((_lastFps > 0.0) ? (1000.0 / _lastFps) : 0.0);
			PushFrameTime(_lastFrameTimeMs);
		}
		else
		{
			_lastFps = 0.0;
			_lastFrameTimeMs = 0.0;
		}
	}

	private void PushFrameTime(double ms)
	{
		if (!(ms <= 0.0))
		{
			_frameTimes.Enqueue(ms);
			while (_frameTimes.Count > 1800)
			{
				_frameTimes.Dequeue();
			}
		}
	}

	private double PercentileLowFps(double fraction)
	{
		if (_frameTimes.Count < 10)
		{
			return 0.0;
		}
		double[] array = _frameTimes.OrderByDescending((double x) => x).ToArray();
		int count = Math.Max(1, (int)Math.Ceiling((double)array.Length * fraction));
		double num = array.Take(count).Average();
		if (!(num > 0.0))
		{
			return 0.0;
		}
		return 1000.0 / num;
	}

	public void ResetHistory()
	{
		_frameTimes.Clear();
		_lastFps = 0.0;
		_lastFrameTimeMs = 0.0;
	}

	public void Dispose()
	{
		try
		{
			_session?.Dispose();
		}
		catch
		{
		}
		try
		{
			_thread?.Join(500);
		}
		catch
		{
		}
		_session = null;
		IsAvailable = false;
	}
}
