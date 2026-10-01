"""Shared kit for the room equipment models: palette, primitives, the ten-version look and the exporter.

Items face -Z and stand on Y=0; the game rotates them onto walls or floor spots. Sizes and sockets follow the
Unity frame, as in create_clinic_assets.py. Driven by create_room_gear.py; never reads the user's open scene.
"""
from pathlib import Path
import math
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/Resources/Clinic/Models"
bpy.ops.wm.read_factory_settings(use_empty=True)

# The clinic palette (see create_clinic_assets.py and ClinicArt.Color) plus the metals and glows the upper versions use.
COLORS = {
    "Ivory": (.91, .89, .79), "Linen": (.96, .94, .86), "Sage": (.40, .57, .45), "SageDark": (.22, .36, .28),
    "Apricot": (.84, .49, .31), "Gold": (.76, .56, .23), "Ink": (.20, .27, .26), "Blue": (.41, .62, .64),
    "Wood": (.59, .40, .27), "Rose": (.68, .37, .38), "Chrome": (.76, .77, .75), "Platinum": (.82, .84, .86),
    "Graphite": (.24, .27, .30), "Aqua": (.30, .72, .72), "Glow": (.55, .95, .80), "Crimson": (.72, .22, .20),
    "Glass": (.20, .27, .31), "Tyre": (.12, .13, .13), "Clay": (.74, .55, .41), "Leaf": (.31, .49, .30),
    # Real-world surfaces. Textured ones (see TEXTURED) get world-scale UVs; the game paints them procedurally.
    "Walnut": (.36, .23, .14), "Oak": (.70, .54, .36), "Quartz": (.93, .92, .89), "BrushedSteel": (.66, .68, .69),
    "Fabric": (.45, .56, .52), "Leather": (.34, .22, .15), "ScreenUI": (.30, .52, .66), "Paper": (.97, .96, .92),
    "Acrylic": (.84, .91, .92), "LampLight": (.98, .93, .76), "Brass": (.78, .62, .30), "Rubber": (.14, .15, .15),
    "Terracotta": (.72, .42, .30), "Soil": (.25, .18, .12), "Cork": (.72, .55, .36), "Charcoal": (.17, .19, .20),
}
METALLIC = {"Gold": .45, "Chrome": .75, "Platinum": .6, "Graphite": .3, "BrushedSteel": .7, "Brass": .6}
# Surfaces that carry a texture in the game: world-scale cube-projected UVs (one UV unit per metre).
TEXTURED = {"Walnut", "Oak", "Quartz", "BrushedSteel", "Fabric", "Leather", "Cork"}
# Surfaces that show a whole picture on each face (screens and printed paper).
FACE_MAPPED = {"ScreenUI"}
MATS = {}
for role, rgb in COLORS.items():
    mat = bpy.data.materials.new(role); mat.diffuse_color = (*rgb, 1); mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*rgb, 1)
    node.inputs["Roughness"].default_value = .35 if role in METALLIC else .6
    node.inputs["Metallic"].default_value = METALLIC.get(role, 0)
    MATS[role] = mat

# (body, trim, accent) for versions 1-10: painted and plain, then blue trim and chrome hardware, then graphite with
# glowing accents, then platinum with gold. Each version also adds one more indicator light and grows a little.
RAMP = [
    ("Ivory", "Sage", "Sage"), ("Ivory", "Blue", "Blue"), ("Linen", "Blue", "Sage"), ("Linen", "Chrome", "Blue"),
    ("Platinum", "Chrome", "Blue"), ("Platinum", "Chrome", "Aqua"), ("Graphite", "Chrome", "Aqua"),
    ("Graphite", "Aqua", "Glow"), ("Platinum", "Gold", "Glow"), ("Platinum", "Gold", "Glow"),
]


def u(p): return Vector((-p[0], -p[2], p[1]))


def material(ob, role, smooth=False):
    ob.data.materials.append(MATS[role])
    for face in ob.data.polygons: face.use_smooth = smooth
    if role in TEXTURED or role in FACE_MAPPED:
        bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        if role in TEXTURED: bpy.ops.uv.cube_project(cube_size=1.0)
        else: bpy.ops.uv.reset()
        bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def box(p, size, role, bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=u(p)); ob = bpy.context.object
    ob.scale = (size[0], size[2], size[1]); bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    material(ob, role)
    if bevel:
        mod = ob.modifiers.new("Soft edges", "BEVEL"); mod.width = bevel; mod.segments = 2
    return ob


def orb(p, size, role, segments=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=8, location=u(p))
    ob = bpy.context.object; ob.scale = (size[0], size[2], size[1]); return material(ob, role, True)


def tube(a, b, radius, role, vertices=12):
    av, bv = u(a), u(b); direction = bv - av
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=(av + bv) / 2)
    ob = bpy.context.object; ob.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    return material(ob, role, True)


def lights(x, y, z, n, glow, step=.05, r=.014, axis="x"):
    """A level meter: one bar per version above the first, each taller than the last, so every upgrade shows at a glance."""
    for i in range(n):
        h = .035 + .017 * i; w = max(.014, r * 1.7)
        p = (x + i * step, y + h / 2, z) if axis == "x" else (x, y + i * step + h / 2, z)
        box(p, (w, h, w), glow, .004)


def wheels(xs, zs, r, y=None):
    y = r if y is None else y
    for x in xs:
        for z in zs:
            tube((x - .018, y, z), (x + .018, y, z), r, "Tyre", 16)
            tube((x - .024, y, z), (x + .024, y, z), r * .45, "Chrome", 10)


class Kit:
    """Per-version look: colours from the ramp, a growth factor and the number of extra lights."""
    def __init__(self, v):
        self.v = v; self.body, self.trim, self.accent = RAMP[v - 1]
        self.s = 1 + .04 * (v - 1); self.n = v - 1
        self.glow = "Glow" if v >= 6 else "Gold" if v >= 3 else "Apricot"
        self.premium = v >= 9


# ---- per-room drivers ----------------------------------------------------------------------------------------------

def extras(foot, index, k):
    """Trim shared by every item: a chrome skirt from version 4, a glowing edge from 7 and a gold plinth or frame from 9."""
    if index not in foot: return
    w, d, h, y0, zc, wall = foot[index]; w *= k.s; h *= k.s
    if k.v >= 4:
        if wall: box((0, y0 - .03, zc - d / 2), (w + .04, .025, .03), "Chrome", .006)
        else: box((0, .03, zc), (w + .05, .06, d + .05), "Chrome", .01)
    if k.v >= 7:
        if wall: box((0, y0 + h + .03, zc - d / 2), (w + .04, .02, .03), "Glow", .004)
        else: box((0, h + .04, zc - d / 2 - .01), (w * .8, .02, .02), "Glow", .004)
    if k.v >= 9:
        if wall: box((0, y0 + h + .07, zc - d / 2), (w + .08, .035, .04), "Gold", .008)
        else: box((0, .02, zc), (w + .16, .04, d + .16), "Gold", .012)


def build_version(name, make, version, foot, index=0):
    """Model one version in its own collection and merge it into one mesh per colour role under a single root."""
    collection = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children[collection.name]
    kit = Kit(version); make(kit); extras(foot, index, kit)
    for ob in list(collection.objects):
        bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.convert(target="MESH")
    for role in MATS:
        group = [o for o in collection.objects if o.type == "MESH" and o.data.materials and o.data.materials[0].name == role]
        if not group: continue
        bpy.ops.object.select_all(action="DESELECT")
        for ob in group: ob.select_set(True)
        bpy.context.view_layer.objects.active = group[0]; bpy.ops.object.join(); bpy.context.object.name = name + "_" + role
    root = bpy.data.objects.new(name, None); collection.objects.link(root)
    for ob in list(collection.objects):
        if ob != root: ob.parent = root
    return root, collection


def preview(stem, roots, folder):
    """Render the ten versions side by side, low sun and shaded, for a quick look (not part of the game build)."""
    scene = bpy.context.scene
    for i, root in enumerate(roots): root.location = (-i * 1.6, 0, 0)  # the Unity x flip puts version 1 on the left
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 2000, 420
    scene.render.filepath = str(Path(folder) / (stem + ".png"))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = 19.0
    cam.location = (-7.2, 8.5, 5.5); cam.rotation_euler = (math.radians(62), 0, math.radians(180))
    scene.camera = cam
    world = bpy.data.worlds.new("W"); scene.world = world; world.color = (.62, .68, .60)
    bpy.ops.render.render(write_still=True)


def clear():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
    for c in list(bpy.data.collections): bpy.data.collections.remove(c)


def export_room(folder, items, foot, preview_dir=None):
    """Write Gear01..Gear20 for one room (or preview them). Each file holds versions Gear01_V1 ... Gear01_V10."""
    assert len(items) == 20
    out = MODELS / folder; out.mkdir(parents=True, exist_ok=True)
    for index, make in enumerate(items, 1):
        stem = "Gear%02d" % index
        roots = [build_version("%s_V%d" % (stem, v), make, v, foot, index)[0] for v in range(1, 11)]
        if preview_dir:
            preview(folder + "_" + stem, roots, preview_dir); clear(); continue
        bpy.ops.object.select_all(action="DESELECT")
        for root in roots:
            root.select_set(True)
            for child in root.children: child.select_set(True)
        bpy.context.view_layer.objects.active = roots[0]
        bpy.ops.export_scene.fbx(filepath=str(out / (stem + ".fbx")), use_selection=True, object_types={"EMPTY", "MESH"},
                                 add_leaf_bones=False, axis_forward="-Z", axis_up="Y", bake_anim=False)
        print("GEAR_ASSET", folder, stem, make.__name__, (out / (stem + ".fbx")).stat().st_size, flush=True)
        clear()


# ---- shared building blocks for the item modules -----------------------------------------------------------------

def disc(x, y, z, r, t, role, vertices=20):
    """A flat cylinder standing on its end (a base plate, a dial or a wheel face), centred at (x, y, z)."""
    return tube((x, y - t / 2, z), (x, y + t / 2, z), r, role, vertices)


def pole(x, z, h, r, role, base=None, y0=0):
    """An upright rod from y0 to y0 + h, with an optional round foot of radius base."""
    if base: disc(x, y0 + .02, z, base, .04, role)
    return tube((x, y0, z), (x, y0 + h, z), r, role)


def screen(x, y, z, w, h, k, seed=0, bars=True, facing=-1):
    """A monitor face: dark bezel, glass, and one glowing trace bar per version (so a better screen shows more)."""
    box((x, y, z), (w, h, .05), "Ink", .01)
    zf = z + facing * .03
    box((x, y, zf), (w * .88, h * .84, .012), "Glass", .004)
    if bars:
        for i in range(min(k.v, 10)):
            bh = h * (.12 + .5 * abs(math.sin(i * 1.1 + seed)) * .8)
            box((x - w * .38 + i * w * .76 / 9, y - h * .32 + bh / 2, zf + facing * .008), (w * .05, bh, .006), k.glow if i < k.v else "Ink", .002)


def cabinet(cx, cz, w, d, h, k, glass=False, shelves=3, y0=0):
    """A tall cupboard with two doors and shelves stocked with bottles; glass doors from the mid versions."""
    box((cx, y0 + h / 2, cz), (w, h, d), k.body, .03)
    for sx in (-1, 1):
        box((cx + sx * w * .25, y0 + h / 2, cz - d / 2 - .01), (w * .46, h * .9, .022), "Glass" if glass else k.trim, .006)
    for i in range(shelves):
        y = y0 + h * (.18 + .62 * i / max(1, shelves - 1))
        box((cx, y, cz), (w * .92, .02, d * .8), k.trim if k.trim != "Chrome" else "Platinum", .004)
        for j in range(3 + (k.v > 5)):
            tube((cx - w * .34 + j * w * .68 / (2 + (k.v > 5)), y + .02, cz), (cx - w * .34 + j * w * .68 / (2 + (k.v > 5)), y + .15, cz), .035, k.accent if (i + j) % 2 else "Linen", 10)


def table(cx, cz, w, d, h, k, top=None):
    box((cx, h - .03, cz), (w, .06, d), top or k.trim if (top or k.trim) != "Chrome" else "Platinum", .015)
    for sx in (-1, 1):
        for sz in (-1, 1): tube((cx + sx * (w / 2 - .05), 0, cz + sz * (d / 2 - .05)), (cx + sx * (w / 2 - .05), h - .06, cz + sz * (d / 2 - .05)), .022, k.body if k.body != "Platinum" else "Chrome")


def counter_unit(cx, cz, w, d, h, k):
    """A counter: a body, a slightly wider worktop and a kick plate."""
    box((cx, h / 2 - .02, cz), (w, h - .04, d), k.body, .03)
    box((cx, h - .02, cz), (w + .04, .04, d + .04), k.trim if k.trim != "Chrome" else "Platinum", .012)
    box((cx, .04, cz - d / 2 + .02), (w * .96, .08, .03), "Ink", .006)


def tower(cx, cz, w, d, h, k, vent=True):
    """A slim standing appliance with vent slits and a status strip."""
    box((cx, h / 2, cz), (w, h, d), k.body, .03)
    if vent:
        for i in range(5): box((cx, h * (.45 + i * .09), cz - d / 2 - .006), (w * .7, .015, .012), "Ink", .002)
    box((cx, h * .92, cz - d / 2 - .006), (w * .55, .03, .012), k.glow if k.v > 2 else k.accent, .003)
