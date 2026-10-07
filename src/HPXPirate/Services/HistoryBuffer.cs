using System;
using System.Collections.Generic;
using System.Linq;

namespace HPXPirate.Services;

public sealed class HistoryBuffer
{
	private readonly Queue<(DateTime t, double v)> _q = new Queue<(DateTime, double)>();

	private readonly TimeSpan _maxAge;

	public double Last
	{
		get
		{
			if (_q.Count <= 0)
			{
				return 0.0;
			}
			return _q.Last().v;
		}
	}

	public HistoryBuffer(TimeSpan maxAge)
	{
		_maxAge = maxAge;
	}

	public void Add(double value)
	{
		Add(DateTime.Now, value);
	}

	public void Add(DateTime t, double value)
	{
		_q.Enqueue((t, value));
		while (_q.Count > 0 && t - _q.Peek().t > _maxAge)
		{
			_q.Dequeue();
		}
	}

	public IReadOnlyList<(DateTime t, double v)> Snapshot(TimeSpan window)
	{
		DateTime cutoff = DateTime.Now - window;
		return _q.Where(((DateTime t, double v) x) => x.t >= cutoff).ToList();
	}

	public void Clear()
	{
		_q.Clear();
	}
}
