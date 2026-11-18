using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Services;

namespace QuizApp.ViewModels;

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

    public string StatusBadgeText
    {
        get
        {
            if (IsCompleted) return "Đã hoàn thành";
            if (IsCurrent) return "Ải hiện tại • Sẵn sàng!";
            return "Đang khóa";
        }
    }

    public string StatusBadgeBg
    {
        get
        {
            if (IsCompleted) return "#E8F5EE";
            if (IsCurrent) return "#FEF3C7";
            return "#F1F5F9";
        }
    }

    public string StatusBadgeFg
    {
        get
        {
            if (IsCompleted) return "#198754";
            if (IsCurrent) return "#D97706";
            return "#64748B";
        }
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
    public string Icon { get; set; } = "🌱";
    public string AccentColor { get; set; } = "#198754";
    public string TintBg { get; set; } = "#E8F5EE";
    public string GradientStart { get; set; } = "#198754";
    public string GradientEnd { get; set; } = "#157347";
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

    public int MaxStars => 30; // 10 levels * 3
    public string StarsText => $"{TotalStarsEarned}/{MaxStars} ⭐";
    public string ProgressText => $"{CompletedLevelsCount}/10 Ải";
    public string TrophyText => $"{CompletedLevelsCount}/10 Ải 🏆";
    public double ProgressPercent => CompletedLevelsCount * 10.0;
}

public class GameMapViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action<string, string> _onStartQuiz;
    private readonly Action _onBack;

    private HskWorldBoardItem? _selectedWorld;
    private GameSubLevelItem? _selectedSubLevel;
    private bool _isLevelDetailOpen;

    // Player stats
    private string _playerName = "Minh Đạt";
    private int _playerLevel = 7;
    private int _playerExp = 2460;
    private int _expToNextLevel = 3000;
    private int _totalStars = 68;
    private int _streakDays = 7;
    private int _gems = 350;

    public GameMapViewModel(IHskService hskService, Action<string, string> onStartQuiz, Action onBack)
    {
        _hskService = hskService;
        _onStartQuiz = onStartQuiz;
        _onBack = onBack;

        SelectWorldCommand = new RelayCommand(param =>
        {
            if (param is HskWorldBoardItem world)
            {
                SelectWorld(world);
            }
            else if (param is string levelKey)
            {
                var found = Worlds.FirstOrDefault(w => w.LevelKey.Equals(levelKey, StringComparison.OrdinalIgnoreCase));
                if (found != null) SelectWorld(found);
            }
        });

        SelectSubLevelCommand = new RelayCommand(param =>
        {
            if (param is GameSubLevelItem subLevel)
            {
                SelectedSubLevel = subLevel;
                IsLevelDetailOpen = true;
            }
        });

        CloseLevelDetailCommand = new RelayCommand(() => IsLevelDetailOpen = false);

        PlayLevelCommand = new RelayCommand(param =>
        {
            var level = param as GameSubLevelItem ?? SelectedSubLevel;
            if (level != null && !level.IsLocked)
            {
                IsLevelDetailOpen = false;
                _onStartQuiz(level.TargetHsk, "MeaningToHanzi");
            }
        });

        BackCommand = new RelayCommand(_onBack);

        InitializeWorlds();
    }

    public ObservableCollection<HskWorldBoardItem> Worlds { get; } = new();

    public HskWorldBoardItem? SelectedWorld
    {
        get => _selectedWorld;
        set => SetProperty(ref _selectedWorld, value);
    }

    public GameSubLevelItem? SelectedSubLevel
    {
        get => _selectedSubLevel;
        set => SetProperty(ref _selectedSubLevel, value);
    }

    public bool IsLevelDetailOpen
    {
        get => _isLevelDetailOpen;
        set => SetProperty(ref _isLevelDetailOpen, value);
    }

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
        set => SetProperty(ref _playerExp, value);
    }

    public int ExpToNextLevel
    {
        get => _expToNextLevel;
        set => SetProperty(ref _expToNextLevel, value);
    }

    public double ExpPercent => ExpToNextLevel > 0 ? (double)PlayerExp / ExpToNextLevel * 100 : 0;

    public int TotalStars
    {
        get => _totalStars;
        set => SetProperty(ref _totalStars, value);
    }

    public int StreakDays
    {
        get => _streakDays;
        set => SetProperty(ref _streakDays, value);
    }

    public int Gems
    {
        get => _gems;
        set => SetProperty(ref _gems, value);
    }

    public ICommand SelectWorldCommand { get; }
    public ICommand SelectSubLevelCommand { get; }
    public ICommand CloseLevelDetailCommand { get; }
    public ICommand PlayLevelCommand { get; }
    public ICommand BackCommand { get; }

    private void SelectWorld(HskWorldBoardItem world)
    {
        foreach (var w in Worlds)
        {
            w.IsSelected = (w == world);
        }
        SelectedWorld = world;
    }

    private void InitializeWorlds()
    {
        Worlds.Clear();

        var definitions = new[]
        {
            (1, "HSK 1", "Đảo Khởi Nguyên", "Khởi đầu nhập môn", "Làm quen 150 chữ Hán căn bản, phát âm chuẩn Pinyin và các câu chào hỏi thường nhật.", 160, "🏯", "#0284C7", "#E0F2FE", "#0284C7", "#38BDF8", false),
            (2, "HSK 2", "Rừng Giao Tiếp", "Phản xạ sinh hoạt", "Nắm vững 300 từ vựng cho sinh hoạt, mua sắm, gọi món và chỉ đường đời sống.", 300, "🌿", "#10B981", "#DCFCE7", "#10B981", "#34D399", false),
            (3, "HSK 3", "Thung Lũng Tự Tin", "Hội thoại lưu loát", "Nâng tầm 600 từ vựng, tự tin trò chuyện công việc, sở thích, trường học và du lịch.", 600, "🏛️", "#8B5CF6", "#EDE9FE", "#8B5CF6", "#A78BFA", false),
            (4, "HSK 4", "Đỉnh Núi Bứt Phá", "Trung cao cấp", "Làm chủ 1.200 từ vựng, thảo luận đa dạng các chủ đề văn hóa, xã hội và công sở.", 1200, "🧱", "#F59E0B", "#FEF3C7", "#F59E0B", "#FBBF24", false),
            (5, "HSK 5", "Thành Trì Tinh Hoa", "Đọc hiểu nâng cao", "Chinh phục 2.500 từ vựng, đọc hiểu báo chí, xem phim không cần phụ đề và viết luận sắc bén.", 2500, "🗼", "#7C3AED", "#F3E8FF", "#7C3AED", "#A855F7", false),
            (6, "HSK 6", "Hoàng Cung", "Lưu loát đỉnh cao", "5.000 từ vựng tinh hoa, khả năng biểu đạt cảm xúc và tư duy tự nhiên như người bản xứ.", 5000, "🏙️", "#059669", "#E6F4EA", "#059669", "#10B981", false),
            (7, "HSK 7", "Điện Thái Cực", "Học thuật & Dịch thuật", "Hệ HSK 3.0 cao cấp: Nghiên cứu tài liệu chuyên sâu, biên dịch chuẩn xác các tài liệu học thuật.", 7000, "🔮", "#4F46E5", "#EEF2FF", "#4F46E5", "#6366F1", true),
            (8, "HSK 8", "Tháp Đế Vương", "Lý luận & Chuyên ngành", "Văn bản cổ điển, phân tích lý luận xã hội, kinh tế & luật pháp sâu rộng bằng tiếng Hán.", 9000, "👑", "#EA580C", "#FFEDD5", "#EA580C", "#FB923C", true),
            (9, "HSK 9", "Cửu Thiên Bất Bại", "Bậc Thầy Ngôn Ngữ", "Đỉnh cao tuyệt đối của HSK 3.0: Lưu loát vượt bậc, tư duy như một học giả ngôn ngữ thực thụ.", 11000, "🌌", "#9333EA", "#FDF2F8", "#9333EA", "#C084FC", true)
        };

        foreach (var def in definitions)
        {
            var world = new HskWorldBoardItem
            {
                HskLevel = def.Item1,
                LevelKey = def.Item2,
                WorldName = def.Item3,
                Subtitle = def.Item4,
                Description = def.Item5,
                VocabCount = def.Item6,
                Icon = def.Item7,
                AccentColor = def.Item8,
                TintBg = def.Item9,
                GradientStart = def.Item10,
                GradientEnd = def.Item11,
                IsLocked = def.Item12
            };

            // Build exactly 10 sub-levels for each HSK
            BuildSubLevelsForWorld(world);

            Worlds.Add(world);
        }

        // Default select HSK 1
        SelectWorld(Worlds[0]);
    }

    private void BuildSubLevelsForWorld(HskWorldBoardItem world)
    {
        string[] levelTitles = new[]
        {
            "Nhập Môn Bắt Chữ",
            "Từ Vựng Nền Tảng",
            "Siêu Tốc Bắt Cặp",
            "Ghép Câu Thần Tốc",
            "Luyện Tai Tinh Mắt",
            "Điền Chữ Vào Chỗ Trống",
            "Phản Xạ Đối Thoại",
            "Giải Mã Gõ Pinyin",
            "Vượt Ải Tốc Độ",
            "⚔️ ĐẠI CHIẾN TRÙM CUỐI"
        };

        string[] levelDescs = new[]
        {
            "Nhận diện mặt chữ Hán và phát âm chuẩn Pinyin",
            "Ghi nhớ nghĩa và từ loại trọng tâm của bài",
            "Phản xạ tìm cặp từ đồng nghĩa & trái nghĩa",
            "Sắp xếp cấu trúc câu đúng quy tắc ngữ pháp",
            "Nghe hội thoại thực tế và chọn đáp án chính xác",
            "Điền lượng từ, liên từ và phó từ vào chỗ trống",
            "Hoàn thành các đoạn đối thoại tình huống thực tế",
            "Thử thách gõ Pinyin chuyển đổi mặt chữ chuẩn xác",
            "Thử thách tính giờ sinh tử: Trả lời không dừng!",
            "Ải Boss tổng kết: Thử thách toàn diện nhận vương miện 👑"
        };

        string[] types = new[]
        {
            "Nhận diện chữ",
            "Nghĩa từ vựng",
            "Nối từ siêu tốc",
            "Xếp câu",
            "Luyện nghe",
            "Điền khuyết",
            "Đối thoại",
            "Luyện gõ",
            "Speed Run",
            "Đại chiến Boss"
        };

        string[] icons = new[] { "🎯", "📚", "⚡", "🧩", "🎧", "✍️", "🗣️", "⌨️", "⏱️", "👑" };

        int completedCount = 0;
        int totalStars = 0;

        for (int i = 1; i <= 10; i++)
        {
            bool isBoss = (i == 10);
            int exp = isBoss ? 250 : 50 + (i * 10);
            int gems = isBoss ? 50 : 10;

            // Generate realistic progress depending on HSK level
            bool isCompleted = false;
            bool isCurrent = false;
            bool isLocked = false;
            int stars = 0;
            int highScore = 0;

            if (world.HskLevel == 1)
            {
                if (i <= 6)
                {
                    isCompleted = true;
                    stars = i <= 4 ? 3 : 2;
                    highScore = i <= 4 ? 100 : 85;
                    completedCount++;
                    totalStars += stars;
                }
                else if (i == 7)
                {
                    isCurrent = true;
                }
                else
                {
                    isLocked = true;
                }
            }
            else if (world.HskLevel == 2)
            {
                if (i <= 3)
                {
                    isCompleted = true;
                    stars = 3;
                    highScore = 100;
                    completedCount++;
                    totalStars += stars;
                }
                else if (i == 4)
                {
                    isCurrent = true;
                }
                else
                {
                    isLocked = true;
                }
            }
            else if (world.HskLevel == 3)
            {
                if (i == 1)
                {
                    isCurrent = true;
                }
                else
                {
                    isLocked = true;
                }
            }
            else
            {
                isLocked = true;
            }

            string medalBg = "#FEF3C7";
            string medalBorder = "#F59E0B";
            string medalFg = "#B45309";
            string ribbonColor = "#F59E0B";

            if (i == 1)
            {
                medalBg = "#FEF3C7";
                medalBorder = "#F59E0B";
                medalFg = "#B45309";
                ribbonColor = "#F59E0B";
            }
            else if (i == 2)
            {
                medalBg = "#EFF6FF";
                medalBorder = "#3B82F6";
                medalFg = "#1D4ED8";
                ribbonColor = "#3B82F6";
            }
            else if (i == 3)
            {
                medalBg = "#FFEDD5";
                medalBorder = "#EA580C";
                medalFg = "#C2410C";
                ribbonColor = "#EA580C";
            }
            else if (i == 4)
            {
                medalBg = "#F3E8FF";
                medalBorder = "#8B5CF6";
                medalFg = "#6D28D9";
                ribbonColor = "#8B5CF6";
            }
            else if (i == 10)
            {
                medalBg = "#FEF3C7";
                medalBorder = "#EAB308";
                medalFg = "#B45309";
                ribbonColor = "#EAB308";
            }
            else
            {
                medalBg = "#F1F5F9";
                medalBorder = "#94A3B8";
                medalFg = "#475569";
                ribbonColor = "#94A3B8";
            }

            var subLevel = new GameSubLevelItem
            {
                LevelNumber = i,
                Title = $"Ải {i}: {levelTitles[i - 1]}",
                Description = levelDescs[i - 1],
                ChallengeType = types[i - 1],
                Icon = icons[i - 1],
                IsBossLevel = isBoss,
                RewardExp = exp,
                RewardGems = gems,
                TargetHsk = world.LevelKey,
                IsCompleted = isCompleted,
                IsCurrent = isCurrent,
                IsLocked = isLocked,
                StarsEarned = stars,
                HighScore = highScore,
                MedalBg = medalBg,
                MedalBorder = medalBorder,
                MedalFg = medalFg,
                RibbonColor = ribbonColor
            };

            world.SubLevels.Add(subLevel);
        }

        world.CompletedLevelsCount = completedCount;
        world.TotalStarsEarned = totalStars;
    }
}
