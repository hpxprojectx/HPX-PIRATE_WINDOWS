using System;
using System.Collections.Generic;
using HPXPirate.Interop;
using HPXPirate.Models;

namespace HPXPirate.Services;

/// <summary>
/// Real Windows mouse tuning built on SystemParametersInfo. Every value is read
/// back from the OS <b>before</b> it is written, so a setting that is already
/// correct is reported as "Already optimal" and left completely untouched. This is
/// the same honest detect-then-apply model used by the system optimizer, and it is
/// what stops the app from "fixing" settings that were never broken.
///
/// All SPI actions, constants and parameter semantics below were verified against
/// the Microsoft Learn documentation for SystemParametersInfoW (winuser.h).
/// </summary>
public sealed class MouseTuningService
{
    private const uint Flags = NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE;

    private readonly ILogger _log;

    private MouseLiveState? _snapshot;

    public MouseTuningService(ILogger log)
    {
        _log = log;
    }

    /// <summary>The live state captured just before the last Apply, used for Revert.</summary>
    public MouseLiveState? LastSnapshot => _snapshot;

    /// <summary>Reads the current Windows mouse settings. Never writes anything.</summary>
    public MouseLiveState ReadLive()
    {
        MouseLiveState state = new MouseLiveState();
        try
        {
            GetMouseParams(out int _, out int _, out int accel);
            state.AccelerationEnabled = accel != 0;
            state.PointerSpeed = GetMouseSpeed();
            state.ScrollLines = GetScrollLines();
            state.DoubleClickMs = GetDoubleClickMs();
            state.ButtonsSwapped = GetSwapButtons();
            state.SnapToDefault = GetSnapToDefault();
            state.ClickLock = GetClickLock();
            state.PointerTrails = GetTrails();
        }
        catch (Exception ex)
        {
            _log.Error("Reading live mouse state failed.", ex);
        }
        return state;
    }

    /// <summary>
    /// Applies the requested gaming mouse profile. Only values that differ from the
    /// current OS state are written; everything else is reported as already optimal.
    /// </summary>
    public List<TuningStep> Apply(MouseTuningSettings s)
    {
        // A checked "disable acceleration" means the target is acceleration OFF;
        // unchecked means the target is the Windows default (acceleration ON).
        return ApplyCore(
            accelOn: !s.DisableAcceleration,
            pointerSpeed: s.PointerSpeed,
            scrollLines: s.ScrollLines,
            doubleClickMs: s.DoubleClickSpeed,
            swapButtons: s.SwapButtons,
            snap: s.SnapToDefault,
            clickLock: s.ClickLock,
            trails: s.PointerTrails);
    }

    /// <summary>Restores the Windows default mouse values (only where they differ).</summary>
    public List<TuningStep> RestoreDefaults()
    {
        return ApplyCore(
            accelOn: true,
            pointerSpeed: 10,
            scrollLines: 3,
            doubleClickMs: 500,
            swapButtons: false,
            snap: false,
            clickLock: false,
            trails: 0);
    }

    /// <summary>Reverts to the state captured before the last Apply.</summary>
    public List<TuningStep> Revert()
    {
        if (_snapshot == null)
        {
            return new List<TuningStep>();
        }
        return ApplyCore(
            accelOn: _snapshot.AccelerationEnabled,
            pointerSpeed: _snapshot.PointerSpeed,
            scrollLines: _snapshot.ScrollLines,
            doubleClickMs: _snapshot.DoubleClickMs,
            swapButtons: _snapshot.ButtonsSwapped,
            snap: _snapshot.SnapToDefault,
            clickLock: _snapshot.ClickLock,
            trails: _snapshot.PointerTrails);
    }

    private List<TuningStep> ApplyCore(bool accelOn, int pointerSpeed, int scrollLines, int doubleClickMs, bool swapButtons, bool snap, bool clickLock, int trails)
    {
        _snapshot = ReadLive();
        List<TuningStep> steps = new List<TuningStep>();

        // 1. Pointer acceleration ("Enhance pointer precision").
        try
        {
            GetMouseParams(out int t1, out int t2, out int accel);
            bool curOn = accel != 0;
            bool already = curOn == accelOn;
            bool ok = already || SetMouseParams(t1, t2, accelOn ? 1 : 0);
            steps.Add(Make("Enhance pointer precision (acceleration)", already, ok,
                already
                    ? $"Already {(curOn ? "on" : "off")} \u2014 nothing to change."
                    : (ok ? (accelOn ? "Enabled." : "Disabled for raw, consistent aim.") : "Could not change acceleration."),
                curOn ? "On" : "Off", accelOn ? "On" : "Off"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Enhance pointer precision (acceleration)", ex));
        }

        // 2. Pointer speed (1-20).
        try
        {
            int cur = GetMouseSpeed();
            int desired = Math.Clamp(pointerSpeed, 1, 20);
            bool already = cur == desired;
            bool ok = already || SetMouseSpeed(desired);
            steps.Add(Make("Pointer speed", already, ok,
                already ? $"Already {cur}/20 \u2014 nothing to change." : (ok ? $"Set to {desired}/20." : "Could not set pointer speed."),
                $"{cur}/20", $"{desired}/20"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Pointer speed", ex));
        }

        // 3. Wheel scroll lines.
        try
        {
            int cur = GetScrollLines();
            int desired = scrollLines <= 0 ? -1 : Math.Clamp(scrollLines, 1, 100);
            bool already = cur == desired;
            bool ok = already || SetScrollLines(desired);
            steps.Add(Make("Wheel scroll lines", already, ok,
                already ? $"Already {DescribeScroll(cur)} \u2014 nothing to change." : (ok ? $"Set to {DescribeScroll(desired)}." : "Could not set scroll lines."),
                DescribeScroll(cur), DescribeScroll(desired)));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Wheel scroll lines", ex));
        }

        // 4. Double-click speed.
        try
        {
            int cur = GetDoubleClickMs();
            int desired = Math.Clamp(doubleClickMs, 200, 5000);
            bool already = cur == desired;
            bool ok = already || SetDoubleClickMs(desired);
            steps.Add(Make("Double-click speed", already, ok,
                already ? $"Already {cur} ms \u2014 nothing to change." : (ok ? $"Set to {desired} ms." : "Could not set double-click speed."),
                $"{cur} ms", $"{desired} ms"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Double-click speed", ex));
        }

        // 5. Primary button assignment.
        try
        {
            bool cur = GetSwapButtons();
            bool already = cur == swapButtons;
            bool ok = already || SetSwapButtons(swapButtons);
            steps.Add(Make("Primary mouse button", already, ok,
                already ? $"Already {(cur ? "swapped" : "right-handed")} \u2014 nothing to change." : (ok ? "Button assignment updated." : "Could not change button assignment."),
                cur ? "Swapped" : "Right-handed", swapButtons ? "Swapped" : "Right-handed"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Primary mouse button", ex));
        }

        // 6. Snap to default button.
        try
        {
            bool cur = GetSnapToDefault();
            bool already = cur == snap;
            bool ok = already || SetSnapToDefault(snap);
            steps.Add(Make("Snap to default button", already, ok,
                already ? "Already in the requested state \u2014 nothing to change." : (ok ? "Updated." : "Could not change snap-to-default."),
                cur ? "On" : "Off", snap ? "On" : "Off"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Snap to default button", ex));
        }

        // 7. ClickLock.
        try
        {
            bool cur = GetClickLock();
            bool already = cur == clickLock;
            bool ok = already || SetClickLock(clickLock);
            steps.Add(Make("ClickLock", already, ok,
                already ? "Already in the requested state \u2014 nothing to change." : (ok ? "Updated." : "Could not change ClickLock."),
                cur ? "On" : "Off", clickLock ? "On" : "Off"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("ClickLock", ex));
        }

        // 8. Pointer trails.
        try
        {
            int cur = GetTrails();
            int desired = Math.Clamp(trails, 0, 32);
            bool curOff = cur <= 1;
            bool desiredOff = desired <= 1;
            bool already = (curOff && desiredOff) || (!curOff && !desiredOff && cur == desired);
            bool ok = already || SetTrails(desired);
            steps.Add(Make("Pointer trails", already, ok,
                already ? "Already in the requested state \u2014 nothing to change." : (ok ? "Updated." : "Could not change pointer trails."),
                curOff ? "Off" : cur.ToString(), desiredOff ? "Off" : desired.ToString()));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Pointer trails", ex));
        }

        return steps;
    }

    // ------------------------------------------------------------------
    // Step helpers.
    // ------------------------------------------------------------------
    private static TuningStep Make(string label, bool alreadyOptimal, bool success, string detail, string? previous, string? next)
    {
        return new TuningStep
        {
            Label = label,
            AlreadyOptimal = alreadyOptimal,
            Changed = success && !alreadyOptimal,
            Success = success,
            Status = alreadyOptimal ? "Already optimal" : (success ? "Applied" : "Failed"),
            Detail = detail,
            PreviousState = previous,
            NewState = next
        };
    }

    private static TuningStep Failed(string label, Exception ex)
    {
        return new TuningStep
        {
            Label = label,
            Success = false,
            Status = "Failed",
            Detail = ex.Message
        };
    }

    private static string DescribeScroll(int lines)
    {
        return lines <= 0 ? "one page" : $"{lines} line(s)";
    }

    // ------------------------------------------------------------------
    // Raw SystemParametersInfo wrappers.
    // ------------------------------------------------------------------
    private static void GetMouseParams(out int threshold1, out int threshold2, out int acceleration)
    {
        int[] values = new int[3];
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSE, 0u, values, 0u);
        threshold1 = values[0];
        threshold2 = values[1];
        acceleration = values[2];
    }

    private static bool SetMouseParams(int threshold1, int threshold2, int acceleration)
    {
        int[] values = { threshold1, threshold2, acceleration };
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSE, 0u, values, Flags);
    }

    private static int GetMouseSpeed()
    {
        int value = 10;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSESPEED, 0u, ref value, 0u);
        return value;
    }

    private static bool SetMouseSpeed(int speed)
    {
        // SPI_SETMOUSESPEED takes the value in pvParam (an integer), NOT uiParam.
        int value = Math.Clamp(speed, 1, 20);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSESPEED, 0u, ref value, Flags);
    }

    private static int GetScrollLines()
    {
        int value = 3;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWHEELSCROLLLINES, 0u, ref value, 0u);
        return value;
    }

    private static bool SetScrollLines(int lines)
    {
        // uiParam carries the value; WHEEL_PAGESCROLL (UINT_MAX) means "one page".
        uint value = lines <= 0 ? uint.MaxValue : (uint)Math.Clamp(lines, 1, 100);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETWHEELSCROLLLINES, value, IntPtr.Zero, Flags);
    }

    private static int GetDoubleClickMs()
    {
        return (int)NativeMethods.GetDoubleClickTime();
    }

    private static bool SetDoubleClickMs(int ms)
    {
        // uiParam carries the value; the OS caps it at 5000 ms.
        uint value = (uint)Math.Clamp(ms, 200, 5000);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDOUBLECLICKTIME, value, IntPtr.Zero, Flags);
    }

    private static bool GetSwapButtons()
    {
        return NativeMethods.GetSystemMetrics(NativeMethods.SM_SWAPBUTTON) != 0;
    }

    private static bool SetSwapButtons(bool swap)
    {
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSEBUTTONSWAP, swap ? 1u : 0u, IntPtr.Zero, Flags);
    }

    private static bool GetSnapToDefault()
    {
        int value = 0;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSNAPTODEFBUTTON, 0u, ref value, 0u);
        return value != 0;
    }

    private static bool SetSnapToDefault(bool on)
    {
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETSNAPTODEFBUTTON, on ? 1u : 0u, IntPtr.Zero, Flags);
    }

    private static bool GetClickLock()
    {
        int value = 0;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSECLICKLOCK, 0u, ref value, 0u);
        return value != 0;
    }

    private static bool SetClickLock(bool on)
    {
        // SPI_SETMOUSECLICKLOCK takes the BOOL in pvParam, uiParam must be 0.
        int value = on ? 1 : 0;
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSECLICKLOCK, 0u, ref value, Flags);
    }

    private static int GetTrails()
    {
        int value = 0;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSETRAILS, 0u, ref value, 0u);
        return value;
    }

    private static bool SetTrails(int count)
    {
        // 0 or 1 disables the feature; greater than 1 sets the trail length.
        uint value = count <= 1 ? 0u : (uint)Math.Clamp(count, 2, 32);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSETRAILS, value, IntPtr.Zero, Flags);
    }
}
