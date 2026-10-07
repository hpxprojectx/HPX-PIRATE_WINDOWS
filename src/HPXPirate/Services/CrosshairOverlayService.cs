using System;
using System.Windows;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Views;

namespace HPXPirate.Services;

public sealed class CrosshairOverlayService : IDisposable
{
	private readonly ILogger _log;

	private CrosshairOverlayWindow? _window;

	private CrosshairSettings _settings = new CrosshairSettings();

	public const int CrosshairToggleHotkeyId = 16385;

	public bool IsVisible
	{
		get
		{
			CrosshairOverlayWindow? window = _window;
			if (window == null)
			{
				return false;
			}
			return ((UIElement)window).IsVisible;
		}
	}

	public event Action<bool>? VisibilityChanged;

	public CrosshairOverlayService(ILogger log)
	{
		_log = log;
	}

	public void ApplySettings(CrosshairSettings settings)
	{
		CrosshairSettings settings2 = settings;
		_settings = settings2;
		if (_window != null)
		{
			((DispatcherObject)_window).Dispatcher.Invoke((Action)delegate
			{
				_window.Apply(settings2);
			});
		}
	}

	public void Show()
	{
		try
		{
			if (_window == null)
			{
				_window = new CrosshairOverlayWindow();
				_window.Apply(_settings);
				((Window)_window).Closed += delegate
				{
					_window = null;
					this.VisibilityChanged?.Invoke(obj: false);
				};
			}
			((Window)_window).Show();
			_window.Apply(_settings);
			this.VisibilityChanged?.Invoke(obj: true);
			_log.Info("Crosshair overlay shown.");
		}
		catch (Exception ex)
		{
			_log.Error("Failed to show crosshair overlay.", ex);
		}
	}

	public void Hide()
	{
		try
		{
			CrosshairOverlayWindow? window = _window;
			if (window != null)
			{
				((Window)window).Hide();
			}
			this.VisibilityChanged?.Invoke(obj: false);
			_log.Info("Crosshair overlay hidden.");
		}
		catch (Exception ex)
		{
			_log.Error("Failed to hide crosshair overlay.", ex);
		}
	}

	public void Toggle()
	{
		if (IsVisible)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void Dispose()
	{
		try
		{
			CrosshairOverlayWindow? window = _window;
			if (window != null)
			{
				((Window)window).Close();
			}
		}
		catch
		{
		}
		_window = null;
	}
}
