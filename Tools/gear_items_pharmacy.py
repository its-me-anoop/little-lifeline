"""Pharmacy equipment: twenty pieces (see ClinicGear)."""
from gear_kit import *


def dispensing_counter(k):
    s = k.s; counter_unit(0, 0, 1.3 * s, .60, .92, k); box((0, .96, -.10), (1.0 * s, .02, .30), "Linen", .004)
    for i in range(min(2 + k.n // 2, 5)): tube((-.40 * s + i * .20 * s, .97, .10), (-.40 * s + i * .20 * s, 1.12, .10), .04, k.accent if i % 2 else "Linen", 10)
    box((.45 * s, 1.02, .05), (.26 * s, .10, .18), k.trim if k.trim != "Chrome" else "Platinum", .02); lights(-.55 * s, .80, -.305, min(k.n, 8), k.glow, .07, .012)


def pill_tray(k):
    s = k.s; counter_unit(0, 0, .60 * s, .50, .78, k); box((0, .80, 0), (.40 * s, .02, .32), "Linen", .004); box((0, .82, 0), (.36 * s, .02, .02), k.trim, .003)
    for i in range(min(3 + k.n, 12)): orb((-.14 * s + (i % 6) * .055 * s, .835, -.05 + (i // 6) * .09), (.018, .012, .018), ["Apricot", "Linen", "Blue", "Gold"][i % 4], 6)
    lights(-.22 * s, .66, -.255, min(k.n, 6), k.glow, .05, .011)


def shelving(k):
    s = k.s; box((0, .95 * s, .10), (1.0 * s, 1.9 * s, .08), k.body, .02)
    for r in range(4 + (k.v > 5)):
        y = .25 * s + r * .36 * s; box((0, y, -.03), (1.0 * s, .03, .26), k.trim if k.trim != "Chrome" else "Platinum", .006)
        for c in range(5): box((-.38 * s + c * .19 * s, y + .10, -.06), (.14 * s, .17, .12), ["Linen", k.accent, "Apricot"][(r + c) % 3], .008)
    lights(-.44 * s, 1.98 * s, -.04, min(k.n, 8), k.glow, .07, .012)


def medicine_fridge(k):
    s = k.s; box((0, .55 * s, 0), (.62 * s, 1.1 * s, .58), k.body, .03); box((0, .55 * s, -.295), (.56 * s, 1.0 * s, .02), "Glass" if k.v >= 4 else k.trim, .008)
    for i in range(3): box((0, .30 * s + i * .30 * s, -.30), (.52 * s, .015, .02), "Chrome", .003); tube((-.15 * s, .32 * s + i * .30 * s, -.32), (-.15 * s, .44 * s + i * .30 * s, -.32), .03, "Linen" if i % 2 else k.accent, 10)
    box((.24 * s, .56 * s, -.32), (.03, .40, .03), "Chrome", .006); box((0, 1.16 * s, -.30), (.16 * s, .07, .012), "Ink", .003)
    lights(-.24 * s, 1.16 * s, -.305, min(k.n, 6), k.glow, .035, .01)


def label_printer(k):
    s = k.s; counter_unit(0, 0, .50 * s, .42, .80, k); box((0, .88, 0), (.34 * s, .12, .28), k.body, .025); box((0, .86, -.15), (.22 * s, .05, .03), "Ink", .004)
    box((0, .83, -.16), (.16 * s, .02, .07), "Linen", .003); tube((-.08 * s, .96, .05), (.08 * s, .96, .05), .05, "Linen", 12)
    lights(-.14 * s, .68, -.215, min(k.n, 6), k.glow, .05, .011)


def precision_scale(k):
    s = k.s; counter_unit(0, 0, .50 * s, .42, .76, k); box((0, .79, 0), (.36 * s, .05, .30), k.trim if k.trim != "Chrome" else "Platinum", .015)
    disc(0, .83, 0, .10 * s, .02, "Chrome"); box((0, .93, 0), (.20 * s, .16 * s, .02), "Glass", .004)
    for i in range(3): box((-.10 * s + i * .10 * s, .815, -.10), (.04, .012, .04), k.glow if k.v > 3 else k.accent, .003)
    lights(-.12 * s, .64, -.215, min(k.n, 5), k.glow, .05, .01)


def drug_safe(k):
    s = k.s; box((0, .36 * s, 0), (.62 * s, .72 * s, .52), "Graphite" if k.v < 9 else k.body, .03); box((0, .36 * s, -.265), (.52 * s, .62 * s, .02), k.trim if k.trim != "Chrome" else "Platinum", .01)
    disc(.06 * s, .42 * s, -.29, .09 * s, .03, "Chrome"); disc(.06 * s, .42 * s, -.31, .03, .02, "Ink"); tube((-.16 * s, .26 * s, -.29), (-.16 * s, .52 * s, -.29), .016, "Gold", 8)
    box((0, .66 * s, -.29), (.18 * s, .05, .01), "Ink", .002); lights(-.22 * s, .62 * s, -.285, min(k.n, 6), k.glow, .04, .01)


def barcode_scanner(k):
    s = k.s; box((0, .55, 0), (.26 * s, .05, .30), k.body, .02); tube((0, .10, 0), (0, .55, 0), .035, k.trim if k.trim != "Aqua" else "Chrome"); disc(0, .03, 0, .2 * s, .05, k.body)
    box((0, .66, -.04), (.16 * s, .18, .22), k.body, .035); box((0, .66, -.152), (.12 * s, .08, .01), k.glow if k.v > 2 else "Crimson", .003)
    tube((0, .74, .04), (0, .86 * s, .06), .02, k.trim, 8); lights(-.08 * s, .30, -.02, min(k.n, 5), k.glow, .04, .01)


def mortar_pestle(k):
    s = k.s; table(0, 0, .44 * s, .44 * s, .74, k); orb((0, .86 * s, 0), (.13 * s, .10 * s, .13 * s), "Linen" if k.v < 7 else k.trim if k.trim != "Aqua" else "Platinum", 14)
    tube((.06 * s, .90 * s, 0), (.14 * s, 1.14 * s, .04), .025 * s, "Wood" if k.v < 6 else k.accent, 10); orb((.15 * s, 1.16 * s, .04), (.035, .035, .035), "Wood" if k.v < 6 else k.accent, 8)
    for i in range(min(k.n, 6)): orb((-.14 * s + i * .05 * s, .77, -.12), (.014, .01, .014), k.glow if i % 2 else "Apricot", 6)


def pill_robot(k):
    s = k.s; box((0, .85 * s, 0), (.94 * s, 1.7 * s, .72), k.body, .04); box((0, .85 * s, -.365), (.80 * s, 1.5 * s, .02), "Glass", .01)
    for r in range(4): box((0, .30 * s + r * .36 * s, -.34), (.70 * s, .03, .03), k.trim if k.trim != "Chrome" else "Platinum", .006)
    tube((-.30 * s, .20, -.30), (.30 * s, .20, -.30), .02, "Chrome", 8); box((0, .80 * s, -.35), (.12 * s, .22 * s, .05), k.accent, .012)
    screen(.30 * s, 1.50 * s, -.40, .24 * s, .16 * s, k, 12); box((0, .22, -.40), (.36 * s, .12, .10), "Ink", .008)
    lights(-.40 * s, 1.74 * s, -.37, min(k.n, 9), k.glow, .08, .012)


def blister_packer(k):
    s = k.s; counter_unit(0, 0, .80 * s, .55, .80, k); box((0, 1.0 * s, .04), (.70 * s, .36 * s, .44), k.body, .035); box((0, 1.0 * s, -.185), (.60 * s, .22 * s, .02), "Glass", .006)
    box((.0, .86, -.30), (.46 * s, .03, .10), k.trim if k.trim != "Chrome" else "Platinum", .006)
    for i in range(min(3 + k.n // 2, 8)): box((-.24 * s + i * .065 * s, .885, -.30), (.04, .012, .05), ["Linen", "Apricot", "Blue"][i % 3], .003)
    lights(-.30 * s, 1.22 * s, -.175, min(k.n, 7), k.glow, .055, .011)


def syrup_shelf(k):
    s = k.s; box((0, .70 * s, .08), (1.0 * s, 1.4 * s, .08), k.body, .02)
    for r in range(3 + (k.v > 5)):
        y = .25 * s + r * .34 * s; box((0, y, -.03), (1.0 * s, .03, .24), k.trim if k.trim != "Chrome" else "Platinum", .006)
        for c in range(6): tube((-.40 * s + c * .16 * s, y + .02, -.04), (-.40 * s + c * .16 * s, y + .22 * s, -.04), .038 * s, ["Apricot", "Blue", "Rose", "Leaf", k.accent][(r + c) % 5], 10)
    lights(-.44 * s, 1.48 * s, -.03, min(k.n, 8), k.glow, .07, .012)


def counter_display(k):
    s = k.s; box((0, .48 * s, 0), (.80 * s, .96 * s, .36), k.body, .03); box((0, .60 * s, -.185), (.70 * s, .60 * s, .02), "Glass", .008)
    for r in range(2):
        box((0, .42 * s + r * .28 * s, -.17), (.68 * s, .02, .20), k.trim if k.trim != "Chrome" else "Platinum", .004)
        for c in range(4): box((-.26 * s + c * .17 * s, .48 * s + r * .28 * s, -.17), (.11 * s, .10 * s, .09), ["Apricot", "Blue", "Linen", "Rose"][(r + c) % 4], .01)
    lights(-.30 * s, .10, -.185, min(k.n, 7), k.glow, .055, .011)


def vitamin_rack(k):
    s = k.s; pole(0, 0, 1.45 * s, .025, k.trim if k.trim != "Aqua" else "Chrome", .22)
    for r in range(2 + (k.v > 3) + (k.v > 7)):
        y = .45 * s + r * .30 * s; disc(0, y, 0, .24 * s, .03, k.body, 22)
        for c in range(6): a = c * math.tau / 6; tube((math.sin(a) * .17 * s, y, math.cos(a) * .17 * s), (math.sin(a) * .17 * s, y + .15, math.cos(a) * .17 * s), .035, ["Apricot", "Leaf", "Gold", "Blue"][(r + c) % 4], 8)
    orb((0, 1.52 * s, 0), (.05, .05, .05), k.glow if k.v > 4 else k.accent, 8)


def advice_screen(k):
    s = k.s; pole(0, 0, 1.0 * s, .028, k.trim if k.trim != "Aqua" else "Chrome", .19)
    box((0, 1.25 * s, -.04), (.50 * s, .34 * s, .05), "Ink", .015); screen(0, 1.25 * s, -.075, .44 * s, .28 * s, k, 13)
    box((0, .98 * s, -.04), (.14, .03, .10), k.trim, .006); lights(-.18 * s, .80 * s, -.03, min(k.n, 6), k.glow, .06, .011)


def ticket_machine(k):
    s = k.s; box((0, .60 * s, 0), (.38 * s, 1.2 * s, .34), k.body, .035); screen(0, 1.0 * s, -.175, .26 * s, .20 * s, k, 14)
    box((0, .72 * s, -.18), (.18 * s, .05, .03), "Ink", .004); box((0, .60 * s, -.22), (.16 * s, .02, .10), "Linen", .004); box((0, 1.24 * s, -.02), (.42 * s, .06, .38), k.trim, .012)
    lights(-.14 * s, .30 * s, -.176, min(k.n, 6), k.glow, .055, .011)


def bottle_washer(k):
    s = k.s; counter_unit(0, 0, .70 * s, .50, .84, k); box((0, .88, 0), (.54 * s, .03, .38), "Chrome", .006); box((0, .90, 0), (.46 * s, .02, .30), "Blue" if k.v < 6 else k.accent, .004)
    tube((0, .90, .16), (0, 1.14, .16), .02, "Chrome"); tube((0, 1.14, .16), (0, 1.14, .02), .018, "Chrome")
    for i in range(min(2 + k.n // 2, 5)): tube((-.18 * s + i * .09 * s, .92, -.06), (-.18 * s + i * .09 * s, 1.04 * s, -.06), .03, "Glass", 10)
    lights(-.24 * s, .70, -.255, min(k.n, 7), k.glow, .07, .011)


def herb_cabinet(k):
    s = k.s; box((0, .70 * s, 0), (.72 * s, 1.4 * s, .40), "Wood" if k.v < 5 else k.body, .03)
    for r in range(4):
        for c in range(3): box((-.22 * s + c * .22 * s, .22 * s + r * .32 * s, -.205), (.19 * s, .26 * s, .02), ["Apricot", "Wood", "Leaf"][(r + c) % 3] if k.v < 8 else k.trim, .008); box((-.22 * s + c * .22 * s, .22 * s + r * .32 * s, -.22), (.05 * s, .03, .02), "Gold", .004)
    box((0, 1.44 * s, 0), (.76 * s, .05, .44), k.trim, .012); lights(-.30 * s, 1.52 * s, -.02, min(k.n, 7), k.glow, .07, .012)


def delivery_trolley(k):
    s = k.s
    for y in (.30, .70): box((0, y, 0), (.74 * s, .04, .46), k.trim if k.trim != "Chrome" else "Platinum", .012)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .34 * s, .06, z * .20), (x * .34 * s, 1.0 * s, z * .20), .018, k.body if k.body != "Platinum" else "Chrome")
    tube((-.34 * s, 1.0 * s, -.2), (.34 * s, 1.0 * s, -.2), .018, k.body if k.body != "Platinum" else "Chrome"); wheels((-.34 * s, .34 * s), (-.2, .2), .05, .05)
    for i in range(min(2 + k.n // 2, 5)): box((-.27 * s + i * .14 * s, .78, .02), (.11 * s, .14, .15), ["Linen", k.accent, "Apricot"][i % 3], .012)
    lights(-.30 * s, .36, -.235, min(k.n, 7), k.glow, .07, .011)


def automated_cabinet(k):
    s = k.s; box((0, .90 * s, 0), (.86 * s, 1.8 * s, .55), k.body if k.body != "Ivory" else "Platinum", .04)
    for r in range(5): box((0, .30 * s + r * .30 * s, -.285), (.76 * s, .24 * s, .02), k.trim if k.trim != "Chrome" else "Graphite", .008); box((.25 * s, .30 * s + r * .30 * s, -.30), (.08 * s, .03, .02), k.glow if (k.v + r) % 2 else "Chrome", .004)
    screen(-.18 * s, 1.72 * s, -.30, .34 * s, .18 * s, k, 15); lights(.06 * s, 1.72 * s, -.29, min(k.n, 8), k.glow, .045, .012)


ITEMS = [dispensing_counter, pill_tray, shelving, medicine_fridge, label_printer, precision_scale, drug_safe, barcode_scanner, mortar_pestle, pill_robot,
         blister_packer, syrup_shelf, counter_display, vitamin_rack, advice_screen, ticket_machine, bottle_washer, herb_cabinet, delivery_trolley, automated_cabinet]

FOOT = {1: (1.3, .60, .92, 0, 0, False), 2: (.60, .50, .78, 0, 0, False), 4: (.62, .58, 1.1, 0, 0, False), 5: (.50, .42, .80, 0, 0, False), 6: (.50, .42, .76, 0, 0, False),
        7: (.62, .52, .72, 0, 0, False), 10: (.94, .72, 1.7, 0, 0, False), 11: (.80, .55, .80, 0, 0, False), 13: (.80, .36, .96, 0, 0, False), 17: (.70, .50, .84, 0, 0, False),
        18: (.72, .40, 1.4, 0, 0, False), 20: (.86, .55, 1.8, 0, 0, False)}
