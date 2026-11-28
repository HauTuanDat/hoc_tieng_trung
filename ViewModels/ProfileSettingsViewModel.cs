using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class ProfileSettingsViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly User _currentUser;
    private readonly Action _onLogout;
    private readonly Action _onBack;

    private string _fullName = "";
    private string _email = "";
    private string _currentLevel = "HSK 3";
    private int _wordsCount = 1248;
    private int _listeningCount = 18;
    private int _writingCount = 12;
    private int _readingCount = 15;

    // Password change fields
    private string _oldPassword = "";
    private string _newPassword = "";
    private string _confirmNewPassword = "";
    private string _passwordMessage = "";
    private bool _hasPasswordSuccess = false;
    private bool _hasPasswordError = false;

    // Database Connection fields
    private string _connectionString = "";
    private string _dbStatusMessage = "";
    private bool _isDbConnected = false;

    // App Preferences
    private bool _isSoundEnabled = true;
    private double _volumeLevel = 75;
    private bool _isNotificationEnabled = true;
    private bool _isAchievementNotificationEnabled = true;
    private bool _isEventNotificationEnabled = true;

    // Inline UI expansion states
    private bool _isEditingProfile = false;
    private bool _isChangingPassword = false;
    private string _profileMessage = "";
    private string _backupFullName = "";
    private string _backupEmail = "";

    public ProfileSettingsViewModel(IDatabaseService databaseService, User currentUser, Action onLogout, Action onBack)
    {
        _databaseService = databaseService;
        _currentUser = currentUser;
        _onLogout = onLogout;
        _onBack = onBack;

        // Load user data
        FullName = currentUser.FullName;
        Email = currentUser.Email;
        CurrentLevel = currentUser.CurrentLevel;
        WordsCount = currentUser.WordsLearnedCount;
        ListeningCount = currentUser.ListeningCount;
        WritingCount = currentUser.WritingCount;
        ReadingCount = currentUser.ReadingCount;

        ConnectionString = _databaseService.ConnectionString;

        SaveProfileCommand = new RelayCommand(async () => await SaveProfileAsync());
        ToggleEditProfileCommand = new RelayCommand(() =>
        {
            if (!IsEditingProfile)
            {
                _backupFullName = FullName;
                _backupEmail = Email;
                ProfileMessage = "";
            }
            IsEditingProfile = !IsEditingProfile;
        });
        CancelEditProfileCommand = new RelayCommand(() =>
        {
            FullName = _backupFullName;
            Email = _backupEmail;
            ProfileMessage = "";
            IsEditingProfile = false;
        });
        ToggleChangePasswordCommand = new RelayCommand(() =>
        {
            IsChangingPassword = !IsChangingPassword;
        });
        ChangePasswordCommand = new RelayCommand(async () => await ChangePasswordAsync());
        TestDbConnectionCommand = new RelayCommand(async () => await TestDbConnectionAsync());
        LogoutCommand = new RelayCommand(_onLogout);
        BackCommand = new RelayCommand(_onBack);

        SetLightThemeCommand = new RelayCommand(() => { });
        SetDarkThemeCommand = new RelayCommand(() => { });
        ToggleThemeCommand = new RelayCommand(() => { });

        ThemeService.Instance.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        OnPropertyChanged(nameof(IsDarkMode));
        OnPropertyChanged(nameof(IsLightMode));
        OnPropertyChanged(nameof(CurrentThemeDescription));
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string CurrentLevel
    {
        get => _currentLevel;
        set => SetProperty(ref _currentLevel, value);
    }

    public int WordsCount
    {
        get => _wordsCount;
        set => SetProperty(ref _wordsCount, value);
    }

    public int ListeningCount
    {
        get => _listeningCount;
        set => SetProperty(ref _listeningCount, value);
    }

    public int WritingCount
    {
        get => _writingCount;
        set => SetProperty(ref _writingCount, value);
    }

    public int ReadingCount
    {
        get => _readingCount;
        set => SetProperty(ref _readingCount, value);
    }

    public string OldPassword
    {
        get => _oldPassword;
        set => SetProperty(ref _oldPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
    }

    public string PasswordMessage
    {
        get => _passwordMessage;
        set => SetProperty(ref _passwordMessage, value);
    }

    public bool HasPasswordSuccess
    {
        get => _hasPasswordSuccess;
        set => SetProperty(ref _hasPasswordSuccess, value);
    }

    public bool HasPasswordError
    {
        get => _hasPasswordError;
        set => SetProperty(ref _hasPasswordError, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public string DbStatusMessage
    {
        get => _dbStatusMessage;
        set => SetProperty(ref _dbStatusMessage, value);
    }

    public bool IsDbConnected
    {
        get => _isDbConnected;
        set => SetProperty(ref _isDbConnected, value);
    }

    public bool IsSoundEnabled
    {
        get => _isSoundEnabled;
        set
        {
            if (SetProperty(ref _isSoundEnabled, value))
            {
                SoundService.Instance.IsMuted = !value;
            }
        }
    }

    public double VolumeLevel
    {
        get => _volumeLevel;
        set => SetProperty(ref _volumeLevel, value);
    }

    public bool IsNotificationEnabled
    {
        get => _isNotificationEnabled;
        set
        {
            if (SetProperty(ref _isNotificationEnabled, value))
            {
                OnPropertyChanged(nameof(IsDailyReminderEnabled));
            }
        }
    }

    public bool IsDailyReminderEnabled
    {
        get => _isNotificationEnabled;
        set
        {
            if (SetProperty(ref _isNotificationEnabled, value))
            {
                OnPropertyChanged(nameof(IsNotificationEnabled));
            }
        }
    }

    public bool IsAchievementNotificationEnabled
    {
        get => _isAchievementNotificationEnabled;
        set => SetProperty(ref _isAchievementNotificationEnabled, value);
    }

    public bool IsEventNotificationEnabled
    {
        get => _isEventNotificationEnabled;
        set => SetProperty(ref _isEventNotificationEnabled, value);
    }

    public bool IsEditingProfile
    {
        get => _isEditingProfile;
        set => SetProperty(ref _isEditingProfile, value);
    }

    public bool IsChangingPassword
    {
        get => _isChangingPassword;
        set => SetProperty(ref _isChangingPassword, value);
    }

    public string ProfileMessage
    {
        get => _profileMessage;
        set
        {
            if (SetProperty(ref _profileMessage, value))
            {
                OnPropertyChanged(nameof(HasProfileMessage));
            }
        }
    }

    public bool HasProfileMessage => !string.IsNullOrWhiteSpace(ProfileMessage);

    public bool IsDarkMode => false;
    public bool IsLightMode => true;
    public string CurrentThemeDescription => "Chế độ sáng (Light Mode)";

    public ICommand SetLightThemeCommand { get; }
    public ICommand SetDarkThemeCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public ICommand SaveProfileCommand { get; }
    public ICommand ToggleEditProfileCommand { get; }
    public ICommand CancelEditProfileCommand { get; }
    public ICommand ToggleChangePasswordCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand TestDbConnectionCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand BackCommand { get; }

    private async Task SaveProfileAsync()
    {
        _currentUser.FullName = FullName;
        _currentUser.Email = Email;
        bool ok = await _databaseService.UpdateUserProfileAsync(_currentUser);
        if (ok)
        {
            ProfileMessage = "Cập nhật hồ sơ thành công!";
            PasswordMessage = "Cập nhật hồ sơ thành công!";
            HasPasswordSuccess = true;
            HasPasswordError = false;
            IsEditingProfile = false;
        }
        else
        {
            ProfileMessage = "Không thể lưu thay đổi lúc này.";
        }
    }

    private async Task ChangePasswordAsync()
    {
        HasPasswordSuccess = false;
        HasPasswordError = false;
        PasswordMessage = "";

        if (string.IsNullOrEmpty(OldPassword) || string.IsNullOrEmpty(NewPassword))
        {
            PasswordMessage = "Vui lòng nhập đầy đủ mật khẩu cũ và mới!";
            HasPasswordError = true;
            return;
        }

        if (NewPassword.Length < 6)
        {
            PasswordMessage = "Mật khẩu mới tối thiểu 6 ký tự!";
            HasPasswordError = true;
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            PasswordMessage = "Mật khẩu xác nhận không khớp!";
            HasPasswordError = true;
            return;
        }

        bool success = await _databaseService.ChangePasswordAsync(_currentUser.Id, OldPassword, NewPassword);
        if (success)
        {
            PasswordMessage = "Đổi mật khẩu thành công!";
            HasPasswordSuccess = true;
            OldPassword = "";
            NewPassword = "";
            ConfirmNewPassword = "";
        }
        else
        {
            PasswordMessage = "Mật khẩu cũ không đúng!";
            HasPasswordError = true;
        }
    }

    private async Task TestDbConnectionAsync()
    {
        _databaseService.ConnectionString = ConnectionString;
        var (ok, msg) = await _databaseService.TestConnectionAsync();
        IsDbConnected = ok;
        DbStatusMessage = msg;
    }
}

