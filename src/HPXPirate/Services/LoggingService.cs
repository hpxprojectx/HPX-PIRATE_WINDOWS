using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HPXPirate.Services;

public sealed class LoggingService : ILogger
{
	private readonly object _gate = new object();

	private readonly string _logFile;

	private readonly ConcurrentQueue<LogEntry> _recent = new ConcurrentQueue<LogEntry>();

	private const int MaxRecent = 500;

	private const long MaxBytes = 2097152L;

	public IReadOnlyList<LogEntry> Recent => _recent.ToArray();

	public string LogFilePath => _logFile;

	public event Action<LogEntry>? EntryWritten;

	public LoggingService(string logDirectory)
	{
		Directory.CreateDirectory(logDirectory);
		_logFile = Path.Combine(logDirectory, "hpx_pirate.log");
		TryRotate();
	}

	public void Debug(string message)
	{
		Write(LogLevel.Debug, message);
	}

	public void Info(string message)
	{
		Write(LogLevel.Info, message);
	}

	public void Warn(string message)
	{
		Write(LogLevel.Warn, message);
	}

	public void Error(string message, Exception? ex = null)
	{
		Write(LogLevel.Error, message, ex);
	}

	public void Critical(string message, Exception? ex = null)
	{
		Write(LogLevel.Critical, message, ex);
	}

	private void Write(LogLevel level, string message, Exception? ex = null)
	{
		LogEntry logEntry = new LogEntry(DateTime.Now, level, (ex == null) ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");
		_recent.Enqueue(logEntry);
		while (_recent.Count > 500)
		{
			_recent.TryDequeue(out LogEntry _);
		}
		try
		{
			string text = $"{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{level,-8}] {logEntry.Message}";
			lock (_gate)
			{
				File.AppendAllText(_logFile, text + Environment.NewLine, Encoding.UTF8);
			}
		}
		catch
		{
		}
		this.EntryWritten?.Invoke(logEntry);
	}

	private void TryRotate()
	{
		try
		{
			if (File.Exists(_logFile) && new FileInfo(_logFile).Length > 2097152)
			{
				string text = _logFile + ".1";
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Move(_logFile, text);
			}
		}
		catch
		{
		}
	}
}
