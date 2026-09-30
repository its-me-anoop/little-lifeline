#!/usr/bin/env python3
"""Compose the one-minute Little Lifeline story promo (1080x1920, 30 fps): from one desk to a fully equipped hospital.

    python3 Tools/compose_story_video.py WORKDIR OUT.mp4

WORKDIR holds clean simulator recordings (no interface) named in SCENES. Frames stream from ffmpeg, get a slow
push-in and a short caption, and are piped back to ffmpeg; the game's own score and sounds are mixed in afterwards.
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts"
AUDIO = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicAudio"
ICON = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/AppIcon.png"
W, H, FPS, DURATION = 1080, 1920, 30, 60
SRC_W, SRC_H = 1080, 2346            # 1320x2868 recordings scaled to the frame width
FIELD, INK, SUB = (247, 178, 92), (31, 55, 46), (74, 58, 34)
FADE = 0.7

work, out = Path(sys.argv[1]), Path(sys.argv[2])
display = lambda size: ImageFont.truetype(str(FONTS / "ClinicDisplay.ttf"), size)
body = lambda size: ImageFont.truetype(str(FONTS / "LifelineBody.ttf"), size)

# (start, end, clip, caption, push-in centre y as a share of the recording, zoom from, zoom to)
SCENES = [
    (0, 7, "s1_empty", "It starts with one desk.", .52, 1.00, 1.08),
    (7, 14, "s2_first", "Your first nurse.\nYour first patient.", .50, 1.02, 1.10),
    (14, 22, "s3_grow", "Every visit helps you grow.", .45, 1.00, 1.07),
    (22, 30, "s4_busy", "Rooms get bigger.\nEquipment gets better.", .45, 1.00, 1.08),
    (30, 38, "s5_care", "Every piece, upgraded\nwith care.", .50, 1.00, 1.06),
    (38, 47, "s6_full", "Until it's a busy,\nfully equipped hospital.", .45, 1.10, 1.00),
    (47, 55, "s7_wide", "All built by you.", .45, 1.00, 1.10),
]
END = 55


def ease(t):
    t = min(1.0, max(0.0, t))
    return 1 - (1 - t) ** 3


class Clip:
    """Reads a recording's frames in order, starting a second in, at the frame width."""
    def __init__(self, name):
        cmd = ["ffmpeg", "-v", "error", "-ss", "1.0", "-i", str(work / f"{name}.mov"), "-r", str(FPS),
               "-vf", f"scale={SRC_W}:{SRC_H}", "-f", "rawvideo", "-pix_fmt", "rgb24", "-"]
        self.proc = subprocess.Popen(cmd, stdout=subprocess.PIPE)
        self.size = SRC_W * SRC_H * 3
        self.last = None

    def next(self):
        data = self.proc.stdout.read(self.size)
        if len(data) == self.size:
            self.last = Image.frombytes("RGB", (SRC_W, SRC_H), data)
        return self.last

    def close(self):
        self.proc.kill()


def push_in(frame, t, span, cy, z0, z1):
    z = z0 + (z1 - z0) * ease(t / span) if z1 > z0 else z0 + (z1 - z0) * (t / span)
    cw, ch = W / z, H / z
    top = 150                          # keep the camera cutout out of frame
    y0 = min(max(top, cy * SRC_H - ch / 2), SRC_H - ch)
    x0 = (SRC_W - cw) / 2
    return frame.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize((W, H), Image.BICUBIC)


SCRIM = Image.new("L", (W, 620))
for y in range(620):
    SCRIM.paste(int(215 * (y / 620) ** 1.6), (0, y, W, y + 1))


def caption(img, text, t, span):
    img.paste(Image.new("RGB", (W, 620), (22, 36, 30)), (0, H - 620), SCRIM)
    k_in = ease((t - .35) / .8)
    k_out = 1 - ease((t - (span - .7)) / .5)
    k = min(k_in, k_out)
    if k <= 0:
        return
    layer = Image.new("RGBA", (W, 400), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    f = display(84)
    lines = text.split("\n")
    y = 400 - len(lines) * 100 - 40
    for line in lines:
        tw = d.textlength(line, font=f)
        d.text(((W - tw) / 2 + 3, y + 4), line, font=f, fill=(0, 0, 0, 90))
        d.text(((W - tw) / 2, y), line, font=f, fill=(255, 250, 238, 255))
        y += 100
    layer.putalpha(layer.getchannel("A").point(lambda a: int(a * k)))
    img.paste(layer, (0, int(H - 430 + (1 - k_in) * 40)), layer)


def end_card(t, backdrop):
    img = backdrop.filter(ImageFilter.GaussianBlur(18))
    wash = Image.new("RGB", (W, H), FIELD)
    img = Image.blend(img, wash, .82)
    d = ImageDraw.Draw(img)
    k = ease(t / 1.0)
    icon = Image.open(ICON).convert("RGBA").resize((320, 320), Image.LANCZOS)
    mask = Image.new("L", (320, 320), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, 319, 319), 72, fill=int(255 * k))
    img.paste(icon, ((W - 320) // 2, int(560 + (1 - k) * 50)), mask)
    for text, font, y, fill, delay in (("Little Lifeline", display(128), 950, INK, .2), ("Build your little clinic", body(58), 1115, SUB, .45),
                                        ("Free on the App Store", display(62), 1300, INK, .8)):
        kk = ease((t - delay) / .8)
        layer = Image.new("RGBA", (W, 170), (0, 0, 0, 0))
        ImageDraw.Draw(layer).text(((W - d.textlength(text, font=font)) / 2, 10), text, font=font, fill=fill + (255,))
        layer.putalpha(layer.getchannel("A").point(lambda a: int(a * kk)))
        img.paste(layer, (0, int(y + (1 - kk) * 30)), layer)
    return img


def render_all(sink):
    clips = {}
    last_frame = None
    for n in range(FPS * DURATION):
        t = n / FPS
        if t >= END:
            frame = end_card(t - END, last_frame)
            if t < END + FADE:
                frame = Image.blend(last_frame, frame, ease((t - END) / FADE))
            if t > DURATION - .6:
                frame = Image.blend(frame, Image.new("RGB", (W, H), FIELD), (t - (DURATION - .6)) / .6)
        else:
            idx = next(i for i, s in enumerate(SCENES) if s[0] <= t < s[1])
            a, b, name, text, cy, z0, z1 = SCENES[idx]
            if name not in clips:
                clips[name] = Clip(name)
            frame = push_in(clips[name].next(), t - a, b - a, cy, z0, z1)
            caption(frame, text, t - a, b - a)
            # Cross-dissolve into the next scene over its first FADE seconds.
            if idx + 1 < len(SCENES) and t > b - FADE:
                a2, b2, name2, text2, cy2, z02, z12 = SCENES[idx + 1]
                if name2 not in clips:
                    clips[name2] = Clip(name2)
                nxt = push_in(clips[name2].next(), 0, b2 - a2, cy2, z02, z12)
                frame = Image.blend(frame, nxt, ease((t - (b - FADE)) / FADE))
            if idx > 0 and t < a:
                pass
            if t < .5:
                frame = Image.blend(Image.new("RGB", (W, H), (0, 0, 0)), frame, t / .5)
            last_frame = frame
        sink.write(frame.tobytes())
        if n % 150 == 0:
            print("frame", n, flush=True)
    for c in clips.values():
        c.close()


silent = out.with_suffix(".silent.mp4")
enc = subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                        "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", str(silent)], stdin=subprocess.PIPE)
render_all(enc.stdin)
enc.stdin.close()
enc.wait()

# The game's score, cross-faded into itself to fill the minute, with a few of its sounds on the story beats.
cues = [(1.2, "door"), (9.0, "collect"), (16.0, "payment"), (24.5, "upgrade"), (32.0, "upgrade"), (40.5, "complete"), (55.4, "reward")]
inputs = ["-i", str(AUDIO / "morning-rounds.wav"), "-i", str(AUDIO / "morning-rounds.wav")]
graph = ["[1:a][2:a]acrossfade=d=4[m]", "[m]atrim=0:60,afade=t=in:d=2,afade=t=out:st=56:d=4,volume=0.9[mus]"]
labels = ["[mus]"]
for i, (at, name) in enumerate(cues):
    inputs += ["-i", str(AUDIO / f"{name}.wav")]
    ms = int(at * 1000)
    graph.append(f"[{i + 3}:a]adelay={ms}|{ms},volume=0.7[s{i}]")
    labels.append(f"[s{i}]")
graph.append("".join(labels) + f"amix=inputs={len(labels)}:normalize=0[a]")
subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(silent), *inputs, "-filter_complex", ";".join(graph), "-map", "0:v", "-map", "[a]",
                "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-t", str(DURATION), str(out)], check=True)
silent.unlink()
print(out)
