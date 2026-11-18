-- =============================================
-- QuizApp Database Setup Script for SQL Server
-- =============================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QuizDb')
BEGIN
    CREATE DATABASE QuizDb COLLATE Vietnamese_CI_AS;
END
GO

USE QuizDb;
GO

-- Create Questions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Questions')
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
        CorrectAnswer NVARCHAR(10) NOT NULL, -- 'A', 'B', 'C', 'D'
        ExplanationAfterAnswer NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- Seed sample vocabulary questions if table is empty
IF NOT EXISTS (SELECT 1 FROM Questions)
BEGIN
    INSERT INTO Questions (Category, Tag, Prompt, Explanation, PartOfSpeech, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, ExplanationAfterAnswer)
    VALUES
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Tò mò, ham học hỏi',
        N'Muốn tìm hiểu, khám phá những điều mới lạ xung quanh.',
        N'Tính từ • Mô tả tính cách',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Curious', N'Bored', N'Careless', N'Anxious',
        N'A',
        N'"Curious" nghĩa là tò mò, ham hiểu biết.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Hào phóng, rộng lượng',
        N'Sẵn lòng cho đi, chia sẻ tiền bạc, thời gian hoặc sự giúp đỡ cho người khác.',
        N'Tính từ • Mô tả tính cách',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Selfish', N'Generous', N'Greedy', N'Strict',
        N'B',
        N'"Generous" nghĩa là hào phóng, rộng lượng.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Tự tin, quyết đoán',
        N'Có niềm tin mạnh mẽ vào khả năng và giá trị của bản thân.',
        N'Tính từ • Mô tả tính cách',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Shy', N'Hesitant', N'Confident', N'Fearful',
        N'C',
        N'"Confident" nghĩa là tự tin vào bản thân.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Chăm chỉ, siêng năng',
        N'Luôn nỗ lực và cẩn thận trong công việc hoặc học tập.',
        N'Tính từ • Mô tả tính cách',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Curious', N'Diligent', N'Generous', N'Confident',
        N'B',
        N'"Diligent" nghĩa là chăm chỉ, siêng năng và cần mẫn.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Kiên nhẫn, nhẫn nại',
        N'Khả năng chờ đợi hoặc đối mặt với khó khăn mà không tức giận hay nản lòng.',
        N'Tính từ • Phẩm chất cá nhân',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Patient', N'Aggressive', N'Stubborn', N'Impatient',
        N'A',
        N'"Patient" mang nghĩa kiên nhẫn, nhẫn nại.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Đáng tin cậy, trung thực',
        N'Luôn nói sự thật, không lừa dối và có thể tin cậy được.',
        N'Tính từ • Đạo đức & Tính cách',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Deceitful', N'Sly', N'Honest', N'Boastful',
        N'C',
        N'"Honest" nghĩa là trung thực, chân thật.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Lạc quan, yêu đời',
        N'Luôn nhìn vào mặt tích cực và hy vọng những điều tốt đẹp trong tương lai.',
        N'Tính từ • Thái độ sống',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Pessimistic', N'Depressed', N'Cynical', N'Optimistic',
        N'D',
        N'"Optimistic" nghĩa là lạc quan, tích cực.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Sáng tạo, đổi mới',
        N'Có nhiều ý tưởng độc đáo và khả năng tạo ra những điều mới mẻ.',
        N'Tính từ • Năng lực tư duy',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Creative', N'Ordinary', N'Dull', N'Repetitive',
        N'A',
        N'"Creative" nghĩa là có óc sáng tạo.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Lịch sự, nhã nhặn',
        N'Cư xử đúng mực, tôn trọng người khác trong giao tiếp và hành vi.',
        N'Tính từ • Ứng xử xã hội',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Rude', N'Impolite', N'Polite', N'Harsh',
        N'C',
        N'"Polite" nghĩa là lịch sự, tao nhã.'
    ),
    (
        N'CƠ BẢN',
        N'✨ NGHĨA CỦA TỪ',
        N'Dũng cảm, can đảm',
        N'Sẵn sàng đối mặt với nguy hiểm, nỗi sợ hãi hoặc đau đớn mà không lùi bước.',
        N'Tính từ • Phẩm chất cá nhân',
        N'Từ tiếng Anh nào phù hợp với nghĩa trên?',
        N'Cowardly', N'Brave', N'Timid', N'Anxious',
        N'B',
        N'"Brave" nghĩa là dũng cảm, can trường.'
    );
END
GO

-- Create QuizHistory table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'QuizHistory')
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
GO

-- Create ChineseVocabularies table for HSK 1~6
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'ChineseVocabularies')
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
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ChineseVocabularies') AND name = N'LastPracticed')
    BEGIN
        ALTER TABLE ChineseVocabularies ADD LastPracticed DATETIME NULL;
    END
END
GO

-- Create TypingProgress table for tracking user's last typing position
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'TypingProgress')
BEGIN
    CREATE TABLE TypingProgress (
        Level NVARCHAR(20) PRIMARY KEY,
        LastIndex INT NOT NULL DEFAULT 0,
        LastVocabId INT NOT NULL DEFAULT 0,
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- Create TypingHistory table for tracking user's typed practice words and sentences
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'TypingHistory')
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
GO

-- Create Users table for authentication and profile
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Users')
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
GO

-- Seed default user (Minh Đạt / minhdat@gmail.com / mật khẩu: 123456)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'minhdat@gmail.com' OR Username = N'minhdat')
BEGIN
    -- SHA256 of '123456' is '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'
    INSERT INTO Users (Username, Email, FullName, PasswordHash, AvatarUrl, IsPremium, WordsLearnedCount, ListeningCount, WritingCount, ReadingCount, SentenceCount, StreakDays, CurrentLevel)
    VALUES (N'minhdat', N'minhdat@gmail.com', N'Minh Đạt', N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Assets/avatar_minhdat.png', 1, 0, 0, 0, 0, 0, 0, N'HSK 1');
END
ELSE
BEGIN
    UPDATE Users 
    SET FullName = N'Minh Đạt' 
    WHERE (Username = N'minhdat' OR Email = N'minhdat@gmail.com') 
      AND (FullName LIKE N'%Ä%' OR FullName LIKE N'%?%' OR FullName != N'Minh Đạt');
END
GO


