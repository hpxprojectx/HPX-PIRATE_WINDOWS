using System;
using System.IO;
using System.Management;

namespace HPXPirate.Services;

public sealed class RestorePointService
{
	private readonly ILogger _log;

	public RestorePointService(ILogger log)
	{
		_log = log;
	}

	public bool TryCreateRestorePoint(string description, out string message)
	{
		try
		{
			ManagementScope managementScope = new ManagementScope("\\\\.\\root\\default");
			managementScope.Connect();
			using ManagementClass managementClass = new ManagementClass(managementScope, new ManagementPath("SystemRestore"), null);
			ManagementBaseObject methodParameters = managementClass.GetMethodParameters("CreateRestorePoint");
			methodParameters["Description"] = description;
			methodParameters["RestorePointType"] = 12;
			methodParameters["EventType"] = 100;
			managementClass.InvokeMethod("CreateRestorePoint", methodParameters, null);
			message = "System Restore point created.";
			_log.Info("Restore point created: " + description);
			return true;
		}
		catch (Exception ex)
		{
			message = "System Restore point could not be created (System Restore may be disabled). An HPX internal backup will be used instead.";
			_log.Warn("Restore point failed: " + ex.Message);
			return false;
		}
	}

	public string WriteInternalSnapshot(string json)
	{
		AppPaths.EnsureCreated();
		string text = Path.Combine(AppPaths.RestorePoints, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json");
		File.WriteAllText(text, json);
		_log.Info("Internal restore snapshot written: " + text);
		return text;
	}

	public bool SystemRestoreEnabled()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\default", "SELECT * FROM SystemRestore");
			using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher.Get().GetEnumerator())
			{
				if (managementObjectEnumerator.MoveNext())
				{
					_ = (ManagementObject)managementObjectEnumerator.Current;
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
}
