"""Generate the original audio assets used by Wogua Shooter.

The generator intentionally uses only numpy and the Python standard library so
the project can recreate its audio without a DAW or an external package.
"""

from pathlib import Path
import math
import wave

import numpy as np


SAMPLE_RATE = 44100
TAU = math.tau


def midi_to_hz(note):
    return 440.0 * (2.0 ** ((note - 69.0) / 12.0))


def write_wav(path, samples):
    samples = np.asarray(samples, dtype=np.float32)
    if samples.ndim == 1:
        samples = samples[:, None]
    peak = float(np.max(np.abs(samples))) if samples.size else 0.0
    if peak > 0.98:
        samples = samples * (0.98 / peak)
    pcm = np.clip(samples, -1.0, 1.0)
    pcm = (pcm * 32767.0).astype(np.int16)
    with wave.open(str(path), "wb") as output:
        output.setnchannels(samples.shape[1])
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm.tobytes())


def envelope(length, attack, release):
    result = np.ones(length, dtype=np.float32)
    attack_samples = min(length, max(1, int(attack * SAMPLE_RATE)))
    release_samples = min(length, max(1, int(release * SAMPLE_RATE)))
    result[:attack_samples] = np.linspace(0.0, 1.0, attack_samples, endpoint=False)
    result[-release_samples:] *= np.linspace(1.0, 0.0, release_samples)
    return result


def add_note(left, right, start, duration, frequency, amplitude, waveform="sine", pan=0.0):
    start_sample = int(start * SAMPLE_RATE)
    length = min(len(left) - start_sample, max(1, int(duration * SAMPLE_RATE)))
    if length <= 0:
        return
    t = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    phase = TAU * frequency * t
    if waveform == "triangle":
        tone = (2.0 / math.pi) * np.arcsin(np.sin(phase))
    elif waveform == "square":
        tone = np.sign(np.sin(phase))
    elif waveform == "saw":
        tone = 2.0 * ((frequency * t) % 1.0) - 1.0
    else:
        tone = np.sin(phase)
    tone = tone * envelope(length, min(0.04, duration * 0.2), min(0.2, duration * 0.45))
    # Constant-power pan keeps the center image stable while adding movement.
    angle = (pan + 1.0) * math.pi * 0.25
    left[start_sample:start_sample + length] += tone * amplitude * math.cos(angle)
    right[start_sample:start_sample + length] += tone * amplitude * math.sin(angle)


def add_kick(left, right, start, amplitude=0.32):
    start_sample = int(start * SAMPLE_RATE)
    length = min(len(left) - start_sample, int(0.32 * SAMPLE_RATE))
    if length <= 0:
        return
    t = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    frequency = 145.0 * np.exp(-t * 11.0) + 46.0
    phase = TAU * np.cumsum(frequency) / SAMPLE_RATE
    tone = np.sin(phase) * np.exp(-t * 15.0)
    left[start_sample:start_sample + length] += tone * amplitude
    right[start_sample:start_sample + length] += tone * amplitude * 0.92


def add_snare(left, right, start, amplitude=0.18):
    start_sample = int(start * SAMPLE_RATE)
    length = min(len(left) - start_sample, int(0.22 * SAMPLE_RATE))
    if length <= 0:
        return
    t = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    rng = np.random.default_rng(int(start * 1000) + 17)
    noise = rng.normal(0.0, 1.0, length).astype(np.float32)
    noise = noise - np.concatenate(([0.0], noise[:-1])) * 0.65
    snap = np.sin(TAU * 185.0 * t) * np.exp(-t * 24.0)
    sound = (noise * 0.8 + snap * 0.35) * np.exp(-t * 20.0) * amplitude
    left[start_sample:start_sample + length] += sound * 0.92
    right[start_sample:start_sample + length] += sound


def add_hat(left, right, start, amplitude=0.055):
    start_sample = int(start * SAMPLE_RATE)
    length = min(len(left) - start_sample, int(0.07 * SAMPLE_RATE))
    if length <= 0:
        return
    rng = np.random.default_rng(int(start * 10000) + 31)
    t = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    noise = rng.normal(0.0, 1.0, length).astype(np.float32)
    noise = noise - np.concatenate(([0.0], noise[:-1])) * 0.96
    sound = noise * np.exp(-t * 78.0) * amplitude
    left[start_sample:start_sample + length] += sound * 0.76
    right[start_sample:start_sample + length] += sound


def make_bgm():
    bpm = 112.0
    beat = 60.0 / bpm
    duration = beat * 64.0
    total = int(duration * SAMPLE_RATE)
    left = np.zeros(total, dtype=np.float32)
    right = np.zeros(total, dtype=np.float32)

    # A four-chord minor-pentatonic loop gives the game a playful action pulse.
    chord_roots = [48, 44, 51, 46]  # C, Ab, Eb, Bb
    scale = [0, 3, 5, 7, 10, 12]
    for bar in range(16):
        bar_start = bar * beat * 4.0
        root = chord_roots[(bar // 4) % len(chord_roots)]
        for interval, pan in zip((0, 3, 7), (-0.22, 0.0, 0.22)):
            add_note(left, right, bar_start, beat * 4.25, midi_to_hz(root + 12 + interval), 0.055, "triangle", pan)
        for step in range(4):
            bass_note = root + (0 if step in (0, 2) else 7)
            add_note(left, right, bar_start + step * beat, beat * 0.82, midi_to_hz(bass_note), 0.13, "saw", -0.08)
        melody = [0, 2, 3, 5, 3, 2, 0, 3] if bar % 2 == 0 else [3, 5, 4, 2, 0, 2, 3, 5]
        for step, degree in enumerate(melody):
            if bar % 4 == 3 and step in (1, 5):
                continue
            note = root + 24 + scale[degree]
            add_note(left, right, bar_start + step * beat * 0.5, beat * 0.34, midi_to_hz(note), 0.075, "triangle", 0.18 if step % 2 else -0.18)

    for step in range(64):
        start = step * beat
        beat_in_bar = step % 4
        add_kick(left, right, start, 0.27 if beat_in_bar == 0 else 0.21)
        if beat_in_bar in (1, 3):
            add_snare(left, right, start, 0.14)
        add_hat(left, right, start + beat * 0.5, 0.045)
        if beat_in_bar == 3:
            add_hat(left, right, start + beat * 0.75, 0.035)

    # Make the loop boundary quiet so Unity can repeat it without a click.
    fade = int(0.025 * SAMPLE_RATE)
    left[:fade] *= np.linspace(0.0, 1.0, fade)
    right[:fade] *= np.linspace(0.0, 1.0, fade)
    left[-fade:] *= np.linspace(1.0, 0.0, fade)
    right[-fade:] *= np.linspace(1.0, 0.0, fade)
    return np.column_stack((left, right))


def make_sfx(kind):
    durations = {
        "pea": 0.14,
        "corn": 0.3,
        "hit": 0.22,
        "explosion": 0.62,
    }
    duration = durations[kind]
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    rng = np.random.default_rng({"pea": 41, "corn": 43, "hit": 47, "explosion": 53}[kind])
    noise = rng.normal(0.0, 1.0, length).astype(np.float32)
    if kind == "pea":
        chirp = np.sin(TAU * (930.0 * t - 3450.0 * t * t)) * np.exp(-t * 23.0)
        click = noise * np.exp(-t * 75.0) * 0.16
        sound = chirp * 0.65 + click
    elif kind == "corn":
        sweep = np.sin(TAU * (220.0 * t - 500.0 * t * t)) * np.exp(-t * 9.0)
        crack = noise * np.exp(-t * 29.0) * 0.22
        sound = sweep * 0.72 + crack
    elif kind == "hit":
        impact = np.sin(TAU * (720.0 * t - 1350.0 * t * t)) * np.exp(-t * 20.0)
        metal = np.sin(TAU * 1680.0 * t) * np.exp(-t * 34.0) * 0.28
        sound = impact * 0.64 + metal + noise * np.exp(-t * 38.0) * 0.12
    else:
        boom = np.sin(TAU * (115.0 * t - 60.0 * t * t)) * np.exp(-t * 5.6)
        rumble = np.sin(TAU * 48.0 * t) * np.exp(-t * 4.4) * 0.43
        dust = noise * np.exp(-t * 7.3) * 0.42
        sound = boom * 0.66 + rumble + dust
    sound *= envelope(length, 0.001, min(0.12, duration * 0.45))
    return sound


def main():
    root = Path(__file__).resolve().parents[1]
    output = root / "Assets" / "Resources" / "Audio"
    output.mkdir(parents=True, exist_ok=True)
    write_wav(output / "WoguaAdventure_BGM.wav", make_bgm())
    write_wav(output / "SFX_PeaShot.wav", make_sfx("pea"))
    write_wav(output / "SFX_CornShot.wav", make_sfx("corn"))
    write_wav(output / "SFX_EnemyHit.wav", make_sfx("hit"))
    write_wav(output / "SFX_CornExplosion.wav", make_sfx("explosion"))
    for path in sorted(output.glob("*.wav")):
        with wave.open(str(path), "rb") as audio:
            print(f"{path.name}: {audio.getnchannels()}ch {audio.getframerate()}Hz {audio.getnframes() / audio.getframerate():.2f}s")


if __name__ == "__main__":
    main()
