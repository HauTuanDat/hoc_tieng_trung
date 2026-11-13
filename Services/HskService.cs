using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using Microsoft.Data.SqlClient;
using QuizApp.Models;

namespace QuizApp.Services;

public class HskService : IHskService
{
    private readonly IDatabaseService _databaseService;

    public HskService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<int> GetTotalVocabCountAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(*) FROM ChineseVocabularies";
            await using var cmd = new SqlCommand(sql, conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch
        {
            return 0;
        }
    }

    public async Task<int> GetLearnedCountByLevelAsync(string level)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(*) FROM ChineseVocabularies WHERE (IsMastered = 1 OR PracticeCount > 0)";
            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND Level = @Level";
            }

            await using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@Level", level);
            }
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch
        {
            return 0;
        }
    }

    public async Task<List<HskLevelProgress>> GetLevelProgressListAsync()
    {
        var meta = new List<HskLevelProgress>
        {
            new() { LevelName = "HSK 1", Title = "HSK 1", Subtitle = "Nhập môn", Description = "150 từ vựng căn bản về chào hỏi, số đếm, gia đình, thời gian và giao tiếp hàng ngày.", Icon = "🌱", GradientStart = "#FF8A00", GradientEnd = "#E52E71", AccentColor = "#FF8A00", LightBgColor = "#FFF7ED" },
            new() { LevelName = "HSK 2", Title = "HSK 2", Subtitle = "Sơ cấp", Description = "150 từ vựng sơ cấp về mua sắm, hỏi đường, ăn uống, thói quen và sở thích sinh hoạt.", Icon = "🚲", GradientStart = "#10B981", GradientEnd = "#059669", AccentColor = "#10B981", LightBgColor = "#ECFDF5" },
            new() { LevelName = "HSK 3", Title = "HSK 3", Subtitle = "Sơ trung cấp", Description = "300 từ vựng sơ trung cấp về công việc, du lịch, cảm xúc và biểu đạt suy nghĩ.", Icon = "🐼", GradientStart = "#00C0FF", GradientEnd = "#0072FF", AccentColor = "#0284C7", LightBgColor = "#F0F9FF" },
            new() { LevelName = "HSK 4", Title = "HSK 4", Subtitle = "Trung cấp", Description = "600 từ vựng trung cấp thảo luận các chủ đề đời sống, xã hội, văn phòng và văn hóa.", Icon = "🧳", GradientStart = "#6C5CE7", GradientEnd = "#5F27CD", AccentColor = "#7C3AED", LightBgColor = "#F5F3FF" },
            new() { LevelName = "HSK 5", Title = "HSK 5", Subtitle = "Trung cao cấp", Description = "1,300 từ vựng trung cao cấp đọc báo chí, xem phim tài liệu, thuyết trình và đàm phán.", Icon = "🚀", GradientStart = "#EC4899", GradientEnd = "#BE185D", AccentColor = "#DB2777", LightBgColor = "#FDF2F8" },
            new() { LevelName = "HSK 6", Title = "HSK 6", Subtitle = "Cao cấp", Description = "2,500 từ vựng cao cấp, học thuật, thành ngữ và biểu đạt tự nhiên như người bản xứ.", Icon = "👑", GradientStart = "#C01538", GradientEnd = "#880E25", AccentColor = "#E11D48", LightBgColor = "#FFF1F2" },
            new() { LevelName = "Kế toán", Title = "Kế toán", Subtitle = "Nâng cao • 会计", Description = "490 từ vựng chuyên sâu về hóa đơn, chứng từ, khai báo thuế, kiểm toán và báo cáo tài chính doanh nghiệp.", Icon = "🧮", GradientStart = "#1D68EC", GradientEnd = "#0C43B7", AccentColor = "#2563EB", LightBgColor = "#EFF6FF" },
            new() { LevelName = "Xuất nhập khẩu", Title = "Xuất nhập khẩu", Subtitle = "Nâng cao • 进出口", Description = "480 từ vựng chuyên ngành logistics, vận tải biển hàng không, hải quan thông quan và thanh toán L/C.", Icon = "🚢", GradientStart = "#0D9488", GradientEnd = "#0A5C54", AccentColor = "#0D9488", LightBgColor = "#F0FDFA" },
            new() { LevelName = "Công xưởng", Title = "Công xưởng", Subtitle = "Nâng cao • 工厂", Description = "771 từ vựng kỹ thuật, thiết bị máy móc, dây chuyền sản xuất, an toàn lao động và kiểm phẩm QC.", Icon = "🏭", GradientStart = "#EA580C", GradientEnd = "#9A3412", AccentColor = "#EA580C", LightBgColor = "#FFF7ED" }
        };

        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT Level, COUNT(*) AS Total, SUM(CASE WHEN (IsMastered = 1 OR PracticeCount > 0) THEN 1 ELSE 0 END) AS Mastered
            FROM ChineseVocabularies
            GROUP BY Level";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var stats = new Dictionary<string, (int Total, int Mastered)>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync())
            {
                string lvl = reader.GetString(0);
                int total = reader.GetInt32(1);
                int mastered = reader.GetInt32(2);
                stats[lvl] = (total, mastered);
            }

            foreach (var item in meta)
            {
                if (stats.TryGetValue(item.LevelName, out var st))
                {
                    item.TotalWords = st.Total;
                    item.MasteredWords = st.Mastered;
                }
            }
        }
        catch
        {
            // Return defaults if connection issue
        }

        return meta;
    }

    public async Task<List<ChineseVocabulary>> GetVocabulariesAsync(string? level = null, string? searchQuery = null, int limit = 5000)
    {
        var list = new List<ChineseVocabulary>();
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            var sb = new StringBuilder(@"
            SELECT TOP (@Limit) Id, Level, OrderNumber, Word, Pinyin, Meaning, 
                   ExampleHanzi, ExamplePinyin, ExampleMeaning, IsMastered, PracticeCount, 
                   LastPracticed, CreatedAt 
            FROM ChineseVocabularies 
            WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" AND Level = @Level ");
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                sb.Append(" AND (Word LIKE @Query OR Pinyin LIKE @Query OR Meaning LIKE @Query) ");
            }

            sb.Append(" ORDER BY Level ASC, OrderNumber ASC");

            await using var cmd = new SqlCommand(sb.ToString(), conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@Level", level);
            }
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                cmd.Parameters.AddWithValue("@Query", $"%{searchQuery}%");
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadVocab(reader));
            }
        }
        catch
        {
        }

        return list;
    }

    public async Task<List<ChineseVocabulary>> GetLearnedVocabulariesAsync(string level, string? searchQuery = null)
    {
        var list = new List<ChineseVocabulary>();
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            var sb = new StringBuilder(@"
            SELECT Id, Level, OrderNumber, Word, Pinyin, Meaning, 
                   ExampleHanzi, ExamplePinyin, ExampleMeaning, IsMastered, PracticeCount, 
                   LastPracticed, CreatedAt 
            FROM ChineseVocabularies 
            WHERE (IsMastered = 1 OR PracticeCount > 0) ");

            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" AND Level = @Level ");
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                sb.Append(" AND (Word LIKE @Query OR Pinyin LIKE @Query OR Meaning LIKE @Query) ");
            }

            sb.Append(" ORDER BY LastPracticed DESC, OrderNumber ASC");

            await using var cmd = new SqlCommand(sb.ToString(), conn);
            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@Level", level);
            }
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                cmd.Parameters.AddWithValue("@Query", $"%{searchQuery}%");
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadVocab(reader));
            }
        }
        catch
        {
        }

        return list;
    }

    public async Task MarkMasteredAsync(int id, bool isMastered)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = @"
            UPDATE ChineseVocabularies 
            SET IsMastered = @IsMastered, 
                PracticeCount = CASE WHEN @IsMastered = 1 AND PracticeCount = 0 THEN 1 ELSE PracticeCount END,
                LastPracticed = CASE WHEN @IsMastered = 1 THEN GETDATE() ELSE LastPracticed END
            WHERE Id = @Id";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IsMastered", isMastered);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task UnlearnWordAsync(int id)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "UPDATE ChineseVocabularies SET IsMastered = 0, PracticeCount = 0, LastPracticed = NULL WHERE Id = @Id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task ResetLevelProgressAsync(string level)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "UPDATE ChineseVocabularies SET IsMastered = 0, PracticeCount = 0, LastPracticed = NULL WHERE Level = @Level";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Level", level);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task RecordPracticeAsync(int id, string? typedWord = null, string? typedSentence = null, bool markMastered = true)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = @"
            UPDATE ChineseVocabularies 
            SET PracticeCount = PracticeCount + 1, 
                IsMastered = CASE WHEN @MarkMastered = 1 THEN 1 ELSE IsMastered END,
                LastPracticed = GETDATE() 
            WHERE Id = @Id;

            SELECT Level, Word FROM ChineseVocabularies WHERE Id = @Id;";

            string vocabLevel = "HSK 1";
            string vocabWord = string.Empty;

            await using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@MarkMastered", markMastered ? 1 : 0);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    vocabLevel = reader.GetString(0);
                    vocabWord = reader.GetString(1);
                }
            }

            // Also record in TypingHistory if typed input is provided
            if (!string.IsNullOrWhiteSpace(typedWord))
            {
                string histSql = @"
                INSERT INTO TypingHistory (VocabId, Level, Word, TypedWord, TypedSentence, IsCorrect, CreatedAt)
                VALUES (@VocabId, @Level, @Word, @TypedWord, @TypedSentence, 1, GETDATE());";

                await using var histCmd = new SqlCommand(histSql, conn);
                histCmd.Parameters.AddWithValue("@VocabId", id);
                histCmd.Parameters.AddWithValue("@Level", vocabLevel);
                histCmd.Parameters.AddWithValue("@Word", vocabWord);
                histCmd.Parameters.AddWithValue("@TypedWord", typedWord);
                histCmd.Parameters.AddWithValue("@TypedSentence", (object?)typedSentence ?? DBNull.Value);
                await histCmd.ExecuteNonQueryAsync();
            }
        }
        catch { }
    }

    public async Task SaveTypingProgressAsync(string level, int lastIndex, int vocabId)
    {
        if (string.IsNullOrWhiteSpace(level)) return;
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = @"
            IF EXISTS (SELECT 1 FROM TypingProgress WHERE Level = @Level)
                UPDATE TypingProgress SET LastIndex = @LastIndex, LastVocabId = @LastVocabId, UpdatedAt = GETDATE() WHERE Level = @Level
            ELSE
                INSERT INTO TypingProgress (Level, LastIndex, LastVocabId, UpdatedAt) VALUES (@Level, @LastIndex, @LastVocabId, GETDATE());";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Level", level);
            cmd.Parameters.AddWithValue("@LastIndex", lastIndex);
            cmd.Parameters.AddWithValue("@LastVocabId", vocabId);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task<(int LastIndex, int LastVocabId)?> GetTypingProgressAsync(string level)
    {
        if (string.IsNullOrWhiteSpace(level)) return null;
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "SELECT LastIndex, LastVocabId FROM TypingProgress WHERE Level = @Level";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Level", level);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int lastIdx = reader.GetInt32(0);
                int lastVocabId = reader.GetInt32(1);
                return (lastIdx, lastVocabId);
            }
        }
        catch { }
        return null;
    }

    public async Task<(string TypedWord, string? TypedSentence)?> GetLastTypedRecordAsync(int vocabId)
    {
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string sql = "SELECT TOP 1 TypedWord, TypedSentence FROM TypingHistory WHERE VocabId = @VocabId ORDER BY Id DESC";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@VocabId", vocabId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string tw = reader.GetString(0);
                string? ts = reader.IsDBNull(1) ? null : reader.GetString(1);
                return (tw, ts);
            }
        }
        catch { }
        return null;
    }

    public async Task<List<ChineseVocabulary>> GetVocabulariesForTypingAsync(string level, string filterMode = "ALL", int limit = 5000)
    {
        var list = new List<ChineseVocabulary>();
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            var sb = new StringBuilder(@"
            SELECT TOP (@Limit) Id, Level, OrderNumber, Word, Pinyin, Meaning, 
                   ExampleHanzi, ExamplePinyin, ExampleMeaning, IsMastered, PracticeCount, 
                   LastPracticed, CreatedAt 
            FROM ChineseVocabularies 
            WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" AND Level = @Level ");
            }

            if (string.Equals(filterMode, "Unmastered", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" AND (IsMastered = 0 AND PracticeCount = 0) ");
            }
            else if (string.Equals(filterMode, "Mastered", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" AND (IsMastered = 1 OR PracticeCount > 0) ");
            }

            sb.Append(" ORDER BY Level ASC, OrderNumber ASC");

            await using var cmd = new SqlCommand(sb.ToString(), conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@Level", level);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadVocab(reader));
            }
        }
        catch { }
        return list;
    }

    public async Task MarkBatchCompletedAsync(IEnumerable<int> wordIds, bool isMastered = true)
    {
        var idList = wordIds.ToList();
        if (idList.Count == 0) return;

        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string idCsv = string.Join(",", idList);
            string sql = $@"
            UPDATE ChineseVocabularies 
            SET PracticeCount = PracticeCount + 1, 
                IsMastered = 1,
                LastPracticed = GETDATE()
            WHERE Id IN ({idCsv})";

            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task<(List<Question> Questions, List<int> WordIds, bool IsAllCompleted)> GetNextQuizBatchAsync(
        string level, int batchSize = 20, string mode = "MeaningToHanzi")
    {
        var questions = new List<Question>();
        var wordIds = new List<int>();

        var allLevelWords = await GetVocabulariesAsync(level, limit: 5000);
        if (allLevelWords.Count < 4)
        {
            allLevelWords = await GetVocabulariesAsync(null, limit: 5000);
        }
        if (allLevelWords.Count < 4) return (questions, wordIds, false);

        // STRICT RULE: Only select words that have NOT been learned yet (IsMastered == 0 and PracticeCount == 0)!
        var unlearnedWords = allLevelWords
            .Where(w => !w.IsMastered && w.PracticeCount == 0)
            .OrderBy(w => w.OrderNumber)
            .Take(batchSize)
            .ToList();

        if (unlearnedWords.Count == 0)
        {
            // ALL words in this HSK level have been completed!
            return (questions, wordIds, true);
        }

        var rng = new Random();
        foreach (var word in unlearnedWords)
        {
            wordIds.Add(word.Id);
            var q = BuildQuestionFromWord(word, allLevelWords, mode, rng);
            questions.Add(q);
        }

        return (questions, wordIds, false);
    }

    public async Task<List<Question>> GenerateReviewQuizForLearnedWordsAsync(string level, int count = 20, string mode = "MeaningToHanzi")
    {
        var questions = new List<Question>();
        var learnedWords = await GetLearnedVocabulariesAsync(level);
        if (learnedWords.Count < 4) return questions;

        var allWords = await GetVocabulariesAsync(level, limit: 5000);

        var rng = new Random();
        var selected = learnedWords.OrderBy(_ => rng.Next()).Take(count).ToList();

        foreach (var word in selected)
        {
            questions.Add(BuildQuestionFromWord(word, allWords, mode, rng));
        }

        return questions;
    }

    public async Task<List<Question>> GenerateQuizQuestionsAsync(string level, int count = 20, string mode = "MeaningToHanzi")
    {
        var (questions, _, _) = await GetNextQuizBatchAsync(level, batchSize: count, mode: mode);
        return questions;
    }

    private static Question BuildQuestionFromWord(ChineseVocabulary word, List<ChineseVocabulary> pool, string mode, Random rng)
    {
        var distractors = pool.Where(w => w.Id != word.Id)
                              .OrderBy(_ => rng.Next())
                              .Take(3)
                              .ToList();

        var options = new List<ChineseVocabulary> { word };
        options.AddRange(distractors);
        options = options.OrderBy(_ => rng.Next()).ToList();

        int correctIdx = options.IndexOf(word);
        string correctLetter = correctIdx switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            _ => "D"
        };

        Question q = new Question
        {
            Id = word.Id,
            Category = word.Level,
            CreatedAt = DateTime.Now
        };

        if (mode == "HanziToMeaning")
        {
            q.Tag = "🔤 NHẬN DIỆN CHỮ HÁN";
            q.Prompt = word.Word;
            q.Explanation = !string.IsNullOrWhiteSpace(word.ExampleHanzi)
                ? $"Ví dụ: {word.ExampleHanzi}"
                : "Chọn nghĩa tiếng Việt chính xác của chữ Hán trên.";
            q.PartOfSpeech = $"{word.Level} • Từ số {word.OrderNumber}";
            q.QuestionText = "Nghĩa tiếng Việt nào phù hợp nhất với chữ Hán trên?";

            q.OptionA = options[0].Meaning;
            q.OptionB = options[1].Meaning;
            q.OptionC = options[2].Meaning;
            q.OptionD = options[3].Meaning;
            q.CorrectAnswer = correctLetter;
            q.ExplanationAfterAnswer = $"{word.Word}: {word.Meaning}. " +
                (!string.IsNullOrWhiteSpace(word.ExampleMeaning) ? $"Ví dụ: {word.ExampleMeaning}" : "");
        }
        else
        {
            q.Tag = "✨ NGHĨA TIẾNG TRUNG";
            q.Prompt = word.Meaning;
            q.Explanation = !string.IsNullOrWhiteSpace(word.ExampleMeaning)
                ? $"Ví dụ: {word.ExampleMeaning}"
                : "Hãy chọn chữ Hán đúng mang nghĩa trên.";
            q.PartOfSpeech = $"{word.Level} • Từ vựng HSK";
            q.QuestionText = "Chữ Hán nào phù hợp với nghĩa trên?";

            // KHÔNG CÓ PINYIN: Chỉ hiển thị Chữ Hán thuần túy để người học nhận diện mặt chữ
            q.OptionA = options[0].Word;
            q.OptionB = options[1].Word;
            q.OptionC = options[2].Word;
            q.OptionD = options[3].Word;
            q.CorrectAnswer = correctLetter;
            q.ExplanationAfterAnswer = $"Đáp án đúng là: {word.Word} mang nghĩa \"{word.Meaning}\". " +
                (!string.IsNullOrWhiteSpace(word.ExampleHanzi) ? $"\nCâu mẫu: {word.ExampleHanzi}" : "");
        }

        return q;
    }

    private static ChineseVocabulary ReadVocab(SqlDataReader reader)
    {
        return new ChineseVocabulary
        {
            Id = reader.GetInt32(0),
            Level = reader.GetString(1),
            OrderNumber = reader.GetInt32(2),
            Word = reader.GetString(3),
            Pinyin = reader.GetString(4),
            Meaning = reader.GetString(5),
            ExampleHanzi = reader.IsDBNull(6) ? null : reader.GetString(6),
            ExamplePinyin = reader.IsDBNull(7) ? null : reader.GetString(7),
            ExampleMeaning = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsMastered = reader.GetBoolean(9),
            PracticeCount = reader.GetInt32(10),
            LastPracticed = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            CreatedAt = reader.GetDateTime(12)
        };
    }

    public async Task<int> ImportFromExcelAsync(string filePath, IProgress<(int Current, int Total, string Status)>? progress = null)
    {
        return await Task.Run(async () =>
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file Excel tại: {filePath}");
            }

            progress?.Report((0, 100, "Đang mở tệp Excel..."));

            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChineseVocabularies')
            BEGIN
                CREATE TABLE ChineseVocabularies (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Level NVARCHAR(20) NOT NULL,
                    OrderNumber INT NOT NULL,
                    Word NVARCHAR(100) NOT NULL,
                    Pinyin NVARCHAR(200) NOT NULL,
                    Meaning NVARCHAR(1000) NOT NULL,
                    ExampleHanzi NVARCHAR(1000) NULL,
                    ExamplePinyin NVARCHAR(1000) NULL,
                    ExampleMeaning NVARCHAR(1000) NULL,
                    IsMastered BIT NOT NULL DEFAULT 0,
                    PracticeCount INT NOT NULL DEFAULT 0,
                    LastPracticed DATETIME NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_ChineseVocabularies_Level ON ChineseVocabularies(Level);
            END";
            await using (var cmd = new SqlCommand(createTableSql, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var clearCmd = new SqlCommand("TRUNCATE TABLE ChineseVocabularies", conn))
            {
                await clearCmd.ExecuteNonQueryAsync();
            }

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Level", typeof(string));
            dataTable.Columns.Add("OrderNumber", typeof(int));
            dataTable.Columns.Add("Word", typeof(string));
            dataTable.Columns.Add("Pinyin", typeof(string));
            dataTable.Columns.Add("Meaning", typeof(string));
            dataTable.Columns.Add("ExampleHanzi", typeof(string));
            dataTable.Columns.Add("ExamplePinyin", typeof(string));
            dataTable.Columns.Add("ExampleMeaning", typeof(string));
            dataTable.Columns.Add("IsMastered", typeof(bool));
            dataTable.Columns.Add("PracticeCount", typeof(int));
            dataTable.Columns.Add("CreatedAt", typeof(DateTime));

            int totalRows = 0;
            foreach (DataTable sheet in ds.Tables)
            {
                string levelName = sheet.TableName.Trim();
                if (levelName.Equals("HSK1", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 1";
                else if (levelName.Equals("HSK2", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 2";
                else if (levelName.Equals("HSK3", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 3";
                else if (levelName.Equals("HSK4", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 4";
                else if (levelName.Equals("HSK5", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 5";
                else if (levelName.Equals("HSK6", StringComparison.OrdinalIgnoreCase)) levelName = "HSK 6";

                for (int r = 1; r < sheet.Rows.Count; r++)
                {
                    var row = sheet.Rows[r];
                    string word = row[1]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(word)) continue;

                    int orderNum = 0;
                    int.TryParse(row[0]?.ToString()?.Trim(), out orderNum);
                    if (orderNum == 0) orderNum = r;

                    string pinyin = row.ItemArray.Length > 4 ? row[4]?.ToString()?.Trim() ?? "" : "";
                    string meaning = row.ItemArray.Length > 5 ? row[5]?.ToString()?.Trim() ?? "" : "";
                    string exHanzi = row.ItemArray.Length > 6 ? row[6]?.ToString()?.Trim() ?? "" : "";
                    string exPinyin = row.ItemArray.Length > 9 ? row[9]?.ToString()?.Trim() ?? "" : "";
                    string exMeaning = row.ItemArray.Length > 10 ? row[10]?.ToString()?.Trim() ?? "" : "";

                    dataTable.Rows.Add(
                        levelName,
                        orderNum,
                        word,
                        pinyin,
                        meaning,
                        string.IsNullOrWhiteSpace(exHanzi) ? (object)DBNull.Value : exHanzi,
                        string.IsNullOrWhiteSpace(exPinyin) ? (object)DBNull.Value : exPinyin,
                        string.IsNullOrWhiteSpace(exMeaning) ? (object)DBNull.Value : exMeaning,
                        false,
                        0,
                        DateTime.Now
                    );
                    totalRows++;
                }
            }

            progress?.Report((50, 100, $"Đang nạp {totalRows} từ vựng vào SQL Server..."));

            using var bulkCopy = new SqlBulkCopy(conn);
            bulkCopy.DestinationTableName = "ChineseVocabularies";
            bulkCopy.ColumnMappings.Add("Level", "Level");
            bulkCopy.ColumnMappings.Add("OrderNumber", "OrderNumber");
            bulkCopy.ColumnMappings.Add("Word", "Word");
            bulkCopy.ColumnMappings.Add("Pinyin", "Pinyin");
            bulkCopy.ColumnMappings.Add("Meaning", "Meaning");
            bulkCopy.ColumnMappings.Add("ExampleHanzi", "ExampleHanzi");
            bulkCopy.ColumnMappings.Add("ExamplePinyin", "ExamplePinyin");
            bulkCopy.ColumnMappings.Add("ExampleMeaning", "ExampleMeaning");
            bulkCopy.ColumnMappings.Add("IsMastered", "IsMastered");
            bulkCopy.ColumnMappings.Add("PracticeCount", "PracticeCount");
            bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

            bulkCopy.WriteToServer(dataTable);
            progress?.Report((100, 100, $"Hoàn thành nạp {totalRows} từ vựng!"));

            return totalRows;
        });
    }

    private static Dictionary<string, string>? _wordPinyinDict;
    private static readonly object _dictLock = new();

    private async Task<Dictionary<string, string>> GetWordPinyinDictAsync(SqlConnection conn)
    {
        if (_wordPinyinDict != null) return _wordPinyinDict;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Preload common particles & high frequency single characters
        var commons = new Dictionary<string, string>
        {
            ["的"] = "de", ["地"] = "de", ["得"] = "de", ["了"] = "le", ["着"] = "zhe", ["过"] = "guò",
            ["吗"] = "ma", ["呢"] = "ne", ["吧"] = "ba", ["啊"] = "a", ["们"] = "men", ["天"] = "tiān",
            ["名"] = "míng", ["个"] = "gè", ["很"] = "hěn", ["大"] = "dà", ["小"] = "xiǎo", ["有"] = "yǒu",
            ["是"] = "shì", ["在"] = "zài", ["不"] = "bù", ["没"] = "méi", ["好"] = "hǎo", ["人"] = "rén",
            ["这"] = "zhè", ["那"] = "nà", ["去"] = "qù", ["来"] = "lái", ["吃"] = "chī", ["喝"] = "hē",
            ["说"] = "shuō", ["看"] = "kàn", ["听"] = "tīng", ["写"] = "xiě", ["读"] = "dú", ["买"] = "mǎi",
            ["我"] = "wǒ", ["你"] = "nǐ", ["他"] = "tā", ["她"] = "tā", ["它"] = "tā", ["我们"] = "wǒmen",
            ["你们"] = "nǐmen", ["他们"] = "tāmen", ["她们"] = "tāmen", ["老师"] = "lǎoshī", ["学生"] = "xuéshēng",
            ["也"] = "yě", ["都"] = "dōu", ["想"] = "xiǎng", ["要"] = "yào", ["能"] = "néng", ["会"] = "huì",
            ["杯"] = "bēi", ["水"] = "shuǐ", ["中"] = "zhōng", ["需"] = "xū", ["发"] = "fā", ["放"] = "fàng",
            ["优"] = "yōu", ["化"] = "huà", ["传"] = "chuán", ["车"] = "chē", ["站"] = "zhàn", ["开"] = "kāi",
            ["多"] = "duō", ["少"] = "shǎo", ["点"] = "diǎn", ["钱"] = "qián", ["分"] = "fēn", ["上"] = "shàng",
            ["下"] = "xià", ["前"] = "qián", ["后"] = "hòu", ["里"] = "lǐ", ["外"] = "wài", ["家"] = "jiā",
            ["国"] = "guó", ["生"] = "shēng", ["作"] = "zuò", ["工"] = "gōng", ["出"] = "chū", ["入"] = "rù",
            ["进"] = "jìn", ["到"] = "dào", ["问"] = "wèn", ["答"] = "dá", ["请"] = "qǐng", ["见"] = "jiàn",
            ["话"] = "huà", ["字"] = "zì", ["还"] = "hái", ["快"] = "kuài", ["慢"] = "màn", ["真"] = "zhēn"
        };
        foreach (var kv in commons) dict[kv.Key] = kv.Value;

        try
        {
            const string sql = "SELECT Word, Pinyin FROM ChineseVocabularies WHERE Word IS NOT NULL AND Pinyin IS NOT NULL";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string word = reader.GetString(0).Trim();
                string pinyin = reader.GetString(1).Trim();
                if (!string.IsNullOrEmpty(word) && !dict.ContainsKey(word))
                {
                    dict[word] = pinyin;
                }
            }
        }
        catch { }

        lock (_dictLock)
        {
            _wordPinyinDict = dict;
        }

        return _wordPinyinDict;
    }

    public async Task<List<SentenceScrambleQuestion>> GetSentenceScrambleBatchAsync(string level, int count = 10)
    {
        var result = new List<SentenceScrambleQuestion>();
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync();

            var dict = await GetWordPinyinDictAsync(conn);

            // 1. Fetch candidate sentences
            var candidates = new List<(int Id, string Level, string Word, string Pinyin, string ExHanzi, string ExPinyin, string ExMeaning)>();
            string sqlSentences = @"
                SELECT TOP 120 Id, Level, Word, Pinyin, ExampleHanzi, ExamplePinyin, ExampleMeaning 
                FROM ChineseVocabularies 
                WHERE ExampleHanzi IS NOT NULL AND LEN(ExampleHanzi) >= 4 AND ExampleMeaning IS NOT NULL";

            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sqlSentences += " AND Level = @Level";
            }
            sqlSentences += " ORDER BY NEWID()";

            await using (var cmd = new SqlCommand(sqlSentences, conn))
            {
                if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    cmd.Parameters.Add(new SqlParameter("@Level", SqlDbType.NVarChar, 100) { Value = level });
                }
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add((
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                    ));
                }
            }

            // 2. Fetch candidate distractors for the level
            var distractorPool = new List<(string Word, string Pinyin)>();
            string sqlDistractors = @"
                SELECT TOP 120 Word, Pinyin 
                FROM ChineseVocabularies 
                WHERE LEN(Word) <= 4 AND Pinyin IS NOT NULL";

            if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                sqlDistractors += " AND Level = @Level";
            }
            sqlDistractors += " ORDER BY NEWID()";

            await using (var cmdDist = new SqlCommand(sqlDistractors, conn))
            {
                if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    cmdDist.Parameters.Add(new SqlParameter("@Level", SqlDbType.NVarChar, 100) { Value = level });
                }
                await using var reader = await cmdDist.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string w = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
                    string p = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
                    if (!string.IsNullOrEmpty(w) && !distractorPool.Any(x => x.Word == w))
                    {
                        distractorPool.Add((w, p));
                    }
                }
            }

            var rng = new Random();
            char[] trimChars = { ' ', '\t', '\r', '\n', '。', '！', '？', '!', '?', '.', ',', '，', '、', '”', '“', '’', '‘', '；', '：' };

            foreach (var item in candidates)
            {
                string cleanHanzi = item.ExHanzi.Trim(trimChars);
                if (cleanHanzi.Length < 3 || cleanHanzi.Length > 30) continue;
                if (string.IsNullOrWhiteSpace(item.ExMeaning)) continue;

                // Forward Maximum Matching Tokenization
                var correctTokens = new List<SentenceWordToken>();
                int idx = 0;
                int tokenId = 1;

                while (idx < cleanHanzi.Length)
                {
                    // Skip punctuation within sentence
                    if (trimChars.Contains(cleanHanzi[idx]))
                    {
                        idx++;
                        continue;
                    }

                    bool matched = false;
                    int maxLen = Math.Min(4, cleanHanzi.Length - idx);
                    for (int len = maxLen; len >= 1; len--)
                    {
                        string sub = cleanHanzi.Substring(idx, len);
                        if (dict.TryGetValue(sub, out var pinyin))
                        {
                            correctTokens.Add(new SentenceWordToken
                            {
                                Id = tokenId++,
                                Hanzi = sub,
                                Pinyin = pinyin,
                                IsDistractor = false
                            });
                            idx += len;
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                    {
                        string singleChar = cleanHanzi.Substring(idx, 1);
                        dict.TryGetValue(singleChar, out var p);
                        correctTokens.Add(new SentenceWordToken
                        {
                            Id = tokenId++,
                            Hanzi = singleChar,
                            Pinyin = p ?? string.Empty,
                            IsDistractor = false
                        });
                        idx++;
                    }
                }

                // Ideal token count for Duolingo scramble: 2 to 12 tokens
                if (correctTokens.Count < 2 || correctTokens.Count > 12) continue;

                // Pick 2-3 distractors that don't appear in cleanHanzi
                var chosenDistractors = distractorPool
                    .Where(d => !cleanHanzi.Contains(d.Word) && correctTokens.All(t => t.Hanzi != d.Word))
                    .OrderBy(_ => rng.Next())
                    .Take(Math.Min(3, Math.Max(2, 9 - correctTokens.Count)))
                    .Select(d => new SentenceWordToken
                    {
                        Id = tokenId++,
                        Hanzi = d.Word,
                        Pinyin = d.Pinyin,
                        IsDistractor = true
                    })
                    .ToList();

                // If not enough distractors from level, fallback to general dictionary words
                if (chosenDistractors.Count < 2)
                {
                    var fallbackWords = dict.Keys
                        .Where(w => w.Length >= 1 && w.Length <= 3 && !cleanHanzi.Contains(w) && correctTokens.All(t => t.Hanzi != w))
                        .OrderBy(_ => rng.Next())
                        .Take(2)
                        .ToList();

                    foreach (var fw in fallbackWords)
                    {
                        chosenDistractors.Add(new SentenceWordToken
                        {
                            Id = tokenId++,
                            Hanzi = fw,
                            Pinyin = dict.TryGetValue(fw, out var py) ? py : string.Empty,
                            IsDistractor = true
                        });
                    }
                }

                // Build AvailablePool: mix correct tokens + distractors and shuffle
                var poolList = correctTokens.Concat(chosenDistractors).OrderBy(_ => rng.Next()).ToList();
                var question = new SentenceScrambleQuestion
                {
                    VocabId = item.Id,
                    Level = item.Level,
                    TargetSentence = cleanHanzi,
                    TargetPinyin = item.ExPinyin,
                    VietnameseMeaning = item.ExMeaning,
                    Keyword = item.Word,
                    CorrectTokens = correctTokens,
                    AvailablePool = new System.Collections.ObjectModel.ObservableCollection<SentenceWordToken>(poolList),
                    SelectedTray = new System.Collections.ObjectModel.ObservableCollection<SentenceWordToken>()
                };

                result.Add(question);
                if (result.Count >= count) break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SentenceScramble Error]: {ex.Message}");
        }

        return result;
    }
}
