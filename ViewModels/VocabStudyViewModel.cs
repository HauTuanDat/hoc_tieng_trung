using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class LessonItem : ViewModelBase
{
    private bool _isSelected;
    private string _progressText = "0/15";

    public int LessonNumber { get; set; }
    public string Title { get; set; } = "";
    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    public bool IsLocked { get; set; }
}

public class VocabOptionItem : ViewModelBase
{
    private bool _isSelected;
    private bool _isCorrect;
    private bool _isWrong;

    public string Key { get; set; } = ""; // "A", "B", "C", "D"
    public string Text { get; set; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsCorrect
    {
        get => _isCorrect;
        set => SetProperty(ref _isCorrect, value);
    }

    public bool IsWrong
    {
        get => _isWrong;
        set => SetProperty(ref _isWrong, value);
    }
}

public class VocabStudyViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly IDatabaseService _databaseService;
    private readonly IChineseAudioService _audioService;
    private readonly Action _onBack;

    private string _currentLevel = "HSK 1";
    private int _currentLessonNumber = 1;
    private string _lessonHeaderTitle = "HSK 1 - Bài 1: Chào hỏi, xưng hô";
    private string _progressStepText = "1 / 15";
    private double _progressStepValue = 6.6;

    private string _currentWord = "学习";
    private string _currentPinyin = "Xuéxí";
    private string _currentMeaning = "Học tập, học";
    private string _exampleHanzi = "我每天学习中文。";
    private string _examplePinyin = "Wǒ měitiān xuéxí Zhōngwén.";
    private string _exampleMeaning = "Tôi học tiếng Trung mỗi ngày.";

    private string _questionPrompt = "Chọn nghĩa đúng của từ \"学习\"";
    private string _questionNumberText = "Câu 1 / 4";

    private bool _hasAnswered = false;
    private bool _isAnswerCorrect = false;
    private string _answerFeedbackTitle = "";
    private string _answerFeedbackMessage = "";
    private string _answerFeedbackIcon = "✅";

    private List<ChineseVocabulary> _vocabList = new();
    private int _vocabIndex = 0;

    public VocabStudyViewModel(IHskService hskService, IDatabaseService databaseService, Action onBack, string initialLevel = "HSK 1")
    {
        _hskService = hskService;
        _databaseService = databaseService;
        _audioService = new ChineseAudioService();
        _onBack = onBack;
        _currentLevel = initialLevel;

        Lessons = new ObservableCollection<LessonItem>();
        Options = new ObservableCollection<VocabOptionItem>();

        PopulateLessonsForLevel(_currentLevel);

        SelectLevelCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            CurrentLevel = lvl;
            PopulateLessonsForLevel(lvl);
            _ = LoadVocabAsync();
        });

        SelectLessonCommand = new RelayCommand(param =>
        {
            if (param is LessonItem lesson && !lesson.IsLocked)
            {
                foreach (var l in Lessons) l.IsSelected = false;
                lesson.IsSelected = true;
                CurrentLessonNumber = lesson.LessonNumber;
                LessonHeaderTitle = $"{CurrentLevel} - {lesson.Title}";

                // Start from the beginning word of this lesson
                _vocabIndex = (lesson.LessonNumber - 1) * 15;
                if (_vocabList.Count > 0 && _vocabIndex >= _vocabList.Count)
                {
                    _vocabIndex = _vocabIndex % _vocabList.Count;
                }
                DisplayCurrentVocab();
            }
        });

        PlayWordAudioCommand = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(CurrentWord))
            {
                await _audioService.PlayAsync(CurrentWord);
            }
        });

        PlayExampleAudioCommand = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(ExampleHanzi))
            {
                await _audioService.PlayAsync(ExampleHanzi);
            }
        });

        SelectOptionCommand = new RelayCommand(param =>
        {
            if (param is VocabOptionItem opt && !HasAnswered)
            {
                SelectOption(opt);
            }
        });

        NextWordCommand = new RelayCommand(NextWord);
        BackCommand = new RelayCommand(_onBack);

        _ = LoadVocabAsync();
    }

    public string CurrentLevel
    {
        get => _currentLevel;
        set => SetProperty(ref _currentLevel, value);
    }

    public int CurrentLessonNumber
    {
        get => _currentLessonNumber;
        set => SetProperty(ref _currentLessonNumber, value);
    }

    public string LessonHeaderTitle
    {
        get => _lessonHeaderTitle;
        set => SetProperty(ref _lessonHeaderTitle, value);
    }

    public string ProgressStepText
    {
        get => _progressStepText;
        set => SetProperty(ref _progressStepText, value);
    }

    public double ProgressStepValue
    {
        get => _progressStepValue;
        set => SetProperty(ref _progressStepValue, value);
    }

    public string CurrentWord
    {
        get => _currentWord;
        set => SetProperty(ref _currentWord, value);
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

    public string ExampleHanzi
    {
        get => _exampleHanzi;
        set => SetProperty(ref _exampleHanzi, value);
    }

    public string ExamplePinyin
    {
        get => _examplePinyin;
        set => SetProperty(ref _examplePinyin, value);
    }

    public string ExampleMeaning
    {
        get => _exampleMeaning;
        set => SetProperty(ref _exampleMeaning, value);
    }

    public string QuestionPrompt
    {
        get => _questionPrompt;
        set => SetProperty(ref _questionPrompt, value);
    }

    public string QuestionNumberText
    {
        get => _questionNumberText;
        set => SetProperty(ref _questionNumberText, value);
    }

    public bool HasAnswered
    {
        get => _hasAnswered;
        set => SetProperty(ref _hasAnswered, value);
    }

    public bool IsAnswerCorrect
    {
        get => _isAnswerCorrect;
        set => SetProperty(ref _isAnswerCorrect, value);
    }

    public string AnswerFeedbackTitle
    {
        get => _answerFeedbackTitle;
        set => SetProperty(ref _answerFeedbackTitle, value);
    }

    public string AnswerFeedbackMessage
    {
        get => _answerFeedbackMessage;
        set => SetProperty(ref _answerFeedbackMessage, value);
    }

    public string AnswerFeedbackIcon
    {
        get => _answerFeedbackIcon;
        set => SetProperty(ref _answerFeedbackIcon, value);
    }

    public ObservableCollection<LessonItem> Lessons { get; }
    public ObservableCollection<VocabOptionItem> Options { get; }

    public ICommand SelectLevelCommand { get; }
    public ICommand SelectLessonCommand { get; }
    public ICommand PlayWordAudioCommand { get; }
    public ICommand PlayExampleAudioCommand { get; }
    public ICommand SelectOptionCommand { get; }
    public ICommand NextWordCommand { get; }
    public ICommand BackCommand { get; }

    private void PopulateLessonsForLevel(string level)
    {
        Lessons.Clear();
        var topics = GetTopicsForLevel(level);
        for (int i = 0; i < topics.Length; i++)
        {
            int num = i + 1;
            Lessons.Add(new LessonItem
            {
                LessonNumber = num,
                Title = $"Bài {num}: {topics[i]}",
                ProgressText = "0/15",
                IsSelected = num == 1,
                IsLocked = false
            });
        }
        CurrentLessonNumber = 1;
        var first = Lessons.FirstOrDefault();
        if (first != null)
        {
            LessonHeaderTitle = $"{CurrentLevel} - {first.Title}";
        }
    }

    private string[] GetTopicsForLevel(string level)
    {
        return level switch
        {
            "HSK 1" => new[] { "Chào hỏi, xưng hô", "Gia đình, bạn bè", "Số đếm, thời gian", "Ăn uống, mua sắm", "Hoạt động thường nhật", "Phương hướng, vị trí" },
            "HSK 2" => new[] { "Sức khỏe, sinh hoạt", "Công việc, học tập", "Du lịch, giao thông", "Thời tiết, mùa màng", "Cảm xúc, tính cách", "Sở thích, thể thao" },
            "HSK 3" => new[] { "Giao tiếp công sở", "Môi trường, tự nhiên", "Xã hội, văn hóa", "Kế hoạch, mục tiêu", "Biểu đạt, thảo luận", "Thành tựu cuộc sống" },
            "HSK 4" => new[] { "Đời sống, xã hội", "Khoa học, công nghệ", "Nghệ thuật, du lịch", "Kinh tế, việc làm", "Tâm lý, sức khỏe", "Thành ngữ, văn phong" },
            "HSK 5" => new[] { "Thời sự, chính luận", "Đàm phán, thương mại", "Văn học, triết lý", "Môi trường sinh thái", "Khoa học vũ trụ", "Diễn thuyết cao cấp" },
            "HSK 6" => new[] { "Văn ngôn, điển cố", "Báo cáo học thuật", "Ngôn ngữ chuyên sâu", "Văn hóa Trung Hoa", "Bình luận thời cuộc", "Đỉnh cao HSK" },
            "Công xưởng" => new[] { "Dây chuyền, thiết bị", "Quy trình sản xuất", "An toàn lao động", "Kiểm tra chất lượng (QC)", "Bảo trì máy móc", "Bàn giao ca kíp" },
            "Kế toán" => new[] { "Hóa đơn, chứng từ", "Khai báo thuế, sổ sách", "Báo cáo tài chính", "Tiền lương, chi phí", "Công nợ, thanh toán", "Kiểm toán ngân hàng" },
            "Xuất nhập khẩu" => new[] { "Logistics, vận tải", "Hải quan, thông quan", "Hợp đồng thương mại", "Thanh toán quốc tế L/C", "Đóng gói, lưu kho", "Bảo hiểm, khiếu nại" },
            _ => new[] { "Từ vựng cơ bản", "Hội thoại hàng ngày", "Giao tiếp nâng cao", "Ứng dụng thực tế", "Kỹ năng chuyên sâu", "Tổng hợp phản xạ" }
        };
    }

    private async Task LoadVocabAsync()
    {
        try
        {
            var list = await _hskService.GetVocabulariesAsync(CurrentLevel, limit: 120);
            if (list.Count > 0)
            {
                _vocabList = list;
                _vocabIndex = (CurrentLessonNumber - 1) * 15;
                if (_vocabIndex >= _vocabList.Count)
                {
                    _vocabIndex = _vocabIndex % _vocabList.Count;
                }
                DisplayCurrentVocab();
            }
        }
        catch
        {
            // fallback defaults
        }
    }

    private void DisplayCurrentVocab()
    {
        if (_vocabList.Count == 0) return;

        var vocab = _vocabList[_vocabIndex % _vocabList.Count];
        CurrentWord = vocab.Word;
        CurrentPinyin = vocab.Pinyin;
        CurrentMeaning = vocab.Meaning;
        ExampleHanzi = string.IsNullOrWhiteSpace(vocab.ExampleHanzi) ? $"{vocab.Word}是很好。" : vocab.ExampleHanzi;
        ExamplePinyin = string.IsNullOrWhiteSpace(vocab.ExamplePinyin) ? $"{vocab.Pinyin} shì hěn hǎo." : vocab.ExamplePinyin;
        ExampleMeaning = string.IsNullOrWhiteSpace(vocab.ExampleMeaning) ? $"{vocab.Meaning} rất tốt." : vocab.ExampleMeaning;

        int step = (_vocabIndex % 15) + 1;
        ProgressStepText = $"{step} / 15";
        ProgressStepValue = step * 100.0 / 15.0;

        // Update active lesson progress text
        var curLesson = Lessons.FirstOrDefault(l => l.LessonNumber == CurrentLessonNumber);
        if (curLesson != null)
        {
            curLesson.ProgressText = $"{step}/15";
        }

        QuestionPrompt = $"Chọn nghĩa đúng của từ \"{vocab.Word}\"";
        QuestionNumberText = $"Câu {((step - 1) % 4) + 1} / 4";

        HasAnswered = false;
        IsAnswerCorrect = false;
        AnswerFeedbackTitle = "";
        AnswerFeedbackMessage = "";

        // Generate 4 options (1 correct + 3 random distractors)
        var optionsList = new List<string> { vocab.Meaning };
        var random = new Random();
        var otherMeanings = _vocabList
            .Where(v => v.Word != vocab.Word && !string.IsNullOrWhiteSpace(v.Meaning))
            .Select(v => v.Meaning)
            .Distinct()
            .OrderBy(_ => random.Next())
            .Take(3)
            .ToList();

        while (otherMeanings.Count < 3)
        {
            otherMeanings.Add(otherMeanings.Count == 0 ? "Ăn, uống" : (otherMeanings.Count == 1 ? "Đi, đến" : "Làm việc"));
        }

        optionsList.AddRange(otherMeanings);
        optionsList = optionsList.OrderBy(_ => random.Next()).ToList();

        Options.Clear();
        string[] keys = { "A", "B", "C", "D" };
        for (int i = 0; i < optionsList.Count && i < 4; i++)
        {
            Options.Add(new VocabOptionItem
            {
                Key = keys[i],
                Text = optionsList[i]
            });
        }
    }

    private void SelectOption(VocabOptionItem opt)
    {
        HasAnswered = true;
        opt.IsSelected = true;

        var correctOpt = Options.FirstOrDefault(o => o.Text.Equals(CurrentMeaning, StringComparison.OrdinalIgnoreCase));
        if (correctOpt != null) correctOpt.IsCorrect = true;

        if (opt.Text.Equals(CurrentMeaning, StringComparison.OrdinalIgnoreCase))
        {
            opt.IsCorrect = true;
            IsAnswerCorrect = true;
            AnswerFeedbackIcon = "✅";
            AnswerFeedbackTitle = "Chính xác! 🎉";
            AnswerFeedbackMessage = $"Bạn đã chọn đúng nghĩa của từ: {opt.Key}. {opt.Text}. Rất tốt!";
            SoundService.Instance.PlayCorrect();

            if (_vocabList.Count > _vocabIndex)
            {
                _ = _hskService.MarkMasteredAsync(_vocabList[_vocabIndex].Id, true);
            }
        }
        else
        {
            opt.IsWrong = true;
            IsAnswerCorrect = false;
            AnswerFeedbackIcon = "❌";
            AnswerFeedbackTitle = "Chưa chính xác!";
            string correctKey = correctOpt?.Key ?? "A";
            AnswerFeedbackMessage = $"Bạn chọn {opt.Key}. Đáp án đúng là: {correctKey}. {CurrentMeaning}. Hãy bấm 'Nghe lại' hoặc ghi nhớ từ này nhé!";
            SoundService.Instance.PlayIncorrect();
        }
    }

    private void NextWord()
    {
        if (_vocabList.Count > 0)
        {
            _vocabIndex++;
            DisplayCurrentVocab();
        }
    }
}
