"""First aid equipment: twenty pieces (see ClinicGear)."""
from gear_kit import *


def first_aid_kit(k):
    s = k.s
    box((0, 1.45, .06), (.52 * s, .40 * s, .14), k.body, .03)
    box((0, 1.45, -.02), (.40 * s, .28 * s, .02), k.trim, .01)
    box((0, 1.45, -.035), (.20, .06, .015), "Crimson", .004); box((0, 1.45, -.035), (.06, .20, .015), "Crimson", .004)
    box((0, 1.66, .06), (.16, .03, .06), k.trim, .01)
    lights(-.20, 1.30, -.03, min(k.n, 9), k.glow, .05)
    if k.v >= 5: box((0, 1.20, .03), (.40 * s, .05, .10), k.trim, .01)  # a spare-supplies shelf beneath


def bandage_rack(k):
    s = k.s; box((0, 1.40, .05), (.70 * s, .70 * s, .06), k.body, .02)
    rows = 2 + (k.v >= 5) + (k.v >= 8)
    for r in range(rows):
        y = 1.15 + r * .24; box((0, y, -.04), (.70 * s, .03, .13), k.trim, .01)
        for i in range(3 + (k.v > 3) + (k.v > 6)):
            x = -.27 * s + i * (.54 * s / max(1, 2 + (k.v > 3) + (k.v > 6)))
            tube((x, y + .02, -.045), (x, y + .17, -.045), .045, "Linen" if (i + r) % 2 else k.accent, 10)
    lights(-.28, 1.78, -.02, min(k.n, 6), k.glow, .08, .015)


def exam_lamp(k):
    s = k.s; tube((0, .03, 0), (0, 1.55 * s, 0), .022, k.trim); tube((0, .03, 0), (0, .05, 0), .22, k.body)
    tube((0, 1.55 * s, 0), (0, 1.62 * s, -.32 * s), .02, k.trim); r = .12 + .01 * k.v
    orb((0, 1.60 * s, -.36 * s), (r * 1.2, r * .55, r * 1.2), k.body)
    orb((0, 1.545 * s, -.36 * s), (r * .8, r * .12, r * .8), k.glow)
    for i in range(min(k.n, 9)):
        a = i * math.tau / 9; orb((math.sin(a) * r * .85, 1.55 * s, -.36 * s + math.cos(a) * r * .85), (.014, .014, .014), k.glow, 6)


def bp_monitor(k):
    s = k.s; tube((0, .03, 0), (0, 1.30, 0), .025, k.trim); tube((0, .03, 0), (0, .06, 0), .25, k.body)
    box((0, 1.30, -.05), (.34 * s, .22 * s, .10), k.body, .03); box((0, 1.30, -.105), (.26 * s, .15 * s, .01), "Glass", .004)
    for i in range(min(k.v, 10)): box((-.11 * s + i * .022 * s, 1.26 + .04 * math.sin(i * .9), -.114), (.012, .03 + .012 * (i % 4), .004), k.glow, .002)
    tube((.12, 1.20, -.06), (.32, 1.02, -.20), .012, "Ink", 8); tube((.32, 1.0, -.20), (.32, .84, -.20), .05, k.accent, 12)


def thermometer_station(k):
    s = k.s; box((0, .45, 0), (.50 * s, .90, .40), k.trim, .03); box((0, .93, 0), (.54 * s, .05, .44), k.body, .02)
    box((0, 1.12, .02), (.22 * s, .30, .18), k.body, .03); box((0, 1.18, -.075), (.14 * s, .09, .01), "Glass", .003)
    for i in range(min(k.v, 5)): tube((-.16 * s + i * .08 * s, .95, -.10), (-.16 * s + i * .08 * s, 1.10, -.10), .014, k.accent if i % 2 else "Chrome", 8)
    lights(-.16 * s, 1.07, -.077, min(k.n, 5), k.glow, .08, .011)


def sterilizer(k):
    s = k.s; box((0, .62 * s, 0), (.62 * s, .60 * s, .48), k.body, .03); box((0, .34, 0), (.66 * s, .06, .52), k.trim, .02)
    box((-.05, .64 * s, -.245), (.40 * s, .38 * s, .012), "Glass", .006)
    box((.22 * s, .64 * s, -.245), (.09, .38 * s, .015), k.trim, .005)
    lights(.22 * s, .50 * s, -.26, min(k.n, 8), k.glow, .045, .012, "y")
    if k.v >= 4: tube((-.20 * s, 1.0 * s, -.02), (.20 * s, 1.0 * s, -.02), .022, "Chrome")


def dressing_trolley(k):
    s = k.s
    for y in (.55, .95): box((0, y, 0), (.72 * s, .045, .46), k.body, .02)
    for x in (-.33 * s, .33 * s):
        for z in (-.2, .2): tube((x, .12, z), (x, 1.15, z), .018, k.trim)
    wheels((-.33 * s, .33 * s), (-.2, .2), .06)
    box((-.2, 1.0, 0), (.16, .04, .12), "Linen", .01); tube((.1, 1.0, 0), (.1, 1.18, 0), .04, k.accent, 10)
    for i in range(min(k.v, 8)): box((-.28 * s + i * .075 * s, .60, -.05), (.05, .04 + .01 * (i % 3), .08), k.accent if i % 2 else "Linen", .006)
    if k.v >= 6: box((0, 1.16, .21), (.72 * s, .05, .03), k.trim, .008)


def wash_basin(k):
    s = k.s; box((0, .42, 0), (.66 * s, .84, .50), k.body, .03); box((0, .86, 0), (.72 * s, .05, .56), k.trim, .02)
    box((0, .885, -.02), (.42 * s, .01, .30), "Blue" if k.v < 6 else k.accent, .004); tube((0, .88, .18), (0, 1.12, .18), .02, "Chrome")
    tube((0, 1.12, .18), (0, 1.12, .04), .018, "Chrome")
    box((0, 1.55, .245), (.56 * s, .55 * s, .03), "Glass" if k.v >= 5 else "Blue", .01)
    for i in range(min(k.n, 8)): orb((-.24 * s + i * .07 * s, 1.86 * s, .225), (.016, .016, .016), k.glow, 6)
    if k.v >= 3: box((.34 * s, 1.05, .10), (.09, .20, .09), k.accent, .015)


def oxygen_cylinder(k):
    s = k.s; tube((0, .16, 0), (0, .95 * s, 0), .10, k.accent if k.v > 2 else "Sage"); orb((0, .98 * s, 0), (.10, .07, .10), k.accent if k.v > 2 else "Sage")
    tube((0, 1.0 * s, 0), (0, 1.10 * s, 0), .035, "Chrome"); orb((0, 1.14 * s, -.05), (.07, .07, .03), "Linen"); orb((0, 1.14 * s, -.075), (.05, .05, .01), k.glow if k.v > 4 else "Ink")
    box((0, .10, 0), (.34, .05, .34), k.trim if k.trim != "Chrome" else "Graphite", .01)
    wheels((-.17, .17), (0,), .07, .07)
    tube((-.13, .42, -.10), (.13, .42, -.10), .012, k.trim); lights(-.05, .60 * s, -.101, min(k.n, 3), k.glow, .05, .012)


def defibrillator(k):
    s = k.s; box((0, 1.35, .06), (.48 * s, .42 * s, .16), k.body, .03); box((0, 1.35, -.03), (.40 * s, .32 * s, .02), k.trim if k.v < 8 else "Graphite", .01)
    box((0, 1.38, -.05), (.20, .12, .012), "Glass", .004); box((0, 1.38, -.058), (.14, .008, .004), k.glow, .002)
    orb((0, 1.24, -.05), (.05, .05, .02), "Crimson", 8); tube((-.14, 1.22, -.03), (-.14, 1.36, -.03), .018, "Ink", 8); tube((.14, 1.22, -.03), (.14, 1.36, -.03), .018, "Ink", 8)
    lights(-.20 * s, 1.55 * s, -.03, min(k.n, 9), k.glow, .045, .012)
    if k.v >= 5: box((0, 1.60 * s, .06), (.30, .05, .10), "Crimson", .01)


def pulse_oximeter(k):
    s = k.s; tube((0, .03, 0), (0, .85, 0), .02, k.trim); tube((0, .03, 0), (0, .05, 0), .2, k.body)
    box((0, .95, 0), (.30 * s, .22 * s, .12), k.body, .03); box((0, .96, -.062), (.22 * s, .13 * s, .01), "Glass", .003)
    for i in range(min(k.v, 10)): box((-.10 * s + i * .022 * s, .94 + .05 * abs(math.sin(i * .7)), -.069), (.012, .012, .004), k.glow, .002)
    tube((.12, .90, -.03), (.30, .78, -.15), .01, "Ink", 8); box((.30, .76, -.16), (.08, .05, .05), k.accent, .012)


def stethoscope_wall(k):
    s = k.s; box((0, 1.35, .04), (.56 * s, .62 * s, .04), k.body, .02); box((0, 1.66 * s, .03), (.56 * s, .04, .06), k.trim, .01)
    for i in range(1 + min(k.v, 4) // 2 + (k.v > 6)):
        x = (-.18 + i * .18) * s; tube((x, 1.62 * s, -.01), (x, 1.62 * s, -.05), .014, "Chrome", 8)
        for a in range(10): th = a / 9 * math.pi; orb((x + math.sin(th) * .09, 1.55 * s - (1 - math.cos(th)) * .20, -.05), (.012, .012, .012), "Ink", 6)
        orb((x, 1.29 * s, -.05), (.04, .04, .018), "Chrome" if k.v > 3 else "Gold", 10)
    lights(-.24 * s, 1.10, -.02, min(k.n, 9), k.glow, .06, .013)


def medicine_cabinet(k):
    s = k.s; box((0, .95 * s, 0), (.72 * s, 1.7 * s, .36), k.body, .03)
    for x in (-.18 * s, .18 * s): box((x, .95 * s, -.185), (.31 * s, 1.5 * s, .02), "Glass" if k.v >= 4 else k.trim, .008)
    for y in (.45, .80, 1.15, 1.5):
        box((0, y * s, -.01), (.66 * s, .02, .30), k.trim if k.trim != "Chrome" else "Platinum", .005)
        for i in range(2 + (k.v > 3) + (k.v > 7)): tube((-.24 * s + i * .16 * s, y * s + .02, -.02), (-.24 * s + i * .16 * s, y * s + .14, -.02), .035, k.accent if i % 2 else "Linen", 10)
    lights(-.30 * s, 1.85 * s, -.185, min(k.n, 9), k.glow, .07, .014)


def cold_fridge(k):
    s = k.s; box((0, .48 * s, 0), (.62 * s, .92 * s, .56), k.body, .03); box((0, .48 * s, -.285), (.56 * s, .84 * s, .02), k.trim if k.trim != "Chrome" else "Platinum", .008)
    box((.20 * s, .58 * s, -.31), (.03, .34, .03), "Chrome", .006); box((0, .80 * s, -.30), (.16 * s, .07, .012), "Glass", .003)
    for i in range(min(k.n, 8)): box((-.06 * s + i * .022 * s, .80 * s, -.308), (.012, .028, .004), k.glow, .002)
    if k.v >= 5: box((0, .95 * s, 0), (.66 * s, .04, .60), k.trim, .015)


def splint_rack(k):
    s = k.s; box((0, 1.05, .04), (.80 * s, 1.2 * s, .04), k.body, .02)
    for y in (.75, 1.25): box((0, y * s, -.04), (.80 * s, .04, .12), k.trim, .01)
    for i in range(2 + (k.v > 4) + (k.v > 8)):
        x = (-.30 + i * .60 / max(1, 1 + (k.v > 4) + (k.v > 8))) * s
        box((x, 1.02 * s, -.055), (.07, .40, .03), k.accent if i % 2 else "Linen", .008)
    tube((-.33 * s, .05, -.10), (-.30 * s, 1.15, -.10), .022, "Wood" if k.v < 5 else "Chrome"); tube((.33 * s, .05, -.10), (.30 * s, 1.15, -.10), .022, "Wood" if k.v < 5 else "Chrome")
    lights(-.30 * s, 1.72 * s, -.02, min(k.n, 9), k.glow, .07, .013)


def wheelchair(k):
    s = k.s; box((0, .52, 0), (.46 * s, .06, .44), k.accent if k.v > 2 else "Blue", .03); box((0, .82, .21), (.46 * s, .52, .05), k.accent if k.v > 2 else "Blue", .03)
    for x in (-.25 * s, .25 * s):
        tube((x, .28, .02), (x, .70, .02), .016, k.trim); tube((x, .72, .22), (x, .72, -.16), .018, k.trim); box((x, .74, -.04), (.045, .04, .28), "Ink", .01)
        tube((x - .025, .30, .18), (x + .025, .30, .18), .30, "Tyre", 22); tube((x - .03, .30, .18), (x + .03, .30, .18), .26, k.trim if k.v > 3 else "Chrome", 20)
        tube((x - .02, .13, -.20), (x + .02, .13, -.20), .06, "Tyre", 12)
        tube((x, .25, -.20), (x, .48, -.20), .016, k.trim)
    tube((-.25 * s, 1.08, .22), (.25 * s, 1.08, .22), .02, k.trim)
    lights(-.14 * s, .58, -.22, min(k.n, 9), k.glow, .035, .011)


def stretcher(k):
    s = k.s; box((0, .80, 0), (.68 * s, .09, 1.70 * s), k.body, .03); box((0, .875, .55), (.68 * s, .07, .50 * s), k.accent if k.v > 2 else "Blue", .025)
    for x in (-.28 * s, .28 * s):
        for z in (-.75 * s, .75 * s): tube((x, .12, z), (x, .76, z), .022, k.trim)
        tube((x, .95, -.70 * s), (x, .95, .70 * s), .016, k.trim if k.trim != "Aqua" else "Chrome")
    wheels((-.28 * s, .28 * s), (-.75 * s, .75 * s), .07)
    for x in (-.34 * s, .34 * s): box((x, .86, 0), (.03, .05, 1.5 * s), "Chrome", .008)
    lights(-.24 * s, .84, -.86 * s, min(k.n, 9), k.glow, .06, .013)


def iv_stand(k):
    s = k.s; tube((0, .06, 0), (0, 1.95 * s, 0), .018, k.trim if k.trim != "Aqua" else "Chrome"); tube((0, .08, 0), (0, .10, 0), .30, k.body)
    for a in range(5): th = a * math.tau / 5; tube((0, .10, 0), (math.sin(th) * .28, .06, math.cos(th) * .28), .014, "Chrome", 8)
    for i in range(1 + (k.v > 3) + (k.v > 7)):
        x = (i - .5 * ((k.v > 3) + (k.v > 7))) * .22; tube((x, 1.92 * s, 0), (x, 1.98 * s, 0), .012, "Chrome", 8)
        box((x, 1.78 * s, -.03), (.13, .25, .05), "Glass" if k.v >= 3 else "Blue", .02); box((x, 1.72 * s, -.058), (.09, .12, .006), k.accent, .003)
    box((0, 1.25 * s, -.06), (.20 * s, .22, .10), k.body, .025); box((0, 1.27 * s, -.112), (.14 * s, .09, .008), "Glass", .003)
    lights(-.07 * s, 1.17 * s, -.113, min(k.n, 6), k.glow, .028, .009)


def ecg_cart(k):
    s = k.s; box((0, .55, 0), (.52 * s, .06, .44), k.body, .02); box((0, .30, .14), (.44 * s, .50, .14), k.trim if k.trim != "Chrome" else "Platinum", .02)
    tube((0, .10, 0), (0, .55, 0), .03, "Chrome"); wheels((-.18 * s, .18 * s), (-.15, .15), .05)
    box((0, .88, .12), (.40 * s, .30 * s, .04), k.body, .02); box((0, .88, .092), (.34 * s, .24 * s, .012), "Glass", .003)
    for i in range(10): box((-.15 * s + i * .033 * s, .88 + .05 * math.sin(i * 1.2 + k.v), .084), (.018, .012 + .02 * (i % 3), .004), k.glow if i < k.v else "Ink", .002)
    box((0, .60, -.12), (.30 * s, .04, .14), "Linen", .01); tube((.26 * s, .58, -.10), (.34 * s, .30, -.20), .012, "Ink", 8)
    lights(-.20 * s, .68, -.10, min(k.n, 9), k.glow, .05, .012)


def scanner(k):
    s = k.s; box((0, .12, 0), (.60 * s, .16, .60), k.body, .04); wheels((-.24 * s, .24 * s), (-.24, .24), .055, .055)
    tube((0, .18, .1), (0, 1.15 * s, .1), .05, k.trim if k.trim != "Aqua" else "Chrome")
    tube((0, 1.15 * s, .1), (0, 1.25 * s, -.20 * s), .035, k.trim); box((0, 1.28 * s, -.27 * s), (.28 * s, .10, .22), k.body, .03)
    box((0, 1.24 * s, -.385 * s), (.22 * s, .02, .02), k.glow if k.v >= 3 else "Linen", .004)
    box((.22 * s, .80 * s, .12), (.30 * s, .26 * s, .04), k.body, .02); box((.22 * s, .80 * s, .095), (.25 * s, .20 * s, .012), "Glass", .003)
    for i in range(min(k.v, 10)): box((.115 * s + i * .022 * s, .78 * s + .04 * math.sin(i * .8), .086), (.012, .02, .004), k.glow, .002)
    if k.v >= 7: tube((-.12, 1.20 * s, .1), (-.26 * s, .95 * s, -.05), .015, "Ink", 8); orb((-.26 * s, .93 * s, -.06), (.04, .04, .04), k.trim, 8)


ITEMS = [first_aid_kit, bandage_rack, exam_lamp, bp_monitor, thermometer_station, sterilizer, dressing_trolley, wash_basin,
         oxygen_cylinder, defibrillator, pulse_oximeter, stethoscope_wall, medicine_cabinet, cold_fridge, splint_rack,
         wheelchair, stretcher, iv_stand, ecg_cart, scanner]

# (width, depth, height, bottom y, centre z, hung on a wall) of each piece, for the trim every upper version adds.
FOOT = {1: (.52, .14, .40, 1.25, .06, True), 2: (.70, .06, .70, 1.05, .05, True), 5: (.50, .40, 1.0, 0, 0, False),
        6: (.62, .48, .95, 0, 0, False), 7: (.72, .46, 1.15, 0, 0, False), 8: (.66, .50, .90, 0, 0, False),
        10: (.48, .16, .42, 1.15, .06, True), 12: (.56, .04, .62, 1.05, .04, True), 13: (.72, .36, 1.7, 0, 0, False),
        14: (.62, .56, .92, 0, 0, False), 15: (.80, .04, 1.2, .45, .04, True), 16: (.50, .60, 1.0, 0, 0, False),
        17: (.70, 1.70, .90, 0, 0, False), 19: (.52, .44, .60, 0, 0, False), 20: (.60, .60, .30, 0, 0, False)}
