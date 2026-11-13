using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class QuestionManagementViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly Action _onBack;

    private Question? _selectedQuestion;
    private bool _isEditing;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    // Form fields for editing/adding
    private int _editingId;
    private string _category = "CƠ BẢN";
    private string _tag = "✨ NGHĨA CỦA TỪ";
    private string _prompt = string.Empty;
    private string _explanation = string.Empty;
    private string _partOfSpeech = "Tính từ • Mô tả tính cách";
    private string _questionText = "Từ tiếng Anh nào phù hợp với nghĩa trên?";
    private string _optionA = string.Empty;
    private string _optionB = string.Empty;
    private string _optionC = string.Empty;
    private string _optionD = string.Empty;
    private string _correctAnswer = "A";
    private string _explanationAfterAnswer = string.Empty;

    public ObservableCollection<Question> Questions { get; } = new();

    public ICommand BackCommand { get; }
    public ICommand NewQuestionCommand { get; }
    public ICommand SaveQuestionCommand { get; }
    public ICommand DeleteQuestionCommand { get; }
    public ICommand RefreshCommand { get; }

    public QuestionManagementViewModel(IDatabaseService databaseService, Action onBack)
    {
        _databaseService = databaseService;
        _onBack = onBack;

        BackCommand = new RelayCommand(_onBack);
        NewQuestionCommand = new RelayCommand(PrepareNewQuestion);
        SaveQuestionCommand = new RelayCommand(async () => await SaveQuestionAsync(), () => CanSaveQuestion);
        DeleteQuestionCommand = new RelayCommand(async () => await DeleteQuestionAsync(), () => SelectedQuestion != null);
        RefreshCommand = new RelayCommand(async () => await LoadQuestionsAsync());
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public Question? SelectedQuestion
    {
        get => _selectedQuestion;
        set
        {
            if (SetProperty(ref _selectedQuestion, value) && value != null)
            {
                LoadQuestionIntoForm(value);
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string FormTitle => _editingId == 0 ? "Thêm câu hỏi mới" : $"Chỉnh sửa câu hỏi #{_editingId}";

    public string Category
    {
        get => _category;
        set { SetProperty(ref _category, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    public string Prompt
    {
        get => _prompt;
        set { SetProperty(ref _prompt, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string Explanation
    {
        get => _explanation;
        set => SetProperty(ref _explanation, value);
    }

    public string PartOfSpeech
    {
        get => _partOfSpeech;
        set => SetProperty(ref _partOfSpeech, value);
    }

    public string QuestionText
    {
        get => _questionText;
        set => SetProperty(ref _questionText, value);
    }

    public string OptionA
    {
        get => _optionA;
        set { SetProperty(ref _optionA, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string OptionB
    {
        get => _optionB;
        set { SetProperty(ref _optionB, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string OptionC
    {
        get => _optionC;
        set { SetProperty(ref _optionC, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string OptionD
    {
        get => _optionD;
        set { SetProperty(ref _optionD, value); OnPropertyChanged(nameof(CanSaveQuestion)); }
    }

    public string CorrectAnswer
    {
        get => _correctAnswer;
        set => SetProperty(ref _correctAnswer, value);
    }

    public string ExplanationAfterAnswer
    {
        get => _explanationAfterAnswer;
        set => SetProperty(ref _explanationAfterAnswer, value);
    }

    public bool CanSaveQuestion =>
        !string.IsNullOrWhiteSpace(Prompt) &&
        !string.IsNullOrWhiteSpace(OptionA) &&
        !string.IsNullOrWhiteSpace(OptionB) &&
        !string.IsNullOrWhiteSpace(OptionC) &&
        !string.IsNullOrWhiteSpace(OptionD);

    public async Task InitializeAsync()
    {
        await LoadQuestionsAsync();
        PrepareNewQuestion();
    }

    private async Task LoadQuestionsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Đang tải dữ liệu từ SQL Server...";

            var list = await _databaseService.GetAllQuestionsAsync();
            Questions.Clear();
            foreach (var q in list)
            {
                Questions.Add(q);
            }

            StatusMessage = $"Đã tải {Questions.Count} câu hỏi từ CSDL.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PrepareNewQuestion()
    {
        _editingId = 0;
        Category = "CƠ BẢN";
        Tag = "✨ NGHĨA CỦA TỪ";
        Prompt = string.Empty;
        Explanation = string.Empty;
        PartOfSpeech = "Tính từ • Mô tả tính cách";
        QuestionText = "Từ tiếng Anh nào phù hợp với nghĩa trên?";
        OptionA = string.Empty;
        OptionB = string.Empty;
        OptionC = string.Empty;
        OptionD = string.Empty;
        CorrectAnswer = "A";
        ExplanationAfterAnswer = string.Empty;

        IsEditing = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void LoadQuestionIntoForm(Question q)
    {
        _editingId = q.Id;
        Category = q.Category;
        Tag = q.Tag;
        Prompt = q.Prompt;
        Explanation = q.Explanation;
        PartOfSpeech = q.PartOfSpeech;
        QuestionText = q.QuestionText;
        OptionA = q.OptionA;
        OptionB = q.OptionB;
        OptionC = q.OptionC;
        OptionD = q.OptionD;
        CorrectAnswer = q.CorrectAnswer;
        ExplanationAfterAnswer = q.ExplanationAfterAnswer ?? string.Empty;

        IsEditing = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private async Task SaveQuestionAsync()
    {
        try
        {
            IsLoading = true;
            var question = new Question
            {
                Id = _editingId,
                Category = Category,
                Tag = Tag,
                Prompt = Prompt,
                Explanation = Explanation,
                PartOfSpeech = PartOfSpeech,
                QuestionText = QuestionText,
                OptionA = OptionA,
                OptionB = OptionB,
                OptionC = OptionC,
                OptionD = OptionD,
                CorrectAnswer = CorrectAnswer,
                ExplanationAfterAnswer = string.IsNullOrWhiteSpace(ExplanationAfterAnswer) ? null : ExplanationAfterAnswer
            };

            if (_editingId == 0)
            {
                int newId = await _databaseService.AddQuestionAsync(question);
                question.Id = newId;
                Questions.Add(question);
                StatusMessage = $"Đã thêm câu hỏi #{newId} thành công vào SQL Server!";
            }
            else
            {
                await _databaseService.UpdateQuestionAsync(question);
                var existing = Questions.FirstOrDefault(q => q.Id == _editingId);
                if (existing != null)
                {
                    int index = Questions.IndexOf(existing);
                    Questions[index] = question;
                }
                StatusMessage = $"Đã cập nhật câu hỏi #{_editingId} thành công!";
            }

            PrepareNewQuestion();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khi lưu vào SQL Server: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteQuestionAsync()
    {
        if (SelectedQuestion == null) return;

        try
        {
            IsLoading = true;
            int id = SelectedQuestion.Id;
            bool success = await _databaseService.DeleteQuestionAsync(id);
            if (success)
            {
                Questions.Remove(SelectedQuestion);
                StatusMessage = $"Đã xóa câu hỏi #{id} khỏi SQL Server.";
                PrepareNewQuestion();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khi xóa: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
