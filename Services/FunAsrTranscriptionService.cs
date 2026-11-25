using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using NAudio.Wave;
using SherpaOnnx;

namespace QuizApp.Services;

public sealed record FunAsrCandidate(string Text, string Model);

public sealed record FunAsrTranscription(string Text, string? RecordingIssue = null, string OriginalText = "",
    string ZipformerText = "", string ZipformerOriginalText = "")
{
    public IReadOnlyList<FunAsrCandidate> Candidates => new FunAsrCandidate[]
    {
        new(Text, "paraformer"), new(OriginalText, "paraformer"),
        new(ZipformerText, "zipformer"), new(ZipformerOriginalText, "zipformer")
    }.Where(candidate => NormalizeText(candidate.Text).Length > 0).ToArray();

    public string BestAvailableText => Candidates.GroupBy(candidate => NormalizeText(candidate.Text))
        .OrderByDescending(group => group.Select(candidate => candidate.Model).Distinct().Count())
        .Select(group => group.First().Text).FirstOrDefault() ?? string.Empty;

    internal static string NormalizeText(string text) =>
        string.Concat(text.Normalize(NormalizationForm.FormKC).EnumerateRunes().Where(Rune.IsLetterOrDigit));
}

public sealed class FunAsrTranscriptionService
{
    private sealed record Model(string Name, string Folder, string DownloadUrl, string ModelHash, string TokensHash, string Size);

    private static readonly Model Paraformer = new("FunASR", "paraformer-zh-2023-09-14",
        "https://huggingface.co/csukuangfj/sherpa-onnx-paraformer-zh-2023-09-14/resolve/def027084691107096b5ebba69785756d63de6c5/",
        "F36A0433BCF096BD6D6F11B80A3AC8BED110BDCA632FE0D731DF8D1A84475945", "59ABA8873A2ED1E122C25FEE421E25F283B63290EFBDE85C1F01A853D83CB6E6", "243 MB");
    private static readonly Model Zipformer = new("Zipformer tiếng Trung", "zipformer-ctc-small-zh-int8-2025-07-16",
        "https://huggingface.co/csukuangfj/sherpa-onnx-zipformer-ctc-small-zh-int8-2025-07-16/resolve/220fccc064f49a904d0db9500e4d7e50a9da2490/",
        "32E5F17CC9A77D480F8D94BDA97B7CC7A40965B6651B35385813F561A74129C8", "6FED8C6C248516F38E7FAA19404B57413E8CE259F1CBC1FA4AEBC86EAC32FDFD", "63 MB");

    private static readonly HttpClient Http = new();
    private static readonly string ModelDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuizApp", "FunASR");

    public static FunAsrTranscriptionService Instance { get; } = new();

    private readonly SemaphoreSlim _recognizerGate = new(1);
    private OfflineRecognizer? _recognizer;
    private OfflineRecognizer? _zipformer;

    private FunAsrTranscriptionService() { }

    public async Task<FunAsrTranscription> TranscribeAsync(string wavPath, IProgress<string> status,
        CancellationToken cancellationToken, string? originalWavPath = null, bool checkShortWord = false)
    {
        await _recognizerGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(async () =>
            {
                var (samples, sampleRate) = ReadRecording(wavPath);
                if (samples is null)
                {
                    return new FunAsrTranscription(string.Empty, "too_long");
                }
                // Skip empty recordings and digital silence without starting the model.
                if (samples.Length < 0.15 * sampleRate || !samples.Any(sample => Math.Abs(sample) >= 0.001f))
                {
                    return new FunAsrTranscription(string.Empty, "no_speech");
                }

                _recognizer ??= await LoadRecognizerAsync(Paraformer, status, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                status.Report("FunASR đang nhận dạng giọng nói…");

                string text = Decode(_recognizer, samples, sampleRate);
                cancellationToken.ThrowIfCancellationRequested();
                if (originalWavPath is null)
                {
                    return new FunAsrTranscription(text);
                }

                var (original, originalRate) = ReadRecording(originalWavPath);
                string originalText = original is null ? string.Empty : Decode(_recognizer, original, originalRate);
                cancellationToken.ThrowIfCancellationRequested();
                var result = new FunAsrTranscription(text, OriginalText: originalText);
                bool shortWord = checkShortWord && samples.Length <= 5 * sampleRate;
                if (!shortWord && FunAsrTranscription.NormalizeText(text) == FunAsrTranscription.NormalizeText(originalText))
                    return result;

                try
                {
                    _zipformer ??= await LoadRecognizerAsync(Zipformer, status, cancellationToken).ConfigureAwait(false);
                    status.Report("Đang kiểm tra lại âm đọc bằng Zipformer…");
                    string zipformerText = Decode(_zipformer, samples, sampleRate);
                    cancellationToken.ThrowIfCancellationRequested();
                    string zipformerOriginalText = original is null ? string.Empty : Decode(_zipformer, original, originalRate);
                    cancellationToken.ThrowIfCancellationRequested();
                    return result with { ZipformerText = zipformerText, ZipformerOriginalText = zipformerOriginalText };
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
                    ex is HttpRequestException or TimeoutException or IOException or UnauthorizedAccessException or InvalidOperationException)
                {
                    Debug.WriteLine(ex);
                    return result;
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _recognizerGate.Release();
        }
    }

    private static string Decode(OfflineRecognizer recognizer, float[] samples, int sampleRate)
    {
        using var stream = recognizer.CreateStream();
        if (stream.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Không khởi tạo được bộ nhận dạng giọng nói.");
        }
        stream.AcceptWaveform(sampleRate, samples);
        recognizer.Decode(stream);
        return stream.Result.Text.Trim();
    }

    private static (float[]? Samples, int SampleRate) ReadRecording(string wavPath)
    {
        using var file = new FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var wav = new WaveFileReader(file);
        if (wav.WaveFormat.Channels != 1 || wav.WaveFormat.Encoding != WaveFormatEncoding.Pcm || wav.WaveFormat.BitsPerSample != 16)
        {
            throw new InvalidDataException("Bản thu phải là WAV PCM 16 bit, một kênh.");
        }
        if (wav.TotalTime > TimeSpan.FromSeconds(30))
        {
            return (null, wav.WaveFormat.SampleRate);
        }

        var samples = new float[checked((int)(wav.Length / wav.WaveFormat.BlockAlign))];
        int count = wav.ToSampleProvider().Read(samples);
        Array.Resize(ref samples, count);
        return (samples, wav.WaveFormat.SampleRate);
    }

    private static async Task<OfflineRecognizer> LoadRecognizerAsync(Model definition, IProgress<string> status, CancellationToken cancellationToken)
    {
        string directory = Path.Combine(ModelDirectory, definition.Folder);
        Directory.CreateDirectory(directory);
        string model = await GetModelFileAsync(definition, directory, "model.int8.onnx", definition.ModelHash, status, cancellationToken).ConfigureAwait(false);
        string tokens = await GetModelFileAsync(definition, directory, "tokens.txt", definition.TokensHash, status, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        status.Report($"Đang nạp mô hình {definition.Name}…");
        var config = new OfflineRecognizerConfig();
        if (definition == Zipformer) config.ModelConfig.ZipformerCtc.Model = model;
        else config.ModelConfig.Paraformer.Model = model;
        config.ModelConfig.Tokens = tokens;
        config.ModelConfig.NumThreads = Math.Min(4, Environment.ProcessorCount);
        return new OfflineRecognizer(config);
    }

    private static async Task<string> GetModelFileAsync(Model definition, string directory, string fileName,
        string expectedHash, IProgress<string> status, CancellationToken cancellationToken)
    {
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path) && await HasExpectedHashAsync(path, expectedHash, cancellationToken).ConfigureAwait(false))
        {
            return path;
        }

        status.Report($"Đang tải mô hình {definition.Name} lần đầu (khoảng {definition.Size})…");
        string temporaryPath = path + $".{Guid.NewGuid():N}.download";
        using var downloadTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        downloadTimeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            using (var response = await Http.GetAsync(definition.DownloadUrl + fileName, HttpCompletionOption.ResponseHeadersRead, downloadTimeout.Token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                await using var output = File.Create(temporaryPath);
                await response.Content.CopyToAsync(output, downloadTimeout.Token).ConfigureAwait(false);
            }

            if (!await HasExpectedHashAsync(temporaryPath, expectedHash, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException($"Tệp mô hình {definition.Name} tải về không toàn vẹn.");
            }

            File.Move(temporaryPath, path, overwrite: true);
            return path;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Hết thời gian tải mô hình {definition.Name}.", ex);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static async Task<bool> HasExpectedHashAsync(string path, string expectedHash, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 64 * 1024, useAsync: true);
        byte[] hash = await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash) == expectedHash;
    }
}
