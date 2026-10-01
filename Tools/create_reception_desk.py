"""The reception counter and the feature wall behind it, as real-world clinic furniture.

Run with Blender -b --python Tools/create_reception_desk.py to write Clinic/Models/ReceptionDesk.fbx and
ReceptionFeature.fbx. Import it (after gear_kit) to build the pieces into another scene (see preview_reception.py).
The counter's front (visitor side) faces -Z and the receptionist stands at +Z; its sockets match the original desk,
so staff, patients and the cash stack keep their places. The visitor ledge tops out at 1.115, the staff surface at .96.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gear_kit import MATS, MODELS, box, orb, tube, u, bpy  # noqa: E402

W = 1.80


def socket(name, p):
    ob = bpy.data.objects.new("ReceptionDesk__" + name, None)
    bpy.context.collection.objects.link(ob)
    ob.location = u(p)
    ob.empty_display_size = .1
    return ob


def cylinder(x, z, r, y0, y1, role, vertices=40):
    return tube((x, y0, z), (x, y1, z), r, role, vertices)


def build_desk():
    """A rounded counter in light oak and white quartz, with a lower staff work surface."""
    ends, r = W / 2 - .22, .22
    box((0, .04, .04), (W - .14, .08, .70), "Charcoal", .01)
    for side in (-1, 1):
        cylinder(side * ends, -.18, r - .05, 0, .08, "Charcoal")
    # Cream body with rounded front corners.
    box((0, .54, 0), (W - .44, .92, .84), "Linen", .01)
    for side in (-1, 1):
        box((side * ends, .54, .11), (.44, .92, .62), "Linen", .01)
        cylinder(side * ends, -.20, r, .08, 1.0, "Linen")
    # Light-oak slats across the front, wrapping round each corner.
    for i in range(11):
        box((-.60 + i * .12, .54, -.43), (.07, .86, .03), "Oak", .006)
    for side in (-1, 1):
        for j in range(1, 5):
            a = math.radians(j * 18)
            slat = box((side * (ends + (r + .012) * math.sin(a)), .54, -.20 - (r + .012) * math.cos(a)), (.065, .86, .03), "Oak", .006)
            slat.rotation_euler.z = -side * a
    # Warm strip light under the ledge nose.
    box((0, 1.052, -.44), (W - .5, .012, .02), "LampLight", 0)
    # White quartz visitor ledge with rounded ends, and its upstand facing the staff.
    box((0, 1.09, -.27), (W - .44, .05, .34), "Quartz", .012)
    for side in (-1, 1):
        cylinder(side * ends, -.20, r + .03, 1.065, 1.115, "Quartz")
        box((side * ends, 1.09, -.10), (.5, .05, .2), "Quartz", .012)
    box((0, 1.01, -.10), (W - .2, .12, .03), "Linen", .006)
    # Staff work surface, drawer pedestal and cable tray.
    box((0, .94, .15), (W - .12, .04, .52), "Quartz", .01)
    box((.52, .47, .17), (.42, .9, .46), "Linen", .012)
    for i in range(3):
        y = .2 + i * .27
        box((.52, y, .405), (.38, .24, .012), "Oak", .006)
        box((.52, y + .07, .415), (.14, .015, .014), "BrushedSteel", .004)
    box((-.30, .88, .30), (.7, .03, .12), "Charcoal", .006)
    # Monitor facing the receptionist, keyboard and mouse.
    box((-.36, .968, .13), (.2, .012, .14), "Charcoal", .004)
    tube((-.36, .97, .15), (-.36, 1.2, .13), .018, "BrushedSteel", 10)
    box((-.36, 1.29, .11), (.56, .33, .03), "Charcoal", .01)
    box((-.36, 1.29, .127), (.52, .29, .003), "ScreenUI", 0)
    box((-.36, .968, .31), (.40, .016, .13), "Charcoal", .006)
    box((-.36, .977, .31), (.37, .004, .10), "Graphite", 0)
    orb((-.08, .972, .31), (.03, .016, .045), "Charcoal", 10)
    # Payment tray on the ledge where the day's cash collects.
    box((.51, 1.122, -.26), (.30, .014, .20), "Rubber", .005)
    socket("patient", (0, 0, -.89))
    socket("staff", (0, 0, .70))
    socket("cash", (.51, 1.17, -.15))


def heart(x, y, z, size, role):
    for side in (-1, 1):
        tube((x + side * size * .26, y + size * .18, z), (x + side * size * .26, y + size * .18, z - .012), size * .3, role, 24)
    diamond = box((x, y - size * .02, z - .006), (size * .6, size * .6, .012), role, 0)
    diamond.rotation_euler.y = math.radians(45)


def label(text, x, y, z, size, role):
    """Raised lettering that reads left to right for a viewer in front (-Z)."""
    bpy.ops.object.text_add(location=u((x, y, z)))
    ob = bpy.context.object
    ob.data.body = text
    ob.data.align_x, ob.data.align_y = "CENTER", "CENTER"
    ob.data.size = size
    ob.data.extrude = .004
    ob.rotation_euler = (math.radians(90), 0, math.radians(180))
    bpy.ops.object.convert(target="MESH")
    ob = bpy.context.object
    ob.data.materials.clear()
    ob.data.materials.append(MATS[role])
    return ob


def build_feature():
    """The wall behind the counter: cream, capped in charcoal, a green felt panel with the heart logo, and a sign."""
    width, height = 2.0, 1.2
    box((0, .04, .05), (width, .08, .12), "Oak", .008)
    box((0, height / 2, .05), (width, height, .10), "Linen", .01)
    box((0, height + .025, .05), (width + .04, .05, .14), "Charcoal", .008)
    box((0, .64, -.001), (1.40, .72, .012), "Oak", .004)
    box((0, .64, -.004), (1.34, .66, .02), "Fabric", .01)
    heart(0, .66, -.016, .34, "Paper")
    pulse = [(-.30, .64), (-.12, .64), (-.07, .74), (-.02, .52), (.04, .78), (.09, .64), (.30, .64)]
    for (x0, y0), (x1, y1) in zip(pulse, pulse[1:]):
        tube((x0, y0, -.03), (x1, y1, -.03), .011, "Leaf", 8)
    box((0, 1.08, -.012), (.74, .14, .02), "Charcoal", .008)
    label("Reception", 0, 1.08, -.024, .085, "Paper")
    for side in (-1, 1):
        box((side * .85, .95, -.02), (.06, .06, .04), "Brass", .006)
        orb((side * .85, .93, -.045), (.045, .03, .03), "LampLight", 10)


def assemble(name, make):
    """Build a piece in its own collection and join it into one mesh per material under a root empty."""
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children[collection.name]
    make()
    for ob in [o for o in collection.objects if o.type == "MESH"]:
        bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.convert(target="MESH")
    for role in MATS:
        group = [o for o in collection.objects if o.type == "MESH" and o.data.materials and o.data.materials[0].name.split(".")[0] == role]
        if not group:
            continue
        bpy.ops.object.select_all(action="DESELECT")
        for ob in group:
            ob.select_set(True)
        bpy.context.view_layer.objects.active = group[0]
        bpy.ops.object.join()
        bpy.context.object.name = name + "_" + role
    root = bpy.data.objects.new(name, None)
    collection.objects.link(root)
    for ob in list(collection.objects):
        if ob != root and ob.parent is None:
            ob.parent = root
    return collection, root


def export(collection, root, name):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(MODELS / f"{name}.fbx"), use_selection=True, object_types={"EMPTY", "MESH"},
                             add_leaf_bones=False, axis_forward="-Z", axis_up="Y", bake_anim=False)
    print("DESK_DONE", name, (MODELS / f"{name}.fbx").stat().st_size, flush=True)


if __name__ == "__main__":
    export(*assemble("ReceptionDesk", build_desk), "ReceptionDesk")
    export(*assemble("ReceptionFeature", build_feature), "ReceptionFeature")
