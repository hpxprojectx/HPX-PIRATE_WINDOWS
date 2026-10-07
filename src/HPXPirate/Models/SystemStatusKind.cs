namespace HPXPirate.Models;

public enum SystemStatusKind
{
	Stable,
	CpuBottleneck,
	GpuBottleneck,
	MemoryPressure,
	BackgroundPressure,
	FrameTimeInstability
}
