using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class LearnedVocabViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action _onBack;
    private readonly Action<string, string> _onStartQuiz;

    private string _selectedLevel = "HSK 1";
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private int _totalLearnedCount;
    private int _totalLevelWords = 150;
    private double _progressPercent;
    private string _statusMessage = string.Empty;
    private bool _isPinyinVisible = false; // Mặc định KHÔNG CÓ PINYIN theo yêu cầu người dùng

    public ObservableCollection<ChineseVocabulary> LearnedWords { get; } = new();

    public ICommand BackCommand { get; }
    public ICommand FilterLevelCommand { get; }
    public ICommand ReviewLearnedQuizCommand { get; }
    public ICommand StartLearningNewBatchCommand { get; }
    public ICommand UnlearnWordCommand { get; }
    public ICommand ResetLevelProgressCommand { get; }
    public ICommand TogglePinyinCommand { get; }
    public ICommand PlayAudioCommand { get; }

    public LearnedVocabViewModel(
        IHskService hskService,
        Action onBack,
        Action<string, string> onStartQuiz,
        string initialLevel = "HSK 1")
    {
        _hskService = hskService;
        _onBack = onBack;
        _onStartQuiz = onStartQuiz;
        _selectedLevel = initialLevel;

        BackCommand = new RelayCommand(_onBack);
        PlayAudioCommand = new RelayCommand(param =>
        {
            if (param is string text && !string.IsNullOrWhiteSpace(text))
            {
                _ = ChineseAudioService.Instance.PlayAsync(text);
            }
        });
        FilterLevelCommand = new RelayCommand(param =>
        {
            if (param is string lvl)
            {
                SelectedLevel = lvl;
            }
        });

        ReviewLearnedQuizCommand = new RelayCommand(async () =>
        {
            if (LearnedWords.Count < 4)
            {
                StatusMessage = "Cần tối thiểu 4 từ đã học để tạo bài trắc nghiệm ôn tập!";
                return;
            }
            _onStartQuiz(SelectedLevel, "ReviewLearned");
        });

        StartLearningNewBatchCommand = new RelayCommand(() =>
        {
            _onStartQuiz(SelectedLevel, "MeaningToHanzi");
        });

        UnlearnWordCommand = new RelayCommand(async param =>
        {
            if (param is ChineseVocabulary vocab)
            {
                await _hskService.UnlearnWordAsync(vocab.Id);
                LearnedWords.Remove(vocab);
                TotalLearnedCount = LearnedWords.Count;
                CalculateProgress();
                StatusMessage = $"Đã bỏ đánh dấu từ '{vocab.Word}'. Từ này sẽ xuất hiện lại trong các lượt học mới.";
            }
        });

        ResetLevelProgressCommand = new RelayCommand(async () =>
        {
            await _hskService.ResetLevelProgressAsync(SelectedLevel);
            await LoadLearnedWordsAsync();
            StatusMessage = $"Đã đặt lại toàn bộ tiến độ của {SelectedLevel}. Bạn có thể bắt đầu học lại từ đầu!";
        });

        TogglePinyinCommand = new RelayCommand(() =>
        {
            IsPinyinVisible = !IsPinyinVisible;
        });
    }

    public bool IsPinyinVisible
    {
        get => _isPinyinVisible;
        set
        {
            if (SetProperty(ref _isPinyinVisible, value))
            {
                OnPropertyChanged(nameof(PinyinToggleText));
            }
        }
    }

    public string PinyinToggleText => IsPinyinVisible ? "🚫 Ẩn Pinyin" : "👁 Hiện Pinyin";

    public string SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                _ = LoadLearnedWordsAsync();
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadLearnedWordsAsync();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public int TotalLearnedCount
    {
        get => _totalLearnedCount;
        set
        {
            if (SetProperty(ref _totalLearnedCount, value))
            {
                OnPropertyChanged(nameof(HasLearnedWords));
                OnPropertyChanged(nameof(ProgressSummaryText));
            }
        }
    }

    public int TotalLevelWords
    {
        get => _totalLevelWords;
        set => SetProperty(ref _totalLevelWords, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public bool HasLearnedWords => TotalLearnedCount > 0;

    public string ProgressSummaryText => $"{TotalLearnedCount} / {TotalLevelWords} từ đã hoàn thành ({ProgressPercent:F1}%)";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync()
    {
        await LoadLearnedWordsAsync();
    }

    public async Task LoadLearnedWordsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            var list = await _hskService.GetLearnedVocabulariesAsync(SelectedLevel, SearchQuery);
            LearnedWords.Clear();
            foreach (var item in list)
            {
                LearnedWords.Add(item);
            }

            TotalLearnedCount = LearnedWords.Count;

            // Fetch total words in this level for progress
            var allLevelWords = await _hskService.GetVocabulariesAsync(SelectedLevel, limit: 5000);
            TotalLevelWords = allLevelWords.Count > 0 ? allLevelWords.Count : 150;

            CalculateProgress();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CalculateProgress()
    {
        ProgressPercent = TotalLevelWords > 0 ? ((double)TotalLearnedCount / TotalLevelWords) * 100.0 : 0;
        OnPropertyChanged(nameof(ProgressSummaryText));
    }
}
