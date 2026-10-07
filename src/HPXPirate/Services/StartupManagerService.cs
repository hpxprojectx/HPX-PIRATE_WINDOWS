using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace HPXPirate.Services;

public sealed class StartupManagerService
{
	private readonly ILogger _log;

	private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string AppName = "HPXPIRATE";

	public StartupManagerService(ILogger log)
	{
		_log = log;
	}

	public bool IsEnabled()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			return registryKey?.GetValue("HPXPIRATE") != null;
		}
		catch
		{
			return false;
		}
	}

	public void SetEnabled(bool enabled)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey != null)
			{
				if (enabled)
				{
					string text = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "HPXPirate.exe");
					registryKey.SetValue("HPXPIRATE", "\"" + text + "\" --tray");
					_log.Info("Added to Windows startup.");
				}
				else
				{
					registryKey.DeleteValue("HPXPIRATE", throwOnMissingValue: false);
					_log.Info("Removed from Windows startup.");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Warn("Could not change startup setting: " + ex.Message);
		}
	}

	public List<(string Name, string Command, string Source)> ListStartupEntries()
	{
		List<(string, string, string)> list = new List<(string, string, string)>();
		ReadRunKey(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", "HKCU", list);
		ReadRunKey(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", "HKLM", list);
		return list;
	}

	private static void ReadRunKey(RegistryKey root, string path, string source, List<(string, string, string)> list)
	{
		try
		{
			using RegistryKey registryKey = root.OpenSubKey(path);
			if (registryKey != null)
			{
				string[] valueNames = registryKey.GetValueNames();
				foreach (string text in valueNames)
				{
					string item = registryKey.GetValue(text)?.ToString() ?? "";
					list.Add((text, item, source));
				}
			}
		}
		catch
		{
		}
	}
}
