"""Original dialogue portraits for Little Lifeline's opening story: Maya (the nurse) and the boss.

Painted from scratch as layered vector shapes with soft shading (no photographs, no stock or generated images), then
rasterised with the pre-installed Chromium and downsampled. Run:

    python3 Tools/portraits/paint_portraits.py            # writes Resources/Portraits/{Maya,Boss}.png
    python3 Tools/portraits/paint_portraits.py --out DIR  # review copies elsewhere

The colours follow the 3D characters: Maya has light skin (SkinLight), chestnut hair in a high bun (HairChestnut) and
teal scrubs; the boss has warm medium-brown skin (Skin), short very dark hair (Hair) and a dark green-grey suit (Ink)
with a white shirt and a gold tie.
"""
import argparse
import asyncio
import io
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/Portraits"
SIZE = 512


def defs(p):
    """Gradients and soft-focus filters shared by both portraits, coloured per character."""
    return f"""
  <defs>
    <filter id="b2" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="1.2"/></filter>
    <filter id="b4" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="3"/></filter>
    <filter id="b8" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="7"/></filter>
    <filter id="b16" x="-60%" y="-60%" width="220%" height="220%"><feGaussianBlur stdDeviation="14"/></filter>
    <filter id="grain" x="0" y="0" width="100%" height="100%">
      <feTurbulence type="fractalNoise" baseFrequency=".9" numOctaves="2" seed="7" result="n"/>
      <feColorMatrix in="n" type="saturate" values="0" result="g"/>
      <feComponentTransfer in="g" result="a"><feFuncA type="table" tableValues="0 .07"/></feComponentTransfer>
      <feComposite in="a" in2="SourceGraphic" operator="in"/>
    </filter>
    <radialGradient id="skin" cx="44%" cy="38%" r="70%">
      <stop offset="0" stop-color="{p['skin_light']}"/>
      <stop offset=".55" stop-color="{p['skin']}"/>
      <stop offset="1" stop-color="{p['skin_shadow']}"/>
    </radialGradient>
    <linearGradient id="neck" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="{p['skin']}"/>
      <stop offset=".6" stop-color="{p['skin_mid']}"/>
      <stop offset="1" stop-color="{p['skin_shadow']}"/>
    </linearGradient>
    <linearGradient id="hair" x1="0" y1="0" x2=".6" y2="1">
      <stop offset="0" stop-color="{p['hair_light']}"/>
      <stop offset=".45" stop-color="{p['hair']}"/>
      <stop offset="1" stop-color="{p['hair_dark']}"/>
    </linearGradient>
    <radialGradient id="bun" cx="40%" cy="35%" r="70%">
      <stop offset="0" stop-color="{p['hair_light']}"/>
      <stop offset=".6" stop-color="{p['hair']}"/>
      <stop offset="1" stop-color="{p['hair_dark']}"/>
    </radialGradient>
    <linearGradient id="cloth" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="{p['cloth_light']}"/>
      <stop offset=".5" stop-color="{p['cloth']}"/>
      <stop offset="1" stop-color="{p['cloth_dark']}"/>
    </linearGradient>
    <radialGradient id="iris" cx="45%" cy="40%" r="60%">
      <stop offset="0" stop-color="{p['iris_light']}"/>
      <stop offset=".7" stop-color="{p['iris']}"/>
      <stop offset="1" stop-color="{p['iris_rim']}"/>
    </radialGradient>
    <linearGradient id="lip" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{p['lip_dark']}"/>
      <stop offset="1" stop-color="{p['lip']}"/>
    </linearGradient>
    <linearGradient id="white" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#fbfaf6"/>
      <stop offset="1" stop-color="#d9d6cc"/>
    </linearGradient>
    <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#e3b555"/>
      <stop offset=".5" stop-color="#c2902f"/>
      <stop offset="1" stop-color="#8f6420"/>
    </linearGradient>
    <clipPath id="face"><path d="{p['face']}"/></clipPath>
    <clipPath id="body"><path d="{p['body']}"/></clipPath>
    <clipPath id="neckclip"><path d="{p['neck']}"/></clipPath>
    <clipPath id="eyeL"><path d="{p['eye_l']}"/></clipPath>
    <clipPath id="eyeR"><path d="{p['eye_r']}"/></clipPath>
    <clipPath id="hairclip"><path d="{p['hair_path']}"/></clipPath>
  </defs>"""


def eye(p, side):
    """One eye: white, iris, pupil and catchlight inside the lids, the upper lid line, lashes and the crease."""
    path = p['eye_l'] if side < 0 else p['eye_r']
    cx = 256 + side * p['eye_dx']
    cy = p['eye_y']
    look = p.get('look', 1.5)
    lashes = ""
    if p.get('lashes'):
        o = cx + side * 22
        lashes = "".join(
            f'<path d="M{o - side * k * 5:.1f},{cy - 3 - k * 2.6:.1f} q{side * 3:.1f},{-2:.1f} {side * (6 - k):.1f},{-3.5 - k * .6:.1f}" '
            f'stroke="{p["lash"]}" stroke-width="{1.7 - k * .2:.1f}" fill="none" stroke-linecap="round"/>' for k in range(4))
    clip = "eyeL" if side < 0 else "eyeR"
    return f"""
    <path d="{path}" fill="#f2ede6"/>
    <g clip-path="url(#{clip})">
      <ellipse cx="{cx}" cy="{cy - 6}" rx="26" ry="9" fill="{p['skin_shadow']}" opacity=".35" filter="url(#b2)"/>
      <circle cx="{cx + look}" cy="{cy + .5}" r="{p['iris_r']}" fill="url(#iris)"/>
      <circle cx="{cx + look}" cy="{cy + .5}" r="{p['iris_r']}" fill="none" stroke="{p['iris_rim']}" stroke-width="1.2" opacity=".8"/>
      <circle cx="{cx + look}" cy="{cy + .5}" r="{p['iris_r'] * .45:.1f}" fill="#120c09"/>
      <circle cx="{cx + look - 3.5}" cy="{cy - 3}" r="2.6" fill="#ffffff" opacity=".95"/>
      <circle cx="{cx + look + 3.5}" cy="{cy + 3.5}" r="1.1" fill="#ffffff" opacity=".6"/>
    </g>
    <path d="{p['lid_l'] if side < 0 else p['lid_r']}" fill="none" stroke="{p['lid_tone']}" stroke-width="7" stroke-linecap="round" opacity=".35" filter="url(#b2)" transform="translate(0,-3)"/>
    <path d="{p['lid_l'] if side < 0 else p['lid_r']}" fill="none" stroke="{p['lash']}" stroke-width="{p['lid_w']}" stroke-linecap="round"/>
    {lashes}
    <path d="{p['crease_l'] if side < 0 else p['crease_r']}" fill="none" stroke="{p['skin_shadow']}" stroke-width="2" stroke-linecap="round" opacity=".55"/>
    <path d="{p['lower_l'] if side < 0 else p['lower_r']}" fill="none" stroke="{p['skin_shadow']}" stroke-width="1.4" stroke-linecap="round" opacity=".5"/>"""


def brow_hairs(p, side):
    """Fine hairs along the brow: denser and finer towards the tail, angled out and up as brows grow."""
    import random
    rnd = random.Random(17 if side < 0 else 23)
    b, y = 256 + side * p['brow_in'], p['eye_y']
    p0, p1, p2 = (b, y - p['brow_low']), (b + side * 30, y - p['brow_high'] - 8), (b + side * p['brow_len'], y - p['brow_low'] - 2)
    out = []
    for k in range(p['brow_count']):
        t = k / (p['brow_count'] - 1)
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        yy = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        thick = p['brow_thick'] * (1 - .55 * t)
        yy += rnd.uniform(-thick, thick) * .55
        tx = 2 * (1 - t) * (p1[0] - p0[0]) + 2 * t * (p2[0] - p1[0])
        ty = 2 * (1 - t) * (p1[1] - p0[1]) + 2 * t * (p2[1] - p1[1])
        norm = max(1e-6, (tx * tx + ty * ty) ** .5)
        lift = 1.3 if t < .15 else .45
        dx, dy = tx / norm, ty / norm - lift
        norm = (dx * dx + dy * dy) ** .5
        length = 9 - 3 * t
        out.append(f'<path d="M{x:.1f},{yy + 2:.1f} l{dx / norm * length:.1f},{dy / norm * length:.1f}" stroke="{p["brow"]}" '
                   f'stroke-width="{1.6 - .6 * t:.2f}" stroke-linecap="round" opacity="{.8 - .25 * t:.2f}"/>')
    return "".join(out)


def face_layers(p):
    """Skin with its light from the upper left, the shadow side, eye sockets, cheeks, nose, mouth and brows."""
    return f"""
  <g clip-path="url(#face)">
    <path d="{p['face']}" fill="url(#skin)"/>
    <ellipse cx="352" cy="262" rx="58" ry="140" fill="{p['skin_shadow']}" opacity=".55" filter="url(#b16)"/>
    <ellipse cx="256" cy="392" rx="120" ry="40" fill="{p['skin_shadow']}" opacity=".5" filter="url(#b16)"/>
    <ellipse cx="226" cy="178" rx="56" ry="26" fill="#ffffff" opacity=".22" filter="url(#b8)"/>
    <ellipse cx="{256 - p['eye_dx']}" cy="{p['eye_y'] - 4}" rx="30" ry="15" fill="{p['skin_shadow']}" opacity=".32" filter="url(#b4)"/>
    <ellipse cx="{256 + p['eye_dx']}" cy="{p['eye_y'] - 4}" rx="30" ry="15" fill="{p['skin_shadow']}" opacity=".38" filter="url(#b4)"/>
    <ellipse cx="{206 if p['blush'] else 210}" cy="{p['eye_y'] + 44}" rx="26" ry="15" fill="{p['blush_colour']}" opacity="{p['blush']}" filter="url(#b8)"/>
    <ellipse cx="{306 if p['blush'] else 302}" cy="{p['eye_y'] + 44}" rx="26" ry="15" fill="{p['blush_colour']}" opacity="{p['blush'] * .85:.2f}" filter="url(#b8)"/>
    <ellipse cx="214" cy="{p['eye_y'] + 30}" rx="18" ry="9" fill="#ffffff" opacity=".07" filter="url(#b8)"/>
    <ellipse cx="190" cy="{p['eye_y'] + 50}" rx="16" ry="34" fill="{p['skin_shadow']}" opacity=".18" filter="url(#b8)"/>
    <ellipse cx="322" cy="{p['eye_y'] + 50}" rx="16" ry="34" fill="{p['skin_shadow']}" opacity=".28" filter="url(#b8)"/>
    <ellipse cx="256" cy="{p['nose_y'] + 12}" rx="16" ry="5" fill="{p['skin_deep']}" opacity=".28" filter="url(#b4)"/>
    <path d="M268,{p['eye_y'] - 6} C271,{p['eye_y'] + 20} 276,{p['eye_y'] + 40} 279,{p['eye_y'] + 52}" stroke="{p['skin_shadow']}" stroke-width="7" fill="none" opacity=".45" filter="url(#b4)"/>
    <path d="M252,{p['eye_y'] + 4} C251,{p['eye_y'] + 22} 250,{p['eye_y'] + 36} 251,{p['eye_y'] + 46}" stroke="#ffffff" stroke-width="9" fill="none" opacity=".10" filter="url(#b4)"/>
    <path d="{p['hair_path']}" fill="{p['skin_deep']}" opacity=".45" filter="url(#b8)" transform="translate(4,10)"/>
    <path d="M{256 - p['nose_w'] - 6},{p['nose_y'] + 4} C{256 - p['nose_w'] - 18},{p['nose_y'] + 18} {256 - 38},{p['mouth_y'] - 6} {256 - 38},{p['mouth_y'] + 4}" stroke="{p['skin_shadow']}" stroke-width="5" fill="none" opacity=".45" filter="url(#b4)"/>
    <path d="M{256 + p['nose_w'] + 6},{p['nose_y'] + 4} C{256 + p['nose_w'] + 18},{p['nose_y'] + 18} {256 + 38},{p['mouth_y'] - 6} {256 + 38},{p['mouth_y'] + 4}" stroke="{p['skin_shadow']}" stroke-width="5" fill="none" opacity=".55" filter="url(#b4)"/>
    <ellipse cx="{256 - 36}" cy="{p['mouth_y'] - 10}" rx="16" ry="10" fill="#ffffff" opacity=".10" filter="url(#b4)"/>
    <path d="M352,170 C362,230 356,300 330,350" stroke="{p['rim']}" stroke-width="6" fill="none" opacity=".35" filter="url(#b4)"/>
    <ellipse cx="256" cy="{p['mouth_y'] + 24}" rx="24" ry="9" fill="#ffffff" opacity=".07" filter="url(#b8)"/>
    <ellipse cx="256" cy="{p['mouth_y'] + 12}" rx="22" ry="5" fill="{p['skin_shadow']}" opacity=".35" filter="url(#b4)"/>
    <rect x="0" y="0" width="512" height="512" fill="#000" filter="url(#grain)" opacity=".8"/>
  </g>
  <!-- nose -->
  <path d="M{256 - 9},{p['eye_y'] + 8} C{256 - 11},{p['nose_y'] - 30} {256 - 14},{p['nose_y'] - 14} {256 - 15},{p['nose_y'] - 4}" stroke="{p['skin_shadow']}" stroke-width="4" fill="none" opacity=".3" filter="url(#b2)"/>
  <path d="M{256 + 9},{p['eye_y'] + 8} C{256 + 11},{p['nose_y'] - 30} {256 + 14},{p['nose_y'] - 14} {256 + 15},{p['nose_y'] - 4}" stroke="{p['skin_shadow']}" stroke-width="5" fill="none" opacity=".45" filter="url(#b2)"/>
  <ellipse cx="{256 - p['nose_w'] - 1}" cy="{p['nose_y'] + 1}" rx="6" ry="7" fill="{p['skin_shadow']}" opacity=".3" filter="url(#b2)"/>
  <ellipse cx="{256 + p['nose_w'] + 1}" cy="{p['nose_y'] + 1}" rx="6" ry="7" fill="{p['skin_shadow']}" opacity=".4" filter="url(#b2)"/>
  <ellipse cx="253" cy="{p['nose_y'] - 4}" rx="10" ry="8" fill="#ffffff" opacity=".22" filter="url(#b2)"/>
  <path d="M{256 - p['nose_w']},{p['nose_y'] - 6} C{256 - p['nose_w'] - 2},{p['nose_y'] + 4} {256 - p['nose_w'] + 8},{p['nose_y'] + 8} {256 - 6},{p['nose_y'] + 6}" stroke="{p['skin_deep']}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".75"/>
  <path d="M{256 + p['nose_w']},{p['nose_y'] - 6} C{256 + p['nose_w'] + 2},{p['nose_y'] + 4} {256 + p['nose_w'] - 8},{p['nose_y'] + 8} {256 + 6},{p['nose_y'] + 6}" stroke="{p['skin_deep']}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".85"/>
  <ellipse cx="{256 - 8.5}" cy="{p['nose_y'] + 4.5}" rx="4" ry="2.2" fill="{p['skin_deep']}" opacity=".5" filter="url(#b2)"/>
  <ellipse cx="{256 + 8.5}" cy="{p['nose_y'] + 4.5}" rx="4" ry="2.2" fill="{p['skin_deep']}" opacity=".6" filter="url(#b2)"/>
  <!-- mouth -->
  <path d="{p['lip_top']}" fill="url(#lip)"/>
  <path d="{p['lip_bottom']}" fill="{p['lip']}"/>
  <ellipse cx="{256 - 3}" cy="{p['mouth_y'] + 8}" rx="8" ry="2.2" fill="#ffffff" opacity=".16" filter="url(#b2)"/>
  <path d="{p['mouth_line']}" fill="none" stroke="{p['lip_line']}" stroke-width="2.4" stroke-linecap="round"/>
  {p.get('teeth', '')}
  <!-- eyes and brows -->
  {eye(p, -1)}
  {eye(p, 1)}
  <path d="{p['brow_l']}" fill="{p['brow']}" opacity=".7" filter="url(#b2)"/>
  <path d="{p['brow_r']}" fill="{p['brow']}" opacity=".7" filter="url(#b2)"/>
  {brow_hairs(p, -1)}
  {brow_hairs(p, 1)}"""


def flow(hairline, target, lights, darks, count=34, seed=3, spread=36):
    """Fine strands from points along the hairline to where the hair is gathered (a bun, or the crown)."""
    import random
    rnd = random.Random(seed)
    segments = list(zip(hairline, hairline[1:]))
    out = []
    for k in range(count):
        t = (k + rnd.random()) / count * len(segments)
        (x0, y0), (x1, y1) = segments[min(int(t), len(segments) - 1)]
        f = t - int(t)
        x, y = x0 + (x1 - x0) * f, y0 + (y1 - y0) * f
        tx, ty = target[0] + (x - 256) * spread / 100 + rnd.uniform(-4, 4), target[1] + rnd.uniform(-6, 6)
        c1 = (x + (x - 256) * .06, y - (y - ty) * .4)
        c2 = (tx + (x - 256) * .3, ty + (y - ty) * .3)
        colour = rnd.choice(lights) if k % 3 else rnd.choice(darks)
        width = rnd.uniform(1.1, 2.6)
        opacity = rnd.uniform(.18, .45)
        out.append(f'<path d="M{x:.1f},{y:.1f} C{c1[0]:.1f},{c1[1]:.1f} {c2[0]:.1f},{c2[1]:.1f} {tx:.1f},{ty:.1f}" '
                   f'stroke="{colour}" stroke-width="{width:.2f}" fill="none" stroke-linecap="round" opacity="{opacity:.2f}"/>')
    return '<g clip-path="url(#hairclip)">' + "".join(out) + '</g>'


def ears(p):
    out = ""
    for side in (-1, 1):
        x = 256 + side * p['ear_dx']
        out += f"""
  <ellipse cx="{x}" cy="{p['eye_y'] + 18}" rx="15" ry="27" fill="{p['skin_mid']}"/>
  <ellipse cx="{x + side * 2}" cy="{p['eye_y'] + 18}" rx="8" ry="17" fill="{p['skin_shadow']}" opacity=".6" filter="url(#b2)"/>"""
        if p.get('earring'):
            out += f'<circle cx="{x + side * 1}" cy="{p["eye_y"] + 42}" r="3.4" fill="url(#gold)"/>'
    return out


def body(p):
    """Neck, then clothing over it (the collar sits over the neck)."""
    return f"""
  <path d="{p['neck']}" fill="url(#neck)"/>
  <g clip-path="url(#neckclip)">
    <ellipse cx="256" cy="{p['chin_y'] - 4}" rx="60" ry="26" fill="{p['skin_deep']}" opacity=".55" filter="url(#b8)"/>
    <ellipse cx="290" cy="420" rx="20" ry="70" fill="{p['skin_shadow']}" opacity=".5" filter="url(#b8)"/>
  </g>
  {p['clothes']}"""


def svg(p):
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  {defs(p)}
  {p.get('hair_back', '')}
  {body(p)}
  {ears(p)}
  {face_layers(p)}
  {p['hair_front']}
</svg>"""


def maya():
    eye_y, dx = 250, 42
    p = dict(
        skin_light="#f6d9bf", skin="#e8b896", skin_mid="#dca784", skin_shadow="#b97f60", skin_deep="#8e5541",
        hair_light="#9a6748", hair="#5c3a2a", hair_dark="#2f1c14", brow="#4a2e22", lash="#2a1812",
        iris_light="#8a6a3e", iris="#5b4326", iris_rim="#2c2014", iris_r=10,
        lip_dark="#a9524e", lip="#c97068", lip_line="#7c3532", blush_colour="#e98c7c", blush=.42,
        cloth_light="#63b9b6", cloth="#3c9796", cloth_dark="#25696b",
        eye_y=eye_y, eye_dx=dx, nose_y=308, nose_w=13, mouth_y=338, chin_y=382, ear_dx=88, lashes=True, earring=True,
        lid_w=3.2, look=2.2, lid_tone="#a0705e", brow_in=16, brow_low=24, brow_high=28, brow_len=52, brow_thick=5, brow_count=56,
        face="M256,126 C308,126 340,164 342,222 C344,262 336,302 320,331 C303,361 281,381 256,382 C231,381 209,361 192,331 C176,302 168,262 170,222 C172,164 204,126 256,126 Z",
        neck="M224,330 L222,424 C238,438 274,438 290,424 L288,330 Z",
        body="M138,432 C178,410 208,402 226,398 L286,398 C304,402 334,410 374,432 C422,454 452,480 466,512 L46,512 C60,480 90,454 138,432 Z",
    )
    for side, key in ((-1, "l"), (1, "r")):
        cx = 256 + side * dx
        o, i = cx + side * 22, cx - side * 22  # outer and inner corners
        p["eye_" + key] = f"M{i},{eye_y} C{i + side * 9},{eye_y - 11} {o - side * 11},{eye_y - 12} {o},{eye_y - 2} C{o - side * 10},{eye_y + 8} {i + side * 9},{eye_y + 8} {i},{eye_y} Z"
        p["lid_" + key] = f"M{i - side * 1},{eye_y - 1} C{i + side * 9},{eye_y - 12} {o - side * 11},{eye_y - 13} {o + side * 3},{eye_y - 3}"
        p["crease_" + key] = f"M{i + side * 3},{eye_y - 11} C{i + side * 14},{eye_y - 20} {o - side * 10},{eye_y - 20} {o + side * 1},{eye_y - 10}"
        p["lower_" + key] = f"M{i + side * 5},{eye_y + 5} C{i + side * 16},{eye_y + 9} {o - side * 10},{eye_y + 7} {o - side * 1},{eye_y}"
        b = 256 + side * 18
        p["brow_" + key] = f"M{b},{eye_y - 26} C{b + side * 16},{eye_y - 35} {b + side * 38},{eye_y - 36} {b + side * 50},{eye_y - 27} C{b + side * 48},{eye_y - 25} {b + side * 46},{eye_y - 25} {b + side * 44},{eye_y - 26} C{b + side * 32},{eye_y - 31} {b + side * 16},{eye_y - 30} {b + side * 2},{eye_y - 23} Z"
    m = p["mouth_y"]
    p["lip_top"] = f"M229,{m - 4} C239,{m - 9} 249,{m - 12} 256,{m - 6} C263,{m - 12} 273,{m - 9} 283,{m - 4} C273,{m + 1} 264,{m + 2} 256,{m + 2} C248,{m + 2} 239,{m + 1} 229,{m - 4} Z"
    p["lip_bottom"] = f"M232,{m} C242,{m + 16} 270,{m + 16} 280,{m} C270,{m + 4} 242,{m + 4} 232,{m} Z"
    p["mouth_line"] = f"M225,{m - 8} C238,{m + 2} 274,{m + 2} 287,{m - 8}"
    hair_dark, hair_light = p["hair_dark"], p["hair_light"]
    p["hair_path"] = "M166,242 C160,196 168,150 200,126 C224,108 290,104 318,122 C348,142 360,186 350,242 C344,216 334,198 316,186 C296,174 270,168 248,166 C238,166 232,164 228,160 C214,172 196,186 184,206 C176,218 170,230 166,242 Z"
    p["rim"] = "#ffe9d2"
    strands = flow([(168, 240), (184, 206), (228, 160), (270, 168), (318, 186), (348, 240)], (262, 118),
                   [hair_light, "#b5805a", "#8a5a3e"], [hair_dark], count=60, seed=5, spread=45)
    p["hair_back"] = f"""
  <ellipse cx="262" cy="96" rx="58" ry="44" fill="url(#bun)"/>
  <path d="M212,104 C236,72 292,70 314,100" stroke="{hair_light}" stroke-width="3" fill="none" opacity=".5" filter="url(#b2)"/>
  <path d="M220,118 C244,96 288,94 310,116" stroke="{hair_dark}" stroke-width="2.4" fill="none" opacity=".55"/>
  <path d="M232,82 C252,68 286,72 300,88" stroke="{hair_light}" stroke-width="2" fill="none" opacity=".55"/>
  <rect x="226" y="122" width="62" height="12" rx="6" fill="#1f5a5b"/>
  <path d="M160,250 C150,200 166,150 210,128 C240,114 286,112 314,124 C352,144 364,196 352,252 C348,276 342,290 336,300 L340,250 C334,206 300,170 256,168 C212,170 178,206 172,250 L176,300 C168,288 162,272 160,250 Z" fill="{hair_dark}"/>"""
    p["hair_front"] = f"""
  <path d="{p['hair_path']}" fill="url(#hair)"/>
  <g clip-path="url(#hairclip)"><ellipse cx="226" cy="150" rx="46" ry="22" fill="#ffffff" opacity=".16" filter="url(#b8)" transform="rotate(-24 226 150)"/></g>
  {strands}
  <path d="M228,160 C248,170 290,172 318,186 C334,196 344,214 350,242" stroke="{hair_light}" stroke-width="2.2" fill="none" opacity=".5"/>
  <path d="M228,160 C214,172 196,186 184,206" stroke="{hair_light}" stroke-width="1.8" fill="none" opacity=".45"/>
  <path d="M343,236 C352,258 345,276 349,296 C351,306 347,314 343,320" stroke="{p['hair']}" stroke-width="2" fill="none" stroke-linecap="round" opacity=".85"/>
  <path d="M170,232 C164,254 166,272 172,286" stroke="{p['hair']}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".8"/>"""
    # Teal scrubs with a V-neck, a stethoscope round the neck and an ID badge.
    p["clothes"] = f"""
  <path d="{p['body']}" fill="url(#cloth)"/>
  <g clip-path="url(#body)">
    <ellipse cx="402" cy="476" rx="70" ry="80" fill="{p['cloth_dark']}" opacity=".55" filter="url(#b16)"/>
    <ellipse cx="150" cy="446" rx="60" ry="24" fill="#ffffff" opacity=".18" filter="url(#b8)"/>
    <path d="M150,512 C156,486 162,468 170,456" stroke="{p['cloth_dark']}" stroke-width="5" fill="none" opacity=".45" filter="url(#b4)"/>
    <path d="M362,512 C356,488 350,470 342,458" stroke="{p['cloth_dark']}" stroke-width="5" fill="none" opacity=".5" filter="url(#b4)"/>
    <rect x="0" y="380" width="512" height="132" fill="#000" filter="url(#grain)"/>
  </g>
  <path d="M226,398 L256,466 L286,398 Z" fill="url(#neck)"/>
  <path d="M226,398 L256,466 L286,398 Z" fill="{p['skin_deep']}" opacity=".25"/>
  <path d="M222,396 L256,472 L290,396" stroke="{p['cloth_dark']}" stroke-width="9" fill="none" stroke-linejoin="round"/>
  <path d="M222,394 L256,468 L290,394" stroke="{p['cloth_light']}" stroke-width="2" fill="none" opacity=".6"/>
  <path d="M206,402 C190,440 196,478 222,500" stroke="#2f3a3e" stroke-width="7" fill="none" stroke-linecap="round"/>
  <path d="M306,402 C324,438 322,468 304,488" stroke="#2f3a3e" stroke-width="7" fill="none" stroke-linecap="round"/>
  <path d="M205,404 C190,440 196,476 220,498" stroke="#7d8a8f" stroke-width="1.6" fill="none" opacity=".7"/>
  <circle cx="300" cy="494" r="13" fill="#c9cfd1" stroke="#7f898c" stroke-width="2.4"/>
  <circle cx="297" cy="491" r="5" fill="#ffffff" opacity=".7"/>
  <rect x="330" y="440" width="44" height="58" rx="5" fill="#f6f4ee" stroke="#c8c4b8" stroke-width="1.4"/>
  <rect x="330" y="440" width="44" height="13" rx="5" fill="#2f7f80"/>
  <rect x="337" y="460" width="15" height="18" rx="2" fill="#d5b49b"/>
  <rect x="356" y="462" width="13" height="3" rx="1.5" fill="#8a9a99"/>
  <rect x="356" y="469" width="10" height="3" rx="1.5" fill="#b5c0bf"/>
  <rect x="348" y="430" width="8" height="14" rx="2" fill="#9aa4a6"/>"""
    return p


def boss():
    eye_y, dx = 248, 43
    p = dict(
        skin_light="#d9a77c", skin="#bf8458", skin_mid="#b07548", skin_shadow="#8a5533", skin_deep="#5e3420",
        hair_light="#4a3a30", hair="#241a15", hair_dark="#120d0a", brow="#1c1410", lash="#1a120e",
        iris_light="#5a3a22", iris="#3a2414", iris_rim="#1a100a", iris_r=10,
        lip_dark="#7f4434", lip="#9c5a46", lip_line="#5a2c22", blush_colour="#c96a52", blush=.18,
        cloth_light="#465a57", cloth="#2d3d3b", cloth_dark="#1a2524",
        eye_y=eye_y, eye_dx=dx, nose_y=310, nose_w=15, mouth_y=342, chin_y=384, ear_dx=94, lashes=False, earring=False,
        lid_w=3.0, look=-2.2, lid_tone="#5e3420", brow_in=14, brow_low=22, brow_high=27, brow_len=54, brow_thick=6, brow_count=64,
        face="M256,122 C314,122 348,160 349,222 C351,266 345,304 332,334 C316,364 289,384 256,385 C223,384 196,364 180,334 C167,304 161,266 163,222 C164,160 198,122 256,122 Z",
        neck="M216,330 L214,424 C232,440 280,440 298,424 L296,330 Z",
        body="M118,436 C166,410 202,400 220,396 L292,396 C310,400 346,410 394,436 C440,458 464,482 474,512 L38,512 C48,482 72,458 118,436 Z",
    )
    for side, key in ((-1, "l"), (1, "r")):
        cx = 256 + side * dx
        o, i = cx + side * 22, cx - side * 22
        p["eye_" + key] = f"M{i},{eye_y} C{i + side * 9},{eye_y - 10} {o - side * 11},{eye_y - 11} {o},{eye_y - 1} C{o - side * 10},{eye_y + 7} {i + side * 9},{eye_y + 7} {i},{eye_y} Z"
        p["lid_" + key] = f"M{i - side * 1},{eye_y - 1} C{i + side * 9},{eye_y - 11} {o - side * 11},{eye_y - 12} {o + side * 1},{eye_y - 2}"
        p["crease_" + key] = f"M{i + side * 4},{eye_y - 10} C{i + side * 14},{eye_y - 17} {o - side * 10},{eye_y - 17} {o},{eye_y - 9}"
        p["lower_" + key] = f"M{i + side * 5},{eye_y + 5} C{i + side * 16},{eye_y + 9} {o - side * 10},{eye_y + 7} {o - side * 1},{eye_y + 1}"
        b = 256 + side * 16
        p["brow_" + key] = f"M{b},{eye_y - 23} C{b + side * 16},{eye_y - 33} {b + side * 40},{eye_y - 34} {b + side * 54},{eye_y - 26} C{b + side * 53},{eye_y - 23} {b + side * 50},{eye_y - 23} {b + side * 47},{eye_y - 24} C{b + side * 34},{eye_y - 28} {b + side * 16},{eye_y - 26} {b + side * 2},{eye_y - 18} Z"
    m = p["mouth_y"]
    p["lip_top"] = f"M228,{m - 4} C240,{m - 8} 249,{m - 10} 256,{m - 6} C263,{m - 10} 272,{m - 8} 284,{m - 4} C273,{m} 264,{m + 1} 256,{m + 1} C248,{m + 1} 239,{m} 228,{m - 4} Z"
    p["lip_bottom"] = f"M231,{m} C242,{m + 13} 270,{m + 13} 281,{m} C270,{m + 3} 242,{m + 3} 231,{m} Z"
    p["mouth_line"] = f"M222,{m - 8} C237,{m + 3} 275,{m + 3} 290,{m - 8}"
    hair_dark, hair_light = p["hair_dark"], p["hair_light"]
    p["hair_path"] = "M163,236 C155,178 170,126 220,103 C250,90 306,92 332,113 C356,133 361,176 353,236 C351,218 347,204 342,193 C334,181 324,173 314,170 C300,163 280,161 262,162 C246,163 232,165 220,170 C207,175 197,182 190,190 C178,204 169,220 163,236 Z"
    p["rim"] = "#f2c79f"
    strands = flow([(162, 232), (170, 206), (198, 180), (236, 162), (262, 160), (300, 164), (336, 186), (354, 232)], (282, 98),
                   [hair_light, "#5c4a3e", "#3a2c24"], [hair_dark, "#000000"], count=80, seed=11, spread=55)
    p["hair_front"] = f"""
  <path d="{p['hair_path']}" fill="url(#hair)"/>
  <g clip-path="url(#hairclip)"><ellipse cx="232" cy="134" rx="54" ry="20" fill="#ffffff" opacity=".13" filter="url(#b8)" transform="rotate(-18 232 134)"/></g>
  <path d="M204,172 C210,146 234,120 276,110 C304,105 326,114 338,128 C318,121 292,121 268,129 C240,140 220,156 204,172 Z" fill="{hair_light}" opacity=".3"/>
  {strands}
  <path d="M165,234 C171,216 180,200 190,190 C204,176 230,164 262,162 C290,161 318,168 334,182 C344,194 350,212 353,234" stroke="{p['hair']}" stroke-width="5" fill="none" opacity=".5" filter="url(#b2)"/>
  <g clip-path="url(#hairclip)">
    <ellipse cx="170" cy="222" rx="16" ry="30" fill="{p['skin_mid']}" opacity=".45" filter="url(#b8)"/>
    <ellipse cx="346" cy="222" rx="16" ry="30" fill="{p['skin_shadow']}" opacity=".45" filter="url(#b8)"/>
  </g>
  <path d="M212,170 C218,148 228,128 246,112" stroke="{p['skin_shadow']}" stroke-width="1.6" fill="none" opacity=".55"/>
  <path d="M162,232 C160,214 162,196 168,182" stroke="{hair_light}" stroke-width="3" fill="none" opacity=".35" filter="url(#b2)"/>
  <path d="M354,232 C356,212 354,196 348,182" stroke="{hair_light}" stroke-width="3" fill="none" opacity=".3" filter="url(#b2)"/>"""
    # A dark suit with notched lapels, a white shirt and a gold tie, and a folded pocket square.
    p["clothes"] = f"""
  <path d="{p['body']}" fill="url(#cloth)"/>
  <g clip-path="url(#body)">
    <ellipse cx="404" cy="476" rx="72" ry="84" fill="{p['cloth_dark']}" opacity=".7" filter="url(#b16)"/>
    <ellipse cx="142" cy="448" rx="64" ry="24" fill="#ffffff" opacity=".12" filter="url(#b8)"/>
    <rect x="0" y="380" width="512" height="132" fill="#000" filter="url(#grain)"/>
  </g>
  <path d="M222,396 L256,512 L290,396 Z" fill="url(#white)"/>
  <path d="M276,404 L256,512 L290,396 Z" fill="#b9b5aa" opacity=".45"/>
  <path d="M220,392 L242,446 L256,420 L232,390 Z" fill="#f4f2ec" stroke="#cfcabd" stroke-width="1.2"/>
  <path d="M292,392 L270,446 L256,420 L280,390 Z" fill="#e7e3da" stroke="#c4bfb2" stroke-width="1.2"/>
  <path d="M246,418 L266,418 L262,438 L250,438 Z" fill="url(#gold)"/>
  <path d="M250,438 L262,438 L274,512 L238,512 Z" fill="url(#gold)"/>
  <path d="M246,452 L268,470 M243,474 L271,496 M241,496 L273,512" stroke="#8f6420" stroke-width="3" opacity=".5"/>
  <path d="M262,438 L274,512 L262,512 Z" fill="#7a5418" opacity=".3"/>
  <path d="M222,398 C210,408 200,422 194,438 L210,446 L196,456 L236,512 L250,512 L232,462 C226,440 224,418 228,402 Z" fill="{p['cloth_light']}"/>
  <path d="M290,398 C302,408 312,422 318,438 L302,446 L316,456 L276,512 L262,512 L280,462 C286,440 288,418 284,402 Z" fill="{p['cloth']}"/>
  <path d="M194,438 L210,446 L196,456" stroke="{p['cloth_dark']}" stroke-width="2" fill="none"/>
  <path d="M318,438 L302,446 L316,456" stroke="{p['cloth_dark']}" stroke-width="2" fill="none"/>
  <path d="M332,470 L350,462 L368,470 L366,476 L334,476 Z" fill="#f2ede0"/>
  <path d="M340,470 L350,464 L358,470" stroke="#d8cfba" stroke-width="1.2" fill="none"/>"""
    return p


async def render(svgs, out):
    from playwright.async_api import async_playwright
    from PIL import Image
    out.mkdir(parents=True, exist_ok=True)
    async with async_playwright() as pw:
        browser = await pw.chromium.launch()
        page = await browser.new_page(viewport={"width": SIZE, "height": SIZE}, device_scale_factor=2)
        for name, text in svgs.items():
            await page.set_content(f'<html><body style="margin:0;background:transparent">{text}</body></html>')
            png = await page.screenshot(omit_background=True, clip={"x": 0, "y": 0, "width": SIZE, "height": SIZE})
            image = Image.open(io.BytesIO(png)).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS)
            image.save(out / f"{name}.png", optimize=True)
            (out / f"{name}.svg").write_text(text) if out != OUT else None
        await browser.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, default=OUT)
    args = parser.parse_args()
    asyncio.run(render({"Maya": svg(maya()), "Boss": svg(boss())}, args.out))
    print("wrote", ", ".join(str(args.out / f"{n}.png") for n in ("Maya", "Boss")))


if __name__ == "__main__":
    main()
