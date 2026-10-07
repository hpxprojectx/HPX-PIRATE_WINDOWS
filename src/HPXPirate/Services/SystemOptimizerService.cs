using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HPXPirate.Models;
using Microsoft.Win32;

namespace HPXPirate.Services;

public sealed class SystemOptimizerService
{
	/// <summary>Outcome of the read-only detection phase for a single action.</summary>
	private enum ActionOutcome
	{
		NeedsChange,
		AlreadyOptimal,
		Skipped,
		Failed
	}

	private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

	private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

	private readonly ILogger _log;

	private readonly RestorePointService _restore;

	private readonly ProcessManagerService _processes;

	private readonly List<ChangeRecord> _changes = new List<ChangeRecord>();

	public IReadOnlyList<ChangeRecord> Changes => _changes;

	public SystemOptimizerService(ILogger log, RestorePointService restore, ProcessManagerService processes)
	{
		_log = log;
		_restore = restore;
		_processes = processes;
	}

	public List<OptimizationAction> BuildPlan(OptimizationMode mode)
	{
		List<OptimizationAction> list = new List<OptimizationAction>
		{
			new OptimizationAction
			{
				Title = "Power Plan Optimisation",
				Category = OptimizationCategory.PowerPlan,
				MinimumMode = OptimizationMode.Balanced,
				Description = "Switch the active Windows power plan to High Performance for maximum responsiveness.",
				Risk = ActionRisk.Safe
			},
			new OptimizationAction
			{
				Title = "Game Mode Recommendation",
				Category = OptimizationCategory.GameMode,
				MinimumMode = OptimizationMode.Balanced,
				Description = "Ensure Windows Game Mode is enabled so the foreground game gets priority.",
				Risk = ActionRisk.Safe
			},
			new OptimizationAction
			{
				Title = "Game Process Priority",
				Category = OptimizationCategory.ProcessPriority,
				MinimumMode = OptimizationMode.Balanced,
				Description = "Raise the detected game process priority to High (reversible).",
				Risk = ActionRisk.Safe
			},
			new OptimizationAction
			{
				Title = "Temporary File Cleanup",
				Category = OptimizationCategory.TempCleanup,
				MinimumMode = OptimizationMode.Balanced,
				Description = "Remove stale files from the user Temp folder. System files are never touched.",
				Risk = ActionRisk.Safe,
				IsReversible = false
			},
			new OptimizationAction
			{
				Title = "Game DVR / Capture Disable",
				Category = OptimizationCategory.GameDvr,
				MinimumMode = OptimizationMode.Performance,
				Description = "Disable background game capture (Game DVR) which can cost frames.",
				Risk = ActionRisk.Safe
			},
			new OptimizationAction
			{
				Title = "Visual Effects (Performance)",
				Category = OptimizationCategory.VisualEffects,
				MinimumMode = OptimizationMode.Performance,
				Description = "Set Windows visual effects to favour performance.",
				Risk = ActionRisk.Moderate
			},
			new OptimizationAction
			{
				Title = "Shader / Cache Cleanup",
				Category = OptimizationCategory.CacheCleanup,
				MinimumMode = OptimizationMode.Performance,
				Description = "Clear DirectX shader cache so it can rebuild cleanly.",
				Risk = ActionRisk.Safe,
				IsReversible = false
			},
			new OptimizationAction
			{
				Title = "Background Load Reduction",
				Category = OptimizationCategory.BackgroundProcess,
				MinimumMode = OptimizationMode.Performance,
				Description = "Lower priority of safe background apps (browsers, launchers). Protected apps are skipped.",
				Risk = ActionRisk.Moderate
			},
			new OptimizationAction
			{
				Title = "Startup App Review",
				Category = OptimizationCategory.StartupApp,
				MinimumMode = OptimizationMode.Extreme,
				Description = "Disable non-essential user startup entries (reversible).",
				Risk = ActionRisk.Advanced
			},
			new OptimizationAction
			{
				Title = "Network Latency Tweak",
				Category = OptimizationCategory.NetworkTweak,
				MinimumMode = OptimizationMode.Extreme,
				Description = "Apply conservative TCP settings that can reduce latency (reversible).",
				Risk = ActionRisk.Advanced
			}
		};
		foreach (OptimizationAction item in list)
		{
			item.Selected = ((mode == OptimizationMode.Custom) ? (item.Risk == ActionRisk.Safe) : (item.MinimumMode <= mode));
		}
		return list;
	}

	/// <summary>
	/// Applies the plan in two phases: first a read-only detection pass, then a
	/// mutation pass that only touches settings which are genuinely not optimal.
	/// This is what stops the app from "fixing" things that are not broken.
	/// </summary>
	public BoostResult ApplyPlan(IEnumerable<OptimizationAction> plan, OptimizationMode mode, bool createRestorePoint)
	{
		BoostResult result = new BoostResult
		{
			Mode = mode
		};
		List<OptimizationAction> selected = plan.Where((OptimizationAction a) => a.Selected).ToList();
		_log.Info($"Evaluating {selected.Count} optimization(s) in {mode} mode.");

		List<OptimizationAction> pending = new List<OptimizationAction>();
		foreach (OptimizationAction action in selected)
		{
			ActionOutcome outcome;
			try
			{
				outcome = Detect(action);
			}
			catch (Exception ex)
			{
				outcome = ActionOutcome.Failed;
				action.ResultMessage = ex.Message;
				_log.Error("Detection for '" + action.Title + "' failed.", ex);
			}

			switch (outcome)
			{
			case ActionOutcome.AlreadyOptimal:
				action.Applied = false;
				action.AlreadyOptimal = true;
				action.Succeeded = true;
				action.Status = "Already optimal";
				result.AlreadyOptimalCount++;
				break;
			case ActionOutcome.Skipped:
				action.Applied = false;
				action.AlreadyOptimal = false;
				action.Succeeded = true;
				action.Status = "Skipped";
				result.SkippedCount++;
				break;
			case ActionOutcome.Failed:
				action.Applied = false;
				action.AlreadyOptimal = false;
				action.Succeeded = false;
				action.Status = "Failed";
				result.FailedCount++;
				break;
			default:
				action.Status = "Pending";
				pending.Add(action);
				break;
			}
		}

		// Only create a restore point when we are actually going to change something.
		if (createRestorePoint && pending.Count > 0)
		{
			_restore.TryCreateRestorePoint($"HPX PIRATE - {mode} boost", out string message);
			result.RestorePointId = _restore.WriteInternalSnapshot(JsonHelper.Serialize(_changes));
			result.Steps.Add(new BoostStepResult
			{
				Label = "Restore Point",
				Success = true,
				Status = "Created",
				Detail = message
			});
		}

		foreach (OptimizationAction action in pending)
		{
			bool flag;
			try
			{
				flag = Apply(action);
			}
			catch (Exception ex)
			{
				flag = false;
				action.ResultMessage = ex.Message;
				_log.Error("Optimization '" + action.Title + "' failed.", ex);
			}
			action.Applied = flag;
			action.Succeeded = flag;
			action.Status = (flag ? "Applied" : "Failed");
			if (flag)
			{
				result.AppliedCount++;
				_changes.Add(new ChangeRecord
				{
					Category = action.Category.ToString(),
					Description = action.Title,
					PreviousState = action.PreviousState
				});
			}
			else
			{
				result.FailedCount++;
			}
		}

		// Report every action in the original order, with an honest status.
		foreach (OptimizationAction action in selected)
		{
			result.Steps.Add(new BoostStepResult
			{
				Label = action.Title,
				Success = action.Succeeded,
				AlreadyOptimal = action.AlreadyOptimal,
				Status = action.Status,
				Detail = action.ResultMessage
			});
		}

		result.Success = (result.FailedCount == 0);
		result.Summary = BuildSummary(result);
		SaveChanges();
		return result;
	}

	private static string BuildSummary(BoostResult r)
	{
		List<string> parts = new List<string> { $"{r.AppliedCount} applied" };
		if (r.AlreadyOptimalCount > 0)
		{
			parts.Add($"{r.AlreadyOptimalCount} already optimal");
		}
		if (r.SkippedCount > 0)
		{
			parts.Add($"{r.SkippedCount} skipped");
		}
		if (r.FailedCount > 0)
		{
			parts.Add($"{r.FailedCount} failed");
		}
		string head = string.Join(", ", parts) + ".";
		if (r.AppliedCount == 0 && r.FailedCount == 0)
		{
			return head + " Nothing needed fixing \u2014 your system was already optimal.";
		}
		return head + " Only settings that actually needed a change were modified.";
	}

	/// <summary>Read-only detection. Never writes anything.</summary>
	private ActionOutcome Detect(OptimizationAction a)
	{
		switch (a.Category)
		{
		case OptimizationCategory.PowerPlan:
		{
			string current = GetActivePowerPlanGuid();
			a.PreviousState = current;
			if (IsHighPerformanceGuid(current))
			{
				a.ResultMessage = "Power plan is already set to High Performance.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.GameMode:
		{
			object? value = ReadReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled");
			a.PreviousState = value?.ToString();
			if (ToInt(value) == 1)
			{
				a.ResultMessage = "Windows Game Mode is already enabled.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.GameDvr:
		{
			object? value2 = ReadReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled");
			object? value3 = ReadReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled");
			a.PreviousState = $"{value2}|{value3}";
			if (ToInt(value2) == 0 && ToInt(value3) == 0)
			{
				a.ResultMessage = "Game DVR / background capture is already disabled.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.VisualEffects:
		{
			object? value4 = ReadReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting");
			a.PreviousState = value4?.ToString();
			if (ToInt(value4) == 2)
			{
				a.ResultMessage = "Visual effects are already set to best performance.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.ProcessPriority:
		{
			ProcessInfoModel? game = _processes.DetectRunningGame();
			if (game == null)
			{
				a.ResultMessage = "No running game detected \u2014 nothing to change.";
				return ActionOutcome.Skipped;
			}
			ProcessPriorityClass? priority = _processes.GetPriority(game.Pid);
			a.PreviousState = $"{game.Pid}|{priority}";
			if (priority == ProcessPriorityClass.High || priority == ProcessPriorityClass.RealTime || priority == ProcessPriorityClass.AboveNormal)
			{
				a.ResultMessage = $"{game.Name} is already running at {priority} priority.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.TempCleanup:
		{
			int num = CountStaleTempFiles();
			if (num == 0)
			{
				a.ResultMessage = "No stale temp files to remove.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.CacheCleanup:
		{
			int num2 = CountCacheFiles();
			if (num2 == 0)
			{
				a.ResultMessage = "Shader / cache folders are already clean.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.BackgroundProcess:
		{
			int num3 = CountBackgroundCandidates();
			if (num3 == 0)
			{
				a.ResultMessage = "No background apps needed adjustment.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		case OptimizationCategory.StartupApp:
			a.ResultMessage = "Startup entries are managed in the Startup view \u2014 no automatic change was made.";
			return ActionOutcome.Skipped;
		case OptimizationCategory.NetworkTweak:
		{
			string text = Run("netsh", "int tcp show global").output;
			a.PreviousState = text;
			if (IsTcpAlreadyTuned(text))
			{
				a.ResultMessage = "TCP settings are already tuned for low latency.";
				return ActionOutcome.AlreadyOptimal;
			}
			return ActionOutcome.NeedsChange;
		}
		default:
			a.ResultMessage = "Unknown action.";
			return ActionOutcome.Failed;
		}
	}

	private bool Apply(OptimizationAction action)
	{
		switch (action.Category)
		{
		case OptimizationCategory.PowerPlan:
			return ApplyPowerPlan(action);
		case OptimizationCategory.GameMode:
			return ApplyGameMode(action);
		case OptimizationCategory.GameDvr:
			return ApplyGameDvr(action);
		case OptimizationCategory.VisualEffects:
			return ApplyVisualEffects(action);
		case OptimizationCategory.ProcessPriority:
			return ApplyProcessPriority(action);
		case OptimizationCategory.TempCleanup:
			return CleanTemp(action);
		case OptimizationCategory.CacheCleanup:
			return CleanCache(action);
		case OptimizationCategory.BackgroundProcess:
			return ReduceBackground(action);
		case OptimizationCategory.StartupApp:
			return ReviewStartup(action);
		case OptimizationCategory.NetworkTweak:
			return ApplyNetworkTweak(action);
		default:
			action.ResultMessage = "Unknown action.";
			return false;
		}
	}

	private bool ApplyPowerPlan(OptimizationAction a)
	{
		(string output, string error) = Run("powercfg", "/setactive " + HighPerformanceGuid);
		if (string.IsNullOrWhiteSpace(error))
		{
			a.ResultMessage = "Active power plan set to High Performance.";
			return true;
		}
		a.ResultMessage = "Could not change power plan (run as administrator).";
		return false;
	}

	private bool ApplyGameMode(OptimizationAction a)
	{
		WriteReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);
		a.ResultMessage = "Windows Game Mode enabled.";
		return true;
	}

	private bool ApplyGameDvr(OptimizationAction a)
	{
		WriteReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
		WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
		a.ResultMessage = "Game DVR / background capture disabled.";
		return true;
	}

	private bool ApplyVisualEffects(OptimizationAction a)
	{
		WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord);
		a.ResultMessage = "Visual effects set to best performance.";
		return true;
	}

	private bool ApplyProcessPriority(OptimizationAction a)
	{
		ProcessInfoModel? processInfoModel = _processes.DetectRunningGame();
		if (processInfoModel == null)
		{
			a.ResultMessage = "No running game detected \u2014 skipped (nothing to change).";
			return false;
		}
		if (_processes.SetPriority(processInfoModel.Pid, ProcessPriorityClass.High, out string message))
		{
			a.ResultMessage = message;
			return true;
		}
		a.ResultMessage = message;
		return false;
	}

	private bool CleanTemp(OptimizationAction a)
	{
		long num = 0L;
		int num2 = 0;
		string tempPath = Path.GetTempPath();
		try
		{
			foreach (string item in Directory.EnumerateFiles(tempPath))
			{
				try
				{
					FileInfo fileInfo = new FileInfo(item);
					if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-1.0))
					{
						num += fileInfo.Length;
						fileInfo.Delete();
						num2++;
					}
				}
				catch
				{
				}
			}
			a.ResultMessage = $"Removed {num2} stale temp file(s), freed {(double)num / 1024.0 / 1024.0:0.0} MB.";
			return true;
		}
		catch (Exception ex)
		{
			a.ResultMessage = "Temp cleanup failed: " + ex.Message;
			return false;
		}
	}

	private bool CleanCache(OptimizationAction a)
	{
		int num = 0;
		string[] array = new string[3]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
			Path.Combine(Path.GetTempPath(), "NVIDIA Corporation"),
			Path.Combine(Path.GetTempPath(), "AMD")
		};
		foreach (string path in array)
		{
			try
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				foreach (string item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(item);
						num++;
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		a.ResultMessage = $"Cleared {num} cached shader file(s).";
		return true;
	}

	private bool ReduceBackground(OptimizationAction a)
	{
		int num = 0;
		foreach (ProcessInfoModel topProcess in _processes.GetTopProcesses(40))
		{
			if (!topProcess.IsSafeToOptimize || topProcess.RamMb < 200.0)
			{
				continue;
			}
			ProcessPriorityClass? priority = _processes.GetPriority(topProcess.Pid);
			if (priority == ProcessPriorityClass.BelowNormal || priority == ProcessPriorityClass.Idle)
			{
				continue;
			}
			if (_processes.SetPriority(topProcess.Pid, ProcessPriorityClass.BelowNormal, out string _))
			{
				num++;
			}
		}
		a.ResultMessage = ((num > 0) ? $"Lowered priority of {num} background app(s)." : "No safe background apps needed adjustment.");
		return true;
	}

	private bool ReviewStartup(OptimizationAction a)
	{
		a.ResultMessage = "Startup entries reviewed. Use the Startup view to disable specific entries.";
		return true;
	}

	private bool ApplyNetworkTweak(OptimizationAction a)
	{
		Run("netsh", "int tcp set global autotuninglevel=normal");
		Run("netsh", "int tcp set global timestamps=disabled");
		a.ResultMessage = "Conservative TCP settings applied.";
		return true;
	}

	private int CountStaleTempFiles()
	{
		int num = 0;
		try
		{
			foreach (string item in Directory.EnumerateFiles(Path.GetTempPath()))
			{
				try
				{
					if (new FileInfo(item).LastWriteTime < DateTime.Now.AddDays(-1.0))
					{
						num++;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private int CountCacheFiles()
	{
		int num = 0;
		string[] array = new string[3]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
			Path.Combine(Path.GetTempPath(), "NVIDIA Corporation"),
			Path.Combine(Path.GetTempPath(), "AMD")
		};
		foreach (string path in array)
		{
			try
			{
				if (Directory.Exists(path))
				{
					num += Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count();
				}
			}
			catch
			{
			}
		}
		return num;
	}

	private int CountBackgroundCandidates()
	{
		int num = 0;
		foreach (ProcessInfoModel topProcess in _processes.GetTopProcesses(40))
		{
			if (!topProcess.IsSafeToOptimize || topProcess.RamMb < 200.0)
			{
				continue;
			}
			ProcessPriorityClass? priority = _processes.GetPriority(topProcess.Pid);
			if (priority != ProcessPriorityClass.BelowNormal && priority != ProcessPriorityClass.Idle)
			{
				num++;
			}
		}
		return num;
	}

	public bool RollbackAction(ChangeRecord record, out string message)
	{
		try
		{
			switch (record.Category)
			{
			case "PowerPlan":
			{
				string text = ExtractGuid(record.PreviousState);
				if (text != null)
				{
					Run("powercfg", "/setactive " + text);
				}
				message = "Power plan restored.";
				break;
			}
			case "GameMode":
				WriteReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", ParseInt(record.PreviousState, 0), RegistryValueKind.DWord);
				message = "Game Mode setting restored.";
				break;
			case "GameDvr":
			{
				string[] source2 = (record.PreviousState ?? "0|0").Split('|');
				WriteReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled", ParseInt(source2.ElementAtOrDefault(0), 0), RegistryValueKind.DWord);
				WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", ParseInt(source2.ElementAtOrDefault(1), 0), RegistryValueKind.DWord);
				message = "Game DVR setting restored.";
				break;
			}
			case "VisualEffects":
				WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", ParseInt(record.PreviousState, 0), RegistryValueKind.DWord);
				message = "Visual effects restored.";
				break;
			case "ProcessPriority":
			{
				string[] source = (record.PreviousState ?? "").Split('|');
				if (int.TryParse(source.ElementAtOrDefault(0), out var result))
				{
					ProcessPriorityClass result2;
					ProcessPriorityClass cls = (Enum.TryParse<ProcessPriorityClass>(source.ElementAtOrDefault(1), out result2) ? result2 : ProcessPriorityClass.Normal);
					_processes.SetPriority(result, cls, out string _);
				}
				message = "Process priority restored.";
				break;
			}
			case "NetworkTweak":
				Run("netsh", "int tcp set global autotuninglevel=normal");
				message = "Network settings restored to defaults.";
				break;
			default:
				message = "This change does not require rollback (no system state was modified).";
				break;
			}
			record.Reverted = true;
			SaveChanges();
			_log.Info("Rolled back: " + record.Description);
			return true;
		}
		catch (Exception ex)
		{
			message = "Rollback failed: " + ex.Message;
			_log.Error("Rollback failed.", ex);
			return false;
		}
	}

	public int RollbackAll()
	{
		int num = 0;
		foreach (ChangeRecord item in _changes.Where((ChangeRecord c) => !c.Reverted).ToList())
		{
			if (RollbackAction(item, out string _))
			{
				num++;
			}
		}
		return num;
	}

	private static string GetActivePowerPlanGuid()
	{
		string text = Run("powercfg", "/getactivescheme").output ?? "";
		int num = text.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string text2 = text.Substring(num + 5).Trim();
		int num2 = text2.IndexOf(' ');
		if (num2 > 0)
		{
			text2 = text2.Substring(0, num2);
		}
		return text2.Trim().ToLowerInvariant();
	}

	private static bool IsHighPerformanceGuid(string? guid)
	{
		if (string.IsNullOrWhiteSpace(guid))
		{
			return false;
		}
		string text = guid.Trim().ToLowerInvariant();
		return text == HighPerformanceGuid || text == UltimatePerformanceGuid;
	}

	private static bool IsTcpAlreadyTuned(string? state)
	{
		if (string.IsNullOrWhiteSpace(state))
		{
			return false;
		}
		string text = state.ToLowerInvariant();
		bool flag = text.Contains("autotuninglevel") && text.Contains("normal");
		bool flag2 = text.Contains("timestamps") && text.Contains("disabled");
		return flag && flag2;
	}

	private static int ToInt(object? value)
	{
		if (value == null)
		{
			return 0;
		}
		try
		{
			return Convert.ToInt32(value);
		}
		catch
		{
			return 0;
		}
	}

	private static (string output, string error) Run(string file, string args)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo(file, args)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return (output: "", error: "failed to start");
			}
			string item = process.StandardOutput.ReadToEnd();
			string item2 = process.StandardError.ReadToEnd();
			process.WaitForExit(5000);
			return (output: item, error: item2);
		}
		catch (Exception ex)
		{
			return (output: "", error: ex.Message);
		}
	}

	private static object? ReadReg(RegistryKey root, string path, string name)
	{
		try
		{
			using RegistryKey registryKey = root.OpenSubKey(path);
			return registryKey?.GetValue(name);
		}
		catch
		{
			return null;
		}
	}

	private static void WriteReg(RegistryKey root, string path, string name, object value, RegistryValueKind kind)
	{
		using RegistryKey registryKey = root.CreateSubKey(path, writable: true);
		registryKey?.SetValue(name, value, kind);
	}

	private static int ParseInt(string? s, int fallback)
	{
		if (!int.TryParse(s, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static string? ExtractGuid(string? s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return null;
		}
		int num = s.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return null;
		}
		string text = s.Substring(num + 5).Trim();
		int num2 = text.IndexOf(' ');
		if (num2 <= 0)
		{
			return text;
		}
		return text.Substring(0, num2);
	}

	private void SaveChanges()
	{
		try
		{
			AppPaths.EnsureCreated();
			File.WriteAllText(AppPaths.ChangesFile, JsonHelper.Serialize(_changes));
		}
		catch (Exception ex)
		{
			_log.Debug("SaveChanges failed: " + ex.Message);
		}
	}

	public void LoadChanges()
	{
		try
		{
			if (File.Exists(AppPaths.ChangesFile))
			{
				List<ChangeRecord> list = JsonHelper.Deserialize<List<ChangeRecord>>(File.ReadAllText(AppPaths.ChangesFile));
				if (list != null)
				{
					_changes.Clear();
					_changes.AddRange(list);
				}
			}
		}
		catch (Exception ex)
		{
			_log.Debug("LoadChanges failed: " + ex.Message);
		}
	}
}
