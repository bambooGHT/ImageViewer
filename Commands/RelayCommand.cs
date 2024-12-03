using System.Windows.Input;

namespace ImageViewer.Commands;

public class RelayCommand(Action<object?> func, Predicate<object?>? canExecute = null) : ICommand {
	public bool CanExecute(object? parameter) {
		return canExecute == null || canExecute(parameter);
	}

	public void Execute(object? parameter) {
		func(parameter);
	}

	public event EventHandler? CanExecuteChanged {
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}
}