using System.Data;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using QuizApp.Models;

namespace QuizApp.Services;

public class DictionaryApiService : IDictionaryApiService
{
    private readonly IDatabaseService _databaseService;
    private readonly HttpClient _httpClient;

    public DictionaryApiService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
    }

    public async Task<List<DictionaryWordEntry>> LookupAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<DictionaryWordEntry>();

        string cleanQuery = query.Trim();
        var results = new Dictionary<string, DictionaryWordEntry>(StringComparer.OrdinalIgnoreCase);

        // 1. First, search local database for instant 0ms cached/native results
        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync(ct);

            const string localSql = @"
                SELECT TOP 10 Id, Level, Word, Pinyin, Meaning, ExampleHanzi, ExamplePinyin, ExampleMeaning 
                FROM ChineseVocabularies 
                WHERE Word = @q OR Pinyin = @q OR Meaning LIKE @likeQ
                ORDER BY CASE WHEN Word = @q THEN 0 WHEN Pinyin = @q THEN 1 ELSE 2 END";

            await using var cmd = new SqlCommand(localSql, conn);
            cmd.Parameters.AddWithValue("@q", cleanQuery);
            cmd.Parameters.AddWithValue("@likeQ", $"%{cleanQuery}%");

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                string word = reader.GetString(2);
                if (results.ContainsKey(word)) continue;

                var entry = new DictionaryWordEntry
                {
                    Id = reader.GetInt32(0),
                    Simplified = word,
                    Traditional = word,
                    Pinyin = reader.GetString(3),
                    HskLevel = reader.GetString(1),
                    IsFromLocalDb = true
                };

                string meaning = reader.GetString(4);
                entry.Meanings.Add(new DictionaryMeaning
                {
                    Vietnamese = meaning,
                    PartOfSpeech = reader.GetString(1)
                });

                if (!reader.IsDBNull(5))
                {
                    entry.Examples.Add(new DictionaryExample
                    {
                        Hanzi = reader.GetString(5),
                        Pinyin = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        Meaning = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        Source = reader.GetString(1)
                    });
                }

                results[word] = entry;
            }
        }
        catch { }

        // 2. Query Tom Chinese Online REST API for comprehensive live dictionary data
        try
        {
            string url = $"https://tomchinese.com/api/tra-tu?q={Uri.EscapeDataString(cleanQuery)}";
            using var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("tu", out var tuArray) && tuArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in tuArray.EnumerateArray())
                    {
                        string simplified = elem.TryGetProperty("simplified", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                        if (string.IsNullOrWhiteSpace(simplified)) continue;

                        string traditional = elem.TryGetProperty("traditional", out var tr) ? tr.GetString() ?? simplified : simplified;
                        string pinyin = elem.TryGetProperty("pinyin", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                        string pinyinPlain = elem.TryGetProperty("pinyinPlain", out var pp) ? pp.GetString() ?? string.Empty : string.Empty;
                        string hanViet = elem.TryGetProperty("hanViet", out var hv) ? hv.GetString() ?? string.Empty : string.Empty;
                        string mnemonic = elem.TryGetProperty("mnemonicVi", out var mn) ? mn.GetString() ?? string.Empty : string.Empty;
                        string radical = elem.TryGetProperty("radical", out var rd) ? rd.GetString() ?? string.Empty : string.Empty;
                        int charCount = elem.TryGetProperty("charCount", out var cc) && cc.TryGetInt32(out var count) ? count : simplified.Length;

                        string hskLevel = "Phổ dụng";
                        if (elem.TryGetProperty("hsk3Level", out var hsk3) && hsk3.ValueKind == JsonValueKind.Number)
                        {
                            hskLevel = $"HSK {hsk3.GetInt32()}";
                        }
                        else if (elem.TryGetProperty("hskLevel", out var hsk) && hsk.ValueKind == JsonValueKind.Number)
                        {
                            hskLevel = $"HSK {hsk.GetInt32()}";
                        }

                        if (!results.TryGetValue(simplified, out var entry))
                        {
                            entry = new DictionaryWordEntry
                            {
                                Simplified = simplified,
                                Traditional = traditional,
                                Pinyin = pinyin,
                                PinyinPlain = pinyinPlain,
                                HanViet = hanViet,
                                HskLevel = hskLevel,
                                Radical = radical,
                                StrokeCount = charCount,
                                Mnemonic = mnemonic,
                                IsFromLocalDb = false
                            };
                            results[simplified] = entry;
                        }
                        else
                        {
                            // Merge online rich metadata
                            if (string.IsNullOrWhiteSpace(entry.HanViet)) entry.HanViet = hanViet;
                            if (string.IsNullOrWhiteSpace(entry.Traditional)) entry.Traditional = traditional;
                            if (string.IsNullOrWhiteSpace(entry.Mnemonic)) entry.Mnemonic = mnemonic;
                            if (string.IsNullOrWhiteSpace(entry.Radical)) entry.Radical = radical;
                            if (entry.StrokeCount == 0) entry.StrokeCount = charCount;
                        }

                        // Parse meanings
                        if (elem.TryGetProperty("meanings", out var meaningsArr) && meaningsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var m in meaningsArr.EnumerateArray())
                            {
                                string vi = m.TryGetProperty("vi", out var viProp) ? viProp.GetString() ?? string.Empty : string.Empty;
                                string en = m.TryGetProperty("en", out var enProp) ? enProp.GetString() ?? string.Empty : string.Empty;
                                string pos = m.TryGetProperty("pos", out var posProp) ? posProp.GetString() ?? string.Empty : string.Empty;

                                if (!string.IsNullOrWhiteSpace(vi) && !entry.Meanings.Any(existing => existing.Vietnamese == vi))
                                {
                                    entry.Meanings.Add(new DictionaryMeaning
                                    {
                                        Vietnamese = vi,
                                        English = en,
                                        PartOfSpeech = pos
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // 3. For the primary results, enrich with multiple example sentences from local DB
        var list = results.Values.ToList();
        foreach (var entry in list.Take(5))
        {
            var moreExamples = await GetExamplesAsync(entry.Simplified, ct);
            foreach (var ex in moreExamples)
            {
                if (!entry.Examples.Any(e => e.Hanzi == ex.Hanzi))
                {
                    entry.Examples.Add(ex);
                }
            }
        }

        // Sort: exact matches first, then shorter words
        return list
            .OrderBy(e => e.Simplified.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(e => e.PinyinPlain.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(e => e.Simplified.Length)
            .ToList();
    }

    public async Task<List<DictionaryExample>> GetExamplesAsync(string word, CancellationToken ct = default)
    {
        var examples = new List<DictionaryExample>();
        if (string.IsNullOrWhiteSpace(word)) return examples;

        try
        {
            await using var conn = new SqlConnection(_databaseService.ConnectionString);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT DISTINCT TOP 15 ExampleHanzi, ExamplePinyin, ExampleMeaning, Level 
                FROM ChineseVocabularies 
                WHERE ExampleHanzi IS NOT NULL 
                  AND LEN(ExampleHanzi) > 3 
                  AND ExampleHanzi LIKE @pattern
                ORDER BY Level";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pattern", $"%{word.Trim()}%");

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                examples.Add(new DictionaryExample
                {
                    Hanzi = reader.GetString(0),
                    Pinyin = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Meaning = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Source = reader.GetString(3)
                });
            }
        }
        catch { }

        return examples;
    }
}
