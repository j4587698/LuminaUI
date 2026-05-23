using System;
using System.Windows.Input;

namespace LuminaUI.Controls;

internal sealed class LuminaRelayCommand : ICommand
{
	private readonly Action<object?> _execute;

	private readonly Predicate<object?>? _canExecute;

	public event EventHandler? CanExecuteChanged;

	public LuminaRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException("execute");
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
		this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
