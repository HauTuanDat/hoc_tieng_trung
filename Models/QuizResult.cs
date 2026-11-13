namespace QuizApp.Models;

public class UserAnswer
{
    public Question Question { get; set; } = null!;
    public string SelectedOption { get; set; } = string.Empty; // "A", "B", "C", "D"
    public bool IsCorrect => string.Equals(SelectedOption, Question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
    public string SelectedText => Question.GetOptionText(SelectedOption);
    public string CorrectText => Question.GetOptionText(Question.CorrectAnswer);
}

public class QuizResult
{
    public int TotalQuestions { get; set; } = 20;
    public int CorrectCount { get; set; }
    public int IncorrectCount => TotalQuestions - CorrectCount;
    public double ScorePercentage => TotalQuestions > 0 ? (double)CorrectCount / TotalQuestions * 100 : 0;
    public TimeSpan Duration { get; set; }
    public List<UserAnswer> UserAnswers { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.Now;

    public string Level { get; set; } = "HSK 1";
    public string Mode { get; set; } = "MeaningToHanzi";
    public List<int> WordIds { get; set; } = new();

    public bool IsPassing => ScorePercentage >= 70.0;
    public bool MustRetry => !IsPassing;

    public string GradeEvaluation
    {
        get
        {
            if (ScorePercentage >= 90) return "Xuất sắc! Bạn nắm từ vựng rất vững 🎉";
            if (ScorePercentage >= 70) return "Đạt yêu cầu! Đã vượt mốc 70% 👏";
            if (ScorePercentage >= 50) return "Chưa đạt 70%! Bắt buộc cần thi lại đợt này 💪";
            return "Điểm quá thấp (< 50%)! Bắt buộc phải ôn luyện và thi lại 📚";
        }
    }
}
