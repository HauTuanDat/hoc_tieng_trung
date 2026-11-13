using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class QuizViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IHskService? _hskService;
    private readonly Action<QuizResult> _onQuizCompleted;
    private readonly Action _onExit;

    private List<Question> _questions = new();
    private List<Question> _initialBatchQuestions = new();
    private readonly List<Question> _wrongQuestionsQueue = new();
    private readonly List<Question> _nextReviewQueue = new();
    private List<int> _wordIds = new();

    private string _level = "HSK 1";
    private string _mode = "MeaningToHanzi";
    private int _totalInitialCount = 20;
    private int _initialCorrectCount = 0;
    private bool _isReviewPhase = false;

    private int _currentIndex = 0;
    private Question? _currentQuestion;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _isAnswerChecked;
    private bool _isCurrentAnswerCorrect;
    private string _feedbackExplanation = string.Empty;
    private string _timeElapsedText = "00:00";
    private string _roundStatusNotice = string.Empty;
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer = new();

    private readonly List<UserAnswer> _userAnswers = new();

    public OptionViewModel OptionA { get; } = new() { Key = "A" };
    public OptionViewModel OptionB { get; } = new() { Key = "B" };
    public OptionViewModel OptionC { get; } = new() { Key = "C" };
    public OptionViewModel OptionD { get; } = new() { Key = "D" };

    public ICommand SelectOptionCommand { get; }
    public ICommand CheckOrNextCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand RetryLoadingCommand { get; }
    public ICommand PlayAudioCommand { get; }

    public QuizViewModel(IDatabaseService databaseService, Action<QuizResult> onQuizCompleted, Action onExit, IHskService? hskService = null)
    {
        _databaseService = databaseService;
        _hskService = hskService;
        _onQuizCompleted = onQuizCompleted;
        _onExit = onExit;

        SelectOptionCommand = new RelayCommand(param =>
        {
            if (param is string key)
            {
                SelectOption(key);
            }
        });

        CheckOrNextCommand = new RelayCommand(ExecuteCheckOrNext, () => CanExecuteCheckOrNext);
        ExitCommand = new RelayCommand(() => _onExit());
        RetryLoadingCommand = new RelayCommand(async () => await LoadQuestionsAsync());
        PlayAudioCommand = new RelayCommand(PlayCurrentAudio);

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            TimeElapsedText = _stopwatch.Elapsed.ToString(@"mm\:ss");
        };
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsReviewPhase
    {
        get => _isReviewPhase;
        set
        {
            if (SetProperty(ref _isReviewPhase, value))
            {
                OnPropertyChanged(nameof(RoundStatusNotice));
                OnPropertyChanged(nameof(IsReviewBannerVisible));
            }
        }
    }

    public bool IsReviewBannerVisible => IsReviewPhase;

    public string RoundStatusNotice
    {
        get
        {
            if (IsReviewPhase)
            {
                return "🔄 Đang ở vòng ôn tập câu sai: Trả lời lại các câu chưa đúng để ghi nhớ!";
            }
            return string.Empty;
        }
    }

    public Question? CurrentQuestion
    {
        get => _currentQuestion;
        private set => SetProperty(ref _currentQuestion, value);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                OnPropertyChanged(nameof(QuestionNumberText));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(IsLastQuestion));
            }
        }
    }

    public int TotalQuestions => _questions.Count;

    public string QuestionNumberText
    {
        get
        {
            if (IsReviewPhase)
            {
                return $"Ôn tập câu {CurrentIndex + 1} / {TotalQuestions}";
            }
            return TotalQuestions > 0 ? $"Câu {CurrentIndex + 1} / {TotalQuestions}" : "Câu 0 / 0";
        }
    }

    public double ProgressPercentage
    {
        get
        {
            if (TotalQuestions == 0) return 0;
            return ((double)(CurrentIndex + 1) / TotalQuestions) * 100.0;
        }
    }

    public bool IsLastQuestion => CurrentIndex >= TotalQuestions - 1;

    public bool IsAnswerChecked
    {
        get => _isAnswerChecked;
        private set
        {
            if (SetProperty(ref _isAnswerChecked, value))
            {
                OptionA.IsAnswerChecked = value;
                OptionB.IsAnswerChecked = value;
                OptionC.IsAnswerChecked = value;
                OptionD.IsAnswerChecked = value;
                OnPropertyChanged(nameof(CheckButtonText));
                OnPropertyChanged(nameof(CanExecuteCheckOrNext));
            }
        }
    }

    public bool IsCurrentAnswerCorrect
    {
        get => _isCurrentAnswerCorrect;
        private set => SetProperty(ref _isCurrentAnswerCorrect, value);
    }

    public string FeedbackExplanation
    {
        get => _feedbackExplanation;
        private set => SetProperty(ref _feedbackExplanation, value);
    }

    public string TimeElapsedText
    {
        get => _timeElapsedText;
        private set => SetProperty(ref _timeElapsedText, value);
    }

    public string? SelectedOptionKey
    {
        get
        {
            if (OptionA.IsSelected) return "A";
            if (OptionB.IsSelected) return "B";
            if (OptionC.IsSelected) return "C";
            if (OptionD.IsSelected) return "D";
            return null;
        }
    }

    public bool HasSelectedOption => SelectedOptionKey != null;

    public bool CanExecuteCheckOrNext => !IsAnswerChecked ? HasSelectedOption : true;

    public string CheckButtonText
    {
        get
        {
            if (!IsAnswerChecked)
            {
                return "Kiểm tra đáp án ➔";
            }
            if (IsLastQuestion)
            {
                if (!IsReviewPhase && _wrongQuestionsQueue.Count > 0)
                {
                    return "Bắt đầu ôn tập câu sai ➔";
                }
                return "Xem kết quả ➔";
            }
            return "Câu tiếp theo ➔";
        }
    }

    public async Task StartQuizAsync()
    {
        await LoadQuestionsAsync();
    }

    public void StartWithBatch(List<Question> questions, List<int> wordIds, string level = "HSK 1", string mode = "MeaningToHanzi")
    {
        IsLoading = false;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasError));

        _initialBatchQuestions = new List<Question>(questions);
        _questions = new List<Question>(questions);
        _wordIds = new List<int>(wordIds);
        _level = level;
        _mode = mode;
        _totalInitialCount = questions.Count;
        _initialCorrectCount = 0;
        _wrongQuestionsQueue.Clear();
        _nextReviewQueue.Clear();
        _isReviewPhase = false;
        _userAnswers.Clear();

        if (_questions.Count == 0)
        {
            ErrorMessage = "Không tìm thấy câu hỏi nào để bắt đầu bài thi.";
            OnPropertyChanged(nameof(HasError));
            return;
        }

        CurrentIndex = 0;
        _stopwatch.Restart();
        _timer.Start();

        DisplayCurrentQuestion();
    }

    public void StartWithQuestions(List<Question> questions)
    {
        var ids = questions.Select(q => q.Id).ToList();
        StartWithBatch(questions, ids);
    }

    public async Task LoadQuestionsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            await _databaseService.InitializeDatabaseAsync();
            var allQuestions = await _databaseService.GetAllQuestionsAsync();

            if (allQuestions.Count == 0)
            {
                ErrorMessage = "Chưa có câu hỏi nào trong cơ sở dữ liệu. Vui lòng thêm câu hỏi để bắt đầu.";
                OnPropertyChanged(nameof(HasError));
                return;
            }

            var batch = allQuestions.Take(20).ToList();
            StartWithBatch(batch, batch.Select(q => q.Id).ToList(), "CƠ BẢN", "English");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải câu hỏi từ cơ sở dữ liệu: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DisplayCurrentQuestion()
    {
        if (CurrentIndex < 0 || CurrentIndex >= _questions.Count) return;

        CurrentQuestion = _questions[CurrentIndex];
        IsAnswerChecked = false;
        IsCurrentAnswerCorrect = false;
        FeedbackExplanation = string.Empty;

        OptionA.Reset();
        OptionA.Text = CurrentQuestion.OptionA;
        OptionA.IsCorrectOption = string.Equals(CurrentQuestion.CorrectAnswer, "A", StringComparison.OrdinalIgnoreCase);

        OptionB.Reset();
        OptionB.Text = CurrentQuestion.OptionB;
        OptionB.IsCorrectOption = string.Equals(CurrentQuestion.CorrectAnswer, "B", StringComparison.OrdinalIgnoreCase);

        OptionC.Reset();
        OptionC.Text = CurrentQuestion.OptionC;
        OptionC.IsCorrectOption = string.Equals(CurrentQuestion.CorrectAnswer, "C", StringComparison.OrdinalIgnoreCase);

        OptionD.Reset();
        OptionD.Text = CurrentQuestion.OptionD;
        OptionD.IsCorrectOption = string.Equals(CurrentQuestion.CorrectAnswer, "D", StringComparison.OrdinalIgnoreCase);

        OnPropertyChanged(nameof(QuestionNumberText));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(IsLastQuestion));
        OnPropertyChanged(nameof(CheckButtonText));
        OnPropertyChanged(nameof(CanExecuteCheckOrNext));
        OnPropertyChanged(nameof(RoundStatusNotice));
        OnPropertyChanged(nameof(IsReviewBannerVisible));
        CommandManager.InvalidateRequerySuggested();

        if (_mode == "HanziToMeaning" && CurrentQuestion != null && !string.IsNullOrWhiteSpace(CurrentQuestion.Prompt))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentQuestion.Prompt);
        }
    }

    public void PlayCurrentAudio()
    {
        if (CurrentQuestion == null) return;
        if (_mode == "HanziToMeaning")
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentQuestion.Prompt);
        }
        else
        {
            string correctLetter = CurrentQuestion.CorrectAnswer.ToUpperInvariant();
            string correctWord = CurrentQuestion.GetOptionText(correctLetter);
            if (!string.IsNullOrWhiteSpace(correctWord))
            {
                _ = ChineseAudioService.Instance.PlayAsync(correctWord);
            }
        }
    }

    public void SelectOption(string key)
    {
        if (IsAnswerChecked) return;

        OptionA.IsSelected = string.Equals(key, "A", StringComparison.OrdinalIgnoreCase);
        OptionB.IsSelected = string.Equals(key, "B", StringComparison.OrdinalIgnoreCase);
        OptionC.IsSelected = string.Equals(key, "C", StringComparison.OrdinalIgnoreCase);
        OptionD.IsSelected = string.Equals(key, "D", StringComparison.OrdinalIgnoreCase);

        OnPropertyChanged(nameof(SelectedOptionKey));
        OnPropertyChanged(nameof(HasSelectedOption));
        OnPropertyChanged(nameof(CanExecuteCheckOrNext));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ExecuteCheckOrNext()
    {
        if (!IsAnswerChecked)
        {
            // CHECKING ANSWER
            string? selected = SelectedOptionKey;
            if (selected == null || CurrentQuestion == null) return;

            bool isCorrect = string.Equals(selected, CurrentQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
            IsCurrentAnswerCorrect = isCorrect;
            IsAnswerChecked = true;

            // Âm thanh khi chọn đúng / khi chọn sai
            if (isCorrect)
            {
                SoundService.Instance.PlayCorrect();
            }
            else
            {
                SoundService.Instance.PlayIncorrect();
            }

            // Track answer logic
            if (!IsReviewPhase)
            {
                // First attempt tracking (Initial 20-question batch)
                _userAnswers.Add(new UserAnswer
                {
                    Question = CurrentQuestion,
                    SelectedOption = selected
                });

                if (isCorrect)
                {
                    _initialCorrectCount++;
                }
                else
                {
                    // Add to queue to re-ask after the 20 questions!
                    if (!_wrongQuestionsQueue.Contains(CurrentQuestion))
                    {
                        _wrongQuestionsQueue.Add(CurrentQuestion);
                    }
                }
            }
            else
            {
                // In Review Phase: If user still answered incorrectly, keep in next review queue!
                if (!isCorrect)
                {
                    if (!_nextReviewQueue.Contains(CurrentQuestion))
                    {
                        _nextReviewQueue.Add(CurrentQuestion);
                    }
                }
            }

            // Explanation feedback
            string customExpl = CurrentQuestion.ExplanationAfterAnswer ?? "";
            string correctLetter = CurrentQuestion.CorrectAnswer.ToUpperInvariant();
            string correctWord = CurrentQuestion.GetOptionText(correctLetter);

            if (isCorrect)
            {
                FeedbackExplanation = $"Chính xác! {correctLetter}. {correctWord} là câu trả lời đúng. {customExpl}";
            }
            else
            {
                FeedbackExplanation = $"Chưa chính xác! Đáp án đúng là {correctLetter}. {correctWord}. {customExpl}\n" +
                    (!IsReviewPhase ? "(Câu này sẽ được hỏi lại sau khi bạn hoàn thành 20 câu)" : "");
            }

            PlayCurrentAudio();
        }
        else
        {
            // PROCEEDING TO NEXT QUESTION OR NEXT PHASE
            if (IsLastQuestion)
            {
                if (!IsReviewPhase)
                {
                    // Check if there are wrong questions to ask again
                    if (_wrongQuestionsQueue.Count > 0)
                    {
                        // ENTER REVIEW PHASE
                        IsReviewPhase = true;
                        _questions = new List<Question>(_wrongQuestionsQueue);
                        _wrongQuestionsQueue.Clear();
                        _nextReviewQueue.Clear();
                        CurrentIndex = 0;
                        DisplayCurrentQuestion();
                        return;
                    }
                    else
                    {
                        // All 20 were correct on first try!
                        FinishQuiz();
                    }
                }
                else
                {
                    // Review Phase finished: Check if any were still missed
                    if (_nextReviewQueue.Count > 0)
                    {
                        // Continue reviewing the ones still missed until all are correct!
                        _questions = new List<Question>(_nextReviewQueue);
                        _nextReviewQueue.Clear();
                        CurrentIndex = 0;
                        DisplayCurrentQuestion();
                        return;
                    }
                    else
                    {
                        // All wrong questions have now been answered correctly!
                        FinishQuiz();
                    }
                }
            }
            else
            {
                CurrentIndex++;
                DisplayCurrentQuestion();
            }
        }
    }

    private void FinishQuiz()
    {
        _timer.Stop();
        _stopwatch.Stop();

        double scorePercentage = _totalInitialCount > 0
            ? ((double)_initialCorrectCount / _totalInitialCount) * 100.0
            : 0;

        var result = new QuizResult
        {
            TotalQuestions = _totalInitialCount,
            CorrectCount = _initialCorrectCount,
            Duration = _stopwatch.Elapsed,
            UserAnswers = new List<UserAnswer>(_userAnswers),
            Level = _level,
            Mode = _mode,
            WordIds = new List<int>(_wordIds),
            CompletedAt = DateTime.Now
        };

        // If score is >= 70%, automatically mark batch as completed in SQL Server!
        if (result.IsPassing && _hskService != null && _wordIds.Count > 0)
        {
            _ = _hskService.MarkBatchCompletedAsync(_wordIds, isMastered: true);
            SoundService.Instance.PlayCompleted();
        }

        // Asynchronously save history to DB
        _ = _databaseService.SaveQuizResultAsync(result);

        _onQuizCompleted(result);
    }

    public void HandleKeyboardInput(Key key)
    {
        if (IsLoading || HasError) return;

        if (!IsAnswerChecked)
        {
            switch (key)
            {
                case Key.A:
                case Key.D1:
                case Key.NumPad1:
                    SelectOption("A");
                    break;
                case Key.B:
                case Key.D2:
                case Key.NumPad2:
                    SelectOption("B");
                    break;
                case Key.C:
                case Key.D3:
                case Key.NumPad3:
                    SelectOption("C");
                    break;
                case Key.D:
                case Key.D4:
                case Key.NumPad4:
                    SelectOption("D");
                    break;
                case Key.Enter:
                case Key.Space:
                    if (HasSelectedOption)
                    {
                        ExecuteCheckOrNext();
                    }
                    break;
            }
        }
        else
        {
            if (key == Key.Enter || key == Key.Space)
            {
                ExecuteCheckOrNext();
            }
        }
    }
}
