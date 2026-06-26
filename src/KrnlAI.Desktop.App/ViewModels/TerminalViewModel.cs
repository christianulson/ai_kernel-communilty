using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class TerminalViewModel : ViewModelBase
{
    private HubConnection? _connection;
    private string _command = "";
    private string _connectionStatus = "Disconnected";
    private string? _errorMessage;
    private bool _isExecuting;
    private string _savedInput = "";

    public ObservableCollection<TerminalOutput> Output { get; } = [];
    public ObservableCollection<string> CommandHistory { get; } = [];
    public int CommandHistoryIndex { get; set; } = -1;
    private string _outputLineCount = "0 lines";
    public string OutputLineCount
    {
        get => _outputLineCount;
        set => SetProperty(ref _outputLineCount, value);
    }

    public string Command
    {
        get => _command;
        set => SetProperty(ref _command, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                ((AsyncRelayCommand)ExecuteCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ClearOutputCommand { get; }
    public ICommand NavigateHistoryUpCommand { get; }
    public ICommand NavigateHistoryDownCommand { get; }

    public TerminalViewModel()
    {
        Output.CollectionChanged += (_, _) =>
        {
            OutputLineCount = $"{Output.Count} lines";
        };

        ExecuteCommand = new AsyncRelayCommand(async _ =>
        {
            var cmd = Command?.Trim();
            if (string.IsNullOrWhiteSpace(cmd)) return;

            CommandHistory.Insert(0, cmd);
            CommandHistoryIndex = -1;
            Command = "";

            if (_connection?.State == HubConnectionState.Connected)
            {
                IsExecuting = true;
                try
                {
                    await _connection.InvokeAsync("ExecuteCommand", cmd).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    Output.Add(new TerminalOutput("error", $"Error: {ex.Message}"));
                }
                finally
                {
                    IsExecuting = false;
                }
            }
            else
            {
                Output.Add(new TerminalOutput("error", "Not connected to terminal hub."));
            }
        }, _ => !_isExecuting);

        CancelCommand = new RelayCommand(() =>
        {
            if (_connection?.State == HubConnectionState.Connected && _isExecuting)
            {
                try
                {
                    _ = _connection.InvokeAsync("CancelExecution");
                }
                catch (Exception ex)
                {
                    KrnlLogger.Write($"CancelExecution error: {ex.Message}");
                }
            }
        }, () => _isExecuting);

        ClearOutputCommand = new RelayCommand(() =>
        {
            Output.Clear();
            CommandHistory.Clear();
            ErrorMessage = null;
        });

        NavigateHistoryUpCommand = new RelayCommand(NavigateHistoryUp);
        NavigateHistoryDownCommand = new RelayCommand(NavigateHistoryDown);
    }

    public async Task ConnectAsync(string hubUrl)
    {
        if (_connection?.State == HubConnectionState.Connected) return;
        if (_connection?.State == HubConnectionState.Connecting) return;

        ConnectionStatus = "Connecting";
        ErrorMessage = null;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string>("SendOutput", chunk =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Output.Add(new TerminalOutput("output", chunk));
            });
        });

        _connection.On<string>("SendError", chunk =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Output.Add(new TerminalOutput("error", chunk));
            });
        });

        _connection.Reconnecting += _ =>
        {
            App.Current.Dispatcher.Invoke(() => ConnectionStatus = "Reconnecting");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            App.Current.Dispatcher.Invoke(() => ConnectionStatus = "Connected");
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            App.Current.Dispatcher.Invoke(() => ConnectionStatus = "Disconnected");
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync().ConfigureAwait(false);
            ConnectionStatus = "Connected";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ConnectionStatus = "Error";
            KrnlLogger.Write($"TerminalHub connect error: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection == null) return;
        try
        {
            await _connection.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"TerminalHub disconnect error: {ex.Message}");
        }
        finally
        {
            _connection = null;
            ConnectionStatus = "Disconnected";
        }
    }

    public void NavigateHistoryUp()
    {
        if (CommandHistory.Count == 0) return;

        if (CommandHistoryIndex == -1)
        {
            _savedInput = Command;
            CommandHistoryIndex = 0;
        }
        else if (CommandHistoryIndex < CommandHistory.Count - 1)
        {
            CommandHistoryIndex++;
        }

        Command = CommandHistory[CommandHistoryIndex];
    }

    public void NavigateHistoryDown()
    {
        if (CommandHistoryIndex <= 0)
        {
            CommandHistoryIndex = -1;
            Command = _savedInput;
        }
        else
        {
            CommandHistoryIndex--;
            Command = CommandHistory[CommandHistoryIndex];
        }
    }
}
