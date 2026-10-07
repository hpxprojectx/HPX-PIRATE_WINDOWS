using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HPXPirate.Services;

namespace HPXPirate;

public partial class App : Application
{

	public static AppHost Host { get; private set; }

	protected override void OnStartup(StartupEventArgs e)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		base.OnStartup(e);
		((Application)this).DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
		AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		Host = new AppHost();
		try
		{
			Host.Initialize();
			Host.Logger.Info("HPX PIRATE starting up.");
			MainWindow mainWindow = (MainWindow)(object)(((Application)this).MainWindow = (Window)(object)new MainWindow());
			((Window)mainWindow).Show();
		}
		catch (Exception ex)
		{
			Host.Logger.Critical("Fatal error during startup.", ex);
			MessageBox.Show("HPX PIRATE failed to start.\n\n" + ex.Message + "\n\nNo system changes were applied. Details were written to the log file.", "HPX PIRATE - Startup Error", (MessageBoxButton)0, (MessageBoxImage)16);
			((Application)this).Shutdown(1);
		}
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		Host?.Logger?.Error("Unhandled UI exception.", e.Exception);
		MessageBox.Show("An unexpected error occurred but HPX PIRATE kept running.\n\n" + e.Exception.Message + "\n\nThe error was logged. No system changes were applied.", "HPX PIRATE - Recovered", (MessageBoxButton)0, (MessageBoxImage)48);
		e.Handled = true;
	}

	private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
		{
			Host?.Logger?.Critical("Unhandled domain exception.", ex);
		}
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		Host?.Logger?.Error("Unobserved task exception.", e.Exception);
		e.SetObserved();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		try
		{
			Host?.Shutdown();
		}
		catch (Exception ex)
		{
			Host?.Logger?.Error("Error during shutdown.", ex);
		}
		base.OnExit(e);
	}

}
