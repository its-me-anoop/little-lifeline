#!/usr/bin/env python3
"""Compose original clinic music and action sounds; no samples or external recordings.

Deterministic, 32 kHz mono PCM. Wrapped note and drum tails make the 16-bar score loop
seamlessly. This script and its output are project-owned original compositions.
"""
import array
import hashlib
import json
import math
from pathlib import Path
import random
import wave

RATE = 32000
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicAudio"
OUT.mkdir(parents=True, exist_ok=True)
manifest = {}


def write(name, data, target_peak=None):
    peak = max(abs(v) for v in data)
    if target_peak is not None:
        data = [v * target_peak / peak for v in data]
    elif peak > .92:
        data = [v * .92 / peak for v in data]
    pcm = array.array("h", (round(max(-1, min(1, v)) * 32767) for v in data))
    path = OUT / (name + ".wav")
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())
    manifest[name] = {"seconds": len(data) / RATE, "peak": max(abs(v) for v in data),
                      "rms": math.sqrt(sum(v*v for v in data)/len(data)),
                      "seamDelta": abs(data[-1]-data[0]),
                      "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def note(data, start, duration, midi, gain, timbre="keys", wrap=False):
    f = 440 * 2 ** ((midi-69)/12)
    for i in range(int(duration * RATE)):
        t = i / RATE
        phase = 2 * math.pi * f * t
        if timbre == "bass":
            # Round plucked bass with a little bite, short and bouncy.
            tone = math.sin(phase) + .35*math.sin(2*phase)*math.exp(-t*9) + .12*math.sin(3*phase)*math.exp(-t*14)
            envelope = min(1, t/.006) * math.exp(-t*6.5) * min(1, (duration-t)/.03)
        elif timbre == "marimba":
            tone = math.sin(phase) + .45*math.sin(4*phase)*math.exp(-t*30) + .15*math.sin(10*phase)*math.exp(-t*60)
            envelope = min(1, t/.002) * math.exp(-t*7) * min(1, (duration-t)/.03)
        elif timbre == "kalimba":
            tone = math.sin(phase) + .30*math.sin(5.4*phase)*math.exp(-t*25) + .10*math.sin(2*phase)*math.exp(-t*6)
            envelope = min(1, t/.002) * math.exp(-t*4.2) * min(1, (duration-t)/.04)
        elif timbre == "bell":
            tone = math.sin(phase) + .5*math.sin(2.76*phase)*math.exp(-t*5) + .25*math.sin(5.4*phase)*math.exp(-t*9)
            envelope = min(1, t/.002) * math.exp(-t*3.2) * min(1, (duration-t)/.05)
        else:
            tone = math.sin(phase) + .23*math.sin(2*phase)*math.exp(-t*4) + .09*math.sin(3*phase)*math.exp(-t*8)
            envelope = min(1, t/.006) * math.exp(-t*2.6) * min(1, (duration-t)/.08)
        index = round(start*RATE) + i
        if wrap:
            index %= len(data)
        elif index >= len(data):
            break
        data[index] += tone * envelope * gain


drum_rng = random.Random(51123)
def drum(data, start, kind, gain):
    """Synthesised kit: a pitched-down kick, a clap from short noise bursts, a filtered shaker."""
    length = {"kick": .22, "clap": .18, "shaker": .06}[kind]
    low = 0.
    for i in range(int(length * RATE)):
        t = i / RATE
        if kind == "kick":
            f = 45 + 95 * math.exp(-t*28)
            value = math.sin(2*math.pi*f*t) * math.exp(-t*16)
        elif kind == "clap":
            burst = sum(math.exp(-max(0, t-d)*70) * (t >= d) for d in (0, .011, .023))
            noise = drum_rng.uniform(-1, 1)
            low = .6*low + .4*noise
            value = (noise - low) * (burst * .6 + math.exp(-t*22) * .5)
        else:
            noise = drum_rng.uniform(-1, 1)
            low = .3*low + .7*noise
            value = (noise - low) * math.exp(-t*55)
        data[(round(start*RATE) + i) % len(data)] += value * gain


# "Busy Day": C major, 116 BPM, a cheerful 16-bar loop. I–vi–IV–V with a bouncy bass,
# off-beat marimba chords, light kit and a kalimba tune that returns as a bell in the second half.
bpm = 116
beat = 60 / bpm
eighth = beat / 2
music = [0.] * round(16 * 4 * beat * RATE)
chords = [(48, (64, 67, 72)), (45, (64, 69, 72)), (41, (65, 69, 72)), (43, (62, 67, 71))]
melody = [
    [76, 79, 76, 74, 72, None, 74, 76], [76, 72, 69, None, 72, 76, 74, None],
    [77, 81, 77, 76, 74, None, 72, 74], [71, 74, 79, None, 77, 76, 74, None],
    [76, 79, 84, 79, 76, None, 74, 76], [76, 72, 69, 72, 76, None, 79, None],
    [77, 81, 84, 81, 77, 76, 74, None], [79, None, 76, 74, 72, None, None, None],
]
for bar in range(16):
    root, triad = chords[bar % 4]
    origin = bar * 4 * beat
    # Bass: root, root, octave, root on eighths with rests for bounce.
    for step, offset in ((0, 0), (2, 0), (3, 12), (4, 0), (6, 7), (7, 12)):
        note(music, origin + step*eighth, eighth*.9, root + offset, .16, "bass", True)
    # Off-beat chord stabs.
    for step in (1, 3, 5, 7):
        for pitch in triad:
            note(music, origin + step*eighth, .22, pitch, .030, "marimba", True)
    # Kit: kick on 1 and 3 (plus a pickup every fourth bar), clap on 2 and 4, shaker on eighths.
    for step in (0, 4) + ((7,) if bar % 4 == 3 else ()):
        drum(music, origin + step*eighth, "kick", .34)
    for step in (2, 6):
        drum(music, origin + step*eighth, "clap", .11)
    for step in range(8):
        drum(music, origin + step*eighth, "shaker", .035 if step % 2 else .022)
    # Tune: kalimba first, then bell an octave up with a soft harmony a third below.
    line = melody[bar % 8]
    second = bar >= 8
    for step, pitch in enumerate(line):
        if pitch is None:
            continue
        start = origin + step*eighth
        if second:
            note(music, start, .9, pitch + 12, .050, "bell", True)
            note(music, start, .6, pitch - 4 if pitch % 12 in (4, 11) else pitch - 3, .026, "marimba", True)
        else:
            note(music, start, .8, pitch, .085, "kalimba", True)
# A background score: its peak stays well below the effects so taps and coins cut through.
write("morning-rounds", music, target_peak=.36)

def chime(name, notes, spacing=.065, gain=.18, timbre="keys", tail=.55):
    data = [0.] * int((len(notes)*spacing+tail)*RATE)
    for i, pitch in enumerate(notes):
        note(data, i*spacing, tail, pitch, gain, timbre)
    write(name, data)

def whoosh(data, start, duration, gain):
    low = 0.
    wrng = random.Random(7771)
    for i in range(int(duration*RATE)):
        t = i / RATE
        cutoff = .04 + .5 * (t/duration)
        low = (1-cutoff)*low + cutoff*wrng.uniform(-1, 1)
        data[round(start*RATE)+i] += low * gain * math.sin(math.pi*t/duration)

chime("payment", [84, 91], .05, .16, "kalimba", .35)
# Collect: a bright ka-ching with a sparkle run.
collect = [0.] * int(.8*RATE)
note(collect, 0, .3, 88, .20, "bell"); note(collect, .06, .6, 95, .22, "bell")
for i, pitch in enumerate((96, 100, 103, 108)):
    note(collect, .14 + i*.035, .35, pitch, .05, "kalimba")
write("collect", collect)
chime("care", [72, 76, 79], .08, .14, "kalimba", .5)
chime("consultation", [67, 72, 76], .08, .12, "marimba", .45)
chime("pharmacy", [84, 79, 88], .06, .11, "kalimba", .4)
# Upgrade: rising whoosh under a quick major arpeggio.
upgrade = [0.] * int(.9*RATE)
whoosh(upgrade, 0, .35, .10)
for i, pitch in enumerate((72, 76, 79, 84, 88)):
    note(upgrade, .08 + i*.055, .5, pitch, .15, "marimba")
write("upgrade", upgrade)
# Complete: a small fanfare that lands on a chord.
complete = [0.] * int(1.05*RATE)
for i, pitch in enumerate((72, 76, 79)):
    note(complete, i*.09, .25, pitch, .14, "marimba")
for pitch in (72, 76, 79, 84):
    note(complete, .28, .76, pitch, .09, "bell")
note(complete, .28, .5, 48, .18, "bass")
write("complete", complete)
# Reward: a sparkling glissando for gems and goals.
reward = [0.] * int(1.1*RATE)
for i, pitch in enumerate((79, 83, 86, 91, 95, 98, 103)):
    note(reward, i*.045, .7, pitch, .09, "bell")
write("reward", reward)
# Tap: a soft wooden pop.
chime("tap", [84], gain=.10, timbre="marimba", tail=.12)

rng = random.Random(93217)
def noise_sound(name, duration, decay, gain, frequency):
    data, filtered = [], 0.
    for i in range(int(duration*RATE)):
        t = i/RATE
        filtered = .92*filtered + .08*rng.uniform(-1,1)
        envelope = min(1,t/.008)*math.exp(-t*decay)*min(1,(duration-t)/.02)
        data.append(gain*envelope*(filtered+.2*math.sin(2*math.pi*frequency*t)))
    write(name, data)

noise_sound("footstep", .14, 32, .28, 145)
noise_sound("door", .48, 5.5, .20, 92)
noise_sound("treatment", .24, 14, .15, 370)
noise_sound("construction", .45, 9, .35, 180)
noise_sound("taxi", .65, 3, .15, 74)
(OUT / "composition.json").write_text(json.dumps({"title":"Busy Day", "author":"Original project composition",
    "bpm":116, "bars":16, "source":"Tools/create_clinic_audio.py", "assets":manifest}, indent=2)+"\n")
print(json.dumps(manifest, indent=2))
