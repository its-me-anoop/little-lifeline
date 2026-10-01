"""Store room equipment: twenty real-world pieces (see ClinicGear), each in ten versions. All stand on the floor.

Versions climb from plain plastic and painted steel, through stainless steel, to oak-trimmed, labelled and lit
storage, and each adds more stock or a real feature.
"""
import math

from gear_kit import *
from gear_items_reception import R, tilt


def steel(k): return "Ivory" if k.v < 3 else "Charcoal" if k.v < 5 else "BrushedSteel" if k.v < 9 else "Chrome"


def stock(x, y, z, w, d, n, seed=0):
    """Boxed supplies of mixed sizes along a shelf."""
    colours = ["Paper", "Cork", "Linen", "Blue", "Sage", "Paper", "Apricot"]
    step = w / max(1, n)
    for i in range(n):
        h = .12 + ((i * 7 + seed) % 4) * .03
        box((x - w / 2 + step * (i + .5), y + h / 2, z), (step * .9, h, d * (.7 + ((i + seed) % 3) * .1)), colours[(i * 3 + seed) % 7], .004)


def label(x, y, z, k):
    if k.v >= 4: box((x, y, z), (.08, .03, .003), "Paper", 0)


# ----------------------------------------------------------------
def supply_boxes(k):
    k = R(k.v); s = k.s
    if k.v >= 3:
        box((0, .06, 0), (.8, .12, .8), "Oak" if k.v < 8 else "Walnut", .006)
        for i in range(3): box((-.3 + i * .3, .03, 0), (.1, .06, .78), "Oak", 0)
    y = .12 if k.v >= 3 else 0
    n = 2 + k.v // 2
    for i in range(n):
        layer, slot = i // 4, i % 4
        x, z = (slot % 2 - .5) * .36, (slot // 2 - .5) * .36
        box((x, y + .15 + layer * .3, z), (.34, .3, .34), "Cork", .006)
        box((x, y + .3 + layer * .3, z), (.34, .004, .06), "Paper", 0)
        label(x, y + .15 + layer * .3, z - .172, k)


def hand_truck(k):
    k = R(k.v); s = k.s
    frame = steel(k)
    for x in (-1, 1): tube((x * .2, .1, .1), (x * .2, 1.25 * s, .2), .016, frame, 8)
    for i in range(3 + (k.v >= 5)): tube((-.2, .3 + i * .25, .12 + i * .02), (.2, .3 + i * .25, .12 + i * .02), .01, frame, 6)
    box((0, .03, -.05), (.44, .02, .26), frame, .004)
    wheels((-.24, .24), (.14,), .1)
    tube((-.2, 1.25 * s, .2), (.2, 1.25 * s, .2), .02, "Rubber" if k.v < 7 else "Leather", 8)
    if k.v >= 3:
        box((0, .2, -.02), (.34, .3, .3), "Cork", .006)
    if k.v >= 6:
        box((0, .5, -.02), (.3, .26, .28), "Cork", .006)


def step_ladder(k):
    k = R(k.v); s = k.s
    rail = "Ivory" if k.v < 3 else "BrushedSteel" if k.v < 7 else "Oak" if k.v < 9 else "Walnut"
    steps = 3 + (k.v >= 5)
    h = .28 * steps + .1
    for x in (-1, 1):
        tube((x * .2, 0, -.25), (x * .18, h, 0), .016, rail, 8)
        tube((x * .2, 0, .25), (x * .18, h, .02), .016, rail, 8)
    for i in range(steps):
        y = .25 + i * .28; z = -.25 + (y / h) * .25
        box((0, y, z), (.38, .025, .12), rail if k.v >= 3 else "Charcoal", .004)
    box((0, h, .01), (.36, .03, .16), "Rubber" if k.v < 6 else rail, .006)
    if k.v >= 8:
        for x in (-1, 1): tube((x * .18, h, 0), (x * .18, h + .45, .02), .012, rail, 8)


def water_pallet(k):
    k = R(k.v); s = k.s
    box((0, .07, 0), (.9, .14, .7), "Oak", .004)
    rows = 2 + (k.v >= 4) + (k.v >= 7)
    for layer in range(1 + (k.v >= 5)):
        for r in range(rows):
            for c in range(3):
                x, z = -.3 + c * .3, -.25 + r * .5 / max(1, rows - 1)
                tube((x, .14 + layer * .34, z), (x, .44 + layer * .34, z), .07, "Blue", 12)
                tube((x, .44 + layer * .34, z), (x, .48 + layer * .34, z), .03, "Blue", 8)
    if k.v >= 8:
        # Shrink-wrap bands round the stack.
        for b in range(2 + (k.v >= 9)):
            box((0, .25 + b * .2, 0), (.95, .03, .75), "Paper", .004)


def glove_dispensers(k):
    k = R(k.v); s = k.s
    frame = steel(k)
    box((0, .7, .1), (.6 * s, 1.4, .06), frame, .006)
    for x in (-1, 1): box((x * .3 * s, .02, .02), (.04, .04, .3), frame, .004)
    colours = ["Blue", "Paper", "Sage", "Rose", "Apricot", "Blue"]
    n = 2 + k.v // 2
    for i in range(n):
        row, col = i // 3, i % 3
        x, y = -.18 * s + col * .18 * s, 1.1 - row * .22
        box((x, y, .02), (.16, .1, .1), colours[i % 6], .006)
        box((x, y + .05, -.02), (.06, .004, .03), "Paper", 0)


def gown_rail(k):
    k = R(k.v); s = k.s
    frame = steel(k)
    for x in (-1, 1):
        tube((x * .45 * s, .05, 0), (x * .45 * s, 1.5, 0), .014, frame, 8)
        tube((x * .45 * s, .05, -.2), (x * .45 * s, .05, .2), .014, frame, 8)
    wheels((-.45 * s, .45 * s), (-.2, .2), .04)
    tube((-.45 * s, 1.5, 0), (.45 * s, 1.5, 0), .014, frame, 8)
    colours = ["Blue", "Sage", "Paper", "Blue", "Aqua", "Sage", "Rose", "Blue"]
    for i in range(2 + k.v // 2):
        x = -.36 * s + i * .72 * s / 8
        orb((x, 1.14, 0), (.07, .34, .22), colours[i % 8], 10)
        tube((x, 1.5, 0), (x, 1.46, 0), .004, "Chrome", 6)


def scanner_desk(k):
    k = R(k.v); s = k.s
    top = "Ivory" if k.v < 3 else "Oak" if k.v < 7 else "Walnut"
    box((0, .9, 0), (.7 * s, .04, .45), top, .006)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .32 * s, 0, z * .19), (x * .32 * s, .88, z * .19), .018, steel(k), 8)
    box((-.12, 1.0, .05), (.3, .2, .04), "Charcoal", .008)
    box((-.12, 1.0, .028), (.27, .17, .003), "ScreenUI", 0)
    tube((-.12, .92, .07), (-.12, .9, .08), .04, "Charcoal", 10)
    gun = box((.2, .95, -.05), (.05, .1, .08), "Charcoal" if k.v < 8 else "Graphite", .01)
    tilt(gun, -25)
    if k.v >= 4:
        box((0, .925, -.14), (.3, .01, .1), "Charcoal", .002)
    if k.v >= 7:
        box((.2, .93, .1), (.12, .06, .14), "Graphite", .01)
        box((.2, .96, .03), (.08, .002, .05), "Paper", 0)


def label_printer(k):
    k = R(k.v); s = k.s
    table = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Oak"
    box((0, .45, 0), (.5 * s, .9, .4), table, .01)
    box((0, .9 + .09 * s, 0), (.26 * s, .18 * s, .3), "Ivory" if k.v < 6 else "Graphite", .02)
    box((0, .9 + .05 * s, -.16), (.14, .008, .06), "Paper", 0)
    tube((0, .9 + .1 * s, .08), (0, .9 + .1 * s, .02), .06, "Paper", 14)
    box((.07 * s, .9 + .16 * s, -.12), (.07, .03, .003), "ScreenUI", 0)
    if k.v >= 5:
        for i in range(4): box((-.12 + i * .08, .92, .12), (.06, .002, .04), "Paper", 0)


def cleaning_cart(k):
    k = R(k.v); s = k.s
    body = "Blue" if k.v < 3 else "Charcoal" if k.v < 6 else "BrushedSteel"
    box((0, .45, 0), (.7 * s, .06, .4), body, .01)
    box((0, .1, 0), (.7 * s, .06, .4), body, .01)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .33 * s, .1, z * .18), (x * .33 * s, .9, z * .18), .012, steel(k), 6)
    wheels((-.3 * s, .3 * s), (-.16, .16), .045)
    tube((-.15, .13, 0), (-.15, .38, 0), .12, "Sage" if k.v < 7 else "Blue", 14)
    tube((.15, .13, 0), (.15, .36, 0), .1, "Crimson", 14)
    for i in range(1 + k.v // 3):
        tube((-.25 + i * .12, .48, .05), (-.25 + i * .12, .66, .05), .035, ["Blue", "Leaf", "Paper", "Apricot"][i % 4], 10)
    tube((.3 * s, .48, .1), (.24 * s, 1.35, .12), .012, "Oak", 8)
    if k.v >= 5:
        box((0, .72, .19), (.6 * s, .5, .02), "Charcoal", .004)


def oxygen_cage(k):
    k = R(k.v); s = k.s
    cage = steel(k)
    n = 2 + (k.v >= 3) + (k.v >= 6)
    w = n * .2 + .1
    for x in (-1, 1): box((x * w / 2, .6, 0), (.03, 1.2, .3), cage, .004)
    for y in (.3, .9): box((0, y, -.15), (w, .03, .02), cage, .002)
    box((0, .02, 0), (w, .04, .3), cage, .004)
    for i in range(n):
        x = -w / 2 + .15 + i * .2
        tube((x, .04, 0), (x, 1.05, 0), .08, "Paper" if i % 2 == 0 else "Leaf", 16)
        tube((x, 1.05, 0), (x, 1.1, 0), .05, "Leaf", 12)
        tube((x, 1.1, 0), (x, 1.18, 0), .02, "Chrome", 8)
    if k.v >= 8:
        box((0, 1.25, -.16), (.3, .1, .01), "Leaf", .003)


def medical_fridge(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 5 else "BrushedSteel"
    h = 1.2 + .05 * k.v
    box((0, h / 2, 0), (.6 * s, h, .6), body, .02)
    # A glass door: the frame and handle rail, with the stock in view.
    for side in (-1, 1): box((side * .24 * s, h / 2 + .05, -.3), (.03, h - .3, .012), body, .003)
    for y in (.2, h - .1): box((0, y, -.3), (.5 * s, .03, .012), body, .003)
    for i in range(3 + (k.v >= 5)):
        y = .25 + i * (h - .4) / (3 + (k.v >= 5))
        box((0, y, -.02), (.5 * s, .015, .5), "Chrome", .002)
        stock(0, y + .01, -.05, .46 * s, .3, 3 + i % 2, i)
    box((0, h - .08, -.305), (.3, .06, .004), "ScreenUI", 0)
    box((.24 * s, h / 2, -.32), (.02, .3, .03), "Chrome", .004)


def spare_wheelchairs(k):
    k = R(k.v); s = k.s
    n = 1 + (k.v >= 4) + (k.v >= 8)
    frame = "Charcoal" if k.v < 5 else "Chrome"
    for c in range(n):
        z = c * .18
        for x in (-1, 1):
            tube((x * .22, .3, z), (x * .22, .85, z + .02), .014, frame, 8)
            tube((x * .24, .32, z + .05), (x * .24, .32, z + .1), .28, "Tyre", 18)
        box((0, .5, z), (.4, .02, .1), "Blue" if k.v < 6 else "Leather", .004)
        box((0, .75, z + .03), (.4, .3, .02), "Blue" if k.v < 6 else "Leather", .004)


def bin_station(k):
    k = R(k.v); s = k.s
    colours = ["Crimson", "Gold", "Blue", "Leaf"]
    n = 2 + (k.v >= 4) + (k.v >= 7)
    for i in range(n):
        x = (i - (n - 1) / 2) * .34
        box((x, .35, 0), (.3, .7, .4), colours[i], .02)
        box((x, .71, .01), (.31, .03, .41), "Charcoal", .01)
        box((x, .45, -.201), (.14, .1, .003), "Paper", 0)
    if k.v >= 9:
        box((0, .01, 0), (.34 * n + .06, .02, .46), "Oak", .004)


def linen_cart(k):
    k = R(k.v); s = k.s
    frame = steel(k)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .3 * s, .08, z * .22), (x * .3 * s, 1.2, z * .22), .014, frame, 6)
    wheels((-.28 * s, .28 * s), (-.2, .2), .045)
    shelves = 3
    for i in range(shelves):
        y = .12 + i * .38
        box((0, y, 0), (.64 * s, .02, .48), frame, .004)
        for j in range(1 + (k.v + i) // 3):
            box((-.2 * s + j * .14 * s, y + .05 + (j % 2) * .02, 0), (.12, .08 + (j % 2) * .04, .4), ["Paper", "Blue", "Linen", "Sage"][(i + j) % 4], .02)
    if k.v >= 6:
        box((0, .62, -.25), (.64 * s, 1.1, .01), "Fabric" if k.v < 9 else "Leather", .004)


def iv_pole_rack(k):
    k = R(k.v); s = k.s
    n = 2 + (k.v >= 3) + (k.v >= 6) + (k.v >= 9)
    for i in range(n):
        x = (i - (n - 1) / 2) * .14
        for a in range(5):
            ang = a * math.tau / 5
            tube((x, .05, 0), (x + math.sin(ang) * .2, .03, math.cos(ang) * .2), .008, "Chrome", 6)
        tube((x, .05, 0), (x, 1.8 * s, 0), .01, "Chrome" if k.v < 8 else "Brass", 6)
        for a in range(2): tube((x, 1.8 * s, 0), (x + (a - .5) * .12, 1.85 * s, 0), .006, "Chrome", 6)
    box((0, 1.1, .12), ((n + 1) * .14, .04, .04), steel(k), .004)


def inventory_tablet(k):
    k = R(k.v); s = k.s
    base = "Charcoal" if k.v < 5 else "BrushedSteel" if k.v < 8 else "Walnut"
    tube((0, 0, 0), (0, .03, 0), .18, base, 18)
    tube((0, .03, 0), (0, 1.05, 0), .02, steel(k), 8)
    t = box((0, 1.12, -.02), (.28 * s, .2 * s, .02), "Charcoal", .01)
    tilt(t, 30)
    scr = box((0, 1.12, -.032), (.25 * s, .17 * s, .003), "ScreenUI", 0)
    tilt(scr, 30)
    if k.v >= 5:
        box((.2, 1.0, 0), (.08, .12, .06), "Graphite", .008)


def shelving_unit(k):
    k = R(k.v); s = k.s
    frame = steel(k)
    w, d = .9 * s, .45
    shelves = 3 + (k.v >= 4) + (k.v >= 7)
    h = .38 * shelves + .1
    for x in (-1, 1):
        for z in (-1, 1): tube((x * w / 2, 0, z * d / 2), (x * w / 2, h, z * d / 2), .014, frame, 6)
    for i in range(shelves):
        y = .1 + i * .38
        box((0, y, 0), (w, .02, d), frame if k.v < 7 else "Oak", .004)
        stock(0, y + .01, 0, w - .06, d, 3 + (k.v + i) % 3, i)
        label(0, y - .02, -d / 2 - .002, k)
    if k.v >= 9:
        box((0, h, -d / 2), (w, .02, .02), "LampLight", 0)


def secure_cabinet(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 4 else "Charcoal" if k.v < 7 else "BrushedSteel"
    box((0, .8, 0), (.8 * s, 1.6, .45), body, .015)
    for x in (-1, 1):
        box((x * .2 * s, .82, -.23), (.38 * s, 1.5, .01), body, .004)
        box((x * .03, .9, -.24), (.02, .2, .02), "Chrome" if k.v < 9 else "Brass", .004)
    box((0, 1.1, -.24), (.08, .12, .01), "Charcoal", .004)
    if k.v >= 5:
        box((0, 1.13, -.246), (.05, .03, .002), "ScreenUI", 0)
        orb((0, 1.07, -.246), (.01, .01, .004), "Glow", 6)
    if k.v >= 8:
        box((0, 1.4, -.236), (.3, .06, .004), "Paper", 0)


def dehumidifier(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Graphite"
    box((0, .32 * s, 0), (.38 * s, .64 * s, .3), body, .04)
    for i in range(6 + k.v // 2):
        box((0, .2 + i * .04, -.152), (.28 * s, .012, .004), "Charcoal", 0)
    box((0, .58 * s, -.152), (.14, .04, .004), "ScreenUI", 0)
    wheels((-.14 * s, .14 * s), (-.1, .1), .025)
    if k.v >= 5:
        box((0, .45, -.153), (.1, .01, .002), "Glow", 0)
    if k.v >= 9:
        tube((0, .64 * s, .08), (0, .7 * s, .08), .02, "Brass", 8)


def pallet_jack(k):
    k = R(k.v); s = k.s
    body = "Crimson" if k.v < 4 else "Charcoal" if k.v < 7 else "BrushedSteel"
    for x in (-1, 1): box((x * .2, .06, -.3), (.16, .06, 1.1), body, .01)
    box((0, .2, .3), (.56, .3, .16), body, .02)
    tube((0, .35, .32), (0, 1.1, .5), .02, body, 8)
    tube((-.14, 1.1, .5), (.14, 1.1, .5), .025, "Rubber" if k.v < 8 else "Leather", 8)
    wheels((-.2, .2), (-.8, .3), .05)
    if k.v >= 5:
        box((0, .2, -.3), (.9, .14, .9), "Oak", .004)
        box((0, .42, -.3), (.8, .3, .8), "Cork", .006)


ITEMS = [supply_boxes, hand_truck, step_ladder, water_pallet, glove_dispensers, gown_rail, scanner_desk, label_printer,
         cleaning_cart, oxygen_cage, medical_fridge, spare_wheelchairs, bin_station, linen_cart, iv_pole_rack,
         inventory_tablet, shelving_unit, secure_cabinet, dehumidifier, pallet_jack]
FOOT = {}
