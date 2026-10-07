using System;
using System.Windows.Input;

namespace HPXPirate.ViewModels;

public sealed class RelayCommand : ICommand
{
	private readonly Action<object?> _execute;

	private readonly Predicate<object?>? _canExecute;

	public event EventHandler? CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	public RelayCommand(Action execute, Func<bool>? canExecute = null)
	{
		Action execute2 = execute;
		Func<bool> canExecute2 = canExecute;
		_execute = delegate
		{
			execute2();
		};
		_canExecute = ((canExecute2 == null) ? null : ((Predicate<object>)((object? _) => canExecute2())));
	}

	public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
	{
		_execute = execute;
		_canExecute = canExecute;
	}

	public bool CanExecute(object? parameter)
	{
		return _canExecute?.Invoke(parameter) ?? true;
	}

	public void Execute(object? parameter)
	{
		_execute(parameter);
	}

	public void RaiseCanExecuteChanged()
	{
		CommandManager.InvalidateRequerySuggested();
	}
}
