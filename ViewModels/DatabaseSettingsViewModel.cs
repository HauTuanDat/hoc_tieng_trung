using System.Windows.Input;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class DatabaseSettingsViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly Action _onBack;

    private string _connectionString;
    private bool _isTesting;
    private bool? _isConnectionSuccessful;
    private string _statusMessage = string.Empty;

    public ICommand BackCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveAndInitCommand { get; }

    public DatabaseSettingsViewModel(IDatabaseService databaseService, Action onBack)
    {
        _databaseService = databaseService;
        _onBack = onBack;
        _connectionString = _databaseService.ConnectionString;

        BackCommand = new RelayCommand(_onBack);
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
        SaveAndInitCommand = new RelayCommand(async () => await SaveAndInitAsync());
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public bool? IsConnectionSuccessful
    {
        get => _isConnectionSuccessful;
        set => SetProperty(ref _isConnectionSuccessful, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            IsTesting = true;
            StatusMessage = "Đang kiểm tra kết nối...";
            IsConnectionSuccessful = null;

            _databaseService.ConnectionString = ConnectionString;
            var (success, message) = await _databaseService.TestConnectionAsync();

            IsConnectionSuccessful = success;
            StatusMessage = message;
        }
        catch (Exception ex)
        {
            IsConnectionSuccessful = false;
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveAndInitAsync()
    {
        try
        {
            IsTesting = true;
            StatusMessage = "Đang khởi tạo cơ sở dữ liệu và bảng...";

            _databaseService.ConnectionString = ConnectionString;
            await _databaseService.InitializeDatabaseAsync();

            IsConnectionSuccessful = true;
            StatusMessage = "Khởi tạo thành công CSDL và nạp dữ liệu mẫu!";
        }
        catch (Exception ex)
        {
            IsConnectionSuccessful = false;
            StatusMessage = $"Lỗi khi khởi tạo CSDL: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }
}
