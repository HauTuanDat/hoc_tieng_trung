using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class ScrambleLessonItem : ViewModelBase
{
    private int _number;
    private string _title = string.Empty;
    private bool _isCompleted;
    private bool _isActive;
    private bool _isLocked;

    public int Number
    {
        get => _number;
        set => SetProperty(ref _number, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
}

public class SentenceScrambleViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action _onBack;

    private string _selectedTab = "Tất cả";
    private string _selectedLevel = "HSK 1";
    private int _currentIndex;
    private int _streak;
    private int _score;
    private int _correctCount = 7;
    private int _wrongCount = 1;
    private int _newWordsCount = 5;
    private int _completionRate = 53;
    private string _completedLessonsText = "8 / 15 bài";
    private string _lessonHeader = "Bài 3 - Cuộc sống hàng ngày";

    private bool _isLoading;
    private bool _isChecked;
    private bool _isCorrect;
    private bool _isCompleted;
    private bool _showHint;
    private SentenceScrambleQuestion? _currentQuestion;

    public ObservableCollection<string> Tabs { get; } = new()
    {
        "Tất cả", "HSK 1", "HSK 2", "HSK 3", "HSK 4", "HSK 5", "HSK 6", "Công xưởng", "Kế toán", "XNK"
    };

    public ObservableCollection<ScrambleLessonItem> LessonItems { get; } = new();

    public List<SentenceScrambleQuestion> Questions { get; private set; } = new();

    public SentenceScrambleViewModel(IHskService hskService, Action onBack, string initialLevel = "HSK 1")
    {
        _hskService = hskService;
        _onBack = onBack;
        _selectedLevel = string.IsNullOrWhiteSpace(initialLevel) ? "HSK 1" : initialLevel;
        _selectedTab = _selectedLevel == "HSK 1" ? "Tất cả" : _selectedLevel;

        PopulateLessonsForLevel(_selectedLevel);

        SelectTabCommand = new RelayCommand(param =>
        {
            string tab = param as string ?? "Tất cả";
            SelectedTab = tab;
        });

        SelectLessonCommand = new RelayCommand(param =>
        {
            if (param is ScrambleLessonItem lesson && !lesson.IsLocked)
            {
                foreach (var l in LessonItems) l.IsActive = false;
                lesson.IsActive = true;
                LessonHeader = $"Bài {lesson.Number} - {lesson.Title}";
                _ = LoadBatchAsync();
            }
        });

        SelectTokenCommand = new RelayCommand(param =>
        {
            if (param is SentenceWordToken token)
            {
                SelectToken(token);
            }
        });

        RemoveTokenCommand = new RelayCommand(param =>
        {
            if (param is SentenceWordToken token)
            {
                RemoveToken(token);
            }
        });

        ClearTrayCommand = new RelayCommand(ClearTray);
        CheckAnswerCommand = new RelayCommand(CheckAnswer, () => CanCheck);
        NextQuestionCommand = new RelayCommand(NextQuestion);
        SkipCommand = new RelayCommand(NextQuestion);
        HintCommand = new RelayCommand(GiveHint);
        PlayNormalAudioCommand = new RelayCommand(PlayNormalAudio);
        PlaySlowAudioCommand = new RelayCommand(PlaySlowAudio);
        RestartBatchCommand = new RelayCommand(async () => await LoadBatchAsync());
        BackCommand = new RelayCommand(_onBack);
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                if (value == "Tất cả")
                {
                    _selectedLevel = "HSK 1";
                }
                else if (value == "XNK")
                {
                    _selectedLevel = "Xuất nhập khẩu";
                }
                else
                {
                    _selectedLevel = value;
                }
                PopulateLessonsForLevel(_selectedLevel);
                _ = LoadBatchAsync();
            }
        }
    }

    public string SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                PopulateLessonsForLevel(value);
            }
        }
    }

    public void PopulateLessonsForLevel(string level)
    {
        LessonItems.Clear();
        var topics = GetTopicsForLevel(level);
        for (int i = 0; i < topics.Length; i++)
        {
            int num = i + 1;
            LessonItems.Add(new ScrambleLessonItem
            {
                Number = num,
                Title = topics[i],
                IsCompleted = num < 3,
                IsActive = num == 3,
                IsLocked = num > 3
            });
        }
        var activeLesson = LessonItems.FirstOrDefault(l => l.IsActive) ?? LessonItems.FirstOrDefault();
        if (activeLesson != null)
        {
            LessonHeader = $"Bài {activeLesson.Number} - {activeLesson.Title}";
        }
    }

    private string[] GetTopicsForLevel(string level)
    {
        return level switch
        {
            "HSK 1" => new[] { "Chào hỏi và giới thiệu", "Gia đình và bạn bè", "Cuộc sống hàng ngày", "Mua sắm", "Du lịch", "Ẩm thực" },
            "HSK 2" => new[] { "Sức khỏe và sinh hoạt", "Công việc và học tập", "Du lịch và phương tiện", "Thời tiết 4 mùa", "Cảm xúc và tính cách", "Sở thích và thể thao" },
            "HSK 3" => new[] { "Giao tiếp công sở", "Môi trường tự nhiên", "Xã hội và phong tục", "Kế hoạch và dự định", "Thảo luận công việc", "Thành tựu và ước mơ" },
            "HSK 4" => new[] { "Đời sống hiện đại", "Khoa học công nghệ", "Nghệ thuật du lịch", "Kinh tế và nghề nghiệp", "Tâm lý sức khỏe", "Thành ngữ thông dụng" },
            "HSK 5" => new[] { "Thời sự chính luận", "Đàm phán thương mại", "Văn học tư tưởng", "Bảo vệ sinh thái", "Khoa học vũ trụ", "Diễn thuyết truyền cảm hứng" },
            "HSK 6" => new[] { "Văn ngôn điển cố", "Báo cáo học thuật", "Ngôn ngữ chuyên sâu", "Văn hóa Trung Hoa", "Bình luận quốc tế", "Đỉnh cao giao tiếp HSK" },
            "Công xưởng" => new[] { "Dây chuyền sản xuất", "Quy trình vận hành", "An toàn lao động", "Kiểm soát chất lượng QC", "Bảo trì máy móc", "Bàn giao ca kíp" },
            "Kế toán" => new[] { "Hóa đơn và chứng từ", "Khai báo thuế doanh nghiệp", "Báo cáo tài chính", "Tiền lương phụ cấp", "Quản lý công nợ", "Kiểm toán ngân hàng" },
            "Xuất nhập khẩu" => new[] { "Logistics vận tải", "Khai báo hải quan", "Hợp đồng ngoại thương", "Thanh toán quốc tế L/C", "Đóng gói kho bãi", "Bảo hiểm khiếu nại" },
            _ => new[] { "Chào hỏi cơ bản", "Hội thoại hàng ngày", "Giao tiếp nâng cao", "Ứng dụng thực tế", "Kỹ năng chuyên sâu", "Tổng hợp phản xạ" }
        };
    }

    public string LessonHeader
    {
        get => _lessonHeader;
        set => SetProperty(ref _lessonHeader, value);
    }

    public SentenceScrambleQuestion? CurrentQuestion
    {
        get => _currentQuestion;
        private set
        {
            if (SetProperty(ref _currentQuestion, value))
            {
                OnPropertyChanged(nameof(SelectedTray));
                OnPropertyChanged(nameof(AvailablePool));
                OnPropertyChanged(nameof(CanCheck));
                OnPropertyChanged(nameof(HasSelectedTokens));
            }
        }
    }

    public ObservableCollection<SentenceWordToken>? SelectedTray => CurrentQuestion?.SelectedTray;
    public ObservableCollection<SentenceWordToken>? AvailablePool => CurrentQuestion?.AvailablePool;
    public bool HasSelectedTokens => (SelectedTray?.Count ?? 0) > 0;

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                OnPropertyChanged(nameof(CurrentQuestionNumber));
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    public int CurrentQuestionNumber => _currentIndex + 1;
    public int TotalQuestions => Questions.Count > 0 ? Questions.Count : 15;
    public string ProgressText => $"{CurrentQuestionNumber} / {TotalQuestions}";
    public double ProgressPercentage
    {
        get => TotalQuestions == 0 ? 0 : ((double)CurrentQuestionNumber / TotalQuestions) * 100.0;
        set { }
    }

    public int Streak
    {
        get => _streak;
        set => SetProperty(ref _streak, value);
    }

    public int Score
    {
        get => _score;
        set => SetProperty(ref _score, value);
    }

    public int CorrectCount
    {
        get => _correctCount;
        set => SetProperty(ref _correctCount, value);
    }

    public int WrongCount
    {
        get => _wrongCount;
        set => SetProperty(ref _wrongCount, value);
    }

    public int NewWordsCount
    {
        get => _newWordsCount;
        set => SetProperty(ref _newWordsCount, value);
    }

    public int CompletionRate
    {
        get => _completionRate;
        set => SetProperty(ref _completionRate, value);
    }

    public string CompletedLessonsText
    {
        get => _completedLessonsText;
        set => SetProperty(ref _completedLessonsText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
            {
                OnPropertyChanged(nameof(CanCheck));
            }
        }
    }

    public bool IsCorrect
    {
        get => _isCorrect;
        set => SetProperty(ref _isCorrect, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public bool ShowHint
    {
        get => _showHint;
        set => SetProperty(ref _showHint, value);
    }

    public bool CanCheck => !IsChecked && (SelectedTray?.Count ?? 0) > 0;

    public ICommand SelectTabCommand { get; }
    public ICommand SelectLessonCommand { get; }
    public ICommand SelectTokenCommand { get; }
    public ICommand RemoveTokenCommand { get; }
    public ICommand ClearTrayCommand { get; }
    public ICommand CheckAnswerCommand { get; }
    public ICommand NextQuestionCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand HintCommand { get; }
    public ICommand PlayNormalAudioCommand { get; }
    public ICommand PlaySlowAudioCommand { get; }
    public ICommand RestartBatchCommand { get; }
    public ICommand BackCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadBatchAsync();
    }

    public async Task LoadBatchAsync()
    {
        IsLoading = true;
        IsCompleted = false;
        CurrentIndex = 0;
        Streak = 0;
        Score = 0;
        IsChecked = false;
        IsCorrect = false;
        ShowHint = false;

        try
        {
            var batch = await _hskService.GetSentenceScrambleBatchAsync(_selectedLevel, 15);
            Questions = batch ?? new List<SentenceScrambleQuestion>();

            // If empty (e.g. database has few example sentences for this level), provide high quality sample fallback
            if (Questions.Count == 0)
            {
                Questions = GenerateFallbackQuestions(_selectedLevel);
            }

            if (Questions.Count > 0)
            {
                LoadQuestion(0);
            }
            else
            {
                CurrentQuestion = null;
            }
        }
        catch
        {
            Questions = GenerateFallbackQuestions(_selectedLevel);
            if (Questions.Count > 0) LoadQuestion(0);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= Questions.Count) return;

        CurrentIndex = index;
        IsChecked = false;
        IsCorrect = false;
        ShowHint = false;

        var q = Questions[index];
        foreach (var t in q.AvailablePool)
        {
            t.IsSelected = false;
            t.IsCorrect = null;
        }
        q.SelectedTray.Clear();

        CurrentQuestion = q;
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(HasSelectedTokens));
    }

    public void SelectToken(SentenceWordToken token)
    {
        if (IsChecked || CurrentQuestion == null) return;
        if (token.IsSelected) return;

        token.IsSelected = true;
        CurrentQuestion.SelectedTray.Add(token);
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(HasSelectedTokens));

        _ = ChineseAudioService.Instance.PlayAsync(token.Hanzi);
    }

    public void RemoveToken(SentenceWordToken token)
    {
        if (IsChecked || CurrentQuestion == null) return;

        token.IsSelected = false;
        CurrentQuestion.SelectedTray.Remove(token);
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(HasSelectedTokens));
    }

    public void ClearTray()
    {
        if (IsChecked || CurrentQuestion == null) return;

        foreach (var token in CurrentQuestion.SelectedTray.ToList())
        {
            token.IsSelected = false;
        }
        CurrentQuestion.SelectedTray.Clear();
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(HasSelectedTokens));
    }

    public void CheckAnswer()
    {
        if (CurrentQuestion == null || IsChecked || SelectedTray == null || SelectedTray.Count == 0) return;

        string userSentence = string.Concat(SelectedTray.Select(t => t.Hanzi));
        string targetSentence = CurrentQuestion.CorrectCleanSentence;

        IsChecked = true;
        IsCorrect = string.Equals(userSentence, targetSentence, StringComparison.OrdinalIgnoreCase);

        if (IsCorrect)
        {
            Streak++;
            Score += 10;
            CorrectCount++;
            SoundService.Instance.PlayCorrect();

            foreach (var t in SelectedTray) t.IsCorrect = true;
            _ = ChineseAudioService.Instance.PlayAsync(CurrentQuestion.TargetSentence);
        }
        else
        {
            Streak = 0;
            WrongCount++;
            SoundService.Instance.PlayIncorrect();

            foreach (var t in SelectedTray) t.IsCorrect = false;
        }

        OnPropertyChanged(nameof(ProgressPercentage));
    }

    public void GiveHint()
    {
        if (CurrentQuestion == null || IsChecked) return;

        // Auto move next correct token into tray
        int nextIndex = SelectedTray?.Count ?? 0;
        if (nextIndex < CurrentQuestion.CorrectTokens.Count)
        {
            var correctToken = CurrentQuestion.CorrectTokens[nextIndex];
            var poolToken = CurrentQuestion.AvailablePool.FirstOrDefault(t => !t.IsSelected && t.Hanzi == correctToken.Hanzi);
            if (poolToken != null)
            {
                SelectToken(poolToken);
            }
        }
        ShowHint = true;
    }

    public void NextQuestion()
    {
        if (CurrentIndex + 1 < Questions.Count)
        {
            LoadQuestion(CurrentIndex + 1);
        }
        else
        {
            IsCompleted = true;
            SoundService.Instance.PlayCompleted();
        }
    }

    public void PlayNormalAudio()
    {
        if (CurrentQuestion != null && !string.IsNullOrWhiteSpace(CurrentQuestion.TargetSentence))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentQuestion.TargetSentence, rate: "+0%");
        }
    }

    public void PlaySlowAudio()
    {
        if (CurrentQuestion != null && !string.IsNullOrWhiteSpace(CurrentQuestion.TargetSentence))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentQuestion.TargetSentence, rate: "-35%");
        }
    }

    private static List<SentenceScrambleQuestion> GenerateFallbackQuestions(string level)
    {
        var list = new List<SentenceScrambleQuestion>();

        // Sample sentence 1 (From screenshot): 我想去公园 (Tôi muốn đi công viên vào cuối tuần)
        var q1 = new SentenceScrambleQuestion
        {
            Level = level,
            TargetSentence = "我周末想去公园。",
            TargetPinyin = "Wǒ zhōumò xiǎng qù gōngyuán.",
            VietnameseMeaning = "Tôi muốn đi công viên vào cuối tuần."
        };
        var t1 = new List<SentenceWordToken>
        {
            new() { Id = 1, Hanzi = "公园", Pinyin = "gōngyuán" },
            new() { Id = 2, Hanzi = "我", Pinyin = "wǒ" },
            new() { Id = 3, Hanzi = "去", Pinyin = "qù" },
            new() { Id = 4, Hanzi = "想", Pinyin = "xiǎng" },
            new() { Id = 5, Hanzi = "周末", Pinyin = "zhōumò" }
        };
        q1.CorrectTokens = new List<SentenceWordToken> { t1[1], t1[4], t1[3], t1[2], t1[0] }; // 我 周末 想 去 公园
        foreach (var t in t1) q1.AvailablePool.Add(t);
        list.Add(q1);

        // Sample sentence 2
        var q2 = new SentenceScrambleQuestion
        {
            Level = level,
            TargetSentence = "明天天气怎么样？",
            TargetPinyin = "Míngtiān tiānqì zěnmeyàng?",
            VietnameseMeaning = "Thời tiết ngày mai thế nào?"
        };
        var t2 = new List<SentenceWordToken>
        {
            new() { Id = 6, Hanzi = "明天", Pinyin = "míngtiān" },
            new() { Id = 7, Hanzi = "怎么样", Pinyin = "zěnmeyàng" },
            new() { Id = 8, Hanzi = "天气", Pinyin = "tiānqì" }
        };
        q2.CorrectTokens = new List<SentenceWordToken> { t2[0], t2[2], t2[1] };
        foreach (var t in t2.OrderBy(_ => Guid.NewGuid())) q2.AvailablePool.Add(t);
        list.Add(q2);

        // Sample sentence 3
        var q3 = new SentenceScrambleQuestion
        {
            Level = level,
            TargetSentence = "我在学校学习中文。",
            TargetPinyin = "Wǒ zài xuéxiào xuéxí Zhōngwén.",
            VietnameseMeaning = "Tôi học tiếng Trung ở trường."
        };
        var t3 = new List<SentenceWordToken>
        {
            new() { Id = 9, Hanzi = "学习", Pinyin = "xuéxí" },
            new() { Id = 10, Hanzi = "我", Pinyin = "wǒ" },
            new() { Id = 11, Hanzi = "中文", Pinyin = "Zhōngwén" },
            new() { Id = 12, Hanzi = "在学校", Pinyin = "zài xuéxiào" }
        };
        q3.CorrectTokens = new List<SentenceWordToken> { t3[1], t3[3], t3[0], t3[2] };
        foreach (var t in t3.OrderBy(_ => Guid.NewGuid())) q3.AvailablePool.Add(t);
        list.Add(q3);

        return list;
    }
}
