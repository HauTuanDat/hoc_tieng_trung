namespace QuizApp.Models;

public class ChineseVocabulary
{
    public int Id { get; set; }
    public string Level { get; set; } = "HSK 1"; // "HSK 1" -> "HSK 6"
    public int OrderNumber { get; set; }
    public string Word { get; set; } = string.Empty; // Chữ Hán
    public string Pinyin { get; set; } = string.Empty; // Phiên âm
    public string Meaning { get; set; } = string.Empty; // Nghĩa tiếng Việt
    public string? ExampleHanzi { get; set; } // Ví dụ chữ Hán
    public string? ExamplePinyin { get; set; } // Ví dụ phiên âm
    public string? ExampleMeaning { get; set; } // Ví dụ nghĩa tiếng Việt
    public bool IsMastered { get; set; } // Đã học / Đã thuộc
    public int PracticeCount { get; set; } // Số lần luyện tập
    public DateTime? LastPracticed { get; set; } // Thời điểm học gần nhất
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string DisplayText => $"{Word} ({Pinyin})";
    public string LearnedDateText => LastPracticed.HasValue 
        ? LastPracticed.Value.ToString("dd/MM/yyyy HH:mm") 
        : CreatedAt.ToString("dd/MM/yyyy");
}
