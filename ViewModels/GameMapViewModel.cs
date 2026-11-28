using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class ArenaModeItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string QuizMode { get; set; } = "MeaningToHanzi";
    public string TagText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ArenaLeaderboardItem
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
    public string AvatarKind { get; set; } = "panda";
    public string LevelTag { get; set; } = "HSK 3";
    public string WinRateText { get; set; } = "80%";
    public bool IsCurrentUser { get; set; }
    public bool IsTop1 => Rank == 1;
    public bool IsTop2 => Rank == 2;
    public bool IsTop3 => Rank == 3;
    public bool IsTopThree => Rank <= 3;
    public string RankBadgeBg => Rank switch
    {
        1 => "#FACC15",
        2 => "#CBD5E1",
        3 => "#EA9A64",
        _ => "Transparent"
    };
    public string RankBadgeBorder => Rank switch
    {
        1 => "#EAB308",
        2 => "#94A3B8",
        3 => "#C26E38",
        _ => "Transparent"
    };
    public string RankBadgeFg => Rank switch
    {
        1 => "#713F12",
        2 => "#334155",
        3 => "#7C2D12",
        _ => "#64748B"
    };
}

public class ArenaHskLevelOption : ViewModelBase
{
    private bool _isSelected;
    public string Level { get; set; } = "HSK 1";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class ArenaBadgeItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconKey { get; set; } = "medal";
    public bool IsUnlocked { get; set; }
    public string TintBg { get; set; } = "#FFF5DF";
    public string IconColor { get; set; } = "#D97706";
}

public class GameSubLevelItem : ViewModelBase
{
    private int _starsEarned;
    private bool _isLocked;
    private bool _isCompleted;
    private bool _isCurrent;
    private int _highScore;

    public int LevelNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChallengeType { get; set; } = "Trắc nghiệm";
    public string Icon { get; set; } = "🎯";
    public bool IsBossLevel { get; set; }
    public int RewardExp { get; set; } = 50;
    public int RewardGems { get; set; } = 10;
    public string TargetHsk { get; set; } = "HSK 1";

    public string MedalNumberText => $"#{LevelNumber}";
    public string MedalBg { get; set; } = "#FEF3C7";
    public string MedalBorder { get; set; } = "#F59E0B";
    public string MedalFg { get; set; } = "#B45309";
    public string RibbonColor { get; set; } = "#F59E0B";

    public int StarsEarned
    {
        get => _starsEarned;
        set
        {
            if (SetProperty(ref _starsEarned, value))
            {
                OnPropertyChanged(nameof(Star1Active));
                OnPropertyChanged(nameof(Star2Active));
                OnPropertyChanged(nameof(Star3Active));
            }
        }
    }

    public bool Star1Active => StarsEarned >= 1;
    public bool Star2Active => StarsEarned >= 2;
    public bool Star3Active => StarsEarned >= 3;

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }

    public int HighScore
    {
        get => _highScore;
        set => SetProperty(ref _highScore, value);
    }
}

public class HskWorldBoardItem : ViewModelBase
{
    private bool _isSelected;
    private int _completedLevelsCount;
    private int _totalStarsEarned;

    public int HskLevel { get; set; }
    public string LevelKey { get; set; } = "HSK 1";
    public string WorldName { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VocabCount { get; set; }
    public string AccentColor { get; set; } = "#198754";
    public bool IsLocked { get; set; }
    public ObservableCollection<GameSubLevelItem> SubLevels { get; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public int CompletedLevelsCount
    {
        get => _completedLevelsCount;
        set
        {
            if (SetProperty(ref _completedLevelsCount, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public int TotalStarsEarned
    {
        get => _totalStarsEarned;
        set
        {
            if (SetProperty(ref _totalStarsEarned, value))
            {
                OnPropertyChanged(nameof(StarsText));
            }
        }
    }

    public int MaxStars => 30;
    public string StarsText => $"{TotalStarsEarned}/{MaxStars}";
    public string ProgressText => $"{CompletedLevelsCount}/10 Ải";
    public double ProgressPercent => CompletedLevelsCount * 10.0;
}

public class GameMapViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action<string, string> _onStartQuiz;
    private readonly Action _onBack;
    private readonly DispatcherTimer _seasonTimer;

    private TimeSpan _seasonRemaining = new(12, 8, 24, 17);

    // Player stats
    private string _playerName = "Minh Đạt";
    private int _playerLevel = 7;
    private int _playerExp = 2460;
    private int _expToNextLevel = 3000;
    private int _currentRank = 128;
    private int _rankDelta = 12;
    private int _matchesPlayed = 42;
    private int _winRatePercent = 76;
    private int _bestWinStreak = 12;
    private int _currentScore = 2860;
    private int _gems = 350;

    // Leaderboard state
    private string _selectedLeaderboardPeriod = "Tuần này";

    // Modal state
    private bool _isModeModalOpen;
    private bool _isRewardsModalOpen;
    private ArenaModeItem? _selectedMode;
    private string _selectedMatchLevel = "HSK 3";

    private HskWorldBoardItem? _selectedWorld;

    public GameMapViewModel(IHskService hskService, Action<string, string> onStartQuiz, Action onBack, User? user = null)
    {
        _hskService = hskService;
        _onStartQuiz = onStartQuiz;
        _onBack = onBack;

        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                _playerName = user.FullName;
            }
            if (!string.IsNullOrWhiteSpace(user.CurrentLevel))
            {
                SelectedMatchLevel = user.CurrentLevel;
            }
            if (user.StreakDays > 0)
            {
                _bestWinStreak = Math.Max(_bestWinStreak, user.StreakDays);
            }
        }

        SelectLeaderboardPeriodCommand = new RelayCommand(param =>
        {
            if (param is string period)
            {
                SelectedLeaderboardPeriod = period;
            }
        });

        OpenModeModalCommand = new RelayCommand(param =>
        {
            if (param is string modeId)
            {
                SelectedMode = ArenaModes.FirstOrDefault(m => m.Id == modeId) ?? ArenaModes.FirstOrDefault();
                IsModeModalOpen = true;
            }
            else if (param is ArenaModeItem mode)
            {
                SelectedMode = mode;
                IsModeModalOpen = true;
            }
        });

        CloseModeModalCommand = new RelayCommand(() => IsModeModalOpen = false);

        SelectMatchLevelCommand = new RelayCommand(param =>
        {
            if (param is ArenaHskLevelOption opt)
            {
                SelectedMatchLevel = opt.Level;
            }
            else if (param is string lvl)
            {
                SelectedMatchLevel = lvl;
            }
        });

        StartArenaMatchCommand = new RelayCommand(() =>
        {
            string quizMode = SelectedMode?.QuizMode ?? "MeaningToHanzi";
            string targetLevel = string.IsNullOrWhiteSpace(SelectedMatchLevel) ? "HSK 1" : SelectedMatchLevel;
            IsModeModalOpen = false;
            IsRewardsModalOpen = false;
            _onStartQuiz(targetLevel, quizMode);
        });

        OpenRewardsModalCommand = new RelayCommand(() => IsRewardsModalOpen = true);
        CloseRewardsModalCommand = new RelayCommand(() => IsRewardsModalOpen = false);

        BackCommand = new RelayCommand(_onBack);

        InitializeArenaData();
        LoadLeaderboard();

        _seasonTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _seasonTimer.Tick += (_, _) =>
        {
            if (_seasonRemaining.TotalSeconds > 1)
            {
                _seasonRemaining = _seasonRemaining.Subtract(TimeSpan.FromSeconds(1));
                OnPropertyChanged(nameof(SeasonCountdownText));
            }
        };
        _seasonTimer.Start();
    }

    public ObservableCollection<ArenaModeItem> ArenaModes { get; } = new();
    public ObservableCollection<ArenaLeaderboardItem> LeaderboardEntries { get; } = new();
    public ObservableCollection<ArenaBadgeItem> Badges { get; } = new();
    public ObservableCollection<ArenaHskLevelOption> AvailableHskLevels { get; } = new()
    {
        new ArenaHskLevelOption { Level = "HSK 1" },
        new ArenaHskLevelOption { Level = "HSK 2" },
        new ArenaHskLevelOption { Level = "HSK 3", IsSelected = true },
        new ArenaHskLevelOption { Level = "HSK 4" },
        new ArenaHskLevelOption { Level = "HSK 5" },
        new ArenaHskLevelOption { Level = "HSK 6" }
    };
    public ObservableCollection<HskWorldBoardItem> Worlds { get; } = new();

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    public int PlayerLevel
    {
        get => _playerLevel;
        set => SetProperty(ref _playerLevel, value);
    }

    public int PlayerExp
    {
        get => _playerExp;
        set
        {
            if (SetProperty(ref _playerExp, value))
            {
                OnPropertyChanged(nameof(ExpPercent));
                OnPropertyChanged(nameof(ExpProgressText));
            }
        }
    }

    public int ExpToNextLevel
    {
        get => _expToNextLevel;
        set
        {
            if (SetProperty(ref _expToNextLevel, value))
            {
                OnPropertyChanged(nameof(ExpPercent));
                OnPropertyChanged(nameof(ExpProgressText));
            }
        }
    }

    public double ExpPercent => ExpToNextLevel > 0 ? Math.Clamp((double)PlayerExp / ExpToNextLevel * 100.0, 0, 100) : 0;
    public string ExpProgressText => $"{PlayerExp:N0} / {ExpToNextLevel:N0} EXP";

    public string CurrentRankText => $"Top {_currentRank}";
    public string RankDeltaText => $"+{_rankDelta}";

    public int MatchesPlayed
    {
        get => _matchesPlayed;
        set => SetProperty(ref _matchesPlayed, value);
    }

    public int WinRatePercent
    {
        get => _winRatePercent;
        set
        {
            if (SetProperty(ref _winRatePercent, value))
            {
                OnPropertyChanged(nameof(WinRateText));
            }
        }
    }

    public string WinRateText => $"{WinRatePercent}%";

    public int BestWinStreak
    {
        get => _bestWinStreak;
        set
        {
            if (SetProperty(ref _bestWinStreak, value))
            {
                OnPropertyChanged(nameof(BestWinStreakText));
            }
        }
    }

    public string BestWinStreakText => $"{BestWinStreak} trận";

    public int CurrentScore
    {
        get => _currentScore;
        set
        {
            if (SetProperty(ref _currentScore, value))
            {
                OnPropertyChanged(nameof(CurrentScoreText));
            }
        }
    }

    public string CurrentScoreText => CurrentScore.ToString("N0");

    public int Gems
    {
        get => _gems;
        set => SetProperty(ref _gems, value);
    }

    public string SeasonTitle => "Mùa 7 - Thách Đấu";
    public string SeasonCountdownText =>
        $"{(int)_seasonRemaining.TotalDays} ngày  {_seasonRemaining.Hours:D2}:{_seasonRemaining.Minutes:D2}:{_seasonRemaining.Seconds:D2}";

    public string SelectedLeaderboardPeriod
    {
        get => _selectedLeaderboardPeriod;
        set
        {
            if (SetProperty(ref _selectedLeaderboardPeriod, value))
            {
                OnPropertyChanged(nameof(IsWeekTabActive));
                OnPropertyChanged(nameof(IsMonthTabActive));
                OnPropertyChanged(nameof(IsAllTabActive));
                LoadLeaderboard();
            }
        }
    }

    public bool IsWeekTabActive => SelectedLeaderboardPeriod == "Tuần này";
    public bool IsMonthTabActive => SelectedLeaderboardPeriod == "Tháng này";
    public bool IsAllTabActive => SelectedLeaderboardPeriod == "Tất cả";

    public bool IsModeModalOpen
    {
        get => _isModeModalOpen;
        set => SetProperty(ref _isModeModalOpen, value);
    }

    public bool IsRewardsModalOpen
    {
        get => _isRewardsModalOpen;
        set => SetProperty(ref _isRewardsModalOpen, value);
    }

    public ArenaModeItem? SelectedMode
    {
        get => _selectedMode;
        set => SetProperty(ref _selectedMode, value);
    }

    public string SelectedMatchLevel
    {
        get => _selectedMatchLevel;
        set
        {
            if (SetProperty(ref _selectedMatchLevel, value))
            {
                foreach (var item in AvailableHskLevels)
                {
                    item.IsSelected = item.Level == value;
                }
            }
        }
    }

    public HskWorldBoardItem? SelectedWorld
    {
        get => _selectedWorld;
        set => SetProperty(ref _selectedWorld, value);
    }

    public ICommand SelectLeaderboardPeriodCommand { get; }
    public ICommand OpenModeModalCommand { get; }
    public ICommand CloseModeModalCommand { get; }
    public ICommand SelectMatchLevelCommand { get; }
    public ICommand StartArenaMatchCommand { get; }
    public ICommand OpenRewardsModalCommand { get; }
    public ICommand CloseRewardsModalCommand { get; }
    public ICommand BackCommand { get; }

    private void InitializeArenaData()
    {
        ArenaModes.Clear();
        ArenaModes.Add(new ArenaModeItem
        {
            Id = "quick",
            Title = "Thi đấu nhanh",
            Subtitle = "Thử thách trong 3 phút",
            DurationText = "3 phút/trận",
            QuizMode = "HanziToMeaning",
            TagText = "GHÉP TRẬN TỨC THÌ",
            Description = "Phản xạ nhanh 20 câu hỏi Hán tự và nghĩa tiếng Việt trong 3 phút để tích điểm hạng tức thì."
        });
        ArenaModes.Add(new ArenaModeItem
        {
            Id = "tournament",
            Title = "Chế độ giải đấu",
            Subtitle = "Tranh tài xếp hạng",
            DurationText = "5 phút/trận",
            QuizMode = "MeaningToHanzi",
            TagText = "SÂN CHƠI CẠNH TRANH",
            Description = "Thi đấu tính điểm xếp hạng chính thức Mùa 7. Vượt ngưỡng 80% để nhận thưởng huy hiệu & EXP x2."
        });
        ArenaModes.Add(new ArenaModeItem
        {
            Id = "special",
            Title = "Thử thách đặc biệt",
            Subtitle = "Vượt qua các chủ đề khó",
            DurationText = "5 phút/trận",
            QuizMode = "PinyinToMeaning",
            TagText = "CHỦ ĐỀ NÂNG CAO",
            Description = "Chinh phục bộ câu hỏi tổng hợp Pinyin, từ đồng nghĩa và ngữ cảnh chuyên sâu theo cấp độ HSK."
        });

        Badges.Clear();
        Badges.Add(new ArenaBadgeItem { Title = "Tân Binh Tỏa Sáng", Subtitle = "Hoàn thành 10 trận đấu đầu tiên", IconKey = "medal", IsUnlocked = true, TintBg = "#FEF3C7", IconColor = "#D97706" });
        Badges.Add(new ArenaBadgeItem { Title = "Chuỗi Thắng Rực Lửa", Subtitle = "Đạt chuỗi thắng 10 trận liên tiếp", IconKey = "flame", IsUnlocked = true, TintBg = "#FFEDD5", IconColor = "#EA580C" });
        Badges.Add(new ArenaBadgeItem { Title = "Phản Xạ Siêu Tốc", Subtitle = "Đạt độ chính xác trên 90% ở Thi đấu nhanh", IconKey = "zap", IsUnlocked = true, TintBg = "#E0F2FE", IconColor = "#0284C7" });
        Badges.Add(new ArenaBadgeItem { Title = "Chinh Phục Top 100", Subtitle = "Lọt vào bảng vàng Top 100 mùa giải", IconKey = "crown", IsUnlocked = false, TintBg = "#F1F5F9", IconColor = "#94A3B8" });
    }

    private void LoadLeaderboard()
    {
        LeaderboardEntries.Clear();

        if (SelectedLeaderboardPeriod == "Tháng này")
        {
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 1, Name = "HauTuanDat", ScoreText = "48.2k", AvatarKind = "panda", LevelTag = "HSK 5", WinRateText = "91%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 2, Name = "NgọcThắng", ScoreText = "44.6k", AvatarKind = "panda", LevelTag = "HSK 4", WinRateText = "88%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 3, Name = "LýVănThuan", ScoreText = "41.9k", AvatarKind = "panda", LevelTag = "HSK 4", WinRateText = "86%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 4, Name = "MaiDat", ScoreText = "38.4k", AvatarKind = "girl", LevelTag = "HSK 3", WinRateText = "82%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 5, Name = "DatK22", ScoreText = "35.1k", AvatarKind = "boy", LevelTag = "HSK 3", WinRateText = "79%" });
        }
        else if (SelectedLeaderboardPeriod == "Tất cả")
        {
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 1, Name = "HauTuanDat", ScoreText = "152k", AvatarKind = "panda", LevelTag = "HSK 6", WinRateText = "94%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 2, Name = "LýVănThuan", ScoreText = "128k", AvatarKind = "panda", LevelTag = "HSK 5", WinRateText = "90%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 3, Name = "NgọcThắng", ScoreText = "114k", AvatarKind = "panda", LevelTag = "HSK 5", WinRateText = "87%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 4, Name = "MaiDat", ScoreText = "97.5k", AvatarKind = "girl", LevelTag = "HSK 4", WinRateText = "84%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 5, Name = "DatK22", ScoreText = "89.2k", AvatarKind = "boy", LevelTag = "HSK 4", WinRateText = "81%" });
        }
        else
        {
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 1, Name = "HauTuanDat", ScoreText = "12.5k", AvatarKind = "panda", LevelTag = "HSK 5", WinRateText = "92%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 2, Name = "LýVănThuan", ScoreText = "11.8k", AvatarKind = "panda", LevelTag = "HSK 4", WinRateText = "89%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 3, Name = "NgọcThắng", ScoreText = "10.9k", AvatarKind = "panda", LevelTag = "HSK 4", WinRateText = "86%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 4, Name = "MaiDat", ScoreText = "9.7k", AvatarKind = "girl", LevelTag = "HSK 3", WinRateText = "81%" });
            LeaderboardEntries.Add(new ArenaLeaderboardItem { Rank = 5, Name = "DatK22", ScoreText = "8.9k", AvatarKind = "boy", LevelTag = "HSK 3", WinRateText = "78%" });
        }
    }
}
