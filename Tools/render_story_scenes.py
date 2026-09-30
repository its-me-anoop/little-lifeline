"""Render the chapters of the Little Lifeline story promo with the game's own models (Blender, EEVEE).

    Blender -b --python Tools/render_story_scenes.py -- OUTDIR CHAPTER [--test]

Chapters: reception, firstaid, equipment, upgrades, waiting, hospital, lineup. Each writes OUTDIR/CHAPTER/0001.jpg ...
at 1080x1920, 30 fps, on the promo's cream field with a mint floor tile. --test renders three frames at half size.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/Resources/Clinic/Models"
args = sys.argv[sys.argv.index("--") + 1:]
OUT, CHAPTER, TEST = Path(args[0]), args[1], "--test" in args
FPS = 30


def srgb(c):
    c = c / 255
    return c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4


def rgb(r, g, b):
    return (srgb(r), srgb(g), srgb(b), 1)


CREAM, MINT, MINT_LINE, MINT_EDGE, FOREST = rgb(238, 239, 223), rgb(222, 232, 205), rgb(208, 222, 190), rgb(190, 208, 170), rgb(46, 74, 60)
OUTFITS = [rgb(201, 128, 94), rgb(176, 108, 112), rgb(96, 128, 150), rgb(196, 158, 88), rgb(120, 150, 118)]


# ---------------------------------------------------------------- scene setup
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x, scene.render.resolution_y = 1080, 1920
scene.render.resolution_percentage = 50 if TEST else 100
scene.render.fps = FPS
scene.render.image_settings.file_format = "JPEG"
scene.render.image_settings.quality = 93
scene.view_settings.view_transform = "Standard"
scene.eevee.taa_render_samples = 24
world = bpy.data.worlds.new("Field")
scene.world = world
world.use_nodes = True
# The camera sees the exact field colour; the scene is lit by a softer version of it.
nodes, links = world.node_tree.nodes, world.node_tree.links
field = FOREST if CHAPTER == "lineup" else CREAM
seen = nodes["Background"]
seen.inputs["Color"].default_value = field
lit = nodes.new("ShaderNodeBackground")
lit.inputs["Color"].default_value = field
lit.inputs["Strength"].default_value = .55
mix, path = nodes.new("ShaderNodeMixShader"), nodes.new("ShaderNodeLightPath")
links.new(path.outputs["Is Camera Ray"], mix.inputs[0])
links.new(lit.outputs[0], mix.inputs[1])
links.new(seen.outputs[0], mix.inputs[2])
links.new(mix.outputs[0], nodes["World Output"].inputs["Surface"])

sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 2.1
sun.data.angle = math.radians(18)
sun.rotation_euler = (math.radians(42), math.radians(-18), math.radians(-30))
scene.collection.objects.link(sun)


def material(name, color, rough=.62, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = color
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    return m


def cube(name, loc, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = size
    o.data.materials.append(mat)
    bevel = o.modifiers.new("b", "BEVEL")
    bevel.width, bevel.segments = min(.04, min(size) * .3), 3
    return o


def tile(cx, cy, w, d):
    """A floating mint slab with a faint grid, top face at z=0."""
    cube("Tile", (cx, cy, -.15), (w, d, .3), material("TileTop", MINT))
    cube("TileEdge", (cx, cy, -.33), (w - .02, d - .02, .06), material("TileEdge", MINT_EDGE))
    line = material("TileLine", MINT_LINE)
    for i in range(1, int(w)):
        cube("GridX", (cx - w / 2 + i, cy, .001), (.012, d - .04, .002), line)
    for j in range(1, int(d)):
        cube("GridY", (cx, cy - d / 2 + j, .001), (w - .04, .012, .002), line)


def disc(cx, cy, r, color):
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=r, depth=.12, location=(cx, cy, -.06))
    o = bpy.context.object
    o.data.materials.append(material("Disc", color))
    return o


def rug(cx, cy, w, d):
    cube("Rug", (cx, cy, .004), (w, d, .008), material("Rug", rgb(232, 214, 184)))


# ---------------------------------------------------------------- models
def import_model(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.data.objects if o not in before]
    root = next(o for o in new if o.parent is None and o.type in ("EMPTY", "ARMATURE"))
    holder = bpy.data.objects.new(path.stem + "Holder", None)
    scene.collection.objects.link(holder)
    for o in new:
        if o.parent is None:
            o.parent = holder
    return holder, new


def soften(objects):
    for o in objects:
        if o.type != "MESH":
            continue
        for slot in o.material_slots:
            if slot.material and slot.material.use_nodes:
                p = slot.material.node_tree.nodes.get("Principled BSDF")
                if p:
                    p.inputs["Roughness"].default_value = max(.5, p.inputs["Roughness"].default_value)


def place(holder, x, y, yaw=0.0, scale=1.0):
    holder.location = (x, y, 0)
    holder.rotation_euler = (0, 0, math.radians(yaw))
    holder.scale = (scale, scale, scale)
    return holder


def prop(name, x, y, yaw=0.0, scale=1.0):
    holder, objs = import_model(MODELS / f"{name}.fbx")
    soften(objs)
    return place(holder, x, y, yaw, scale)


class Gear:
    """One equipment piece with its ten versions; shows one at a time."""
    def __init__(self, room, item, x, y, yaw=0.0, version=1, scale=1.0):
        self.holder, objs = import_model(MODELS / f"{room}Gear" / f"Gear{item:02d}.fbx")
        soften(objs)
        place(self.holder, x, y, yaw, scale)
        self.base = scale
        self.roots = {}
        for o in objs:
            for v in range(1, 11):
                if o.name.split(".")[0] == f"Gear{item:02d}_V{v}":
                    self.roots[v] = o
        self.show(version)

    def show(self, version):
        for v, root in self.roots.items():
            hide = v != version
            for o in [root] + list(root.children_recursive):
                o.hide_render = hide
                o.hide_viewport = hide

    def pop(self, k):
        s = self.base * max(.001, k)
        self.holder.scale = (s, s, s)


class Person:
    """A rigged character with a sequence of looping actions; the holder carries position and facing."""
    def __init__(self, kind, x, y, yaw=0.0, outfit=None):
        self.holder, objs = import_model(MODELS / f"{kind}.fbx")
        soften(objs)
        self.arm = next(o for o in objs if o.type == "ARMATURE")
        self.prefix = self.arm.name.split(".")[0]
        self.kind = kind
        self.actions = {a.name.split("|")[-1].split(".")[0]: a for a in bpy.data.actions if a.name.startswith(self.prefix + "|")}
        if outfit is not None:
            for o in objs:
                if o.type == "MESH":
                    for slot in o.material_slots:
                        if slot.material and slot.material.name.split(".")[0] in ("Apricot",):
                            m = slot.material.copy()
                            m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = outfit
                            slot.material = m
        place(self.holder, x, y, yaw)
        self.arm.animation_data_create()
        self.arm.animation_data.action = None
        self.track = self.arm.animation_data.nla_tracks.new()

    def play(self, *segments):
        """segments: (action, start_frame, end_frame, phase)."""
        for name, a, b, phase in segments:
            act = self.actions.get(name) or self.actions["Idle"]
            strip = self.track.strips.new(f"{name}{a}", int(a), act)
            try:
                if act.slots:
                    strip.action_slot = act.slots[0]
            except AttributeError:
                pass
            strip.action_frame_start, strip.action_frame_end = 1, 33
            strip.repeat = max(.05, (b - a) / 32)
            strip.frame_start_ui if hasattr(strip, "frame_start_ui") else None
            strip.extrapolation = "HOLD_FORWARD" if (name, a, b, phase) == segments[-1] else "NOTHING"
            strip.blend_type = "REPLACE"
        return self


def lerp(a, b, t):
    return a + (b - a) * t


def ease(t):
    t = min(1, max(0, t))
    return 1 - (1 - t) ** 3


def smooth(t):
    t = min(1, max(0, t))
    return t * t * (3 - 2 * t)


def walk(person, path, f0, f1, f):
    """Move along a polyline between frames f0 and f1, facing the direction of travel."""
    if f <= f0:
        p = path[0]
        seg = (path[0], path[1])
    elif f >= f1:
        p = path[-1]
        seg = (path[-2], path[-1])
    else:
        total = sum((Vector(path[i + 1]) - Vector(path[i])).length for i in range(len(path) - 1))
        d = (f - f0) / (f1 - f0) * total
        for i in range(len(path) - 1):
            L = (Vector(path[i + 1]) - Vector(path[i])).length
            if d <= L:
                p = Vector(path[i]).lerp(Vector(path[i + 1]), d / L)
                seg = (path[i], path[i + 1])
                break
            d -= L
    person.holder.location = (p[0], p[1], 0)
    direction = Vector(seg[1]) - Vector(seg[0])
    if direction.length > 1e-4:
        person.holder.rotation_euler = (0, 0, math.atan2(direction.x, -direction.y) + math.radians(FACING))


def face(person, target):
    d = Vector(target) - Vector(person.holder.location[:2])
    person.holder.rotation_euler = (0, 0, math.atan2(d.x, -d.y) + math.radians(FACING))


# ---------------------------------------------------------------- camera
cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
cam.data.type = "ORTHO"
cam.data.shift_y = .16
scene.collection.objects.link(cam)
scene.camera = cam
ELEVATION, AZIMUTH = math.radians(33), math.radians(-38)


def aim(target, scale, azimuth=None, shift=.16):
    az = AZIMUTH if azimuth is None else azimuth
    d = Vector((math.sin(az) * math.cos(ELEVATION), -math.cos(az) * math.cos(ELEVATION), math.sin(ELEVATION)))
    cam.location = Vector(target) + d * 40
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    cam.data.shift_y = shift
    cam.data.clip_end = 200


# Model-facing correction (degrees): the rigs and furniture face -Y after import when this is 0.
FACING = 0.0
DESK_YAW = 180.0

# ---------------------------------------------------------------- chapters
frames = []


def ch_reception():
    tile(0, 0, 8, 8)
    prop("Plant", -2.8, 2.6)
    prop("Cupboard", 1.9, 2.4, DESK_YAW)
    prop("ReceptionDesk", 0, .4, DESK_YAW)
    staff = Person("Receptionist", 0, 1.1).play(("Idle", 0, 420, 0))
    face(staff, (0, -2))
    patient = Person("Patient", -2.6, -3.2, outfit=OUTFITS[0]).play(("Walk", 0, 150, 0), ("CheckIn", 150, 420, 0))
    path = [(-2.6, -3.4), (-1.0, -1.6), (0, -.5)]
    n = 390

    def step(f):
        walk(patient, path, 0, 150, f)
        if f > 150:
            face(patient, (0, 1))
        k = smooth(f / n)
        aim((lerp(-.4, 0, k), lerp(-.6, -.2, k), .5), lerp(7.4, 5.2, k))
    return n, step


def ch_firstaid():
    tile(1, 0, 10, 8)
    prop("Plant", -3.8, 2.6)
    prop("ReceptionDesk", -1.6, .4, DESK_YAW)
    Person("Receptionist", -1.6, 1.1).play(("Idle", 0, 300, 0)).holder.rotation_euler = (0, 0, math.radians(FACING))
    bay = prop("TreatmentBay", 2.4, .9, DESK_YAW)
    nurse = Person("Nurse", 5.2, 2.8).play(("Walk", 0, 120, 0), ("Treat", 120, 300, 0))
    patient = Person("Patient", -1.6, -.5, outfit=OUTFITS[1]).play(("Walk", 30, 130, 0), ("Treat", 130, 300, 0))
    n = 240

    def step(f):
        k = ease((f - 8) / 24)
        s = max(.001, k)
        bay.scale = (s, s, s)
        walk(nurse, [(5.2, 2.8), (4.2, 1.2), (3.23, .96)], 20, 120, f)
        if f > 120:
            face(nurse, (2.4, .9))
        walk(patient, [(-1.6, -.5), (.4, -1.2), (2.4, -.6), (2.4, .85)], 30, 130, f)
        if f > 130:
            patient.holder.rotation_euler = (0, 0, math.radians(FACING + DESK_YAW))
        kk = smooth(f / n)
        aim((lerp(.2, 1.8, kk), lerp(-.2, .3, kk), .5), lerp(7.6, 6.2, kk))
    return n, step


FIRSTAID_SPOTS = [(1.0, 2.6, 180, 6), (1.9, 2.8, 180, 13), (3.3, 2.8, 180, 7), (4.3, 2.4, 150, 19), (4.6, 1.2, 90, 9),
                  (4.5, -.3, 90, 16), (.3, 1.5, -90, 3), (.4, -.1, -90, 18)]


def ch_equipment():
    tile(2.3, .8, 7, 6)
    prop("TreatmentBay", 2.4, .9, DESK_YAW)
    Person("Nurse", 3.23, .96).play(("Treat", 0, 300, 0)).holder.rotation_euler = (0, 0, math.radians(FACING - 90))
    p = Person("Patient", 2.4, .85, outfit=OUTFITS[1]).play(("Treat", 0, 300, 0))
    p.holder.rotation_euler = (0, 0, math.radians(FACING + DESK_YAW))
    gear = [Gear("FirstAid", item, x, y, yaw + 180, version=4) for x, y, yaw, item in FIRSTAID_SPOTS]
    n = 270

    def step(f):
        for i, g in enumerate(gear):
            g.pop(ease((f - 20 - i * 26) / 14))
        k = smooth(f / n)
        aim((2.5, 1.0, .6), lerp(7.6, 8.4, k))
    return n, step


def ch_upgrades():
    tile(0, 0, 5, 3)
    items = [Gear("FirstAid", 6, -1.35, .1, 180), Gear("FirstAid", 14, 0, .1, 180), Gear("FirstAid", 19, 1.35, .1, 180)]
    n = 240

    def step(f):
        version = min(10, 1 + max(0, (f - 12)) // 22)
        since = (f - 12) % 22 if f >= 12 and version < 10 or (f - 12) < 22 * 9 else 99
        for it in items:
            it.show(version)
            it.pop(lerp(.9, 1, ease(since / 8)) if since < 8 else 1)
        aim((0, .1, .5), lerp(7.4, 6.8, smooth(f / n)), shift=.12)
    return n, step


def ch_waiting():
    tile(0, 0, 7, 6)
    rug(0, .3, 3.4, 2.2)
    prop("WaitingBench", 0, .9, 180)
    prop("Plant", -2.0, 1.2)
    prop("Plant", 2.1, 1.2)
    Gear("Waiting", 5, -2.6, -.4, 180, version=6)
    Gear("Waiting", 2, 2.7, -.6, 180, version=6)
    Gear("Waiting", 9, -2.6, 2.3, 180, version=6)
    for i, x in enumerate((-.58, 0, .58)[:2]):
        s = Person("Patient", x, .9, outfit=OUTFITS[2 + i]).play(("Sit", 0, 300, 0))
        s.holder.rotation_euler = (0, 0, math.radians(FACING + 180))
    walker = Person("Patient", 3.2, -2.8, outfit=OUTFITS[4]).play(("Walk", 0, 140, 0), ("Sit", 140, 300, 0))
    n = 210

    def step(f):
        walk(walker, [(3.2, -2.9), (1.6, -1.0), (.58, .9)], 10, 140, f)
        if f > 140:
            walker.holder.rotation_euler = (0, 0, math.radians(FACING + 180))
        aim((0, .2, .5), lerp(6.6, 5.4, smooth(f / n)))
    return n, step


def ch_hospital():
    tile(0, 0, 16, 12)
    # Reception: two desks, receptionists, a short line of visitors.
    for i, x in enumerate((-5.4, -3.2)):
        prop("ReceptionDesk", x, -2.2, DESK_YAW)
        r = Person("Receptionist", x, -1.5).play(("Idle", 0, 300, i * 7))
        r.holder.rotation_euler = (0, 0, math.radians(FACING))
        c = Person("Patient", x, -3.1, outfit=OUTFITS[i]).play(("CheckIn", 0, 300, 0))
        c.holder.rotation_euler = (0, 0, math.radians(FACING + 180))
    for j in range(4):
        q = Person("Patient", -4.3 + j * .7, -4.3, outfit=OUTFITS[(j + 2) % 5]).play(("Idle", 0, 300, j * 5))
        q.holder.rotation_euler = (0, 0, math.radians(FACING + 180))
    for x, y, yaw, item in ((-6.8, -.8, 90, 4), (-6.8, -2.0, 90, 11), (-1.8, -.9, -90, 19), (-1.8, -2.4, -90, 13), (-6.6, -4.9, 0, 16), (-6.0, -.8, 0, 7)):
        Gear("Reception", item, x, y, yaw + 180, version=10)
    # First aid: three bays with nurses and patients, the back wall lined with equipment.
    for i, x in enumerate((-5.6, -3.2, -.8)):
        prop("TreatmentBay", x, 2.4, DESK_YAW)
        nn = Person("Nurse", x + .83, 2.46).play(("Treat", 0, 300, i * 6))
        nn.holder.rotation_euler = (0, 0, math.radians(FACING - 90))
        pp = Person("Patient", x, 2.35, outfit=OUTFITS[(i + 1) % 5]).play(("Treat", 0, 300, 0))
        pp.holder.rotation_euler = (0, 0, math.radians(FACING + DESK_YAW))
    for k, item in enumerate((6, 13, 7, 19, 14, 9, 17, 20)):
        Gear("FirstAid", item, -7.0 + k * .95, 4.7, 180, version=10)
    Gear("FirstAid", 16, .6, 3.4, 90, version=10)
    Gear("FirstAid", 18, -6.9, 1.6, 90, version=10)
    # Waiting room: benches with patients, plants, and its own equipment.
    rug(4.2, .6, 5.0, 3.6)
    for b, y in enumerate((1.5, -.6)):
        prop("WaitingBench", 3.6, y, 180)
        for s, dx in enumerate((-.58, 0, .58)):
            if b == 1 and s == 2:
                continue
            sp = Person("Patient", 3.6 + dx, y, outfit=OUTFITS[(b * 3 + s) % 5]).play(("Sit", 0, 300, 0))
            sp.holder.rotation_euler = (0, 0, math.radians(FACING + 180))
    for x, y, item in ((6.4, 3.6, 17), (6.8, 1.8, 10), (6.8, .2, 4), (6.4, -1.8, 2), (1.6, 3.8, 5), (1.8, -2.4, 18), (5.2, -3.4, 12)):
        Gear("Waiting", item, x, y, 180, version=10)
    for x, y in ((-7.3, 5.3), (7.3, 5.3), (1.0, -5.3), (7.3, -5.2)):
        prop("Plant", x, y)
    walker = Person("Nurse", 1.2, -4.0).play(("Walk", 0, 300, 0))
    n = 270

    def step(f):
        walk(walker, [(1.2, -4.2), (1.2, 4.0)], 0, 300, f)
        k = smooth(f / n)
        aim((lerp(-3.0, 0, k), lerp(2.2, .2, k), .6), lerp(5.2, 17.5, k))
    return n, step


def ch_lineup():
    disc(0, 0, 2.6, rgb(58, 92, 74))
    people = [Person("Receptionist", -1.8, 0), Person("Nurse", -.6, 0), Person("Patient", .6, 0, outfit=OUTFITS[0]), Person("Nurse", 1.8, 0)]
    for i, p in enumerate(people):
        p.play(("Call", 0, 120, i * 4))
        p.holder.rotation_euler = (0, 0, math.radians(FACING))
    n = 90

    def step(f):
        aim((0, 0, .9), lerp(8.4, 7.8, smooth(f / n)), azimuth=math.radians(-12), shift=.06)
    return n, step


n, step = globals()["ch_" + CHAPTER]()
out = OUT / CHAPTER
out.mkdir(parents=True, exist_ok=True)
todo = [0, n // 2, n - 1] if TEST else range(n)
for f in todo:
    scene.frame_set(f + 1)
    step(f)
    scene.render.filepath = str(out / f"{f + 1:04d}.jpg")
    bpy.ops.render.render(write_still=True)
    if f % 30 == 0:
        print("STORY", CHAPTER, f, flush=True)
print("STORY_DONE", CHAPTER, n, flush=True)
