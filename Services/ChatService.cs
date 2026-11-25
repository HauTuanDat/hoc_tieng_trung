using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using QuizApp.Models;

namespace QuizApp.Services;

public class ChatService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };
    private static string ApiKey => System.IO.File.Exists("gemini_key.txt") ? System.IO.File.ReadAllText("gemini_key.txt").Trim() : "";

    public async Task<string> SendMessageAsync(List<ChatMessage> history, string userMessage)
    {
        var contents = new List<object>
        {
            new
            {
                role = "user",
                parts = new[] { new { text = "Bạn là trợ lý AI gia sư tiếng Trung của ứng dụng Mộc Ngữ. Hãy trả lời ngắn gọn, dễ hiểu bằng tiếng Việt. Khi giải thích từ vựng hoặc mẫu câu tiếng Trung, luôn ghi kèm chữ Hán, Pinyin có dấu thanh và nghĩa tiếng Việt." } }
            },
            new
            {
                role = "model",
                parts = new[] { new { text = "Chào bạn! Mình là Trợ lý AI Mộc Ngữ. Mình sẵn sàng giúp bạn dịch câu, giải thích ngữ pháp HSK và luyện giao tiếp tiếng Trung." } }
            }
        };

        foreach (var msg in history.TakeLast(6))
        {
            contents.Add(new
            {
                role = msg.IsUser ? "user" : "model",
                parts = new[] { new { text = msg.Content } }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        var payload = new
        {
            contents,
            generationConfig = new
            {
                maxOutputTokens = 800
            }
        };

        // Lấy danh sách model thực tế đang mở trên API Key của bạn
        var candidateModels = await GetAvailableModelsAsync();
        string lastError = "Máy chủ AI đang bận, bạn thử gửi lại sau vài giây nhé!";

        foreach (var model in candidateModels)
        {
            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/{model}:generateContent?key={ApiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                string jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"Máy chủ AI đang bận (Mã {(int)response.StatusCode}). Đang thử model khác...";
                    continue;
                }

                using var doc = JsonDocument.Parse(jsonString);
                string? reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    return reply.Trim();
                }
            }
            catch (Exception ex)
            {
                lastError = $"Lỗi kết nối: {ex.Message}";
            }
        }

        return lastError;
    }

    private async Task<List<string>> GetAvailableModelsAsync()
    {
        var result = new List<string>();
        try
        {
            var response = await _httpClient.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={ApiKey}");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.GetProperty("models").EnumerateArray())
                {
                    string name = item.GetProperty("name").GetString() ?? "";
                    bool canGenerate = item.TryGetProperty("supportedGenerationMethods", out var methods) &&
                                       methods.EnumerateArray().Any(m => m.GetString() == "generateContent");

                    if (canGenerate && name.Contains("gemini", StringComparison.OrdinalIgnoreCase) && !name.Contains("vision") && !name.Contains("embedding"))
                    {
                        if (name.Contains("flash", StringComparison.OrdinalIgnoreCase))
                            result.Insert(0, name); // Ưu tiên dòng flash nhanh lên đầu
                        else
                            result.Add(name);
                    }
                }
            }
        }
        catch { }

        if (result.Count == 0)
        {
            result.Add("models/gemini-3.8-flash");
        }
        return result;
    }
}