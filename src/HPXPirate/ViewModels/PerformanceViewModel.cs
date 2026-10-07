using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class PerformanceViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private OptimizationMode _mode;

	private bool _isBusy;

	private string _resultText = "Select a mode and press APPLY OPTIMISATION.";

	public OptimizationMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (SetProperty(ref _mode, value, "Mode"))
			{
				RefreshPlan();
			}
		}
	}

	public ObservableCollection<OptimizationAction> Plan { get; } = new ObservableCollection<OptimizationAction>();


	public ObservableCollection<ChangeRecord> Changes { get; } = new ObservableCollection<ChangeRecord>();


	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		private set
		{
			SetProperty(ref _isBusy, value, "IsBusy");
			((RelayCommand)ApplyCommand).RaiseCanExecuteChanged();
		}
	}

	public string ResultText
	{
		get
		{
			return _resultText;
		}
		private set
		{
			SetProperty(ref _resultText, value, "ResultText");
		}
	}

	public bool CreateRestorePoint
	{
		get
		{
			return _host.AppSettings.CreateRestorePoints;
		}
		set
		{
			_host.AppSettings.CreateRestorePoints = value;
			OnPropertyChanged("CreateRestorePoint");
			_host.Settings.Save();
		}
	}

	public ICommand ApplyCommand { get; }

	public ICommand RollbackCommand { get; }

	public PerformanceViewModel(AppHost host)
	{
		_host = host;
		ApplyCommand = new RelayCommand(async delegate
		{
			await ApplyAsync();
		}, () => !IsBusy);
		RollbackCommand = new RelayCommand(Rollback, () => _host.Optimizer.Changes.Any((ChangeRecord c) => !c.Reverted));
		RefreshPlan();
		RefreshChanges();
	}

	private void RefreshPlan()
	{
		Plan.Clear();
		foreach (OptimizationAction item in _host.Optimizer.BuildPlan(Mode))
		{
			Plan.Add(item);
		}
	}

	private void RefreshChanges()
	{
		Changes.Clear();
		foreach (ChangeRecord item in _host.Optimizer.Changes.OrderByDescending((ChangeRecord c) => c.Timestamp))
		{
			Changes.Add(item);
		}
		((RelayCommand)RollbackCommand).RaiseCanExecuteChanged();
	}

	private async Task ApplyAsync()
	{
		IsBusy = true;
		ResultText = "Applying optimisations...";
		List<OptimizationAction> plan = Plan.ToList();
		OptimizationMode mode = Mode;
		bool restore = CreateRestorePoint;
		BoostResult result = await Task.Run(() => _host.Optimizer.ApplyPlan(plan, mode, restore));
		Application current = Application.Current;
		if (current != null)
		{
			((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
			{
				ResultText = result.Summary;
				RefreshPlan();
				RefreshChanges();
				IsBusy = false;
			});
		}
	}

	private void Rollback()
	{
		int num = _host.Optimizer.RollbackAll();
		ResultText = ((num > 0) ? $"Reverted {num} change(s)." : "Nothing to revert.");
		RefreshChanges();
	}
}
