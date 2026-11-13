using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action<string, string> _onStartQuiz;
    private readonly Action<string> _onOpenVocabList;
    private readonly Action<string> _onOpenTypingPractice;
    private readonly Action<string>? _onOpenLearnedVocab;
    private readonly Action<string>? _onOpenSentenceScramble;
    private readonly Action<string>? _onOpenDictionary;
    private readonly Action<string>? _onOpenVocabStudy;

    private User _currentUser = new() { FullName = "Minh Đạt", CurrentLevel = "HSK 3", StreakDays = 12 };
    private int _totalVocabularies = 6743;
    private int _masteredVocabularies = 0;
    private int _currentStreak = 0;
    private int _listeningCount = 0;
    private int _writingCount = 0;
    private int _readingCount = 0;
    private int _sentenceCount = 0;

    private bool _isLoading;
    private bool _isImporting;
    private string _importStatusMessage = string.Empty;
    private double _importProgressPercent;
    private string _excelFilePath;

    public ObservableCollection<HskLevelProgress> HskLevels { get; } = new();
    public ObservableCollection<HskLevelProgress> AdvancedLevels { get; } = new();

    public ICommand StartQuizCommand { get; }
    public ICommand StartQuizHanziCommand { get; }
    public ICommand OpenVocabListCommand { get; }
    public ICommand OpenTypingPracticeCommand { get; }
    public ICommand OpenLearnedVocabCommand { get; }
    public ICommand OpenSentenceScrambleCommand { get; }
    public ICommand OpenDictionaryCommand { get; }
    public ICommand OpenVocabStudyCommand { get; }
    public ICommand ContinueLearningCommand { get; }
    public ICommand ImportExcelCommand { get; }
    public ICommand RefreshCommand { get; }

    public DashboardViewModel(
        IHskService hskService,
        Action<string, string> onStartQuiz,
        Action<string> onOpenVocabList,
        Action<string> onOpenTypingPractice,
        Action<string>? onOpenLearnedVocab = null,
        Action<string>? onOpenSentenceScramble = null,
        Action<string>? onOpenDictionary = null,
        Action<string>? onOpenVocabStudy = null,
        User? user = null)
    {
        _hskService = hskService;
        _onStartQuiz = onStartQuiz;
        _onOpenVocabList = onOpenVocabList;
        _onOpenTypingPractice = onOpenTypingPractice;
        _onOpenLearnedVocab = onOpenLearnedVocab;
        _onOpenSentenceScramble = onOpenSentenceScramble;
        _onOpenDictionary = onOpenDictionary;
        _onOpenVocabStudy = onOpenVocabStudy;

        if (user != null)
        {
            _currentUser = user;
            _currentStreak = user.StreakDays;
            _masteredVocabularies = user.WordsLearnedCount;
            _listeningCount = user.ListeningCount;
            _writingCount = user.WritingCount;
            _readingCount = user.ReadingCount;
            _sentenceCount = user.SentenceCount;
        }

        string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string defaultPath = Path.Combine(downloads, "11.LUYỆN GÕ HSK 1~6 new.xlsx");
        _excelFilePath = File.Exists(defaultPath) ? defaultPath : Path.Combine(downloads, "11.LUYỆN GÕ HSK 1~6 new");

        StartQuizCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            _onStartQuiz(level, "MeaningToHanzi");
        });

        StartQuizHanziCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            _onStartQuiz(level, "HanziToMeaning");
        });

        OpenVocabListCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "ALL";
            _onOpenVocabList(level);
        });

        OpenTypingPracticeCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            _onOpenTypingPractice(level);
        });

        OpenLearnedVocabCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            _onOpenLearnedVocab?.Invoke(level);
        });

        OpenSentenceScrambleCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            _onOpenSentenceScramble?.Invoke(level);
        });

        OpenDictionaryCommand = new RelayCommand(param =>
        {
            string q = param as string ?? string.Empty;
            _onOpenDictionary?.Invoke(q);
        });

        OpenVocabStudyCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            _onOpenVocabStudy?.Invoke(lvl);
        });

        ContinueLearningCommand = new RelayCommand(() =>
        {
            _onOpenVocabStudy?.Invoke("HSK 3");
        });

        ImportExcelCommand = new RelayCommand(async () => await ExecuteImportAsync(), () => !IsImporting);
        RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
    }

    public User CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public int TotalVocabularies
    {
        get => _totalVocabularies;
        set => SetProperty(ref _totalVocabularies, value);
    }

    public int MasteredVocabularies
    {
        get => _masteredVocabularies;
        set => SetProperty(ref _masteredVocabularies, value);
    }

    public int CurrentStreak
    {
        get => _currentStreak;
        set => SetProperty(ref _currentStreak, value);
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

    public int SentenceCount
    {
        get => _sentenceCount;
        set => SetProperty(ref _sentenceCount, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        set
        {
            if (SetProperty(ref _isImporting, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ImportStatusMessage
    {
        get => _importStatusMessage;
        set => SetProperty(ref _importStatusMessage, value);
    }

    public double ImportProgressPercent
    {
        get => _importProgressPercent;
        set => SetProperty(ref _importProgressPercent, value);
    }

    public string ExcelFilePath
    {
        get => _excelFilePath;
        set => SetProperty(ref _excelFilePath, value);
    }

    public async Task InitializeAsync()
    {
        await LoadDashboardDataAsync();
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            IsLoading = true;
            TotalVocabularies = await _hskService.GetTotalVocabCountAsync();
            var levels = await _hskService.GetLevelProgressListAsync();

            HskLevels.Clear();
            AdvancedLevels.Clear();
            int totalMastered = 0;
            foreach (var lvl in levels)
            {
                totalMastered += lvl.MasteredWords;
                if (lvl.LevelName.StartsWith("HSK"))
                {
                    HskLevels.Add(lvl);
                }
                else
                {
                    AdvancedLevels.Add(lvl);
                }
            }

            if (totalMastered > 0)
            {
                MasteredVocabularies = totalMastered;
            }
        }
        catch
        {
            // fallback
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ExecuteImportAsync()
    {
        if (string.IsNullOrWhiteSpace(ExcelFilePath))
        {
            ImportStatusMessage = "Vui lòng chọn đường dẫn tệp Excel!";
            return;
        }

        try
        {
            IsImporting = true;
            ImportStatusMessage = "Đang bắt đầu nạp từ vựng...";
            ImportProgressPercent = 0;

            var progress = new Progress<(int Current, int Total, string Status)>(report =>
            {
                ImportStatusMessage = report.Status;
                ImportProgressPercent = report.Current;
            });

            int total = await _hskService.ImportFromExcelAsync(ExcelFilePath, progress);
            ImportStatusMessage = $"Thành công! Đã nạp {total:N0} từ vựng vào SQL Server.";
            await LoadDashboardDataAsync();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Lỗi khi nhập Excel: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }
}
