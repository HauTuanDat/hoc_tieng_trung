using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly Action<User> _onLoginSuccess;

    private bool _isLoginTab = true;
    private string _loginUsername = "minhdat@gmail.com";
    private string _loginPassword = "";
    private bool _rememberMe = true;

    private string _registerFullName = "";
    private string _registerUsername = "";
    private string _registerPassword = "";
    private string _registerConfirmPassword = "";

    private string _errorMessage = "";
    private string _successMessage = "";
    private bool _isLoading;
    private bool _isPasswordVisible = false;

    public LoginViewModel(IDatabaseService databaseService, Action<User> onLoginSuccess)
    {
        _databaseService = databaseService;
        _onLoginSuccess = onLoginSuccess;

        SwitchToLoginTabCommand = new RelayCommand(() =>
        {
            IsLoginTab = true;
            ErrorMessage = "";
            SuccessMessage = "";
        });

        SwitchToRegisterTabCommand = new RelayCommand(() =>
        {
            IsLoginTab = false;
            ErrorMessage = "";
            SuccessMessage = "";
        });

        TogglePasswordVisibilityCommand = new RelayCommand(() =>
        {
            IsPasswordVisible = !IsPasswordVisible;
        });

        LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), () => !IsLoading);
        RegisterCommand = new RelayCommand(async () => await ExecuteRegisterAsync(), () => !IsLoading);
        QuickDemoLoginCommand = new RelayCommand(async () =>
        {
            LoginUsername = "minhdat@gmail.com";
            LoginPassword = "123456";
            await ExecuteLoginAsync();
        });
    }

    public bool IsLoginTab
    {
        get => _isLoginTab;
        set
        {
            if (SetProperty(ref _isLoginTab, value))
            {
                OnPropertyChanged(nameof(IsRegisterTab));
            }
        }
    }

    public bool IsRegisterTab => !IsLoginTab;

    public string LoginUsername
    {
        get => _loginUsername;
        set => SetProperty(ref _loginUsername, value);
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public string RegisterFullName
    {
        get => _registerFullName;
        set => SetProperty(ref _registerFullName, value);
    }

    public string RegisterUsername
    {
        get => _registerUsername;
        set => SetProperty(ref _registerUsername, value);
    }

    public string RegisterPassword
    {
        get => _registerPassword;
        set => SetProperty(ref _registerPassword, value);
    }

    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set => SetProperty(ref _registerConfirmPassword, value);
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    public ICommand SwitchToLoginTabCommand { get; }
    public ICommand SwitchToRegisterTabCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand QuickDemoLoginCommand { get; }

    public async Task ExecuteLoginAsync()
    {
        ErrorMessage = "";
        SuccessMessage = "";

        if (string.IsNullOrWhiteSpace(LoginUsername))
        {
            ErrorMessage = "Vui lòng nhập Email hoặc số điện thoại / tên tài khoản!";
            return;
        }

        if (string.IsNullOrEmpty(LoginPassword))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu!";
            return;
        }

        IsLoading = true;
        try
        {
            var (success, message, user) = await _databaseService.LoginAsync(LoginUsername, LoginPassword);
            if (success && user != null)
            {
                SuccessMessage = "Đăng nhập thành công! Đang chuyển hướng...";
                await Task.Delay(300);
                _onLoginSuccess(user);
            }
            else
            {
                ErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể kết nối cơ sở dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ExecuteRegisterAsync()
    {
        ErrorMessage = "";
        SuccessMessage = "";

        if (string.IsNullOrWhiteSpace(RegisterFullName))
        {
            ErrorMessage = "Vui lòng nhập họ và tên của bạn!";
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterUsername))
        {
            ErrorMessage = "Vui lòng nhập Email hoặc Tên tài khoản!";
            return;
        }

        if (string.IsNullOrEmpty(RegisterPassword) || RegisterPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có độ dài tối thiểu 6 ký tự!";
            return;
        }

        if (RegisterPassword != RegisterConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp!";
            return;
        }

        IsLoading = true;
        try
        {
            var (success, message, newUser) = await _databaseService.RegisterAsync(RegisterFullName, RegisterUsername, RegisterPassword);
            if (success && newUser != null)
            {
                SuccessMessage = "Đăng ký thành công! Đang đăng nhập...";
                await Task.Delay(400);
                _onLoginSuccess(newUser);
            }
            else
            {
                ErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi đăng ký: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

