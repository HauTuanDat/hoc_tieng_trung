using QuizApp.Models;

namespace QuizApp.Services;

public interface IDatabaseService
{
    string ConnectionString { get; set; }
    Task<(bool Success, string Message)> TestConnectionAsync();
    Task InitializeDatabaseAsync();
    Task<List<Question>> GetAllQuestionsAsync();
    Task<Question?> GetQuestionByIdAsync(int id);
    Task<int> AddQuestionAsync(Question question);
    Task<bool> UpdateQuestionAsync(Question question);
    Task<bool> DeleteQuestionAsync(int id);
    Task SaveQuizResultAsync(QuizResult result);

    // Authentication & User Management
    Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrEmail, string password);
    Task<(bool Success, string Message, User? User)> RegisterAsync(string fullName, string emailOrUsername, string password);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> UpdateUserProfileAsync(User user);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}
