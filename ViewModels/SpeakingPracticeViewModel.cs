using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Input;
using System.Windows.Threading;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public sealed record PronunciationCriterionRow(string Label, PronunciationCriterion Criterion)
{
    public string ScoreText => Criterion.Score is int score ? $"{score}/{Criterion.Maximum}" : "Chưa đủ dữ liệu";
    public string Coverage => Criterion.PitchEvaluated switch
    {
        0 => "Chưa có cao độ đủ rõ để chấm",
        int pitch when pitch < Criterion.Targets => $"Đo được F0 ở {pitch}/{Criterion.Targets} âm tiết",
        _ when Criterion.Score.HasValue && Criterion.Evaluated < Criterion.Targets =>
            $"Đủ tín hiệu ở {Criterion.Evaluated}/{Criterion.Targets} âm tiết",
        _ => string.Empty
    };
}

public sealed record PronunciationScoreSummary(string Title, string Value, string Coverage);

public class SpeakingPracticeViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly IChineseAudioService _audioService;
    private readonly IAudioRecorderService _recorderService;
    private readonly DispatcherTimer _inputLevelTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly FunAsrTranscriptionService _transcriptionService = FunAsrTranscriptionService.Instance;
    private readonly LocalPronunciationService _pronunciationService = LocalPronunciationService.Instance;

    private static readonly ChineseVocabulary DefaultVocabulary = new()
    {
        Word = "爱",
        Pinyin = "ài",
        Meaning = "yêu",
        ExampleHanzi = "我爱你",
        ExamplePinyin = "Wǒ ài nǐ",
        ExampleMeaning = "Anh yêu em"
    };

    private string _currentLevel = "HSK 1";
    private string _lessonTitle = "Bài 1 • HSK 1";
    private string _currentHanzi = string.Empty;
    private string _currentPinyin = string.Empty;
    private string _currentMeaning = string.Empty;
    private string _sampleDialogue = string.Empty;
    private bool _isSentenceMode;

    private const string ReadyMessage = "Bấm Thu âm, đọc nội dung trên thẻ, rồi bấm Dừng thu âm.";
    private const string PronunciationReadyMessage = "Thu âm để xem điểm từng tiêu chí và nhận xét theo âm tiết.";

    private string _transcribedText = string.Empty;
    private string _heardPinyin = string.Empty;
    private string _statusMessage = ReadyMessage;
    private string _pronunciationStatus = PronunciationReadyMessage;
    private IReadOnlyList<PronunciationSyllable> _pronunciationSyllables = [];
    private IReadOnlyList<PronunciationCriterionRow> _pronunciationCriteria = [];
    private PronunciationScoreSummary? _pronunciationSummary;
    private string _pronunciationPace = string.Empty;
    private bool _showSentenceRetry;
    private double _inputLevel;
    private bool _isRecording;
    private bool _isTranscribing;
    private bool _isLoadingLevel;
    private bool _isActive = true;
    private string? _recordedAudioPath;
    private string? _playbackAudioPath;
    private CancellationTokenSource? _transcription;

    private List<ChineseVocabulary> _vocabPool = new();
    private int _currentIndex = 0;

    public SpeakingPracticeViewModel(IHskService hskService, Action onBack)
    {
        _hskService = hskService;
        _audioService = new ChineseAudioService();
        _recorderService = new AudioRecorderService();
        _inputLevelTimer.Tick += (_, _) => InputLevel = _recorderService.InputLevel;
        SelectLevelCommand = new RelayCommand(
            async param => await LoadVocabForLevelAsync(param as string ?? "HSK 1"),
            _ => CanChangeWord());
        SelectPracticeModeCommand = new RelayCommand(SelectPracticeMode, CanSelectPracticeMode);

        PlayAudioCommand = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(CurrentHanzi))
            {
                _recorderService.StopPlayback();
                await _audioService.PlayAsync(CurrentHanzi);
            }
        }, () => !IsRecording);

        PlayDialogueAudioCommand = new RelayCommand(async () =>
        {
            var item = CurrentVocabulary;
            _recorderService.StopPlayback();
            await _audioService.PlayAsync(HasExampleSentence ? item.ExampleHanzi! : CurrentHanzi);
        }, () => !IsRecording);

        ToggleRecordCommand = new RelayCommand(async () => await ExecuteToggleRecordAsync(),
            () => _isActive && !_isTranscribing && !_isLoadingLevel);

        PlayUserRecordingCommand = new RelayCommand(() =>
        {
            if ((_playbackAudioPath ?? _recordedAudioPath) is string path)
            {
                _audioService.Stop();
                _recorderService.PlayRecording(path);
            }
        }, () => HasRecorded && !IsRecording && (_playbackAudioPath is not null || !_isTranscribing));

        NextWordCommand = new RelayCommand(NextWord, CanChangeWord);
        SkipWordCommand = new RelayCommand(NextWord, CanChangeWord);
        BackCommand = new RelayCommand(onBack);

        DisplayCurrent();
        _ = LoadVocabForLevelAsync("HSK 1");
    }

    public string CurrentLevel
    {
        get => _currentLevel;
        set => SetProperty(ref _currentLevel, value);
    }

    public string LessonTitle
    {
        get => _lessonTitle;
        set => SetProperty(ref _lessonTitle, value);
    }

    public string CurrentHanzi
    {
        get => _currentHanzi;
        set => SetProperty(ref _currentHanzi, value);
    }

    public string CurrentPinyin
    {
        get => _currentPinyin;
        set => SetProperty(ref _currentPinyin, value);
    }

    public string CurrentMeaning
    {
        get => _currentMeaning;
        set => SetProperty(ref _currentMeaning, value);
    }

    public string SampleDialogue
    {
        get => _sampleDialogue;
        set => SetProperty(ref _sampleDialogue, value);
    }

    public bool IsSentenceMode
    {
        get => _isSentenceMode;
        private set => SetProperty(ref _isSentenceMode, value);
    }

    public bool HasExampleSentence => !string.IsNullOrWhiteSpace(CurrentVocabulary.ExampleHanzi);

    private ChineseVocabulary CurrentVocabulary => _vocabPool.Count > 0 ? _vocabPool[_currentIndex] : DefaultVocabulary;

    public string TranscribedText
    {
        get => _transcribedText;
        private set => SetProperty(ref _transcribedText, value);
    }

    public string HeardPinyin
    {
        get => _heardPinyin;
        private set => SetProperty(ref _heardPinyin, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PronunciationStatus
    {
        get => _pronunciationStatus;
        private set => SetProperty(ref _pronunciationStatus, value);
    }

    public IReadOnlyList<PronunciationSyllable> PronunciationSyllables
    {
        get => _pronunciationSyllables;
        private set => SetProperty(ref _pronunciationSyllables, value);
    }

    public IReadOnlyList<PronunciationCriterionRow> PronunciationCriteria
    {
        get => _pronunciationCriteria;
        private set => SetProperty(ref _pronunciationCriteria, value);
    }

    public PronunciationScoreSummary? PronunciationSummary
    {
        get => _pronunciationSummary;
        private set => SetProperty(ref _pronunciationSummary, value);
    }

    public string PronunciationPace
    {
        get => _pronunciationPace;
        private set => SetProperty(ref _pronunciationPace, value);
    }

    public bool ShowSentenceRetry
    {
        get => _showSentenceRetry;
        private set => SetProperty(ref _showSentenceRetry, value);
    }

    public string RecordButtonText => IsRecording ? "⏹ Dừng thu âm" : "🎙 Thu âm";

    public double InputLevel
    {
        get => _inputLevel;
        private set => SetProperty(ref _inputLevel, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetProperty(ref _isRecording, value))
            {
                if (value)
                    _inputLevelTimer.Start();
                else
                {
                    _inputLevelTimer.Stop();
                    InputLevel = 0;
                }
                OnPropertyChanged(nameof(RecordButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasRecorded => _recordedAudioPath != null;
    public string PlaybackButtonText => _playbackAudioPath is null ? "Nghe bản thu gốc" : "Nghe lại giọng của bạn";

    public ICommand SelectLevelCommand { get; }
    public ICommand SelectPracticeModeCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PlayDialogueAudioCommand { get; }
    public ICommand ToggleRecordCommand { get; }
    public ICommand PlayUserRecordingCommand { get; }
    public ICommand NextWordCommand { get; }
    public ICommand SkipWordCommand { get; }
    public ICommand BackCommand { get; }

    private async Task ExecuteToggleRecordAsync()
    {
        if (!_isActive || _isTranscribing || _isLoadingLevel) return;

        if (!IsRecording)
        {
            try
            {
                _audioService.Stop();
                ClearRecording();
                _recorderService.StartRecording();
                IsRecording = true;
                StatusMessage = "Đang thu âm. Bấm Dừng thu âm khi bạn nói xong.";
                PronunciationStatus = "Bước 1/6 · Ghi âm";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Không mở được microphone. Hãy kiểm tra thiết bị và quyền truy cập microphone trong Windows.";
            }
            return;
        }

        string audioPath;
        _isTranscribing = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            audioPath = await _recorderService.StopRecordingAsync();
            if (!_isActive)
            {
                DeleteAudioFile(audioPath);
                _isTranscribing = false;
                return;
            }
            _recordedAudioPath = audioPath;
            OnPropertyChanged(nameof(HasRecorded));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _isTranscribing = false;
            StatusMessage = "Không lưu được bản thu. Hãy thử thu âm lại.";
            PronunciationStatus = PronunciationReadyMessage;
            return;
        }
        finally
        {
            IsRecording = false;
        }

        using var request = new CancellationTokenSource();
        _transcription = request;
        PronunciationStatus = "Bước 2/6 · Nhận dạng lời nói";
        CommandManager.InvalidateRequerySuggested();
        string cleanedPath = Path.Combine(Path.GetTempPath(), $"MocNgu-{Guid.NewGuid():N}.wav");
        string playbackPath = Path.ChangeExtension(cleanedPath, ".playback.wav");
        try
        {
            var result = await AnalyzeRecordingAsync(audioPath, cleanedPath, playbackPath, request);
            request.Token.ThrowIfCancellationRequested();

            ApplyAssessment(result);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (_isActive)
            {
                PronunciationStatus = "Không khởi động được bộ chấm phát âm. Hãy chạy lại Install-Pronunciation.ps1.";
            }
        }
        finally
        {
            DeleteAudioFile(cleanedPath);
            if (_playbackAudioPath != playbackPath) DeleteAudioFile(playbackPath);
            if (!_isActive) DeleteAudioFile(audioPath);
            _transcription = null;
            _isTranscribing = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ApplyAssessment(PronunciationAssessment result)
    {
        PronunciationSyllables = result.Syllables ?? [];
        if (result.Scores is PronunciationScores scores)
        {
            PronunciationCriteria =
            [
                new("Thanh mẫu · 声母", scores.Initials),
                new("Vận mẫu · 韵母", scores.Finals),
                new("Thanh điệu · 声调", scores.Tones),
                new("Độ chính xác âm tiết · 音节准确度", scores.Syllables),
                new("Độ tự nhiên · 自然度", scores.Naturalness)
            ];
            var scored = PronunciationCriteria.Where(row => row.Criterion.Score.HasValue).ToArray();
            PronunciationSummary = result.TotalScore switch
            {
                int total => new("Tổng điểm", $"{total}/100", string.Empty),
                _ when scored.Length == 0 => new("Điểm phát âm", "Chưa chấm được", string.Empty),
                _ => new("Điểm một phần", $"{scored.Sum(row => row.Criterion.Score.GetValueOrDefault())}/{scored.Sum(row => row.Criterion.Maximum)}",
                    $"{scored.Length}/{PronunciationCriteria.Count} tiêu chí có điểm. Chưa có tổng điểm.")
            };
            PronunciationPace = result.PaceFeedback switch
            {
                "steady_pace" => "Nhịp đọc tương đối đều.",
                "long_pause" => "Có khoảng ngắt dài. Hãy thử đọc liền mạch hơn.",
                "uneven_pace" => "Tốc độ hoặc độ dài âm tiết chưa đều.",
                _ => string.Empty
            };
        }

        ShowSentenceRetry = !IsSentenceMode && HasExampleSentence && result.TotalScore is null &&
            result.Status is "ok" or "model_disagreement" or "ipa_uncertain" or "noise_only" or "low_frequency_noise" or "no_speech";

        PronunciationStatus = result.Status switch
        {
            "ok" when result.TotalScore.HasValue => "Bước 6/6 · Kết quả chấm phát âm",
            "ok" or "model_disagreement" when result.Scores is PronunciationScores partial => PartialAssessmentMessage(partial),
            "model_disagreement" => "Hai mô hình chưa thống nhất về âm đọc. Hãy thử thu lại.",
            "noise_only" => "Bản thu có tiếng nền lấn át hoặc giọng quá nhỏ. Hãy đọc lại gần mic.",
            "low_frequency_noise" => "Tiếng rung trầm che mất phần giọng cần đo. Hãy nghe lại bản thu, nói gần mic hơn và tránh chạm vào máy khi thu lại.",
            "ipa_uncertain" when TranscribedText.Length > 0 => "Ứng dụng có văn bản nhận dạng nhưng chưa đủ tín hiệu để xác định âm tiết hoặc thanh điệu. Hãy đọc lại bằng giọng bình thường.",
            "ipa_uncertain" => "Bộ chấm chưa xác định chắc các âm tiết trong bản thu.",
            "no_speech" => "Bản thu quá ngắn hoặc gần như im lặng. Hãy thử thu âm lại.",
            "too_long" => "Hãy đọc nội dung trên thẻ trong tối đa 30 giây.",
            "reference_unavailable" => "Chưa xác định được cách đọc mẫu cho mục này.",
            "not_installed" => "Chưa cài bộ chấm phát âm. Chạy Install-Pronunciation.ps1 trong thư mục dự án.",
            "timeout" => "Bộ chấm phản hồi quá lâu. Hãy thử lại với bản thu ngắn.",
            "filter_error" => "Chưa xử lý được tiếng nền để chấm phát âm. Hãy thử thu lại.",
            _ => "Không xử lý được bản thu bằng bộ chấm phát âm. Hãy thử lại."
        };
    }

    private static string PartialAssessmentMessage(PronunciationScores scores)
    {
        var missing = new[]
        {
            scores.Initials.Score is null ? "phụ âm đầu" : null,
            scores.Finals.Score is null ? "phần vần" : null,
            scores.Tones.Score is null ? "thanh điệu" : null
        }.OfType<string>().ToArray();
        return missing.Length > 0
            ? $"Chưa xác nhận được {string.Join(", ", missing)}. Hãy thử thu lại."
            : "Cần một bản thu rõ hơn để tính tổng điểm. Hãy thử thu lại.";
    }

    private async Task<PronunciationAssessment> AnalyzeRecordingAsync(string audioPath, string cleanedPath, string playbackPath,
        CancellationTokenSource request)
    {
        StatusMessage = "Đang lọc tiếng nền trong bản thu…";
        PronunciationAssessment preparation;
        try
        {
            preparation = await _pronunciationService.PrepareAudioAsync(audioPath, cleanedPath, request.Token, playbackPath);
        }
        catch (Exception ex) when (!request.IsCancellationRequested)
        {
            Debug.WriteLine(ex);
            preparation = new("filter_error");
        }
        request.Token.ThrowIfCancellationRequested();
        if (preparation.PlaybackReady)
        {
            _playbackAudioPath = playbackPath;
            OnPropertyChanged(nameof(PlaybackButtonText));
            CommandManager.InvalidateRequerySuggested();
        }
        if (preparation.Status is "no_speech" or "too_long" or "low_frequency_noise" or "noise_only")
        {
            StatusMessage = string.Empty;
            return preparation;
        }

        bool prepared = preparation.Status == "ok";
        var transcription = await RecognizeSpeechAsync(prepared ? cleanedPath : audioPath, prepared ? audioPath : null, request);
        request.Token.ThrowIfCancellationRequested();
        if (transcription.RecordingIssue is string issue) return new(issue);
        if (!prepared) return preparation.Status == "error" ? new("filter_error") : preparation;

        PronunciationStatus = "Bước 3/6 · Đang chuẩn bị phân tích âm vị…";
        var progress = new Progress<string>(stage =>
        {
            if (!_isActive || _transcription != request || PronunciationSummary is not null) return;
            PronunciationStatus = stage switch
            {
                "phonemes" => "Bước 3/6 · Phân tích thanh mẫu, vận mẫu và âm tiết…",
                "tones" => "Bước 4/6 · Đo cao độ F0 và phân tích thanh điệu…",
                "scoring" => "Bước 5/6 · Tổng hợp điểm…",
                _ => PronunciationStatus
            };
        });
        var result = await _pronunciationService.AssessAsync(cleanedPath, CurrentHanzi, CurrentPinyin, progress, request.Token,
            transcription.Candidates, audioPath, preparation.TrimStartSeconds, preparation.RumbleDominant);
        request.Token.ThrowIfCancellationRequested();
        HeardPinyin = result.HeardPinyin ?? string.Empty;
        if (result.PreferredTranscript is { Length: > 0 } preferred)
        {
            TranscribedText = preferred;
            StatusMessage = (result.Status, result.UsedLessonSpelling, result.TranscriptUncertain, result.AlternateTranscript) switch
            {
                ("low_frequency_noise" or "ipa_uncertain", _, _, _) => "Chữ trên là dự đoán. Tín hiệu thu được chưa đủ rõ để chấm phát âm.",
                (_, true, _, { Length: > 0 } homophone) => $"Có chữ đồng âm. Qz ưu tiên cách viết trong bài. Phương án khác là {homophone}.",
                (_, _, true, { Length: > 0 } alternative) => $"Chưa chắc chữ nhận dạng. Phương án khác là {alternative}.",
                (_, _, true, _) => "Chưa đủ tín hiệu để xác nhận chữ trên.",
                (_, _, false, { Length: > 0 } alternative) => $"Phương án nhận dạng khác là {alternative}.",
                _ => string.Empty
            };
        }
        else if (TranscribedText.Length > 0)
        {
            StatusMessage = "Chữ trên chưa được đối chiếu với âm đọc.";
        }
        return result;
    }

    private async Task<FunAsrTranscription> RecognizeSpeechAsync(string audioPath, string? originalPath, CancellationTokenSource request)
    {
        StatusMessage = "Đang chuẩn bị nhận dạng giọng nói…";
        bool recognizing = true;
        var progress = new Progress<string>(message =>
        {
            if (recognizing && _isActive && _transcription == request) StatusMessage = message;
        });

        try
        {
            var result = await _transcriptionService.TranscribeAsync(audioPath, progress, request.Token, originalPath,
                checkShortWord: !IsSentenceMode);
            request.Token.ThrowIfCancellationRequested();

            TranscribedText = result.BestAvailableText;
            StatusMessage = (result.RecordingIssue, TranscribedText.Length, originalPath) switch
            {
                (null, 0, _) => "Chưa nhận ra lời nói. Hãy nghe lại bản thu hoặc thử nói rõ hơn.",
                (null, _, not null) => "Đang đối chiếu chữ nhận dạng với âm đọc…",
                _ => string.Empty
            };
            return result;
        }
        catch (Exception ex) when (!request.IsCancellationRequested)
        {
            Debug.WriteLine(ex);
            if (_isActive)
            {
                StatusMessage = ex is HttpRequestException or TimeoutException
                    ? "Không tải được mô hình FunASR. Hãy kiểm tra kết nối mạng rồi thử lại."
                    : "FunASR gặp lỗi khi xử lý bản thu. Hãy thử thu âm lại.";
            }
            return new(string.Empty);
        }
        finally
        {
            recognizing = false;
        }
    }

    private async Task LoadVocabForLevelAsync(string level)
    {
        if (!CanChangeWord()) return;

        string previousStatus = StatusMessage;
        _isLoadingLevel = true;
        StatusMessage = "Đang tải từ vựng…";
        CommandManager.InvalidateRequerySuggested();
        try
        {
            var list = await _hskService.GetVocabulariesAsync(level, limit: 100);
            if (!_isActive) return;

            if (list.Count == 0)
            {
                StatusMessage = level == CurrentLevel ? previousStatus : $"Chưa có từ vựng cho {level}.";
                return;
            }

            CurrentLevel = level;
            _vocabPool = list;
            _currentIndex = 0;
            DisplayCurrent();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (_isActive) StatusMessage = level == CurrentLevel ? previousStatus : $"Không tải được từ vựng cho {level}.";
        }
        finally
        {
            _isLoadingLevel = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void DisplayCurrent()
    {
        var item = CurrentVocabulary;
        if (IsSentenceMode && !HasExampleSentence) IsSentenceMode = false;

        CurrentHanzi = IsSentenceMode ? item.ExampleHanzi! : item.Word;
        CurrentPinyin = IsSentenceMode ? item.ExamplePinyin ?? string.Empty : item.Pinyin;
        CurrentMeaning = IsSentenceMode ? item.ExampleMeaning ?? string.Empty : item.Meaning;
        LessonTitle = $"Bài {(_currentIndex / 5) + 1} • {item.Level}";
        SampleDialogue = HasExampleSentence
            ? $"{item.ExampleHanzi} ({item.ExamplePinyin}) - {item.ExampleMeaning}"
            : $"{item.Word}! {item.Pinyin}! {item.Meaning}";
        OnPropertyChanged(nameof(HasExampleSentence));

        ClearRecording();
        StatusMessage = ReadyMessage;
        CommandManager.InvalidateRequerySuggested();
    }

    private bool CanSelectPracticeMode(object? parameter) =>
        CanChangeWord() && (parameter is "Word" || parameter is "Sentence" && HasExampleSentence);

    private void SelectPracticeMode(object? parameter)
    {
        bool readSentence = parameter is "Sentence";
        if (!CanSelectPracticeMode(parameter) || IsSentenceMode == readSentence) return;

        _audioService.Stop();
        IsSentenceMode = readSentence;
        DisplayCurrent();
    }

    private void ClearRecording()
    {
        _recorderService.StopPlayback();
        if (_playbackAudioPath is string playbackPath)
        {
            _playbackAudioPath = null;
            OnPropertyChanged(nameof(PlaybackButtonText));
            DeleteAudioFile(playbackPath);
        }
        TranscribedText = string.Empty;
        HeardPinyin = string.Empty;
        PronunciationStatus = PronunciationReadyMessage;
        PronunciationSyllables = [];
        PronunciationCriteria = [];
        PronunciationSummary = null;
        PronunciationPace = string.Empty;
        ShowSentenceRetry = false;
        if (_recordedAudioPath is string path)
        {
            _recordedAudioPath = null;
            OnPropertyChanged(nameof(HasRecorded));
            if (_transcription is null) DeleteAudioFile(path);
        }
    }

    private static void DeleteAudioFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex);
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        _transcription?.Cancel();
        _audioService.Stop();
        _recorderService.CancelRecording();
        IsRecording = false;
        ClearRecording();
    }

    private bool CanChangeWord() => _isActive && !IsRecording && !_isTranscribing && !_isLoadingLevel;

    private void NextWord()
    {
        if (CanChangeWord() && _vocabPool.Count > 0)
        {
            _currentIndex = (_currentIndex + 1) % _vocabPool.Count;
            DisplayCurrent();
        }
    }
}
