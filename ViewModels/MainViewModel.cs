using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IHskService _hskService;
    private readonly IDictionaryApiService _dictionaryApiService;

    private ViewModelBase? _currentViewModel;
    private string _activeViewName = "Dashboard";
    private string _currentTopicTitle = "Mộc Ngữ • Học tiếng Trung";
    private string _globalSearchQuery = string.Empty;

    private User? _currentUser;
    private bool _isLoggedIn = false;

    // Track active quiz batch for retrying
    private List<Question> _lastBatchQuestions = new();
    private List<int> _lastBatchWordIds = new();

    public MainViewModel()
    {
        _databaseService = new SqlServerDatabaseService();
        _hskService = new HskService(_databaseService);
        _dictionaryApiService = new DictionaryApiService(_databaseService);

        // Navigation Commands
        NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
        NavigateToCoursesCommand = new RelayCommand(NavigateToCourseSelection);
        NavigateToPracticeCenterCommand = new RelayCommand(NavigateToPracticeCenter);
        NavigateToVocabListCommand = new RelayCommand(() => NavigateToVocabList("ALL"));
        NavigateToDictionaryCommand = new RelayCommand(param =>
        {
            string q = param as string ?? GlobalSearchQuery;
            NavigateToDictionary(q);
        });
        NavigateToLearnedVocabCommand = new RelayCommand(() => NavigateToLearnedVocab("HSK 1"));
        NavigateToProfileSettingsCommand = new RelayCommand(NavigateToProfileSettings);

        // Sub-skill commands
        NavigateToSpeakingCommand = new RelayCommand(NavigateToSpeakingPractice);
        NavigateToVocabStudyCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            NavigateToVocabStudy(lvl);
        });
        NavigateToQuizCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            StartHskQuiz(lvl, "MeaningToHanzi");
        });
        NavigateToSentenceScrambleCommand = new RelayCommand(param =>
        {
            string level = param as string ?? "HSK 1";
            NavigateToSentenceScramble(level);
        });
        NavigateToTypingCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            NavigateToTypingPractice(lvl);
        });
        NavigateToManageQuestionsCommand = new RelayCommand(NavigateToQuestionManagement);

        // App-wide actions
        GlobalSearchCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(GlobalSearchQuery))
            {
                NavigateToDictionary(GlobalSearchQuery);
            }
        });
        LogoutCommand = new RelayCommand(Logout);
        ToggleSoundCommand = new RelayCommand(() => IsSoundMuted = !IsSoundMuted);

        // Start at Login View
        NavigateToLogin();
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                OnPropertyChanged(nameof(IsOnLoginView));
                OnPropertyChanged(nameof(IsOnQuizView));
                OnPropertyChanged(nameof(IsOnDashboard));
            }
        }
    }

    public string ActiveViewName
    {
        get => _activeViewName;
        private set => SetProperty(ref _activeViewName, value);
    }

    public string CurrentTopicTitle
    {
        get => _currentTopicTitle;
        private set => SetProperty(ref _currentTopicTitle, value);
    }

    public string GlobalSearchQuery
    {
        get => _globalSearchQuery;
        set => SetProperty(ref _globalSearchQuery, value);
    }

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnPropertyChanged(nameof(UserDisplayName));
            }
        }
    }

    public string UserDisplayName => CurrentUser?.FullName ?? "Khách";

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set => SetProperty(ref _isLoggedIn, value);
    }

    public bool IsOnLoginView => CurrentViewModel is LoginViewModel;
    public bool IsOnQuizView => CurrentViewModel is QuizViewModel;
    public bool IsOnDashboard => CurrentViewModel is DashboardViewModel;

    private bool _isSoundMuted;
    public bool IsSoundMuted
    {
        get => _isSoundMuted;
        set
        {
            if (SetProperty(ref _isSoundMuted, value))
            {
                SoundService.Instance.IsMuted = value;
                OnPropertyChanged(nameof(SoundToggleIcon));
                OnPropertyChanged(nameof(SoundToggleTooltip));
            }
        }
    }

    public string SoundToggleIcon => IsSoundMuted ? "🔇" : "🔊";
    public string SoundToggleTooltip => IsSoundMuted ? "Âm thanh: Đang tắt" : "Âm thanh: Đang bật";

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToCoursesCommand { get; }
    public ICommand NavigateToPracticeCenterCommand { get; }
    public ICommand NavigateToVocabListCommand { get; }
    public ICommand NavigateToDictionaryCommand { get; }
    public ICommand NavigateToLearnedVocabCommand { get; }
    public ICommand NavigateToProfileSettingsCommand { get; }
    public ICommand NavigateToSpeakingCommand { get; }
    public ICommand NavigateToVocabStudyCommand { get; }
    public ICommand NavigateToQuizCommand { get; }
    public ICommand NavigateToSentenceScrambleCommand { get; }
    public ICommand NavigateToTypingCommand { get; }
    public ICommand NavigateToManageQuestionsCommand { get; }
    public ICommand GlobalSearchCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ToggleSoundCommand { get; }

    public void NavigateToLogin()
    {
        IsLoggedIn = false;
        ActiveViewName = "Login";
        CurrentTopicTitle = "Đăng nhập • Mộc Ngữ";
        CurrentViewModel = new LoginViewModel(_databaseService, OnLoginSuccess);
    }

    private void OnLoginSuccess(User user)
    {
        CurrentUser = user;
        IsLoggedIn = true;
        NavigateToDashboard();
    }

    public void Logout()
    {
        CurrentUser = null;
        NavigateToLogin();
    }

    public void NavigateToDashboard()
    {
        var vm = new DashboardViewModel(
            _hskService,
            onStartQuiz: StartHskQuiz,
            onOpenVocabList: NavigateToVocabList,
            onOpenTypingPractice: NavigateToTypingPractice,
            onOpenLearnedVocab: NavigateToLearnedVocab,
            onOpenSentenceScramble: NavigateToSentenceScramble,
            onOpenDictionary: NavigateToDictionary,
            onOpenVocabStudy: NavigateToVocabStudy,
            user: CurrentUser
        );
        CurrentViewModel = vm;
        ActiveViewName = "Dashboard";
        CurrentTopicTitle = "Tổng quan • Mộc Ngữ";
        _ = vm.InitializeAsync();
    }

    public void NavigateToCourseSelection()
    {
        var vm = new CourseSelectionViewModel(
            _hskService,
            onSelectCourse: lvl => NavigateToVocabStudy(lvl),
            onBack: NavigateToDashboard
        );
        CurrentViewModel = vm;
        ActiveViewName = "Courses";
        CurrentTopicTitle = "Chọn khóa học • Mộc Ngữ";
    }

    public void NavigateToPracticeCenter()
    {
        var vm = new PracticeCenterViewModel(
            _hskService,
            onNavigateToSkill: skill =>
            {
                switch (skill)
                {
                    case "Speaking": NavigateToSpeakingPractice(); break;
                    case "VocabStudy": NavigateToVocabStudy("HSK 1"); break;
                    case "Listening": StartHskQuiz("HSK 1", "HanziToMeaning"); break;
                    case "Typing": NavigateToTypingPractice("HSK 1"); break;
                    case "Reading": StartHskQuiz("HSK 1", "MeaningToHanzi"); break;
                    case "Scramble": NavigateToSentenceScramble("HSK 1"); break;
                    default: NavigateToDashboard(); break;
                }
            },
            onGoToCourses: NavigateToCourseSelection
        );
        CurrentViewModel = vm;
        ActiveViewName = "PracticeCenter";
        CurrentTopicTitle = "Trung tâm luyện tập • Mộc Ngữ";
    }

    public void NavigateToVocabStudy(string level = "HSK 1")
    {
        var vm = new VocabStudyViewModel(_hskService, _databaseService, onBack: NavigateToDashboard, initialLevel: level);
        CurrentViewModel = vm;
        ActiveViewName = "VocabStudy";
        CurrentTopicTitle = $"Học từ vựng • {level}";
    }

    public void NavigateToSpeakingPractice()
    {
        var vm = new SpeakingPracticeViewModel(_hskService, onBack: NavigateToDashboard);
        CurrentViewModel = vm;
        ActiveViewName = "Speaking";
        CurrentTopicTitle = "Luyện nói phát âm AI • Mộc Ngữ";
    }

    public void NavigateToVocabList(string level = "ALL")
    {
        var vm = new VocabListViewModel(_hskService, onBack: NavigateToDashboard, onStartQuiz: lvl => StartHskQuiz(lvl, "MeaningToHanzi"), initialLevel: level);
        CurrentViewModel = vm;
        ActiveViewName = "VocabList";
        CurrentTopicTitle = "Danh sách Từ vựng";
        _ = vm.InitializeAsync();
    }

    public void NavigateToDictionary(string query = "")
    {
        var vm = new DictionaryViewModel(_dictionaryApiService, onBack: NavigateToDashboard);
        CurrentViewModel = vm;
        ActiveViewName = "Dictionary";
        CurrentTopicTitle = "Tra cứu Từ điển Tiếng Trung";
        _ = vm.InitializeWithQueryAsync(string.IsNullOrWhiteSpace(query) ? null : query);
    }

    public void NavigateToLearnedVocab(string level = "HSK 1")
    {
        var vm = new LearnedVocabViewModel(_hskService, onBack: NavigateToDashboard, onStartQuiz: StartHskQuiz, initialLevel: level);
        CurrentViewModel = vm;
        ActiveViewName = "LearnedVocab";
        CurrentTopicTitle = $"Tiến độ học tập • {level}";
        _ = vm.InitializeAsync();
    }

    public void NavigateToProfileSettings()
    {
        var user = CurrentUser ?? new User { FullName = "Minh Đạt", Email = "minhdat@gmail.com", CurrentLevel = "HSK 3" };
        var vm = new ProfileSettingsViewModel(_databaseService, user, onLogout: Logout, onBack: NavigateToDashboard);
        CurrentViewModel = vm;
        ActiveViewName = "Settings";
        CurrentTopicTitle = "Hồ sơ cá nhân & Cài đặt";
    }

    public void NavigateToSentenceScramble(string level = "HSK 1")
    {
        var vm = new SentenceScrambleViewModel(_hskService, onBack: NavigateToDashboard, initialLevel: level);
        CurrentViewModel = vm;
        ActiveViewName = "SentenceScramble";
        CurrentTopicTitle = $"Xếp câu phản xạ • {level}";
        _ = vm.InitializeAsync();
    }

    public void NavigateToTypingPractice(string level = "HSK 1")
    {
        var vm = new TypingPracticeViewModel(_hskService, onBack: NavigateToDashboard, level: level);
        CurrentViewModel = vm;
        ActiveViewName = "Typing";
        CurrentTopicTitle = $"Luyện viết & gõ HSK • {level}";
        _ = vm.InitializeAsync();
    }

    public void NavigateToQuestionManagement()
    {
        var mgmtVm = new QuestionManagementViewModel(_databaseService, onBack: NavigateToDashboard);
        CurrentViewModel = mgmtVm;
        ActiveViewName = "Management";
        _ = mgmtVm.InitializeAsync();
    }

    public void StartHskQuiz(string level, string mode)
    {
        var quizVm = new QuizViewModel(_databaseService, OnQuizCompleted, OnQuizExit, _hskService);
        CurrentViewModel = quizVm;
        ActiveViewName = "Quiz";
        CurrentTopicTitle = $"Trắc nghiệm • {level}";

        _ = Task.Run(async () =>
        {
            var (batchQuestions, batchWordIds, isAllCompleted) = await _hskService.GetNextQuizBatchAsync(level, batchSize: 20, mode: mode);
            _lastBatchQuestions = new List<Question>(batchQuestions);
            _lastBatchWordIds = new List<int>(batchWordIds);

            App.Current.Dispatcher.Invoke(() =>
            {
                if (batchQuestions.Count > 0)
                {
                    quizVm.StartWithBatch(batchQuestions, batchWordIds, level, mode);
                }
                else
                {
                    _ = quizVm.StartQuizAsync();
                }
            });
        });
    }

    private void OnQuizCompleted(QuizResult result)
    {
        CurrentViewModel = new ResultViewModel(
            result,
            onRetryBatch: () => StartHskQuiz(result.Level, result.Mode),
            onContinueNextBatch: (lvl, mode) => StartHskQuiz(lvl, mode),
            onBackToHome: NavigateToDashboard
        );
        ActiveViewName = "Result";
    }

    private void OnQuizExit()
    {
        NavigateToDashboard();
    }

    public void HandleKeyDown(Key key)
    {
        if (CurrentViewModel is QuizViewModel quizVm)
        {
            quizVm.HandleKeyboardInput(key);
        }
    }
}
