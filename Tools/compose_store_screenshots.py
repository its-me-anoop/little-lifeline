#!/usr/bin/env python3
"""Frame raw simulator captures as App Store screenshots with a short headline.

    python3 Tools/compose_store_screenshots.py OUT_DIR PREFIX shot1.png shot2.png ...

Each capture keeps its native pixels (scaled down only), sits whole below a headline in the
game's display face. The output size equals the input
size, so iPhone 6.9" and iPad 13" captures stay valid for their display types.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts"
FIELD = (247, 178, 92)      # the icon's warm field
INK = (31, 55, 46)          # deep clinic green
SUB = (74, 58, 34)          # darker tint of the field for the subline
EDGE = (214, 138, 58)

CAPTIONS = [
    ("Run your own little clinic", "Check patients in, treat them and send them home well."),
    ("Collect the day's takings", "Every visit pays at reception. Tap to scoop up the coins."),
    ("Grow into a doctors clinic", "Consultations, a pharmacy, taxis and six room tiers."),
    ("New goals every day", "Keep your streak going for bigger gem rewards."),
    ("Earn gems as you grow", "Milestones pay out free gems all the way up."),
    ("Speed things up", "Finish builds, top up coins or double your collections."),
]


def wrap(draw, text, font, width):
    words, lines, line = text.split(), [], ""
    for word in words:
        trial = (line + " " + word).strip()
        if draw.textlength(trial, font=font) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = word
    lines.append(line)
    return lines


def compose(src: Path, headline: str, subline: str, out: Path):
    shot = Image.open(src).convert("RGB")
    W, H = shot.size
    canvas = Image.new("RGB", (W, H), FIELD)
    draw = ImageDraw.Draw(canvas)
    unit = W / 1320 if H / W > 1.6 else W / 1720
    title = ImageFont.truetype(str(FONTS / "ClinicDisplay.ttf"), int(92 * unit))
    body = ImageFont.truetype(str(FONTS / "LifelineBody.ttf"), int(46 * unit))
    margin = int(96 * unit)
    y = int(150 * unit)
    for line in wrap(draw, headline, title, W - 2 * margin):
        draw.text((margin, y), line, font=title, fill=INK)
        y += int(104 * unit)
    y += int(30 * unit)
    for line in wrap(draw, subline, body, W - 2 * margin):
        draw.text((margin, y), line, font=body, fill=SUB)
        y += int(64 * unit)
    top = y + int(56 * unit)
    # Fit the whole capture so bottom docks stay readable; no bleed.
    scale = min(0.86, (H - top - int(80 * unit)) / H)
    frame_w = int(W * scale)
    frame_h = int(H * scale)
    framed = shot.resize((frame_w, frame_h), Image.LANCZOS)
    radius = int(64 * unit)
    mask = Image.new("L", (frame_w, frame_h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, frame_w - 1, frame_h - 1), radius, fill=255)
    x = (W - frame_w) // 2
    border = int(10 * unit)
    draw.rounded_rectangle((x - border, top - border, x + frame_w + border, top + frame_h + border), radius + border, fill=EDGE)
    canvas.paste(framed, (x, top), mask)
    canvas.save(out, optimize=True)
    print(out, canvas.size)


if __name__ == "__main__":
    out_dir, prefix, shots = Path(sys.argv[1]), sys.argv[2], sys.argv[3:]
    out_dir.mkdir(parents=True, exist_ok=True)
    for index, (src, (headline, subline)) in enumerate(zip(shots, CAPTIONS), 1):
        compose(Path(src), headline, subline, out_dir / f"{prefix}-{index:02d}.png")
