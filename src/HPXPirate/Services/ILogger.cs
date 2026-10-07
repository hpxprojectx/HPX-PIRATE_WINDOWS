using System;
using System.Collections.Generic;

namespace HPXPirate.Services;

public interface ILogger
{
	IReadOnlyList<LogEntry> Recent { get; }

	event Action<LogEntry>? EntryWritten;

	void Debug(string message);

	void Info(string message);

	void Warn(string message);

	void Error(string message, Exception? ex = null);

	void Critical(string message, Exception? ex = null);
}
