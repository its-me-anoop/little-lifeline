#!/usr/bin/env python3
"""Social promo images for a release, built from the app icon and real in-game captures.

    python3 Tools/compose_social_promo.py VARIANT OUT_DIR BACK_CAPTURE.png FRONT_CAPTURE.png [ICON.png]

VARIANT picks the copy (see VARIANTS). Use captures and the icon from the version the post
promotes, so the image shows what people will actually install. Writes promo-portrait.png
(1080x1350, 4:5) and promo-square.png (1080x1080).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts"
ICON = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/AppIcon.png"
FIELD = (247, 178, 92)
FIELD_DEEP = (232, 150, 64)
INK = (31, 55, 46)
SUB = (74, 58, 34)
BEZEL = (28, 38, 34)
CHIP = (252, 236, 206)

VARIANTS = {
    # Live release. No gems, goals or guide yet; nothing here may promise 4.0 features.
    "3.3": {"lines": ["Build your own", "little clinic"],
            "chips": ["Train your staff", "Open a doctors clinic", "No ads"],
            "footer": "Little Lifeline · Free on the App Store"},
    "4.0": {"lines": ["Your little clinic", "just got busier"],
            "chips": ["Daily goals", "Free gems", "A helping hand"],
            "footer": "Little Lifeline 4.0 · Free on the App Store"},
}


def font(name, size):
    return ImageFont.truetype(str(FONTS / name), size)


def rounded(image, radius):
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, image.width - 1, image.height - 1), radius, fill=255)
    out = Image.new("RGBA", image.size)
    out.paste(image, (0, 0), mask)
    return out


def phone(capture, height):
    shot = Image.open(capture).convert("RGB")
    width = round(height * shot.width / shot.height)
    screen = rounded(shot.resize((width, height), Image.LANCZOS), round(width * 0.11))
    pad = round(width * 0.035)
    body = Image.new("RGBA", (width + 2 * pad, height + 2 * pad))
    ImageDraw.Draw(body).rounded_rectangle((0, 0, body.width - 1, body.height - 1), round(width * 0.13), fill=BEZEL)
    body.alpha_composite(screen, (pad, pad))
    return body


def shadowed(canvas, layer, xy, blur=18, offset=14):
    shadow = Image.new("RGBA", canvas.size)
    alpha = layer.split()[3].point(lambda a: a * 0.35)
    tint = Image.new("RGBA", layer.size, (90, 50, 10, 255))
    tint.putalpha(alpha)
    shadow.alpha_composite(tint, (xy[0], xy[1] + offset))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(blur)))
    canvas.alpha_composite(layer, xy)


def wrap(draw, text, face, width):
    lines, line = [], ""
    for word in text.split():
        trial = (line + " " + word).strip()
        if draw.textlength(trial, font=face) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = word
    return lines + [line]


def compose(size, variant, left, right, out, icon_path=ICON):
    W, H = size
    canvas = Image.new("RGBA", size, FIELD)
    # A soft ground band behind the phones gives the composition a floor without a gradient.
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((0, round(H * 0.72), W, H), fill=FIELD_DEEP)
    margin = 72
    copy = VARIANTS[variant]
    icon = rounded(Image.open(icon_path).convert("RGB").resize((112, 112), Image.LANCZOS), 26)
    shadowed(canvas, icon, (margin, margin), blur=8, offset=6)
    draw.text((margin + 136, margin + 18), "Little Lifeline", font=font("ClinicDisplay.ttf", 44), fill=INK)
    draw.text((margin + 138, margin + 70), "Clinic management game", font=font("LifelineBody.ttf", 26), fill=SUB)
    title = font("ClinicDisplay.ttf", 76 if H > W else 64)
    y = margin + 160
    # Break by sense on the tall layout rather than leaving one word on its own line.
    lines = copy["lines"] if H > W else wrap(draw, " ".join(copy["lines"]), title, W - 2 * margin)
    for line in lines:
        draw.text((margin, y), line, font=title, fill=INK)
        y += 84 if H > W else 70
    chip_font = font("LifelineBodyBold.ttf", 28)
    x, y = margin, y + 34
    for label in copy["chips"]:
        w = draw.textlength(label, font=chip_font) + 40
        draw.rounded_rectangle((x, y, x + w, y + 52), 26, fill=CHIP)
        draw.text((x + 20, y + 10), label, font=chip_font, fill=INK)
        x += w + 12
    top = y + 88
    footer_h = 96
    phone_h = H - top - footer_h
    # The feature screen sits in front; the clinic view peeks out behind it.
    back = phone(left, round(phone_h * 0.9))
    front = phone(right, phone_h)
    gap = round(W * 0.04)
    overlap = round(back.width * 0.18)
    total = front.width + back.width - overlap
    bx = (W - total) // 2
    shadowed(canvas, back, (bx, top + round(phone_h * 0.08)))
    shadowed(canvas, front, (bx + back.width - overlap + gap // 2, top))
    foot = font("LifelineBodyBold.ttf", 30)
    tw = draw.textlength(copy["footer"], font=foot)
    draw = ImageDraw.Draw(canvas)
    draw.text(((W - tw) / 2, H - footer_h + 30), copy["footer"], font=foot, fill=INK)
    canvas.convert("RGB").save(out, optimize=True)
    print(out, size)


if __name__ == "__main__":
    variant, out_dir = sys.argv[1], Path(sys.argv[2]); out_dir.mkdir(parents=True, exist_ok=True)
    left, right = sys.argv[3], sys.argv[4]
    icon = Path(sys.argv[5]) if len(sys.argv) > 5 else ICON
    compose((1080, 1350), variant, left, right, out_dir / "promo-portrait.png", icon)
    compose((1080, 1080), variant, left, right, out_dir / "promo-square.png", icon)
