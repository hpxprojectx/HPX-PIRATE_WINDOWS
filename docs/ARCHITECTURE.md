# HPX PIRATE — Architecture

This document describes how HPX PIRATE is put together so a new contributor can be
productive quickly.

---

## 1. High-level shape

```
┌─────────────────────────────────────────────────────────────┐
│                         App (WPF)                           │
│   OnStartup → AppHost.Initialize() → MainWindow.Show()      │
│   Crash guards: Dispatcher / AppDomain / TaskScheduler      │
└──────────────────────────────▲──────────────────────────────┘
                               │ DataContext / App.Host
┌──────────────────────────────┴──────────────────────────────┐
│                        MainWindow (shell)                   │
│   Custom chrome · left navigation · tray icon · content     │
│   Nav tag → View (Dashboard, Performance, …)                │
└──────────────────────────────▲──────────────────────────────┘
                               │ bindings
┌──────────────────────────────┴──────────────────────────────┐
│                        ViewModels (MVVM)                    │
│   ViewModelBase (INotifyPropertyChanged) · RelayCommand     │
└──────────────────────────────▲──────────────────────────────┘
                               │ method calls / events
┌──────────────────────────────┴──────────────────────────────┐
│                     Services (AppHost)                      │
│   logging · settings · profiles · hardware · fps(ETW) ·     │
│   analyzer · optimizer · restore · processes · startup ·    │
│   input mapper · crosshair · drag engines · monitor ·       │
│   mouse tuning · keyboard tuning · fps boost                │
└──────────────────────────────▲──────────────────────────────┘
                               │ P/Invoke / WMI / ETW / registry
┌──────────────────────────────┴──────────────────────────────┐
│                     Windows OS (Win32)                      │
└─────────────────────────────────────────────────────────────┘
```

**Design rules**

1. **MVVM.** Views bind to ViewModels; ViewModels talk to Services; Services talk to the OS.
2. **One composition root.** `AppHost` owns every service (manual DI, no container library).
3. **No blocking UI.** Long work runs on background threads; results marshal back via
   `Dispatcher`.
4. **Fail soft.** Any hardware/OS feature that is unavailable degrades to a no-op or "n/a"
   instead of throwing.
5. **Reversible by construction.** Anything that mutates the system records a change that
   can be undone.
6. **Detect first, change last.** Mutation services run a read-only detection pass and
   only touch settings that are genuinely non-optimal, reporting an honest status.

---

## 2. Composition root — `AppHost`

`AppHost.Initialize()` creates services in dependency order:

| Service | Responsibility |
| --- | --- |
| `LoggingService` | Rolling file log under `%APPDATA%\HPX Pirate\logs`. |
| `SettingsService` | Loads/saves `settings.json`; exposes `Current` (`AppSettings`). |
| `ProfileManager` | CRUD + import/export/backup for game profiles. |
| `HardwareMonitorService` | CPU/GPU/RAM/disk/network via LibreHardwareMonitor + PerformanceCounter + WMI + `GlobalMemoryStatusEx`. |
| `FpsMonitorService` | ETW capture of `DxgKrnl` Present events → FPS / frame time. |
| `BottleneckAnalyzer` | CPU-vs-GPU verdict from load + frame time. |
| `ProcessManagerService` | Enumerate processes, set priority, remember original priority. |
| `RestorePointService` | Create System Restore points via WMI `SystemRestore`. |
| `SystemOptimizerService` | Tweak catalog, **detect-first** apply/undo, change log. |
| `StartupManagerService` | Read/observe startup entries. |
| `InputMapperService` | Low-level keyboard/mouse hooks + remap table. |
| `CrosshairOverlayService` | Owns the click-through overlay window + global hotkey. |
| `EasyDragEngine` / `DragAssistEngine` | Response curves, deadzone, EMA smoothing, measured metrics. |
| `MonitorService` | Aggregates FPS + hardware into a live sample stream for the UI. |
| `MouseTuningService` | Read/write real Windows mouse parameters via `SystemParametersInfo`; snapshot + rollback. |
| `KeyboardTuningService` | Read/write real Windows keyboard parameters + accessibility toggles; snapshot + rollback. |
| `FpsBoostService` | Reversible FPS-boost plan (power plan, MMCSS, priority, Game Mode, Game DVR, GPU scheduling) with real before/after ETW measurement. |

`AppHost.Shutdown()` disposes the monitor, crosshair, input hooks and saves settings.

---

## 3. Threading model

| Concern | Thread |
| --- | --- |
| UI | WPF dispatcher thread. |
| Input hooks | A **dedicated STA thread** with its own message pump (`InputHook`). |
| Hardware polling | Background timer/task; results posted to the dispatcher. |
| ETW FPS capture | Background ETW session thread. |
| Overlay rendering | WPF dispatcher (overlay is a WPF `Window`). |
| Tuning apply/rollback | `Task.Run` background work, marshalled back to the UI. |

Hooks and hotkeys must live on a thread with a running message loop — that is why
`InputMapperService`/`CrosshairOverlayService` spin up their own STA threads rather than
using the UI thread.

---

## 4. The FPS monitor (non-injection)

HPX PIRATE uses the same class of technique as PresentMon:

1. Start an ETW session with the **Microsoft-Windows-DxgKrnl** provider.
2. Listen for `Present` / `PresentHistory` events emitted by the GPU scheduler.
3. Convert present timestamps into instantaneous FPS and frame time.
4. Feed a `HistoryBuffer` for 1% / 0.1% low calculation and the live `LineGraph`.

Because it reads **OS-level telemetry** and never touches the game process, there is no
injection and nothing for an anti-cheat to flag. Some protected titles simply do not emit
these events; in that case the monitor reports "no capture" and the rest of the app works.

`FpsAnalyzer` turns a finished capture into: average / min / max FPS, frame-time
consistency, stutter count, and a human-readable verdict.

---

## 5. The optimizer (safe, reversible, and honest)

`SystemOptimizerService` holds a catalog of `Optimization` items. Each item declares:

- a human-readable description and category,
- the **current** and **target** state,
- an `Apply()` and `Undo()` action,
- whether it needs admin / a restore point.

The apply flow is **two-phase** so the app never claims to fix something that was already
fine:

```
Phase 1 — DETECT (read-only)
    for each item: read the live value, compare to target
        → NeedsChange | AlreadyOptimal | Skipped | Failed
Phase 2 — APPLY (only items marked NeedsChange)
    create Restore Point (only if at least one item needs change)
    apply each NeedsChange item, record OptimizationChange, log
```

Each step reports one of four honest outcomes:

| Outcome | Meaning |
| --- | --- |
| **Applied** | The setting was non-optimal and has now been changed. |
| **Already optimal** | The setting was already correct — nothing was touched. |
| **Skipped** | Not applicable on this machine (e.g. unsupported), so it was left alone. |
| **Failed** | The change could not be applied (logged with the reason). |

The summary is truthful, e.g. *"Nothing needed fixing — your system was already optimal."*
Only **HKCU** keys are modified. No system files are deleted. No Windows services are
blindly disabled. The full change log is persisted so rollback survives restarts.

---

## 6. Mouse & keyboard tuning (real Windows parameters)

Both services use the Win32 **`SystemParametersInfo`** API (user32) with
`SPIF_UPDATEINIFILE | SPIF_SENDCHANGE`, so the changes are the same ones the Windows
Control Panel would make — fully real, and fully reversible.

**Mouse** (`MouseTuningService`):

| Setting | SPI |
| --- | --- |
| Pointer speed (1–20) | `SPI_GET/SETMOUSESPEED` |
| Enhance Pointer Precision (acceleration) | `SPI_GET/SETMOUSE` (3-int array) |
| Double-click speed | `SPI_GET/SETDOUBLECLICKTIME` |
| Scroll lines per notch | `SPI_GET/SETWHEELSCROLLLINES` |
| Swap primary/secondary buttons | `SPI_SETMOUSEBUTTONSWAP` + `SM_SWAPBUTTON` |
| ClickLock | `SPI_GET/SETMOUSECLICKLOCK` |
| Snap to default button | `SPI_GET/SETSNAPTODEFBUTTON` |
| Mouse trails | `SPI_GET/SETMOUSETRAILS` |

**Keyboard** (`KeyboardTuningService`):

| Setting | SPI |
| --- | --- |
| Repeat delay (0–3) | `SPI_GET/SETKEYBOARDDELAY` |
| Repeat rate (0–31) | `SPI_GET/SETKEYBOARDSPEED` |
| Sticky Keys | `SPI_GET/SETSTICKYKEYS` |
| Filter Keys | `SPI_GET/SETFILTERKEYS` |
| Toggle Keys | `SPI_GET/SETTOGGLEKEYS` |

Both follow the same pattern as the optimizer: **read live → detect → apply only what
differs → snapshot for rollback**. `Revert()` restores the exact values captured before
the last `Apply()`, and `RestoreDefaults()` returns the Windows defaults. Accessibility
toggles are only ever turned **off** (never silently enabled). Each step surfaces a
`TuningStep` with `Changed` / `AlreadyOptimal` / `Success` / `Status` / `Detail`, so the
UI can show honest results.

---

## 7. FPS Boost (real, reversible, measured)

`FpsBoostService` builds a plan of reversible steps and — crucially — **measures the real
FPS before and after** using the app's own ETW monitor, so the result is never a fake
number.

Plan steps (each is detect-first, so an already-optimal machine is left alone):

1. Switch to the **Ultimate Performance** power plan.
2. Set MMCSS `SystemResponsiveness` to 10 (favours games over background audio).
3. Set the **Games** MMCSS task priority.
4. Raise the **game process priority** to High.
5. Enable **Windows Game Mode**.
6. Disable **Game DVR / Game Bar capture**.
7. Hardware-accelerated GPU scheduling (read-only → reported as `Skipped` when it cannot
   be changed programmatically).

Every step yields an `FpsOutcome` (`NeedsChange` / `AlreadyOptimal` / `Skipped` /
`Failed`) and a `ChangeRecord` that can be rolled back individually or all at once
(`RollbackAction`, `RollbackAll`). The change log persists to
`%APPDATA%\HPX Pirate\fpsboost.json`.

The ViewModel (`FpsBoostViewModel`) runs a short measurement window, captures
`FpsBefore`, applies the plan, re-measures, and calls `CompleteMeasurement()` — the UI then
shows the genuine before/after delta.

---

## 8. Input mapping & drag engines

- **Hooks**: `SetWindowsHookEx(WH_KEYBOARD_LL / WH_MOUSE_LL)` on the dedicated STA thread.
- **Remap table**: source → target key/button, with per-profile overrides.
- **Drag engines**: apply a response curve to the *user's own* mouse deltas:

```
raw delta → deadzone → response curve → EMA smoothing → micro-movement stabiliser → output
```

Metrics (stability, smoothness) are **measured** from the actual input stream
(variance of deltas, jitter, etc.) — never fabricated. The engines have no knowledge of
the screen, the game, or any target; they only reshape the user's own input.

---

## 9. Crosshair overlay

A borderless WPF `Window` with extended styles:

```
WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
```

- **Transparent** → clicks pass through to the game.
- **ToolWindow** → no taskbar entry.
- **NoActivate** → never steals focus.
- Toggled by a global hotkey registered with `RegisterHotKey`.

Rendering (shape, colour, size, thickness, gap, opacity, offset, centre dot, outline) is
done in `CrosshairOverlayWindow.Render()` and mirrored in the designer preview.

---

## 10. Persistence

- Format: **JSON** (`System.Text.Json`, camelCase, `JsonStringEnumConverter`).
- Location: `%APPDATA%\HPX Pirate`.
- `JsonHelper` centralises (de)serialisation + atomic writes.
- Profiles are backed up to `profiles\backups\` before being overwritten.
- Optimizer changes → `history\`; FPS Boost changes → `fpsboost.json`.

---

## 11. UI / theming

- `Themes/Colors.xaml` — palette (dark base, neon-magenta `#FF2BD6` accent, metallic
  silver). `Themes/Controls.xaml` — control templates (buttons, cards, sliders, toggles,
  nav buttons).
- Custom chrome via `WindowChrome` + `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)`;
  `WM_GETMINMAXINFO` is handled so maximise respects the custom frame. The dark-titlebar
  attribute is applied with a **Windows 11 attribute id (20)** and a **Windows 10 fallback
  (19)**, each guarded by try/catch, so the app looks correct on both.
- `Controls/LineGraph.cs` — a dependency-free real-time chart `FrameworkElement`.

---

## 12. Testing strategy

- **Headless logic tests** (`tests/HPXPirate.LogicTests`) compile only the
  platform-independent files and exercise settings, profiles, drag engines, analyzers and
  the history buffer. They run anywhere .NET runs.
- **Manual smoke tests** on Windows cover startup, persistence, profile IO, keymapping,
  monitoring, mouse/keyboard tuning and rollback (see README → Testing).

---

## 13. Packaging

- The shipped build is **self-contained** (`dotnet publish --self-contained true -r win-x64`),
  so the .NET 8 runtime travels inside the app and the target machine needs nothing
  installed. `HPXPirate.runtimeconfig.json` lists the runtime under `includedFrameworks`
  (bundled) rather than `frameworks` (external).
- The ETW monitor ships native helpers (`amd64/KernelTraceControl.dll`, `msdia140.dll`)
  that are copied next to the exe.
- The Inno Setup script (`installer/HPX-PIRATE.iss`) packages the self-contained `publish`
  folder and no longer requires a separate .NET runtime installer.

---

## 14. Extending

- **New tweak**: add an `Optimization` to the catalog with `Apply`/`Undo`; it automatically
  appears in the UI, the change log and rollback.
- **New tuning step**: add a detect+apply+restore triple to `MouseTuningService` /
  `KeyboardTuningService`; it shows up in the corresponding view with honest status.
- **New FPS-boost step**: add a `StepDef` to `FpsBoostService.BuildPlan()` with its
  detect/apply/rollback actions.
- **New module**: add a ViewModel + View, register the nav button in `MainWindow.xaml`,
  and (if it needs a service) wire it into `AppHost`.
- **New curve**: extend the response-curve enum and the `DragEngine` evaluation switch.
