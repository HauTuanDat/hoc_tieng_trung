namespace QuizApp.ViewModels;

public class OptionViewModel : ViewModelBase
{
    private string _key = "A";
    private string _text = string.Empty;
    private bool _isSelected;
    private bool _isAnswerChecked;
    private bool _isCorrectOption;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(StatusDescription));
            }
        }
    }

    public bool IsAnswerChecked
    {
        get => _isAnswerChecked;
        set
        {
            if (SetProperty(ref _isAnswerChecked, value))
            {
                OnPropertyChanged(nameof(IsCorrectHighlight));
                OnPropertyChanged(nameof(IsWrongHighlight));
                OnPropertyChanged(nameof(StatusDescription));
            }
        }
    }

    public bool IsCorrectOption
    {
        get => _isCorrectOption;
        set
        {
            if (SetProperty(ref _isCorrectOption, value))
            {
                OnPropertyChanged(nameof(IsCorrectHighlight));
                OnPropertyChanged(nameof(IsWrongHighlight));
            }
        }
    }

    public bool IsCorrectHighlight => IsAnswerChecked && IsCorrectOption;
    public bool IsWrongHighlight => IsAnswerChecked && IsSelected && !IsCorrectOption;

    public string StatusDescription
    {
        get
        {
            if (!IsAnswerChecked)
            {
                return IsSelected ? "Đã chọn" : "";
            }
            if (IsCorrectOption) return "✓ Đáp án đúng";
            if (IsSelected && !IsCorrectOption) return "✕ Chưa chính xác";
            return "";
        }
    }

    public void Reset()
    {
        IsSelected = false;
        IsAnswerChecked = false;
        IsCorrectOption = false;
    }
}
