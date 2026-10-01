"""Office equipment: twenty real-world pieces (see ClinicGear), each in ten versions.

The first eight stand on the manager's desk (the game places them on its top, 0.915 m up); the rest stand on the floor.
Versions climb from plain laminate and plastic, through steel and oak, to walnut, leather and brass, and each adds a
real feature, like the reception's pieces.
"""
import math

from gear_kit import *
from gear_items_reception import R, tilt, plant_tuft, desk_phone, filing_cabinet, planter


def wood(k): return "Ivory" if k.v < 3 else "Oak" if k.v < 7 else "Walnut"


# ---------------------------------------------------------------- on the desk
def laptop(k):
    k = R(k.v); s = k.s; y = 0
    if k.v >= 3:
        stand = k.metal if k.v < 7 else wood(k)
        box((0, .03, .03), (.30 * s, .012, .24), stand, .004)
        tilt(box((0, .06, .08), (.28 * s, .012, .08), stand, .004), -20)
        y = .07
    shell = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Graphite"
    base = box((0, y + .01, 0), (.32 * s, .018, .22 * s), shell, .006)
    tilt(base, 8 if k.v >= 3 else 0)
    box((0, y + .022, -.02), (.28 * s, .003, .12 * s), "Charcoal", 0)
    lid = box((0, y + .13 * s, .11 * s), (.32 * s, .21 * s, .012), shell, .006)
    tilt(lid, -12)
    scr = box((0, y + .13 * s, .102 * s), (.29 * s, .18 * s, .003), "ScreenUI", 0)
    tilt(scr, -12)
    if k.v >= 5:
        box((0, .008, -.2), (.32 * s, .014, .11), "Charcoal" if k.v < 9 else "Walnut", .004)
        orb((.24 * s, .012, -.18), (.03, .014, .045), "Charcoal", 10)
    if k.v >= 8:
        box((-.34, .22, .06), (.04, .44, .04), k.metal, .005)
        box((-.34, .36, .04), (.42 * s, .26 * s, .03), "Charcoal", .008)
        box((-.34, .36, .024), (.39 * s, .23 * s, .003), "ScreenUI", 0)


def desk_lamp(k):
    k = R(k.v); s = k.s
    if k.v >= 7:
        # A banker's lamp: brass stem, green glass shade.
        tube((0, 0, 0), (0, .03, 0), .09 * s, "Brass" if k.v >= 9 else "Oak", 20)
        tube((0, .03, 0), (0, .28 * s, 0), .012, "Brass", 10)
        shade = box((0, .32 * s, -.03), (.34 * s, .06, .12), "Sage" if k.v < 10 else "SageDark", .03)
        tilt(shade, 10)
        box((0, .29 * s, -.03), (.30 * s, .006, .1), "LampLight", 0)
        tube((.06, .03, 0), (.06, .16, -.02), .003, "Brass", 6)
        return
    base = "Charcoal" if k.v < 4 else k.metal
    tube((0, 0, 0), (0, .025, 0), .08 * s, base, 20)
    arm = "Charcoal" if k.v < 3 else k.metal
    tube((0, .025, 0), (.02, .24 * s, .05), .008, arm, 8)
    tube((.02, .24 * s, .05), (.0, .36 * s, -.1), .008, arm, 8)
    orb((.0, .36 * s, -.1), (.05, .05, .05), arm, 10)
    head = tube((0, .35 * s, -.12), (0, .31 * s, -.2), .06 if k.v >= 4 else .05, "Charcoal" if k.v < 4 else k.body, 16)
    tube((0, .31 * s, -.2), (0, .305 * s, -.205), .05, "LampLight", 16)


def document_tray(k):
    k = R(k.v); s = k.s
    trays = 1 + (k.v >= 3) + (k.v >= 6)
    body = "Charcoal" if k.v < 4 else "Acrylic" if k.v < 7 else wood(k)
    for t in range(trays):
        y = t * .07
        box((0, y + .005, 0), (.26 * s, .01, .34 * s), body, .003)
        for side in (-1, 1):
            box((side * .125 * s, y + .03, 0), (.01, .05, .34 * s), body, .002)
        box((0, y + .03, .165 * s), (.26 * s, .05, .01), body, .002)
        for p in range(2 + t):
            box((0, y + .014 + p * .004, .01), (.21, .003, .29), "Paper", 0)
        if t < trays - 1:
            for cx in (-1, 1):
                for cz in (-1, 1):
                    tube((cx * .11 * s, y + .01, cz * .15 * s), (cx * .11 * s, y + .075, cz * .15 * s), .005, k.metal, 6)
    if k.v >= 9:
        box((0, .03, -.172 * s), (.08, .02, .004), "Brass", 0)


def pen_cup(k):
    k = R(k.v); s = k.s
    cup = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Leather" if k.v < 9 else "Walnut"
    tube((0, 0, 0), (0, .1 * s, 0), .038 * s, cup, 16)
    colours = ["Blue", "Crimson", "Charcoal", "Leaf", "Brass"]
    for i in range(2 + k.v // 2):
        a = i * 2.4
        tube((math.sin(a) * .015, .04, math.cos(a) * .015), (math.sin(a) * .03, .16 * s, math.cos(a) * .03), .005, colours[i % 5], 6)
    if k.v >= 3:
        # A notepad beside it.
        box((.12, .006, .02), (.1, .012, .14), "Paper", .002)
        box((.12, .013, -.045), (.1, .004, .01), "Charcoal" if k.v < 8 else "Leather", 0)
    if k.v >= 6:
        box((-.12, .01, .02), (.12, .02, .09), "Charcoal" if k.v < 9 else "Walnut", .004)
        for i in range(3): box((-.12, .025 + i * .006, .02), (.1, .004, .08), ["Rose", "Paper", "Sage"][i], 0)


def nameplate(k):
    k = R(k.v); s = k.s
    base = "Acrylic" if k.v < 4 else "Oak" if k.v < 7 else "Walnut"
    box((0, .012, 0), (.26 * s, .024, .07), base, .006)
    plate = box((0, .045, .0), (.24 * s, .05, .006), "Charcoal" if k.v < 5 else "Brass" if k.v >= 8 else "BrushedSteel", .002)
    tilt(plate, 15)
    for i in range(6 + k.v // 3):
        box((-.08 * s + i * .025, .046, -.006), (.012, .016, .002), "Paper" if k.v < 5 else "Charcoal", 0)


def photo_frame(k):
    k = R(k.v); s = k.s
    frame = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 6 else wood(k) if k.v < 9 else "Brass"
    f = box((0, .08 * s, 0), (.16 * s, .13 * s, .015), frame, .004)
    tilt(f, 12)
    p = box((0, .08 * s, -.009), (.13 * s, .1 * s, .003), "Blue", 0)
    tilt(p, 12)
    tilt(box((0, .07 * s, -.011), (.08 * s, .04 * s, .002), "Leaf", 0), 12)
    tube((0, .02, .03), (0, .09, .05), .004, frame, 6)
    if k.v >= 5:
        f2 = box((.17, .06 * s, .02), (.11 * s, .09 * s, .012), frame, .003)
        tilt(f2, 12); f2.rotation_euler.z += math.radians(-15)
        p2 = box((.17, .06 * s, .012), (.085 * s, .065 * s, .002), "Apricot", 0)
        tilt(p2, 12); p2.rotation_euler.z += math.radians(-15)


def desk_plant(k):
    k = R(k.v); s = k.s
    pot = "Terracotta" if k.v < 4 else "Ivory" if k.v < 7 else "Charcoal" if k.v < 9 else "Brass"
    tube((0, 0, 0), (0, .08 * s, 0), .05 * s, pot, 16)
    tube((0, .075 * s, 0), (0, .08 * s, 0), .046 * s, "Soil", 14)
    if k.v < 5:
        for i in range(5 + k.v):
            a = i * 2.4; r = .02 + .01 * (i % 3)
            orb((math.sin(a) * r, .1 * s + (i % 2) * .02, math.cos(a) * r), (.025, .035, .025), "Leaf" if i % 2 else "Sage", 8)
    else:
        plant_tuft(0, .07 * s, 0, .09 + .006 * k.v, 6 + k.v)
    if k.v >= 8:
        box((0, .004, 0), (.14 * s, .008, .14 * s), "Walnut", .003)


# ---------------------------------------------------------------- on the floor
def bookcase(k):
    k = R(k.v); s = k.s
    shelves = 3 + (k.v >= 4) + (k.v >= 8)
    w, d, h = .8 * s, .3, .38 * shelves + .1
    body = wood(k) if k.v >= 3 else "Ivory"
    for side in (-1, 1): box((side * w / 2, h / 2, 0), (.03, h, d), body, .006)
    box((0, h / 2, d / 2 - .01), (w, h, .015), body, .004)
    box((0, h, 0), (w + .03, .03, d + .02), body, .006)
    colours = ["Crimson", "Blue", "Sage", "Charcoal", "Apricot", "Paper", "Leaf", "Rose", "Leather"]
    for sh in range(shelves):
        y = .06 + sh * .38
        box((0, y, 0), (w - .02, .025, d - .02), body, .004)
        n = 5 + (k.v >= 5) * 2 + sh % 2
        x = -w / 2 + .05
        for b in range(n):
            bh, bw = .2 + ((b * 7 + sh) % 4) * .03, .035 + ((b + sh) % 3) * .01
            if x + bw > w / 2 - .12 and k.v >= 6 and b == n - 1:
                orb((w / 2 - .09, y + .06, 0), (.05, .05, .05), "Brass", 10)
                break
            box((x + bw / 2, y + .012 + bh / 2, -.01), (bw, bh, .2), colours[(b * 3 + sh) % 9], .003)
            x += bw + .004
    if k.v >= 9:
        box((0, h + .035, -d / 2 + .01), (w * .9, .015, .012), "Brass", 0)
        for side in (-1, 1): tube((side * .2, h + .02, -.1), (side * .2, h + .1, -.16), .006, "Brass", 6)


def printer_station(k):
    k = R(k.v); s = k.s
    stand = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 7 else wood(k)
    box((0, .3, 0), (.56 * s, .6, .46), stand, .012)
    if k.v >= 3:
        box((0, .18, -.232), (.5 * s, .24, .01), "Graphite" if k.v < 7 else stand, .004)
        box((0, .22, -.24), (.14, .02, .01), k.metal, .003)
    body = "Ivory" if k.v < 5 else "Graphite"
    box((0, .6 + .16 * s, 0), (.5 * s, .32 * s, .42), body, .02)
    box((0, .6 + .33 * s, .02), (.46 * s, .04, .38), "Charcoal", .008)
    box((0, .6 + .08 * s, -.215), (.36 * s, .06, .015), "Charcoal", .004)
    box((0, .6 + .09 * s, -.23), (.3 * s, .003, .12), "Paper", 0)
    scr = box((.16 * s, .6 + .3 * s, -.2), (.12, .004, .08), "ScreenUI", 0)
    tilt(scr, 30)
    if k.v >= 6:
        box((0, .6 + .36 * s, .02), (.44 * s, .03, .34), "Graphite", .006)
    if k.v >= 9:
        box((-.18 * s, .6 + .26 * s, -.212), (.05, .012, .004), "Brass", 0)


def coat_stand(k):
    k = R(k.v); s = k.s
    wd = "Charcoal" if k.v < 3 else "BrushedSteel" if k.v < 5 else wood(k) if k.v < 9 else "Walnut"
    for i in range(4):
        a = i * math.tau / 4 + .4
        tube((0, .06, 0), (math.sin(a) * .22, 0, math.cos(a) * .22), .015, wd, 8)
    tube((0, 0, 0), (0, 1.75 * s, 0), .022, wd, 12)
    orb((0, 1.77 * s, 0), (.04, .04, .04), "Brass" if k.v >= 8 else wd, 10)
    for i in range(4 + (k.v >= 6) * 2):
        a = i * math.tau / (4 + (k.v >= 6) * 2)
        tube((0, 1.6 * s, 0), (math.sin(a) * .12, 1.68 * s, math.cos(a) * .12), .008, "Brass" if k.v >= 8 else wd, 6)
    coats = ["Blue", "Leather", "Sage", "Charcoal"]
    for i in range(min(3, 1 + k.v // 3)):
        a = i * 2.1 + .5
        orb((math.sin(a) * .13, 1.25 * s, math.cos(a) * .13), (.12, .36, .08), coats[i], 10)
    if k.v >= 4:
        tube((0, .5, 0), (0, .52, 0), .16, wd, 18)


def rug(k):
    k = R(k.v)
    w, d = 1.4 + .04 * k.v, .9 + .03 * k.v
    field = ["Linen", "Linen", "Sage", "Fabric", "Fabric", "Fabric", "Fabric", "SageDark", "Rose", "Rose"][k.v - 1]
    box((0, .004, 0), (w, .008, d), "Charcoal" if k.v >= 5 else "Oak", 0)
    box((0, .007, 0), (w - .08, .008, d - .08), field, 0)
    if k.v >= 3:
        box((0, .009, 0), (w - .24, .006, d - .24), "Paper" if k.v < 7 else "Linen", 0)
        box((0, .011, 0), (w - .3, .006, d - .3), field, 0)
    if k.v >= 6:
        for i in range(5):
            d0 = orb((-w * .3 + i * w * .15, .012, 0), (.1, .004, .1), "Brass" if k.v >= 9 else "Apricot", 8)
    if k.v >= 8:
        for side in (-1, 1):
            for i in range(10):
                box((-w / 2 + .06 + i * (w - .12) / 9, .004, side * (d / 2 + .02)), (.012, .004, .04), "Linen", 0)


def whiteboard(k):
    k = R(k.v); s = k.s
    frame = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Brass"
    for side in (-1, 1):
        tube((side * .52 * s, .08, 0), (side * .52 * s, 1.75, 0), .018, frame, 10)
        tube((side * .52 * s, .08, -.2), (side * .52 * s, .08, .2), .018, frame, 10)
    wheels((-.52 * s, .52 * s), (-.2, .2), .04)
    box((0, 1.2, 0), (1.0 * s, .7, .03), frame, .008)
    box((0, 1.2, -.017), (.96 * s, .66, .004), "Paper", 0)
    box((0, .84, -.04), (.8 * s, .02, .06), frame, .004)
    marks = ["Blue", "Crimson", "Leaf", "Charcoal"]
    for i in range(2 + k.v // 2):
        box((-.35 * s + (i % 4) * .2 * s, 1.4 - (i // 4) * .14, -.02), (.15, .012, .002), marks[i % 4], 0)
    for i in range(min(4, 1 + k.v // 3)):
        tube((-.3 + i * .08, .86, -.05), (-.26 + i * .08, .86, -.05), .008, marks[i], 8)
    if k.v >= 6:
        box((.25 * s, 1.05, -.02), (.3, .24, .002), "Apricot", 0)
        for r in range(3): box((.25 * s, 1.1 - r * .06, -.021), (.24, .008, .001), "Charcoal", 0)
    if k.v >= 9:
        box((0, 1.58, -.01), (.3, .04, .006), "Brass", 0)


def waste_bins(k):
    k = R(k.v); s = k.s
    n = 1 + (k.v >= 3) + (k.v >= 6)
    colours = ["Charcoal", "Blue", "Leaf"]
    for i in range(n):
        x = (i - (n - 1) / 2) * .32
        body = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Walnut" if i == 0 else "BrushedSteel"
        tube((x, 0, 0), (x, .42 * s, 0), .13, body, 18)
        tube((x, .42 * s, 0), (x, .44 * s, 0), .135, colours[i] if k.v >= 3 else "Charcoal", 18)
        if k.v >= 5:
            box((x, .3 * s, -.13), (.1, .08, .004), "Paper", 0)
    if k.v >= 9:
        box((0, .01, 0), (.34 * n, .02, .34), "Walnut", .004)


def side_table(k):
    k = R(k.v); s = k.s
    top = "Ivory" if k.v < 3 else "Oak" if k.v < 6 else "Quartz" if k.v < 9 else "Walnut"
    legs = "Charcoal" if k.v < 7 else "Brass"
    tube((0, .56, 0), (0, .59, 0), .24 * s, top, 24)
    tube((0, .04, 0), (0, .56, 0), .025, legs, 10)
    tube((0, 0, 0), (0, .04, 0), .16, legs, 18)
    # A water carafe and glasses.
    tube((0, .59, 0), (0, .8, 0), .05, "Acrylic", 14)
    tube((0, .59, 0), (0, .72, 0), .046, "Blue", 14)
    tube((0, .8, 0), (0, .83, 0), .025, "Acrylic", 10)
    for i in range(min(4, 1 + k.v // 3)):
        a = i * 1.6 + .5
        tube((math.sin(a) * .15, .59, math.cos(a) * .15), (math.sin(a) * .15, .69, math.cos(a) * .15), .03, "Acrylic", 12)
    if k.v >= 6:
        box((.12, .6, -.12), (.14, .012, .1), "Walnut" if k.v >= 8 else "Charcoal", .003)


def display_cabinet(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 3 else "Oak" if k.v < 7 else "Walnut"
    w, d, h = .7 * s, .38, 1.5 * s
    box((0, .05, 0), (w, .1, d), body, .01)
    box((0, h, 0), (w + .02, .04, d + .02), body, .01)
    for side in (-1, 1): box((side * w / 2, h / 2, 0), (.03, h, d), body, .006)
    box((0, h / 2, d / 2 - .01), (w, h, .015), body, .004)
    # A glazed front: just the slim door frames, so the awards stay in view.
    for side in (-1, 1): box((side * (w / 2 - .03), h / 2, -d / 2), (.025, h - .1, .012), body, .002)
    box((0, h / 2, -d / 2), (.02, h - .1, .012), body, .002)
    shelves = 2 + (k.v >= 5)
    items = 1 + k.v // 2
    for sh in range(shelves):
        y = .12 + (sh + 1) * (h - .15) / (shelves + 1) - .25
        box((0, y, 0), (w - .04, .015, d - .04), "Acrylic", .002)
    for i in range(items):
        sh = i % shelves; y = .12 + (sh + 1) * (h - .15) / (shelves + 1) - .24
        x = -w * .3 + (i // shelves) * w * .3
        if i % 3 == 0:
            tube((x, y, 0), (x, y + .03, 0), .04, "Walnut", 12); tube((x, y + .03, 0), (x, y + .12, 0), .01, "Brass", 8)
            orb((x, y + .16, 0), (.05, .05, .05), "Brass", 12)
        elif i % 3 == 1:
            f = box((x, y + .08, .05), (.14, .16, .012), "Brass" if k.v >= 7 else "Charcoal", .003); tilt(f, 10)
            p = box((x, y + .08, .043), (.11, .13, .002), "Paper", 0); tilt(p, 10)
        else:
            box((x, y + .04, 0), (.08, .08, .08), "Crimson", .004)
    if k.v >= 8:
        box((0, h - .06, -d / 2 - .005), (w * .8, .02, .01), "LampLight", 0)


def climate_unit(k):
    k = R(k.v); s = k.s * (1.1 if k.v >= 8 else 1)
    body = "Ivory" if k.v < 5 else "BrushedSteel" if k.v < 8 else "Linen"
    box((0, .85 * s, 0), (.36 * s, 1.7 * s, .26), body, .04)
    for i in range(10 + k.v):
        box((0, .35 + i * .045, -.132), (.28 * s, .012, .004), "Graphite", 0)
    box((0, 1.52 * s, -.132), (.2 * s, .06, .004), "ScreenUI", 0)
    if k.v >= 4:
        box((0, 1.4 * s, -.133), (.16, .012, .002), "Glow", 0)
    if k.v >= 8:
        for side in (-1, 1): box((side * .19 * s, .85 * s, 0), (.02, 1.6 * s, .24), "Walnut", .004)
    if k.v >= 10:
        box((0, .02, 0), (.44 * s, .04, .32), "Walnut", .006)


def floor_lamp(k):
    k = R(k.v); s = k.s
    base = "Charcoal" if k.v < 4 else "Quartz" if k.v >= 7 else k.metal
    tube((0, 0, 0), (0, .04, 0), .16 * s, base, 22)
    stem = "Charcoal" if k.v < 3 else k.metal if k.v < 8 else "Brass"
    if k.v >= 6:
        # An arc lamp reaching over the visitor chairs.
        pts = [(0, .04, 0)] + [(-.35 * math.sin(t * math.pi / 2) * s, .04 + 1.5 * math.sin(t * math.pi * .55) * s, 0) for t in (.25, .5, .75, 1)]
        for a, b in zip(pts, pts[1:]): tube(a, b, .012, stem, 8)
        x, y = pts[-1][0], pts[-1][1]
        orb((x, y - .06, 0), (.16 * s, .1, .16 * s), "Linen" if k.v < 9 else "Brass", 16)
        tube((x, y - .12, 0), (x, y - .125, 0), .1, "LampLight", 14)
        return
    tube((0, .04, 0), (0, 1.45 * s, 0), .014, stem, 8)
    shade = "Linen" if k.v < 4 else "Paper"
    tube((0, 1.38 * s, 0), (0, 1.62 * s, 0), .16 if k.v < 4 else .18, shade, 20)
    tube((0, 1.38 * s, 0), (0, 1.385 * s, 0), .14, "LampLight", 16)


ITEMS = [laptop, desk_lamp, document_tray, desk_phone, pen_cup, nameplate, photo_frame, desk_plant,
         bookcase, filing_cabinet, printer_station, coat_stand, planter, rug, whiteboard,
         waste_bins, side_table, display_cabinet, climate_unit, floor_lamp]
FOOT = {}
