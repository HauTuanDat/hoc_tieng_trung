namespace QuizApp.Models;

public class Question
{
    public int Id { get; set; }
    public string Category { get; set; } = "CƠ BẢN";
    public string Tag { get; set; } = "✨ NGHĨA CỦA TỪ";
    public string Prompt { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string PartOfSpeech { get; set; } = string.Empty;
    public string QuestionText { get; set; } = "Từ tiếng Anh nào phù hợp với nghĩa trên?";
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = "A"; // "A", "B", "C", "D"
    public string? ExplanationAfterAnswer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string GetOptionText(string optionKey)
    {
        return optionKey.ToUpperInvariant() switch
        {
            "A" => OptionA,
            "B" => OptionB,
            "C" => OptionC,
            "D" => OptionD,
            _ => string.Empty
        };
    }
}
