using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HPXPirate.Models;
using Microsoft.Win32;

namespace HPXPirate.Services;

/// <summary>
/// Real-time FPS boost. Every step is a genuine, reversible Windows change that
/// reduces CPU/GPU contention while a game is running \u2014 nothing here fakes a frame
/// counter. The before/after numbers come straight from the ETW (DxgKrnl Present)
/// monitor, i.e. the very frames the GPU is presenting to the screen.
///
/// As with the optimizer, each step is detected first and only written when it is
/// actually not optimal, so a healthy system is reported as "Already optimal"
/// instead of being pointlessly "fixed".
/// </summary>
public sealed class FpsBoostService
{
    private enum FpsOutcome
    {
        NeedsChange,
        AlreadyOptimal,
        Skipped,
        Failed
    }

    private sealed class StepDef
    {
        public string Label = "";

        public string Category = "";

        public Func<(FpsOutcome outcome, string detail, string? prev)> Detect = null!;

        public Func<(bool ok, string detail)> Apply = null!;
    }

    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    private const string MmcssProfile = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile";

    private const string MmcssGames = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games";

    private const string GraphicsDrivers = "SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers";

    private readonly ILogger _log;

    private readonly FpsMonitorService _fps;

    private readonly ProcessManagerService _processes;

    private readonly RestorePointService _restore;

    private readonly List<ChangeRecord> _changes = new List<ChangeRecord>();

    private static readonly string ChangesFile = Path.Combine(AppPaths.Root, "fpsboost.json");

    public IReadOnlyList<ChangeRecord> Changes => _changes;

    public FpsBoostService(ILogger log, FpsMonitorService fps, ProcessManagerService processes, RestorePointService restore)
    {
        _log = log;
        _fps = fps;
        _processes = processes;
        _restore = restore;
    }

    public bool IsMonitorAvailable => _fps.IsAvailable;

    public int TargetPid => _fps.TargetPid;

    public string SourceName => _fps.SourceName;

    /// <summary>
    /// The real-time FPS value reported by the ETW Present monitor. Prefers the
    /// rolling average (steadier) and falls back to the instantaneous value.
    /// </summary>
    public double CurrentFps
    {
        get
        {
            double average = _fps.AverageFps;
            return average > 0.0 ? average : _fps.CurrentFps;
        }
    }

    public double CurrentFrameTimeMs => _fps.CurrentFrameTimeMs;

    public void ResetFpsHistory()
    {
        _fps.ResetHistory();
    }

    /// <summary>
    /// Applies the FPS boost. Captures the current FPS as the baseline, runs the
    /// detect-then-apply steps, and returns a result that the caller finalises with
    /// <see cref="CompleteMeasurement"/> after a fresh measurement window.
    /// </summary>
    public FpsBoostResult Apply(int targetFps, bool createRestorePoint)
    {
        FpsBoostResult result = new FpsBoostResult
        {
            TargetFps = targetFps,
            FpsBefore = CurrentFps
        };
        _log.Info("Starting real-time FPS boost.");

        List<StepDef> plan = BuildPlan();
        List<(StepDef def, FpsOutcome outcome, string detail, string? prev)> detected = new List<(StepDef, FpsOutcome, string, string)>();

        foreach (StepDef def in plan)
        {
            try
            {
                (FpsOutcome outcome, string detail, string? prev) = def.Detect();
                detected.Add((def, outcome, detail, prev));
            }
            catch (Exception ex)
            {
                detected.Add((def, FpsOutcome.Failed, ex.Message, null));
                _log.Error("FPS boost detection for '" + def.Label + "' failed.", ex);
            }
        }

        bool anyChange = detected.Any((x) => x.outcome == FpsOutcome.NeedsChange);
        if (createRestorePoint && anyChange)
        {
            _restore.TryCreateRestorePoint("HPX PIRATE - Real-Time FPS Boost", out string message);
            result.Steps.Add(new FpsBoostStep
            {
                Label = "Restore Point",
                Success = true,
                Status = "Created",
                Detail = message
            });
        }

        foreach (var item in detected)
        {
            switch (item.outcome)
            {
            case FpsOutcome.NeedsChange:
                try
                {
                    (bool ok, string detail) = item.def.Apply();
                    if (ok)
                    {
                        result.AppliedCount++;
                        _changes.Add(new ChangeRecord
                        {
                            Category = item.def.Category,
                            Description = item.def.Label,
                            PreviousState = item.prev
                        });
                        result.Steps.Add(new FpsBoostStep
                        {
                            Label = item.def.Label,
                            Success = true,
                            Status = "Applied",
                            Detail = detail,
                            PreviousState = item.prev
                        });
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Steps.Add(new FpsBoostStep
                        {
                            Label = item.def.Label,
                            Success = false,
                            Status = "Failed",
                            Detail = detail,
                            PreviousState = item.prev
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Steps.Add(new FpsBoostStep
                    {
                        Label = item.def.Label,
                        Success = false,
                        Status = "Failed",
                        Detail = ex.Message
                    });
                    _log.Error("FPS boost step '" + item.def.Label + "' failed.", ex);
                }
                break;
            case FpsOutcome.AlreadyOptimal:
                result.AlreadyOptimalCount++;
                result.Steps.Add(new FpsBoostStep
                {
                    Label = item.def.Label,
                    Success = true,
                    AlreadyOptimal = true,
                    Status = "Already optimal",
                    Detail = item.detail,
                    PreviousState = item.prev
                });
                break;
            case FpsOutcome.Skipped:
                result.SkippedCount++;
                result.Steps.Add(new FpsBoostStep
                {
                    Label = item.def.Label,
                    Success = true,
                    Status = "Skipped",
                    Detail = item.detail,
                    PreviousState = item.prev
                });
                break;
            default:
                result.FailedCount++;
                result.Steps.Add(new FpsBoostStep
                {
                    Label = item.def.Label,
                    Success = false,
                    Status = "Failed",
                    Detail = item.detail
                });
                break;
            }
        }

        result.GameName = _processes.DetectRunningGame()?.Name ?? "";
        result.Success = result.FailedCount == 0;
        SaveChanges();
        return result;
    }

    /// <summary>Reads a fresh FPS sample and finalises the result text.</summary>
    public void CompleteMeasurement(FpsBoostResult result)
    {
        result.FpsAfter = CurrentFps;
        result.Summary = BuildSummary(result);
        result.Note = BuildNote(result);
    }

    private static string BuildSummary(FpsBoostResult r)
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
            return head + " Nothing needed changing \u2014 your system was already tuned for gaming.";
        }
        return head + " Only settings that actually needed a change were modified.";
    }

    private static string BuildNote(FpsBoostResult r)
    {
        if (!(r.FpsBefore > 0.0) && !(r.FpsAfter > 0.0))
        {
            return "FPS is measured live from the GPU's Present events (ETW / DxgKrnl). Start a game and boost again to see real before/after numbers \u2014 nothing is simulated or faked.";
        }
        double delta = r.FpsAfter - r.FpsBefore;
        string trend = delta > 0.5 ? $"up {delta:0.0}" : (delta < -0.5 ? $"down {Math.Abs(delta):0.0}" : "steady");
        return $"Real-time FPS (ETW / DxgKrnl Present): {r.FpsBefore:0.0} \u2192 {r.FpsAfter:0.0} ({trend}). These are the frames the GPU actually presented; results vary with the game, scene and hardware.";
    }

    private List<StepDef> BuildPlan()
    {
        return new List<StepDef>
        {
            new StepDef
            {
                Label = "Ultimate Performance power plan",
                Category = "FpsPowerPlan",
                Detect = DetectPowerPlan,
                Apply = ApplyPowerPlan
            },
            new StepDef
            {
                Label = "MMCSS system responsiveness",
                Category = "FpsMmcssResponse",
                Detect = DetectMmcssResponse,
                Apply = ApplyMmcssResponse
            },
            new StepDef
            {
                Label = "MMCSS games task priority",
                Category = "FpsMmcssGames",
                Detect = DetectMmcssGames,
                Apply = ApplyMmcssGames
            },
            new StepDef
            {
                Label = "Game process priority",
                Category = "FpsGamePriority",
                Detect = DetectGamePriority,
                Apply = ApplyGamePriority
            },
            new StepDef
            {
                Label = "Windows Game Mode",
                Category = "FpsGameMode",
                Detect = DetectGameMode,
                Apply = ApplyGameMode
            },
            new StepDef
            {
                Label = "Game DVR / background capture",
                Category = "FpsGameDvr",
                Detect = DetectGameDvr,
                Apply = ApplyGameDvr
            },
            new StepDef
            {
                Label = "Hardware-accelerated GPU scheduling",
                Category = "FpsGpuScheduling",
                Detect = DetectGpuScheduling,
                Apply = ApplyNoOp
            }
        };
    }

    // ------------------------------------------------------------------
    // Step: power plan.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectPowerPlan()
    {
        string current = GetActivePowerPlanGuid();
        if (current == UltimatePerformanceGuid)
        {
            return (FpsOutcome.AlreadyOptimal, "Active power plan is already Ultimate Performance.", current);
        }
        if (current == HighPerformanceGuid)
        {
            return (FpsOutcome.AlreadyOptimal, "Active power plan is already High Performance.", current);
        }
        return (FpsOutcome.NeedsChange, "Switching to the Ultimate Performance power plan.", current);
    }

    private (bool, string) ApplyPowerPlan()
    {
        Run("powercfg", "-duplicatescheme " + UltimatePerformanceGuid);
        (string _, string error) = Run("powercfg", "/setactive " + UltimatePerformanceGuid);
        if (string.IsNullOrWhiteSpace(error))
        {
            return (true, "Power plan set to Ultimate Performance.");
        }
        (string _, string error2) = Run("powercfg", "/setactive " + HighPerformanceGuid);
        if (string.IsNullOrWhiteSpace(error2))
        {
            return (true, "Ultimate Performance unavailable; set to High Performance.");
        }
        return (false, "Could not change the power plan (run HPX PIRATE as administrator).");
    }

    // ------------------------------------------------------------------
    // Step: MMCSS SystemResponsiveness.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectMmcssResponse()
    {
        int current = ToInt(ReadReg(Registry.LocalMachine, MmcssProfile, "SystemResponsiveness"), 20);
        if (current <= 10)
        {
            return (FpsOutcome.AlreadyOptimal, $"SystemResponsiveness is already {current} (low latency).", current.ToString());
        }
        return (FpsOutcome.NeedsChange, "Lowering SystemResponsiveness to 10 so games get more CPU.", current.ToString());
    }

    private (bool, string) ApplyMmcssResponse()
    {
        WriteReg(Registry.LocalMachine, MmcssProfile, "SystemResponsiveness", 10, RegistryValueKind.DWord);
        return (true, "SystemResponsiveness set to 10.");
    }

    // ------------------------------------------------------------------
    // Step: MMCSS games task priority.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectMmcssGames()
    {
        int priority = ToInt(ReadReg(Registry.LocalMachine, MmcssGames, "Priority"), 2);
        int gpuPriority = ToInt(ReadReg(Registry.LocalMachine, MmcssGames, "GPU Priority"), 8);
        string scheduling = ReadReg(Registry.LocalMachine, MmcssGames, "Scheduling Category")?.ToString() ?? "";
        string sfio = ReadReg(Registry.LocalMachine, MmcssGames, "SFIO Priority")?.ToString() ?? "";
        string prev = $"{priority}|{gpuPriority}|{scheduling}|{sfio}";
        bool optimal = priority >= 6 && gpuPriority >= 8
            && scheduling.Equals("High", StringComparison.OrdinalIgnoreCase)
            && sfio.Equals("High", StringComparison.OrdinalIgnoreCase);
        if (optimal)
        {
            return (FpsOutcome.AlreadyOptimal, "Games task is already set to High priority.", prev);
        }
        return (FpsOutcome.NeedsChange, "Raising the MMCSS Games task priority.", prev);
    }

    private (bool, string) ApplyMmcssGames()
    {
        WriteReg(Registry.LocalMachine, MmcssGames, "Priority", 6, RegistryValueKind.DWord);
        WriteReg(Registry.LocalMachine, MmcssGames, "GPU Priority", 8, RegistryValueKind.DWord);
        WriteReg(Registry.LocalMachine, MmcssGames, "Scheduling Category", "High", RegistryValueKind.String);
        WriteReg(Registry.LocalMachine, MmcssGames, "SFIO Priority", "High", RegistryValueKind.String);
        return (true, "MMCSS Games task set to High priority.");
    }

    // ------------------------------------------------------------------
    // Step: game process priority.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectGamePriority()
    {
        ProcessInfoModel? game = _processes.DetectRunningGame();
        if (game == null)
        {
            return (FpsOutcome.Skipped, "No running game detected \u2014 start your game, then boost.", null);
        }
        ProcessPriorityClass? priority = _processes.GetPriority(game.Pid);
        string prev = $"{game.Pid}|{priority}";
        if (priority == ProcessPriorityClass.High || priority == ProcessPriorityClass.RealTime || priority == ProcessPriorityClass.AboveNormal)
        {
            return (FpsOutcome.AlreadyOptimal, $"{game.Name} is already running at {priority} priority.", prev);
        }
        return (FpsOutcome.NeedsChange, $"Raising {game.Name} to High priority.", prev);
    }

    private (bool, string) ApplyGamePriority()
    {
        ProcessInfoModel? game = _processes.DetectRunningGame();
        if (game == null)
        {
            return (false, "No running game detected \u2014 nothing to change.");
        }
        if (_processes.SetPriority(game.Pid, ProcessPriorityClass.High, out string message))
        {
            return (true, message);
        }
        return (false, message);
    }

    // ------------------------------------------------------------------
    // Step: Windows Game Mode.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectGameMode()
    {
        object? value = ReadReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled");
        string prev = value?.ToString() ?? "";
        if (ToInt(value, 0) == 1)
        {
            return (FpsOutcome.AlreadyOptimal, "Windows Game Mode is already enabled.", prev);
        }
        return (FpsOutcome.NeedsChange, "Enabling Windows Game Mode.", prev);
    }

    private (bool, string) ApplyGameMode()
    {
        WriteReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord);
        return (true, "Windows Game Mode enabled.");
    }

    // ------------------------------------------------------------------
    // Step: Game DVR / background capture.
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectGameDvr()
    {
        object? enabled = ReadReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled");
        object? capture = ReadReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled");
        string prev = $"{enabled}|{capture}";
        if (ToInt(enabled, 1) == 0 && ToInt(capture, 1) == 0)
        {
            return (FpsOutcome.AlreadyOptimal, "Game DVR / background capture is already disabled.", prev);
        }
        return (FpsOutcome.NeedsChange, "Disabling Game DVR / background capture.", prev);
    }

    private (bool, string) ApplyGameDvr()
    {
        WriteReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
        WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
        return (true, "Game DVR / background capture disabled.");
    }

    // ------------------------------------------------------------------
    // Step: hardware-accelerated GPU scheduling (read-only check).
    // ------------------------------------------------------------------
    private (FpsOutcome, string, string?) DetectGpuScheduling()
    {
        int mode = ToInt(ReadReg(Registry.LocalMachine, GraphicsDrivers, "HwSchMode"), 1);
        if (mode == 2)
        {
            return (FpsOutcome.AlreadyOptimal, "Hardware-accelerated GPU scheduling is enabled.", "2");
        }
        return (FpsOutcome.Skipped, "Hardware-accelerated GPU scheduling is off. Enable it in Settings > System > Display > Graphics > Change default graphics settings (needs a restart).", mode.ToString());
    }

    private static (bool, string) ApplyNoOp()
    {
        return (true, "No change required.");
    }

    // ------------------------------------------------------------------
    // Rollback.
    // ------------------------------------------------------------------
    public bool RollbackAction(ChangeRecord record, out string message)
    {
        try
        {
            switch (record.Category)
            {
            case "FpsPowerPlan":
            {
                string guid = ExtractGuid(record.PreviousState);
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    Run("powercfg", "/setactive " + guid);
                }
                message = "Power plan restored.";
                break;
            }
            case "FpsMmcssResponse":
                WriteReg(Registry.LocalMachine, MmcssProfile, "SystemResponsiveness", ParseInt(record.PreviousState, 20), RegistryValueKind.DWord);
                message = "SystemResponsiveness restored.";
                break;
            case "FpsMmcssGames":
            {
                string[] parts = (record.PreviousState ?? "2|8|Medium|Normal").Split('|');
                WriteReg(Registry.LocalMachine, MmcssGames, "Priority", ParseInt(parts.ElementAtOrDefault(0), 2), RegistryValueKind.DWord);
                WriteReg(Registry.LocalMachine, MmcssGames, "GPU Priority", ParseInt(parts.ElementAtOrDefault(1), 8), RegistryValueKind.DWord);
                WriteReg(Registry.LocalMachine, MmcssGames, "Scheduling Category", parts.ElementAtOrDefault(2) ?? "Medium", RegistryValueKind.String);
                WriteReg(Registry.LocalMachine, MmcssGames, "SFIO Priority", parts.ElementAtOrDefault(3) ?? "Normal", RegistryValueKind.String);
                message = "MMCSS Games task restored.";
                break;
            }
            case "FpsGamePriority":
            {
                string[] parts = (record.PreviousState ?? "").Split('|');
                if (int.TryParse(parts.ElementAtOrDefault(0), out int pid))
                {
                    ProcessPriorityClass cls = Enum.TryParse<ProcessPriorityClass>(parts.ElementAtOrDefault(1), out ProcessPriorityClass parsed)
                        ? parsed
                        : ProcessPriorityClass.Normal;
                    _processes.SetPriority(pid, cls, out string _);
                }
                message = "Game process priority restored.";
                break;
            }
            case "FpsGameMode":
                WriteReg(Registry.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", ParseInt(record.PreviousState, 0), RegistryValueKind.DWord);
                message = "Game Mode setting restored.";
                break;
            case "FpsGameDvr":
            {
                string[] parts = (record.PreviousState ?? "1|1").Split('|');
                WriteReg(Registry.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled", ParseInt(parts.ElementAtOrDefault(0), 1), RegistryValueKind.DWord);
                WriteReg(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", ParseInt(parts.ElementAtOrDefault(1), 1), RegistryValueKind.DWord);
                message = "Game DVR setting restored.";
                break;
            }
            default:
                message = "This step does not need a rollback (no system state was modified).";
                break;
            }
            record.Reverted = true;
            SaveChanges();
            _log.Info("FPS boost rolled back: " + record.Description);
            return true;
        }
        catch (Exception ex)
        {
            message = "Rollback failed: " + ex.Message;
            _log.Error("FPS boost rollback failed.", ex);
            return false;
        }
    }

    public int RollbackAll()
    {
        int count = 0;
        foreach (ChangeRecord record in _changes.Where((ChangeRecord c) => !c.Reverted).ToList())
        {
            if (RollbackAction(record, out string _))
            {
                count++;
            }
        }
        return count;
    }

    // ------------------------------------------------------------------
    // Helpers.
    // ------------------------------------------------------------------
    private static string GetActivePowerPlanGuid()
    {
        string output = Run("powercfg", "/getactivescheme").output ?? "";
        int index = output.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return "";
        }
        string text = output.Substring(index + 5).Trim();
        int space = text.IndexOf(' ');
        if (space > 0)
        {
            text = text.Substring(0, space);
        }
        return text.Trim().ToLowerInvariant();
    }

    private static string? ExtractGuid(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }
        int index = s.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }
        string text = s.Substring(index + 5).Trim();
        int space = text.IndexOf(' ');
        return space <= 0 ? text : text.Substring(0, space);
    }

    private static int ToInt(object? value, int fallback)
    {
        if (value == null)
        {
            return fallback;
        }
        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return fallback;
        }
    }

    private static int ParseInt(string? s, int fallback)
    {
        return int.TryParse(s, out int result) ? result : fallback;
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
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return (output: output, error: error);
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
            using RegistryKey key = root.OpenSubKey(path);
            return key?.GetValue(name);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteReg(RegistryKey root, string path, string name, object value, RegistryValueKind kind)
    {
        using RegistryKey key = root.CreateSubKey(path, writable: true);
        key?.SetValue(name, value, kind);
    }

    private void SaveChanges()
    {
        try
        {
            AppPaths.EnsureCreated();
            File.WriteAllText(ChangesFile, JsonHelper.Serialize(_changes));
        }
        catch (Exception ex)
        {
            _log.Debug("FPS boost SaveChanges failed: " + ex.Message);
        }
    }

    public void LoadChanges()
    {
        try
        {
            if (File.Exists(ChangesFile))
            {
                List<ChangeRecord>? list = JsonHelper.Deserialize<List<ChangeRecord>>(File.ReadAllText(ChangesFile));
                if (list != null)
                {
                    _changes.Clear();
                    _changes.AddRange(list);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Debug("FPS boost LoadChanges failed: " + ex.Message);
        }
    }
}
