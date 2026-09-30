"""Reception equipment: twenty pieces (see ClinicGear)."""
from gear_kit import *


def appointment_book(k):
    s = k.s; pole(0, 0, .95 * s, .03, k.trim if k.trim != "Aqua" else "Chrome", .17)
    box((0, .98 * s, 0), (.52 * s, .05, .38), k.trim, .012)
    box((-.11 * s, 1.02 * s, -.01), (.22 * s, .02, .32), "Linen", .004); box((.11 * s, 1.02 * s, -.01), (.22 * s, .02, .32), "Linen", .004)
    box((0, 1.025 * s, -.01), (.02, .03, .33), k.accent, .004)
    for i in range(min(k.n, 8)): box((-.2 * s + i * .05 * s, 1.045 * s, .17), (.03, .012, .05), k.glow if i % 2 else k.accent, .002)


def desk_bell(k):
    s = k.s; box((0, .30, 0), (.34 * s, .60, .30 * s), k.body, .03); box((0, .61, 0), (.38 * s, .04, .34 * s), k.trim, .01)
    disc(0, .66, 0, .10 * s, .02, "Ink"); orb((0, .72, 0), (.09 * s, .07 * s, .09 * s), "Gold" if k.v < 7 else k.glow)
    tube((0, .78, 0), (0, .83, 0), .012, "Chrome", 8)
    lights(-.12 * s, .62, -.19, min(k.n, 6), k.glow, .05, .012)


def ticket_dispenser(k):
    s = k.s; box((0, .55 * s, 0), (.32 * s, 1.1 * s, .30), k.body, .03); box((0, 1.14 * s, -.01), (.36 * s, .10, .34), k.trim, .02)
    screen(0, .95 * s, -.155, .22 * s, .16 * s, k, 1)
    box((0, .70 * s, -.16), (.16 * s, .04, .03), "Ink", .004); box((0, .62 * s, -.20), (.14 * s, .02, .10), "Linen", .004)
    lights(-.10 * s, .30 * s, -.156, min(k.n, 7), k.glow, .035, .011)


def reception_computer(k):
    s = k.s; table(0, 0, .78 * s, .50, .74, k)
    screen(0, 1.04 * s, .12, .42 * s, .30 * s, k, 2); tube((0, .76, .12), (0, .90 * s, .12), .02, "Chrome", 8)
    box((0, .775, -.10), (.36 * s, .02, .12), "Ink", .006); box((.30 * s, .55, .0), (.14, .34, .32), k.body, .03)
    lights(.27 * s, .46, -.17, min(k.n, 5), k.glow, .04, .01)


def receipt_printer(k):
    s = k.s; box((0, .32, 0), (.40 * s, .64, .36), k.trim if k.trim != "Chrome" else "Platinum", .03); box((0, .68, 0), (.46 * s, .09, .40 * s), k.body, .02)
    box((0, .75, -.07), (.20 * s, .05, .14), "Ink", .008); box((0, .80, -.12), (.14 * s, .07, .01), "Linen", .002)
    tube((-.09 * s, .72, .10), (.09 * s, .72, .10), .07, "Linen", 14)
    lights(-.12 * s, .60, -.185, min(k.n, 6), k.glow, .045, .011)


def card_reader(k):
    s = k.s; pole(0, 0, .96 * s, .028, k.trim if k.trim != "Aqua" else "Chrome", .16)
    box((0, 1.0 * s, -.02), (.16 * s, .24 * s, .05), k.body, .02); screen(0, 1.05 * s, -.055, .12 * s, .08 * s, k, 3, False)
    for r in range(3):
        for c in range(3): box((-.04 * s + c * .04 * s, .96 * s - r * .035 * s, -.052), (.025, .02, .01), "Ink", .002)
    box((0, .89 * s, -.05), (.10 * s, .012, .02), k.glow, .002); lights(-.06 * s, .78 * s, -.02, min(k.n, 5), k.glow, .03, .009)


def brochure_rack(k):
    s = k.s; pole(0, 0, 1.35 * s, .025, k.trim if k.trim != "Aqua" else "Chrome", .2)
    for r in range(2 + (k.v > 3) + (k.v > 7)):
        y = .55 * s + r * .30 * s; box((0, y, -.03), (.44 * s, .03, .16), k.body, .008)
        for c in range(3): box((-.15 * s + c * .15 * s, y + .11, -.09), (.11 * s, .19, .015), k.accent if (r + c) % 2 else "Linen", .003)
    box((0, 1.40 * s, .0), (.46 * s, .05, .18), k.trim, .01)


def wall_clock(k):
    s = k.s; disc(0, 1.85, .03, .30 * s, .05, k.trim, 28); disc(0, 1.85, -.005, .26 * s, .02, "Linen", 28)
    box((0, 1.92 * s, -.02), (.02, .13 * s, .012), "Ink", .002); box((.05 * s, 1.85, -.02), (.10 * s, .02, .012), "Ink", .002)
    for i in range(12):
        a = i * math.tau / 12; orb((math.sin(a) * .22 * s, 1.85 + math.cos(a) * .22 * s, -.02), (.012, .012, .012), k.glow if k.v >= 5 else "Ink", 6)
    lights(-.14 * s, 1.44, -.02, min(k.n, 6), k.glow, .05, .011)


def notice_board(k):
    s = k.s; box((0, 1.5, .04), (.90 * s, .66 * s, .04), "Wood", .02); box((0, 1.5, .012), (.80 * s, .56 * s, .02), "Clay", .006)
    for i in range(min(2 + k.n, 9)):
        x = -.30 * s + (i % 3) * .30 * s; y = 1.36 + (i // 3) * .17 * s
        box((x, y, -.008), (.20 * s, .15 * s, .008), "Linen" if i % 2 else k.accent, .002); orb((x, y + .06 * s, -.016), (.014, .014, .01), "Crimson", 6)
    lights(-.36 * s, 1.14, -.02, min(k.n, 6), k.glow, .06, .012)


def sanitizer_stand(k):
    s = k.s; pole(0, 0, .95 * s, .03, k.trim if k.trim != "Aqua" else "Chrome", .17)
    box((0, 1.05 * s, 0), (.16 * s, .22 * s, .12), k.body, .025); tube((0, 1.18 * s, 0), (0, 1.22 * s, -.09), .014, k.accent, 8)
    box((0, .96 * s, -.03), (.16 * s, .03, .10), "Ink", .005)
    box((0, 1.05 * s, -.062), (.10 * s, .12 * s, .008), k.accent if k.v > 1 else "Linen", .002)
    lights(-.06 * s, .78 * s, -.03, min(k.n, 5), k.glow, .03, .009)


def signin_tablet(k):
    s = k.s; pole(0, 0, .90 * s, .03, k.trim if k.trim != "Aqua" else "Chrome", .17)
    box((0, 1.05 * s, -.04), (.34 * s, .26 * s, .04), k.body, .02); screen(0, 1.05 * s, -.075, .30 * s, .22 * s, k, 4)
    tube((.19 * s, .86 * s, -.03), (.24 * s, .98 * s, -.05), .01, "Ink", 8)
    lights(-.14 * s, .74 * s, -.03, min(k.n, 6), k.glow, .04, .01)


def security_camera(k):
    s = k.s; box((0, 2.05, .05), (.14 * s, .14, .10), k.trim, .02); tube((0, 2.05, .05), (0, 2.02, -.10), .028 * s, k.body, 10)
    tube((0, 2.02, -.10), (0, 2.02, -.30 * s), .05 * s, k.body, 14); disc(0, 2.02, -.31 * s, .04 * s, .02, "Glass")
    orb((.05 * s, 2.06, -.22 * s), (.012, .012, .012), "Crimson" if k.v < 6 else k.glow, 6)
    lights(-.05 * s, 1.85, .02, min(k.n, 5), k.glow, .028, .008)


def water_cooler(k):
    s = k.s; box((0, .52 * s, 0), (.34 * s, 1.04 * s, .34), k.body, .03); box((0, .60 * s, -.175), (.28 * s, .22, .01), "Ink", .004)
    tube((0, 1.04 * s, 0), (0, 1.5 * s, 0), .14 * s, "Glass", 18); disc(0, 1.52 * s, 0, .08 * s, .03, "Blue" if k.v < 6 else k.glow)
    for x, c in ((-.07 * s, "Blue"), (.07 * s, "Crimson")): tube((x, .68 * s, -.17), (x, .68 * s, -.22), .018, c, 8)
    lights(-.12 * s, .16, -.175, min(k.n, 6), k.glow, .04, .01)


def umbrella_stand(k):
    s = k.s; tube((0, 0, 0), (0, .60 * s, 0), .17 * s, k.body, 20); disc(0, .60 * s, 0, .155 * s, .01, "Ink", 20)
    for i in range(1 + min(k.n // 2, 4)):
        x = (i - .5 * min(k.n // 2, 4)) * .07 * s; tube((x, .30, 0), (x, .95 * s, 0), .012, "Ink", 8)
        orb((x, .98 * s, 0), (.07 * s, .05 * s, .07 * s), k.accent if i % 2 else "Rose", 10)
    box((0, .34, -.171 * s), (.22 * s, .05, .01), k.trim, .003)


def magazine_table(k):
    s = k.s; disc(0, .46, 0, .34 * s, .05, k.trim if k.trim != "Chrome" else "Wood", 24)
    tube((0, 0, 0), (0, .44, 0), .03, k.body if k.body != "Platinum" else "Chrome"); disc(0, .02, 0, .2 * s, .04, k.body if k.body != "Platinum" else "Chrome", 20)
    for i in range(min(2 + k.n // 2, 6)): box((-.12 * s + i * .05 * s, .50 + i * .012, -.02 * i), (.22 * s, .012, .30 * s), k.accent if i % 2 else "Linen", .002)


def reception_planter(k):
    s = k.s; box((0, .22 * s, 0), (.46 * s, .44 * s, .30), k.trim if k.trim != "Chrome" else "Platinum", .04)
    box((0, .45 * s, 0), (.40 * s, .02, .24), "Wood", .004)
    for i in range(3 + k.v):
        a = i * 2.399; orb((math.sin(a) * .10 * s, .60 * s + (i % 3) * .10 * s, math.cos(a) * .06), (.08 * s, .17 * s, .06 * s), "Leaf", 8)
    if k.v >= 6: orb((0, 1.0 * s, 0), (.04, .04, .04), k.glow, 6)


def wall_screen(k):
    s = k.s; box((0, 1.65, .05), (.95 * s, .56 * s, .06), "Ink", .015); box((0, 1.65, .02), (.88 * s, .49 * s, .012), "Glass", .004)
    for i in range(min(k.v, 10)):
        bh = .05 * s + .30 * s * abs(math.sin(i * 1.3 + 1)) * .5; box((-.36 * s + i * .08 * s, 1.50 * s + bh / 2, .008), (.045 * s, bh, .006), k.glow, .002)
    lights(-.40 * s, 1.34, .02, min(k.n, 5), k.glow, .05, .011)


def air_purifier(k):
    tower(0, 0, .30 * k.s, .28, .92 * k.s, k)
    disc(0, .10, -.148, .05, .01, "Ink"); lights(-.09 * k.s, .18, -.15, min(k.n, 6), k.glow, .035, .009)


def cash_safe(k):
    s = k.s; box((0, .28 * s, 0), (.50 * s, .56 * s, .46), k.body if k.body != "Ivory" else "Graphite", .03)
    box((0, .28 * s, -.235), (.42 * s, .48 * s, .02), k.trim if k.trim != "Chrome" else "Platinum", .01)
    disc(.05 * s, .32 * s, -.26, .07 * s, .025, "Chrome"); disc(.05 * s, .32 * s, -.275, .025, .02, "Ink")
    tube((-.10 * s, .22 * s, -.26), (-.10 * s, .42 * s, -.26), .014, "Gold", 8); lights(-.16 * s, .48 * s, -.246, min(k.n, 6), k.glow, .04, .009)


def queue_display(k):
    s = k.s; pole(0, 0, 1.7 * s, .035, k.trim if k.trim != "Aqua" else "Chrome", .2)
    box((0, 1.85 * s, 0), (.62 * s, .34 * s, .08), "Ink", .02); box((0, 1.85 * s, -.045), (.56 * s, .28 * s, .01), "Glass", .003)
    for i in range(min(3 + k.n // 2, 8)): box((-.24 * s + i * .07 * s, 1.85 * s, -.056), (.03 * s, .14 * s * (.5 + .5 * ((i * 5) % 3) / 2), .006), k.glow, .002)
    box((0, 2.07 * s, 0), (.66 * s, .04, .09), k.trim, .01)


ITEMS = [appointment_book, desk_bell, ticket_dispenser, reception_computer, receipt_printer, card_reader, brochure_rack, wall_clock,
         notice_board, sanitizer_stand, signin_tablet, security_camera, water_cooler, umbrella_stand, magazine_table,
         reception_planter, wall_screen, air_purifier, cash_safe, queue_display]

# (width, depth, height, bottom y, centre z, hung on a wall) of each piece, for the trim every upper version adds.
FOOT = {2: (.34, .30, .60, 0, 0, False), 4: (.78, .50, .74, 0, 0, False), 5: (.40, .36, .70, 0, 0, False), 8: (.60, .06, .60, 1.55, .04, True),
        9: (.90, .04, .66, 1.17, .04, True), 13: (.34, .34, 1.04, 0, 0, False), 16: (.46, .30, .45, 0, 0, False), 17: (.95, .06, .56, 1.37, .05, True),
        19: (.50, .46, .56, 0, 0, False)}
