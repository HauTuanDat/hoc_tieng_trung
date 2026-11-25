"""Pitch and timing evidence for Mandarin pronunciation assessment."""

import numpy as np
from parselmouth import Sound
from scipy.ndimage import median_filter
from scipy.signal import butter, sosfiltfilt


HOP_SECONDS = 0.01


def measure_audio(samples, sample_rate):
    """Track pitch by autocorrelation after attenuating higher formants."""
    low_pass = butter(4, 900, fs=sample_rate, output="sos")
    filtered = sosfiltfilt(low_pass, samples)
    track = Sound(filtered, sample_rate).to_pitch_ac(
        time_step=HOP_SECONDS, pitch_floor=65, pitch_ceiling=550,
        silence_threshold=0.035, octave_jump_cost=0.5,
    )
    times = track.xs()
    pitch = track.selected_array["frequency"].copy()
    pitch[pitch == 0] = np.nan
    midpoints = np.rint(times * sample_rate).astype(int)
    radius = round(HOP_SECONDS * sample_rate)
    energy = np.array([np.sqrt(np.mean(samples[max(0, center - radius):center + radius] ** 2))
                       for center in midpoints])
    energy_floor = max(0.00035, float(np.quantile(energy, 0.95)) * 0.07)
    return times, pitch, (energy >= energy_floor) | np.isfinite(pitch)


def syllable_spans(anchors, times, pitch, active):
    """Use CTC token times and nearby dips in voicing or energy to find boundaries."""
    if not anchors or not active.any():
        return []
    first, last = np.flatnonzero(active)[[0, -1]]
    edges = (times[first], times[last] + HOP_SECONDS)
    anchors = np.clip(anchors, *edges)
    boundaries = [edges[0]]
    for left, right in zip(anchors, anchors[1:]):
        # CTC emissions usually occur after the vowel onset. Searching a little
        # earlier keeps the following syllable out of the first F0 contour.
        midpoint = (left + right) / 2 - min(0.06, (right - left) * 0.20)
        search_width = min((right - left) * 0.47, 0.19)
        candidates = np.flatnonzero(np.abs(times - midpoint) <= search_width)
        if len(candidates):
            distance = np.abs(times[candidates] - midpoint) / max(search_width, HOP_SECONDS)
            cost = 0.55 * distance + 0.4 * active[candidates] + np.isfinite(pitch[candidates])
            midpoint = times[candidates[np.argmin(cost)]]
        boundaries.append(max(boundaries[-1], midpoint))
    boundaries.append(edges[1])
    return list(zip(boundaries, boundaries[1:]))


def pitch_contour(times, pitch, span):
    if span is None:
        return None
    indices = np.flatnonzero((times >= span[0]) & (times < span[1]) & np.isfinite(pitch))
    if len(indices) < 6:
        return None
    # Unvoiced consonants may separate nuclei. A short creaky break inside a
    # third tone is allowed, while a long gap could mix two different syllables.
    semitones = np.log2(pitch[indices]) * 12
    breaks = (np.diff(indices) > 7) | (np.abs(np.diff(semitones)) > 6)
    runs = np.split(semitones, np.flatnonzero(breaks) + 1)
    longest = max(runs, key=len)
    if len(longest) < 6:
        return None
    smoothed = median_filter(longest, size=3, mode="nearest")
    return np.asarray([np.median(part) for part in np.array_split(smoothed, 5)])


def proportion(value, minimum, full):
    return float(np.clip((value - minimum) / (full - minimum), 0, 1))


def contour_fit(contour, tone, speaker_center, speaker_range, allow_half_third):
    """Return a soft fit against each tone's shape in speaker-relative semitones."""
    start, early, middle, late, end = contour
    bottom = min(early, middle, late)
    if tone == 1:
        level = 1 - proportion(float(np.ptp(contour)), 2.5, 5.8)
        height = 1.0 if speaker_range < 3 else proportion(float(np.mean(contour)) - speaker_center, -4, 0)
        return 0.8 * level + 0.2 * height
    if tone == 2:
        rise = max(end - start, (end - min(start, early, middle)) * 0.85)
        return proportion(rise, 0.5, 3.5)
    if tone == 3:
        dipping = min(proportion(start - bottom, 0.15, 2.5), proportion(end - bottom, 0.15, 2.5))
        lower = min(1 - proportion(float(np.mean(contour)) - speaker_center, 0, 3),
                    1 - proportion(end - start, 0.4, 3.5))
        if speaker_range >= 3:
            return max(dipping, (1 if allow_half_third else 0.7) * lower)
        return dipping
    return proportion(start - end, 0.7, 4.0)


def transcription_tone_fits(contour, model_tone, confidence, center, spread):
    if contour is None:
        return {tone: 0.5 + (0.25 if model_tone == tone else 0) for tone in range(1, 6)}

    fits = {tone: contour_fit(contour, tone, center, spread, False) for tone in range(1, 5)}
    start, _, middle, late, end = contour
    # Tone 3 can reach a low pitch early and stay there. Tone 4 typically
    # continues falling later into the vowel.
    low_tail = (proportion(start - middle, 3, 6)
                * (1 - proportion(abs(middle - end), 1.5, 3))
                * (1 - proportion(abs(late - end), 0.5, 1.5)))
    fits[3] = max(fits[3], low_tail)
    fits[4] *= 1 - 0.65 * low_tail
    model_weight = 0.42 * confidence * (1 - 0.7 * low_tail)
    return {tone: (1 - model_weight) * fit + model_weight * (model_tone == tone)
            for tone, fit in (fits | {5: 0.5}).items()}


def naturalness(times, active, spans, pause_boundaries):
    durations = np.array([np.count_nonzero((times >= start) & (times < end) & active) * HOP_SECONDS
                          for start, end in spans])
    if np.count_nonzero(durations >= 0.045) < len(spans) / 2:
        return None, "rhythm_uncertain"
    # Keep a wide speaking-rate range and tolerate sentence-final lengthening.
    pace = float(np.median(durations))
    pace_penalty = max(1 - proportion(pace, 0.045, 0.10), proportion(pace, 0.52, 0.95))
    uneven_penalty = 0.0
    if len(durations) >= 3 and pace > 0:
        uneven_penalty = float(np.mean(np.clip((durations[:-1] / pace - 1.9) / 1.6, 0, 1)))

    pause_penalty = 0.0
    for _, boundary in spans[:-1]:
        split = np.searchsorted(times, boundary)
        previous = np.flatnonzero(active[:split])
        following = np.flatnonzero(active[split:])
        if not len(previous) or not len(following):
            continue
        gap = times[split + following[0]] - times[previous[-1]] - HOP_SECONDS
        threshold = 0.48 if boundary in pause_boundaries else 0.19
        pause_penalty = max(pause_penalty, proportion(gap, threshold, threshold + 0.42))

    penalty = 0.40 * pace_penalty + 0.20 * uneven_penalty + 0.40 * pause_penalty
    if pause_penalty >= 0.35:
        detail = "long_pause"
    elif uneven_penalty >= 0.15 or pace_penalty >= 0.3:
        detail = "uneven_pace"
    else:
        detail = "steady_pace"
    return 1 - penalty, detail
