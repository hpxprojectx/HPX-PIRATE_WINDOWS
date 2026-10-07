using System;

namespace HPXPirate.Services;

public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Message);
