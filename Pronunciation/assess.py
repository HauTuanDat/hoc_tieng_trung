"""Local Mandarin IPA inference. Read JSON requests from stdin and reply on stdout."""

import json
import os
import re
import sys
import traceback
import unicodedata
from contextlib import redirect_stdout
from pathlib import Path

os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"

ROOT = Path(sys.argv[1])
UPSTREAM = ROOT / "upstream"
sys.path.insert(0, str(UPSTREAM / "src"))

with redirect_stdout(sys.stderr):
    import numpy as np
    import torch
    import torchaudio
    import yaml
    from pypinyin import Style, lazy_pinyin
    from pypinyin.contrib.tone_convert import to_tone, to_tone3
    from acoustics import (contour_fit, measure_audio, naturalness, pitch_contour,
                           proportion, syllable_spans, transcription_tone_fits)
    from recording import prepare_recording, read_recording, reduce_noise
    from converter import SCHEME_UNT_PHESOCA_NARROW, is_cjk, load_converter_data
    from utils import compat_remap_state, levenshtein_align, load_vocab


MIN_TOKEN_CONFIDENCE = 0.5
TONE_LETTERS = "˥˦˧˨˩"
TONE_MARKS = "\u0304\u0301\u030c\u0300"
INITIALS = ("zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h",
            "j", "q", "x", "r", "z", "c", "s")


def has_confident_syllable(prediction):
    return any(confidence >= MIN_TOKEN_CONFIDENCE and not token.startswith("<")
               for token, confidence, _ in prediction)


def pinyin_parts(syllable):
    initial = next((initial for initial in INITIALS if syllable.startswith(initial)), "")
    final = syllable[len(initial):]
    if syllable.startswith("y"):
        final = ("v" + syllable[2:] if syllable.startswith("yu") else
                 syllable[1:] if syllable.startswith("yi") else "i" + syllable[1:])
    if syllable.startswith("w"):
        final = syllable[1:] if syllable.startswith("wu") else "u" + syllable[1:]
    if initial in ("j", "q", "x") and final.startswith("u"):
        final = "v" + final[1:]
    if final == "i" and initial in ("z", "c", "s", "zh", "ch", "sh", "r"):
        final = "apical" if len(initial) == 1 and initial != "r" else "retroflex"
    final = {"iu": "iou", "ui": "uei", "un": "uen", "vn": "ün", "v": "ü", "ve": "üe",
             "van": "üan"}.get(final, final)
    return initial, final


def parse_written_pinyin(text, characters, fallback, lexicon):
    """Split joined Pinyin such as píngguǒ while preserving the written tones."""
    normalized = unicodedata.normalize("NFC", text.lower().replace("u:", "ü"))
    groups = re.findall(r"[a-züêāáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜńňǹḿ0-5]+", normalized)
    if not groups:
        return fallback
    candidates = {}
    offset = 0
    for group in groups:
        for start in range(len(group)):
            options = []
            for end in range(start + 1, min(len(group), start + 7) + 1):
                part = group[start:end]
                plain = unicodedata.normalize("NFD", part)
                marks = sum(character in TONE_MARKS or character.isdigit() for character in plain)
                if marks > 1 or any(character.isdigit() for character in part[:-1]):
                    continue
                value = to_tone3(part, neutral_tone_with_five=True)
                if value.endswith("0"):
                    value = value[:-1] + "5"
                if value[:-1] in lexicon["py2ipa"]:
                    options.append((offset + end, value))
            candidates[offset + start] = options
        offset += len(group)

    paths = {0: (0, [])}
    for index, character in enumerate(characters):
        common = {value[:-1] for value in lexicon["char2py"].get(character, [])}
        base = fallback[index][:-1] if len(fallback) == len(characters) else ""
        next_paths = {}
        for start, (score, values) in paths.items():
            for end, value in candidates.get(start, []):
                spelling = value[:-1]
                fit = score + (3 if spelling == base else 2 if spelling in common else 0)
                if end not in next_paths or fit > next_paths[end][0]:
                    next_paths[end] = fit, values + [value]
        paths = next_paths
    if offset not in paths:
        return fallback

    written = paths[offset][1]
    if len(fallback) == len(written) and not any(character in TONE_MARKS or character.isdigit()
                                                 for character in unicodedata.normalize("NFD", normalized)):
        return [known if value[:-1] == known[:-1] else value for value, known in zip(written, fallback)]
    return written


def criterion(values, maximum, extra=0):
    measured = [value for value in values if value is not None]
    score = None
    if measured and len(measured) * 2 >= len(values):
        score = int(maximum * sum(measured) / (len(measured) + extra) + 0.5)
    return {"score": score, "maximum": maximum, "evaluated": len(measured), "targets": len(values)}


def asr_characters(text):
    return [ch for ch in unicodedata.normalize("NFKC", text).translate(str.maketrans("0123456789", "零一二三四五六七八九"))
            if is_cjk(ch) or ch.isalnum()]


def asr_matches(reference, transcripts):
    """Find text support without treating ASR's choice of character as acoustic proof."""
    expected = [row[0] for row in reference]
    return {target_index for transcript in transcripts if transcript
            for operation, _, target_index in levenshtein_align(asr_characters(transcript), expected)
            if operation == "match"}


def transcript_models(candidates):
    grouped = {}
    for candidate in candidates:
        text, model = (candidate, None) if isinstance(candidate, str) else (candidate["text"], candidate["model"])
        key = "".join(asr_characters(text))
        if key:
            grouped.setdefault(key, (text, set()))[1].add(model)
    return list(grouped.values())


class MandarinAssessor:
    def __init__(self):
        self.model = None
        self.lexicon = load_converter_data()
        vocab = UPSTREAM / "vocab"
        self.token_ids, self.tokens = load_vocab(vocab / "vocab_mandarin_ipa_combined.json")
        self.tones = json.loads((vocab / "vocab_mandarin_ipa_tone_combined.json").read_text(encoding="utf-8"))
        self.spellings = {}
        for spelling, variants in self.lexicon["py2ipa"].items():
            ipa = variants[SCHEME_UNT_PHESOCA_NARROW]
            self.spellings.setdefault(ipa, []).append(spelling)
        self.base_tokens = {}
        for token, index in self.token_ids.items():
            base = token.rstrip(TONE_LETTERS)
            if base in self.spellings:
                self.base_tokens.setdefault(base, []).append(index)

    def load_model(self):
        from funasr import AutoModel

        torch.set_num_threads(min(4, os.cpu_count() or 1))
        base = ROOT / "sensevoice"
        config = yaml.safe_load((base / "config.yaml").read_text(encoding="utf-8"))
        config["frontend_conf"].update(cmvn_file=str(base / "am.mvn"), dither=0.0)
        config["tokenizer_conf"]["bpemodel"] = str(base / "chn_jpn_yue_eng_ko_spectok.bpe.model")
        self.frame_seconds = config["frontend_conf"]["frame_shift"] * config["frontend_conf"]["lfr_n"] / 1000
        # The IPA checkpoint includes the entire encoder. Build it from the local
        # config so FunASR does not fetch or load a second checkpoint.
        encoder_model, options = AutoModel.build_model(**config, device="cpu", ncpu=torch.get_num_threads())
        network = torch.nn.Module()
        network.sense_voice = encoder_model
        network.frontend = options["frontend"]
        network.ctc_head = torch.nn.Sequential(
            torch.nn.Linear(512, 512), torch.nn.ReLU(), torch.nn.Dropout(0.1),
            torch.nn.Linear(512, len(self.token_ids)),
        )
        checkpoint = torch.load(ROOT / "model" / "base.pt", map_location="cpu", weights_only=True)
        state = compat_remap_state(checkpoint.get("model_state_dict", checkpoint))
        network.load_state_dict(state, strict=True)
        self.model = network.eval()

    def reference(self, hanzi, pinyin):
        characters = [(position, character) for position, character in enumerate(hanzi) if is_cjk(character)]
        if not characters:
            return []

        fallback = lazy_pinyin(hanzi, style=Style.TONE3, neutral_tone_with_five=True, errors="ignore")
        pinyin_ipa = self.lexicon["py2ipa"]
        written = parse_written_pinyin(pinyin, [character for _, character in characters], fallback, self.lexicon)

        if len(written) != len(characters):
            return []

        syllables = []
        for index, ((position, character), value) in enumerate(zip(characters, written)):
            if not value or value[-1] not in "12345" or value[:-1] not in pinyin_ipa:
                return []
            tone = int(value[-1])
            accepted = [tone]
            pause_after = index + 1 == len(written) or bool(hanzi[position + 1:characters[index + 1][0]].strip())
            following = "" if pause_after else written[index + 1][-1:]
            if tone == 3 and following == "3":
                accepted.append(2)
            if character == "不" and tone == 4 and following == "4":
                accepted.append(2)
            if character == "一" and tone == 1 and following in ("1", "2", "3", "4"):
                if index == 0 or characters[index - 1][1] != "第":
                    accepted.append(2 if following == "4" else 4)
            syllables.append((character, value, pinyin_ipa[value[:-1]][SCHEME_UNT_PHESOCA_NARROW], accepted, pause_after))
        return syllables

    @torch.inference_mode()
    def recognize(self, samples, sample_rate):
        if self.model is None:
            self.load_model()
        waveform = torch.from_numpy(samples)
        if sample_rate != 16000:
            waveform = torchaudio.functional.resample(waveform, sample_rate, 16000)
        features, lengths = self.model.frontend(waveform.unsqueeze(0), torch.tensor([waveform.numel()]))
        encoded, lengths, *_ = self.model.sense_voice.encoder(features, lengths)
        logits = self.model.ctc_head(encoded[0, :int(lengths[0])]).softmax(dim=-1)
        probabilities, ids = logits.max(dim=-1)
        tokens, frame_counts = torch.unique_consecutive(ids, return_counts=True)
        groups = probabilities.split(frame_counts.tolist())
        prediction = []
        offset = 0
        for token, group in zip(tokens.tolist(), groups):
            if token != 0:
                peak = int(group.argmax())
                prediction.append((self.tokens[token], float(group[peak]), (offset + peak) * self.frame_seconds + 0.0125))
            offset += len(group)
        return prediction, logits

    def corroborated_token(self, probabilities, expected_base, anchor, competing_confidence):
        # Consult the same CTC neighbourhood without forcing the reference through
        # the decoder. The expected sound must already be a strong alternative.
        options = self.base_tokens.get(expected_base)
        if not options:
            return None
        center = round((anchor - 0.0125) / self.frame_seconds)
        first, last = max(0, center - 1), min(len(probabilities), center + 2)
        scores = probabilities[first:last, options]
        frame_confidence = scores.sum(dim=1)
        frame = int(frame_confidence.argmax())
        confidence = float(frame_confidence[frame])
        if confidence < max(0.33, competing_confidence * 0.70):
            return None
        return self.tokens[options[int(scores[frame].argmax())]]

    def heard_pinyin(self, token):
        spellings = self.spellings.get(token.rstrip(TONE_LETTERS), [])
        tone = self.tones.get(token, 5)
        return "/".join(to_tone(f"{spelling}{tone}") for spelling in spellings)

    def transcript_agreements(self, prediction, transcripts):
        recognized = [token.rstrip(TONE_LETTERS) for token, confidence, _ in prediction
                      if confidence >= MIN_TOKEN_CONFIDENCE]
        pinyin_ipa = self.lexicon["py2ipa"]
        agreements = []
        for transcript in transcripts:
            syllables = lazy_pinyin("".join(asr_characters(transcript)), errors="ignore")
            alternatives = [pinyin_ipa[syllable][SCHEME_UNT_PHESOCA_NARROW]
                            for syllable in syllables if syllable in pinyin_ipa]
            if alternatives:
                alignment = levenshtein_align(recognized, alternatives)
                agreements.append(sum(op == "match" for op, _, _ in alignment) / len(alignment))
            else:
                agreements.append(0.0)
        return agreements

    def select_transcript(self, options, agreements, prediction, tone_fits=None, *, lesson_reference=None):
        if not options:
            return {}

        recognized = [index for index, (_, confidence, _) in enumerate(prediction)
                      if confidence >= MIN_TOKEN_CONFIDENCE]
        bases = [prediction[index][0].rstrip(TONE_LETTERS) for index in recognized]
        ranks = []
        readings_by_option = {}
        for index, ((text, models), agreement) in enumerate(zip(options, agreements)):
            score = agreement + 0.09 * len(models)
            if tone_fits and agreement > 0:
                readings = self.reference("".join(asr_characters(text)), "")
                readings_by_option[index] = [(row[2], row[3]) for row in readings]
                alignment = levenshtein_align(bases, [row[2] for row in readings])
                tone_match = sum(max(tone_fits[recognized[spoken]][tone] for tone in readings[target][3])
                                 for operation, spoken, target in alignment if operation == "match")
                if alignment:
                    score += 0.34 * tone_match / len(alignment)
            ranks.append(score)

        ranked = sorted(range(len(ranks)), key=ranks.__getitem__, reverse=True)
        best = ranked[0]
        used_lesson_spelling = False
        if lesson_reference and agreements[best] == 1 and best in readings_by_option:
            target_chars = [row[0] for row in lesson_reference]
            target_reading = [(row[2], row[3]) for row in lesson_reference]
            if readings_by_option[best] == target_reading:
                for index in ranked[1:]:
                    if (agreements[index] == 1 and readings_by_option.get(index) == target_reading
                            and asr_characters(options[index][0]) == target_chars):
                        # Both spellings were decoded and match the same Pinyin and tones.
                        ranked.remove(index)
                        ranked.insert(0, index)
                        best = index
                        used_lesson_spelling = True
                        break
        alternate = ranked[1] if len(ranked) > 1 else None
        close = alternate is not None and ranks[best] - ranks[alternate] < 0.12
        return {"preferred_transcript": options[best][0],
                "alternate_transcript": options[alternate][0] if alternate is not None else None,
                "transcript_uncertain": agreements[best] < 0.5 or close,
                "used_lesson_spelling": used_lesson_spelling}

    def assess(self, request, report):
        status, samples, sample_rate = read_recording(request["wav_path"])
        rumble = request.get("rumble_dominant", False)
        if status == "ok" and not request.get("prepared", False):
            status, samples, _, rumble = reduce_noise(samples, sample_rate)
        if status != "ok":
            return {"status": status}

        report("phonemes")
        prediction, probabilities = self.recognize(samples, sample_rate)
        options = transcript_models(request.get("transcripts", []))
        transcripts = [text for text, _ in options]
        agreements = self.transcript_agreements(prediction, transcripts)
        anchor_offset = 0.0
        original_path = request.get("original_wav_path")
        if original_path and any(transcripts):
            agreement = max(agreements)
            if agreement < 1:
                original_status, original, original_rate = read_recording(original_path)
                if original_status == "ok":
                    alternate, alternate_probabilities = self.recognize(original, original_rate)
                    alternate_agreements = self.transcript_agreements(alternate, transcripts)
                    original_offset = request.get("trim_start_seconds", 0.0)
                    if max(alternate_agreements) > agreement:
                        prediction, probabilities = alternate, alternate_probabilities
                        agreements = alternate_agreements
                        anchor_offset = original_offset
                    if rumble and max(agreements) < 1:
                        replacing_confident = has_confident_syllable(prediction)
                        retry_status, retry, retry_offset, _ = reduce_noise(original, original_rate, rumble_retry=True)
                        if retry_status == "ok":
                            alternate, alternate_probabilities = self.recognize(retry, original_rate)
                            alternate_agreements = self.transcript_agreements(alternate, transcripts)
                            # Replacing recognizable phonemes requires a stronger
                            # decode that also agrees with an independent transcript.
                            strong_retry = all(confidence >= 0.80 for _, confidence, _ in alternate)
                            if max(alternate_agreements) == 1 and (not replacing_confident or strong_retry):
                                prediction, probabilities = alternate, alternate_probabilities
                                agreements = alternate_agreements
                                anchor_offset = original_offset - retry_offset
        heard = " ".join(pinyin for token, confidence, _ in prediction if confidence >= MIN_TOKEN_CONFIDENCE
                         if (pinyin := self.heard_pinyin(token)))
        evidence = {"heard_pinyin": heard}
        if not has_confident_syllable(prediction):
            return {"status": "low_frequency_noise" if rumble else "ipa_uncertain", **evidence,
                    **self.select_transcript(options, agreements, prediction)}

        reference = self.reference(request["hanzi"], request["pinyin"])
        if not reference:
            return {"status": "reference_unavailable", **evidence,
                    **self.select_transcript(options, agreements, prediction)}
        supported = asr_matches(reference, transcripts)

        prediction_bases = [token.rstrip(TONE_LETTERS) for token, _, _ in prediction]
        reference_bases = [syllable[2] for syllable in reference]
        alignment = levenshtein_align(prediction_bases, reference_bases)
        rows = []
        initials, finals, syllables = [], [], []
        for operation, spoken_index, target_index in alignment:
            spoken, confidence, anchor = prediction[spoken_index] if spoken_index >= 0 else (None, 0, 0)
            character, pinyin = reference[target_index][:2] if target_index >= 0 else (None, None)
            spoken_base = prediction_bases[spoken_index] if spoken_index >= 0 else None
            corroborated = None
            if target_index in supported and operation == "sub":
                corroborated = self.corroborated_token(probabilities, reference_bases[target_index], anchor, confidence)
                if corroborated is not None:
                    spoken = corroborated
                    spoken_base = reference_bases[target_index]
            row = {"hanzi": character, "pinyin": to_tone(pinyin) if pinyin else None,
                   "ipa": spoken, "tone": self.tones.get(spoken)}
            if pinyin is not None:
                expected_initial, expected_final = pinyin_parts(pinyin[:-1])
                row.update(expected_initial=expected_initial, expected_final=expected_final)
            initials_match, finals_match = None, None
            if operation == "ins" and target_index in supported:
                outcome = "model_disagreement"
            elif operation == "ins":
                outcome = "missing"
                initials_match, finals_match = False, False
            elif corroborated is None and (confidence < MIN_TOKEN_CONFIDENCE or spoken_base not in self.spellings):
                outcome = "model_disagreement" if target_index in supported else "uncertain"
            elif operation == "del":
                outcome = "extra"
            else:
                alternatives = self.spellings[spoken_base]
                spelling = max(alternatives, key=lambda value: sum(x == y for x, y in zip(
                    pinyin_parts(value), (expected_initial, expected_final))))
                actual_initial, actual_final = pinyin_parts(spelling)
                initials_match, finals_match = expected_initial == actual_initial, expected_final == actual_final
                if operation == "match" or corroborated is not None:
                    outcome = "tone_uncertain"
                    row.update(actual_initial=actual_initial, actual_final=actual_final)
                elif target_index in supported:
                    outcome = "model_disagreement"
                    initials_match = True if initials_match else None
                    finals_match = True if finals_match else None
                    row.update(actual_initial=actual_initial if initials_match else None,
                               actual_final=actual_final if finals_match else None)
                else:
                    outcome = "syllable_mismatch"
                    row.update(actual_initial=actual_initial, actual_final=actual_final)
            row["outcome"] = outcome
            rows.append(row)
            if target_index >= 0:
                initials.append(initials_match)
                finals.append(finals_match)
                syllables.append(None if outcome in ("uncertain", "model_disagreement")
                                 else operation == "match" or corroborated is not None)

        report("tones")
        times, pitch, active = measure_audio(samples, sample_rate)
        spans = syllable_spans([time - anchor_offset for _, _, time in prediction], times, pitch, active)
        voiced = np.log2(pitch[np.isfinite(pitch)]) * 12
        center = float(np.median(voiced)) if len(voiced) else 0
        spread = float(np.quantile(voiced, 0.9) - np.quantile(voiced, 0.1)) if len(voiced) else 0
        contours = [pitch_contour(times, pitch, spans[index] if spans else None) for index in range(len(prediction))]
        tone_fits = [transcription_tone_fits(contour, self.tones.get(token), confidence, center, spread)
                     for contour, (token, confidence, _) in zip(contours, prediction)]
        evidence.update(self.select_transcript(options, agreements, prediction, tone_fits, lesson_reference=reference))
        durations = [np.count_nonzero((times >= lo) & (times < hi) & np.isfinite(pitch)) for lo, hi in spans]
        typical_duration = float(np.median([length for length in durations if length >= 6])) if any(
            length >= 6 for length in durations) else 0
        tone_values = []
        measured_tones = 0
        recognizable_tones = 0
        pause_boundaries = set()
        for row, (_, spoken_index, target_index) in zip(rows, alignment):
            if target_index < 0:
                continue
            if row["outcome"] == "missing":
                tone_values.append(0)
                continue
            _, _, _, expected_tones, pause_after = reference[target_index]
            span = spans[spoken_index] if spans else None
            if span is not None and pause_after:
                pause_boundaries.add(span[1])
            if row["outcome"] in ("uncertain", "model_disagreement"):
                tone_values.append(None)
                continue
            recognizable_tones += 1
            contour = contours[spoken_index]
            model_match = row["tone"] in expected_tones
            if contour is None:
                tone_values.append((0.6 if model_match else 0.2) if row["tone"] is not None else None)
                row["tone_feedback"] = "model_only" if row["tone"] is not None else None
                continue
            measured_tones += 1
            if expected_tones == [5]:
                # Neutral tone depends on the previous tone. Its shorter
                # voicing is a useful cue when a neighbouring syllable exists.
                acoustic = (1 - proportion(durations[spoken_index] / typical_duration, 0.65, 1.25)
                            if len(spans) > 1 else 0.5)
                fit = 0.65 * model_match + 0.35 * acoustic
            else:
                acoustic = max(contour_fit(contour, tone, center, spread, not pause_after) for tone in expected_tones)
                reliable_tone = (model_match and row["outcome"] == "tone_uncertain" and
                                 row["ipa"] == prediction[spoken_index][0] and prediction[spoken_index][1] >= 0.80)
                fit = 0.78 + 0.22 * acoustic if reliable_tone else 0.35 * model_match + 0.65 * acoustic
            row["pitch_change"] = round(float(contour[-1] - contour[0]), 1)
            row["tone_feedback"] = "close" if fit >= 0.72 else "different" if fit < 0.4 else "partial"
            if row["outcome"] == "tone_uncertain":
                row["outcome"] = "match" if fit >= 0.72 else "tone_mismatch"
            tone_values.append(fit)

        report("scoring")
        extra = sum(row["outcome"] == "extra" for row in rows)
        reliable_count = sum(row["outcome"] not in ("uncertain", "model_disagreement", "missing", "extra") for row in rows)
        pace, pace_feedback = (naturalness(times, active, spans, pause_boundaries)
                               if spans and reliable_count * 2 >= len(reference) else (None, "rhythm_uncertain"))
        tones = criterion(tone_values, 25)
        tones["pitch_evaluated"] = measured_tones
        if measured_tones == 0 or measured_tones * 2 < recognizable_tones:
            tones["score"] = None
        scores = {
            "initials": criterion(initials, 20),
            "finals": criterion(finals, 20),
            "tones": tones,
            "syllables": criterion(syllables, 25, extra=extra),
            "naturalness": criterion([pace], 10),
        }
        disagreement = any(row["outcome"] == "model_disagreement" for row in rows)
        complete = not disagreement and all(item["score"] is not None for item in scores.values())
        total = sum(item["score"] for item in scores.values()) if complete else None
        return {"status": "model_disagreement" if disagreement else "ok", "syllables": rows,
                "scores": scores, "total_score": total, "pace_feedback": pace_feedback, **evidence}


def main():
    assessor = None
    for line in sys.stdin:
        try:
            with redirect_stdout(sys.stderr):
                request = json.loads(line)
                if request.get("output_path"):
                    result = prepare_recording(request["wav_path"], request["output_path"], request.get("playback_path"))
                else:
                    if assessor is None:
                        assessor = MandarinAssessor()
                    result = assessor.assess(request, report_stage)
            response = json.dumps(result, ensure_ascii=False)
        except Exception:
            traceback.print_exc(file=sys.stderr)
            response = json.dumps({"status": "error"})
        print(response, flush=True)


def report_stage(stage):
    print(json.dumps({"stage": stage}), file=sys.__stdout__, flush=True)


if __name__ == "__main__":
    main()
