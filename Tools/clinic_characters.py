"""Little Lifeline's in-game characters: Patient, Receptionist and Nurse (4.3 (23) art pass).

Each character is one continuous, smoothly skinned body on the classic 18-bone clinic skeleton: lofted torso, limbs and
neck that bend at the joints instead of separate rigid pieces, a sculpted head with eye whites, irises, brows, nose,
lips and ears, a hair cap the game tints per visitor, role clothing (hoodie, blazer, scrubs) and proper shoes.

What stays exactly as before, because sockets, seats, runtime offsets and tests rely on it: bone names, hierarchy and
rest positions, the six clip names, the torso front surface (staff badges sit on it), the seated pelvis height and the
walk's 1.08 m stride per cycle. Runs inside Blender via Tools/create_clinic_assets.py; nothing here opens a GUI scene.

Mobile budget: about 4,000 vertices and at most 10 materials per character, all flat colours (no textures).
"""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


def u(p):
    """Unity game frame (x right, y up, z forward) to Blender, matching create_clinic_assets.u."""
    return Vector((-p[0], -p[2], p[1]))


def _v(p):
    return Vector(p)


# ---------------------------------------------------------------------------------------------------------------- skeleton
# The classic rest pose (create_clinic_assets.action_points("Idle", 0)). Never change these numbers.
def _classic_rest():
    p = {"root": (0, 0, 0), "pelvis": (0, .74, 0), "spine": (0, .93, 0), "chest": (0, 1.10, 0), "neck": (0, 1.24, 0),
         "head": (0, 1.38, 0), "head_tip": (0, 1.53, 0)}
    for side, label in ((-1, "L"), (1, "R")):
        hand = (side * .285, .64, .015)
        p.update({"shoulder." + label: (side * .225, 1.10, 0), "elbow." + label: (side * .27, .87, 0), "hand." + label: hand,
                  "hand_tip." + label: (hand[0], hand[1] - .05, hand[2] + .05), "hip." + label: (side * .115, .73, 0),
                  "knee." + label: (side * .115, .39, .015), "ankle." + label: (side * .115, .09, .015),
                  "toe." + label: (side * .115, .07, .18)})
    return p


REST = _classic_rest()
LINKS = [("root", "root", "pelvis", None), ("pelvis", "pelvis", "spine", "root"), ("spine", "spine", "chest", "pelvis"),
         ("chest", "chest", "neck", "spine"), ("neck", "neck", "head", "chest"), ("head", "head", "head_tip", "neck")]
for _side in ("L", "R"):
    LINKS += [("upper_arm." + _side, "shoulder." + _side, "elbow." + _side, "chest"),
              ("forearm." + _side, "elbow." + _side, "hand." + _side, "upper_arm." + _side),
              ("hand." + _side, "hand." + _side, "hand_tip." + _side, "forearm." + _side),
              ("upper_leg." + _side, "hip." + _side, "knee." + _side, "pelvis"),
              ("lower_leg." + _side, "knee." + _side, "ankle." + _side, "upper_leg." + _side),
              ("foot." + _side, "ankle." + _side, "toe." + _side, "lower_leg." + _side)]
ACTIONS = ("Idle", "Walk", "CheckIn", "Treat", "Sit", "Call")
UPPER_ARM = (Vector(REST["elbow.R"]) - Vector(REST["shoulder.R"])).length
FOREARM = (Vector(REST["hand.R"]) - Vector(REST["elbow.R"])).length
THIGH = (Vector(REST["knee.R"]) - Vector(REST["hip.R"])).length
SHIN = (Vector(REST["ankle.R"]) - Vector(REST["knee.R"])).length
STRIDE = .27  # half of the 0.54 m step; WalkCycleLength (1.08 m) in ClinicActors is two steps


def _two_bone(start, end, first, second, pole):
    """Middle joint of a two-bone chain reaching from start towards end, bending towards pole. Lengths are kept;
    a target out of reach straightens the chain and leaves the end joint where it was asked to be."""
    start, end, pole = Vector(start), Vector(end), Vector(pole)
    axis = end - start; d = axis.length
    if d < 1e-6:
        return tuple(start + pole.normalized() * first)
    axis.normalize()
    side = pole - axis * pole.dot(axis)
    side = side.normalized() if side.length > 1e-6 else Vector((0, 0, 1))
    if d >= first + second:
        return tuple(start + axis * (first * d / (first + second)))
    a = (first * first - second * second + d * d) / (2 * d)
    h = math.sqrt(max(0.0, first * first - a * a))
    return tuple(start + axis * a + side * h)


def _smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def pose(kind, t):
    """Joint positions for a clip at t in [0, 1). Same keys as REST."""
    p = {k: tuple(v) for k, v in REST.items()}
    phase = t * math.tau

    def move(keys, dx=0.0, dy=0.0, dz=0.0):
        for k in keys:
            x, y, z = p[k]; p[k] = (x + dx, y + dy, z + dz)

    upper = ["spine", "chest", "neck", "head", "head_tip", "shoulder.L", "shoulder.R", "elbow.L", "elbow.R",
             "hand.L", "hand.R", "hand_tip.L", "hand_tip.R"]

    def arm(label, hand_target, pole):
        side = -1 if label == "L" else 1
        shoulder = p["shoulder." + label]
        elbow = _two_bone(shoulder, hand_target, UPPER_ARM, FOREARM, pole)
        direction = (Vector(hand_target) - Vector(elbow)).normalized()
        hand = Vector(elbow) + direction * FOREARM
        tip = hand + direction * .045 + Vector((0, 0, .045)) * (1 if direction.z < .5 else 0)
        p["elbow." + label] = tuple(elbow); p["hand." + label] = tuple(hand); p["hand_tip." + label] = tuple(tip)

    def leg(label, ankle, toe):
        hip = p["hip." + label]
        knee = _two_bone(hip, ankle, THIGH, SHIN, (0, 0, 1))
        p["knee." + label] = knee; p["ankle." + label] = ankle; p["toe." + label] = toe

    if kind == "Idle":
        breath = math.sin(phase); shift = math.sin(phase + .7)
        move(["pelvis"] + upper + ["hip.L", "hip.R"], dx=.006 * shift)
        move(upper, dy=.004 + .004 * breath)
        move(["chest", "neck", "head", "head_tip"], dz=.003 * breath)
        move(["head_tip"], dx=.008 * math.sin(phase * 2 + .3), dz=.004 * math.sin(phase))
        for label, side in (("L", -1), ("R", 1)):
            shoulder = Vector(p["shoulder." + label])
            arm(label, (shoulder.x + side * .055, shoulder.y - .44 + .004 * breath, .035), (side * .3, -.2, -1))
    elif kind == "Walk":
        s_left = t % 1.0
        bob = -.011 * math.cos(4 * math.pi * s_left)
        sway = .010 * math.sin(phase)
        move(["pelvis", "hip.L", "hip.R"] + upper, dx=sway, dy=bob)
        move(["chest", "neck", "shoulder.L", "shoulder.R"], dz=.018)
        move(["head", "head_tip"], dz=.024)
        for label, side, offset in (("L", -1, 0.0), ("R", 1, .5)):
            s = (t + offset) % 1.0
            x = side * .115 + sway * .3
            if s < .5:  # stance: the foot stays planted while the body travels 0.54 m
                z = STRIDE - 2 * STRIDE * (s / .5)
                heel = .05 * _smooth((s - .36) / .14)            # heel rises before toe-off
                toe_up = .035 * (1 - _smooth(s / .10))           # toes come down after heel strike
                ankle = (x, .09 + heel, z); toe = (x, .07 + toe_up - heel * .1, z + .165 - heel * .6)
            else:       # swing: the foot lifts and passes under the body
                w = (s - .5) / .5
                z = -STRIDE + 2 * STRIDE * _smooth(w)
                lift = .095 * math.sin(math.pi * w)
                ankle = (x, .09 + lift + .03 * (1 - w), z); toe = (x, .07 + lift * .7 + .01, z + .165)
            leg(label, ankle, toe)
            # The opposite leg's travel drives this arm: forward arm with the opposite foot.
            other = (t + offset + .5) % 1.0
            reach = (STRIDE - 2 * STRIDE * (other / .5)) / STRIDE if other < .5 else (-1 + 2 * _smooth((other - .5) / .5))
            swing = .32 * reach
            shoulder = Vector(p["shoulder." + label])
            p["shoulder." + label] = (shoulder.x, shoulder.y, shoulder.z + .022 * reach)
            shoulder = Vector(p["shoulder." + label])
            bend = .22 + .24 * max(0.0, reach)
            elbow = shoulder + Vector((side * .035, -UPPER_ARM * math.cos(swing), UPPER_ARM * math.sin(swing)))
            hand = elbow + Vector((side * .012, -FOREARM * math.cos(swing + bend), FOREARM * math.sin(swing + bend)))
            arm(label, tuple(hand), (side * .2, -.3, -1))
    elif kind in ("CheckIn", "Treat"):
        move(["chest", "neck", "shoulder.L", "shoulder.R"], dz=.025)
        move(["head", "head_tip"], dz=.04, dy=-.006)
        move(["head_tip"], dz=.012 * math.sin(phase), dx=.010 * math.sin(phase * .5 * 2 + 1))
        for label, side in (("L", -1), ("R", 1)):
            target = (side * .18, 1.03 + math.sin(phase + side) * .025, .39 + math.sin(phase) * .025)
            arm(label, target, (side * 1, -1, -.4))
    elif kind == "Sit":
        move(["pelvis", "hip.L", "hip.R"] + upper, dy=-.19)
        move(["chest", "neck", "shoulder.L", "shoulder.R"], dz=-.012)
        move(["head", "head_tip"], dz=.012)
        for label, side in (("L", -1), ("R", 1)):
            x = side * .115
            leg(label, (x, .09, .33), (x, .07, .49))
            p["knee." + label] = (x, .40, .32)  # the classic seated knee, so thighs rest along the seat
            arm(label, (side * .20, .54, .20), (side * .4, -.2, -1))
    elif kind == "Call":
        for label, side in (("L", -1), ("R", 1)):
            shoulder = Vector(p["shoulder." + label])
            if side > 0:
                arm(label, (.33 + math.sin(phase) * .07, 1.43, .13), (1, -.6, -.2))
            else:
                arm(label, (shoulder.x - .05, shoulder.y - .44, .03), (-.3, -.2, -1))
        move(["head_tip"], dx=.012)
    return p


# ------------------------------------------------------------------------------------------------------------- mesh parts
class _Part:
    """One mesh piece in the Unity frame, with per-vertex bone weights and per-face material roles."""

    def __init__(self, name):
        self.name = name; self.verts = []; self.faces = []; self.weights = []; self.roles = []; self.smooth = True

    def add(self, p, weights):
        self.verts.append(Vector(p)); self.weights.append(weights); return len(self.verts) - 1


def _frame(axis):
    axis = Vector(axis).normalized()
    ref = Vector((0, 0, 1)) if abs(axis.z) < .9 else Vector((1, 0, 0))
    side = axis.cross(ref).normalized()
    front = side.cross(axis).normalized()
    if front.z < 0:
        front = -front; side = -side
    return side, front


def loft(name, rings, sides, role_of_band, cap_start=True, cap_end=True):
    """rings: list of (center, axis, half_width, half_depth, weights, depth_offset). Bands get role_of_band(i)."""
    part = _Part(name); index = []
    for center, axis, rx, rz, weights, *rest in rings:
        offset = rest[0] if rest else 0.0
        side, front = _frame(axis)
        ring = []
        for k in range(sides):
            a = math.tau * k / sides
            q = Vector(center) + side * (rx * math.cos(a)) + front * (rz * math.sin(a) + offset * max(0.0, math.sin(a)))
            ring.append(part.add(q, weights))
        index.append(ring)
    for i in range(len(index) - 1):
        for k in range(sides):
            a, b = index[i][k], index[i][(k + 1) % sides]
            c, d = index[i + 1][(k + 1) % sides], index[i + 1][k]
            part.faces.append((a, b, c, d)); part.roles.append(role_of_band(i, k))
    for end, ring, cap, data, neighbour in ((0, index[0], cap_start, rings[0], rings[1]), (1, index[-1], cap_end, rings[-1], rings[-2])):
        if not cap:
            continue
        band = 0 if end == 0 else len(index) - 2
        if cap == "dome":
            # A rounded end: three shrinking rings bulging outwards along the limb, then a pole.
            centre = Vector(data[0]); outward = (centre - Vector(neighbour[0])).normalized()
            side, front = _frame(data[1]); rx, rz = data[2], data[3]
            previous = ring; depth = min(rx, rz) * .9
            for step, angle in enumerate((.45, .85, 1.25)):
                scale = math.cos(angle); lift = math.sin(angle) * depth
                current = []
                for k in range(sides):
                    a = math.tau * k / sides
                    q = centre + outward * lift + side * (rx * scale * math.cos(a)) + front * (rz * scale * math.sin(a))
                    current.append(part.add(q, data[4]))
                for k in range(sides):
                    quad = (previous[k], previous[(k + 1) % sides], current[(k + 1) % sides], current[k])
                    part.faces.append(quad if end == 1 else tuple(reversed(quad))); part.roles.append(role_of_band(band, k))
                previous = current
            pole = part.add(centre + outward * depth, data[4])
            for k in range(sides):
                tri = (previous[k], previous[(k + 1) % sides], pole)
                part.faces.append(tri if end == 1 else tuple(reversed(tri))); part.roles.append(role_of_band(band, k))
            continue
        centre = part.add(Vector(data[0]), data[4])
        for k in range(sides):
            part.faces.append((ring[k], ring[(k + 1) % sides], centre))
            part.roles.append(role_of_band(band, k))
    return part


def ellipsoid(name, center, radii, role, weights, segments=12, rings=8, shape=None, keep=None):
    """A smooth ellipsoid. shape(x, y, z) may reshape unit-sphere points; keep(x, y, z) drops points (and their faces)."""
    part = _Part(name); grid = []
    for r in range(rings + 1):
        theta = math.pi * r / rings
        row = []
        for s in range(segments):
            phi = math.tau * s / segments
            x, y, z = math.sin(theta) * math.cos(phi), math.cos(theta), math.sin(theta) * math.sin(phi)
            if keep is not None and not keep(x, y, z):
                row.append(None); continue
            if shape:
                x, y, z = shape(x, y, z)
            q = Vector(center) + Vector((x * radii[0], y * radii[1], z * radii[2]))
            row.append(part.add(q, weights(q) if callable(weights) else weights))
        grid.append(row)
    for r in range(rings):
        for s in range(segments):
            quad = [grid[r][s], grid[r][(s + 1) % segments], grid[r + 1][(s + 1) % segments], grid[r + 1][s]]
            quad = [v for i, v in enumerate(quad) if v is not None and v not in quad[:i]]
            if len(quad) >= 3:
                part.faces.append(tuple(quad)); part.roles.append(role)
    return part


def slab(name, center, size, role, weights, tilt=0.0):
    """A small flat box (badges, pockets, collar points). tilt rotates it about the forward axis, in radians."""
    part = _Part(name); part.smooth = False
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
    c, s = math.cos(tilt), math.sin(tilt)
    ids = []
    for x in (-hx, hx):
        for y in (-hy, hy):
            for z in (-hz, hz):
                q = Vector(center) + Vector((x * c - y * s, x * s + y * c, z))
                ids.append(part.add(q, weights))
    for quad in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        part.faces.append(tuple(ids[i] for i in quad)); part.roles.append(role)
    return part


def _blend(a, b, x):
    """Weights moving from bone a to bone b as x goes 0 to 1."""
    x = _smooth(x)
    if x <= .001:
        return {a: 1.0}
    if x >= .999:
        return {b: 1.0}
    return {a: 1 - x, b: x}


def _chain_weights(t, bones, joints, radius):
    """t is a distance along a chain; joints are the distances where bones[i] hands over to bones[i+1]."""
    for i, j in enumerate(joints):
        if t < j - radius:
            return {bones[i]: 1.0}
        if t <= j + radius:
            return _blend(bones[i], bones[i + 1], (t - (j - radius)) / (2 * radius))
    return {bones[-1]: 1.0}


# ----------------------------------------------------------------------------------------------------------------- body
# The torso rings are shared with the game, which lays badges and training marks on the same surface.
STYLES_PATH = Path(__file__).resolve().parents[1] / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicCharacterStyles.json"
STYLES = json.loads(STYLES_PATH.read_text())
TORSO = [(r["y"], r["halfWidth"], r["halfDepth"], r["front"]) for r in STYLES["torso"]]


def torso_front(x, y):
    """The torso's front surface depth (Unity z) at (x, y), for clothing details that lie on it."""
    for (y0, w0, d0, f0), (y1, w1, d1, f1) in zip(TORSO, TORSO[1:]):
        if y0 <= y <= y1:
            t = (y - y0) / (y1 - y0)
            w, d, f = w0 + (w1 - w0) * t, d0 + (d1 - d0) * t, f0 + (f1 - f0) * t
            return (d + f) * math.sqrt(max(0.0, 1 - (x / w) ** 2))
    return 0.0


def _torso_weights(y):
    if y < .74:
        return {"pelvis": 1.0}
    return _chain_weights(y, ["pelvis", "spine", "chest", "neck"], [.84, 1.02, 1.245], .06)


def build_body(kind):
    """kind: Patient, Receptionist or Nurse. Returns the list of parts in the Unity frame."""
    nurse, receptionist, patient = kind == "Nurse", kind == "Receptionist", kind == "Patient"
    uniform = {"Patient": "Apricot", "Receptionist": "Sage", "Nurse": "Blue"}[kind]
    waistline = .845  # trousers below, top above
    parts = []

    # Torso: one smooth loft from the hips to the base of the neck.
    rings = [((0, y, 0), (0, 1, 0), w, d, _torso_weights(y), f) for y, w, d, f in TORSO]

    def torso_role(i, k):
        y = (TORSO[i][0] + TORSO[i + 1][0]) / 2
        if y > 1.244:
            return "Skin"
        return uniform if y >= waistline else "Trousers"
    parts.append(loft("Torso", rings, 16, torso_role, cap_start=True, cap_end=False))

    # Neck to the jaw.
    neck_rings = [((0, y, .016 * (y - 1.24) / .105), (0, 1, .12), r, r * .92,
                   _chain_weights(y, ["chest", "neck", "head"], [1.25, 1.36], .03)) for y, r in
                  ((1.240, .056), (1.275, .051), (1.310, .049), (1.345, .050))]
    parts.append(loft("Neck", neck_rings, 12, lambda i, k: "Skin", cap_start=False, cap_end=False))

    for label, side in (("L", -1), ("R", 1)):
        shoulder, elbow, hand = Vector(REST["shoulder." + label]), Vector(REST["elbow." + label]), Vector(REST["hand." + label])
        upper_len = (elbow - shoulder).length; fore_len = (hand - elbow).length
        # Arm: shoulder to wrist, bending at the elbow. Sleeves: full (hoodie, blazer) or short (scrubs).
        arm_rings = []
        for t, r_w, r_d in ((.0, .058, .058), (.04, .055, .053), (.10, .050, .047), (.17, .045, .043), (.215, .041, .040),
                            (.245, .040, .039), (.29, .041, .038), (.36, .038, .034), (.42, .032, .029), (.455, .029, .025)):
            if t <= upper_len:
                c = shoulder + (elbow - shoulder).normalized() * t; axis = elbow - shoulder
            else:
                c = elbow + (hand - elbow).normalized() * (t - upper_len); axis = hand - elbow
            weights = _chain_weights(t, ["upper_arm." + label, "forearm." + label, "hand." + label], [upper_len, upper_len + fore_len], .035)
            if t < .05:
                weights = {"chest": .30 * (1 - t / .05), "upper_arm." + label: .70 + .30 * t / .05}
            arm_rings.append((tuple(c), tuple(axis), r_w, r_d, weights))
        sleeve_end = .12 if nurse else .44

        def arm_role(i, k, rings=arm_rings, label=label, sleeve_end=sleeve_end):
            t = (i + .5)
            ts = (.0, .04, .10, .17, .215, .245, .29, .36, .42, .455)
            mid = (ts[i] + ts[i + 1]) / 2
            return uniform if mid < sleeve_end else "Skin"
        parts.append(loft("Arm " + label, arm_rings, 10, arm_role, cap_start="dome", cap_end=True))
        # Deltoid: fills the notch between the shoulder line and the top of the arm, and bends with both.
        parts.append(ellipsoid("Deltoid " + label, (side * .200, 1.130, -.004), (.048, .040, .054), uniform,
                               {"chest": .35, "upper_arm." + label: .65}, 12, 7))
        if nurse:  # short sleeve hem
            c = shoulder + (elbow - shoulder).normalized() * .12
            parts.append(loft("Sleeve hem " + label, [(tuple(c + (elbow - shoulder).normalized() * -.012), tuple(elbow - shoulder), .054, .052, {"upper_arm." + label: 1.0}),
                                                      (tuple(c + (elbow - shoulder).normalized() * .012), tuple(elbow - shoulder), .054, .052, {"upper_arm." + label: 1.0})],
                              12, lambda i, k: uniform, False, False))
        else:      # cuffs: ribbed (hoodie) or a shirt cuff peeping from the blazer
            c = elbow + (hand - elbow).normalized() * (.40 - upper_len)
            cuff_role = "Linen" if receptionist else uniform
            parts.append(loft("Cuff " + label, [(tuple(c), tuple(hand - elbow), .036, .033, {"forearm." + label: 1.0}),
                                                (tuple(c + (hand - elbow).normalized() * .03), tuple(hand - elbow), .036, .033, {"forearm." + label: .5, "hand." + label: .5})],
                              10, lambda i, k, r=cuff_role: r, False, False))
        # Hand: one relaxed mitten (palm and fingers) hanging from the wrist, and a thumb.
        palm = hand + Vector((side * .004, -.046, .008))
        parts.append(ellipsoid("Hand " + label, tuple(palm), (.024, .050, .017), "Skin", {"hand." + label: 1.0}, 10, 7,
                               shape=lambda x, y, z: (x * (1 - .18 * max(0.0, -y)), y, z * (1 - .25 * max(0.0, -y)))))
        parts.append(ellipsoid("Thumb " + label, tuple(palm + Vector((-side * .010, .014, .016))), (.009, .022, .009), "Skin", {"hand." + label: 1.0}, 8, 5))

        # Leg: hip to ankle, bending at the knee.
        hip, knee, ankle = Vector(REST["hip." + label]), Vector(REST["knee." + label]), Vector(REST["ankle." + label])
        thigh_len = (knee - hip).length; shin_len = (ankle - knee).length
        leg_rings = []
        for t, r_w, r_d in ((.00, .078, .084), (.06, .080, .082), (.12, .075, .076), (.22, .066, .066), (.30, .057, .058),
                            (.345, .054, .056), (.40, .058, .060), (.48, .056, .058), (.56, .048, .049), (.62, .042, .043), (.655, .044, .047)):
            if t <= thigh_len:
                c = hip + (knee - hip).normalized() * t; axis = knee - hip
            else:
                c = knee + (ankle - knee).normalized() * (t - thigh_len); axis = ankle - knee
            inward = side * -.020 * max(0.0, 1 - t / thigh_len)
            if t < .06:
                weights = {"pelvis": .40 * (1 - t / .06), "upper_leg." + label: .60 + .40 * t / .06}
            else:
                weights = _chain_weights(t, ["upper_leg." + label, "lower_leg." + label, "foot." + label], [thigh_len, thigh_len + shin_len], .04)
            leg_rings.append((tuple(c + Vector((inward, .02 * (1 - min(1.0, t / .06)), 0))), tuple(axis), r_w, r_d, weights))
        parts.append(loft("Leg " + label, leg_rings, 12, lambda i, k: "Trousers", cap_start="dome", cap_end=True))
        # Trouser hem over the shoe.
        parts.append(loft("Trouser hem " + label, [((ankle.x, .125, .010), (0, 1, 0), .049, .052, {"lower_leg." + label: 1.0}),
                                                   ((ankle.x, .100, .012), (0, 1, 0), .050, .053, {"lower_leg." + label: .6, "foot." + label: .4})],
                          12, lambda i, k: "Trousers", False, False))
        parts.extend(_shoe(kind, label, ankle))
    parts.extend(_clothing(kind, uniform))
    parts.extend(_head(kind))
    return parts


def _shoe(kind, label, ankle):
    upper = {"Patient": "Ink", "Receptionist": "Wood", "Nurse": "Linen"}[kind]
    sole = {"Patient": "Linen", "Receptionist": "Ink", "Nurse": "Ink"}[kind]
    w = {"foot." + label: 1.0}

    def toe_shape(x, y, z):
        # Flatter at the toe, rounder at the heel; a shoe rather than an egg.
        if z > 0:
            y *= 1 - .35 * z
        return x * (1 - .08 * max(0.0, z)), y, z
    return [ellipsoid("Shoe " + label, (ankle.x, .062, .058), (.050, .050, .118), upper, w, 14, 8, shape=toe_shape,
                      keep=lambda x, y, z: y > -.55),
            loft("Sole " + label, [((ankle.x, .006, .058), (0, 1, 0), .050, .117, w), ((ankle.x, .028, .058), (0, 1, 0), .051, .118, w)],
                 14, lambda i, k, r=sole: r, True, True)]


def v_patch(name, half_width, top, bottom, role, weights, lift=.0025):
    """A thin V lying on the torso front: a neckline or a shirt showing between lapels."""
    part = _Part(name)
    rows = 6
    for r in range(rows + 1):
        y = top - (top - bottom) * r / rows
        w = half_width * (1 - r / rows)
        for x in (-w, 0.0, w):
            part.add((x, y, torso_front(x, y) + lift), weights)
    for r in range(rows):
        a = r * 3
        b = a + 3
        part.faces.append((a, a + 1, b + 1, b)); part.roles.append(role)
        part.faces.append((a + 1, a + 2, b + 2, b + 1)); part.roles.append(role)
    return part


def _clothing(kind, uniform):
    chest = {"chest": 1.0}; spine = {"spine": 1.0}
    parts = []
    if kind == "Patient":
        # A hoodie: hood folded on the back, drawstrings, kangaroo pocket and a ribbed hem.
        parts.append(ellipsoid("Hood", (0, 1.214, -.064), (.108, .044, .050), uniform, {"chest": .7, "neck": .3}, 12, 7))
        for x in (-.030, .030):
            parts.append(slab("Hood drawstring", (x, 1.165, .108), (.007, .070, .006), "Linen", chest))
        parts.append(slab("Kangaroo pocket", (0, .935, .099), (.170, .072, .010), uniform, spine))
        parts.append(loft("Hoodie hem", [((0, .842, 0), (0, 1, 0), .145, .096, {"pelvis": .5, "spine": .5}),
                                         ((0, .872, 0), (0, 1, 0), .145, .097, {"spine": 1.0})], 16, lambda i, k: uniform, False, False))
    elif kind == "Receptionist":
        # A blazer: shirt and lapels at the front, a hem over the hips, a belt and a name badge.
        parts.append(v_patch("Shirt front", .050, 1.238, 1.070, "Linen", chest))
        for side in (-1, 1):
            parts.append(slab("Blazer lapel", (side * .040, 1.135, .104), (.030, .150, .010), "SageDark", chest, tilt=side * .30))
        parts.append(slab("Blazer button", (0, 1.000, .108), (.014, .014, .006), "SageDark", spine))
        parts.append(loft("Blazer hem", [((0, .905, .003), (0, 1, 0), .145, .096, {"spine": 1.0}),
                                         ((0, .850, .004), (0, 1, 0), .149, .099, {"pelvis": .5, "spine": .5}),
                                         ((0, .780, .004), (0, 1, 0), .162, .104, {"pelvis": 1.0})], 16, lambda i, k: uniform, False, False))
        parts.append(slab("Name badge", (.085, 1.105, .108), (.054, .034, .008), "Gold", chest))
    else:
        # Scrubs: a V-neck, chest pocket with a pen, name badge.
        parts.append(v_patch("Scrub V-neck", .046, 1.236, 1.150, "Skin", chest))
        parts.append(slab("Scrub pocket", (-.078, 1.065, .107), (.070, .064, .008), uniform, chest))
        parts.append(slab("Pocket pen", (-.096, 1.105, .112), (.009, .046, .008), "Gold", chest))
        parts.append(slab("Name badge", (.082, 1.110, .108), (.056, .036, .008), "Gold", chest))
        parts.append(loft("Scrub hem", [((0, .846, 0), (0, 1, 0), .143, .095, {"pelvis": .5, "spine": .5}),
                                        ((0, .866, 0), (0, 1, 0), .143, .096, {"spine": 1.0})], 16, lambda i, k: uniform, False, False))
    if kind != "Patient":
        parts.append(loft("Belt", [((0, .828, 0), (0, 1, 0), .148, .097, {"pelvis": 1.0}),
                                   ((0, .846, 0), (0, 1, 0), .147, .096, {"pelvis": .5, "spine": .5})],
                          16, lambda i, k: "Wood" if kind == "Receptionist" else "Trousers", False, False))
    return parts


HEAD_CENTER = Vector((0, 1.405, .022))
HEAD_RADII = (.100, .126, .108)


def _head_shape(x, y, z):
    """Unit sphere to a head: a narrower jaw and chin, a fuller cranium, a flatter back."""
    if y < 0:
        jaw = 1 - .26 * (-y) ** 1.6
        x *= jaw
        if z > 0:
            z *= 1 - .10 * (-y)
    else:
        x *= 1 + .03 * y
    if z < 0:
        z *= .93
    return x, y, z


def head_surface(x, y):
    """Front surface point of the sculpted head at Unity-frame (x, y), found by bisection on the head shape."""
    best = None
    for theta_i in range(0, 181):
        theta = math.pi * theta_i / 180
        for phi_i in range(0, 91):
            phi = math.pi * phi_i / 90
            sx, sy, sz = math.sin(theta) * math.cos(phi), math.cos(theta), math.sin(theta) * math.sin(phi)
            hx, hy, hz = _head_shape(sx, sy, sz)
            q = HEAD_CENTER + Vector((hx * HEAD_RADII[0], hy * HEAD_RADII[1], hz * HEAD_RADII[2]))
            d = (q.x - x) ** 2 + (q.y - y) ** 2
            if best is None or d < best[0]:
                best = (d, q)
    return best[1]


def _head(kind):
    head = {"head": 1.0}
    parts = [ellipsoid("Head", tuple(HEAD_CENTER), HEAD_RADII, "Skin", head, 24, 14, shape=_head_shape)]
    # Ears.
    for side in (-1, 1):
        parts.append(ellipsoid("Ear", (side * .098, 1.398, .008), (.014, .026, .020), "Skin", head, 8, 6))
    # Eyes: white, iris; brows in the hair colour.
    for side in (-1, 1):
        eye = head_surface(side * .036, 1.418)
        parts.append(ellipsoid("Eye white", tuple(eye + Vector((0, 0, -.004))), (.0165, .0125, .007), "Linen", head, 10, 6))
        parts.append(ellipsoid("Iris", tuple(eye + Vector((side * -.001, -.001, .0015))), (.0085, .0095, .004), "Ink", head, 8, 5))
        brow = head_surface(side * .038, 1.447)
        parts.append(slab("Brow", tuple(brow + Vector((0, 0, .002))), (.032, .0065, .006), "Hair", head, tilt=side * -.10))
    nose = head_surface(0, 1.386)
    parts.append(ellipsoid("Nose", tuple(nose + Vector((0, 0, -.001))), (.0105, .019, .011), "Skin", head, 8, 6))
    mouth = head_surface(0, 1.346)
    parts.append(ellipsoid("Lips", tuple(mouth + Vector((0, 0, .000))), (.021, .0055, .006), "Lip", head, 10, 5,
                           shape=lambda x, y, z: (x, y + .45 * x * x, z)))
    # Hair cap: follows the skull a little proud of it, fuller on top, a hairline that rises at the temples and sits
    # above the ears. The game tints it per person and adds styles (curls, buns, ponytails, scarves) on top.
    def keep(x, y, z):
        if z > .30:
            return y > .50 + .16 * abs(x)
        if z > -.30:
            return y > .06
        return y > -.40

    def hair_shape(x, y, z):
        x, y, z = _head_shape(x, y, z)
        return x, y * (1.10 if y > 0 else 1.0), z
    parts.append(ellipsoid("Hair", tuple(HEAD_CENTER + Vector((0, .004, -.006))), (HEAD_RADII[0] * 1.10, HEAD_RADII[1] * 1.06, HEAD_RADII[2] * 1.09),
                           "Hair", head, 32, 18, shape=hair_shape, keep=keep))
    return parts


# --------------------------------------------------------------------------------------------------------------- assembly
def build(name, materials, collection=None):
    """Create the rig and its single skinned mesh in the current collection; return the rig.
    materials maps role names to bpy materials (create_clinic_assets.MATS)."""
    rest = REST
    bpy.ops.object.armature_add(); rig = bpy.context.object; rig.name = name + "Rig"
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in list(rig.data.edit_bones):
        rig.data.edit_bones.remove(bone)
    for bone, a, b, parent in LINKS:
        item = rig.data.edit_bones.new(bone); item.head = u(rest[a]); item.tail = u(rest[b])
        if parent:
            item.parent = rig.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")

    parts = build_body(name)
    mesh = bpy.data.meshes.new(name + "Skin")
    verts, faces, roles, weights, smooth = [], [], [], [], []
    for part in parts:
        base = len(verts)
        verts.extend(u(v) for v in part.verts)
        weights.extend(part.weights)
        for face, role in zip(part.faces, part.roles):
            faces.append(tuple(base + i for i in face)); roles.append(role); smooth.append(part.smooth)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name + "Skin", mesh)
    (collection or bpy.context.collection).objects.link(ob)
    order = []
    for role in roles:
        if role not in order:
            order.append(role)
    for role in order:
        mesh.materials.append(materials[role])
    for poly, role, sm in zip(mesh.polygons, roles, smooth):
        poly.material_index = order.index(role); poly.use_smooth = sm
    bm = bmesh.new(); bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh); bm.free()
    groups = {}
    for i, w in enumerate(weights):
        total = sum(w.values())
        for bone, value in w.items():
            if bone not in groups:
                groups[bone] = ob.vertex_groups.new(name=bone)
            groups[bone].add([i], value / total, "REPLACE")
    ob.parent = rig
    modifier = ob.modifiers.new("Articulated clinic skeleton", "ARMATURE"); modifier.object = rig

    rig.animation_data_create()
    for kind in ACTIONS:
        action = bpy.data.actions.new(name + "_" + kind); rig.animation_data.action = action
        for frame in range(1, 34):
            bpy.context.scene.frame_set(frame); target = pose(kind, (frame - 1) / 32)
            matrices = {}
            for bone, a, b, parent in LINKS:
                rest_bone = rig.data.bones[bone]
                target_a, target_b = u(target[a]), u(target[b])
                delta = (rest_bone.tail_local - rest_bone.head_local).rotation_difference(target_b - target_a)
                matrix = delta.to_matrix().to_4x4() @ rest_bone.matrix_local
                matrix.translation = target_a; matrices[bone] = matrix
            for bone, a, b, parent in LINKS:
                pb = rig.pose.bones[bone]; rest_bone = rig.data.bones[bone]
                basis = rest_bone.matrix_local.inverted()
                if parent:
                    basis = basis @ rig.data.bones[parent].matrix_local @ matrices[parent].inverted()
                pb.matrix_basis = basis @ matrices[bone]
                pb.rotation_mode = "QUATERNION"
                pb.keyframe_insert("location", frame=frame); pb.keyframe_insert("rotation_quaternion", frame=frame)
                pb.keyframe_insert("scale", frame=frame)
            bpy.context.view_layer.update()
        bpy.context.scene.frame_set(17); bpy.context.view_layer.update()
        expected = pose(kind, .5)
        for bone, a, b, parent in LINKS:
            error = (rig.pose.bones[bone].head - u(expected[a])).length
            assert error < .002, f"{name} {kind} {bone} detached from authored joint: {error}"
        action.use_fake_user = True
        track = rig.animation_data.nla_tracks.new(); track.name = kind
        strip = track.strips.new(kind, 1, action); strip.action_frame_start = 1; strip.action_frame_end = 33
        track.mute = True
    rig.animation_data.action = None
    for track in rig.animation_data.nla_tracks:
        track.mute = False
    return rig


def statistics(rig):
    mesh = next(c for c in rig.children if c.type == "MESH")
    return {"vertices": len(mesh.data.vertices), "triangles": sum(len(p.vertices) - 2 for p in mesh.data.polygons),
            "materials": [m.name for m in mesh.data.materials], "bones": len(rig.data.bones)}
