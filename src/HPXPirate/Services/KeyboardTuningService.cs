using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HPXPirate.Interop;
using HPXPirate.Models;

namespace HPXPirate.Services;

/// <summary>
/// Real Windows keyboard tuning built on SystemParametersInfo. As with the mouse
/// service, every value is read from the OS first and only written when it differs,
/// so healthy settings are reported as "Already optimal" and never touched.
///
/// Accessibility features (StickyKeys / FilterKeys / ToggleKeys) are only ever
/// turned <b>off</b> when the matching "disable" option is checked \u2014 the app never
/// switches an accessibility feature on behind the user's back.
/// </summary>
public sealed class KeyboardTuningService
{
    private const uint Flags = NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE;

    private readonly ILogger _log;

    private KeyboardLiveState? _snapshot;

    public KeyboardTuningService(ILogger log)
    {
        _log = log;
    }

    /// <summary>The live state captured just before the last Apply, used for Revert.</summary>
    public KeyboardLiveState? LastSnapshot => _snapshot;

    /// <summary>Reads the current Windows keyboard settings. Never writes anything.</summary>
    public KeyboardLiveState ReadLive()
    {
        KeyboardLiveState state = new KeyboardLiveState();
        try
        {
            state.RepeatDelay = GetKeyboardDelay();
            state.RepeatRate = GetKeyboardSpeed();
            state.StickyKeysOn = GetStickyKeys();
            state.FilterKeysOn = GetFilterKeys();
            state.ToggleKeysOn = GetToggleKeys();
        }
        catch (Exception ex)
        {
            _log.Error("Reading live keyboard state failed.", ex);
        }
        return state;
    }

    /// <summary>Applies the requested gaming keyboard profile (only where it differs).</summary>
    public List<TuningStep> Apply(KeyboardTuningSettings s)
    {
        _snapshot = ReadLive();
        List<TuningStep> steps = new List<TuningStep>();

        // 1. Key repeat delay (0 = shortest, 3 = longest).
        try
        {
            int cur = GetKeyboardDelay();
            int desired = Math.Clamp(s.RepeatDelay, 0, 3);
            bool already = cur == desired;
            bool ok = already || SetKeyboardDelay(desired);
            steps.Add(Make("Key repeat delay", already, ok,
                already ? $"Already {cur}/3 \u2014 nothing to change." : (ok ? $"Set to {desired}/3 (shorter = snappier)." : "Could not set repeat delay."),
                $"{cur}/3", $"{desired}/3"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Key repeat delay", ex));
        }

        // 2. Key repeat rate (0 = slowest, 31 = fastest).
        try
        {
            int cur = GetKeyboardSpeed();
            int desired = Math.Clamp(s.RepeatRate, 0, 31);
            bool already = cur == desired;
            bool ok = already || SetKeyboardSpeed(desired);
            steps.Add(Make("Key repeat rate", already, ok,
                already ? $"Already {cur}/31 \u2014 nothing to change." : (ok ? $"Set to {desired}/31 (fastest)." : "Could not set repeat rate."),
                $"{cur}/31", $"{desired}/31"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Key repeat rate", ex));
        }

        // 3-5. Accessibility toggles \u2014 only ever turned off.
        steps.Add(HandleToggle("Sticky Keys", s.DisableStickyKeys, GetStickyKeys, SetStickyKeys));
        steps.Add(HandleToggle("Filter Keys", s.DisableFilterKeys, GetFilterKeys, SetFilterKeys));
        steps.Add(HandleToggle("Toggle Keys", s.DisableToggleKeys, GetToggleKeys, SetToggleKeys));

        return steps;
    }

    /// <summary>Restores the Windows default keyboard values (only where they differ).</summary>
    public List<TuningStep> RestoreDefaults()
    {
        return Apply(new KeyboardTuningSettings
        {
            RepeatDelay = 1,
            RepeatRate = 31,
            DisableStickyKeys = true,
            DisableFilterKeys = true,
            DisableToggleKeys = true
        });
    }

    /// <summary>Reverts to the state captured before the last Apply.</summary>
    public List<TuningStep> Revert()
    {
        KeyboardLiveState? original = _snapshot;
        if (original == null)
        {
            return new List<TuningStep>();
        }
        _snapshot = ReadLive();
        List<TuningStep> steps = new List<TuningStep>();

        try
        {
            int cur = GetKeyboardDelay();
            int desired = Math.Clamp(original.RepeatDelay, 0, 3);
            bool already = cur == desired;
            bool ok = already || SetKeyboardDelay(desired);
            steps.Add(Make("Key repeat delay", already, ok,
                already ? $"Already {cur}/3." : (ok ? $"Restored to {desired}/3." : "Could not restore repeat delay."),
                $"{cur}/3", $"{desired}/3"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Key repeat delay", ex));
        }

        try
        {
            int cur = GetKeyboardSpeed();
            int desired = Math.Clamp(original.RepeatRate, 0, 31);
            bool already = cur == desired;
            bool ok = already || SetKeyboardSpeed(desired);
            steps.Add(Make("Key repeat rate", already, ok,
                already ? $"Already {cur}/31." : (ok ? $"Restored to {desired}/31." : "Could not restore repeat rate."),
                $"{cur}/31", $"{desired}/31"));
        }
        catch (Exception ex)
        {
            steps.Add(Failed("Key repeat rate", ex));
        }

        // Restore the accessibility toggles to whatever they were before Apply.
        // This is an explicit user action, so restoring the user's own prior state
        // (including turning a feature back on) is correct here.
        steps.Add(RestoreToggle("Sticky Keys", original.StickyKeysOn, GetStickyKeys, SetStickyKeys));
        steps.Add(RestoreToggle("Filter Keys", original.FilterKeysOn, GetFilterKeys, SetFilterKeys));
        steps.Add(RestoreToggle("Toggle Keys", original.ToggleKeysOn, GetToggleKeys, SetToggleKeys));

        return steps;
    }

    private TuningStep RestoreToggle(string label, bool desired, Func<bool> get, Func<bool, bool> set)
    {
        try
        {
            bool cur = get();
            bool already = cur == desired;
            bool ok = already || set(desired);
            return Make(label, already, ok,
                already ? $"Already {(cur ? "on" : "off")} \\u2014 nothing to change." : (ok ? $"Restored to {(desired ? "on" : "off")}." : "Could not restore."),
                cur ? "On" : "Off", desired ? "On" : "Off");
        }
        catch (Exception ex)
        {
            return Failed(label, ex);
        }
    }

    private TuningStep HandleToggle(string label, bool disable, Func<bool> get, Func<bool, bool> set)
    {
        try
        {
            bool cur = get();
            if (!disable)
            {
                return new TuningStep
                {
                    Label = label,
                    Success = true,
                    AlreadyOptimal = false,
                    Changed = false,
                    Status = "Skipped",
                    Detail = $"Left as-is (currently {(cur ? "on" : "off")}).",
                    PreviousState = cur ? "On" : "Off",
                    NewState = cur ? "On" : "Off"
                };
            }
            bool already = !cur;
            bool ok = already || set(false);
            return Make(label, already, ok,
                already ? "Already off \u2014 nothing to change." : (ok ? "Turned off (prevents accidental key toggling in-game)." : "Could not turn off."),
                cur ? "On" : "Off", "Off");
        }
        catch (Exception ex)
        {
            return Failed(label, ex);
        }
    }

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

    // ------------------------------------------------------------------
    // Raw SystemParametersInfo wrappers.
    // ------------------------------------------------------------------
    private static int GetKeyboardDelay()
    {
        int value = 1;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDDELAY, 0u, ref value, 0u);
        return value;
    }

    private static bool SetKeyboardDelay(int delay)
    {
        uint value = (uint)Math.Clamp(delay, 0, 3);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETKEYBOARDDELAY, value, IntPtr.Zero, Flags);
    }

    private static int GetKeyboardSpeed()
    {
        int value = 31;
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDSPEED, 0u, ref value, 0u);
        return value;
    }

    private static bool SetKeyboardSpeed(int speed)
    {
        uint value = (uint)Math.Clamp(speed, 0, 31);
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETKEYBOARDSPEED, value, IntPtr.Zero, Flags);
    }

    private static bool GetStickyKeys()
    {
        NativeMethods.STICKYKEYS data = new NativeMethods.STICKYKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.STICKYKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSTICKYKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        return (data.dwFlags & NativeMethods.SKF_STICKYKEYSON) != 0;
    }

    private static bool SetStickyKeys(bool on)
    {
        NativeMethods.STICKYKEYS data = new NativeMethods.STICKYKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.STICKYKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSTICKYKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        if (on)
        {
            data.dwFlags |= NativeMethods.SKF_STICKYKEYSON;
        }
        else
        {
            data.dwFlags &= ~NativeMethods.SKF_STICKYKEYSON;
        }
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETSTICKYKEYS, data.cbSize, ref data, Flags);
    }

    private static bool GetFilterKeys()
    {
        NativeMethods.FILTERKEYS data = new NativeMethods.FILTERKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.FILTERKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFILTERKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        return (data.dwFlags & NativeMethods.FKF_FILTERKEYSON) != 0;
    }

    private static bool SetFilterKeys(bool on)
    {
        NativeMethods.FILTERKEYS data = new NativeMethods.FILTERKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.FILTERKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFILTERKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        if (on)
        {
            data.dwFlags |= NativeMethods.FKF_FILTERKEYSON;
        }
        else
        {
            data.dwFlags &= ~NativeMethods.FKF_FILTERKEYSON;
        }
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETFILTERKEYS, data.cbSize, ref data, Flags);
    }

    private static bool GetToggleKeys()
    {
        NativeMethods.TOGGLEKEYS data = new NativeMethods.TOGGLEKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.TOGGLEKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETTOGGLEKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        return (data.dwFlags & NativeMethods.TKF_TOGGLEKEYSON) != 0;
    }

    private static bool SetToggleKeys(bool on)
    {
        NativeMethods.TOGGLEKEYS data = new NativeMethods.TOGGLEKEYS
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.TOGGLEKEYS>()
        };
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETTOGGLEKEYS, data.cbSize, ref data, 0u))
        {
            return false;
        }
        if (on)
        {
            data.dwFlags |= NativeMethods.TKF_TOGGLEKEYSON;
        }
        else
        {
            data.dwFlags &= ~NativeMethods.TKF_TOGGLEKEYSON;
        }
        return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETTOGGLEKEYS, data.cbSize, ref data, Flags);
    }
}
