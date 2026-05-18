using System.Windows;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly RoutedEventHandler _loadedHandler;

    public LoginWindow()
    {
        InitializeComponent();
        _viewModel = new LoginViewModel();
        DataContext = _viewModel;

        _viewModel.LoginCompleted += OnLoginCompleted;
        PasswordBox.PasswordChanged += OnPasswordChanged;
        _loadedHandler = (s, e) => UsernameTextBox.Focus();
        Loaded += _loadedHandler;
    }

    private void OnPasswordChanged(object? sender, RoutedEventArgs e) => _viewModel.Password = PasswordBox.Password;

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= _loadedHandler;
        PasswordBox.PasswordChanged -= OnPasswordChanged;
        _viewModel.LoginCompleted -= OnLoginCompleted;
        base.OnClosed(e);
    }

    private void OnLoginCompleted(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public string Username => _viewModel.Username;
    public string Token => _viewModel.Token;
}

public class LoginViewModel : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private string _token = string.Empty;

    public event EventHandler? LoginCompleted;

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);
            ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public string Token => _token;

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(_ => { _ = ExecuteLoginAsync(); }, _ => CanLogin());
    }

    private bool CanLogin() => !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var kernelClient = ServiceLocator.Instance.KernelClient;
            var response = await kernelClient.LoginAsync(new LoginRequest(Username, Password));

            if (response.Success && !string.IsNullOrEmpty(response.Token))
            {
                _token = response.Token;
                kernelClient.SetAuthToken(_token);

                var settingsService = ServiceLocator.Instance.SettingsService;
                var settings = settingsService.LoadSettings();
                settings = settings with { AuthToken = _token, Username = RememberMe ? Username : string.Empty, IsAuthenticated = true };
                settingsService.SaveSettings(settings);

                LoginCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = response.Message ?? "Login falhou";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao conectar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
