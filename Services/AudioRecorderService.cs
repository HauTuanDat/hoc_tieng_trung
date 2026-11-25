using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using NAudio.Wave;

namespace QuizApp.Services;

public class AudioRecorderService : IAudioRecorderService
{
    private static readonly WaveFormat Format = new(16000, 16, 1);
    private static readonly int CaptureLimit = 30 * Format.AverageBytesPerSecond + Format.BlockAlign;
    private RecordingSession? _session;
    private MediaPlayer? _player;

    public bool IsRecording => _session is not null;
    public double InputLevel => _session?.InputLevel ?? 0;

    public void StartRecording()
    {
        if (IsRecording) throw new InvalidOperationException("Đang thu âm.");

        var builder = new WasapiRecorderBuilder().WithFormat(Format).WithBufferLength(30);
        try
        {
            _session = OpenSession(builder.WithCommunicationsMode().Build());
        }
        catch (Exception ex) when (ex is COMException or NotSupportedException)
        {
            Debug.WriteLine(ex);
            _session = OpenSession(new WasapiRecorderBuilder().WithFormat(Format).WithBufferLength(30).Build());
        }
    }

    public async Task<string> StopRecordingAsync()
    {
        var session = _session ?? throw new InvalidOperationException("Chưa bắt đầu thu âm.");
        _session = null;
        await FinishRecordingAsync(session, keep: true);
        return session.Path;
    }

    public void CancelRecording()
    {
        if (_session is not RecordingSession session) return;
        _session = null;
        _ = FinishRecordingAsync(session, keep: false);
    }

    private static RecordingSession OpenSession(WasapiRecorder capture)
    {
        string? path = null;
        WaveFileWriter? writer = null;
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "MocNguAudio");
            Directory.CreateDirectory(tempDir);
            path = Path.Combine(tempDir, $"record_{Guid.NewGuid():N}.wav");
            writer = new WaveFileWriter(path, capture.WaveFormat);
            var session = new RecordingSession(capture, writer, path);
            capture.DataAvailable += (data, _, _, _) =>
            {
                int count = (int)Math.Min(data.Length, CaptureLimit - session.Writer.Length);
                session.Writer.Write(data[..count]);
                session.UpdateInputLevel(data[..count]);
                if (session.Writer.Length == CaptureLimit) session.Capture.StopRecording();
            };
            capture.RecordingStopped += (_, args) =>
            {
                session.ResetInputLevel();
                session.Stopped.TrySetResult(args.Exception);
            };
            capture.StartRecording();
            return session;
        }
        catch
        {
            capture.Dispose();
            writer?.Dispose();
            if (path is not null) TryDelete(path);
            throw;
        }
    }

    private static async Task FinishRecordingAsync(RecordingSession session, bool keep)
    {
        try
        {
            try
            {
                await session.Capture.DisposeAsync();
            }
            finally
            {
                session.Writer.Dispose();
            }

            if (keep && await session.Stopped.Task is Exception error)
            {
                throw new IOException("Thiết bị đã dừng thu âm.", error);
            }
        }
        catch (Exception ex)
        {
            TryDelete(session.Path);
            if (keep) throw;
            Debug.WriteLine(ex);
        }
        if (!keep) TryDelete(session.Path);
    }

    private sealed record RecordingSession(WasapiRecorder Capture, WaveFileWriter Writer, string Path)
    {
        private static readonly double MeterDecay = Math.Exp(-2 * Math.PI * 160 / Format.SampleRate);
        private double _previousSample;
        private double _highPass;
        private double _speechBand;
        private double _inputLevel;

        public TaskCompletionSource<Exception?> Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public double InputLevel => Volatile.Read(ref _inputLevel);

        public void UpdateInputLevel(ReadOnlySpan<byte> data)
        {
            var samples = MemoryMarshal.Cast<byte, short>(data);
            if (samples.IsEmpty) return;

            // Attenuate low frequencies in the meter so mic rumble does not
            // look like strong speech. The samples written to the WAV are unchanged.
            double sumSquares = 0;
            foreach (short sample in samples)
            {
                double value = sample / 32768.0;
                double highPass = MeterDecay * (_highPass + value - _previousSample);
                _speechBand = MeterDecay * (_speechBand + highPass - _highPass);
                _highPass = highPass;
                _previousSample = value;
                sumSquares += _speechBand * _speechBand;
            }

            double decibels = 10 * Math.Log10(sumSquares / samples.Length);
            double level = Math.Clamp((decibels + 55) / 43, 0, 1);
            Volatile.Write(ref _inputLevel, Math.Max(level, InputLevel * 0.85));
        }

        public void ResetInputLevel() => Volatile.Write(ref _inputLevel, 0);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex);
        }
    }

    public void PlayRecording(string wavFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(wavFilePath) || !File.Exists(wavFilePath))
            {
                return;
            }

            StopPlayback();
            var player = new MediaPlayer { Volume = 1.0 };
            _player = player;
            player.MediaOpened += (_, _) =>
            {
                if (_player == player) player.Play();
            };
            player.MediaEnded += (_, _) =>
            {
                if (_player == player) StopPlayback();
            };
            player.MediaFailed += (_, args) =>
            {
                Debug.WriteLine(args.ErrorException);
                if (_player == player) StopPlayback();
            };
            player.Open(new Uri(Path.GetFullPath(wavFilePath), UriKind.Absolute));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioRecorderService] Play error: {ex.Message}");
            StopPlayback();
        }
    }

    public void StopPlayback()
    {
        var player = _player;
        _player = null;
        try
        {
            player?.Close();
        }
        catch
        {
            // Ignore
        }
    }
}

