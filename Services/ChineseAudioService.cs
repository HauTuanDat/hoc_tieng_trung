using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace QuizApp.Services;

public class ChineseAudioService : IChineseAudioService
{
    private static readonly Lazy<ChineseAudioService> _instance = new(() => new ChineseAudioService());
    public static ChineseAudioService Instance => _instance.Value;

    private readonly HttpClient _httpClient = new();
    private readonly MediaPlayer _mediaPlayer = new();
    private readonly string _cacheDir;
    private CancellationTokenSource? _currentCts;

    public bool IsMuted { get; set; } = false;
    public string SelectedVoice { get; set; } = "zh-CN-XiaoxiaoNeural";
    public string SelectedSpeed { get; set; } = "+0%"; // "+0%", "-15%", "-30%", "+15%"

    public ChineseAudioService()
    {
        _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuizApp", "AudioCache");
        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }
        catch { }
    }

    public async Task PlayAsync(string text, string? voice = null, string? rate = null)
    {
        if (IsMuted || string.IsNullOrWhiteSpace(text)) return;

        voice ??= SelectedVoice;
        rate ??= SelectedSpeed;

        string cleanText = text.Trim();
        string cacheFile = GetCacheFilePath(cleanText, voice, rate);

        _currentCts?.Cancel();
        _currentCts = new CancellationTokenSource();
        var ct = _currentCts.Token;

        try
        {
            // 1. Check local cache first for instant 0ms playback
            if (File.Exists(cacheFile) && new FileInfo(cacheFile).Length > 200)
            {
                PlayLocalFile(cacheFile);
                return;
            }

            // 2. Fetch using Edge TTS with XiaoxiaoNeural
            byte[]? audioBytes = await FetchEdgeTtsAudioAsync(cleanText, voice, rate, ct);

            // 3. Fallback to Google TTS if Edge TTS is unavailable
            if (audioBytes == null || audioBytes.Length == 0)
            {
                audioBytes = await FetchGoogleTtsAudioAsync(cleanText, ct);
            }

            if (audioBytes != null && audioBytes.Length > 200 && !ct.IsCancellationRequested)
            {
                await File.WriteAllBytesAsync(cacheFile, audioBytes, ct);
                PlayLocalFile(cacheFile);
            }
        }
        catch (OperationCanceledException)
        {
            // User triggered another sound, cancelled cleanly
        }
        catch
        {
            // Non-critical, ignore playback errors
        }
    }

    public void Stop()
    {
        _currentCts?.Cancel();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                _mediaPlayer.Stop();
            }
            catch { }
        });
    }

    private void PlayLocalFile(string filePath)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Open(new Uri(Path.GetFullPath(filePath), UriKind.Absolute));
                _mediaPlayer.Play();
            }
            catch { }
        });
    }

    private async Task<byte[]?> FetchEdgeTtsAudioAsync(string text, string voice, string rate, CancellationToken ct)
    {
        try
        {
            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0");
            ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
            ws.Options.SetRequestHeader("Pragma", "no-cache");
            ws.Options.SetRequestHeader("Cache-Control", "no-cache");

            string connectionId = Guid.NewGuid().ToString("N");
            var uri = new Uri($"wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4&ConnectionId={connectionId}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(6));

            await ws.ConnectAsync(uri, timeoutCts.Token);

            // Step 1: Send configuration message
            string configMsg = "Content-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"true\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            var configBytes = Encoding.UTF8.GetBytes(configMsg);
            await ws.SendAsync(new ArraySegment<byte>(configBytes), WebSocketMessageType.Text, true, timeoutCts.Token);

            // Step 2: Send SSML synthesis message
            string reqId = Guid.NewGuid().ToString("N");
            string safeText = System.Security.SecurityElement.Escape(text);
            string ssml = $"X-RequestId:{reqId}\r\nContent-Type:application/ssml+xml\r\nPath:ssml\r\n\r\n<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='zh-CN'><voice name='{voice}'><prosody pitch='+0Hz' rate='{rate}' volume='+0%'>{safeText}</prosody></voice></speak>";
            var ssmlBytes = Encoding.UTF8.GetBytes(ssml);
            await ws.SendAsync(new ArraySegment<byte>(ssmlBytes), WebSocketMessageType.Text, true, timeoutCts.Token);

            // Step 3: Receive audio chunks
            using var audioMs = new MemoryStream();
            var buffer = new byte[32768];

            while (ws.State == WebSocketState.Open && !timeoutCts.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 2)
                {
                    // Binary payload format: 2-byte header length + header ASCII text + audio bytes
                    int headerLen = (buffer[0] << 8) | buffer[1];
                    int audioOffset = 2 + headerLen;
                    if (result.Count > audioOffset)
                    {
                        audioMs.Write(buffer, audioOffset, result.Count - audioOffset);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string textMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (textMsg.Contains("Path:turn.end"))
                    {
                        break;
                    }
                }
            }

            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch { }

            return audioMs.Length > 0 ? audioMs.ToArray() : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<byte[]?> FetchGoogleTtsAudioAsync(string text, CancellationToken ct)
    {
        try
        {
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(text)}&tl=zh-CN&client=tw-ob";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            using var resp = await _httpClient.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                return await resp.Content.ReadAsByteArrayAsync(ct);
            }
        }
        catch { }
        return null;
    }

    private string GetCacheFilePath(string text, string voice, string rate)
    {
        string raw = $"{voice}_{rate}_{text}";
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        string hashStr = Convert.ToHexString(hash).ToLowerInvariant();
        return Path.Combine(_cacheDir, $"{hashStr}.mp3");
    }
}
