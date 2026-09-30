"""Waiting room equipment: twenty pieces (see ClinicGear)."""
from gear_kit import *


def reading_rack(k):
    s = k.s; box((0, .6 * s, .10), (.70 * s, 1.2 * s, .08), k.body, .02)
    for r in range(2 + (k.v > 3) + (k.v > 7)):
        y = .35 * s + r * .30 * s; box((0, y, -.02), (.70 * s, .03, .22), k.trim if k.trim != "Chrome" else "Platinum", .008)
        for c in range(4): box((-.26 * s + c * .17 * s, y + .14, -.08), (.12 * s, .24, .02), k.accent if (r + c) % 2 else "Linen", .004)
    lights(-.30 * s, 1.26 * s, .03, min(k.n, 6), k.glow, .055, .012)


def water_dispenser(k):
    s = k.s; box((0, .55 * s, 0), (.36 * s, 1.1 * s, .36), k.body, .03); box((0, .70 * s, -.185), (.26 * s, .30, .01), "Ink", .004)
    tube((0, 1.1 * s, 0), (0, 1.55 * s, 0), .15 * s, "Glass", 18); disc(0, 1.57 * s, 0, .09 * s, .03, k.accent)
    for x, c in ((-.07 * s, "Blue"), (.07 * s, "Crimson")): tube((x, .78 * s, -.185), (x, .78 * s, -.24), .018, c, 8)
    box((0, .36 * s, -.20), (.22 * s, .04, .10), "Chrome", .006); lights(-.12 * s, .16, -.185, min(k.n, 6), k.glow, .045, .01)


def coat_rack(k):
    s = k.s; pole(0, 0, 1.75 * s, .028, k.trim if k.trim != "Aqua" else "Chrome", .24)
    for a in range(4 + (k.v > 5) * 2):
        th = a * math.tau / (4 + (k.v > 5) * 2); tube((0, 1.66 * s, 0), (math.sin(th) * .20 * s, 1.74 * s, math.cos(th) * .20 * s), .014, "Chrome", 8)
        if a % 2 == 0 and a / 2 < k.n: box((math.sin(th) * .20 * s, 1.50 * s, math.cos(th) * .20 * s), (.10, .36 * s, .05), k.accent if a % 4 else "Rose", .01)
    orb((0, 1.78 * s, 0), (.04, .04, .04), k.glow if k.v > 5 else "Gold", 8)


def waiting_tv(k):
    s = k.s; table(0, 0, .95 * s, .42, .55, k, "Wood"); box((0, .58, 0), (.30, .04, .20), k.body, .01)
    screen(0, .98 * s, 0, .86 * s, .50 * s, k, 5); tube((0, .60, 0), (0, .74 * s, 0), .02, "Chrome", 8)
    lights(-.34 * s, .40, -.21, min(k.n, 6), k.glow, .06, .012)


def floor_lamp(k):
    s = k.s; pole(0, 0, 1.55 * s, .02, k.trim if k.trim != "Aqua" else "Chrome", .17)
    tube((0, 1.0 * s, 0), (0, 1.75 * s, 0), .22 * s, "Linen" if k.v < 6 else k.glow, 22)
    tube((0, 1.76 * s, 0), (0, 1.80 * s, 0), .23 * s, k.trim if k.trim != "Aqua" else "Gold", 22)
    lights(-.06 * s, .55 * s, -.03, min(k.n, 5), k.glow, .03, .009)


def toy_corner(k):
    s = k.s; box((0, .015, 0), (.90 * s, .03, .70 * s), k.accent if k.v > 1 else "Sage", .01)
    for i in range(min(3 + k.n, 10)):
        x = -.30 * s + (i % 4) * .20 * s; z = -.12 + (i // 4) * .22 * s; h = .10 + .04 * (i % 3)
        box((x, .03 + h / 2 + (i // 8) * .10, z), (.12 * s, h, .12 * s), ["Apricot", "Blue", "Rose", "Gold", "Leaf"][i % 5], .01)
    if k.v >= 6: orb((.30 * s, .2, .2), (.07, .07, .07), k.glow, 8)


def coffee_table(k):
    s = k.s; table(0, 0, .80 * s, .50 * s, .42, k, "Wood"); box((0, .22, 0), (.68 * s, .03, .38 * s), k.trim if k.trim != "Chrome" else "Platinum", .008)
    tube((-.15 * s, .44, 0), (-.15 * s, .52, 0), .045, "Linen", 10); disc(-.15 * s, .445, 0, .07, .01, "Ink"); box((.12 * s, .44, .0), (.20 * s, .02, .14 * s), k.accent, .004)
    lights(-.30 * s, .30, -.26 * s, min(k.n, 6), k.glow, .05, .011)


def magazine_stand(k):
    s = k.s; box((0, .55 * s, .06), (.56 * s, 1.1 * s, .06), k.body, .015)
    for r in range(2 + (k.v > 4) + (k.v > 8)):
        y = .30 * s + r * .28 * s; box((0, y, -.02), (.56 * s, .03, .14), k.trim if k.trim != "Chrome" else "Platinum", .006)
        for c in range(3): box((-.18 * s + c * .18 * s, y + .11, -.05), (.13 * s, .18, .012), k.accent if (r + c) % 2 else "Rose", .003)
    tube((-.26 * s, 0, .06), (-.26 * s, .30, .06), .02, k.trim); tube((.26 * s, 0, .06), (.26 * s, .30, .06), .02, k.trim)


def potted_tree(k):
    s = k.s; tube((0, 0, 0), (0, .42 * s, 0), .17 * s, "Clay" if k.v < 6 else k.trim if k.trim != "Aqua" else "Platinum", 18)
    tube((0, .30 * s, 0), (0, 1.25 * s, 0), .022, "Wood", 8)
    for i in range(4 + k.v):
        a = i * 2.399; h = 1.0 + (i % 4) * .12; orb((math.sin(a) * .17 * s, h * s, math.cos(a) * .17 * s), (.17 * s, .15 * s, .17 * s), "Leaf", 10)
    if k.v >= 7: orb((0, 1.55 * s, 0), (.05, .05, .05), k.glow, 6)


def fish_tank(k):
    s = k.s; box((0, .35, 0), (.80 * s, .70, .40), "Wood" if k.v < 5 else k.body, .03)
    box((0, .95 * s, 0), (.78 * s, .50 * s, .36), "Glass", .01); box((0, .77 * s, 0), (.70 * s, .04, .30), "Clay", .004)
    box((0, 1.22 * s, 0), (.80 * s, .04, .40), k.trim if k.trim != "Chrome" else "Platinum", .01)
    for i in range(min(2 + k.n, 8)): orb((-.28 * s + (i % 4) * .18 * s, .90 * s + (i // 4) * .14 * s, -.04 * (i % 2)), (.05 * s, .03 * s, .02 * s), "Apricot" if i % 2 else k.glow, 8)
    tube((.32 * s, .78 * s, .10), (.32 * s, 1.0 * s, .10), .015, "Leaf", 8)


def snack_shelf(k):
    s = k.s; box((0, .80 * s, .06), (.80 * s, 1.6 * s, .10), k.body, .02)
    for r in range(3 + (k.v > 4)):
        y = .30 * s + r * .34 * s; box((0, y, -.03), (.80 * s, .03, .22), k.trim if k.trim != "Chrome" else "Platinum", .006)
        for c in range(4): box((-.29 * s + c * .19 * s, y + .10, -.06), (.13 * s, .17, .09), ["Apricot", "Blue", "Gold", "Rose"][(r + c) % 4], .01)
    lights(-.34 * s, 1.68 * s, -.04, min(k.n, 6), k.glow, .06, .012)


def wall_art(k):
    s = k.s; box((0, 1.6, .04), (.86 * s, .62 * s, .05), "Wood" if k.v < 6 else "Gold", .02); box((0, 1.6, .01), (.74 * s, .50 * s, .012), "Linen", .004)
    for i in range(min(2 + k.n, 9)):
        orb((-.28 * s + (i % 5) * .14 * s, 1.52 * s + (i // 5) * .14 * s, -.005), (.05 * s, .05 * s, .012), ["Apricot", "Sage", "Blue", "Rose", "Gold"][i % 5], 8)
    lights(-.30 * s, 1.26, -.02, min(k.n, 5), k.glow, .06, .011)


def charging_station(k):
    s = k.s; box((0, .45 * s, 0), (.46 * s, .90 * s, .34), k.body, .03); box((0, .92 * s, 0), (.50 * s, .05, .38), k.trim, .012)
    for i in range(min(2 + k.n // 2, 6)): box((-.17 * s + (i % 3) * .17 * s, .50 * s + (i // 3) * .18 * s, -.175), (.10 * s, .13 * s, .01), "Ink", .003); box((-.17 * s + (i % 3) * .17 * s, .50 * s + (i // 3) * .18 * s, -.183), (.06 * s, .02, .006), k.glow, .002)
    lights(-.18 * s, .80 * s, -.175, min(k.n, 6), k.glow, .06, .011)


def standing_fan(k):
    s = k.s; pole(0, 0, 1.2 * s, .022, k.trim if k.trim != "Aqua" else "Chrome", .2)
    disc(0, 1.3 * s, -.02, .22 * s, .05, k.body, 28); disc(0, 1.3 * s, -.05, .20 * s, .01, "Glass", 28)
    for a in range(3 + (k.v > 5)):
        th = a * math.tau / (3 + (k.v > 5)); box((math.sin(th) * .09 * s, 1.3 * s + math.cos(th) * .09 * s, -.055), (.07 * s, .12 * s, .01), k.accent, .003)
    orb((0, 1.3 * s, -.06), (.03, .03, .02), "Chrome", 8); lights(-.06 * s, .7 * s, -.03, min(k.n, 5), k.glow, .03, .009)


def info_kiosk(k):
    s = k.s; box((0, .55 * s, 0), (.46 * s, 1.1 * s, .30), k.body, .03); screen(0, .95 * s, -.155, .36 * s, .38 * s, k, 6)
    box((0, .58 * s, -.16), (.34 * s, .06, .03), "Ink", .006); box((0, 1.12 * s, -.01), (.50 * s, .05, .34), k.trim, .012)
    lights(-.17 * s, .30 * s, -.156, min(k.n, 6), k.glow, .06, .011)


def kids_table(k):
    s = k.s; table(0, 0, .70 * s, .50 * s, .42, k, k.accent if k.v > 1 else "Sage")
    for i in range(2): box((-.42 * s + i * .84 * s, .15, 0), (.22 * s, .30, .22 * s), ["Apricot", "Blue"][i], .02)
    for i in range(min(2 + k.n // 2, 5)): box((-.22 * s + i * .10 * s, .44, .04 * (i % 2)), (.07 * s, .05 + .02 * (i % 3), .07 * s), ["Rose", "Gold", "Leaf", "Blue", "Apricot"][i % 5], .008)


def bookcase(k):
    s = k.s; box((0, .80 * s, .10), (.90 * s, 1.6 * s, .28), k.body, .03)
    for r in range(4):
        y = .22 * s + r * .38 * s; box((0, y, -.02), (.84 * s, .03, .24), k.trim if k.trim != "Chrome" else "Platinum", .006)
        for c in range(min(5 + k.v // 3, 8)): box((-.36 * s + c * (.72 * s / 7), y + .13 * s, -.03), (.06 * s, .20 * s * (.8 + .2 * ((c + r) % 3) / 2), .18), ["Apricot", "Blue", "Sage", "Rose", "Gold", "Linen"][(c + r) % 6], .004)
    lights(-.40 * s, 1.68 * s, -.02, min(k.n, 7), k.glow, .06, .012)


def side_sofa(k):
    s = k.s; box((0, .26, 0), (1.3 * s, .28, .62 * s), k.accent if k.v > 1 else "Sage", .07); box((0, .52 * s, .26 * s), (1.3 * s, .44 * s, .14), k.accent if k.v > 1 else "Sage", .07)
    for x in (-1, 1): box((x * .62 * s, .42, 0), (.14 * s, .30, .62 * s), k.body if k.body != "Platinum" else "Linen", .05)
    for i in range(2): box((-.30 * s + i * .6 * s, .46, -.04), (.42 * s, .10, .40 * s), "Linen" if k.v < 8 else k.glow, .03)
    for x in (-1, 1): tube((x * .55 * s, 0, -.2), (x * .55 * s, .12, -.2), .03, k.trim if k.trim != "Aqua" else "Chrome")


def room_divider(k):
    s = k.s
    for i in range(3):
        x = (i - 1) * .44 * s; box((x, .85 * s, 0), (.42 * s, 1.5 * s, .035), k.accent if (i + k.v) % 2 else k.body, .012); box((x, .85 * s, -.022), (.34 * s, 1.1 * s, .006), "Linen" if k.v < 7 else k.glow, .002)
    for x in (-.66 * s, .66 * s): tube((x, 0, 0), (x, 1.62 * s, 0), .022, k.trim if k.trim != "Aqua" else "Chrome")
    lights(-.30 * s, 1.68 * s, -.03, min(k.n, 6), k.glow, .12, .012)


def tea_trolley(k):
    s = k.s
    for y in (.45, .85): box((0, y, 0), (.72 * s, .04, .42), k.trim if k.trim != "Chrome" else "Platinum", .012)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .33 * s, .10, z * .18), (x * .33 * s, .90, z * .18), .018, k.body if k.body != "Platinum" else "Chrome")
    wheels((-.33 * s, .33 * s), (-.18, .18), .05, .05)
    tube((-.2 * s, .87, 0), (-.2 * s, 1.03 * s, 0), .06, "Linen", 12); tube((.05 * s, .87, 0), (.05 * s, .96 * s, 0), .05, k.accent, 12)
    for i in range(min(k.n, 7)): box((-.28 * s + i * .08 * s, .49, .04), (.06 * s, .05, .10), ["Rose", "Gold", "Leaf"][i % 3], .006)


ITEMS = [reading_rack, water_dispenser, coat_rack, waiting_tv, floor_lamp, toy_corner, coffee_table, magazine_stand, potted_tree, fish_tank,
         snack_shelf, wall_art, charging_station, standing_fan, info_kiosk, kids_table, bookcase, side_sofa, room_divider, tea_trolley]

FOOT = {1: (.70, .22, 1.2, 0, 0, False), 2: (.36, .36, 1.1, 0, 0, False), 7: (.80, .50, .42, 0, 0, False), 10: (.80, .40, .70, 0, 0, False), 11: (.80, .22, 1.6, 0, 0, False),
        12: (.86, .05, .62, 1.29, .04, True), 13: (.46, .34, .90, 0, 0, False), 15: (.46, .30, 1.1, 0, 0, False), 17: (.90, .28, 1.6, 0, 0, False), 18: (1.3, .62, .5, 0, 0, False),
        20: (.72, .42, .9, 0, 0, False)}
