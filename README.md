# HPX PIRATE

**Gaming Performance Center for Windows 10 / 11 (x64)**

HPX PIRATE is a lightweight, all-in-one Windows desktop utility that helps you get
**more stable frame times, lower input latency and a cleaner, faster system** — without
ever touching game memory, injecting DLLs, or doing anything that a fair-play policy
would flag.

> **Fair-play first.** HPX PIRATE contains **no** aimbot, **no** head-lock, **no**
> auto-targeting, **no** wallhack, **no** memory ESP, **no** enemy detection, **no** DLL
> injection and **no** anti-cheat bypass. Every feature works only on **your own** input
> and **your own** system. See [Fair-Play & Safety](#fair-play--safety).

> **No .NET install required (v1.1.0+).** The release is **self-contained** — the .NET 8
> runtime is bundled inside the app. It runs on a clean Windows 10 / Windows 11 machine
> with nothing else to install. (See [Requirements](#requirements).)

---

## What's new in 1.1.0

- **Honest optimizer.** The Performance Engine no longer "fixes" things that are already
  optimal. It now runs a **read-only detection pass first** and only changes a setting
  when it is genuinely non-optimal. Every step reports one of four truthful states:
  **Applied**, **Already optimal**, **Skipped** or **Failed** — and a restore point is
  created only when something actually changes. (This fixes the "it says it fixed stuff
  even when there was no bug" complaint.)
- **New module — Mouse Tuning.** Real Windows mouse settings read/write via
  `SystemParametersInfo`: pointer speed, acceleration (Enhance Pointer Precision),
  double-click speed, scroll lines, button swap, click-lock, snap-to-default, mouse
  trails, plus live values and one-click **Restore Defaults** / **Undo**.
- **New module — Keyboard Tuning.** Real Windows keyboard settings: repeat delay, repeat
  rate, and the accessibility toggles (Sticky Keys, Filter Keys, Toggle Keys) — with live
  values and full **Undo**.
- **New module — FPS Boost.** A real, reversible FPS booster with a **live before/after
  measurement** taken from the app's own ETW FPS monitor (not a fake counter). Steps
  include the Ultimate Performance power plan, MMCSS `SystemResponsiveness`, game-process
  priority, Windows Game Mode, Game DVR, and Hardware-accelerated GPU scheduling. Each
  step is honest about whether it was needed and can be rolled back.
- **Self-contained packaging** so the app runs on Windows 10 with no .NET prerequisite.

All existing features and the UI are unchanged.

---

## Table of contents

- [Feature overview](#feature-overview)
- [Screens / modules](#screens--modules)
- [Fair-Play & Safety](#fair-play--safety)
- [Architecture](#architecture)
- [Requirements](#requirements)
- [Quick start (end users)](#quick-start-end-users)
- [Build from source](#build-from-source)
- [Create the installer](#create-the-installer)
- [Testing](#testing)
- [Configuration & data files](#configuration--data-files)
- [Performance & resource usage](#performance--resource-usage)
- [Troubleshooting](#troubleshooting)
- [Project layout](#project-layout)
- [Roadmap](#roadmap)
- [License](#license)

---

## Feature overview

| Module | What it does |
| --- | --- |
| **HPX PERFORMANCE ENGINE** | Applies reversible, documented Windows tweaks (power plan, Game Mode, Game DVR, visual effects, priority, temp/cache cleanup) with a Restore Point + per-change UNDO. **Detects first, changes only what is non-optimal.** |
| **FPS Monitor** | Real-time FPS, frame time, 1% / 0.1% lows captured via **ETW** (`DxgKrnl` Present events — the PresentMon technique). No injection. |
| **HPX FPS BOOST** | A real, reversible FPS booster. Applies power-plan / MMCSS / priority / Game Mode / Game DVR / GPU-scheduling tweaks and **measures the real before/after FPS** with the built-in ETW monitor. Honest per-step status; full rollback. |
| **HPX ANALYZER** | Turns a capture into a verdict: average / min / max FPS, frame-time consistency, stutter count, and a plain-language summary. |
| **Bottleneck Detection** | Compares CPU vs GPU load & frame time to tell you which component is limiting you. |
| **Game Profile Manager** | Save/load/export/import per-game profiles (optimizer preset + crosshair + mapping + drag settings). Automatic backup before overwrite. |
| **CONTROL MAPPER** | Remap keyboard/mouse buttons with a visual editor; global low-level hooks (`WH_KEYBOARD_LL` / `WH_MOUSE_LL`). |
| **MOUSE TUNING** | Read & set the real Windows mouse parameters (`SystemParametersInfo`): pointer speed, acceleration, double-click speed, scroll lines, button swap, click-lock, snap-to, trails. Live values + Restore Defaults + Undo. |
| **KEYBOARD TUNING** | Read & set the real Windows keyboard parameters: repeat delay, repeat rate, and Sticky/Filter/Toggle Keys. Live values + Undo. |
| **HPX DRAG ASSIST** | Response curves (Linear / Smooth / FastStart / SlowStart / Precision / Custom), deadzone, EMA smoothing and a micro-movement stabiliser. |
| **HPX EASY DRAG** | A dedicated lab that makes mouse **dragging** feel lighter and more consistent — with **real measured** stability & smoothness scores (not fake numbers). |
| **HPX CROSSHAIR** | A click-through, always-on-top overlay crosshair with shape/colour/size/gap/opacity/offset controls and a global toggle hotkey. |
| **Process Manager** | View processes, set priority, and keep a full change log with one-click rollback. |
| **Real-Time Monitor** | Live graphs for FPS, frame time, CPU, GPU and RAM, plus a before/after benchmark tool. |
| **Settings & Recovery** | Dark mode, start-with-Windows, tray, restore points, backup/restore, safe & diagnostic modes. |

---

## Screens / modules

The left navigation exposes thirteen screens:

1. **Dashboard** – system snapshot, quick actions, live status.
2. **Performance** – the HPX PERFORMANCE ENGINE (tweak catalog, detect-first apply, undo).
3. **Profiles** – Game Profile Manager (create, save, load, export, import, backup).
4. **Control Mapper** – visual keyboard/mouse remapping.
5. **Drag Assist** – response curves and drag tuning.
6. **Easy Drag** – the Easy Drag lab with measured metrics.
7. **Crosshair** – overlay designer with live preview.
8. **Monitor** – FPS monitor, real-time graphs, analyzer and benchmark.
9. **Optimizer** – Process Manager, startup apps, change log & rollback.
10. **Mouse Tuning** – Windows pointer/acceleration/scroll/double-click settings.
11. **Keyboard Tuning** – Windows repeat delay/rate and accessibility toggles.
12. **FPS Boost** – real, reversible FPS boost with measured before/after.
13. **Settings** – general, performance, safety & recovery.

The UI uses a dark, cyberpunk card dashboard (neon-magenta accent, semi-transparent
panels, subtle animations) and a custom chrome window with a matching dark title bar.

---

## Fair-Play & Safety

### Fair-play guarantees (hard-coded design rules)

- **No** aimbot / aim-assist / head-lock / trigger-bot.
- **No** auto-targeting or enemy/player detection of any kind.
- **No** wallhack, **no** memory ESP, **no** reading of game memory.
- **No** DLL injection, **no** hooking of game processes, **no** anti-cheat bypass.
- **Easy Drag / Drag Assist** operate **only** on the user's own mouse input stream
  (smoothing, curves, deadzone). They never look at what is on screen or in memory.

### Safety guarantees (system changes)

- A **System Restore Point** is created before applying optimizations (configurable) —
  and only when a change is actually needed.
- **Every** change is logged with an **UNDO / RESTORE** action.
- The app **shows what it will change before you apply it**, and reports honestly when
  nothing needed changing.
- Only **HKCU** (current-user) registry keys are touched — never machine-wide risky edits
  without a backup.
- **No** system files are ever deleted; **no** Windows services are blindly disabled.
- **No** automatic overclocking.
- Priority order enforced everywhere: **STABILITY → SAFETY → LOW LATENCY → PERFORMANCE → VISUALS**.
- The app never crashes when no game is running — the FPS monitor simply reports "no
  active capture" and the rest of the app keeps working.

---

## Architecture

- **.NET 8 (net8.0-windows), WPF**, MVVM.
- **ViewModels** per module + a shared `AppHost` service container (manual DI).
- **Services** (all in `Services/`): logging, settings, profiles, hardware monitor,
  FPS monitor (ETW), bottleneck analyzer, FPS analyzer, restore points, process manager,
  system optimizer, mouse tuning, keyboard tuning, FPS boost, drag engine, input mapper,
  crosshair overlay, history buffer.
- **Interop** (`Interop/`): P/Invoke signatures (including the full
  `SystemParametersInfo` set) + low-level input hooks on a dedicated STA thread.
- **Persistence**: JSON (`System.Text.Json`, camelCase, enum-as-string) under
  `%APPDATA%\HPX Pirate`.
- **Charts**: a dependency-free custom `LineGraph` `FrameworkElement` (no charting NuGet).

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full breakdown.

---

## Requirements

**End users**

- Windows 10 (1809+) or Windows 11, **64-bit**.
- **No .NET install needed** — the release is **self-contained** (the .NET 8 runtime is
  bundled). Just run the app.
- Administrator rights (needed for restore points and system tuning). The app manifest
  requests elevation.

**Developers / build**

- **.NET SDK 8.0** (builds fine from Linux/macOS thanks to `EnableWindowsTargeting`).
- **Inno Setup 6** (only to build the `.exe` installer).
- Windows is **not** required to *compile*; it is required to *run* the app.

---

## Quick start (end users)

1. Run **`HPX-PIRATE-Setup.exe`** and follow the wizard (accept the UAC prompt).
   There is nothing else to install — the .NET runtime is included.
2. Launch **HPX PIRATE** from the Start menu or desktop.
3. Open **Performance → Apply** to tune Windows (a restore point is created first if
   anything actually needs changing).
4. Open **Monitor**, start a capture, and play your game to see FPS and frame times.
5. Open **FPS Boost** to apply real, reversible FPS tweaks and see the measured
   before/after result.
6. Use **Profiles** to save a per-game setup, and **Settings** to make it start with Windows.

Everything is reversible: use **UNDO** per change, or **UNDO ALL** in the Optimizer.

---

## Build from source

### 1. Restore & build

```bash
# from the repository root
dotnet restore HPX-PIRATE.sln
dotnet build src/HPXPirate/HPXPirate.csproj -c Release
```

> Building on Linux/macOS works because the project sets
> `<EnableWindowsTargeting>true</EnableWindowsTargeting>`. You still need the .NET 8 SDK.

### 2. Run

```bash
dotnet run --project src/HPXPirate/HPXPirate.csproj -c Release
```

(On Windows this launches the WPF window. On Linux it compiles but cannot display WPF.)

### 3. Publish (self-contained, x64 — recommended)

```bash
dotnet publish src/HPXPirate/HPXPirate.csproj -c Release -r win-x64 \
    --self-contained true -o publish
```

Output: `publish/HPXPirate.exe` plus the bundled .NET runtime — the target machine needs
**nothing** installed.

For a **single-file** self-contained build (one big `.exe`):

```bash
dotnet publish src/HPXPirate/HPXPirate.csproj -c Release -r win-x64 \
    --self-contained true -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true -o publish-single
```

For a smaller **framework-dependent** build (requires the .NET 8 Desktop Runtime on the
target machine):

```bash
dotnet publish src/HPXPirate/HPXPirate.csproj -c Release -r win-x64 \
    --self-contained false -o publish-fd
```

See [`BUILD.md`](BUILD.md) for the complete, copy-pasteable instructions including the
installer.

---

## Create the installer

```powershell
# elevated PowerShell, from the repo root
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

This publishes the app (self-contained by default) and compiles
`dist\HPX-PIRATE-Setup.exe` with Inno Setup. The raw Inno Setup script is
[`installer/HPX-PIRATE.iss`](installer/HPX-PIRATE.iss).

---

## Testing

A **headless, dependency-free** logic test suite validates the platform-independent
core (settings persistence, profile CRUD/import/export/backup/restore, drag engine,
FPS analyzer, bottleneck analyzer, history buffer):

```bash
dotnet run --project tests/HPXPirate.LogicTests -c Release
# -> "56 passed, 0 failed"
```

To isolate test data from your real config:

```bash
XDG_CONFIG_HOME=/tmp/hpx_test_config dotnet run --project tests/HPXPirate.LogicTests -c Release
```

Manual smoke tests to run on Windows:

- App starts with no game running (FPS monitor shows "no capture").
- Change a setting → close → reopen → setting persisted.
- Create a profile → save → load → values restored.
- Apply one optimization → UNDO restores the previous state.
- Apply an optimization on an already-optimal system → it reports "Already optimal" and
  changes nothing.
- Toggle the crosshair overlay; it stays click-through and always-on-top.
- Mouse Tuning / Keyboard Tuning → Apply → Undo restores the original Windows values.

---

## Configuration & data files

All user data lives under **`%APPDATA%\HPX Pirate`**:

```
%APPDATA%\HPX Pirate\
├── settings.json            # app settings
├── fpsboost.json            # FPS Boost change log (for rollback)
├── logs\                    # rolling log files
├── profiles\                # *.json game profiles
│   └── backups\             # automatic profile backups
└── history\                 # optimization change log (for rollback)
```

Logs are plain text and safe to share when reporting issues.

---

## Performance & resource usage

- **Idle**: monitoring interval automatically relaxes (see *ReduceMonitoringWhenIdle*)
  so the app stays light on CPU/RAM when you are not gaming.
- **No background polling of game memory** — FPS is captured from OS-level ETW events
  only while a capture is active.
- Hardware sensors (LibreHardwareMonitor) degrade gracefully: if a sensor is
  unavailable, the UI shows "n/a" instead of failing.

> We deliberately avoid marketing claims like "2× FPS" or "100% anti-drop". HPX PIRATE
> reports **measured** numbers only.

---

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| App won't start on a fresh Windows 10 | Use the **self-contained** build (v1.1.0+). If you have an older framework-dependent build, install the **.NET 8 Desktop Runtime (x64)**. |
| No FPS shown | Start a capture in **Monitor** *while* the game is running; some anti-cheat-protected games block ETW providers. |
| Temps/load show "n/a" | Your hardware/sensors may need a newer LibreHardwareMonitor; run as admin. |
| Optimizations not applying | Make sure you accepted the UAC prompt (the app needs admin). |
| Optimizer says "Already optimal" | That is correct — nothing needed changing, so nothing was modified. |
| Want to revert everything | **Optimizer → UNDO ALL**, **FPS Boost → Roll back all**, or use the Windows System Restore point created before changes. |

Logs: `%APPDATA%\HPX Pirate\logs\`.

---

## Project layout

```
HPX-PIRATE.sln
src/HPXPirate/
├── App.xaml(.cs)              # startup, crash protection
├── MainWindow.xaml(.cs)       # shell + navigation + tray
├── Themes/                    # Colors.xaml, Controls.xaml (dark cyberpunk)
├── Models/                    # Settings, GameProfile, Optimization, PerformanceMetrics,
│                              #   TuningStep, MouseLiveState, KeyboardLiveState, ...
├── Services/                  # all engine services (see Architecture)
├── Interop/                   # P/Invoke + input hooks
├── ViewModels/                # one per module
├── Views/                     # one XAML view per module + overlay window
├── Controls/LineGraph.cs      # dependency-free chart
└── Converters/                # value converters
tests/HPXPirate.LogicTests/    # headless logic tests
installer/                     # Inno Setup script + build helper
docs/                          # ARCHITECTURE.md
```

---

## Roadmap

- Per-monitor crosshair offsets.
- More response-curve presets and importable curve files.
- Optional CSV export of monitor sessions.
- Localisation (English / Indonesian) for the UI strings.

---

## License

Released under the **MIT License** — see [`LICENSE`](LICENSE).

HPX PIRATE is an independent utility and is **not affiliated with, endorsed by, or
sponsored by** any game publisher. Use it responsibly and always respect the terms of
service of the games you play.
