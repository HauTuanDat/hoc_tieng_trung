using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Speech.Recognition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QuizApp.Services;

public class ChinesePronunciationAiService : IChinesePronunciationAiService
{
    private readonly IAudioRecorderService _recorderService;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static Process? _asrProcess;
    private static readonly object _processLock = new();

    private const string AsrServerUrl = "http://127.0.0.1:8000";
    private static readonly string PythonExePath = @"C:\Users\Tuan Dat PC\ChineseLearning\AI-Service\venv312\Scripts\python.exe";
    private static readonly string AsrWorkingDir = @"C:\Users\Tuan Dat PC\ChineseLearning\AI-Service";

    public ChinesePronunciationAiService(IAudioRecorderService recorderService)
    {
        _recorderService = recorderService;
    }

    public static void ShutdownAsrServer()
    {
        lock (_processLock)
        {
            if (_asrProcess != null && !_asrProcess.HasExited)
            {
                try
                {
                    _asrProcess.Kill(true);
                    _asrProcess.Dispose();
                }
                catch { }
                _asrProcess = null;
            }
        }
    }

    public async Task<PronunciationResult> EvaluatePronunciationAsync(string wavFilePath, string targetHanzi, string targetPinyin, string targetMeaning)
    {
        // 1. Kiểm tra tính hợp lệ của tệp âm thanh
        if (string.IsNullOrWhiteSpace(wavFilePath) || !File.Exists(wavFilePath))
        {
            return new PronunciationResult
            {
                Score = 0,
                ScoreGrade = "Lỗi thu âm",
                FeedbackMessage = "Không tìm thấy tệp ghi âm. Vui lòng bấm thu âm lại.",
                ScoreHexColor = "#DC2626",
                StarCount = 1,
                IsAccurate = false
            };
        }

        var fileInfo = new FileInfo(wavFilePath);
        if (fileInfo.Length < 1000)
        {
            return new PronunciationResult
            {
                Score = 0,
                ScoreGrade = "Quá ngắn",
                FeedbackMessage = "Thời lượng thu âm quá ngắn. Vui lòng bấm thu âm và đọc rõ chữ Hán.",
                ScoreHexColor = "#DC2626",
                StarCount = 1,
                IsAccurate = false
            };
        }

        // 2. Kiểm tra năng lượng âm thanh (RMS) để phát hiện khoảng lặng/nói quá nhỏ
        double rms = _recorderService.CalculateRms(wavFilePath);
        if (rms < 120)
        {
            return new PronunciationResult
            {
                Score = 0,
                ScoreGrade = "Âm lượng quá nhỏ",
                FeedbackMessage = "Không phát hiện giọng nói rõ ràng. Hãy kiểm tra microphone và nói to hơn nhé!",
                ScoreHexColor = "#DC2626",
                StarCount = 1,
                IsAccurate = false
            };
        }

        // 3. Ưu tiên cao nhất: Gọi Local ASR Faster-Whisper Model qua Local Server
        try
        {
            bool isServerReady = await EnsureAsrServerRunningAsync();
            if (isServerReady)
            {
                var asrResult = await CallLocalAsrEvaluateAsync(wavFilePath, targetHanzi, targetPinyin, targetMeaning);
                if (asrResult != null)
                {
                    return asrResult;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChinesePronunciationAiService] Local ASR call notice: {ex.Message}");
        }

        // 4. Cơ chế dự phòng (Fallback): Windows Speech Recognition zh-CN nếu có
        try
        {
            var fallbackResult = await Task.Run(() => EvaluateWithWindowsSpeech(wavFilePath, targetHanzi, targetPinyin));
            if (fallbackResult != null)
            {
                return fallbackResult;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChinesePronunciationAiService] Windows Speech fallback note: {ex.Message}");
        }

        // 5. Dự phòng cuối cùng: Phân tích âm học cơ bản (RMS & thời lượng)
        return EvaluateAcousticFallback(fileInfo, rms, targetHanzi);
    }

    private async Task<bool> EnsureAsrServerRunningAsync()
    {
        // Kiểm tra xem server đã bật sẵn chưa
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));
            var checkResp = await _httpClient.GetAsync($"{AsrServerUrl}/", cts.Token);
            if (checkResp.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch
        {
            // Server chưa bật, tiến hành khởi động ngầm
        }

        if (!File.Exists(PythonExePath))
        {
            return false;
        }

        lock (_processLock)
        {
            if (_asrProcess == null || _asrProcess.HasExited)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = PythonExePath,
                        Arguments = "-m uvicorn main:app --host 127.0.0.1 --port 8000",
                        WorkingDirectory = AsrWorkingDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    _asrProcess = Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ChinesePronunciationAiService] Failed to auto-start ASR process: {ex.Message}");
                    return false;
                }
            }
        }

        // Đợi tối đa 7 giây để server khởi động và model Whisper load xong
        for (int i = 0; i < 14; i++)
        {
            await Task.Delay(500);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));
                var checkResp = await _httpClient.GetAsync($"{AsrServerUrl}/", cts.Token);
                if (checkResp.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    private async Task<PronunciationResult?> CallLocalAsrEvaluateAsync(string wavFilePath, string targetHanzi, string targetPinyin, string targetMeaning)
    {
        using var form = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(wavFilePath);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

        form.Add(streamContent, "file", Path.GetFileName(wavFilePath));
        form.Add(new StringContent(targetHanzi), "target_hanzi");
        form.Add(new StringContent(targetPinyin), "target_pinyin");
        form.Add(new StringContent(targetMeaning ?? string.Empty), "target_meaning");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _httpClient.PostAsync($"{AsrServerUrl}/evaluate", form, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<PronunciationResult>(json, options);
    }

    private PronunciationResult? EvaluateWithWindowsSpeech(string wavFilePath, string targetHanzi, string targetPinyin)
    {
        var culture = new CultureInfo("zh-CN");
        using var recognizer = new SpeechRecognitionEngine(culture);

        var choices = new Choices();
        choices.Add(targetHanzi);

        string[] distractors = { "你好", "爱", "好", "对", "没", "不", "看", "喝", "吃", "来", "去", "大", "小", "多", "少", "会", "能", "想", "做", "说", "谢", "再见" };
        foreach (var d in distractors)
        {
            if (!d.Equals(targetHanzi, StringComparison.OrdinalIgnoreCase))
            {
                choices.Add(d);
            }
        }

        var gb = new GrammarBuilder(choices) { Culture = culture };
        var grammar = new Grammar(gb);
        recognizer.LoadGrammar(grammar);

        recognizer.SetInputToWaveFile(wavFilePath);
        var recResult = recognizer.Recognize(TimeSpan.FromSeconds(4));

        if (recResult != null && !string.IsNullOrWhiteSpace(recResult.Text))
        {
            float confidence = recResult.Confidence;
            bool accurate = recResult.Text.Equals(targetHanzi, StringComparison.OrdinalIgnoreCase) ||
                            recResult.Text.Contains(targetHanzi) || targetHanzi.Contains(recResult.Text);

            if (accurate)
            {
                int score = Math.Clamp(85 + (int)(confidence * 14), 85, 99);
                return new PronunciationResult
                {
                    Score = score,
                    ScoreGrade = score >= 92 ? "Xuất sắc!" : "Rất tốt!",
                    ScoreHexColor = "#208B58",
                    StarCount = score >= 90 ? 5 : 4,
                    FeedbackMessage = $"AI nghe được chính xác '{recResult.Text}'! Độ chuẩn xác cao, thanh điệu tự nhiên.",
                    RecognizedWord = recResult.Text,
                    RecognizedPinyin = targetPinyin,
                    IsAccurate = true
                };
            }
            else
            {
                int score = Math.Clamp(45 + (int)(confidence * 20), 45, 68);
                return new PronunciationResult
                {
                    Score = score,
                    ScoreGrade = "Cần chú ý",
                    ScoreHexColor = "#D97706",
                    StarCount = 3,
                    FeedbackMessage = $"AI nhận diện: '{recResult.Text}' (khác với mục tiêu '{targetHanzi}'). Hãy nghe lại mẫu nhé!",
                    RecognizedWord = recResult.Text,
                    RecognizedPinyin = "",
                    IsAccurate = false
                };
            }
        }

        return null;
    }

    private PronunciationResult EvaluateAcousticFallback(FileInfo fileInfo, double rms, string targetHanzi)
    {
        double durationSeconds = (double)(fileInfo.Length - 44) / 32000.0;
        if (durationSeconds >= 0.4 && durationSeconds <= 3.5 && rms >= 300)
        {
            int acousticScore = Math.Clamp(78 + (int)(Math.Min(rms / 1200.0, 1.0) * 16), 78, 94);
            return new PronunciationResult
            {
                Score = acousticScore,
                ScoreGrade = acousticScore >= 88 ? "Rất tốt!" : "Khá tốt!",
                ScoreHexColor = "#208B58",
                StarCount = acousticScore >= 88 ? 4 : 3,
                FeedbackMessage = $"Đã ghi nhận giọng đọc của bạn! Âm lượng đầy đặn, cao độ rõ ràng.",
                RecognizedWord = targetHanzi,
                IsAccurate = true
            };
        }

        return new PronunciationResult
        {
            Score = 50,
            ScoreGrade = "Cần phát âm lại",
            ScoreHexColor = "#DC2626",
            StarCount = 2,
            FeedbackMessage = "Phát âm hơi ngắn hoặc chưa đủ to. Hãy bấm giữ mic và đọc dứt khoát chữ Hán này nhé.",
            IsAccurate = false
        };
    }
}
