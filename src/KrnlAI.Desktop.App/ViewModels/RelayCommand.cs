using System.Threading;
using System.Windows.Input;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private int _isExecutingFlag;

    public static event Action<Exception>? UnhandledException;

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => Interlocked.CompareExchange(ref _isExecutingFlag, 0, 0) == 0 && (_canExecute?.Invoke(parameter) ?? true);

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
            await _execute(parameter);
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
