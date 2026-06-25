using System.Windows.Input;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}

public class AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Func<object?, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private int _isExecutingFlag;

    public static event Action<Exception>? UnhandledException;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => Interlocked.CompareExchange(ref _isExecutingFlag, 0, 0) == 0 && (canExecute?.Invoke(parameter) ?? true);

    public void Execute(object? parameter)
    {
        if (Interlocked.Exchange(ref _isExecutingFlag, 1) != 0) return;

        RaiseCanExecuteChanged();

        _ = ExecuteAsync(parameter);
    }

    private async Task ExecuteAsync(object? parameter)
    {
        try
        {
            await _execute(parameter).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"[AsyncRelayCommand] Unhandled: {ex}");
            UnhandledException?.Invoke(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isExecutingFlag, 0);
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
