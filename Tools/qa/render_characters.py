"""Before/after review renders of the clinic characters, straight from their FBX files.

Run with Blender (or the bpy module) in a separate process; it never opens the project .blend:

    blender -b --python Tools/qa/render_characters.py -- <fbx-folder> <output-folder> [label]
    python Tools/qa/render_characters.py <fbx-folder> <output-folder> [label]     # with the bpy module

Writes, labelled as Blender source-art previews (not Unity or device screenshots):
  <label>-closeup.png   the three models front three-quarter, idle, with runtime-like material colours
  <label>-game.png      the same at the game's camera angle, about 150 px tall (the game zoomed in a little), four headings each
  <label>-game-x3.png   the game view enlarged three times, nearest neighbour, to judge what a phone shows
  <label>-walk.png      eight walk frames of the patient from the side
  <label>-poses.png     every action at its middle frame for each model
  <label>-variants.png  the twelve visitor appearances and the five staff looks, dressed as the game dresses them
                        (colours, hair, headwear and bodywear from Resources/ClinicCharacterStyles.json)
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
FBX = Path(argv[0]); OUT = Path(argv[1]); LABEL = argv[2] if len(argv) > 2 else "preview"
OUT.mkdir(parents=True, exist_ok=True)
# Optional: CHARACTER_PASSES=closeup,game,walk,poses renders only those passes.
import os
PASSES = set((os.environ.get("CHARACTER_PASSES") or "closeup,game,walk,poses,variants").split(","))
STYLE_FILE = Path(__file__).resolve().parents[2] / "Unity/OrbitOrchard/Assets/IdleClinic/Resources/ClinicCharacterStyles.json"
MODELS = ("Patient", "Receptionist", "Nurse")
ACTIONS = ("Idle", "Walk", "CheckIn", "Treat", "Sit", "Call")

# Runtime colours and glossiness (ClinicArt.Color / Glossiness): what Unity's Standard shader shows, not the FBX's own values.
COLOURS = {
    "Ivory": (.88, .875, .81), "Linen": (.94, .93, .86), "Sage": (.40, .57, .45), "SageDark": (.22, .36, .28),
    "Apricot": (.84, .49, .31), "Gold": (.76, .56, .23), "Ink": (.20, .27, .26), "Blue": (.41, .62, .64),
    "Skin": (.78, .56, .39), "Wood": (.59, .40, .27), "Clay": (.74, .55, .41), "Denim": (.30, .42, .51),
    "Hair": (.16, .12, .10), "Lip": (.62, .33, .30), "Iris": (.10, .12, .13), "Sole": (.30, .27, .25),
    "Leather": (.20, .16, .14), "Rose": (.68, .37, .38), "Mustard": (.71, .55, .24), "Trousers": (.20, .24, .27),
    "SkinDeep": (.36, .22, .16), "SkinBrown": (.55, .36, .24), "SkinLight": (.91, .72, .55), "SkinOlive": (.69, .51, .32),
    "HairSilver": (.69, .70, .64), "HairChestnut": (.34, .22, .17), "HairGold": (.66, .48, .25), "Slate": (.35, .39, .42),
    "Timber": (.45, .31, .22), "Chrome": (.76, .77, .75), "SignBlue": (.16, .33, .58),
}
GLOSS = {"Gold": .40, "Skin": .30, "SkinDeep": .30, "SkinBrown": .30, "SkinLight": .30, "SkinOlive": .30, "Hair": .34,
         "HairSilver": .34, "HairChestnut": .34, "HairGold": .34, "Lip": .38}
# ClinicAppearance's per-visitor wardrobe (kept in step with the C# arrays).
CLOTHES = ["Apricot", "Denim", "Sage", "Mustard", "Rose", "Blue", "Apricot", "Denim", "Sage", "Rose", "Mustard", "Blue"]
SKINS = ["Skin", "SkinDeep", "SkinLight", "SkinBrown", "SkinOlive", "SkinDeep", "SkinLight", "SkinBrown", "SkinLight", "SkinDeep", "SkinOlive", "SkinBrown"]
HEIGHTS = [1, 1.06, .94, 1.03, .97, 1.02, .84, 1.10, .92, .96, 1.01, .95]
WIDTHS = [1, 1.05, .94, 1.14, .96, 1.07, .87, 1.04, 1.05, .98, 1.12, .93]


def srgb_to_linear(c):
    return tuple(x / 12.92 if x <= .04045 else ((x + .055) / 1.055) ** 2.4 for x in c)


def runtime_material(name):
    key = name.split(".")[0]
    mat = bpy.data.materials.get("Preview " + key)
    if mat:
        return mat
    mat = bpy.data.materials.new("Preview " + key)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    rgb = COLOURS.get(key, (.86, .84, .77))
    node.inputs["Base Color"].default_value = (*srgb_to_linear(rgb), 1)
    node.inputs["Roughness"].default_value = 1 - GLOSS.get(key, .19)
    node.inputs["Metallic"].default_value = .45 if key == "Gold" else 0
    return mat


def load(name, offset):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(FBX / (name + ".fbx")))
    objects = [o for o in bpy.data.objects if o not in before]
    rig = next(o for o in objects if o.type == "ARMATURE")
    for ob in objects:
        if ob.type == "MESH":
            ob["roles"] = [(slot.material.name if slot.material else "Ivory").split(".")[0] for slot in ob.material_slots]
            for slot in ob.material_slots:
                slot.material = runtime_material(slot.material.name if slot.material else "Ivory")
    # The clips also key the rig object itself, so placement goes on a parent, as the game's actor root does.
    holder = bpy.data.objects.new(name + " placement", None); bpy.context.scene.collection.objects.link(holder)
    rig.parent = holder; holder.location = offset
    rig["holder"] = holder.name
    return rig


def place(rig, location=None, heading=None):
    holder = bpy.data.objects[rig["holder"]]
    if location is not None:
        holder.location = location
    if heading is not None:
        holder.rotation_euler = (0, 0, heading)


def hide(rig, hidden=True):
    for ob in [rig] + list(rig.children):
        ob.hide_render = hidden


def action(rig, kind):
    for act in bpy.data.actions:
        if act.name.startswith(rig.name + "|") and act.name.endswith(kind):
            rig.animation_data.action = act
            if hasattr(rig.animation_data, "action_slot") and act.slots:
                rig.animation_data.action_slot = act.slots[0]
            return act
    raise KeyError(rig.name + " " + kind)


def scene_setup(width, height):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"; scene.cycles.samples = 64; scene.cycles.use_denoising = True
    scene.render.resolution_x = width; scene.render.resolution_y = height; scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.get("Clinic preview") or bpy.data.worlds.new("Clinic preview")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (*srgb_to_linear((.86, .84, .78)), 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = .55
    scene.world = world
    if "Clinic daylight" not in bpy.data.objects:
        light = bpy.data.lights.new("Clinic daylight", "SUN"); light.energy = 3.2; light.angle = .25
        sun = bpy.data.objects.new("Clinic daylight", light); scene.collection.objects.link(sun)
        # Unity's Euler(48,-35,0) daylight, in Blender axes.
        direction = Vector((.384, -.548, -.743))
        sun.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    if "Floor" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0)); floor = bpy.context.object; floor.name = "Floor"
        mat = bpy.data.materials.new("Preview floor"); mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*srgb_to_linear((.79, .65, .49)), 1)
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = .8
        floor.data.materials.append(mat)
    return scene


def camera(location, target, ortho_scale=None, lens=50):
    cam = bpy.data.objects.get("Preview camera")
    if cam is None:
        cam = bpy.data.objects.new("Preview camera", bpy.data.cameras.new("Preview camera"))
        bpy.context.scene.collection.objects.link(cam)
    cam.location = location
    cam.rotation_euler = (Vector(target) - Vector(location)).to_track_quat("-Z", "Y").to_euler()
    if ortho_scale:
        cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho_scale
    else:
        cam.data.type = "PERSP"; cam.data.lens = lens
    bpy.context.scene.camera = cam


def render(path):
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("CHARACTER_PREVIEW", path, flush=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
rigs = {name: load(name, (i * .9 - .9, 0, 0)) for i, name in enumerate(MODELS)}

# 1. Close-up: front three-quarter, idle middle frame.
scene = scene_setup(1500, 900)
for rig in rigs.values():
    action(rig, "Idle")
scene.frame_set(17)
camera((1.6, -5.2, 1.45), (0, 0, .80), lens=50)
if "closeup" in PASSES:
    render(OUT / (LABEL + "-closeup.png"))
if "face" in PASSES:
    camera((.35, -1.6, 1.5), (-.9, 0, 1.38), lens=85)
    render(OUT / (LABEL + "-face.png"))

# 2. Game view: the clinic camera's direction, orthographic; characters about 100 px tall like the phone screenshots.
for rig in rigs.values():
    hide(rig)
headings = (0, 45, 90, 180)
copies = []
for row, name in enumerate(MODELS):
    for col, heading in enumerate(headings):
        rig = rigs[name] if col == 0 else None
        if rig is None:
            before = set(bpy.data.objects)
            rig = load(name, (0, 0, 0))
            copies.append([o for o in bpy.data.objects if o not in before])
        hide(rig, False)
        # Blender -Y is the character's front; the game camera sits at +Y, so 180 degrees faces the camera.
        place(rig, (col * .95 - 1.4, row * 1.1 - 1.1, 0), math.radians(180 - heading))
        action(rig, "Idle")
scene = scene_setup(560, 560)
scene.frame_set(17)
offset = Vector((-1.2, 12, 15)).normalized() * 30
camera(tuple(offset), (0, 0, .7), ortho_scale=4.3)
if "game" in PASSES:
    render(OUT / (LABEL + "-game.png"))
try:
    from PIL import Image
    img = Image.open(OUT / (LABEL + "-game.png"))
    img.resize((img.width * 3, img.height * 3), Image.NEAREST).save(OUT / (LABEL + "-game-x3.png"))
except ImportError:
    pass
for group in copies:
    for ob in group:
        ob.hide_render = True
for rig in rigs.values():
    hide(rig)

# 3. Walk cycle: eight frames of the patient, side view.
walkers = []
for i in range(8):
    before = set(bpy.data.objects)
    rig = load("Patient", (i * .62 - 2.2, 0, 0))
    walkers.append(([o for o in bpy.data.objects if o not in before], rig))
    place(rig, heading=math.radians(90))
    act = action(rig, "Walk")
    # Bake each copy at its own phase: an NLA-free action offset by frames.
    rig.animation_data.action = None
    strip_track = rig.animation_data.nla_tracks.new()
    strip = strip_track.strips.new("Walk", 1 - i * 4, act)
scene = scene_setup(1800, 520)
scene.frame_set(1)
camera((0, -9, 1.0), (0, 0, .78), ortho_scale=5.2)
if "walk" in PASSES:
    render(OUT / (LABEL + "-walk.png"))
for group, rig in walkers:
    for ob in group:
        ob.hide_render = True

# 4. Every action at its middle frame, per model.
posers = []
for row, name in enumerate(MODELS):
    for col, kind in enumerate(ACTIONS):
        before = set(bpy.data.objects)
        rig = load(name, (col * .85 - 2.1, row * 1.6, 0))
        posers.append([o for o in bpy.data.objects if o not in before])
        place(rig, heading=math.radians(-25))
        action(rig, kind)
scene = scene_setup(1800, 1300)
scene.frame_set(9)
camera((1.0, -9.5, 4.2), (0, 1.5, .55), ortho_scale=6.6)
if "poses" in PASSES:
    render(OUT / (LABEL + "-poses.png"))


# 5. The game's wardrobe: twelve visitors and five staff looks, with headwear and bodywear from the shared style file.
def dress(rig, mapping):
    for ob in rig.children:
        if ob.type == "MESH":
            for slot, role in zip(ob.material_slots, ob["roles"]):
                slot.material = runtime_material(mapping.get(role, role))


def add_shapes(parent, shapes, hair):
    for shape in shapes:
        role = hair if shape["role"].startswith("@") else shape["role"]
        p, size = shape["position"], shape["size"]
        location = (-p["x"], -p["z"], p["y"])
        if shape["shape"] == "Box":
            bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0))
        else:
            bpy.ops.mesh.primitive_uv_sphere_add(radius=.5, segments=16, ring_count=10, location=(0, 0, 0))
            bpy.ops.object.shade_smooth()
        ob = bpy.context.object; ob.parent = parent; ob.location = location
        ob.scale = (size["x"], size["z"], size["y"]); ob.data.materials.append(runtime_material(role))


def accessories(rig, appearance, patient, who=None):
    styles = json.loads(STYLE_FILE.read_text())
    hair = styles["hairRoles"][appearance]
    holder = bpy.data.objects[rig["holder"]]
    bpy.context.view_layer.update()
    head = rig.matrix_world @ rig.pose.bones["head"].head
    chest = holder.matrix_world.inverted() @ (rig.matrix_world @ rig.pose.bones["chest"].head)
    headwear = bpy.data.objects.new("Headwear", None); bpy.context.scene.collection.objects.link(headwear)
    headwear.location = head; headwear.rotation_euler = holder.rotation_euler; headwear.scale = holder.scale
    for group in styles["headwear"]:
        if (group.get("patientsOnly") and not patient) or (group["appearances"] and appearance not in group["appearances"]):
            continue
        add_shapes(headwear, group["shapes"], hair)
    bodywear = bpy.data.objects.new("Bodywear", None); bpy.context.scene.collection.objects.link(bodywear)
    bodywear.parent = holder; bodywear.location = (0, 0, chest.z - 1.10)
    for group in styles["bodywear"]:
        if patient and group["who"] == "Patient" and appearance in group["appearances"]:
            add_shapes(bodywear, group["shapes"], hair)
        if not patient and group["who"] == who:
            add_shapes(bodywear, group["shapes"], hair)


if "variants" in PASSES:
    for ob in bpy.data.objects:
        if ob.type in ("MESH", "ARMATURE") and ob.name not in ("Floor",):
            ob.hide_render = True
    styles = json.loads(STYLE_FILE.read_text())
    scene = scene_setup(1800, 1200); scene.cycles.samples = 32
    scene.frame_set(17)
    for a in range(12):
        rig = load("Patient", ((a % 6) * .75 - 1.9, (a // 6) * 1.2, 0))
        place(rig, heading=math.radians(-25))
        bpy.data.objects[rig["holder"]].scale = (WIDTHS[a], WIDTHS[a], HEIGHTS[a])
        dress(rig, {"Skin": SKINS[a], "Apricot": CLOTHES[a], "Hair": styles["hairRoles"][a], "Trousers": styles["trouserRoles"][a]})
        action(rig, "Idle")
        scene.frame_set(17)
        accessories(rig, a, True)
    staff = [("Nurse", "Nurse", 0, {"Trousers": "Blue"}), ("Nurse", "Doctor", 4, {"Blue": "Linen", "Trousers": "Slate"}),
             ("Nurse", "Pharmacist", 9, {"Blue": "SageDark", "Trousers": "Ink"}), ("Receptionist", "Receptionist", 5, {"Trousers": "Ink"}),
             ("Receptionist", "Boss", 0, {"Sage": "Ink", "Trousers": "Ink"})]
    for i, (model, who, a, colours) in enumerate(staff):
        rig = load(model, (i * .85 - 1.7, 2.6, 0))
        place(rig, heading=math.radians(-25))
        mapping = {"Skin": SKINS[a], "Hair": styles["hairRoles"][a]}; mapping.update(colours)
        dress(rig, mapping)
        action(rig, "Idle")
        scene.frame_set(17)
        accessories(rig, a, False, who)
    camera((1.2, -8.5, 3.0), (0, 1.3, .75), ortho_scale=6.4)
    render(OUT / (LABEL + "-variants.png"))
