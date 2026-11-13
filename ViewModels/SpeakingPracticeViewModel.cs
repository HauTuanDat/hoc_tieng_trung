using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class SpeakingPracticeViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly IChineseAudioService _audioService;
    private readonly IAudioRecorderService _recorderService;
    private readonly IChinesePronunciationAiService _aiService;
    private readonly Action _onBack;

    private string _currentLevel = "HSK 1";
    private string _lessonTitle = "Bài 1 • HSK 1";
    private string _currentHanzi = "爱";
    private string _currentPinyin = "ài";
    private string _currentMeaning = "yêu";
    private string _sampleDialogue = "我爱你 (Wǒ ài nǐ) - Anh Yêu Em";

    private int _score = 0;
    private string _scoreText = "Chưa phát âm";
    private string _scoreColor = "#208B58";
    private string _starsDisplay = "☆☆☆☆☆";
    private string _feedbackMessage = "Bấm 'Thu âm phát âm' và đọc rõ chữ Hán để AI chấm điểm.";
    private string _statusMessage = "Sẵn sàng thu âm";
    private string _recordButtonText = "🎙 Thu âm phát âm";

    private bool _isRecording = false;
    private bool _isAnalyzing = false;
    private bool _hasRecorded = false;
    private string? _recordedAudioPath;

    private List<ChineseVocabulary> _vocabPool = new();
    private int _currentIndex = 0;

    public SpeakingPracticeViewModel(IHskService hskService, Action onBack)
    {
        _hskService = hskService;
        _audioService = new ChineseAudioService();
        _recorderService = new AudioRecorderService();
        _aiService = new ChinesePronunciationAiService(_recorderService);
        _onBack = onBack;

        SelectLevelCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            CurrentLevel = lvl;
            _ = LoadVocabForLevelAsync(lvl);
        });

        PlayAudioCommand = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(CurrentHanzi))
            {
                await _audioService.PlayAsync(CurrentHanzi);
            }
        });

        PlayDialogueAudioCommand = new RelayCommand(async () =>
        {
            if (_vocabPool.Count > 0 && !string.IsNullOrWhiteSpace(_vocabPool[_currentIndex % _vocabPool.Count].ExampleHanzi))
            {
                await _audioService.PlayAsync(_vocabPool[_currentIndex % _vocabPool.Count].ExampleHanzi!);
            }
            else if (!string.IsNullOrWhiteSpace(CurrentHanzi))
            {
                await _audioService.PlayAsync(CurrentHanzi);
            }
        });

        ToggleRecordCommand = new RelayCommand(async () => await ExecuteToggleRecordAsync());

        PlayUserRecordingCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(_recordedAudioPath))
            {
                _recorderService.PlayRecording(_recordedAudioPath);
            }
        }, () => HasRecorded);

        NextWordCommand = new RelayCommand(NextWord);
        SkipWordCommand = new RelayCommand(NextWord);
        BackCommand = new RelayCommand(_onBack);

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

    public int Score
    {
        get => _score;
        set => SetProperty(ref _score, value);
    }

    public string ScoreText
    {
        get => _scoreText;
        set => SetProperty(ref _scoreText, value);
    }

    public string ScoreColor
    {
        get => _scoreColor;
        set => SetProperty(ref _scoreColor, value);
    }

    public string StarsDisplay
    {
        get => _starsDisplay;
        set => SetProperty(ref _starsDisplay, value);
    }

    public string FeedbackMessage
    {
        get => _feedbackMessage;
        set => SetProperty(ref _feedbackMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string RecordButtonText
    {
        get => _recordButtonText;
        set => SetProperty(ref _recordButtonText, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => SetProperty(ref _isAnalyzing, value);
    }

    public bool HasRecorded
    {
        get => _hasRecorded;
        set => SetProperty(ref _hasRecorded, value);
    }

    public ICommand SelectLevelCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PlayDialogueAudioCommand { get; }
    public ICommand ToggleRecordCommand { get; }
    public ICommand PlayUserRecordingCommand { get; }
    public ICommand NextWordCommand { get; }
    public ICommand SkipWordCommand { get; }
    public ICommand BackCommand { get; }

    private async Task ExecuteToggleRecordAsync()
    {
        if (!IsRecording)
        {
            // START RECORDING
            _recorderService.StartRecording();
            IsRecording = true;
            RecordButtonText = "⏹ Đang thu âm... (Bấm để xong)";
            StatusMessage = "🔴 Đang nghe bạn phát âm... Hãy đọc to và rõ ràng!";
        }
        else
        {
            // STOP RECORDING AND EVALUATE
            string audioPath = _recorderService.StopRecording();
            IsRecording = false;
            RecordButtonText = "🎙 Thu âm phát âm";
            _recordedAudioPath = audioPath;
            HasRecorded = !string.IsNullOrWhiteSpace(audioPath);

            IsAnalyzing = true;
            StatusMessage = "⏳ AI đang phân tích phát âm & cao độ thanh điệu...";

            var eval = await _aiService.EvaluatePronunciationAsync(audioPath, CurrentHanzi, CurrentPinyin, CurrentMeaning);

            IsAnalyzing = false;
            Score = eval.Score;
            ScoreText = eval.ScoreGrade;
            ScoreColor = eval.ScoreHexColor;
            FeedbackMessage = eval.FeedbackMessage;
            StatusMessage = eval.IsAccurate ? "✅ Đã chấm điểm xong!" : "⚠️ Cần rèn luyện thêm";
            StarsDisplay = FormatStars(eval.StarCount);

            if (eval.Score >= 80)
            {
                SoundService.Instance.PlayCorrect();
            }
            else
            {
                SoundService.Instance.PlayIncorrect();
            }
        }
    }

    private string FormatStars(int count)
    {
        return count switch
        {
            5 => "⭐⭐⭐⭐⭐",
            4 => "⭐⭐⭐⭐☆",
            3 => "⭐⭐⭐☆☆",
            2 => "⭐⭐☆☆☆",
            1 => "⭐☆☆☆☆",
            _ => "☆☆☆☆☆"
        };
    }

    private async Task LoadVocabForLevelAsync(string level)
    {
        try
        {
            var list = await _hskService.GetVocabulariesAsync(level, limit: 100);
            if (list.Count > 0)
            {
                _vocabPool = list;
                _currentIndex = 0;
                DisplayCurrent();
            }
        }
        catch
        {
            // fallback
        }
    }

    private void DisplayCurrent()
    {
        if (_vocabPool.Count == 0) return;
        var item = _vocabPool[_currentIndex % _vocabPool.Count];
        CurrentHanzi = item.Word;
        CurrentPinyin = item.Pinyin;
        CurrentMeaning = item.Meaning;
        LessonTitle = $"Bài {(_currentIndex / 5) + 1} • {item.Level}";
        SampleDialogue = string.IsNullOrWhiteSpace(item.ExampleHanzi)
            ? $"{item.Word}! {item.Pinyin}! {item.Meaning}"
            : $"{item.ExampleHanzi} ({item.ExamplePinyin}) - {item.ExampleMeaning}";

        ResetScoreState();
    }

    private void ResetScoreState()
    {
        _recorderService.StopPlayback();
        Score = 0;
        ScoreText = "Chưa phát âm";
        ScoreColor = "#7A8A81";
        StarsDisplay = "☆☆☆☆☆";
        FeedbackMessage = "Bấm 'Thu âm phát âm' và đọc rõ chữ Hán để AI chấm điểm.";
        StatusMessage = "Sẵn sàng thu âm";
        HasRecorded = false;
        _recordedAudioPath = null;
    }

    private void NextWord()
    {
        if (_vocabPool.Count > 0)
        {
            _currentIndex = (_currentIndex + 1) % _vocabPool.Count;
            DisplayCurrent();
        }
    }
}
