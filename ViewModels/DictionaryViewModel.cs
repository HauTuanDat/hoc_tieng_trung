using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class DictionaryViewModel : ViewModelBase
{
    private readonly IDictionaryApiService _dictionaryService;
    private readonly Action _onBack;

    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private bool _hasSearched;
    private bool _isAutoPlayAudio = true;
    private DictionaryWordEntry? _selectedWord;

    public ObservableCollection<DictionaryWordEntry> SearchResults { get; } = new();
    public ObservableCollection<string> RecentSearches { get; } = new();

    public List<string> QuickSuggestions { get; } = new()
    {
        "你好", "学习", "会计", "发票", "海运", "报关", "质量", "工作", "苹果", "谢谢", "朋友", "中国"
    };

    public DictionaryViewModel(IDictionaryApiService dictionaryService, Action onBack)
    {
        _dictionaryService = dictionaryService;
        _onBack = onBack;

        SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync());
        SelectWordCommand = new RelayCommand(param =>
        {
            if (param is DictionaryWordEntry entry)
            {
                SelectWord(entry);
            }
        });

        QuickSearchCommand = new RelayCommand(async param =>
        {
            if (param is string q && !string.IsNullOrWhiteSpace(q))
            {
                SearchQuery = q;
                await ExecuteSearchAsync();
            }
        });

        PlayWordNormalAudioCommand = new RelayCommand(() => PlayWordAudio("+0%"));
        PlayWordSlowAudioCommand = new RelayCommand(() => PlayWordAudio("-35%"));
        PlayExampleAudioCommand = new RelayCommand(param =>
        {
            if (param is DictionaryExample ex && !string.IsNullOrWhiteSpace(ex.Hanzi))
            {
                _ = ChineseAudioService.Instance.PlayAsync(ex.Hanzi, rate: "+0%");
            }
            else if (param is string sentence && !string.IsNullOrWhiteSpace(sentence))
            {
                _ = ChineseAudioService.Instance.PlayAsync(sentence, rate: "+0%");
            }
        });

        PlayExampleSlowAudioCommand = new RelayCommand(param =>
        {
            if (param is DictionaryExample ex && !string.IsNullOrWhiteSpace(ex.Hanzi))
            {
                _ = ChineseAudioService.Instance.PlayAsync(ex.Hanzi, rate: "-35%");
            }
            else if (param is string sentence && !string.IsNullOrWhiteSpace(sentence))
            {
                _ = ChineseAudioService.Instance.PlayAsync(sentence, rate: "-35%");
            }
        });

        ClearSearchCommand = new RelayCommand(() =>
        {
            SearchQuery = string.Empty;
            SearchResults.Clear();
            SelectedWord = null;
            HasSearched = false;
        });

        BackCommand = new RelayCommand(_onBack);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(HasNoResults));
            }
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set
        {
            if (SetProperty(ref _hasSearched, value))
            {
                OnPropertyChanged(nameof(HasNoResults));
            }
        }
    }

    public bool IsAutoPlayAudio
    {
        get => _isAutoPlayAudio;
        set => SetProperty(ref _isAutoPlayAudio, value);
    }

    public DictionaryWordEntry? SelectedWord
    {
        get => _selectedWord;
        set
        {
            if (SetProperty(ref _selectedWord, value))
            {
                OnPropertyChanged(nameof(HasSelectedWord));
                OnPropertyChanged(nameof(ExamplesCount));
            }
        }
    }

    public bool HasSelectedWord => SelectedWord != null;
    public bool HasResults => SearchResults.Count > 0;
    public bool HasNoResults => HasSearched && !IsLoading && SearchResults.Count == 0;
    public int ExamplesCount => SelectedWord?.Examples.Count ?? 0;

    public ICommand SearchCommand { get; }
    public ICommand SelectWordCommand { get; }
    public ICommand QuickSearchCommand { get; }
    public ICommand PlayWordNormalAudioCommand { get; }
    public ICommand PlayWordSlowAudioCommand { get; }
    public ICommand PlayExampleAudioCommand { get; }
    public ICommand PlayExampleSlowAudioCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand BackCommand { get; }

    public async Task InitializeWithQueryAsync(string? query = null)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            SearchQuery = query;
            await ExecuteSearchAsync();
        }
        else
        {
            // Default initial lookup with a popular introductory word
            SearchQuery = "你好";
            await ExecuteSearchAsync();
        }
    }

    public async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        string q = SearchQuery.Trim();
        IsLoading = true;
        HasSearched = true;

        try
        {
            var list = await _dictionaryService.LookupAsync(q);
            SearchResults.Clear();
            foreach (var item in list)
            {
                SearchResults.Add(item);
            }

            if (SearchResults.Count > 0)
            {
                SelectWord(SearchResults[0]);

                // Record recent search
                if (!RecentSearches.Contains(q))
                {
                    RecentSearches.Insert(0, q);
                    if (RecentSearches.Count > 8) RecentSearches.RemoveAt(RecentSearches.Count - 1);
                }
            }
            else
            {
                SelectedWord = null;
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(HasNoResults));
        }
    }

    public void SelectWord(DictionaryWordEntry entry)
    {
        SelectedWord = entry;

        if (IsAutoPlayAudio && !string.IsNullOrWhiteSpace(entry.Simplified))
        {
            _ = ChineseAudioService.Instance.PlayAsync(entry.Simplified, rate: "+0%");
        }
    }

    public void PlayWordAudio(string rate = "+0%")
    {
        if (SelectedWord != null && !string.IsNullOrWhiteSpace(SelectedWord.Simplified))
        {
            _ = ChineseAudioService.Instance.PlayAsync(SelectedWord.Simplified, rate: rate);
        }
    }
}
