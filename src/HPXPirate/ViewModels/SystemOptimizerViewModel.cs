using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class SystemOptimizerViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private ProcessInfoModel? _selectedProcess;

	private string _status = "";

	public ObservableCollection<ProcessInfoModel> Processes { get; } = new ObservableCollection<ProcessInfoModel>();


	public ObservableCollection<StartupEntry> StartupEntries { get; } = new ObservableCollection<StartupEntry>();


	public ObservableCollection<ChangeRecord> Changes { get; } = new ObservableCollection<ChangeRecord>();


	public ProcessInfoModel? SelectedProcess
	{
		get
		{
			return _selectedProcess;
		}
		set
		{
			SetProperty(ref _selectedProcess, value, "SelectedProcess");
			((RelayCommand)OptimizeProcessCommand).RaiseCanExecuteChanged();
		}
	}

	public string Status
	{
		get
		{
			return _status;
		}
		private set
		{
			SetProperty(ref _status, value, "Status");
		}
	}

	public ICommand RefreshCommand { get; }

	public ICommand OptimizeProcessCommand { get; }

	public ICommand CleanTempCommand { get; }

	public ICommand CleanCacheCommand { get; }

	public ICommand RestorePointCommand { get; }

	public ICommand BackupSettingsCommand { get; }

	public ICommand RestoreSettingsCommand { get; }

	public ICommand RollbackCommand { get; }

	public SystemOptimizerViewModel(AppHost host)
	{
		_host = host;
		RefreshCommand = new RelayCommand(Refresh);
		OptimizeProcessCommand = new RelayCommand(OptimizeSelectedProcess, () => SelectedProcess != null);
		CleanTempCommand = new RelayCommand((Action)delegate
		{
			RunCleanup(OptimizationCategory.TempCleanup);
		}, (Func<bool>?)null);
		CleanCacheCommand = new RelayCommand((Action)delegate
		{
			RunCleanup(OptimizationCategory.CacheCleanup);
		}, (Func<bool>?)null);
		RestorePointCommand = new RelayCommand(CreateRestorePoint);
		BackupSettingsCommand = new RelayCommand(BackupSettings);
		RestoreSettingsCommand = new RelayCommand(RestoreSettings);
		RollbackCommand = new RelayCommand(Rollback, () => _host.Optimizer.Changes.Any((ChangeRecord c) => !c.Reverted));
		Refresh();
	}

	private void Refresh()
	{
		List<ProcessInfoModel> topProcesses = _host.Processes.GetTopProcesses(30);
		Processes.Clear();
		foreach (ProcessInfoModel item in topProcesses)
		{
			Processes.Add(item);
		}
		StartupEntries.Clear();
		foreach (var (name, command, source) in _host.Startup.ListStartupEntries())
		{
			StartupEntries.Add(new StartupEntry(name, command, source));
		}
		Changes.Clear();
		foreach (ChangeRecord item2 in _host.Optimizer.Changes.OrderByDescending((ChangeRecord c) => c.Timestamp))
		{
			Changes.Add(item2);
		}
		((RelayCommand)RollbackCommand).RaiseCanExecuteChanged();
	}

	private void OptimizeSelectedProcess()
	{
		if (SelectedProcess != null)
		{
			if (!SelectedProcess.IsSafeToOptimize)
			{
				Status = SelectedProcess.Name + " is protected (system/security/game) and was not modified.";
				return;
			}
			_host.Processes.SetPriority(SelectedProcess.Pid, ProcessPriorityClass.BelowNormal, out string message);
			Status = message;
			Refresh();
		}
	}

	private void RunCleanup(OptimizationCategory category)
	{
		OptimizationAction optimizationAction = _host.Optimizer.BuildPlan(OptimizationMode.Custom).FirstOrDefault((OptimizationAction a) => a.Category == category);
		if (optimizationAction == null)
		{
			Status = "Action not available.";
			return;
		}
		optimizationAction.Selected = true;
		BoostResult boostResult = _host.Optimizer.ApplyPlan(new OptimizationAction[1] { optimizationAction }, OptimizationMode.Custom, createRestorePoint: false);
		Status = boostResult.Steps.FirstOrDefault()?.Detail ?? boostResult.Summary;
		Refresh();
	}

	private void CreateRestorePoint()
	{
		_host.Restore.TryCreateRestorePoint("HPX PIRATE manual restore point", out string message);
		Status = message;
	}

	private void BackupSettings()
	{
		try
		{
			AppPaths.EnsureCreated();
			string text = Path.Combine(AppPaths.Backups, $"settings_{DateTime.Now:yyyyMMdd_HHmmss}.json");
			File.Copy(AppPaths.SettingsFile, text, overwrite: true);
			Status = "Settings backed up: " + text;
		}
		catch (Exception ex)
		{
			Status = "Backup failed: " + ex.Message;
		}
	}

	private void RestoreSettings()
	{
		try
		{
			FileInfo fileInfo = (from f in new DirectoryInfo(AppPaths.Backups).GetFiles("settings_*.json")
				orderby f.LastWriteTime descending
				select f).FirstOrDefault();
			if (fileInfo == null)
			{
				Status = "No settings backup found.";
				return;
			}
			File.Copy(fileInfo.FullName, AppPaths.SettingsFile, overwrite: true);
			_host.Settings.Load();
			Status = "Settings restored from " + fileInfo.Name;
		}
		catch (Exception ex)
		{
			Status = "Restore failed: " + ex.Message;
		}
	}

	private void Rollback()
	{
		int num = _host.Optimizer.RollbackAll();
		Status = ((num > 0) ? $"Reverted {num} change(s)." : "Nothing to revert.");
		Refresh();
	}
}
