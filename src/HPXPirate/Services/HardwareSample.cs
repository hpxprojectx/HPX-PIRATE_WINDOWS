namespace HPXPirate.Services;

public readonly record struct HardwareSample(double CpuUsage, double CpuTemperature, int CpuCores, double GpuUsage, double GpuTemperature, double GpuMemUsedMb, double GpuMemTotalMb, string GpuName, double RamUsedMb, double RamTotalMb, double DiskUsagePercent, double NetworkKbps);
