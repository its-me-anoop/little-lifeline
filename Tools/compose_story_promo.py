#!/usr/bin/env python3
"""Compose the one-minute Little Lifeline story promo from the rendered chapters (Tools/render_story_scenes.py).

    python3 Tools/compose_story_promo.py RENDERS OUT.mp4

Adds numbered chapter labels, headlines, a coin counter with flying coins, a circle wipe into the closing line-up,
and the end card, then mixes the game's own score and sounds.
"""
import math
import random
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts"
AUDIO = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicAudio"
ICON = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/AppIcon.png"
W, H, FPS, DURATION = 1080, 1920, 30, 60
CREAM, INK, SUBTLE, TERRA, FOREST = (238, 239, 223), (38, 62, 50), (112, 124, 108), (206, 110, 72), (46, 74, 60)
GOLD, GOLD_DARK, GOLD_LIGHT, LIGHT_INK = (236, 184, 64), (196, 140, 36), (255, 226, 140), (242, 240, 226)

renders, out = Path(sys.argv[1]), Path(sys.argv[2])
display = lambda s: ImageFont.truetype(str(FONTS / "ClinicDisplay.ttf"), s)
body = lambda s: ImageFont.truetype(str(FONTS / "LifelineBody.ttf"), s)

# (chapter render, start second, frames) laid end to end.
SHOTS = [("reception", 0), ("firstaid", 13), ("equipment", 21), ("upgrades", 30), ("waiting", 38), ("hospital", 45), ("lineup", 54)]
END = 57
# (start, end, number, label, headline, dark)
CAPTIONS = [
    (0, 6.2, "01", "LITTLE LIFELINE", "It all starts with\none desk.", False),
    (6.2, 13, "02", "RECEPTION", "Welcome your\nfirst patient.", False),
    (13, 21, "03", "FIRST AID", "Hire your\nfirst nurse.", False),
    (21, 30, "04", "EQUIPMENT", "Fill every room\nwith care.", False),
    (30, 38, "05", "UPGRADES", "From basic\nto brilliant.", False),
    (38, 45, "06", "WAITING ROOM", "Make room for\neveryone.", False),
    (45, 54, "07", "YOUR HOSPITAL", "Grow a fully\nequipped hospital.", False),
    (54, 57, "08", "BUILT BY YOU", "Staffed, equipped\nand all yours.", True),
]
# Coin balance keyframes (second, coins) and payments that fly a coin to the counter (second, start x, start y, label).
BALANCE = [(0, 0), (5.2, 0), (5.9, 50), (15.5, 50), (16.2, 150), (22, 150), (22.6, 90), (25.5, 90), (26.2, 240), (34, 240),
           (34.6, 420), (40.5, 420), (41.2, 610), (46, 610), (53.5, 12480)]
PAYMENTS = [(5.0, 600, 1180, "+50"), (15.4, 560, 1150, "+100"), (25.4, 520, 1200, "+150"), (33.8, 540, 1220, "+180"), (40.4, 520, 1250, "+190")]


def ease(t):
    t = min(1.0, max(0.0, t))
    return 1 - (1 - t) ** 3


def smooth(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def balance(t):
    for (a, va), (b, vb) in zip(BALANCE, BALANCE[1:]):
        if a <= t <= b:
            return int(va + (vb - va) * smooth((t - a) / (b - a)))
    return BALANCE[-1][1]


def coin(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((0, 0, size - 1, size - 1), fill=GOLD_DARK + (255,))
    d.ellipse((size * .06, size * .06, size * .94, size * .88), fill=GOLD + (255,))
    d.ellipse((size * .22, size * .2, size * .78, size * .74), outline=GOLD_LIGHT + (255,), width=max(2, size // 14))
    c, a = size / 2, size * .14
    d.line((c - a, size * .47, c + a, size * .47), fill=GOLD_LIGHT + (255,), width=max(2, size // 12))
    d.line((c, size * .47 - a, c, size * .47 + a), fill=GOLD_LIGHT + (255,), width=max(2, size // 12))
    return img


COIN_SMALL, COIN_MID = coin(44), coin(56)


def fade(layer, k):
    if k >= 1:
        return layer
    layer = layer.copy()
    layer.putalpha(layer.getchannel("A").point(lambda a: int(a * max(0, k))))
    return layer


def text_block(number, label, headline, dark):
    """Eyebrow pill + spaced label + two-line headline, pre-rendered."""
    layer = Image.new("RGBA", (W, 420), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    pill_fill = (236, 196, 110) if dark else TERRA
    d.rounded_rectangle((80, 12, 136, 44), 16, fill=pill_fill + (255,))
    f = display(24)
    d.text((108 - d.textlength(number, font=f) / 2, 14), number, font=f, fill=(FOREST if dark else LIGHT_INK) + (255,))
    x = 156
    for ch in label:
        d.text((x, 12), ch, font=display(27), fill=((236, 196, 110) if dark else TERRA) + (255,))
        x += d.textlength(ch, font=display(27)) + 5
    y = 90
    for line in headline.split("\n"):
        d.text((78, y), line, font=display(98), fill=(LIGHT_INK if dark else INK) + (255,))
        y += 112
    return layer


BLOCKS = [text_block(n, l, h, dk) for _, _, n, l, h, dk in CAPTIONS]


def counter(value, dark=False):
    pill = Image.new("RGBA", (330, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(pill)
    d.rounded_rectangle((6, 10, 324, 90), 40, fill=(252, 252, 244, 255))
    pill.paste(COIN_MID, (24, 22), COIN_MID)
    d.text((96, 22), f"{value:,}", font=display(46), fill=INK + (255,))
    label = "COINS"
    x = 312 - d.textlength(label, font=display(20)) - 20
    for ch in label:
        d.text((x, 42), ch, font=display(20), fill=SUBTLE + (255,))
        x += d.textlength(ch, font=display(20)) + 3
    return pill


def chip(text, dark=False):
    f = display(34)
    w = int(ImageDraw.Draw(Image.new("RGB", (1, 1))).textlength(text, font=f)) + 104
    img = Image.new("RGBA", (w, 66), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, 65), 33, fill=(252, 252, 244, 255))
    img.paste(COIN_SMALL, (14, 11), COIN_SMALL)
    d.text((68, 12), text, font=f, fill=INK + (255,))
    return img


random.seed(4)
DUST = [(random.uniform(0, W), random.uniform(0, H), random.uniform(6, 16), random.uniform(0, 6.28)) for _ in range(26)]


def dust(img, t, dark):
    d = ImageDraw.Draw(img, "RGBA")
    for x, y, r, ph in DUST:
        yy = (y - t * 14 * (r / 10)) % H
        xx = x + 18 * math.sin(t * .4 + ph)
        col = (255, 255, 255, 18) if dark else (180, 186, 170, 34)
        d.ellipse((xx - r, yy - r, xx + r, yy + r), fill=col)


def shot_frame(t):
    for i, (name, start) in enumerate(SHOTS):
        nxt = SHOTS[i + 1][1] if i + 1 < len(SHOTS) else END
        if start <= t < nxt:
            files = sorted((renders / name).glob("*.jpg"))
            idx = min(len(files) - 1, int((t - start) * FPS))
            return Image.open(files[idx]).convert("RGB"), name, t - start, nxt - start
    return None, None, 0, 0


def end_card(t):
    img = Image.new("RGB", (W, H), CREAM)
    dust(img, t + 57, False)
    d = ImageDraw.Draw(img, "RGBA")
    k = ease(t / .8)
    r = int(430 * (.85 + .15 * k))
    d.ellipse((W / 2 - r, 780 - r, W / 2 + r, 780 + r), fill=(226, 234, 212, int(255 * k)))
    icon = Image.open(ICON).convert("RGBA").resize((300, 300), Image.LANCZOS)
    mask = Image.new("L", (300, 300), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, 299, 299), 68, fill=int(255 * k))
    img.paste(icon, ((W - 300) // 2, int(540 + (1 - k) * 40)), mask)
    for text, font, y, fill, delay in (("Little Lifeline", display(118), 900, INK, .15), ("Build your little clinic", body(50), 1050, SUBTLE, .35)):
        kk = ease((t - delay) / .7)
        layer = Image.new("RGBA", (W, 160), (0, 0, 0, 0))
        ImageDraw.Draw(layer).text(((W - d.textlength(text, font=font)) / 2, 10), text, font=font, fill=fill + (255,))
        img.paste(fade(layer, kk), (0, int(y + (1 - kk) * 26)), fade(layer, kk))
    kk = ease((t - .6) / .6)
    label = "Free on iPhone and iPad"
    f = display(38)
    w = d.textlength(label, font=f) + 90
    pill = Image.new("RGBA", (int(w), 84), (0, 0, 0, 0))
    ImageDraw.Draw(pill).rounded_rectangle((0, 0, int(w) - 1, 83), 42, fill=FOREST + (255,))
    ImageDraw.Draw(pill).text((45, 18), label, font=f, fill=LIGHT_INK + (255,))
    img.paste(fade(pill, kk), (int((W - w) / 2), 1170), fade(pill, kk))
    x = W / 2 - 235
    for i, label in enumerate(("Plays offline", "No ads", "No account")):
        kc = ease((t - .9 - i * .12) / .5)
        f2 = body(32)
        cw = d.textlength(label, font=f2) + 56
        c = Image.new("RGBA", (int(cw), 58), (0, 0, 0, 0))
        cd = ImageDraw.Draw(c)
        cd.rounded_rectangle((0, 0, int(cw) - 1, 57), 29, outline=(196, 206, 184, 255), width=2, fill=(248, 249, 238, 255))
        cd.text((28, 10), label, font=f2, fill=SUBTLE + (255,))
        img.paste(fade(c, kc), (int(x), 1300), fade(c, kc))
        x += cw + 16
    # Gently falling coins around the badge.
    for i in range(7):
        ph = i * 1.7
        cx = W / 2 + 470 * math.cos(ph + t * .5) * .95
        cy = 780 + 470 * math.sin(ph + t * .5) * .95
        if 0 < cx < W and cy < 1080:
            img.paste(fade(COIN_SMALL, k), (int(cx - 22), int(cy - 22)), fade(COIN_SMALL, k))
    return img


def frame(n):
    t = n / FPS
    if t >= END:
        img = end_card(t - END)
        if t < END + .5:                         # the dark line-up shrinks away into the end card
            prev, _, lt, _ = shot_frame(END - 1 / FPS)
            r = int(1200 * (1 - ease((t - END) / .5)))
            mask = Image.new("L", (W, H), 0)
            ImageDraw.Draw(mask).ellipse((W / 2 - r, 860 - r, W / 2 + r, 860 + r), fill=255)
            img.paste(prev, (0, 0), mask)
        if t > DURATION - .4:
            img = Image.blend(img, Image.new("RGB", (W, H), CREAM), (t - (DURATION - .4)) / .4)
        return img
    img, name, lt, span = shot_frame(t)
    dark = name == "lineup"
    # Cross-dissolve into the next chapter render.
    for i, (nm, start) in enumerate(SHOTS[1:], 1):
        if start - .4 <= t < start and nm != "lineup":
            nxt, _, _, _ = shot_frame(start)
            img = Image.blend(img, nxt, smooth((t - (start - .4)) / .4))
    # A dark circle opens from the hospital into the line-up.
    if name == "hospital" and t > 53.4:
        k = ease((t - 53.4) / .6)
        lineup, _, _, _ = shot_frame(54)
        r = int(1250 * k)
        mask = Image.new("L", (W, H), 0)
        ImageDraw.Draw(mask).ellipse((W / 2 - r, 1100 - r, W / 2 + r, 1100 + r), fill=255)
        img.paste(lineup, (0, 0), mask)
        dark = k > .6
    dust(img, t, dark)
    # Caption: new text rises in, old text lifts away.
    for (a, b, *_), block in zip(CAPTIONS, BLOCKS):
        if a - .01 <= t < b:
            k_in = ease((t - a - .15) / .55)
            k_out = 1 - smooth((t - (b - .3)) / .3)
            k = min(k_in, k_out) if b < END else k_in
            layer = fade(block, k)
            img.paste(layer, (0, int(330 + (1 - k_in) * 36 - (1 - k_out) * 24)), layer)
    # Coin counter (not in the dark line-up).
    if not dark and t >= .6:
        kc = ease((t - .6) / .6)
        pill = fade(counter(balance(t)), kc)
        img.paste(pill, (W - 330 - 56, 238), pill)
    # Payments: a chip pops near the visit and coins fly to the counter.
    for at, x, y, label in PAYMENTS:
        if at <= t < at + 1.6:
            k = ease((t - at) / .35)
            up = (t - at) * 30
            c = chip(label)
            alpha = min(k, 1 - smooth((t - at - 1.2) / .4))
            img.paste(fade(c, alpha), (int(x - c.width / 2), int(y - 40 - up)), fade(c, alpha))
            for j in range(5):
                s = (t - at - .15 - j * .08) / .6
                if 0 <= s <= .9:
                    e = ease(s)
                    cx = x + (W - 330 - 56 + 50 - x) * e + 60 * math.sin(e * math.pi) * (1 if j % 2 else -1)
                    cy = y + (286 - y) * e - 160 * math.sin(e * math.pi)
                    img.paste(COIN_SMALL, (int(cx - 22), int(cy - 22)), COIN_SMALL)
    # The hospital's takings pour in.
    if name == "hospital" and 46 <= t < 53.2:
        for j in range(14):
            s = ((t - 46) * .9 + j / 14) % 1
            x0, y0 = 780 + (j * 37) % 240, 1500 - (j * 53) % 400
            # Rise up the right margin, clear of the headline, then tuck into the counter.
            if s < .75:
                e = ease(s / .75)
                cx, cy = x0 + (1010 - x0) * e, y0 + (420 - y0) * e
            else:
                e = ease((s - .75) / .25)
                cx, cy = 1010 + (W - 330 - 6 - 1010) * e, 420 + (286 - 420) * e
                if e > .6:
                    continue
            img.paste(COIN_SMALL, (int(cx - 22), int(cy - 22)), COIN_SMALL)
    if t < .4:
        img = Image.blend(Image.new("RGB", (W, H), CREAM), img, t / .4)
    return img


silent = out.with_suffix(".silent.mp4")
enc = subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                        "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-pix_fmt", "yuv420p", str(silent)], stdin=subprocess.PIPE)
for n in range(FPS * DURATION):
    enc.stdin.write(frame(n).tobytes())
    if n % 300 == 0:
        print("frame", n, flush=True)
enc.stdin.close()
enc.wait()

cues = [(5.0, "payment", .8), (5.6, "collect", .7), (15.4, "payment", .7), (16.0, "care", .6), (21.7, "upgrade", .5), (24.3, "upgrade", .5),
        (25.4, "collect", .6), (27.5, "upgrade", .5), (33.8, "collect", .6), (40.4, "collect", .6), (45.4, "complete", .7), (53.5, "reward", .8), (57.2, "reward", .7)]
cues += [(30 + (12 + 22 * k) / FPS, "tap", .45) for k in range(9)]
inputs = ["-i", str(AUDIO / "morning-rounds.wav"), "-i", str(AUDIO / "morning-rounds.wav")]
graph = ["[1:a][2:a]acrossfade=d=4[m]", "[m]atrim=0:60,afade=t=in:d=1.5,afade=t=out:st=56.5:d=3.5,volume=0.85[mus]"]
labels = ["[mus]"]
for i, (at, name, vol) in enumerate(cues):
    inputs += ["-i", str(AUDIO / f"{name}.wav")]
    ms = int(at * 1000)
    graph.append(f"[{i + 3}:a]adelay={ms}|{ms},volume={vol}[s{i}]")
    labels.append(f"[s{i}]")
graph.append("".join(labels) + f"amix=inputs={len(labels)}:normalize=0[a]")
subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(silent), *inputs, "-filter_complex", ";".join(graph), "-map", "0:v", "-map", "[a]",
                "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-t", str(DURATION), str(out)], check=True)
silent.unlink()
print(out)
