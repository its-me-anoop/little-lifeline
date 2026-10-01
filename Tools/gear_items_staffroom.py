"""Staff room equipment: twenty real-world pieces (see ClinicGear), each in ten versions.

The first seven stand on the kitchenette worktop (the game places them on it, 1.06 m up) and the next two on the table
(0.9 m); the rest stand on the floor. Versions climb from plain plastic, through steel and oak, to walnut and brass.
"""
import math

from gear_kit import *
from gear_items_reception import R, tilt, plant_tuft, planter
from gear_items_office import wood, waste_bins, coat_stand, rug


# ---------------------------------------------------------------- on the worktop
def coffee_machine(k):
    k = R(k.v); s = k.s
    body = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Chrome"
    if k.v < 4:
        # A filter machine with a glass jug.
        box((0, .18 * s, .04), (.2 * s, .36 * s, .2), body, .02)
        box((0, .34 * s, -.04), (.2 * s, .06, .2), body, .02)
        tube((0, .02, -.06), (0, .15 * s, -.06), .065, "Acrylic", 16)
        tube((0, .02, -.06), (0, .08 * s, -.06), .062, "Soil", 16)
        box((0, .01, -.06), (.18 * s, .02, .18), "Charcoal", .004)
        return
    box((0, .2 * s, .02), (.28 * s, .4 * s, .32), body, .03)
    box((0, .38 * s, -.02), (.26 * s, .04, .3), "Graphite", .01)
    box((0, .12 * s, -.14), (.18 * s, .03, .06), "Charcoal", .006)
    tube((0, .2 * s, -.15), (0, .16 * s, -.15), .015, "Chrome", 8)
    tube((0, .045, -.12), (0, .11, -.12), .035, "Paper", 12)
    scr = box((0, .31 * s, -.141), (.12, .06, .004), "ScreenUI", 0)
    if k.v >= 6:
        tube((.1 * s, .2 * s, -.14), (.1 * s, .09 * s, -.19), .008, "Chrome", 8)
    if k.v >= 8:
        tube((-.1 * s, .41 * s, .06), (-.1 * s, .47 * s, .06), .06, "Acrylic", 14)
        tube((-.1 * s, .41 * s, .06), (-.1 * s, .44 * s, .06), .058, "Soil", 14)
    if k.v >= 9:
        for side in (-1, 1): box((side * .145 * s, .2 * s, .02), (.01, .38 * s, .3), "Walnut", .003)
    if k.v >= 10:
        box((0, .43 * s, -.02), (.2, .01, .2), "Brass", .003)


def kettle(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 7 else "Chrome" if k.v < 9 else "Brass"
    tube((0, 0, 0), (0, .02, 0), .09 * s, "Charcoal", 16)
    tube((0, .02, 0), (0, .2 * s, 0), .08 * s, body, 18)
    tube((0, .2 * s, 0), (0, .22 * s, 0), .06 * s, body, 16)
    tube((.08 * s, .17 * s, 0), (.13 * s, .2 * s, 0), .012, body, 8)
    for y in (.06, .19):
        tube((-.08 * s, y * s, 0), (-.12 * s, y * s, 0), .01, "Charcoal" if k.v < 7 else "Walnut", 8)
    tube((-.12 * s, .06 * s, 0), (-.12 * s, .19 * s, 0), .012, "Charcoal" if k.v < 7 else "Walnut", 8)
    if k.v >= 5:
        box((0, .1 * s, -.08 * s), (.02, .1, .004), "Glow", 0)


def microwave(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Graphite"
    box((0, .15 * s, 0), (.46 * s, .28 * s, .34), body, .015)
    box((-.05 * s, .15 * s, -.172), (.3 * s, .2 * s, .006), "Charcoal", .004)
    box((-.05 * s, .15 * s, -.176), (.26 * s, .16 * s, .002), "Glass", 0)
    box((.17 * s, .21 * s, -.173), (.07, .04, .004), "ScreenUI", 0)
    for r in range(3):
        for c in range(2):
            box((.155 * s + c * .03, .15 * s - r * .025, -.174), (.02, .015, .003), "Charcoal", 0)
    if k.v >= 7:
        box((.1 * s, .15 * s, -.176), (.012, .18 * s, .01), "Chrome", .003)
    if k.v >= 9:
        box((0, .30 * s, 0), (.44 * s, .015, .32), "Walnut", .003)


def toaster(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 3 else "Sage" if k.v < 5 else "BrushedSteel" if k.v < 8 else "Chrome"
    slots = 2 + (k.v >= 6) * 2
    box((0, .1 * s, 0), (.14 * slots * s * .8 + .05, .19 * s, .18), body, .03)
    for i in range(slots):
        x = (i - (slots - 1) / 2) * .055 * s
        box((x, .195 * s, 0), (.02, .006, .13), "Charcoal", 0)
    box((.14 * slots * s * .4 + .03, .12 * s, 0), (.015, .02, .03), "Charcoal", .003)
    if k.v >= 4:
        box((.14 * slots * s * .4 + .03, .07 * s, -.03), (.012, .012, .012), "Glow", 0)
    if k.v >= 9:
        tilt(box((-.04, .23 * s, 0), (.1, .08, .012), "Oak", 0), 5)


def mug_tree(k):
    k = R(k.v); s = k.s
    stand = "Charcoal" if k.v < 4 else "BrushedSteel" if k.v < 7 else wood(k)
    tube((0, 0, 0), (0, .02, 0), .08, stand, 16)
    tube((0, .02, 0), (0, .32 * s, 0), .01, stand, 8)
    colours = ["Paper", "Sage", "Blue", "Apricot", "Rose", "Charcoal"]
    for i in range(min(6, 2 + k.v // 2)):
        a = i * math.tau / 6
        y = .12 + (i % 3) * .07
        tube((0, y * s, 0), (math.sin(a) * .08, (y + .03) * s, math.cos(a) * .08), .004, stand, 6)
        tube((math.sin(a) * .1, (y - .06) * s, math.cos(a) * .1), (math.sin(a) * .1, (y + .02) * s, math.cos(a) * .1), .035, colours[i], 12)


def dish_rack(k):
    k = R(k.v); s = k.s
    rack = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Chrome"
    box((0, .01, 0), (.4 * s, .02, .3), "Charcoal" if k.v < 6 else "Oak" if k.v < 9 else "Walnut", .005)
    for x in range(8):
        tube((-.16 * s + x * .045 * s, .02, -.12), (-.16 * s + x * .045 * s, .12, -.12), .003, rack, 6)
    for i in range(1 + k.v // 3):
        tilt(tube((-.1 + i * .06, .02, .02), (-.1 + i * .06, .2, .02), .075, "Paper", 18), 90 - 8)
    for i in range(min(3, k.v // 2)):
        tube((.12 + i * .02, .02, -.02 + i * .04), (.12 + i * .02, .11, -.02 + i * .04), .032, "Acrylic", 12)


def radio(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 3 else "Charcoal" if k.v < 5 else wood(k) if k.v < 9 else "Walnut"
    box((0, .08 * s, 0), (.26 * s, .16 * s, .1), body, .02)
    box((-.05 * s, .08 * s, -.051), (.12 * s, .11 * s, .004), "Fabric" if k.v >= 5 else "Graphite", .002)
    box((.08 * s, .11 * s, -.051), (.07, .03, .003), "ScreenUI", 0)
    tube((.08 * s, .05 * s, -.05), (.08 * s, .05 * s, -.065), .015, "Brass" if k.v >= 8 else "Charcoal", 12)
    if k.v < 5:
        tube((.1 * s, .16 * s, .03), (.16 * s, .34 * s, .03), .004, "Chrome", 6)
    if k.v >= 7:
        box((0, .165 * s, 0), (.2, .01, .03), "Brass" if k.v >= 8 else "Charcoal", .003)


# ---------------------------------------------------------------- on the table
def fruit_bowl(k):
    k = R(k.v); s = k.s
    bowl = "Ivory" if k.v < 4 else "Oak" if k.v < 7 else "Walnut" if k.v < 9 else "Brass"
    tube((0, 0, 0), (0, .02, 0), .06, bowl, 16)
    orb((0, .07, 0), (.16 * s, .06, .16 * s), bowl, 18)
    fruit = [("Crimson", .045), ("Apricot", .042), ("Leaf", .04), ("Gold", .038), ("Crimson", .044), ("Apricot", .04)]
    for i in range(min(6, 2 + k.v // 2)):
        a = i * 2.4; c, r = fruit[i]
        orb((math.sin(a) * .07, .1 + (i // 4) * .04, math.cos(a) * .07), (r, r, r), c, 12)
    if k.v >= 5:
        # A bunch of bananas across the top.
        for j in range(3):
            tube((-.06, .13 + j * .01, -.02 + j * .015), (.06, .15 + j * .01, .02 + j * .015), .014, "Gold", 8)


def magazine_stack(k):
    k = R(k.v); s = k.s
    colours = ["Blue", "Rose", "Sage", "Paper", "Apricot", "Crimson"]
    if k.v >= 5:
        box((0, .01, 0), (.34, .02, .26), "Oak" if k.v < 9 else "Walnut", .004)
    y = .02 if k.v >= 5 else 0
    for i in range(2 + k.v // 2):
        m = box((.01 * (i % 2), y + .006 + i * .01, 0), (.22, .008, .29), colours[i % 6], 0)
        m.rotation_euler.z += math.radians((i * 7) % 11 - 5)
    if k.v >= 7:
        tube((.2, y, .08), (.2, y + .09, .08), .035, "Paper" if k.v < 9 else "Quartz", 12)


# ---------------------------------------------------------------- on the floor
def fridge(k):
    k = R(k.v); s = k.s * (1.08 if k.v >= 8 else 1)
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Graphite"
    h = 1.3 + .05 * k.v
    box((0, h / 2, 0), (.58 * s, h, .6), body, .025)
    box((0, h * .72, -.302), (.56 * s, .012, .004), "Charcoal", 0)
    handle = "Chrome" if k.v < 9 else "Brass"
    box((.22 * s, h * .85, -.32), (.02, .3, .03), handle, .005)
    box((.22 * s, h * .45, -.32), (.02, .4, .03), handle, .005)
    if k.v >= 3:
        for i in range(3):
            box((-.12 + i * .1, h * .9, -.303), (.06, .08, .003), ["Paper", "Sage", "Apricot"][i], 0)
    if k.v >= 6:
        box((-.14 * s, h * .78, -.303), (.14, .16, .004), "ScreenUI", 0)
    if k.v >= 8:
        tube((-.1 * s, h * .58, -.31), (-.1 * s, h * .5, -.31), .025, "Charcoal", 10)


def armchair(k):
    k = R(k.v); s = k.s
    cover = "Linen" if k.v < 3 else "Fabric" if k.v < 7 else "Leather"
    legs = "Charcoal" if k.v < 4 else "Oak" if k.v < 8 else "Walnut"
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .3 * s, 0, z * .3), (x * .32 * s, .14, z * .32), .02, legs, 8)
    box((0, .26, 0), (.72 * s, .2, .7), cover, .06)
    box((0, .38, -.04), (.56 * s, .12, .58), cover, .05)
    box((0, .62, .3), (.72 * s, .52, .14), cover, .06)
    for x in (-1, 1): box((x * .33 * s, .48, 0), (.1, .24, .68), cover, .05)
    if k.v >= 5:
        box((.12, .55, .2), (.3, .26, .1), "Sage" if k.v < 9 else "Rose", .05)
    if k.v >= 8:
        box((0, .2, -.6), (.46 * s, .3, .3), cover, .05)


def tv_unit(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 3 else "Oak" if k.v < 7 else "Walnut"
    box((0, .22, 0), (1.0 * s, .44, .38), body, .01)
    for i in range(2 + (k.v >= 5)):
        box((-.3 * s + i * .3 * s, .22, -.192), (.26 * s, .36, .006), "Charcoal" if k.v < 7 else body, .004)
    w = .7 + .04 * k.v
    box((0, .46, 0), (.24, .02, .16), "Charcoal", .004)
    tube((0, .46, .02), (0, .52, .02), .02, "Charcoal", 8)
    box((0, .52 + w * .29, .02), (w, w * .56, .04), "Charcoal", .01)
    box((0, .52 + w * .29, -.002), (w - .03, w * .56 - .03, .003), "ScreenUI", 0)
    if k.v >= 6:
        box((0, .47, -.12), (.5, .06, .08), "Charcoal", .01)
    if k.v >= 9:
        box((0, .445, -.19), (.9 * s, .015, .01), "Brass", 0)


def shoe_rack(k):
    k = R(k.v); s = k.s
    body = "Charcoal" if k.v < 3 else "BrushedSteel" if k.v < 5 else wood(k)
    tiers = 2 + (k.v >= 4)
    for side in (-1, 1): box((side * .36 * s, tiers * .15, 0), (.03, tiers * .3, .3), body, .004)
    colours = ["Charcoal", "Paper", "Leather", "Blue", "Crimson"]
    for t in range(tiers):
        y = .06 + t * .28
        box((0, y, 0), (.72 * s, .02, .3), body, .004)
        for i in range(1 + (k.v + t) // 3):
            x = -.26 * s + i * .18 * s
            box((x, y + .05, 0), (.08, .08, .26), colours[(i + t) % 5], .03)
    if k.v >= 7:
        box((0, tiers * .3 + .02, 0), (.76 * s, .04, .32), "Leather" if k.v >= 9 else "Fabric", .015)


def water_boiler(k):
    k = R(k.v); s = k.s
    body = "Ivory" if k.v < 4 else "BrushedSteel" if k.v < 8 else "Chrome"
    box((0, .6, 0), (.4 * s, 1.2, .38), "Charcoal" if k.v < 6 else wood(k), .02)
    box((0, 1.2 + .22 * s, 0), (.34 * s, .44 * s, .3), body, .03)
    tube((0, 1.2 + .1 * s, -.16), (0, 1.2 + .05 * s, -.2), .012, "Chrome", 8)
    box((0, 1.21, -.12), (.26 * s, .02, .12), "Charcoal", .004)
    box((0, 1.2 + .34 * s, -.151), (.1, .05, .003), "ScreenUI", 0)
    for i in range(min(4, 1 + k.v // 3)):
        tube((-.12 + i * .08, .2, -.2), (-.12 + i * .08, .26, -.2), .03, ["Paper", "Sage", "Blue", "Apricot"][i], 10)


def massage_chair(k):
    k = R(k.v); s = k.s
    cover = "Charcoal" if k.v < 4 else "Fabric" if k.v < 7 else "Leather"
    shell = "Ivory" if k.v < 5 else "Graphite" if k.v < 8 else "Walnut"
    box((0, .2, 0), (.7 * s, .4, .72), shell, .05)
    tilt(box((0, .5, -.05), (.56 * s, .12, .62), cover, .05), -6)
    tilt(box((0, .9, .32), (.6 * s, .8, .16), cover, .07), -18)
    for x in (-1, 1): box((x * .34 * s, .55, -.02), (.12, .34, .66), shell, .05)
    tilt(box((0, .16, -.46), (.4 * s, .12, .3), cover, .04), 55)
    if k.v >= 5:
        box((.34 * s, .74, -.28), (.1, .02, .12), "ScreenUI", 0)
    if k.v >= 8:
        box((0, 1.3, .44), (.34 * s, .16, .12), cover, .05)


def side_lamp_table(k):
    k = R(k.v); s = k.s
    top = "Ivory" if k.v < 3 else "Oak" if k.v < 7 else "Walnut"
    box((0, .5, 0), (.4, .03, .4), top, .006)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .17, 0, z * .17), (x * .17, .49, z * .17), .014, "Charcoal" if k.v < 8 else "Brass", 8)
    if k.v >= 4: box((0, .18, 0), (.36, .02, .36), top, .004)
    tube((0, .515, 0), (0, .54, 0), .07, "Ivory" if k.v < 6 else "Quartz", 14)
    tube((0, .54, 0), (0, .75 * s, 0), .012, "Brass" if k.v >= 7 else "Charcoal", 8)
    tube((0, .72 * s, 0), (0, .88 * s, 0), .1 if k.v < 6 else .12, "Linen" if k.v < 8 else "Paper", 18)
    tube((0, .72 * s, 0), (0, .725 * s, 0), .09, "LampLight", 14)


def bistro_table(k):
    """A small round table for two; its top stays 0.75 m up in every version so what stands on it never moves."""
    k = R(k.v)
    top = "Ivory" if k.v < 3 else "Oak" if k.v < 6 else "Quartz" if k.v < 9 else "Walnut"
    legs = "Charcoal" if k.v < 7 else "Brass"
    tube((0, .72, 0), (0, .75, 0), .32, top, 28)
    tube((0, .04, 0), (0, .72, 0), .03, legs, 10)
    tube((0, 0, 0), (0, .04, 0), .2, legs, 20)
    stools = 1 + (k.v >= 4)
    for i in range(stools):
        a = -.9 + i * 1.8
        x, z = math.sin(a) * .42, math.cos(a) * .42
        seat = "Charcoal" if k.v < 4 else "Fabric" if k.v < 8 else "Leather"
        tube((x, .44, z), (x, .48, z), .15, seat, 18)
        tube((x, 0, z), (x, .44, z), .015, legs, 8)
        tube((x, .02, z), (x, .03, z), .12, legs, 16)


ITEMS = [coffee_machine, kettle, microwave, toaster, tv_unit, bistro_table, mug_tree, fruit_bowl, magazine_stack,
         fridge, armchair, radio, coat_stand, planter, waste_bins, shoe_rack, water_boiler, rug, massage_chair, side_lamp_table]
FOOT = {}
