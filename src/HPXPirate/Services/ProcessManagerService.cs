using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class ProcessManagerService
{
	private readonly ILogger _log;

	private static readonly HashSet<string> Protected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"system", "idle", "registry", "memory compression", "secure system", "smss", "csrss", "wininit", "winlogon", "services",
		"lsass", "lsaiso", "svchost", "dwm", "fontdrvhost", "sihost", "ctfmon", "taskhostw", "explorer", "shellexperiencehost",
		"startmenuexperiencehost", "searchhost", "runtimebroker", "audiodg", "conhost", "wmiprvse", "msmpeng", "nissrv", "securityhealthservice", "windefend",
		"avp", "avast", "avgsvc", "mcafee", "bdagent", "ekrn", "nortonsecurity", "nvcontainer", "nvdisplay.container", "amdow",
		"radeonsoftware", "igfxem", "igfxtray", "hpxpirate"
	};

	private static readonly HashSet<string> GameHints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"freefire", "garena", "pubg", "pubgm", "codm", "gameloop", "bluestacks", "ldplayer", "memu", "nox",
		"msiplayer", "steam", "steamwebhelper", "valorant", "cs2", "csgo", "fortnite", "dota2", "genshinimpact", "roblox",
		"leagueclient", "overwatch", "apex_legends", "r5apex", "rainbowsix"
	};

	public ProcessManagerService(ILogger log)
	{
		_log = log;
	}

	public bool IsProtected(string name)
	{
		return Protected.Contains(name);
	}

	public bool LooksLikeGame(string name)
	{
		string i = Path.GetFileNameWithoutExtension(name);
		if (!GameHints.Contains(i))
		{
			return GameHints.Any((string g) => i.Contains(g, StringComparison.OrdinalIgnoreCase));
		}
		return true;
	}

	public List<ProcessInfoModel> GetTopProcesses(int count = 25)
	{
		List<ProcessInfoModel> list = new List<ProcessInfoModel>();
		new Dictionary<int, (TimeSpan, DateTime)>();
		Process[] processes;
		try
		{
			processes = Process.GetProcesses();
		}
		catch (Exception ex)
		{
			_log.Error("GetProcesses failed.", ex);
			return list;
		}
		Process[] array = processes;
		foreach (Process process in array)
		{
			try
			{
				double ramMb = (double)process.WorkingSet64 / 1048576.0;
				double cpuPercent = 0.0;
				try
				{
					cpuPercent = process.TotalProcessorTime.TotalMilliseconds;
				}
				catch
				{
				}
				list.Add(new ProcessInfoModel
				{
					Pid = process.Id,
					Name = process.ProcessName,
					Description = SafeDescription(process),
					RamMb = ramMb,
					CpuPercent = cpuPercent,
					IsGame = LooksLikeGame(process.ProcessName),
					IsProtected = IsProtected(process.ProcessName)
				});
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		return list.OrderByDescending((ProcessInfoModel x) => x.RamMb).Take(count).ToList();
	}

	private static string SafeDescription(Process p)
	{
		try
		{
			return p.MainModule?.FileVersionInfo.FileDescription ?? "";
		}
		catch
		{
			return "";
		}
	}

	public bool SetPriority(int pid, ProcessPriorityClass cls, out string message)
	{
		try
		{
			using Process process = Process.GetProcessById(pid);
			if (IsProtected(process.ProcessName))
			{
				message = process.ProcessName + " is protected and was not modified.";
				return false;
			}
			process.PriorityClass = cls;
			message = $"{process.ProcessName} priority set to {cls}.";
			_log.Info(message);
			return true;
		}
		catch (Exception ex)
		{
			message = "Could not change priority: " + ex.Message;
			_log.Warn(message);
			return false;
		}
	}

	public ProcessPriorityClass? GetPriority(int pid)
	{
		try
		{
			using Process process = Process.GetProcessById(pid);
			return process.PriorityClass;
		}
		catch
		{
			return null;
		}
	}

	public ProcessInfoModel? DetectRunningGame()
	{
		foreach (ProcessInfoModel topProcess in GetTopProcesses(60))
		{
			if (topProcess.IsGame && !topProcess.IsProtected)
			{
				return topProcess;
			}
		}
		return null;
	}
}
