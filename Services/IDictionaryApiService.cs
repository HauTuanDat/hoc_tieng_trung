using QuizApp.Models;

namespace QuizApp.Services;

public interface IDictionaryApiService
{
    Task<List<DictionaryWordEntry>> LookupAsync(string query, CancellationToken ct = default);
    Task<List<DictionaryExample>> GetExamplesAsync(string word, CancellationToken ct = default);
}
