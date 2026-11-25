using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace QuizApp.Services;

public sealed record PronunciationSyllable(
    string? Hanzi, string? Pinyin, string? Ipa, int? Tone, string Outcome,
    string? ExpectedInitial, string? ExpectedFinal, string? ActualInitial, string? ActualFinal,
    string? ToneFeedback, double? PitchChange)
{
    public string Target => Hanzi is null ? "Âm tiết đọc thêm" : $"{Hanzi}  {Pinyin}";

    public string Feedback => Outcome switch
    {
        "match" => "Âm tiết và thanh điệu sát mẫu",
        "tone_mismatch" => "Âm tiết khớp, thanh điệu cần luyện thêm",
        "tone_uncertain" => "Âm tiết khớp, chưa đo rõ thanh điệu",
        "syllable_mismatch" => "Âm tiết khác mẫu",
        "model_disagreement" => "Nhận dạng chữ và phân tích âm đọc cho kết quả khác nhau.",
        "missing" => "Chưa nhận ra âm tiết",
        "extra" => "Nhận dạng thêm âm tiết",
        _ => "Chưa xác định rõ"
    };

    public string Detected => Ipa is null || Outcome == "model_disagreement" ? string.Empty : (Tone switch
    {
        5 => $"{Ipa} · thanh nhẹ",
        >= 1 and <= 4 => $"{Ipa} · thanh {Tone}",
        _ => Ipa
    });

    public string SegmentFeedback => string.Join(" · ", new[]
    {
        DescribeSegment("Thanh mẫu", ExpectedInitial, ActualInitial, DisplayInitial),
        DescribeSegment("Vận mẫu", ExpectedFinal, ActualFinal, DisplayFinal)
    }.OfType<string>());

    public string ToneDetail => (ToneFeedback, PitchChange) switch
    {
        ("close", double change) => $"Cao độ đầu–cuối: {change:+0.0;-0.0;0.0} bán âm",
        ("partial" or "different", double change) => $"Đường cao độ chưa sát mẫu ({change:+0.0;-0.0;0.0} bán âm đầu–cuối)",
        ("model_only", _) => "Chưa đo rõ F0. Thanh hiển thị do mô hình nhận dạng.",
        _ => string.Empty
    };

    private static string? DescribeSegment(string label, string? expected, string? actual, Func<string, string> display)
    {
        if (expected is null) return null;
        if (actual is null) return $"{label} mẫu {display(expected)}";
        if (expected == actual) return $"{label} {display(expected)}";
        return $"{label} nghe gần {display(actual)} → mẫu {display(expected)}";
    }

    private static string DisplayInitial(string initial) => initial.Length == 0 ? "không có" : initial;

    private static string DisplayFinal(string final) => final switch
    {
        "apical" => "i (đầu lưỡi trước)",
        "retroflex" => "i (uốn lưỡi)",
        _ => final
    };
}

public sealed record PronunciationCriterion(int? Score, int Maximum, int Evaluated, int Targets, int? PitchEvaluated);

public sealed record PronunciationScores(
    PronunciationCriterion Initials, PronunciationCriterion Finals, PronunciationCriterion Tones,
    PronunciationCriterion Syllables, PronunciationCriterion Naturalness);

public sealed record PronunciationAssessment(
    string Status, IReadOnlyList<PronunciationSyllable>? Syllables = null,
    PronunciationScores? Scores = null, int? TotalScore = null, string? PaceFeedback = null,
    string? Stage = null, double TrimStartSeconds = 0, bool RumbleDominant = false,
    string? PreferredTranscript = null, string? HeardPinyin = null,
    string? AlternateTranscript = null, bool TranscriptUncertain = false, bool PlaybackReady = false,
    bool UsedLessonSpelling = false);

public sealed class LocalPronunciationService
{
    public static LocalPronunciationService Instance { get; } = new();

    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuizApp", "Pronunciation");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly object _workerLock = new();
    private readonly SemaphoreSlim _gate = new(1);
    private Process? _worker;
    private bool _isClosing;

    private LocalPronunciationService() { }

    public Task<PronunciationAssessment> PrepareAudioAsync(string wavPath, string outputPath, CancellationToken cancellationToken,
        string? playbackPath = null) => RunAsync(new { wavPath, outputPath, playbackPath }, null, cancellationToken);

    public Task<PronunciationAssessment> AssessAsync(string wavPath, string hanzi, string pinyin,
        IProgress<string> progress, CancellationToken cancellationToken, IReadOnlyList<FunAsrCandidate>? transcripts = null,
        string? originalWavPath = null, double trimStartSeconds = 0, bool rumbleDominant = false) => RunAsync(new
        {
            wavPath, hanzi, pinyin, transcripts = transcripts ?? [], originalWavPath, trimStartSeconds, rumbleDominant,
            prepared = originalWavPath is not null
        }, progress, cancellationToken);

    private async Task<PronunciationAssessment> RunAsync(object payload, IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string python = Path.Combine(Root, "venv", "Scripts", "python.exe");
            if (!File.Exists(python) || !File.Exists(Path.Combine(Root, "model", "base.pt")))
            {
                return new("not_installed");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromMinutes(2));
            var worker = GetWorker(python);
            string request = JsonSerializer.Serialize(payload, JsonOptions);
            await worker.StandardInput.WriteLineAsync(request.AsMemory(), deadline.Token).ConfigureAwait(false);
            while (await worker.StandardOutput.ReadLineAsync(deadline.Token).ConfigureAwait(false) is string response)
            {
                var result = JsonSerializer.Deserialize<PronunciationAssessment>(response, JsonOptions) ?? new("error");
                if (result.Stage is not string stage) return result;
                progress?.Report(stage);
            }
            throw new IOException("The local pronunciation process exited before responding.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            StopWorker();
            return new("timeout");
        }
        catch
        {
            StopWorker();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private Process GetWorker(string python)
    {
        lock (_workerLock)
        {
            if (_isClosing) throw new OperationCanceledException();
            if (_worker is { HasExited: false }) return _worker;
            _worker?.Dispose();
            _worker = null;

            var start = new ProcessStartInfo(python)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            start.ArgumentList.Add("-u");
            start.ArgumentList.Add("-B");
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Pronunciation", "assess.py"));
            start.ArgumentList.Add(Root);
            start.Environment["PYTHONUTF8"] = "1";

            var worker = new Process { StartInfo = start };
            worker.ErrorDataReceived += (_, args) => Debug.WriteLine(args.Data);
            try
            {
                worker.Start();
                worker.BeginErrorReadLine();
                _worker = worker;
                return worker;
            }
            catch
            {
                worker.Dispose();
                throw;
            }
        }
    }

    public void StopWorker(bool shuttingDown = false)
    {
        lock (_workerLock)
        {
            _isClosing |= shuttingDown;
            if (_worker is not Process worker) return;
            _worker = null;
            try
            {
                if (!worker.HasExited) worker.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                worker.Dispose();
            }
        }
    }
}
