"""Consultation room equipment: twenty pieces (see ClinicGear)."""
from gear_kit import *


def exam_couch(k):
    s = k.s; box((0, .55, 0), (.72 * s, .12, 1.85 * s), k.accent if k.v > 1 else "Linen", .04); box((0, .30, 0), (.66 * s, .40, 1.6 * s), k.body, .03)
    box((0, .74 * s, .75 * s), (.66 * s, .30, .40), k.accent if k.v > 1 else "Linen", .05)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .28 * s, 0, z * .7 * s), (x * .28 * s, .12, z * .7 * s), .03, k.trim if k.trim != "Aqua" else "Chrome")
    box((0, .62, -.92 * s), (.6 * s, .02, .14), "Linen", .004); lights(-.26 * s, .64, -.4 * s, min(k.n, 6), k.glow, .08, .012, "x")


def doctor_desk(k):
    s = k.s; counter_unit(0, 0, 1.3 * s, .65, .76, k); box((.42 * s, .36, -.02), (.40 * s, .68, .55), k.trim if k.trim != "Chrome" else "Platinum", .02)
    for i in range(3): box((.42 * s, .18 + i * .22, -.30), (.30 * s, .015, .02), "Chrome", .003)
    box((-.3 * s, .80, .02), (.26 * s, .02, .20), "Linen", .004); lights(-.55 * s, .70, -.34, min(k.n, 7), k.glow, .07, .012)


def desktop_computer(k):
    s = k.s; box((0, .36, 0), (.42 * s, .72, .40), k.body, .03); box((0, .78, 0), (.50 * s, .05, .44), k.trim, .012)
    screen(0, 1.08 * s, .10, .44 * s, .30 * s, k, 7); tube((0, .80, .10), (0, .94 * s, .10), .022, "Chrome", 8)
    box((0, .805, -.14), (.36 * s, .02, .12), "Ink", .006); lights(-.15 * s, .50, -.205, min(k.n, 6), k.glow, .05, .011)


def otoscope_set(k):
    s = k.s; box((0, 1.45, .04), (.44 * s, .52 * s, .05), k.body, .02)
    for i in range(2):
        x = (-.10 + i * .20) * s; tube((x, 1.35, -.02), (x, 1.55 * s, -.02), .028 * s, k.trim if k.trim != "Chrome" else "Platinum", 10); orb((x, 1.56 * s, -.02), (.04 * s, .03, .04 * s), k.accent, 8)
        box((x, 1.30, -.04), (.07 * s, .04, .05), "Ink", .006)
    lights(-.18 * s, 1.18, -.03, min(k.n, 8), k.glow, .05, .011)


def wall_bp_unit(k):
    s = k.s; box((0, 1.4, .05), (.44 * s, .40 * s, .10), k.body, .03); screen(0, 1.46, -.01, .30 * s, .18 * s, k, 8)
    tube((.0, 1.25, -.02), (.12 * s, 1.05 * s, -.06), .012, "Ink", 8); tube((.12 * s, 1.05 * s, -.06), (.12 * s, .92 * s, -.06), .05 * s, k.accent, 12)
    lights(-.17 * s, 1.22, -.05, min(k.n, 7), k.glow, .05, .011)


def weighing_scale(k):
    s = k.s; box((0, .04, 0), (.46 * s, .08, .46 * s), k.body if k.body != "Ivory" else "Linen", .02); box((0, .085, 0), (.40 * s, .012, .40 * s), "Ink", .004)
    pole(0, .22 * s, .95 * s, .025, k.trim if k.trim != "Aqua" else "Chrome")
    box((0, 1.0 * s, .22 * s), (.30 * s, .18 * s, .08), k.body, .02); screen(0, 1.0 * s, .18 * s, .22 * s, .12 * s, k, 9, False)
    box((0, .70 * s, .22 * s), (.02, .28 * s, .05), "Ink", .004); lights(-.10 * s, .55 * s, .18 * s, min(k.n, 5), k.glow, .05, .01)


def height_chart(k):
    s = k.s; box((0, 1.0, .03), (.30 * s, 1.8 * s, .03), k.body, .01)
    for i in range(18): box((-.05 * s, .22 + i * .085 * s, -.006), (.06 * s if i % 5 else .14 * s, .012, .006), "Ink", .002)
    box((.10 * s, 1.0, -.006), (.06 * s, 1.6 * s, .006), k.accent, .002); box((0, .28 + .05 * k.v, -.03), (.30 * s, .03, .03), k.glow, .004)


def skeleton_model(k):
    s = k.s; pole(0, 0, 1.0 * s, .022, k.trim if k.trim != "Aqua" else "Chrome", .22)
    orb((0, 1.62 * s, 0), (.085 * s, .10 * s, .09 * s), "Linen", 12); box((0, 1.4 * s, 0), (.26 * s, .30 * s, .13), "Linen", .02)
    for i in range(4): box((0, 1.52 * s - i * .07 * s, -.07), (.22 * s, .022, .02), "Ivory", .004)
    box((0, 1.12 * s, 0), (.22 * s, .10 * s, .10), "Linen", .02)
    for x in (-1, 1): tube((x * .14 * s, 1.50 * s, 0), (x * .18 * s, 1.0 * s, 0), .014, "Linen", 8); tube((x * .07 * s, 1.08 * s, 0), (x * .07 * s, .50 * s, 0), .018, "Linen", 8)
    lights(-.10 * s, .3 * s, -.03, min(k.n, 5), k.glow, .05, .01)


def xray_light_box(k):
    s = k.s; box((0, 1.5, .04), (.80 * s, .56 * s, .07), k.body if k.body != "Ivory" else "Platinum", .02); box((0, 1.5, .0), (.70 * s, .46 * s, .012), k.glow if k.v > 4 else "Linen", .004)
    orb((0, 1.52 * s, -.012), (.10 * s, .13 * s, .006), "Ink", 10); box((0, 1.40 * s, -.012), (.22 * s, .02, .006), "Ink", .002)
    for i in range(6): box((-.06 * s, 1.56 * s - i * .03, -.012), (.10 * s, .008, .006), "Ink", .002)
    lights(-.34 * s, 1.2, -.02, min(k.n, 7), k.glow, .05, .011)


def anatomy_poster(k):
    s = k.s; box((0, 1.5, .03), (.62 * s, .86 * s, .03), "Wood" if k.v < 6 else "Gold", .012); box((0, 1.5, .006), (.54 * s, .78 * s, .01), "Linen", .003)
    orb((0, 1.62 * s, -.004), (.06 * s, .07 * s, .008), k.accent, 8); box((0, 1.44 * s, -.004), (.14 * s, .30 * s, .008), k.accent, .003)
    for x in (-1, 1): box((x * .20 * s, 1.50 * s, -.004), (.05 * s, .40 * s, .008), "Rose", .002)
    lights(-.24 * s, 1.03, -.02, min(k.n, 6), k.glow, .07, .011)


def privacy_screen(k):
    s = k.s
    for i in range(3):
        x = (i - 1) * .48 * s; box((x, .90 * s, 0), (.46 * s, 1.6 * s, .035), k.accent if i % 2 == 0 and k.v > 1 else k.body, .012)
        box((x, 1.65 * s, 0), (.46 * s, .04, .045), k.trim, .008)
    for x in (-.72 * s, .72 * s): tube((x, 0, 0), (x, 1.72 * s, 0), .022, k.trim if k.trim != "Aqua" else "Chrome")
    lights(-.34 * s, 1.20 * s, -.03, min(k.n, 6), k.glow, .14, .012, "x")


def consulting_sink(k):
    s = k.s; counter_unit(0, 0, .70 * s, .50, .86, k); box((0, .87, -.02), (.42 * s, .012, .30), "Blue" if k.v < 6 else k.accent, .004)
    tube((0, .87, .18), (0, 1.12, .18), .02, "Chrome"); tube((0, 1.12, .18), (0, 1.12, .04), .018, "Chrome"); box((.30 * s, 1.0, .14), (.09, .18, .09), k.accent, .015)
    box((0, 1.55, .245), (.60 * s, .55 * s, .03), "Glass", .01); lights(-.24 * s, 1.88 * s, .23, min(k.n, 7), k.glow, .07, .011)


def doctor_stool(k):
    s = k.s; disc(0, .58 * s, 0, .22 * s, .08, k.accent if k.v > 1 else "Sage", 22); pole(0, 0, .55 * s, .03, k.trim if k.trim != "Aqua" else "Chrome", .28)
    for a in range(5): th = a * math.tau / 5; tube((0, .09, 0), (math.sin(th) * .26 * s, .03, math.cos(th) * .26 * s), .02, k.body if k.body != "Platinum" else "Chrome", 8); orb((math.sin(th) * .26 * s, .03, math.cos(th) * .26 * s), (.035, .035, .035), "Ink", 8)
    lights(-.06 * s, .18, -.26 * s, min(k.n, 5), k.glow, .03, .009)


def office_printer(k):
    s = k.s; counter_unit(0, 0, .60 * s, .48, .62, k)
    box((0, .75, 0), (.52 * s, .22 * s, .42), k.body, .03); box((0, .86 * s, .03), (.46 * s, .03, .34), "Linen", .004); box((0, .72, -.22), (.36 * s, .05, .03), "Ink", .004)
    box((0, .64, -.22), (.30 * s, .02, .10), "Linen", .004); lights(.14 * s, .80, -.215, min(k.n, 5), k.glow, .03, .009)


def filing_cabinet(k):
    s = k.s; box((0, .65 * s, 0), (.50 * s, 1.3 * s, .55), k.body if k.body != "Ivory" else "Platinum", .03)
    for i in range(4): box((0, .20 * s + i * .30 * s, -.285), (.44 * s, .26 * s, .02), k.trim if k.trim != "Chrome" else "Graphite", .01); box((0, .24 * s + i * .30 * s, -.30), (.16 * s, .02, .03), "Chrome", .004); box((0, .30 * s + i * .30 * s, -.30), (.10 * s, .05, .004), "Linen", .002)
    lights(-.20 * s, 1.36 * s, -.28, min(k.n, 7), k.glow, .055, .011)


def exam_light(k):
    s = k.s; pole(0, 0, 1.6 * s, .02, k.trim if k.trim != "Aqua" else "Chrome", .2)
    tube((0, 1.6 * s, 0), (0, 1.66 * s, -.30 * s), .018, k.trim if k.trim != "Aqua" else "Chrome"); r = .10 + .012 * k.v
    disc(0, 1.66 * s, -.34 * s, r, .05, k.body, 24); disc(0, 1.635 * s, -.34 * s, r * .8, .012, k.glow if k.v > 2 else "Linen", 24)
    for i in range(min(k.n, 9)): a = i * math.tau / 9; orb((math.sin(a) * r * .65, 1.63 * s, -.34 * s + math.cos(a) * r * .65), (.012, .012, .012), k.glow, 6)


def ecg_monitor(k):
    s = k.s; box((0, .55, 0), (.50 * s, .06, .44), k.body, .02); tube((0, .10, 0), (0, .55, 0), .03, "Chrome"); wheels((-.18 * s, .18 * s), (-.15, .15), .05)
    box((0, .30, .12), (.40 * s, .40, .14), k.trim if k.trim != "Chrome" else "Platinum", .02)
    screen(0, .92 * s, .12, .42 * s, .32 * s, k, 10); lights(-.20 * s, .66, -.10, min(k.n, 8), k.glow, .05, .012)
    tube((.24 * s, .58, -.05), (.32 * s, .34, -.16), .012, "Ink", 8)


def ultrasound_cart(k):
    s = k.s; box((0, .70 * s, 0), (.50 * s, .60 * s, .50), k.body, .04); disc(0, .10, 0, .03, .18, "Chrome", 10); wheels((-.20 * s, .20 * s), (-.18, .18), .05)
    box((0, .10, 0), (.50 * s, .05, .50), k.trim if k.trim != "Chrome" else "Platinum", .02); screen(0, 1.22 * s, .10, .44 * s, .30 * s, k, 11)
    tube((0, 1.0 * s, .10), (0, 1.1 * s, .10), .02, "Chrome", 8); tube((.26 * s, .86 * s, -.1), (.36 * s, .70 * s, -.22), .012, "Ink", 8); orb((.36 * s, .68 * s, -.23), (.035, .05, .03), k.accent, 8)
    lights(-.20 * s, .52, -.26, min(k.n, 7), k.glow, .05, .011)


def nebulizer(k):
    s = k.s; box((0, .36, 0), (.36 * s, .72, .34), k.body, .03); box((0, .38, -.175), (.26 * s, .34, .01), "Ink", .004)
    box((0, .74, 0), (.38 * s, .05, .36), k.trim, .012); tube((.12 * s, .78, -.02), (.12 * s, .98 * s, -.02), .03 * s, "Glass", 12)
    tube((.12 * s, .98 * s, -.02), (.26 * s, .86 * s, -.10), .014, k.accent, 8); orb((.28 * s, .84 * s, -.11), (.05 * s, .04, .04), k.accent, 8)
    lights(-.12 * s, .22, -.18, min(k.n, 7), k.glow, .04, .01)


def laptop_cart(k):
    s = k.s; box((0, .92, 0), (.56 * s, .04, .42), k.trim if k.trim != "Chrome" else "Platinum", .012)
    for x in (-1, 1):
        for z in (-1, 1): tube((x * .25 * s, .06, z * .17), (x * .25 * s, .90, z * .17), .018, k.body if k.body != "Platinum" else "Chrome")
    wheels((-.25 * s, .25 * s), (-.17, .17), .05, .05)
    box((0, .96, .02), (.40 * s, .02, .28), "Ink", .006); box((0, 1.10 * s, .18), (.40 * s, .28 * s, .02), "Ink", .008); box((0, 1.10 * s, .168), (.34 * s, .22 * s, .006), "Glass", .003)
    box((0, .50, 0), (.46 * s, .03, .34), k.trim, .008); lights(-.20 * s, .96, -.20, min(k.n, 6), k.glow, .05, .011)


ITEMS = [exam_couch, doctor_desk, desktop_computer, otoscope_set, wall_bp_unit, weighing_scale, height_chart, skeleton_model, xray_light_box, anatomy_poster,
         privacy_screen, consulting_sink, doctor_stool, office_printer, filing_cabinet, exam_light, ecg_monitor, ultrasound_cart, nebulizer, laptop_cart]

FOOT = {1: (.72, 1.85, .60, 0, 0, False), 2: (1.3, .65, .76, 0, 0, False), 3: (.42, .40, .72, 0, 0, False), 6: (.46, .46, .10, 0, 0, False), 12: (.70, .50, .86, 0, 0, False),
        14: (.60, .48, .62, 0, 0, False), 15: (.50, .55, 1.3, 0, 0, False), 18: (.50, .50, .70, 0, 0, False), 19: (.36, .34, .72, 0, 0, False)}
