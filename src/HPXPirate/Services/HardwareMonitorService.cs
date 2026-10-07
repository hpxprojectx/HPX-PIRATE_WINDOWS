using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using HPXPirate.Interop;
using LibreHardwareMonitor.Hardware;

namespace HPXPirate.Services;

public sealed class HardwareMonitorService : IDisposable
{
	private readonly ILogger _log;

	private PerformanceCounter? _cpuCounter;

	private PerformanceCounter? _diskCounter;

	private PerformanceCounter? _netCounter;

	private Computer? _computer;

	private bool _lhmReady;

	private string _gpuName = "";

	private double _gpuMemTotalMb;

	private int _cpuCores = Environment.ProcessorCount;

	public HardwareMonitorService(ILogger log)
	{
		_log = log;
	}

	public void Initialize()
	{
		_cpuCounter = TryCreateCounter("Processor Information", "% Processor Time", "_Total") ?? TryCreateCounter("Processor", "% Processor Time", "_Total");
		_diskCounter = TryCreateCounter("PhysicalDisk", "% Disk Time", "_Total");
		_netCounter = TryCreateCounter("Network Interface", "Bytes Total/sec", null);
		TryReadGpuInfo();
		try
		{
			_computer = new Computer
			{
				IsCpuEnabled = true,
				IsGpuEnabled = true,
				IsMemoryEnabled = true,
				IsMotherboardEnabled = false,
				IsControllerEnabled = false,
				IsNetworkEnabled = false,
				IsStorageEnabled = false
			};
			_computer.Open();
			_lhmReady = true;
			_log.Info("LibreHardwareMonitor initialised.");
		}
		catch (Exception ex)
		{
			_lhmReady = false;
			_log.Warn("LibreHardwareMonitor unavailable (temps/GPU load limited): " + ex.Message);
		}
	}

	private PerformanceCounter? TryCreateCounter(string category, string counter, string? instance)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		try
		{
			PerformanceCounter val = ((instance == null) ? new PerformanceCounter(category, counter, true) : new PerformanceCounter(category, counter, instance, true));
			val.NextValue();
			return val;
		}
		catch (Exception ex)
		{
			_log.Debug($"Counter '{category}\\{counter}' unavailable: {ex.Message}");
			return null;
		}
	}

	private void TryReadGpuInfo()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				string text = item["Name"]?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(text))
				{
					_gpuName = text;
					object obj = item["AdapterRAM"];
					if (obj != null)
					{
						try
						{
							_gpuMemTotalMb = Convert.ToDouble(obj) / 1048576.0;
							break;
						}
						catch
						{
							break;
						}
					}
					break;
				}
			}
		}
		catch (Exception ex)
		{
			_log.Debug("WMI GPU query failed: " + ex.Message);
		}
	}

	public HardwareSample Sample()
	{
		double num = SafeNext(_cpuCounter);
		if (num < 0.0)
		{
			num = 0.0;
		}
		double ramTotalMb = 0.0;
		double ramUsedMb = 0.0;
		try
		{
			NativeMethods.MEMORYSTATUSEX mEMORYSTATUSEX = default(NativeMethods.MEMORYSTATUSEX);
			mEMORYSTATUSEX.dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>();
			NativeMethods.MEMORYSTATUSEX lpBuffer = mEMORYSTATUSEX;
			if (NativeMethods.GlobalMemoryStatusEx(ref lpBuffer))
			{
				ramTotalMb = (double)lpBuffer.ullTotalPhys / 1048576.0;
				ramUsedMb = (double)(lpBuffer.ullTotalPhys - lpBuffer.ullAvailPhys) / 1048576.0;
			}
		}
		catch
		{
		}
		double num2 = 0.0;
		double num3 = 0.0;
		double gpuMemUsedMb = 0.0;
		double num4 = _gpuMemTotalMb;
		double num5 = 0.0;
		if (_lhmReady && _computer != null)
		{
			try
			{
				foreach (IHardware item in _computer.Hardware)
				{
					item.Update();
					IHardware[] subHardware = item.SubHardware;
					for (int i = 0; i < subHardware.Length; i++)
					{
						subHardware[i].Update();
					}
					string text = item.HardwareType.ToString();
					ISensor[] sensors = item.Sensors;
					foreach (ISensor sensor in sensors)
					{
						if (!sensor.Value.HasValue)
						{
							continue;
						}
						float value = sensor.Value.Value;
						string text2 = sensor.SensorType.ToString();
						string name = sensor.Name;
						if (text.Contains("Cpu"))
						{
							if (text2 == "Temperature" && (name.Contains("Package") || name.Contains("Total") || name.Contains("Core")))
							{
								num5 = Math.Max(num5, value);
							}
							if (text2 == "Load" && name.Contains("Total"))
							{
								num = value;
							}
						}
						else if (text.Contains("Gpu"))
						{
							if (text2 == "Load" && (name.Contains("Core") || name.Contains("Total") || name.Contains("D3D")))
							{
								num2 = Math.Max(num2, value);
							}
							if (text2 == "Temperature" && (name.Contains("Core") || name.Contains("GPU")))
							{
								num3 = Math.Max(num3, value);
							}
							if (text2 == "SmallData" && name.Contains("Memory Used"))
							{
								gpuMemUsedMb = value;
							}
							if (text2 == "SmallData" && name.Contains("Memory Total") && value > 0f)
							{
								num4 = value;
							}
							if (text2 == "Load" && name.Contains("Memory"))
							{
								gpuMemUsedMb = num4 * (double)value / 100.0;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_log.Debug("LHM sample failed: " + ex.Message);
			}
		}
		double num6 = SafeNext(_diskCounter);
		if (num6 < 0.0)
		{
			num6 = 0.0;
		}
		double networkKbps = SampleNetwork();
		return new HardwareSample(Math.Clamp(num, 0.0, 100.0), num5, _cpuCores, Math.Clamp(num2, 0.0, 100.0), num3, gpuMemUsedMb, num4, _gpuName, ramUsedMb, ramTotalMb, Math.Clamp(num6, 0.0, 100.0), networkKbps);
	}

	private double SampleNetwork()
	{
		try
		{
			if (_netCounter == null)
			{
				return 0.0;
			}
			return (double)_netCounter.NextValue() * 8.0 / 1000.0;
		}
		catch
		{
			return 0.0;
		}
	}

	private static double SafeNext(PerformanceCounter? c)
	{
		try
		{
			return (c != null) ? c.NextValue() : 0f;
		}
		catch
		{
			return 0.0;
		}
	}

	public void Dispose()
	{
		try
		{
			((Component)(object)_cpuCounter)?.Dispose();
		}
		catch
		{
		}
		try
		{
			((Component)(object)_diskCounter)?.Dispose();
		}
		catch
		{
		}
		try
		{
			((Component)(object)_netCounter)?.Dispose();
		}
		catch
		{
		}
		try
		{
			_computer?.Close();
		}
		catch
		{
		}
	}
}
