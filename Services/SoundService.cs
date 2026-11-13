using System.IO;
using System.Media;

namespace QuizApp.Services;

public interface ISoundService
{
    void PlayCorrect();
    void PlayIncorrect();
    void PlayCompleted();
    bool IsMuted { get; set; }
}

public class SoundService : ISoundService
{
    private static readonly Lazy<SoundService> _instance = new(() => new SoundService());
    public static SoundService Instance => _instance.Value;

    private SoundPlayer? _correctPlayer;
    private SoundPlayer? _incorrectPlayer;
    private SoundPlayer? _completedPlayer;
    private bool _isMuted;

    public bool IsMuted
    {
        get => _isMuted;
        set => _isMuted = value;
    }

    public SoundService()
    {
        // Pre-generate audio streams asynchronously so startup is instant
        Task.Run(() =>
        {
            try
            {
                var correctBytes = GenerateCorrectWav();
                _correctPlayer = new SoundPlayer(new MemoryStream(correctBytes));
                _correctPlayer.LoadAsync();

                var incorrectBytes = GenerateIncorrectWav();
                _incorrectPlayer = new SoundPlayer(new MemoryStream(incorrectBytes));
                _incorrectPlayer.LoadAsync();

                var completedBytes = GenerateCompletedWav();
                _completedPlayer = new SoundPlayer(new MemoryStream(completedBytes));
                _completedPlayer.LoadAsync();
            }
            catch
            {
                // Fallback to system sounds if needed
            }
        });
    }

    public void PlayCorrect()
    {
        if (_isMuted) return;
        Task.Run(() =>
        {
            try
            {
                if (_correctPlayer != null)
                {
                    _correctPlayer.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch
            {
                SystemSounds.Asterisk.Play();
            }
        });
    }

    public void PlayIncorrect()
    {
        if (_isMuted) return;
        Task.Run(() =>
        {
            try
            {
                if (_incorrectPlayer != null)
                {
                    _incorrectPlayer.Play();
                }
                else
                {
                    SystemSounds.Hand.Play();
                }
            }
            catch
            {
                SystemSounds.Hand.Play();
            }
        });
    }

    public void PlayCompleted()
    {
        if (_isMuted) return;
        Task.Run(() =>
        {
            try
            {
                if (_completedPlayer != null)
                {
                    _completedPlayer.Play();
                }
                else
                {
                    SystemSounds.Exclamation.Play();
                }
            }
            catch
            {
                SystemSounds.Exclamation.Play();
            }
        });
    }

    private static byte[] GenerateCorrectWav()
    {
        // Upbeat bright arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
        return SynthesizeChime(new[]
        {
            (523.25, 0.07, 0.8),
            (659.25, 0.07, 0.85),
            (783.99, 0.09, 0.9),
            (1046.50, 0.26, 1.0)
        });
    }

    private static byte[] GenerateIncorrectWav()
    {
        // Soft, gentle double-thump: low A3 (220Hz) -> F3 (174Hz) with warm decay
        return SynthesizeChime(new[]
        {
            (220.0, 0.09, 0.65),
            (174.61, 0.18, 0.75)
        });
    }

    private static byte[] GenerateCompletedWav()
    {
        // Joyous victory fanfare: C5 -> E5 -> G5 -> C6 -> E6
        return SynthesizeChime(new[]
        {
            (523.25, 0.08, 0.8),
            (659.25, 0.08, 0.85),
            (783.99, 0.08, 0.9),
            (1046.50, 0.12, 0.95),
            (1318.51, 0.35, 1.0)
        });
    }

    private static byte[] SynthesizeChime((double Frequency, double Duration, double Volume)[] notes)
    {
        const int sampleRate = 44100;
        int totalSamples = 0;
        foreach (var note in notes)
        {
            totalSamples += (int)(sampleRate * note.Duration);
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int dataLength = totalSamples * 2; // 16-bit mono

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(dataLength + 36);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size
        bw.Write((short)1); // PCM
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // ByteRate
        bw.Write((short)2); // BlockAlign
        bw.Write((short)16); // BitsPerSample
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataLength);

        // Generate audio waveform
        foreach (var (freq, duration, volume) in notes)
        {
            int noteSamples = (int)(sampleRate * duration);
            for (int i = 0; i < noteSamples; i++)
            {
                double t = (double)i / sampleRate;
                double progress = (double)i / noteSamples;

                // Smooth attack and exponential decay envelope
                double attack = Math.Min(1.0, progress * 20.0); // fast 5% attack
                double decay = Math.Exp(-3.5 * progress); // smooth bell-like fade
                double envelope = attack * decay * volume;

                // Fundamental sine wave + subtle 2nd harmonic overtone for warm crystal tone
                double fundamental = Math.Sin(2.0 * Math.PI * freq * t);
                double overtone = 0.25 * Math.Sin(4.0 * Math.PI * freq * t);
                double sampleValue = (fundamental + overtone) * envelope * 18000.0;

                short sample = (short)Math.Clamp(sampleValue, short.MinValue, short.MaxValue);
                bw.Write(sample);
            }
        }

        bw.Flush();
        return ms.ToArray();
    }
}
