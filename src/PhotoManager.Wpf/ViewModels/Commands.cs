using System.Windows.Input;

namespace PhotoManager.Wpf.ViewModels;

public sealed class AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private bool _running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running && (canExecute?.Invoke() ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return; _running = true; Raise();
        try { await execute(); } finally { _running = false; Raise(); }
    }
    public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null) : ICommand
{
    private bool _running;
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_running) return false;
        if (!TryGetValue(parameter, out var value)) return false;
        return canExecute?.Invoke(value) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        if (!TryGetValue(parameter, out var value)) return;
        _running = true;
        Raise();
        try { await execute(value); }
        finally { _running = false; Raise(); }
    }

    public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static bool TryGetValue(object? parameter, out T value)
    {
        if (parameter is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return parameter is null && default(T) is null;
    }
}

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
