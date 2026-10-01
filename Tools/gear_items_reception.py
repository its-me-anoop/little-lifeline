"""Reception equipment: twenty real-world pieces (see ClinicGear), each in ten versions.

The first six stand on the reception counter (the game places them on the visitor ledge or the staff work surface);
wall pieces are authored centred on their mounting point with their back at z=0; floor pieces stand on y=0.
Versions climb from plain plastic, through brushed steel and oak, to walnut and brass, and each adds a real feature.
"""
import math

from gear_kit import *

# (body, trim, accent) per version: plastic, steel, oak, then walnut with brass.
RAMP = [
    ("Ivory", "Charcoal", "Sage"), ("Ivory", "Graphite", "Blue"), ("Linen", "Graphite", "Sage"),
    ("BrushedSteel", "Charcoal", "Blue"), ("BrushedSteel", "Graphite", "Sage"), ("Graphite", "BrushedSteel", "Oak"),
    ("Oak", "BrushedSteel", "Sage"), ("Oak", "Graphite", "Brass"), ("Walnut", "Brass", "Quartz"), ("Walnut", "Brass", "Quartz"),
]
FRAME = ["Charcoal", "Charcoal", "Graphite", "BrushedSteel", "BrushedSteel", "BrushedSteel", "Oak", "Oak", "Walnut", "Walnut"]
METAL = ["BrushedSteel", "BrushedSteel", "BrushedSteel", "BrushedSteel", "Chrome", "Chrome", "Chrome", "Brass", "Brass", "Brass"]


class R:
    def __init__(self, v):
        self.v = v
        self.body, self.trim, self.accent = RAMP[v - 1]
        self.frame, self.metal = FRAME[v - 1], METAL[v - 1]
        self.s = 1 + .025 * (v - 1)


def tilt(ob, degrees):
    """Lean an object back about the x axis (its top moves away from the viewer)."""
    ob.rotation_euler.x += math.radians(degrees)
    return ob


def plant_tuft(x, y, z, size, leaves):
    for i in range(leaves):
        a = i * 2.399
        r = size * (.3 + .5 * ((i * 7) % 5) / 5)
        orb((x + math.sin(a) * r * .6, y + size * (.5 + .45 * (i % 3) / 2), z + math.cos(a) * r * .6), (size * .32, size * .5, size * .22), "Leaf" if i % 3 else "Sage", 10)


# ---------------------------------------------------------------- on the counter
def visitor_book(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 5:
        stand = "Oak" if k.v in (7, 8) else "Walnut" if k.v >= 9 else k.trim
        box((0, .02, .02), (.50 * s, .04, .36 * s), stand, .012); y = .04
    cover = "Leather" if k.v >= 4 else ("Sage" if k.v == 3 else "Ivory")
    if k.v == 1:
        box((0, y + .025, 0), (.24, .05, .32), cover, .01)
        box((.005, y + .025, -.002), (.22, .042, .31), "Paper", .004)
        return
    box((0, y + .012, 0), (.46 * s, .024, .33 * s), cover, .008)
    for side in (-1, 1):
        box((side * .112 * s, y + .034, 0), (.21 * s, .026, .30 * s), "Paper", .006)
    box((0, y + .036, 0), (.012, .03, .30 * s), "Charcoal", .002)
    if k.v >= 6:
        for side in (-1, 1):
            for r in range(5):
                box((side * .112 * s, y + .048, -.10 + r * .05), (.16 * s, .003, .006), "Graphite", 0)
        box((.03, y + .05, .12), (.02, .004, .14), "Crimson", 0)
    if k.v >= 3:
        pen = "Brass" if k.v >= 9 else "Charcoal"
        tube((.16 * s, y + .055, -.08), (.16 * s, y + .055, .10), .009, pen, 10)
        orb((.16 * s, y + .055, .105), (.012, .012, .012), "Brass" if k.v >= 8 else "Graphite", 8)
    if k.v >= 8:
        for cx in (-1, 1):
            for cz in (-1, 1):
                box((cx * .225 * s, y + .026, cz * .16 * s), (.03, .006, .03), "Brass", .003)
    if k.v >= 10:
        tube((-.30, 0, .05), (-.30, .12, .05), .028, "Acrylic", 14)
        tube((-.30, .12, .05), (-.30, .2, .05), .004, "Leaf", 6)
        orb((-.30, .21, .05), (.028, .024, .028), "Rose", 10)


def desk_bell(k):
    k = R(k.v); s = k.s
    if k.v >= 7:
        tube((0, 0, 0), (0, .006, 0), .12 * s, "Fabric", 24)
    base = ["Charcoal", "Charcoal", "Rubber", "Oak", "Oak", "Oak", "Walnut", "Walnut", "Walnut", "Walnut"][k.v - 1]
    tube((0, .006, 0), (0, .03, 0), .085 * s, base, 24)
    dome = "BrushedSteel" if k.v < 5 else "Chrome" if k.v < 8 else "Brass"
    d = orb((0, .03, 0), (.075 * s, .06 * s, .075 * s), dome, 20)
    tube((0, .085 * s, 0), (0, .11 * s, 0), .007, "Charcoal" if k.v < 8 else "Brass", 8)
    orb((0, .114 * s, 0), (.014, .01, .014), "Charcoal" if k.v < 8 else "Brass", 10)
    if k.v >= 2:
        tube((0, .028, 0), (0, .034, 0), .088 * s, k.metal, 24)
    if k.v >= 3:
        card = box((.15 * s, .045, .02), (.13, .08, .006), "Paper", .002)
        tilt(card, 12)
        box((.15 * s, .06, .015), (.09, .01, .002), "Ink", 0)
    if k.v >= 9:
        box((.15 * s, .004, .02), (.15, .008, .05), "Walnut", .003)


def card_reader(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 3:
        stand = "Oak" if k.v in (7, 8) else "Walnut" if k.v >= 9 else k.trim
        tube((0, 0, .02), (0, .014, .02), .08 * s, stand, 20)
        tube((0, .014, .02), (0, .06, .02), .016, k.metal, 10)
        y = .06
    body = "Charcoal" if k.v < 4 else k.body if k.v < 7 else "Graphite"
    shell = box((0, y + .018, 0), (.11 * s, .036, .19 * s), body, .012)
    tilt(shell, 18 if k.v >= 3 else 6)
    screen_h = .06 if k.v < 5 else .09
    scr = box((0, y + .038, -.045 + (.02 if k.v >= 5 else 0)), (.086 * s, .004, screen_h * s), "ScreenUI", .002)
    tilt(scr, 18 if k.v >= 3 else 6)
    if k.v < 5:
        for r in range(4):
            for c in range(3):
                key = box((-.025 + c * .025, y + .04 - r * .006, .02 + r * .022), (.018, .005, .014), "Graphite" if (r + c) % 5 else "Crimson", .002)
                tilt(key, 18 if k.v >= 3 else 6)
    if k.v >= 6:
        for i in range(3):
            box((.035, y + .075 + i * .004, .05 - i * .008), (.004, .004, .02 + i * .012), "LampLight", 0)
    if k.v >= 9:
        tube((0, .011, .02), (0, .016, .02), .083 * s, "Brass", 20)
    if k.v >= 10:
        paper = box((0, y + .09, .075), (.07, .07, .003), "Paper", 0)
        tilt(paper, -20)


def desk_phone(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 9:
        box((0, .012, 0), (.34 * s, .024, .28 * s), "Walnut", .01); y = .024
    body = "Charcoal" if k.v < 4 else "Graphite" if k.v < 7 else k.body
    box((0, y + .03, 0), (.24 * s, .06, .20 * s), body, .018)
    face = box((.02, y + .07, -.005), (.17 * s, .02, .15 * s), "Charcoal", .008)
    tilt(face, 14)
    for r in range(4):
        for c in range(3):
            key = box((.04 + c * .03, y + .085 - r * .007, -.03 + r * .028), (.022, .006, .018), "Ivory" if k.v < 7 else "Quartz", .003)
            tilt(key, 14)
    # Handset lying in its cradle on the left.
    box((-.085 * s, y + .075, 0), (.055, .035, .22 * s), body, .016)
    orb((-.085 * s, y + .085, -.1 * s), (.03, .025, .035), body, 10)
    orb((-.085 * s, y + .085, .1 * s), (.03, .025, .035), body, 10)
    if k.v >= 3:
        d = box((.035, y + .11, .055), (.1, .003, .04), "ScreenUI", .001)
        tilt(d, 14)
    if k.v >= 5:
        for i in range(5):
            box((.125, y + .075, -.05 + i * .025), (.012, .008, .014), k.accent if k.accent != "Quartz" else "Sage", .003)
    if k.v >= 6:
        box((.20 * s, y + .015, .02), (.07, .03, .08), "Charcoal", .01)
        box((.20 * s, y + .10, .03), (.045, .16, .03), body, .012)
        box((.20 * s, y + .14, .014), (.03, .03, .003), "ScreenUI", .001)
    if k.v >= 7:
        tube((-.20 * s, y, .06), (-.20 * s, y + .17, .06), .006, k.metal, 8)
        tube((-.235 * s, y + .17, .06), (-.165 * s, y + .17, .06), .012, "Charcoal", 10)
        orb((-.235 * s, y + .14, .06), (.02, .03, .015), "Charcoal", 10)
        orb((-.165 * s, y + .14, .06), (.02, .03, .015), "Charcoal", 10)
    if k.v >= 10:
        for z in (-.09, .09):
            box((-.085 * s, y + .07, z * s), (.07, .01, .02), "Brass", .003)


def receipt_printer(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 7:
        drawer = "Charcoal" if k.v < 9 else "Walnut"
        box((0, .05, .02), (.40 * s, .10, .34 * s), drawer, .012)
        box((0, .05, -.152 * s), (.34 * s, .07, .01), "Graphite" if k.v < 9 else "Walnut", .004)
        box((0, .055, -.162 * s), (.10, .014, .012), k.metal, .004)
        y = .10
    body = "Ivory" if k.v < 4 else k.body if k.v < 9 else "Graphite"
    box((0, y + .055 * s, 0), (.18 * s, .11 * s, .22 * s), body, .02)
    box((0, y + .112 * s, .02), (.17 * s, .008, .14 * s), k.trim if k.trim != "Quartz" else "Graphite", .004)
    box((0, y + .10 * s, -.07 * s), (.13 * s, .006, .02), "Charcoal", .002)
    if k.v >= 2:
        paper = box((0, y + .15 * s, -.075 * s), (.11 * s, .09, .003), "Paper", 0)
        tilt(paper, -12)
    if k.v >= 3:
        orb((.07 * s, y + .09 * s, -.112 * s), (.009, .009, .004), "Glow", 8)
    if k.v >= 5:
        box((-.05 * s, y + .06 * s, -.112 * s), (.05, .012, .004), k.accent if k.accent != "Quartz" else "Sage", .002)
    if k.v >= 10:
        for i in range(3):
            box((.17 * s, y + .004 + i * .004, .02 + i * .006), (.07, .003, .11), "Paper", 0)


def sanitizer_station(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 7:
        tray = "Oak" if k.v < 9 else "Walnut"
        box((.04, .012, 0), (.40 * s, .024, .20 * s), tray, .01); y = .024
    bottle = "Acrylic" if k.v < 5 else "Linen"
    tube((0, y, 0), (0, y + .16 * s, 0), .04 * s, bottle, 16)
    tube((0, y + .05, 0), (0, y + .11, 0), .0415 * s, "Paper" if k.v < 4 else k.accent if k.accent != "Quartz" else "Sage", 16)
    pump = "Ivory" if k.v < 5 else "BrushedSteel" if k.v < 9 else "Brass"
    tube((0, y + .16 * s, 0), (0, y + .2 * s, 0), .012, pump, 10)
    box((0, y + .205 * s, -.025), (.022, .014, .06), pump, .004)
    if k.v >= 2:
        tube((0, y + .005, 0), (0, y + .02, 0), .046 * s, "Glow", 16)
    if k.v >= 3:
        box((.13 * s, y + .045, 0), (.13, .09, .11), k.body if k.body not in ("Walnut",) else "Quartz", .01)
        orb((.13 * s, y + .095, 0), (.035, .03, .03), "Paper", 10)
    if k.v >= 6:
        tube((-.1 * s, y, .01), (-.1 * s, y + .09, .01), .045, k.accent if k.accent not in ("Oak", "Quartz") else "Sage", 16)
        tube((-.1 * s, y + .09, .01), (-.1 * s, y + .1, .01), .047, "Charcoal", 16)
    if k.v >= 10:
        tube((.22, y, .04), (.22, y + .05, .04), .03, "Terracotta", 12)
        plant_tuft(.22, y + .03, .04, .06, 5)


# ---------------------------------------------------------------- on the walls (centred on the mount, back at z=0)
def wall_clock(k):
    k = R(k.v); s = k.s * (1.25 if k.v >= 9 else 1)
    rim = ["Charcoal", "Charcoal", "Graphite", "BrushedSteel", "BrushedSteel", "Chrome", "Oak", "Oak", "Walnut", "Walnut"][k.v - 1]
    tube((0, 0, 0), (0, 0, -.05), .22 * s, rim, 40)
    tube((0, 0, -.045), (0, 0, -.052), .195 * s, "Paper", 40)
    for i in range(12):
        a = i * math.tau / 12
        big = i % 3 == 0 and k.v >= 3
        box((math.sin(a) * .165 * s, math.cos(a) * .165 * s, -.055), (.012 if not big else .02, .032 if not big else .045, .004), "Brass" if k.v >= 9 else "Charcoal", 0)
    hour = box((.03 * s, .03 * s, -.058), (.012, .10 * s, .004), "Charcoal", 0)
    hour.rotation_euler.y = math.radians(-45)
    box((0, .06 * s, -.061), (.009, .13 * s, .004), "Charcoal", 0)
    if k.v >= 5:
        sec = box((.05 * s, -.02 * s, -.064), (.004, .15 * s, .003), "Crimson", 0)
        sec.rotation_euler.y = math.radians(-110)
    tube((0, 0, -.052), (0, 0, -.068), .012, "Brass" if k.v >= 8 else "Charcoal", 10)
    if k.v >= 10:
        # A brass bezel round the face (a clear cover would hide the dial in the game's opaque materials).
        for i in range(24):
            a = i * math.tau / 24
            box((math.sin(a) * .205 * s, math.cos(a) * .205 * s, -.058), (.055 * s, .02, .012), "Brass", 0).rotation_euler.y = -a


def notice_board(k):
    k = R(k.v); s = k.s
    w, h = .92 * s, .62 * s
    box((0, 0, -.018), (w, h, .036), k.frame, .012)
    box((0, 0, -.038), (w - .07, h - .07, .008), "Cork", .004)
    papers = 3 + k.v
    colours = ["Paper", "Paper", "Sage", "Apricot", "Paper", "Blue", "Paper", "Rose", "Paper", "Linen", "Paper", "Paper", "Sage"]
    for i in range(papers):
        c, r = i % 5, i // 5
        pw, ph = (.13, .17) if i % 3 else (.15, .11)
        x = -w / 2 + .12 + c * (w - .22) / 4 + ((i * 17) % 7 - 3) * .006
        yy = h / 2 - .14 - r * .2 - ((i * 11) % 5) * .008
        box((x, yy, -.045), (pw, ph, .003), colours[i % len(colours)], 0)
        orb((x, yy + ph / 2 - .02, -.05), (.012, .012, .01), ["Crimson", "Blue", "Gold", "Leaf"][i % 4], 8)
    if k.v >= 6:
        box((0, h / 2 - .015, -.042), (w - .1, .05, .006), k.accent if k.accent != "Quartz" else "Sage", .003)
    if k.v >= 9:
        for cx in (-1, 1):
            for cy in (-1, 1):
                box((cx * (w / 2 - .02), cy * (h / 2 - .02), -.04), (.04, .04, .006), "Brass", .003)


def security_camera(k):
    k = R(k.v); s = k.s
    mount = "Ivory" if k.v < 4 else "Graphite" if k.v < 9 else "Brass"
    tube((0, 0, 0), (0, 0, -.02), .06, mount, 16)
    if k.v <= 3:
        tube((0, 0, -.02), (0, -.02, -.12), .015, mount, 8)
        orb((0, -.06, -.14), (.07 * s, .06 * s, .07 * s), "Ivory", 16)
        orb((0, -.09, -.16), (.045 * s, .03 * s, .045 * s), "Glass", 14)
        return
    tube((0, 0, -.02), (0, .02, -.14), .014, mount, 8)
    body = "Ivory" if k.v < 6 else "Graphite"
    tube((0, .0, -.12), (0, -.04, -.32 * s), .045 * s, body, 16)
    tube((0, -.04, -.32 * s), (0, -.045, -.335 * s), .035 * s, "Glass", 16)
    if k.v >= 6:
        box((0, .04, -.22 * s), (.11 * s, .01, .24 * s), body, .004)
    if k.v >= 7:
        orb((.03, .0, -.3 * s), (.006, .006, .006), "Crimson", 6)


def wall_screen(k):
    k = R(k.v); s = k.s * (1.0 if k.v < 5 else 1.15)
    w, h = .92 * s, .54 * s
    if k.v >= 8:
        surround = "Oak" if k.v < 9 else "Walnut"
        box((0, 0, -.01), (w + .22, h + .22, .02), surround, .01)
        for i in range(8):
            box((-w / 2 - .06 + i * (w + .12) / 7, 0, -.022), (.035, h + .2, .006), "Charcoal" if k.v < 9 else "Walnut", 0)
    bezel = .035 if k.v < 3 else .015
    box((0, 0, -.045), (w, h, .03), "Charcoal", .008)
    box((0, 0, -.061), (w - bezel * 2, h - bezel * 2, .003), "ScreenUI", 0)
    if k.v >= 7:
        box((0, -h / 2 - .07, -.05), (w * .8, .05, .06), "Charcoal", .015)
        box((0, -h / 2 - .07, -.081), (w * .78, .036, .002), "Graphite", 0)
    if k.v >= 9:
        box((0, h / 2 + .09, -.03), (.16, .03, .01), "Brass", .004)
    if k.v >= 10:
        box((0, 0, -.028), (w + .03, h + .03, .004), "LampLight", 0)


# ---------------------------------------------------------------- on the floor
def leaflet_stand(k):
    k = R(k.v); s = k.s
    tiers = 2 + (k.v >= 3) + (k.v >= 5) + (k.v >= 8)
    h = .5 + tiers * .22
    box((0, .012, .04), (.52 * s, .024, .34), k.frame, .01)
    for x in (-.24 * s, .24 * s):
        tube((x, .02, .1), (x, h, .1), .014, k.metal if k.v < 7 else k.frame, 10)
    colours = ["Sage", "Apricot", "Blue", "Paper", "Rose", "Linen"]
    for t in range(tiers):
        y = .38 + t * .22
        pocket = box((0, y, .06 - t * .01), (.46 * s, .16, .05), "Acrylic", .006)
        tilt(pocket, 8)
        for j in range(3):
            leaf = box((-.14 * s + j * .14 * s, y + .06, .055 - t * .01), (.12 * s, .2, .012), colours[(t * 3 + j) % 6], .002)
            tilt(leaf, 8)
    if k.v >= 6:
        box((0, h + .07, .1), (.5 * s, .12, .025), k.accent if k.accent != "Quartz" else "Sage", .008)
        box((0, h + .07, .086), (.3 * s, .03, .004), "Paper", 0)
    if k.v >= 10:
        box((0, .06, -.13), (.14, .04, .006), "Brass", .003)


def checkin_kiosk(k):
    k = R(k.v); s = k.s
    base = "Charcoal" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    box((0, .02, 0), (.48 * s, .04, .40 * s), base, .015)
    column = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Linen"
    box((0, .52, .04), (.20 * s, .96, .16), column, .03)
    if k.v >= 7:
        for x in (-1, 1):
            box((x * .12 * s, .52, .04), (.03, .92, .18), "Oak" if k.v < 9 else "Walnut", .01)
    sw, sh = (.36, .26) if k.v < 5 else (.44, .32)
    head = box((0, 1.08, -.02), ((sw + .05) * s, (sh + .05) * s, .06), column if k.v < 7 else "Graphite", .02)
    tilt(head, 20)
    scr = box((0, 1.085, -.052), (sw * s, sh * s, .004), "ScreenUI", .001)
    tilt(scr, 20)
    if k.v >= 3:
        box((0, .82, -.045), (.12 * s, .012, .02), "Charcoal", .003)
        box((0, .81, -.06), (.08, .05, .002), "Paper", 0)
    if k.v >= 6:
        shelf = box((.15 * s, .9, -.06), (.12, .02, .12), "Charcoal", .006)
        box((.15 * s, .925, -.06), (.07, .03, .1), "Graphite", .008)
    if k.v >= 8:
        for x in (-1, 1):
            wing = box((x * (sw / 2 + .06) * s, 1.1, -.04), (.02, .34 * s, .14), "Acrylic", .005)
            wing.rotation_euler.z += math.radians(x * 10)
    if k.v >= 9:
        box((0, .3, -.045), (.1, .05, .006), "Brass", .003)
    if k.v >= 10:
        box((0, 1.08 + (sh / 2 + .05) * s, 0), (sw * s, .015, .02), "LampLight", 0)


def water_cooler(k):
    k = R(k.v); s = k.s
    cab = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    box((0, .45, 0), (.32 * s, .9, .32 * s), cab, .03)
    box((0, .62, -.163 * s), (.24 * s, .22, .006), "Charcoal", .004)
    tap = "Charcoal" if k.v < 9 else "Brass"
    for x, c in ((-.05, "Blue"), (.05, "Crimson")):
        box((x * s, .68, -.172 * s), (.03, .03, .03), tap, .005)
        box((x * s, .7, -.186 * s), (.02, .01, .004), c, 0)
    box((0, .54, -.175 * s), (.18 * s, .02, .06), "Graphite", .004)
    tube((0, .9, 0), (0, .92, 0), .1, "Charcoal", 16)
    tube((0, .92, 0), (0, 1.3, 0), .13 * s, "Acrylic", 24)
    tube((0, 1.3, 0), (0, 1.34, 0), .06, "Blue", 16)
    if k.v >= 3:
        tube((.2 * s, .6, 0), (.2 * s, .95, 0), .035, "Acrylic", 12)
        tube((.2 * s, .6, 0), (.2 * s, .62, 0), .037, k.frame, 12)
        for i in range(5):
            tube((.2 * s, .62 + i * .06, 0), (.2 * s, .68 + i * .06, 0), .03, "Paper", 12)
    if k.v >= 4:
        tube((-.28 * s, 0, .02), (-.28 * s, .38, .02), .1, "Charcoal" if k.v < 9 else "Walnut", 16)
        tube((-.28 * s, .38, .02), (-.28 * s, .4, .02), .104, k.metal, 16)
    if k.v >= 10:
        tube((.3, 0, .1), (.3, .22, .1), .1, "Terracotta", 14)
        plant_tuft(.3, .2, .1, .22, 9)


def queue_barrier(k):
    k = R(k.v); s = k.s
    posts = 2 + (k.v >= 3)
    span = .4
    xs = [(i - (posts - 1) / 2) * span for i in range(posts)]
    post = "Charcoal" if k.v < 4 else "Chrome" if k.v < 8 else "Brass"
    belt = "Crimson" if k.v < 4 else "Blue" if k.v < 6 else "Sage" if k.v < 9 else "Rose"
    for x in xs:
        tube((x, 0, 0), (x, .03, 0), .13, post if k.v >= 4 else "Charcoal", 20)
        tube((x, .03, 0), (x, .95, 0), .025, post, 12)
        tube((x, .95, 0), (x, .99, 0), .04, post, 14)
    for a, b in zip(xs, xs[1:]):
        if k.v >= 9:
            n = 8
            for i in range(n):
                t0, t1 = i / n, (i + 1) / n
                y0 = .9 - .1 * math.sin(math.pi * t0)
                y1 = .9 - .1 * math.sin(math.pi * t1)
                tube((a + (b - a) * t0, y0, 0), (a + (b - a) * t1, y1, 0), .018, belt, 10)
        else:
            box(((a + b) / 2, .9, 0), (b - a, .05, .008), belt, .002)
    if k.v >= 5:
        box((xs[0], 1.1, 0), (.2, .14, .012), k.frame, .006)
        box((xs[0], 1.1, -.008), (.17, .11, .003), "Paper", 0)
        tube((xs[0], .99, 0), (xs[0], 1.03, 0), .01, post, 8)


def umbrella_stand(k):
    k = R(k.v); s = k.s
    if k.v >= 2:
        tube((0, 0, 0), (0, .02, 0), .19 * s, "Charcoal", 24)
    body = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    tube((0, .02, 0), (0, .52 * s, 0), .15 * s, body, 24)
    if k.v >= 7:
        for i in range(12):
            a = i * math.tau / 12
            box((math.sin(a) * .152 * s, .27 * s, math.cos(a) * .152 * s), (.02, .48 * s, .02), "Walnut" if k.v < 9 else "Oak", .004)
    if k.v >= 9:
        tube((0, .44 * s, 0), (0, .47 * s, 0), .156 * s, "Brass", 24)
    colours = ["Blue", "Crimson", "Sage", "Charcoal", "Apricot"]
    for i in range(1 + k.v // 3):
        a = i * 2.2
        x, z = math.sin(a) * .06, math.cos(a) * .06
        tube((x, .1, z), (x * 1.6, .78 * s, z * 1.6), .018, colours[i % 5], 8)
        tube((x * 1.6, .78 * s, z * 1.6), (x * 1.6 + .05, .82 * s, z * 1.6), .014, "Leather" if k.v >= 5 else "Charcoal", 8)


def planter(k):
    k = R(k.v); s = k.s
    if k.v <= 3:
        tube((0, 0, 0), (0, .36 * s, 0), .2 * s, "Terracotta", 24)
        tube((0, .33 * s, 0), (0, .38 * s, 0), .215 * s, "Terracotta", 24)
    elif k.v <= 6:
        tube((0, 0, 0), (0, .46 * s, 0), .22 * s, "Charcoal" if k.v == 5 else "Ivory", 28)
        if k.v >= 6:
            tube((0, .1, 0), (0, .14, 0), .223 * s, "Brass", 28)
    else:
        wood = "Oak" if k.v < 9 else "Walnut"
        box((0, .25 * s, 0), (.46 * s, .5 * s, .46 * s), wood, .02)
        for i in range(5):
            box((-.18 * s + i * .09 * s, .25 * s, -.233 * s), (.02, .46 * s, .006), "Charcoal" if k.v < 9 else "Oak", 0)
        if k.v >= 9:
            box((0, .44 * s, -.234 * s), (.46 * s, .03, .006), "Brass", 0)
    top = .38 * s if k.v <= 3 else .46 * s if k.v <= 6 else .5 * s
    tube((0, top - .02, 0), (0, top, 0), .19 * s, "Soil", 20)
    height = .5 + .09 * k.v
    for i in range(3 + k.v // 3):
        a = i * 2.4
        tube((math.sin(a) * .05, top, math.cos(a) * .05), (math.sin(a) * .16, top + height * (.6 + .4 * (i % 2)), math.cos(a) * .16), .012, "SageDark", 6)
    plant_tuft(0, top + height * .45, 0, .26 + .02 * k.v, 8 + k.v * 2)


def filing_cabinet(k):
    k = R(k.v); s = k.s
    drawers = 2 + (k.v >= 3) + (k.v >= 6)
    dh = .34
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    box((0, drawers * dh / 2 + .04, 0), (.48 * s, drawers * dh, .56), body, .015)
    box((0, .02, .01), (.44 * s, .04, .52), "Charcoal", .006)
    for i in range(drawers):
        y = .04 + dh * i + dh / 2
        box((0, y, -.283), (.44 * s, dh - .03, .012), body, .006)
        box((0, y + .06, -.293), (.16, .02, .014), "Charcoal" if k.v < 9 else "Brass", .004)
        box((0, y + .11, -.291), (.08, .035, .004), "Paper", 0)
    top = .04 + drawers * dh
    if k.v >= 2:
        box((-.1, top + .02, 0), (.24, .04, .3), "Charcoal", .006)
        for i in range(3):
            box((-.1, top + .045 + i * .006, 0), (.2, .005, .27), "Paper", 0)
    if k.v >= 5:
        tube((.14, top, .05), (.14, top + .12, .05), .06, "Terracotta" if k.v < 9 else "Quartz", 14)
        plant_tuft(.14, top + .1, .05, .12, 7)
    if k.v >= 8:
        tube((-.16, top + .05, .14), (-.16, top + .38, .14), .01, "Brass", 8)
        tube((-.16, top + .36, .14), (-.16, top + .46, .14), .08, "Linen", 16)


def air_purifier(k):
    k = R(k.v); s = k.s * (1.12 if k.v >= 10 else 1)
    body = "Ivory" if k.v < 5 else "BrushedSteel" if k.v < 7 else "Linen"
    if k.v >= 9:
        tube((0, 0, 0), (0, .06, 0), .17 * s, "Walnut", 28)
    tube((0, .06 if k.v >= 9 else 0, 0), (0, .78 * s, 0), .15 * s, body, 28)
    tube((0, .78 * s, 0), (0, .8 * s, 0), .13 * s, "Charcoal", 28)
    for i in range(5):
        tube((0, .79 * s, 0), (0, .805 * s, 0), (.02 + i * .022) * s, "Graphite" if i % 2 else "Charcoal", 20)
    if k.v >= 3:
        tube((0, .7 * s, 0), (0, .715 * s, 0), .152 * s, "Glow", 28)
    if k.v >= 7:
        tube((0, .14, 0), (0, .5 * s, 0), .153 * s, "Fabric", 28)
    if k.v >= 9:
        tube((0, .5 * s, 0), (0, .52 * s, 0), .155 * s, "Brass", 28)
    box((0, .6 * s, -.15 * s), (.07, .04, .006), "ScreenUI", 0)


def cash_safe(k):
    k = R(k.v); s = k.s * (1.0 if k.v < 5 else 1.12)
    y = 0
    if k.v >= 7:
        wood = "Oak" if k.v < 9 else "Walnut"
        box((0, .03, 0), (.56 * s, .06, .5 * s), wood, .01)
        y = .06
    body = "Charcoal" if k.v < 5 else "BrushedSteel" if k.v < 7 else "Graphite"
    box((0, y + .23 * s, 0), (.44 * s, .46 * s, .42 * s), body, .02)
    box((0, y + .23 * s, -.213 * s), (.36 * s, .38 * s, .01), "Graphite" if body != "Graphite" else "Charcoal", .006)
    handle = "Chrome" if k.v < 9 else "Brass"
    tube((.1 * s, y + .23 * s, -.22 * s), (.1 * s, y + .23 * s, -.25 * s), .045, handle, 16)
    for a in (0, 90):
        spoke = box((.1 * s, y + .23 * s, -.255 * s), (.09, .012, .012), handle, .003)
        spoke.rotation_euler.y = math.radians(a)
    if k.v >= 3:
        box((-.08 * s, y + .3 * s, -.22 * s), (.1, .12, .008), "Charcoal", .004)
        for r in range(3):
            for c in range(3):
                box((-.105 * s + c * .025, y + .33 * s - r * .025, -.226 * s), (.016, .016, .004), "Graphite", .002)
    if k.v >= 4:
        orb((-.04 * s, y + .36 * s, -.225 * s), (.007, .007, .004), "Glow", 6)
    for z in (-.12, .12):
        box((-.19 * s, y + (.23 + z) * s, -.214 * s), (.02, .06, .014), handle, .004)


def queue_display(k):
    k = R(k.v); s = k.s * (1.0 if k.v < 5 else 1.15)
    base = "Charcoal" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    tube((0, 0, 0), (0, .03, 0), .2, base, 24)
    pole = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 9 else "Brass"
    tube((0, .03, 0), (0, 1.55, 0), .025, pole, 12)
    box((0, 1.72, 0), (.52 * s, .32 * s, .05), "Charcoal", .012)
    box((0, 1.72, -.027), (.48 * s, .28 * s, .003), "ScreenUI", 0)
    if k.v >= 3:
        box((0, 1.72, .027), (.48 * s, .28 * s, .003), "ScreenUI", 0)
    if k.v >= 6:
        box((0, 1.05, -.05), (.16, .2, .1), k.accent if k.accent not in ("Oak", "Quartz") else "Sage", .015)
        orb((0, 1.1, -.1), (.025, .025, .012), "Crimson", 10)
        box((0, 1.0, -.1), (.07, .01, .004), "Paper", 0)
    if k.v >= 10:
        box((0, 1.72 + .17 * s, 0), (.5 * s, .012, .05), "LampLight", 0)


ITEMS = [visitor_book, desk_bell, card_reader, desk_phone, receipt_printer, sanitizer_station, leaflet_stand, wall_clock,
         notice_board, checkin_kiosk, water_cooler, security_camera, queue_barrier, umbrella_stand, planter,
         wall_screen, filing_cabinet, air_purifier, cash_safe, queue_display]

# No shared chrome skirts or glowing trims in reception: each piece carries its own real upgrades.
FOOT = {}
