namespace QuizApp.Models;

public class DictionaryMeaning
{
    public string Vietnamese { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
    public string PartOfSpeech { get; set; } = string.Empty; // danh từ, động từ, tính từ...
}

public class DictionaryExample
{
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Source { get; set; } = "Thực tế"; // "HSK", "Chuyên ngành", "Ngữ cảnh"
}

public class DictionaryWordEntry
{
    public int Id { get; set; }
    public string Simplified { get; set; } = string.Empty;   // Chữ Hán giản thể
    public string Traditional { get; set; } = string.Empty; // Chữ Hán phồn thể
    public string Pinyin { get; set; } = string.Empty;      // Phiên âm có thanh điệu
    public string PinyinPlain { get; set; } = string.Empty; // Pinyin không dấu
    public string HanViet { get; set; } = string.Empty;     // Âm Hán Việt
    public string HskLevel { get; set; } = string.Empty;    // HSK 1, HSK 2...
    public string Radical { get; set; } = string.Empty;     // Bộ thủ
    public int StrokeCount { get; set; }                    // Số nét
    public string Mnemonic { get; set; } = string.Empty;    // Mẹo nhớ chữ Hán chiết tự
    public List<DictionaryMeaning> Meanings { get; set; } = new();
    public List<DictionaryExample> Examples { get; set; } = new();
    public List<string> RelatedWords { get; set; } = new();
    public bool IsFromLocalDb { get; set; }

    public string PrimaryMeaning => Meanings.FirstOrDefault()?.Vietnamese ?? string.Empty;
    public string DisplayTitle => $"{Simplified} ({Pinyin})";
    public string HanVietDisplay => string.IsNullOrWhiteSpace(HanViet) ? string.Empty : $"Hán Việt: {HanViet.ToUpperInvariant()}";
    public string HskBadgeText => string.IsNullOrWhiteSpace(HskLevel) ? "Phổ dụng" : HskLevel;
}
