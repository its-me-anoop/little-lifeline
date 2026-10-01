"""Preview the redesigned reception in Blender (EEVEE) with real-world materials.

    Blender -b --python Tools/preview_reception.py -- OUT.png [--version N] [--shot wide|desk]

Builds the reception exactly as in the floor plan: 4.8 x 5.0 m, glass storefront to the street, cream walls with dark
caps, the staff-room door behind, the lobby open to the west. Two counters, the feature wall, the twenty equipment
pieces at one version, receptionists, patients at the counter and a short queue behind stanchions.
Room frame (Unity axes): x 0..4.8 west to east, z 0 at the storefront to 5.0 at the back wall, y up.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gear_kit  # noqa: E402  (resets the scene)
from gear_kit import box, tube, u, bpy, Vector  # noqa: E402
import create_reception_desk as desk  # noqa: E402
import gear_items_reception as rec  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:]
OUT = args[0]
VERSION = int(args[args.index("--version") + 1]) if "--version" in args else 6
SHOT = args[args.index("--shot") + 1] if "--shot" in args else "wide"
MODELS = gear_kit.MODELS
scene = bpy.context.scene


def place(root, x, z, yaw=0.0, y=0.0):
    root.location = u((x, y, z))
    root.rotation_euler.z = math.radians(-yaw)
    return root


def gear(item, x, z, yaw=0.0, y=0.0):
    root, _ = gear_kit.build_version(f"R{item}", rec.ITEMS[item], VERSION, rec.FOOT, item + 1)
    return place(root, x, z, yaw, y)


# ---------------------------------------------------------------- the room shell
for role in ("FloorTile", "Paving", "WallCream", "WallAccent", "GlassPane"):
    gear_kit.MATS[role] = bpy.data.materials.new(role)


def floor(x0, z0, x1, z1, role):
    box(((x0 + x1) / 2, -.01, (z0 + z1) / 2), (x1 - x0, .02, z1 - z0), role, 0)


def wall(x0, z0, x1, z1, height=2.6, thick=.16, role="WallCream"):
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
    size = (abs(x1 - x0) + thick, height, thick) if abs(x1 - x0) > abs(z1 - z0) else (thick, height, abs(z1 - z0) + thick)
    box((cx, height / 2, cz), size, role, .01)
    box((cx, height + .03, cz), (size[0] + .02, .06, size[2] + .02), "Charcoal", .008)
    box((cx, .06, cz), (size[0] + .01, .12, size[2] + .01), "Oak", .004)


floor(-2.2, -.6, 4.95, 5.1, "FloorTile")
floor(-2.2, -1.6, 4.95, -.6, "Paving")
# The wall behind the counters is painted deep sage so the cream feature panel and white quartz stand out.
wall(0, 5.0, 3.4, 5.0, role="WallAccent")
wall(4.35, 5.0, 4.8, 5.0, role="WallAccent")
box((3.875, 2.35, 5.0), (.95, .5, .16), "WallAccent", .01)
box((3.875, 2.63, 5.0), (.97, .06, .18), "Charcoal", .008)
for x in (3.42, 4.33):
    box((x, 1.05, 4.97), (.06, 2.1, .2), "Walnut", .005)
box((3.875, 2.08, 4.97), (.97, .06, .2), "Walnut", .005)
door = box((3.875 - .38, 1.03, 4.7), (.05, 2.02, .82), "Oak", .01)
box((3.44, 1.0, 4.35), (.02, .12, .04), "BrushedSteel", .004)
wall(4.8, 0, 4.8, 5.0)
wall(-2.2, 5.0, 0, 5.0)
# Storefront, cut away as the game camera sees it: a low sill, glass to knee height and stub mullions; open doors west.
box((2.4, .15, 0), (4.9, .3, .14), "Charcoal", .008)
box((2.4, .6, 0), (4.8, .6, .03), "GlassPane", 0)
box((2.4, .92, 0), (4.9, .05, .1), "Charcoal", .006)
for x in (0, 1.2, 2.4, 3.6, 4.8):
    box((x, .5, 0), (.06, 1.0, .12), "Charcoal", .004)
for x in (-2.0, -.1):
    box((x, .5, 0), (.08, 1.0, .14), "Charcoal", .004)
for x in (-2.3, .2):
    box((x, .5, -.05), (.5, .95, .03), "GlassPane", 0)
box((-1.05, .005, .7), (1.6, .012, 1.0), "Fabric", .004)

# ---------------------------------------------------------------- the counters and the feature wall
for cx in (1.35, 3.45):
    collection, root = desk.assemble(f"Desk{cx}", desk.build_desk)
    place(root, cx, 3.1)
collection, root = desk.assemble("Feature", desk.build_feature)
place(root, 2.0, 4.85)

# ---------------------------------------------------------------- equipment, as the game places it
LEDGE, WORK = 1.115, .96
gear(0, .78, 2.81, 0, LEDGE)       # visitor book
gear(5, 1.22, 2.81, 0, LEDGE)      # sanitizer station
gear(2, 1.60, 2.81, 0, LEDGE)      # card reader
gear(1, 2.11, 2.81, 0, LEDGE)      # desk bell
gear(3, 1.53, 3.30, 180, WORK)     # desk phone, facing the receptionist
gear(4, 1.97, 3.30, 180, WORK)     # receipt printer
gear(9, 4.25, .6, 90)              # self check-in kiosk, facing the queue
gear(6, 4.45, 1.45, 90)            # leaflet stand against the east wall
gear(15, 4.76, 2.35, 90, 1.62)     # information screen
gear(7, 4.76, 3.45, 90, 1.95)      # wall clock
gear(8, 4.76, 4.35, 90, 1.4)       # notice board
gear(11, 4.76, .25, 135, 2.3)      # security camera over the entrance corner
gear(18, 4.5, 3.8, 90)             # cash safe behind the counter's end
gear(17, 3.18, 4.62)               # air purifier beside the staff door
gear(14, .76, 4.62)                # planter beside the feature wall
gear(16, .24, 4.55)                # filing cabinet behind the counter
gear(19, .28, 1.9)                 # queue display
gear(10, -1.8, 4.6)                # water cooler, lobby side
gear(13, -1.85, .45)               # umbrella stand by the doors
for x in (1.0, 1.85, 2.7):
    gear(12, x, 1.0)               # queue barrier


# ---------------------------------------------------------------- people
class Person:
    def __init__(self, kind, x, z, face_x, face_z, action, outfit=None, phase=0):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(MODELS / f"{kind}.fbx"))
        new = [o for o in bpy.data.objects if o not in before]
        self.holder = bpy.data.objects.new(kind + "Holder", None)
        scene.collection.objects.link(self.holder)
        for o in new:
            if o.parent is None:
                o.parent = self.holder
        arm = next(o for o in new if o.type == "ARMATURE")
        prefix = arm.name.split(".")[0]
        acts = {a.name.split("|")[-1].split(".")[0]: a for a in bpy.data.actions if a.name.startswith(prefix + "|")}
        arm.animation_data_create()
        arm.animation_data.action = acts.get(action, acts["Idle"])
        try:
            arm.animation_data.action_slot = arm.animation_data.action.slots[0]
        except (AttributeError, IndexError):
            pass
        scene.frame_set(1 + phase)
        if outfit:
            for o in new:
                if o.type == "MESH":
                    for slot in o.material_slots:
                        if slot.material and slot.material.name.split(".")[0] == "Apricot":
                            m = slot.material.copy(); m.name = "Outfit" + outfit
                            slot.material = m
        p = u((x, 0, z)); t = u((face_x, 0, face_z))
        self.holder.location = p
        d = t - p
        self.holder.rotation_euler.z = math.atan2(d.x, -d.y)


Person("Receptionist", 1.35, 3.8, 1.35, 2.0, "Idle")
Person("Receptionist", 3.45, 3.8, 3.45, 2.0, "CheckIn", phase=8)
Person("Patient", 1.35, 2.21, 1.35, 3.5, "CheckIn", "Rose")
Person("Patient", 3.45, 2.21, 3.45, 3.5, "Idle", "Denim")
for i, (x, z, c) in enumerate(((1.1, .55, "Mustard"), (1.8, .55, "Sage"), (2.5, .55, "Apricot"), (3.2, .55, "Denim"))):
    Person("Patient", x, z, x, z + 2, "Idle", c, phase=i * 5)
Person("Nurse", -1.0, 3.6, 0, 3.0, "Walk", phase=10)


# ---------------------------------------------------------------- materials
def srgb(c):
    return tuple((v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4) for v in c)


def principled(m):
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type not in ("OUTPUT_MATERIAL",):
            nt.nodes.remove(n)
    p = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(p.outputs[0], nt.nodes["Material Output"].inputs["Surface"])
    return nt, p


def ramp(nt, fac, a, b, pos=(0, 1)):
    r = nt.nodes.new("ShaderNodeValToRGB")
    r.color_ramp.elements[0].position, r.color_ramp.elements[1].position = pos
    r.color_ramp.elements[0].color = (*a, 1); r.color_ramp.elements[1].color = (*b, 1)
    nt.links.new(fac, r.inputs[0])
    return r.outputs[0]


def coords(nt, scale=1.0):
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (scale, scale, scale)
    nt.links.new(tc.outputs["Object"], mp.inputs[0])
    return mp.outputs[0]


def wood(m, dark, light):
    nt, p = principled(m)
    mp = coords(nt, 1.0)
    sep = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(mp, sep.inputs[0])
    comb = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(sep.outputs["X"], comb.inputs["X"]); nt.links.new(sep.outputs["Y"], comb.inputs["Y"])
    squash = nt.nodes.new("ShaderNodeMath"); squash.operation = "MULTIPLY"; squash.inputs[1].default_value = .08
    nt.links.new(sep.outputs["Z"], squash.inputs[0]); nt.links.new(squash.outputs[0], comb.inputs["Z"])
    w = nt.nodes.new("ShaderNodeTexNoise"); w.inputs["Scale"].default_value = 38; w.inputs["Detail"].default_value = 6
    w.inputs["Distortion"].default_value = .6
    nt.links.new(comb.outputs[0], w.inputs[0])
    col = ramp(nt, w.outputs["Fac"], srgb(dark), srgb(light), (.35, .65))
    nt.links.new(col, p.inputs["Base Color"])
    p.inputs["Roughness"].default_value = .42


def stone(m, base, fleck):
    nt, p = principled(m)
    nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 60; nz.inputs["Detail"].default_value = 8
    nt.links.new(coords(nt), nz.inputs[0])
    col = ramp(nt, nz.outputs["Fac"], srgb(fleck), srgb(base), (.25, .45))
    nt.links.new(col, p.inputs["Base Color"])
    p.inputs["Roughness"].default_value = .18


def tiles(m, base, grout, size):
    nt, p = principled(m)
    b = nt.nodes.new("ShaderNodeTexBrick")
    b.inputs["Scale"].default_value = 1 / size; b.inputs["Mortar Size"].default_value = .006
    b.offset = 0; b.inputs["Brick Width"].default_value = 1.0; b.inputs["Row Height"].default_value = 1.0
    b.inputs["Color1"].default_value = (*srgb(base), 1); b.inputs["Color2"].default_value = (*srgb(tuple(v * .97 for v in base)), 1)
    b.inputs["Mortar"].default_value = (*srgb(grout), 1)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    nt.links.new(tc.outputs["Object"], b.inputs[0])
    nt.links.new(b.outputs["Color"], p.inputs["Base Color"])
    p.inputs["Roughness"].default_value = .35


def flat(m, color, rough=.6, metal=0.0, emit=0.0, alpha=1.0, trans=0.0):
    nt, p = principled(m)
    p.inputs["Base Color"].default_value = (*srgb(color), 1)
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    if emit:
        p.inputs["Emission Color"].default_value = (*srgb(color), 1)
        p.inputs["Emission Strength"].default_value = emit
    if trans:
        p.inputs["Transmission Weight"].default_value = trans
    if alpha < 1:
        p.inputs["Alpha"].default_value = alpha
        m.blend_method = "BLEND" if hasattr(m, "blend_method") else None
        try:
            m.surface_render_method = "BLENDED"
        except AttributeError:
            pass


def screen_image():
    w, h = 256, 160
    img = bpy.data.images.new("ReceptionScreen", w, h)
    px = []
    for y in range(h):
        top = h - 1 - y
        for x in range(w):
            c = (.93, .95, .96)
            if top < 22: c = (.18, .47, .50)
            elif x < 46: c = (.84, .89, .90)
            if 6 <= top < 14 and 8 <= x < 60: c = (.92, .97, .97)
            if x < 46 and top >= 34 and (top - 34) % 18 < 8 and 8 <= x < 38: c = (.72, .77, .80)
            if 56 <= x < 246 and top >= 32:
                row, r = (top - 32) // 24, (top - 32) % 24
                if row < 5 and r < 19:
                    c = (1, 1, 1)
                    if 5 <= r < 9 and 64 <= x < 150: c = (.30, .36, .40)
                    if 12 <= r < 15 and 64 <= x < 120: c = (.72, .77, .80)
                    if 6 <= r < 14 and 214 <= x < 238: c = (.36, .68, .46) if row == 0 else (.93, .66, .40) if row == 2 else (.72, .77, .80)
            px += [*srgb(c), 1]
    img.pixels = px
    return img


def screen(m):
    nt, p = principled(m)
    t = nt.nodes.new("ShaderNodeTexImage"); t.image = screen_image()
    nt.links.new(t.outputs[0], p.inputs["Base Color"])
    nt.links.new(t.outputs[0], p.inputs["Emission Color"])
    p.inputs["Emission Strength"].default_value = 2.8
    p.inputs["Roughness"].default_value = .15
    p.inputs["Coat Weight"].default_value = .6  # glossy glass face over the panel


OUTFITS = {"Rose": (.72, .28, .32), "Denim": (.20, .34, .52), "Mustard": (.82, .58, .16), "Sage": (.30, .52, .40), "Apricot": (.88, .44, .24)}
for m in list(bpy.data.materials):
    role = m.name.split(".")[0]
    if role.startswith("Outfit"):
        flat(m, OUTFITS.get(role[6:], (.8, .5, .3)), .7); continue
    if role == "Oak": wood(m, (.62, .46, .30), (.80, .64, .45))
    elif role == "Walnut": wood(m, (.24, .14, .08), (.44, .29, .18))
    elif role == "Quartz": stone(m, (.95, .945, .93), (.78, .76, .72))
    elif role == "FloorTile": tiles(m, (.72, .62, .50), (.56, .47, .37), .6)
    elif role == "Paving": tiles(m, (.74, .74, .70), (.62, .62, .58), .45)
    elif role == "WallCream": flat(m, (.95, .92, .86), .8)
    elif role == "WallAccent": flat(m, (.30, .45, .38), .85)
    elif role == "GlassPane": flat(m, (.80, .90, .92), .05, trans=.95, alpha=.25)
    elif role == "BrushedSteel": flat(m, (.74, .75, .76), .32, .9)
    elif role in ("Brass", "Gold"): flat(m, (.82, .66, .34), .28, .95)
    elif role == "Chrome": flat(m, (.86, .87, .88), .12, 1.0)
    elif role == "Fabric": flat(m, (.17, .34, .27), .95)
    elif role == "Leather": flat(m, (.36, .22, .14), .45)
    elif role == "Cork": stone(m, (.72, .55, .36), (.52, .37, .22))
    elif role == "ScreenUI": screen(m)
    elif role == "Paper": flat(m, (.97, .96, .92), .8, emit=.25)
    elif role == "LampLight": flat(m, (1.0, .86, .6), .5, emit=6)
    elif role == "Glow": flat(m, (.55, .95, .80), .4, emit=3)
    elif role == "Acrylic": flat(m, (.9, .95, .96), .04, trans=.9, alpha=.35)
    elif role == "Glass": flat(m, (.25, .32, .36), .06)
    elif role == "Linen": flat(m, (.95, .93, .87), .55)
    elif role == "Charcoal": flat(m, (.12, .14, .15), .5)
    elif role in gear_kit.COLORS: flat(m, gear_kit.COLORS[role], .55, gear_kit.METALLIC.get(role, 0))

# ---------------------------------------------------------------- light, camera, render
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.taa_render_samples = 64
try:
    scene.eevee.use_raytracing = True
except AttributeError:
    pass
scene.view_settings.view_transform = "AgX"
scene.view_settings.look = "AgX - Medium High Contrast" if "AgX - Medium High Contrast" in [i.identifier for i in scene.view_settings.bl_rna.properties["look"].enum_items] else "None"
world = bpy.data.worlds.new("Sky"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (*srgb((.96, .93, .86)), 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = .9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); scene.collection.objects.link(sun)
sun.data.energy = 3.0; sun.data.angle = math.radians(8); sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA")); scene.collection.objects.link(fill)
fill.data.energy = 900; fill.data.size = 6; fill.location = u((2.4, 4.0, 2.5)); fill.rotation_euler = (0, 0, 0)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam)
if SHOT == "desk":
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1100
    target, d, cam.data.ortho_scale = u((1.45, 1.05, 3.0)), Vector((.35, .9, .55)), 2.1
else:
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1300
    target, d, cam.data.ortho_scale = u((1.5, .6, 2.6)), Vector((.55, .95, 1.05)), 7.6
cam.data.type = "ORTHO"
d = d.normalized()
cam.location = target + d * 20
cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
cam.data.clip_end = 100
scene.camera = cam

def soft_bloom():
    """A soft halo from the emission pass only (screens, lamps), added over the render; lit walls never bloom."""
    scene.view_layers[0].use_pass_emit = True
    tree = bpy.data.node_groups.new("Bloom", "CompositorNodeTree")
    scene.compositing_node_group = tree
    tree.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    rl = tree.nodes.new("CompositorNodeRLayers")
    glare = tree.nodes.new("CompositorNodeGlare")
    for key, value in (("Type", "Bloom"), ("Quality", "High"), ("Threshold", .6), ("Smoothness", .5),
                       ("Strength", .9), ("Size", .5)):
        socket = glare.inputs.get(key)
        if socket is not None:
            socket.default_value = value
    tree.links.new(rl.outputs["Emission"], glare.inputs["Image"])
    add = tree.nodes.new("ShaderNodeMix"); add.data_type = "RGBA"; add.blend_type = "ADD"
    add.inputs[0].default_value = .55
    tree.links.new(rl.outputs["Image"], add.inputs[6]); tree.links.new(glare.outputs["Glare"], add.inputs[7])
    # A touch more saturation so the sage, walnut and outfits read against the pale surfaces.
    hue = tree.nodes.new("CompositorNodeHueSat")
    tree.links.new(add.outputs[2], hue.inputs["Image"])
    if hue.inputs.get("Saturation") is not None:
        hue.inputs["Saturation"].default_value = 1.12
    out = tree.nodes.new("NodeGroupOutput")
    tree.links.new(hue.outputs["Image"], out.inputs[0])


soft_bloom()
scene.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("PREVIEW_DONE", OUT, flush=True)
