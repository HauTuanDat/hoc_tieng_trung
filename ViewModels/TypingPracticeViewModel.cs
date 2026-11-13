using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class TypingPracticeViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action _onBack;

    private string _selectedLevel = "HSK 1";
    private int _selectedLimit = 5000; // Default: All words / No limit
    private string _selectedFilterMode = "ALL"; // ALL, Unmastered, Mastered

    private List<ChineseVocabulary> _vocabPool = new();
    private int _currentIndex;
    private ChineseVocabulary? _currentVocab;

    private string _inputWord = string.Empty;
    private string _inputSentence = string.Empty;

    private bool _hasChecked;
    private bool _isCorrect;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private int _score;
    private int _attemptsForCurrentWord;

    private bool _isResumedSession;
    private string _resumeNotice = string.Empty;
    private string _lastSavedRecordInfo = string.Empty;
    private string _jumpTargetIndexText = string.Empty;
    private bool _isLoading;
    private bool _isAutoPlayAudio = true;

    public ICommand BackCommand { get; }
    public ICommand CheckOrNextCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand SkipWordCommand { get; }
    public ICommand FillHintCommand { get; }
    public ICommand PreviousWordCommand { get; }
    public ICommand NextWordCommand { get; }
    public ICommand JumpToWordCommand { get; }
    public ICommand RestartFromBeginningCommand { get; }
    public ICommand FilterLevelCommand { get; }
    public ICommand FilterModeCommand { get; }
    public ICommand SelectLimitCommand { get; }
    public ICommand PlayWordAudioCommand { get; }
    public ICommand PlaySentenceAudioCommand { get; }

    public TypingPracticeViewModel(IHskService hskService, Action onBack, string level = "HSK 1")
    {
        _hskService = hskService;
        _onBack = onBack;
        _selectedLevel = string.IsNullOrWhiteSpace(level) ? "HSK 1" : level;

        BackCommand = new RelayCommand(_onBack);
        CheckOrNextCommand = new RelayCommand(ExecuteCheckOrNext);
        RetryCommand = new RelayCommand(ExecuteRetry);
        SkipWordCommand = new RelayCommand(ExecuteSkip);
        FillHintCommand = new RelayCommand(ExecuteFillHint);
        PreviousWordCommand = new RelayCommand(ExecutePreviousWord, () => CanGoPrevious);
        NextWordCommand = new RelayCommand(ExecuteNextWord, () => CanGoNext);
        JumpToWordCommand = new RelayCommand(ExecuteJumpToWord);
        RestartFromBeginningCommand = new RelayCommand(ExecuteRestartFromBeginning);
        PlayWordAudioCommand = new RelayCommand(PlayWordAudio);
        PlaySentenceAudioCommand = new RelayCommand(PlaySentenceAudio);

        FilterLevelCommand = new RelayCommand(param =>
        {
            if (param is string lvl && !string.Equals(SelectedLevel, lvl, StringComparison.OrdinalIgnoreCase))
            {
                SelectedLevel = lvl;
            }
        });

        FilterModeCommand = new RelayCommand(param =>
        {
            if (param is string mode && !string.Equals(SelectedFilterMode, mode, StringComparison.OrdinalIgnoreCase))
            {
                SelectedFilterMode = mode;
            }
        });

        SelectLimitCommand = new RelayCommand(param =>
        {
            if (param is string limitStr && int.TryParse(limitStr, out int lim) && SelectedLimit != lim)
            {
                SelectedLimit = lim;
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
                OnPropertyChanged(nameof(LevelName));
                _ = ReloadVocabulariesAsync();
            }
        }
    }

    public string LevelName => SelectedLevel;

    public int SelectedLimit
    {
        get => _selectedLimit;
        set
        {
            if (SetProperty(ref _selectedLimit, value))
            {
                _ = ReloadVocabulariesAsync();
            }
        }
    }

    public string SelectedFilterMode
    {
        get => _selectedFilterMode;
        set
        {
            if (SetProperty(ref _selectedFilterMode, value))
            {
                _ = ReloadVocabulariesAsync();
            }
        }
    }

    public ChineseVocabulary? CurrentVocab
    {
        get => _currentVocab;
        private set
        {
            if (SetProperty(ref _currentVocab, value))
            {
                OnPropertyChanged(nameof(IsCurrentVocabMastered));
                OnPropertyChanged(nameof(VocabMasteryStatusText));
            }
        }
    }

    public bool IsCurrentVocabMastered => CurrentVocab?.IsMastered == true || (CurrentVocab?.PracticeCount ?? 0) > 0;

    public string VocabMasteryStatusText => IsCurrentVocabMastered 
        ? $"✅ Đã thuộc • Luyện viết: {CurrentVocab?.PracticeCount ?? 0} lần" 
        : "Chưa thuộc • Từ mới";

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
    }

    public int TotalWords => _vocabPool.Count;

    public string ProgressText => TotalWords > 0 ? $"Từ {CurrentIndex + 1} / {TotalWords}" : "0 / 0";

    public double ProgressPercentage => TotalWords > 0 ? ((double)(CurrentIndex + 1) / TotalWords) * 100 : 0;

    public bool CanGoPrevious => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < TotalWords - 1;

    public int Score
    {
        get => _score;
        set => SetProperty(ref _score, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsAutoPlayAudio
    {
        get => _isAutoPlayAudio;
        set => SetProperty(ref _isAutoPlayAudio, value);
    }

    public bool IsResumedSession
    {
        get => _isResumedSession;
        set => SetProperty(ref _isResumedSession, value);
    }

    public string ResumeNotice
    {
        get => _resumeNotice;
        set => SetProperty(ref _resumeNotice, value);
    }

    public string LastSavedRecordInfo
    {
        get => _lastSavedRecordInfo;
        set => SetProperty(ref _lastSavedRecordInfo, value);
    }

    public string JumpTargetIndexText
    {
        get => _jumpTargetIndexText;
        set => SetProperty(ref _jumpTargetIndexText, value);
    }

    public string InputWord
    {
        get => _inputWord;
        set
        {
            if (SetProperty(ref _inputWord, value))
            {
                if (HasError)
                {
                    HasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }
    }

    public string InputSentence
    {
        get => _inputSentence;
        set
        {
            if (SetProperty(ref _inputSentence, value))
            {
                if (HasError)
                {
                    HasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }
    }

    public bool HasChecked
    {
        get => _hasChecked;
        private set
        {
            if (SetProperty(ref _hasChecked, value))
            {
                OnPropertyChanged(nameof(ActionButtonText));
            }
        }
    }

    public bool IsCorrect
    {
        get => _isCorrect;
        private set
        {
            if (SetProperty(ref _isCorrect, value))
            {
                OnPropertyChanged(nameof(ActionButtonText));
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        private set => SetProperty(ref _successMessage, value);
    }

    public string ActionButtonText
    {
        get
        {
            if (HasChecked && IsCorrect)
            {
                return CurrentIndex >= TotalWords - 1 ? "Hoàn thành ➔" : "Từ tiếp theo ➔";
            }
            return "💾 Kiểm tra & Lưu kết quả ➔";
        }
    }

    public async Task InitializeAsync()
    {
        await ReloadVocabulariesAsync(checkSavedProgress: true);
    }

    public async Task ReloadVocabulariesAsync(bool checkSavedProgress = true)
    {
        try
        {
            IsLoading = true;
            _vocabPool = await _hskService.GetVocabulariesForTypingAsync(SelectedLevel, SelectedFilterMode, SelectedLimit);

            if (_vocabPool.Count > 0)
            {
                int targetIndex = 0;
                if (checkSavedProgress)
                {
                    var savedProgress = await _hskService.GetTypingProgressAsync(SelectedLevel);
                    if (savedProgress.HasValue && savedProgress.Value.LastIndex > 0 && savedProgress.Value.LastIndex < _vocabPool.Count)
                    {
                        targetIndex = savedProgress.Value.LastIndex;
                        IsResumedSession = true;
                        ResumeNotice = $"💾 Đã tự động khôi phục tiến độ bài viết: Tiếp tục từ Từ {targetIndex + 1} / {_vocabPool.Count}";
                    }
                    else
                    {
                        IsResumedSession = false;
                        ResumeNotice = string.Empty;
                    }
                }
                else
                {
                    IsResumedSession = false;
                }

                CurrentIndex = targetIndex;
                DisplayCurrent();
            }
            else
            {
                CurrentVocab = null;
                CurrentIndex = 0;
                HasError = true;
                ErrorMessage = "Không tìm thấy từ vựng nào phù hợp với bộ lọc hiện tại.";
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DisplayCurrent()
    {
        if (CurrentIndex < 0 || CurrentIndex >= _vocabPool.Count) return;

        CurrentVocab = _vocabPool[CurrentIndex];
        InputWord = string.Empty;
        InputSentence = string.Empty;
        HasChecked = false;
        IsCorrect = false;
        HasError = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        LastSavedRecordInfo = string.Empty;
        _attemptsForCurrentWord = 0;

        _ = LoadSavedHistoryForCurrentVocabAsync();

        if (IsAutoPlayAudio && !string.IsNullOrWhiteSpace(CurrentVocab?.Word))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentVocab.Word);
        }
    }

    private async Task LoadSavedHistoryForCurrentVocabAsync()
    {
        if (CurrentVocab == null) return;
        var record = await _hskService.GetLastTypedRecordAsync(CurrentVocab.Id);
        if (record.HasValue)
        {
            LastSavedRecordInfo = $"Lần viết trước: \"{record.Value.TypedWord}\"" +
                (!string.IsNullOrWhiteSpace(record.Value.TypedSentence) ? $" • Câu: \"{record.Value.TypedSentence}\"" : "");
        }
    }

    public void ExecuteCheckOrNext()
    {
        if (CurrentVocab == null) return;

        if (HasChecked && IsCorrect)
        {
            MoveToNextWord();
            return;
        }

        string rawWordInput = InputWord?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawWordInput))
        {
            HasError = true;
            ErrorMessage = "Vui lòng nhập từ mới (chữ Hán hoặc Pinyin) trước khi kiểm tra!";
            return;
        }

        _attemptsForCurrentWord++;

        string targetHanzi = CurrentVocab.Word.Trim();
        string targetPinyin = NormalizeText(CurrentVocab.Pinyin);
        string userWord = NormalizeText(rawWordInput);

        bool matchHanzi = string.Equals(rawWordInput, targetHanzi, StringComparison.OrdinalIgnoreCase);
        bool matchPinyin = string.Equals(userWord, targetPinyin, StringComparison.OrdinalIgnoreCase);

        if (matchHanzi || matchPinyin)
        {
            HasChecked = true;
            IsCorrect = true;
            HasError = false;
            ErrorMessage = string.Empty;

            SoundService.Instance.PlayCorrect();

            if (_attemptsForCurrentWord == 1)
            {
                Score++;
            }

            CurrentVocab.IsMastered = true;
            CurrentVocab.PracticeCount++;
            CurrentVocab.LastPracticed = DateTime.Now;
            OnPropertyChanged(nameof(IsCurrentVocabMastered));
            OnPropertyChanged(nameof(VocabMasteryStatusText));

            SuccessMessage = $"🎉 Chính xác tuyệt vời! Bạn đã viết đúng từ: {CurrentVocab.Word} ({CurrentVocab.Pinyin}) - {CurrentVocab.Meaning}!\n" +
                             $"💾 Đã lưu kết quả & đánh dấu từ đã thuộc vào cơ sở dữ liệu.";

            _ = _hskService.RecordPracticeAsync(CurrentVocab.Id, typedWord: rawWordInput, typedSentence: InputSentence, markMastered: true);
            _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab.Id);
        }
        else
        {
            HasChecked = true;
            IsCorrect = false;
            HasError = true;
            SuccessMessage = string.Empty;

            SoundService.Instance.PlayIncorrect();

            ErrorMessage = $"⚠️ Chưa chính xác! Bạn đã viết: \"{rawWordInput}\".\n" +
                           $"Đáp án đúng cần viết là Chữ Hán: \"{CurrentVocab.Word}\" hoặc Pinyin: \"{CurrentVocab.Pinyin}\".\n" +
                           $"Hãy xóa đi và viết lại nhé!";
        }
    }

    public void ExecutePreviousWord()
    {
        if (CanGoPrevious)
        {
            CurrentIndex--;
            DisplayCurrent();
            _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab?.Id ?? 0);
        }
    }

    public void ExecuteNextWord()
    {
        if (CanGoNext)
        {
            CurrentIndex++;
            DisplayCurrent();
            _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab?.Id ?? 0);
        }
    }

    public void ExecuteJumpToWord()
    {
        if (int.TryParse(JumpTargetIndexText?.Trim(), out int targetNum))
        {
            if (targetNum >= 1 && targetNum <= TotalWords)
            {
                CurrentIndex = targetNum - 1;
                DisplayCurrent();
                JumpTargetIndexText = string.Empty;
                _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab?.Id ?? 0);
                return;
            }
        }
        ErrorMessage = $"Vui lòng nhập số từ 1 đến {TotalWords}!";
        HasError = true;
    }

    public void ExecuteRestartFromBeginning()
    {
        CurrentIndex = 0;
        IsResumedSession = false;
        ResumeNotice = string.Empty;
        DisplayCurrent();
        _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab?.Id ?? 0);
    }

    public void ExecuteRetry()
    {
        InputWord = string.Empty;
        InputSentence = string.Empty;
        HasChecked = false;
        IsCorrect = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void ExecuteSkip()
    {
        MoveToNextWord();
    }

    public void ExecuteFillHint()
    {
        if (CurrentVocab == null) return;
        InputWord = CurrentVocab.Word;
        if (!string.IsNullOrWhiteSpace(CurrentVocab.ExampleHanzi))
        {
            InputSentence = CurrentVocab.ExampleHanzi;
        }
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void PlayWordAudio()
    {
        if (CurrentVocab != null && !string.IsNullOrWhiteSpace(CurrentVocab.Word))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentVocab.Word);
        }
    }

    public void PlaySentenceAudio()
    {
        if (CurrentVocab != null && !string.IsNullOrWhiteSpace(CurrentVocab.ExampleHanzi))
        {
            _ = ChineseAudioService.Instance.PlayAsync(CurrentVocab.ExampleHanzi);
        }
    }

    private void MoveToNextWord()
    {
        if (CurrentIndex < _vocabPool.Count - 1)
        {
            CurrentIndex++;
            DisplayCurrent();
            _ = _hskService.SaveTypingProgressAsync(SelectedLevel, CurrentIndex, CurrentVocab?.Id ?? 0);
        }
        else
        {
            HasChecked = true;
            IsCorrect = true;
            SuccessMessage = $"🏆 Hoàn thành phiên luyện viết! Bạn đã đạt {Score} / {TotalWords} từ và toàn bộ tiến độ đã được lưu thành công.";
        }
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace(" ", "")
                   .Replace("，", "")
                   .Replace("。", "")
                   .Replace(",", "")
                   .Replace(".", "")
                   .Replace("！", "")
                   .Replace("!", "")
                   .ToLowerInvariant();
    }
}
