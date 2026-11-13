using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuizApp.Models;

public class SentenceWordToken : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool? _isCorrect;

    public int Id { get; set; }
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public bool IsDistractor { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool? IsCorrect
    {
        get => _isCorrect;
        set
        {
            if (_isCorrect != value)
            {
                _isCorrect = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

public class SentenceScrambleQuestion
{
    public int VocabId { get; set; }
    public string Level { get; set; } = "HSK 1";
    public string TargetSentence { get; set; } = string.Empty;
    public string TargetPinyin { get; set; } = string.Empty;
    public string VietnameseMeaning { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;

    public List<SentenceWordToken> CorrectTokens { get; set; } = new();
    public ObservableCollection<SentenceWordToken> AvailablePool { get; set; } = new();
    public ObservableCollection<SentenceWordToken> SelectedTray { get; set; } = new();

    public string CorrectCleanSentence => string.Concat(CorrectTokens.Select(t => t.Hanzi));
}
