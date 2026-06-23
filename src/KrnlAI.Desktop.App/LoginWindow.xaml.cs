using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
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
        _viewModel.StopOAuthCallbackListener();
        base.OnClosed(e);
    }

    private void OnLoginCompleted(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public string Username => _viewModel.Username;
    public string Token => _viewModel.Token;
    public string? RefreshToken => _viewModel.RefreshToken;
}

public class LoginViewModel : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private string _token = string.Empty;
    private string? _refreshToken;
    private string? _oauthState;
    private HttpListener? _oauthListener;

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
    public string? RefreshToken => _refreshToken;

    public ICommand LoginCommand { get; }
    public ICommand LoginWithAzureAdCommand { get; }
    public ICommand LoginWithGoogleCommand { get; }
    public ICommand LoginWithWindowsHelloCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(_ => { _ = ExecuteLoginAsync(); }, _ => CanLogin());
        LoginWithAzureAdCommand = new RelayCommand(_ => { _ = ExecuteOAuth2LoginAsync("azure-ad"); });
        LoginWithGoogleCommand = new RelayCommand(_ => { _ = ExecuteOAuth2LoginAsync("google"); });
        LoginWithWindowsHelloCommand = new AsyncRelayCommand(ExecuteWindowsHelloAsync);
    }

    private bool CanLogin() => !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var kernelClient = ServiceLocator.Instance.KernelClient;
            var response = await kernelClient.LoginAsync(new LoginRequest(Email: Username, Password));

            if (response.Success && !string.IsNullOrEmpty(response.Token))
            {
                _token = response.Token;
                _refreshToken = response.RefreshToken;
                kernelClient.SetTokens(_token, _refreshToken);

                var settingsService = ServiceLocator.Instance.SettingsService;
                var settings = settingsService.LoadSettings();
                settings = settings with { AuthToken = _token, RefreshToken = _refreshToken, Username = RememberMe ? Username : string.Empty, IsAuthenticated = true };
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

    private async Task ExecuteOAuth2LoginAsync(string provider)
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            _oauthState = Guid.NewGuid().ToString("N");

            const string callbackPath = "/oauth/callback";
            var redirectUri = $"http://localhost:49813{callbackPath}";

            var kernelClient = ServiceLocator.Instance.KernelClient;
            var api = ServiceLocator.Instance.GatewayApi;
            if (api == null) { ErrorMessage = "API Gateway não disponível"; return; }
            var loginResponse = await api.OAuth2LoginAsync(
                new OAuth2LoginRequest(
                    provider, redirectUri, _oauthState));

            if (loginResponse?.AuthUrl == null)
            {
                ErrorMessage = loginResponse?.Error ?? "Falha ao iniciar login OAuth2";
                return;
            }

            var browserUri = GetOAuthBrowserUri(loginResponse, redirectUri);
            var code = await StartOAuthCallbackListenerAsync(redirectUri, callbackPath, browserUri);
            if (code == null)
            {
                ErrorMessage = "Callback OAuth2 não recebido";
                return;
            }

            var callbackResult = await api.OAuth2CallbackAsync(
                new OAuth2CallbackRequest(
                    code, _oauthState, provider));

            if (callbackResult?.Token is not null)
            {
                _token = callbackResult.Token;
                _refreshToken = callbackResult.RefreshToken;
                kernelClient.SetTokens(_token, _refreshToken);
                LoginCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Falha no login OAuth2";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro no OAuth2: {ex.Message}";
        }
        finally
        {
            _oauthState = null;
            IsLoading = false;
        }
    }

    public static Uri GetOAuthBrowserUri(OAuth2LoginResponse loginResponse, string fallbackRedirectUri)
    {
        return Uri.TryCreate(loginResponse.AuthUrl, UriKind.Absolute, out var authUri)
            ? authUri
            : new Uri(fallbackRedirectUri);
    }

    private async Task<string?> StartOAuthCallbackListenerAsync(string redirectUri, string callbackPath, Uri browserUri)
    {
        var uri = new Uri(redirectUri);

        try
        {
            _oauthListener = new HttpListener();
            _oauthListener.Prefixes.Add($"http://localhost:{uri.Port}/");
            _oauthListener.Start();

            Process.Start(new ProcessStartInfo(browserUri.ToString())
            {
                UseShellExecute = true,
                Verb = "open"
            });

            var context = await _oauthListener.GetContextAsync();
            var code = context.Request.QueryString["code"];
            var state = context.Request.QueryString["state"];

            var responseBytes = System.Text.Encoding.UTF8.GetBytes(
                "<html><body><p>Autenticação concluída! Feche esta janela.</p></body></html>");
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            context.Response.Close();

            if (state != _oauthState)
            {
                ErrorMessage = "State parameter mismatch — possível ataque CSRF";
                return null;
            }

            return code;
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5)
        {
            ErrorMessage = $"OAuth requer privilégios de administrador. Execute como admin ou use login manual. (Erro: acesso negado ao HttpListener)";
            return null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro no callback OAuth: {ex.Message}";
            return null;
        }
        finally
        {
            StopOAuthCallbackListener();
        }
    }

    private async Task ExecuteWindowsHelloAsync()
    {
        try
        {
            var kernelClient = ServiceLocator.Instance.KernelClient;
            var settingsService = ServiceLocator.Instance.SettingsService;
            var settings = settingsService.LoadSettings();

            if (string.IsNullOrEmpty(settings.AuthToken))
            {
                ErrorMessage = "Nenhuma sessão anterior encontrada. Faça login manual primeiro para salvar o token.";
                return;
            }

            try
            {
                var userConsentVerifierType = Type.GetType("Windows.Security.Credentials.UI.UserConsentVerifier, Windows.Security, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cee3e3a7c98f7b10");
                if (userConsentVerifierType != null)
                {
                    var verificationResult = await Task.Run(async () =>
                    {
                        var requestTask = userConsentVerifierType.GetMethod("RequestVerificationAsync")?
                            .Invoke(null, new object[] { $"Krnl-AI - {settings.Username ?? "Biometric Login"}" });
                        if (requestTask is not null)
                        {
                            var result = await ((dynamic)requestTask);
                            return result?.Status?.ToString() == "Verified";
                        }
                        return false;
                    });

                    if (!verificationResult)
                    {
                        ErrorMessage = "Falha na verificação biométrica. Tente o login com senha.";
                        KrnlLogger.Write($"Windows Hello verification failed for user {settings.Username}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                KrnlLogger.Write($"Windows Hello unavailable: {ex.Message}");
                // Fall through to silent login if biometrics are unavailable on this system
            }

            kernelClient.SetTokens(settings.AuthToken, settings.RefreshToken);
            _token = settings.AuthToken;
            _refreshToken = settings.RefreshToken;
            LoginCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
        }
    }

    public void StopOAuthCallbackListener()
    {
        try
        {
            _oauthListener?.Stop();
            _oauthListener?.Close();
            (_oauthListener as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            Core.Services.KrnlLogger.Write($"StopOAuthCallbackListener: {ex.Message}");
        }
        _oauthListener = null;
    }
}
