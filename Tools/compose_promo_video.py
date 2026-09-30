#!/usr/bin/env python3
"""Compose the one-minute Little Lifeline promo (1080x1920, 30 fps) from simulator captures.

    python3 Tools/compose_promo_video.py WORKDIR OUT.mp4

WORKDIR holds clipA.mov, clipB.mov (screen recordings), gear1/4/7/10.png, and the stills named below. Frames are
composited in Python and piped to ffmpeg; the game's own score and sound effects are mixed in afterwards.
"""
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts"
AUDIO = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicAudio"
W, H, FPS, DURATION = 1080, 1920, 30, 60
FIELD, INK, SUB, EDGE = (247, 178, 92), (31, 55, 46), (74, 58, 34), (214, 138, 58)
PHONE_W, PHONE_H = 610, 1325          # 1320x2868 screen scaled
PHONE_X, PHONE_Y = (W - PHONE_W) // 2, 500

work = Path(sys.argv[1])
out = Path(sys.argv[2])
title_font = lambda size: ImageFont.truetype(str(FONTS / "ClinicDisplay.ttf"), size)
body_font = lambda size: ImageFont.truetype(str(FONTS / "LifelineBody.ttf"), size)


def ease(t):
    t = min(1, max(0, t))
    return 1 - (1 - t) ** 4


def smooth(t):
    t = min(1, max(0, t))
    return t * t * (3 - 2 * t)


def load_clip(path, seconds):
    cmd = ["ffmpeg", "-v", "error", "-ss", "1.0", "-i", str(path), "-t", str(seconds), "-r", str(FPS),
           "-vf", f"scale={PHONE_W}:{PHONE_H}", "-f", "rawvideo", "-pix_fmt", "rgb24", "-"]
    data = subprocess.run(cmd, capture_output=True, check=True).stdout
    frame = PHONE_W * PHONE_H * 3
    return [Image.frombytes("RGB", (PHONE_W, PHONE_H), data[i:i + frame]) for i in range(0, len(data) - frame + 1, frame)]


def still(path):
    return Image.open(path).convert("RGB").resize((PHONE_W, PHONE_H), Image.LANCZOS)


clipA = load_clip(work / "clipA.mov", 12)
clipB = load_clip(work / "clipB.mov", 11)
gear = {v: still(work / f"gear{v}.png") for v in (1, 4, 7, 10)}
panel = still(work / "raw-iphone-firstaid.png")
goals = still(work / "raw-iphone-goals.png")
doctors = [Image.open(work / n).convert("RGB") for n in ("doctors.png", "doctors-consultation.png", "doctors-pharmacy.png")]

MASK = Image.new("L", (PHONE_W, PHONE_H), 0)
ImageDraw.Draw(MASK).rounded_rectangle((0, 0, PHONE_W - 1, PHONE_H - 1), 62, fill=255)

# Background: warm field with slow drifting soft discs.
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)


def background(t):
    base = np.zeros((H, W, 3), np.float32) + np.array(FIELD, np.float32)
    for i, (cx, cy, r, amp) in enumerate(((.2, .25, 700, 22), (.85, .6, 800, -18), (.4, .95, 650, 16))):
        x = cx * W + 90 * math.sin(t * .35 + i * 2)
        y = cy * H + 70 * math.cos(t * .3 + i)
        d = np.exp(-(((xx - x) ** 2 + (yy - y) ** 2) / (2 * (r * .5) ** 2)))
        base += (d * amp)[..., None]
    return Image.fromarray(np.clip(base, 0, 255).astype(np.uint8))


def wrap(draw, text, font, width):
    lines, line = [], ""
    for word in text.split():
        trial = (line + " " + word).strip()
        if draw.textlength(trial, font=font) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = word
    lines.append(line)
    return lines


def headline(img, text, sub, t, size=80):
    """Slide-and-fade the headline in during the first 0.7 s."""
    layer = Image.new("RGBA", (W, 520), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    y = 0
    for line in wrap(d, text, title_font(size), W - 170):
        d.text((85, y), line, font=title_font(size), fill=INK + (255,))
        y += int(size * 1.12)
    y += 22
    for line in wrap(d, sub, body_font(44), W - 170):
        d.text((85, y), line, font=body_font(44), fill=SUB + (255,))
        y += 60
    k = ease(t / 0.7)
    layer.putalpha(layer.getchannel("A").point(lambda a: int(a * k)))
    img.paste(layer, (0, int(105 + (1 - k) * 50)), layer)


def phone(img, content, t, lift=0.0, scale=1.0):
    """Paste a rounded phone screen with an edge and soft shadow. `lift` in [0,1] slides it up from below."""
    k = ease(t / 0.9)
    dy = int((1 - k) * 260 + lift)
    w, h = int(PHONE_W * scale), int(PHONE_H * scale)
    screen = content.resize((w, h), Image.LANCZOS) if scale != 1 else content
    mask = MASK.resize((w, h)) if scale != 1 else MASK
    x, y = (W - w) // 2, PHONE_Y + dy - (h - PHONE_H) // 2
    shadow = Image.new("RGBA", (w + 200, h + 200), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((100, 120, 100 + w, 120 + h), 70, fill=(60, 35, 10, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(34))
    img.paste(shadow, (x - 100, y - 100), shadow)
    edge = Image.new("RGBA", (w + 24, h + 24), (0, 0, 0, 0))
    ImageDraw.Draw(edge).rounded_rectangle((0, 0, w + 23, h + 23), 74, fill=EDGE + (255,))
    img.paste(edge, (x - 12, y - 12), edge)
    img.paste(screen, (x, y), mask)


def chip(img, text, x, y, t, fill=(255, 251, 240), ink=INK, size=44, delay=0.0):
    """A pill label that pops in."""
    k = ease((t - delay) / 0.5)
    if k <= 0:
        return
    f = title_font(size)
    d0 = ImageDraw.Draw(img)
    tw = int(d0.textlength(text, font=f))
    pw, ph = tw + 64, size + 40
    layer = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle((0, 0, pw - 1, ph - 1), ph // 2, fill=fill + (255,))
    d.text((32, 16), text, font=f, fill=ink + (255,))
    s = .7 + .3 * k
    layer = layer.resize((int(pw * s), int(ph * s)), Image.LANCZOS)
    layer.putalpha(layer.getchannel("A").point(lambda a: int(a * k)))
    img.paste(layer, (x - layer.width // 2, y - layer.height // 2), layer)


def frame_of(clip, t):
    return clip[min(len(clip) - 1, int(t * FPS))]


def crossfade_stills(images, t, span):
    n = len(images)
    pos = min(n - 1e-6, t / span * n)
    i = int(pos)
    frac = smooth((pos - i - .75) / .25) if i < n - 1 else 0
    a = images[i]
    return Image.blend(a, images[min(n - 1, i + 1)], frac) if frac > 0 else a


def kenburns(image, t, span, box=(PHONE_W, PHONE_H)):
    k = t / span
    z = 1.0 + .16 * k
    iw, ih = image.size
    ch = ih / z
    cw = ch * box[0] / box[1]
    if cw > iw:
        cw = iw / z; ch = cw * box[1] / box[0]
    cx = iw * (.35 + .3 * k)
    cy = ih * (.55 - .05 * k)
    x0 = min(max(0, cx - cw / 2), iw - cw)
    y0 = min(max(0, cy - ch / 2), ih - ch)
    return image.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize(box, Image.LANCZOS)


# ---- scenes -------------------------------------------------------------------------------------------------
def s_title(t):
    img = background(t + 0)
    d = ImageDraw.Draw(img)
    k = ease(t / 1.0)
    f = title_font(150)
    text = "Little Lifeline"
    tw = d.textlength(text, font=f)
    layer = Image.new("RGBA", (W, 260), (0, 0, 0, 0))
    ImageDraw.Draw(layer).text(((W - tw) / 2, 20), text, font=f, fill=INK + (255,))
    layer.putalpha(layer.getchannel("A").point(lambda a: int(a * k)))
    img.paste(layer, (0, int(120 + (1 - k) * 60)), layer)
    sub = "Build your little clinic"
    fk = ease((t - .4) / .8)
    sl = Image.new("RGBA", (W, 90), (0, 0, 0, 0))
    ImageDraw.Draw(sl).text(((W - d.textlength(sub, font=body_font(54))) / 2, 10), sub, font=body_font(54), fill=SUB + (255,))
    sl.putalpha(sl.getchannel("A").point(lambda a: int(a * fk)))
    img.paste(sl, (0, 330), sl)
    phone(img, frame_of(clipA, t), t)
    return img


def s_visit(t):
    img = background(t + 5)
    headline(img, "Check in. Treat. Send them home well.", "Watch every visit, and collect what they leave.", t)
    phone(img, frame_of(clipA, t + 5), t)
    chip(img, "+50 coins", 300, 1000, t, delay=2.4)
    chip(img, "Patient treated", 780, 1785, t, delay=5.5)
    return img


def s_twenty(t):
    img = background(t + 16)
    headline(img, "Twenty pieces in every room", "Each renovation unlocks a new one to upgrade.", t)
    zoom = 1 + .05 * smooth(t / 10)
    phone(img, panel, t, scale=zoom)
    chip(img, "7 of 20 pieces", 300, 1785, t, delay=1.0)
    chip(img, "Ten versions each", 780, 1785, t, delay=2.5)
    return img


def s_versions(t):
    img = background(t + 26)
    headline(img, "Ten versions of every piece", "From basic to advanced, and you can see every upgrade.", t)
    phone(img, crossfade_stills([gear[1], gear[4], gear[7], gear[10]], t, 11), t)
    names = ["Basic", "Professional", "Elite", "Advanced"]
    i = min(3, int(t / 11 * 4))
    chip(img, names[i], 540, 1795, t - i * 11 / 4 + .5, delay=0.0, fill=(255, 251, 240))
    # progress pips
    d = ImageDraw.Draw(img)
    filled = int(1 + 9 * min(1, t / 10))
    for p in range(10):
        x0 = 340 + p * 40
        d.rounded_rectangle((x0, 1850, x0 + 28, 1868), 9, fill=INK if p < filled else (232, 196, 140))
    return img


def s_doctors(t):
    img = background(t + 38)
    headline(img, "Grow into a doctors clinic", "Consultations, a pharmacy and taxis, all paying their way.", t)
    which = min(2, int(t / 8 * 3))
    stills = [kenburns(doctors[j], t, 8, (PHONE_W, PHONE_H)) for j in (which, min(2, which + 1))]
    frac = smooth((t / 8 * 3 - which - .7) / .3) if which < 2 else 0
    content = Image.blend(stills[0], stills[1], frac) if frac > 0 else stills[0]
    phone(img, content, t)
    chip(img, "+120 fares", 780, 1785, t, delay=3.2)
    return img


def s_rooms(t):
    img = background(t + 46)
    headline(img, "Bigger rooms. Busier days.", "Roomy waiting areas, and visitors queue inside.", t)
    phone(img, frame_of(clipB, t), t)
    return img


def s_goals(t):
    img = background(t + 53)
    headline(img, "New goals every day", "Milestones and streaks pay out free gems.", t)
    phone(img, goals, t)
    chip(img, "+5 gems", 540, 1785, t, delay=1.2, fill=(179, 60, 100), ink=(255, 244, 248))
    return img


def s_end(t):
    img = background(t + 57)
    d = ImageDraw.Draw(img)
    k = ease(t / .9)
    icon = Image.open(ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/AppIcon.png").convert("RGBA") if (ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/AppIcon.png").exists() else None
    y = 560
    if icon:
        icon = icon.resize((300, 300), Image.LANCZOS)
        m = Image.new("L", (300, 300), 0)
        ImageDraw.Draw(m).rounded_rectangle((0, 0, 299, 299), 66, fill=255)
        img.paste(icon, ((W - 300) // 2, int(y + (1 - k) * 60)), m)
        y += 380
    for text, font, dy, fill in (("Little Lifeline", title_font(130), 0, INK), ("Free on the App Store", body_font(60), 170, SUB), ("No ads. Play offline.", body_font(48), 250, SUB)):
        layer = Image.new("RGBA", (W, 200), (0, 0, 0, 0))
        ImageDraw.Draw(layer).text(((W - d.textlength(text, font=font)) / 2, 10), text, font=font, fill=fill + (255,))
        layer.putalpha(layer.getchannel("A").point(lambda a: int(a * k)))
        img.paste(layer, (0, int(y + dy + (1 - k) * 40)), layer)
    return img


SCENES = [(0, 5, s_title), (5, 16, s_visit), (16, 26, s_twenty), (26, 38, s_versions), (38, 46, s_doctors), (46, 53, s_rooms), (53, 57, s_goals), (57, 60, s_end)]
FADE = .45


def render(frame_index):
    t = frame_index / FPS
    for i, (a, b, fn) in enumerate(SCENES):
        if a <= t < b:
            img = fn(t - a)
            if i + 1 < len(SCENES) and t > b - FADE:
                nxt = SCENES[i + 1][2](0.0 + (t - (b - FADE)) * 0.0)
                img = Image.blend(img, nxt, smooth((t - (b - FADE)) / FADE))
            if i == 0 and t < .3:
                img = Image.blend(Image.new("RGB", (W, H), FIELD), img, smooth(t / .3))
            if t > DURATION - .5:
                img = Image.blend(img, Image.new("RGB", (W, H), FIELD), smooth((t - (DURATION - .5)) / .5))
            return img
    return Image.new("RGB", (W, H), FIELD)


silent = out.with_suffix(".silent.mp4")
if silent.exists() and '--reuse' in sys.argv:
    print("reusing", silent)
else:
  enc = subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                        "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(silent)], stdin=subprocess.PIPE)
  for n in range(FPS * DURATION):
    enc.stdin.write(render(n).tobytes())
    if n % 150 == 0:
        print("frame", n, flush=True)
  enc.stdin.close()
  enc.wait()

# Audio: the game's score looped with a cross-fade, plus a few of its sounds at scene changes.
cues = [(5.6, "collect"), (16.5, "upgrade"), (26.5, "reward"), (30.8, "upgrade"), (34.6, "upgrade"), (38.5, "door"), (46.5, "payment"), (53.5, "reward"), (57.2, "complete")]
inputs = ["-i", str(AUDIO / "morning-rounds.wav"), "-i", str(AUDIO / "morning-rounds.wav")]
graph = ["[1:a][2:a]acrossfade=d=3[m]", "[m]atrim=0:60,afade=t=in:d=1.5,afade=t=out:st=56.5:d=3.5,volume=0.85[mus]"]
labels = ["[mus]"]
for i, (at, name) in enumerate(cues):
    inputs += ["-i", str(AUDIO / f"{name}.wav")]
    graph.append(f"[{i + 3}:a]adelay={int(at * 1000)}|{int(at * 1000)},volume=0.9[s{i}]")
    labels.append(f"[s{i}]")
graph.append("".join(labels) + f"amix=inputs={len(labels)}:normalize=0[a]")
subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(silent), *inputs, "-filter_complex", ";".join(graph), "-map", "0:v", "-map", "[a]",
                "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-t", str(DURATION), str(out)], check=True)
silent.unlink()
print(out)
