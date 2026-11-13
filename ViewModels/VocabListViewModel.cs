using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class VocabListViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action _onBack;
    private readonly Action<string> _onStartQuiz;

    private string _selectedLevel = "ALL";
    private string _selectedStatus = "Tất cả trạng thái";
    private bool _onlyLearned = false;
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private int _totalItems;

    private int _currentPage = 1;
    private int _pageSize = 15;
    private int _totalPages = 1;

    private List<ChineseVocabulary> _allLoaded = new();

    public ObservableCollection<ChineseVocabulary> Vocabularies { get; } = new();
    public ObservableCollection<string> LevelList { get; } = new()
    {
        "Tất cả khóa học", "HSK 1", "HSK 2", "HSK 3", "HSK 4", "HSK 5", "HSK 6", "Công xưởng", "Kế toán", "Xuất nhập khẩu"
    };

    public ObservableCollection<string> StatusList { get; } = new()
    {
        "Tất cả trạng thái", "Đã học", "Đang học", "Đánh dấu", "Chưa học"
    };

    public ICommand BackCommand { get; }
    public ICommand StartQuizCommand { get; }
    public ICommand FilterLevelCommand { get; }
    public ICommand ToggleMasteredCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand GoToPageCommand { get; }

    public VocabListViewModel(IHskService hskService, Action onBack, Action<string> onStartQuiz, string initialLevel = "ALL")
    {
        _hskService = hskService;
        _onBack = onBack;
        _onStartQuiz = onStartQuiz;
        _selectedLevel = string.IsNullOrWhiteSpace(initialLevel) ? "ALL" : initialLevel;

        BackCommand = new RelayCommand(_onBack);
        StartQuizCommand = new RelayCommand(() => _onStartQuiz(SelectedLevel));
        FilterLevelCommand = new RelayCommand(param =>
        {
            if (param is string lvl)
            {
                SelectedLevel = lvl;
            }
        });

        ToggleMasteredCommand = new RelayCommand(async param =>
        {
            if (param is ChineseVocabulary vocab)
            {
                vocab.IsMastered = !vocab.IsMastered;
                await _hskService.MarkMasteredAsync(vocab.Id, vocab.IsMastered);
                OnPropertyChanged(nameof(Vocabularies));
            }
        });

        PlayAudioCommand = new RelayCommand(param =>
        {
            if (param is string text && !string.IsNullOrWhiteSpace(text))
            {
                _ = ChineseAudioService.Instance.PlayAsync(text);
            }
        });

        NextPageCommand = new RelayCommand(() =>
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                ApplyPagination();
            }
        });

        PrevPageCommand = new RelayCommand(() =>
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                ApplyPagination();
            }
        });

        GoToPageCommand = new RelayCommand(param =>
        {
            if (int.TryParse(param?.ToString(), out int p) && p >= 1 && p <= TotalPages)
            {
                CurrentPage = p;
                ApplyPagination();
            }
        });
    }

    public string SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                CurrentPage = 1;
                _ = LoadVocabulariesAsync();
            }
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                CurrentPage = 1;
                _ = LoadVocabulariesAsync();
            }
        }
    }

    public bool OnlyLearned
    {
        get => _onlyLearned;
        set
        {
            if (SetProperty(ref _onlyLearned, value))
            {
                CurrentPage = 1;
                _ = LoadVocabulariesAsync();
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
                CurrentPage = 1;
                _ = LoadVocabulariesAsync();
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public async Task InitializeAsync()
    {
        await LoadVocabulariesAsync();
    }

    private async Task LoadVocabulariesAsync()
    {
        try
        {
            IsLoading = true;
            string? queryLvl = SelectedLevel == "Tất cả khóa học" || SelectedLevel == "ALL" ? null : SelectedLevel;
            var list = await _hskService.GetVocabulariesAsync(queryLvl, string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery, limit: 3000);

            if (OnlyLearned)
            {
                list = list.Where(v => v.IsMastered || v.PracticeCount > 0).ToList();
            }

            if (SelectedStatus == "Đã học")
            {
                list = list.Where(v => v.IsMastered).ToList();
            }
            else if (SelectedStatus == "Đang học")
            {
                list = list.Where(v => !v.IsMastered && v.PracticeCount > 0).ToList();
            }
            else if (SelectedStatus == "Chưa học")
            {
                list = list.Where(v => !v.IsMastered && v.PracticeCount == 0).ToList();
            }

            _allLoaded = list;
            TotalItems = list.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)_pageSize));
            if (CurrentPage > TotalPages) CurrentPage = 1;

            ApplyPagination();
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

    private void ApplyPagination()
    {
        Vocabularies.Clear();
        int skip = (CurrentPage - 1) * _pageSize;
        var paged = _allLoaded.Skip(skip).Take(_pageSize);
        foreach (var item in paged)
        {
            Vocabularies.Add(item);
        }
    }
}
