using QuizApp.Models;

namespace QuizApp.Services;

public interface IHskService
{
    Task<int> ImportFromExcelAsync(string filePath, IProgress<(int Current, int Total, string Status)>? progress = null);
    Task<List<HskLevelProgress>> GetLevelProgressListAsync();
    Task<List<ChineseVocabulary>> GetVocabulariesAsync(string? level = null, string? searchQuery = null, int limit = 5000);
    Task<List<ChineseVocabulary>> GetLearnedVocabulariesAsync(string level, string? searchQuery = null);
    Task<int> GetTotalVocabCountAsync();
    Task<int> GetLearnedCountByLevelAsync(string level);
    Task<List<Question>> GenerateQuizQuestionsAsync(string level, int count = 20, string mode = "MeaningToHanzi");
    Task<(List<Question> Questions, List<int> WordIds, bool IsAllCompleted)> GetNextQuizBatchAsync(string level, int batchSize = 20, string mode = "MeaningToHanzi");
    Task<List<Question>> GenerateReviewQuizForLearnedWordsAsync(string level, int count = 20, string mode = "MeaningToHanzi");
    Task MarkBatchCompletedAsync(IEnumerable<int> wordIds, bool isMastered = true);
    Task MarkMasteredAsync(int id, bool isMastered);
    Task UnlearnWordAsync(int id);
    Task ResetLevelProgressAsync(string level);
    Task RecordPracticeAsync(int id, string? typedWord = null, string? typedSentence = null, bool markMastered = true);
    Task SaveTypingProgressAsync(string level, int lastIndex, int vocabId);
    Task<(int LastIndex, int LastVocabId)?> GetTypingProgressAsync(string level);
    Task<(string TypedWord, string? TypedSentence)?> GetLastTypedRecordAsync(int vocabId);
    Task<List<ChineseVocabulary>> GetVocabulariesForTypingAsync(string level, string filterMode = "ALL", int limit = 5000);
    Task<List<SentenceScrambleQuestion>> GetSentenceScrambleBatchAsync(string level, int count = 10);
}
