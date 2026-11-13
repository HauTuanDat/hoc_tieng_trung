using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace QuizApp.Services;

public class AudioRecorderService : IAudioRecorderService
{
    [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
    private static extern int mciSendString(string command, StringBuilder? returnString, int returnLength, IntPtr callback);

    private bool _isRecording;
    private string? _lastRecordedFilePath;
    private SoundPlayer? _player;

    public bool IsRecording => _isRecording;

    public void StartRecording()
    {
        try
        {
            mciSendString("close recsound", null, 0, IntPtr.Zero);
            mciSendString("open new type waveaudio alias recsound", null, 0, IntPtr.Zero);
            mciSendString("set recsound bitspersample 16 channels 1 samplespersec 16000 alignment 2 bytespersec 32000", null, 0, IntPtr.Zero);
            mciSendString("record recsound", null, 0, IntPtr.Zero);
            _isRecording = true;
        }
        catch (Exception ex)
        {
            _isRecording = false;
            System.Diagnostics.Debug.WriteLine($"[AudioRecorderService] Start error: {ex.Message}");
        }
    }

    public string StopRecording()
    {
        if (!_isRecording)
        {
            return _lastRecordedFilePath ?? string.Empty;
        }

        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "MocNguAudio");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string filePath = Path.Combine(tempDir, $"record_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.wav");
            mciSendString($"save recsound \"{filePath}\"", null, 0, IntPtr.Zero);
            mciSendString("close recsound", null, 0, IntPtr.Zero);

            _isRecording = false;
            _lastRecordedFilePath = filePath;
            return filePath;
        }
        catch (Exception ex)
        {
            _isRecording = false;
            System.Diagnostics.Debug.WriteLine($"[AudioRecorderService] Stop error: {ex.Message}");
            return string.Empty;
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
            _player = new SoundPlayer(wavFilePath);
            _player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioRecorderService] Play error: {ex.Message}");
        }
    }

    public void StopPlayback()
    {
        try
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
        }
        catch
        {
            // Ignore
        }
    }

    public double CalculateRms(string wavFilePath)
    {
        try
        {
            if (!File.Exists(wavFilePath)) return 0;
            var bytes = File.ReadAllBytes(wavFilePath);
            if (bytes.Length <= 44) return 0; // Header only

            double sumSquares = 0;
            int sampleCount = (bytes.Length - 44) / 2;
            if (sampleCount <= 0) return 0;

            for (int i = 44; i < bytes.Length - 1; i += 2)
            {
                short sample = (short)(bytes[i] | (bytes[i + 1] << 8));
                sumSquares += (double)sample * sample;
            }

            double rms = Math.Sqrt(sumSquares / sampleCount);
            return rms; // normalized RMS volume
        }
        catch
        {
            return 0;
        }
    }
}

