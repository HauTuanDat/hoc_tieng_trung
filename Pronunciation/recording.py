"""Prepare a shared audio signal for ASR, IPA and pitch analysis."""

import numpy as np
import soundfile as sf
from scipy.ndimage import gaussian_filter, gaussian_filter1d, maximum_filter1d, median_filter
from scipy.signal import butter, istft, sosfiltfilt, stft


MAX_SECONDS = 30
MIN_SECONDS = 0.15


def read_recording(path):
    with sf.SoundFile(path) as wav:
        rate = wav.samplerate
        if wav.frames > MAX_SECONDS * rate:
            return "too_long", None, rate
        samples = wav.read(dtype="float32", always_2d=True).mean(axis=1)
    if len(samples) < MIN_SECONDS * rate or np.max(np.abs(samples)) < 0.001:
        return "no_speech", None, rate
    return "ok", samples, rate


def _prepare_signal(samples, rate, *, rumble_retry=False):
    """Remove rumble and reduce steady background without gating speech frames."""
    # The retry is for phoneme recognition. Keep more of the low end in the
    # regular signal for pitch measurement, especially for low voices.
    high_pass = butter(4, 280 if rumble_retry else 60, btype="highpass", fs=rate, output="sos")
    filtered = sosfiltfilt(high_pass, samples).astype(np.float32)
    rumble = bool(np.linalg.norm(filtered) < 0.35 * np.linalg.norm(samples))
    window, hop = round(0.032 * rate), round(0.010 * rate)
    _, times, spectrum = stft(filtered, fs=rate, nperseg=window, noverlap=window - hop)
    power = np.abs(spectrum) ** 2
    level = np.sqrt(power.sum(axis=0))
    nonzero = level >= max(0.00004, float(np.median(level)) * 0.08)
    background = np.zeros_like(nonzero)
    if not rumble_retry and np.count_nonzero(nonzero) >= 18:
        quiet_level = np.quantile(level[nonzero], 0.3)
        background = nonzero & (level <= quiet_level)
        if background.sum() >= 8 and quiet_level * 1.8 < median_filter(level, size=5).max():
            noise = np.median(power[:, background], axis=1, keepdims=True)
            gain = np.sqrt(np.clip(1 - 1.7 * noise / np.maximum(power, 1e-12), 0.035, 1))
            gain = gaussian_filter(gain, sigma=(0.7, 0.8))
            spectrum *= gain
            _, filtered = istft(spectrum, fs=rate, nperseg=window, noverlap=window - hop)
            filtered = filtered[:len(samples)].astype(np.float32)
            level = np.linalg.norm(spectrum, axis=0)

    envelope = median_filter(level, size=5)
    floor = float(np.median(level[background])) if background.any() else 0
    threshold = max(0.00012, float(envelope.max()) * 0.10, floor * 2.25)
    active_frames = np.flatnonzero(envelope >= threshold)
    if len(active_frames) < 6:
        return ("low_frequency_noise" if rumble else "noise_only"), None, 0, rumble, 1

    if floor > 0.0001:
        first, last = times[active_frames[[0, -1]]]
        edges = (times < first - 0.035) | (times > last + 0.04)
        edge_gain = np.where(edges & (envelope < threshold), 0.10, 1.0)
        edge_gain = gaussian_filter1d(edge_gain, sigma=1.25)
        filtered *= np.interp(np.arange(len(filtered)) / rate, times, edge_gain).astype(np.float32)

    # Preserve initial consonants and between-word pauses. Only trim the outside.
    start = max(0, int((times[active_frames[0]] - 0.13) * rate))
    end = min(len(filtered), int((times[active_frames[-1]] + 0.16) * rate))
    speech = np.ascontiguousarray(filtered[start:end])
    peak = float(np.max(np.abs(speech)))
    if peak < 0.001:
        return ("low_frequency_noise" if rumble else "no_speech"), None, 0, rumble, 1

    if floor > 0.0001:
        # Residual near-silence can be decoded as a consonant on short words.
        blocks = np.pad(speech, (0, -len(speech) % hop)).reshape(-1, hop)
        rms = np.sqrt(np.mean(blocks ** 2, axis=1))
        gate_floor = peak * 0.0036
        gain = np.clip((rms - gate_floor) / gate_floor, 0.05, 1)
        speech *= np.interp(np.arange(len(speech)) / hop, np.arange(len(rms)) + 0.5, gain).astype(np.float32)
    if len(speech) < rate * 0.75:
        # Give the encoder a quiet context around isolated words. Padding is
        # excluded from voicing and from the measured pronunciation timing.
        leading, trailing = round(0.072 * rate), round(0.108 * rate)
        speech = np.pad(speech, (leading, trailing))
        start -= leading
    return "ok", speech, start / rate, rumble, min(6, 0.7 / peak)


def reduce_noise(samples, rate, *, rumble_retry=False):
    status, speech, offset, rumble, gain = _prepare_signal(samples, rate, rumble_retry=rumble_retry)
    if speech is not None:
        speech *= gain
    return status, speech, offset, rumble


def playback_audio(samples, rate, rumble):
    high_pass = butter(4, 150 if rumble else 100, btype="highpass", fs=rate, output="sos")
    speech = sosfiltfilt(high_pass, samples).astype(np.float32)
    peak = float(np.max(np.abs(speech)))
    if peak < 0.001:
        return speech

    frame_length = round(0.02 * rate)
    frames = np.pad(speech, (0, -len(speech) % frame_length)).reshape(-1, frame_length)
    levels = np.sqrt(np.mean(frames ** 2, axis=1))
    loudness = float(np.quantile(levels, 0.9))
    # Ignore padding and digital silence when estimating the audible background.
    audible = levels[levels >= max(0.00008, loudness * 0.08)]
    background = float(np.quantile(audible, 0.25)) if audible.size else 0
    contrast = loudness / max(background, 0.00008)
    if background and contrast >= 2.2:
        # Keep 60 ms around stronger frames to preserve softer consonants.
        attenuation = np.clip((levels / background - 1.4) / 1.3, 0.08, 1)
        attenuation = gaussian_filter1d(maximum_filter1d(attenuation, size=7), sigma=1)
        speech *= np.interp(np.arange(len(speech)) / frame_length, np.arange(len(levels)) + 0.5,
                            attenuation).astype(np.float32)

    # Apply one gain stage and boost less when the foreground is hard to distinguish.
    gain_limit = float(np.interp(contrast, (1.8, 5), (1, 5)))
    gain = min(gain_limit, 0.065 / max(loudness, 0.001), 0.46 / peak)
    return speech * gain


def prepare_recording(input_path, output_path, playback_path=None):
    status, samples, rate = read_recording(input_path)
    if status == "ok":
        status, samples, offset, rumble, recognition_gain = _prepare_signal(samples, rate)
        if status == "ok":
            sf.write(output_path, samples * recognition_gain, rate, subtype="PCM_16", format="WAV")
            result = {"status": status, "trim_start_seconds": offset, "rumble_dominant": rumble}
            if playback_path:
                try:
                    replay = playback_audio(samples, rate, rumble)
                    edge = min(round(0.015 * rate), len(replay) // 2)
                    if edge > 1:
                        fade = np.sin(np.linspace(0, np.pi / 2, edge, dtype=np.float32)) ** 2
                        replay[:edge] *= fade
                        replay[-edge:] *= fade[::-1]
                    sf.write(playback_path, replay, rate, subtype="PCM_16", format="WAV")
                except (OSError, RuntimeError):
                    pass
                else:
                    result["playback_ready"] = True
            return result
    return {"status": status}
