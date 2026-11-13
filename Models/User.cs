namespace QuizApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsPremium { get; set; } = true;
    public int WordsLearnedCount { get; set; } = 1248;
    public int ListeningCount { get; set; } = 18;
    public int WritingCount { get; set; } = 12;
    public int ReadingCount { get; set; } = 15;
    public int SentenceCount { get; set; } = 8;
    public int StreakDays { get; set; } = 12;
    public string CurrentLevel { get; set; } = "HSK 3";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
}

