using System.Data;
using Microsoft.Data.SqlClient;
using QuizApp.Models;

namespace QuizApp.Services;

public class SqlServerDatabaseService : IDatabaseService
{
    private string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuizDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return (true, "Kết nối SQL Server thành công!");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // First check if database exists by connecting to master
            var builder = new SqlConnectionStringBuilder(_connectionString);
            string targetDb = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(targetDb))
            {
                targetDb = "QuizDb";
            }

            builder.InitialCatalog = "master";
            string masterConnectionString = builder.ConnectionString;

            await using (var masterConn = new SqlConnection(masterConnectionString))
            {
                await masterConn.OpenAsync();
                string checkDbSql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{targetDb}') " +
                                    $"BEGIN CREATE DATABASE [{targetDb}] COLLATE Vietnamese_CI_AS; END";
                await using var cmd = new SqlCommand(checkDbSql, masterConn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Now connect to targetDb to ensure tables exist
            await using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string createQuestionsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Questions')
                BEGIN
                    CREATE TABLE Questions (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Category NVARCHAR(100) NOT NULL DEFAULT N'CƠ BẢN',
                        Tag NVARCHAR(100) NOT NULL DEFAULT N'✨ NGHĨA CỦA TỪ',
                        Prompt NVARCHAR(500) NOT NULL,
                        Explanation NVARCHAR(1000) NOT NULL,
                        PartOfSpeech NVARCHAR(200) NOT NULL,
                        QuestionText NVARCHAR(500) NOT NULL DEFAULT N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
                        OptionA NVARCHAR(200) NOT NULL,
                        OptionB NVARCHAR(200) NOT NULL,
                        OptionC NVARCHAR(200) NOT NULL,
                        OptionD NVARCHAR(200) NOT NULL,
                        CorrectAnswer NVARCHAR(10) NOT NULL,
                        ExplanationAfterAnswer NVARCHAR(1000) NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuizHistory')
                BEGIN
                    CREATE TABLE QuizHistory (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        TotalQuestions INT NOT NULL,
                        CorrectCount INT NOT NULL,
                        ScorePercentage FLOAT NOT NULL,
                        TimeTakenSeconds INT NOT NULL,
                        CompletedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END

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
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChineseVocabularies') AND name = 'LastPracticed')
                    BEGIN
                        ALTER TABLE ChineseVocabularies ADD LastPracticed DATETIME NULL;
                    END
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TypingProgress')
                BEGIN
                    CREATE TABLE TypingProgress (
                        Level NVARCHAR(20) PRIMARY KEY,
                        LastIndex INT NOT NULL DEFAULT 0,
                        LastVocabId INT NOT NULL DEFAULT 0,
                        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TypingHistory')
                BEGIN
                    CREATE TABLE TypingHistory (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        VocabId INT NOT NULL,
                        Level NVARCHAR(20) NOT NULL,
                        Word NVARCHAR(100) NOT NULL,
                        TypedWord NVARCHAR(200) NOT NULL,
                        TypedSentence NVARCHAR(1000) NULL,
                        IsCorrect BIT NOT NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                    CREATE INDEX IX_TypingHistory_VocabId ON TypingHistory(VocabId);
                    CREATE INDEX IX_TypingHistory_Level ON TypingHistory(Level);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE Users (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Username NVARCHAR(100) NOT NULL UNIQUE,
                        Email NVARCHAR(200) NOT NULL UNIQUE,
                        FullName NVARCHAR(200) NOT NULL,
                        PasswordHash NVARCHAR(256) NOT NULL,
                        AvatarUrl NVARCHAR(500) NULL,
                        IsPremium BIT NOT NULL DEFAULT 1,
                        WordsLearnedCount INT NOT NULL DEFAULT 0,
                        ListeningCount INT NOT NULL DEFAULT 0,
                        WritingCount INT NOT NULL DEFAULT 0,
                        ReadingCount INT NOT NULL DEFAULT 0,
                        SentenceCount INT NOT NULL DEFAULT 0,
                        StreakDays INT NOT NULL DEFAULT 0,
                        CurrentLevel NVARCHAR(50) NOT NULL DEFAULT N'HSK 1',
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                        LastLoginAt DATETIME NULL
                    );
                    CREATE INDEX IX_Users_Username ON Users(Username);
                    CREATE INDEX IX_Users_Email ON Users(Email);
                END

                IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'minhdat@gmail.com' OR Username = N'minhdat')
                BEGIN
                    INSERT INTO Users (Username, Email, FullName, PasswordHash, AvatarUrl, IsPremium, WordsLearnedCount, ListeningCount, WritingCount, ReadingCount, SentenceCount, StreakDays, CurrentLevel)
                    VALUES (N'minhdat', N'minhdat@gmail.com', N'Minh Đạt', N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Assets/avatar_minhdat.png', 1, 0, 0, 0, 0, 0, 0, N'HSK 1');
                END
                ";

                await using var createCmd = new SqlCommand(createQuestionsTableSql, conn);
                await createCmd.ExecuteNonQueryAsync();

                // Check if any question exists; if not, seed defaults
                string countSql = "SELECT COUNT(*) FROM Questions";
                await using var countCmd = new SqlCommand(countSql, conn);
                int count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    await SeedDefaultQuestionsAsync(conn);
                }
            }
        }
        catch (Exception)
        {
            // Propagate or let UI handle if DB server is offline
            throw;
        }
    }

    private async Task SeedDefaultQuestionsAsync(SqlConnection conn)
    {
        string seedSql = @"
        INSERT INTO Questions (Category, Tag, Prompt, Explanation, PartOfSpeech, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, ExplanationAfterAnswer)
        VALUES
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Tò mò, ham học hỏi', N'Muốn tìm hiểu, khám phá những điều mới lạ xung quanh.', N'Tính từ • Mô tả tính cách', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Curious', N'Bored', N'Careless', N'Anxious', N'A', N'""Curious"" nghĩa là tò mò, ham học hỏi.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Hào phóng, rộng lượng', N'Sẵn lòng cho đi, chia sẻ tiền bạc, thời gian hoặc sự giúp đỡ cho người khác.', N'Tính từ • Mô tả tính cách', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Selfish', N'Generous', N'Greedy', N'Strict', N'B', N'""Generous"" nghĩa là hào phóng, rộng lượng.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Tự tin, quyết đoán', N'Có niềm tin mạnh mẽ vào khả năng và giá trị của bản thân.', N'Tính từ • Mô tả tính cách', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Shy', N'Hesitant', N'Confident', N'Fearful', N'C', N'""Confident"" nghĩa là tự tin, tin tưởng vào bản thân.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Chăm chỉ, siêng năng', N'Luôn nỗ lực và cẩn thận trong công việc hoặc học tập.', N'Tính từ • Mô tả tính cách', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Curious', N'Diligent', N'Generous', N'Confident', N'B', N'""Diligent"" nghĩa là chăm chỉ, siêng năng, cần mẫn.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Kiên nhẫn, nhẫn nại', N'Khả năng chờ đợi hoặc đối mặt với khó khăn mà không tức giận hay nản lòng.', N'Tính từ • Phẩm chất cá nhân', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Patient', N'Aggressive', N'Stubborn', N'Impatient', N'A', N'""Patient"" mang nghĩa kiên nhẫn, nhẫn nại.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Đáng tin cậy, trung thực', N'Luôn nói sự thật, không lừa dối và có thể tin cậy được.', N'Tính từ • Đạo đức & Tính cách', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Deceitful', N'Sly', N'Honest', N'Boastful', N'C', N'""Honest"" nghĩa là trung thực, chân thật.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Lạc quan, yêu đời', N'Luôn nhìn vào mặt tích cực và hy vọng những điều tốt đẹp trong tương lai.', N'Tính từ • Thái độ sống', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Pessimistic', N'Depressed', N'Cynical', N'Optimistic', N'D', N'""Optimistic"" nghĩa là lạc quan, tích cực.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Sáng tạo, đổi mới', N'Có nhiều ý tưởng độc đáo và khả năng tạo ra những điều mới mẻ.', N'Tính từ • Năng lực tư duy', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Creative', N'Ordinary', N'Dull', N'Repetitive', N'A', N'""Creative"" nghĩa là có óc sáng tạo.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Lịch sự, nhã nhặn', N'Cư xử đúng mực, tôn trọng người khác trong giao tiếp và hành vi.', N'Tính từ • Ứng xử xã hội', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Rude', N'Impolite', N'Polite', N'Harsh', N'C', N'""Polite"" nghĩa là lịch sự, tao nhã.'),
        (N'CƠ BẢN', N'✨ NGHĨA CỦA TỪ', N'Dũng cảm, can đảm', N'Sẵn sàng đối mặt với nguy hiểm, nỗi sợ hãi hoặc đau đớn mà không lùi bước.', N'Tính từ • Phẩm chất cá nhân', N'Từ tiếng Anh nào phù hợp với nghĩa trên?', N'Cowardly', N'Brave', N'Timid', N'Anxious', N'B', N'""Brave"" nghĩa là dũng cảm, can trường.')
        ";

        await using var cmd = new SqlCommand(seedSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Question>> GetAllQuestionsAsync()
    {
        var list = new List<Question>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
        SELECT Id, Category, Tag, Prompt, Explanation, PartOfSpeech, QuestionText, 
               OptionA, OptionB, OptionC, OptionD, CorrectAnswer, ExplanationAfterAnswer, CreatedAt
        FROM Questions
        ORDER BY Id ASC";

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadQuestion(reader));
        }

        return list;
    }

    public async Task<Question?> GetQuestionByIdAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
        SELECT Id, Category, Tag, Prompt, Explanation, PartOfSpeech, QuestionText, 
               OptionA, OptionB, OptionC, OptionD, CorrectAnswer, ExplanationAfterAnswer, CreatedAt
        FROM Questions
        WHERE Id = @Id";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadQuestion(reader);
        }

        return null;
    }

    public async Task<int> AddQuestionAsync(Question question)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
        INSERT INTO Questions (Category, Tag, Prompt, Explanation, PartOfSpeech, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, ExplanationAfterAnswer)
        OUTPUT INSERTED.Id
        VALUES (@Category, @Tag, @Prompt, @Explanation, @PartOfSpeech, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer, @ExplanationAfterAnswer)";

        await using var cmd = new SqlCommand(sql, conn);
        AddParameters(cmd, question);

        int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        question.Id = newId;
        return newId;
    }

    public async Task<bool> UpdateQuestionAsync(Question question)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
        UPDATE Questions
        SET Category = @Category,
            Tag = @Tag,
            Prompt = @Prompt,
            Explanation = @Explanation,
            PartOfSpeech = @PartOfSpeech,
            QuestionText = @QuestionText,
            OptionA = @OptionA,
            OptionB = @OptionB,
            OptionC = @OptionC,
            OptionD = @OptionD,
            CorrectAnswer = @CorrectAnswer,
            ExplanationAfterAnswer = @ExplanationAfterAnswer
        WHERE Id = @Id";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", question.Id);
        AddParameters(cmd, question);

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "DELETE FROM Questions WHERE Id = @Id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task SaveQuizResultAsync(QuizResult result)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            INSERT INTO QuizHistory (TotalQuestions, CorrectCount, ScorePercentage, TimeTakenSeconds, CompletedAt)
            VALUES (@TotalQuestions, @CorrectCount, @ScorePercentage, @TimeTakenSeconds, @CompletedAt)";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TotalQuestions", result.TotalQuestions);
            cmd.Parameters.AddWithValue("@CorrectCount", result.CorrectCount);
            cmd.Parameters.AddWithValue("@ScorePercentage", result.ScorePercentage);
            cmd.Parameters.AddWithValue("@TimeTakenSeconds", (int)result.Duration.TotalSeconds);
            cmd.Parameters.AddWithValue("@CompletedAt", result.CompletedAt);

            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Non-critical, continue
        }
    }

    private static Question ReadQuestion(SqlDataReader reader)
    {
        return new Question
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            Tag = reader.GetString(reader.GetOrdinal("Tag")),
            Prompt = reader.GetString(reader.GetOrdinal("Prompt")),
            Explanation = reader.GetString(reader.GetOrdinal("Explanation")),
            PartOfSpeech = reader.GetString(reader.GetOrdinal("PartOfSpeech")),
            QuestionText = reader.GetString(reader.GetOrdinal("QuestionText")),
            OptionA = reader.GetString(reader.GetOrdinal("OptionA")),
            OptionB = reader.GetString(reader.GetOrdinal("OptionB")),
            OptionC = reader.GetString(reader.GetOrdinal("OptionC")),
            OptionD = reader.GetString(reader.GetOrdinal("OptionD")),
            CorrectAnswer = reader.GetString(reader.GetOrdinal("CorrectAnswer")),
            ExplanationAfterAnswer = reader.IsDBNull(reader.GetOrdinal("ExplanationAfterAnswer"))
                ? null
                : reader.GetString(reader.GetOrdinal("ExplanationAfterAnswer")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static void AddParameters(SqlCommand cmd, Question q)
    {
        cmd.Parameters.AddWithValue("@Category", (object?)q.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tag", (object?)q.Tag ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Prompt", (object?)q.Prompt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Explanation", (object?)q.Explanation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PartOfSpeech", (object?)q.PartOfSpeech ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@QuestionText", (object?)q.QuestionText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OptionA", (object?)q.OptionA ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OptionB", (object?)q.OptionB ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OptionC", (object?)q.OptionC ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OptionD", (object?)q.OptionD ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CorrectAnswer", (object?)q.CorrectAnswer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExplanationAfterAnswer", (object?)q.ExplanationAfterAnswer ?? DBNull.Value);
    }

    // ==========================================
    // Authentication & User Management Implementation
    // ==========================================

    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var sb = new System.Text.StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            await InitializeDatabaseAsync();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string cleanInput = (usernameOrEmail ?? string.Empty).Trim();
            string hash = HashPassword(password ?? string.Empty);

            string checkExistSql = "SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER(@Input) OR LOWER(Email) = LOWER(@Input)";
            await using (var checkCmd = new SqlCommand(checkExistSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Input", cleanInput);
                int existCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (existCount == 0)
                {
                    return (false, "Tài khoản hoặc email không tồn tại trên hệ thống!", null);
                }
            }

            string sql = @"
            SELECT TOP 1 * FROM Users 
            WHERE (LOWER(Username) = LOWER(@Input) OR LOWER(Email) = LOWER(@Input))
              AND PasswordHash = @Hash";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Input", cleanInput);
            cmd.Parameters.AddWithValue("@Hash", hash);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = ReadUser(reader);
                await reader.CloseAsync();

                // Update last login
                string updateSql = "UPDATE Users SET LastLoginAt = GETDATE() WHERE Id = @Id";
                await using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@Id", user.Id);
                await updateCmd.ExecuteNonQueryAsync();

                return (true, "Đăng nhập thành công!", user);
            }

            return (false, "Mật khẩu không chính xác! Vui lòng kiểm tra lại.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string fullName, string emailOrUsername, string password)
    {
        try
        {
            await InitializeDatabaseAsync();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string cleanName = (fullName ?? string.Empty).Trim();
            string cleanInput = (emailOrUsername ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanName))
            {
                return (false, "Vui lòng nhập họ và tên của bạn!", null);
            }

            if (string.IsNullOrWhiteSpace(cleanInput))
            {
                return (false, "Vui lòng nhập Email hoặc Tên tài khoản!", null);
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return (false, "Mật khẩu phải có độ dài tối thiểu 6 ký tự!", null);
            }

            // Check if username or email already exists
            string checkSql = "SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER(@Input) OR LOWER(Email) = LOWER(@Input)";
            await using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Input", cleanInput);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    return (false, "Tài khoản hoặc email này đã tồn tại trên hệ thống!", null);
                }
            }

            string username = cleanInput.Contains('@') ? cleanInput.Split('@')[0] : cleanInput;
            string email = cleanInput.Contains('@') ? cleanInput : $"{cleanInput}@mocngu.edu.vn";
            string hash = HashPassword(password);

            string insertSql = @"
            INSERT INTO Users (Username, Email, FullName, PasswordHash, AvatarUrl, IsPremium, WordsLearnedCount, ListeningCount, WritingCount, ReadingCount, SentenceCount, StreakDays, CurrentLevel, CreatedAt, LastLoginAt)
            OUTPUT INSERTED.Id
            VALUES (@Username, @Email, @FullName, @PasswordHash, @AvatarUrl, 1, 0, 0, 0, 0, 0, 1, N'HSK 1', GETDATE(), GETDATE())";

            await using var insertCmd = new SqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@Username", username);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@FullName", cleanName);
            insertCmd.Parameters.AddWithValue("@PasswordHash", hash);
            insertCmd.Parameters.AddWithValue("@AvatarUrl", "Assets/avatar_minhdat.png");

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            var newUser = new User
            {
                Id = newId,
                Username = username,
                Email = email,
                FullName = cleanName,
                PasswordHash = hash,
                AvatarUrl = "Assets/avatar_minhdat.png",
                IsPremium = true,
                WordsLearnedCount = 0,
                ListeningCount = 0,
                WritingCount = 0,
                ReadingCount = 0,
                SentenceCount = 0,
                StreakDays = 1,
                CurrentLevel = "HSK 1",
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };

            return (true, "Đăng ký tài khoản thành công!", newUser);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi cơ sở dữ liệu khi đăng ký: {ex.Message}", null);
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT TOP 1 * FROM Users WHERE Id = @Id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    public async Task<bool> UpdateUserProfileAsync(User user)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            UPDATE Users SET 
                FullName = @FullName,
                Email = @Email,
                CurrentLevel = @CurrentLevel,
                WordsLearnedCount = @WordsLearnedCount,
                ListeningCount = @ListeningCount,
                WritingCount = @WritingCount,
                ReadingCount = @ReadingCount,
                SentenceCount = @SentenceCount,
                StreakDays = @StreakDays
            WHERE Id = @Id";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", user.Id);
            cmd.Parameters.AddWithValue("@FullName", user.FullName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@CurrentLevel", user.CurrentLevel);
            cmd.Parameters.AddWithValue("@WordsLearnedCount", user.WordsLearnedCount);
            cmd.Parameters.AddWithValue("@ListeningCount", user.ListeningCount);
            cmd.Parameters.AddWithValue("@WritingCount", user.WritingCount);
            cmd.Parameters.AddWithValue("@ReadingCount", user.ReadingCount);
            cmd.Parameters.AddWithValue("@SentenceCount", user.SentenceCount);
            cmd.Parameters.AddWithValue("@StreakDays", user.StreakDays);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string oldHash = HashPassword(oldPassword);
            string newHash = HashPassword(newPassword);

            string checkSql = "SELECT COUNT(*) FROM Users WHERE Id = @Id AND PasswordHash = @OldHash";
            await using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Id", userId);
                checkCmd.Parameters.AddWithValue("@OldHash", oldHash);
                int ok = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (ok == 0) return false;
            }

            string updateSql = "UPDATE Users SET PasswordHash = @NewHash WHERE Id = @Id";
            await using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                updateCmd.Parameters.AddWithValue("@Id", userId);
                updateCmd.Parameters.AddWithValue("@NewHash", newHash);
                return await updateCmd.ExecuteNonQueryAsync() > 0;
            }
        }
        catch
        {
            return false;
        }
    }

    private static User ReadUser(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? string.Empty : reader.GetString(reader.GetOrdinal("AvatarUrl")),
            IsPremium = reader.GetBoolean(reader.GetOrdinal("IsPremium")),
            WordsLearnedCount = reader.GetInt32(reader.GetOrdinal("WordsLearnedCount")),
            ListeningCount = reader.GetInt32(reader.GetOrdinal("ListeningCount")),
            WritingCount = reader.GetInt32(reader.GetOrdinal("WritingCount")),
            ReadingCount = reader.GetInt32(reader.GetOrdinal("ReadingCount")),
            SentenceCount = reader.GetInt32(reader.GetOrdinal("SentenceCount")),
            StreakDays = reader.GetInt32(reader.GetOrdinal("StreakDays")),
            CurrentLevel = reader.GetString(reader.GetOrdinal("CurrentLevel")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt"))
        };
    }
}
