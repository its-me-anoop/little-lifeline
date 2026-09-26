"""Original fixed-clinic furniture and skinned, articulated characters.

Run using Blender -b --python Tools/create_clinic_assets.py. This separate
process never reads or changes the user's open Blender scene.
"""
from pathlib import Path
import math
import os
import sys
import bpy
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Unity/OrbitOrchard/Assets/IdleClinic/Art/Resources/Clinic/Models"
SOURCE = ROOT / "assets/idle-clinic-game.blend"
ICON_ONLY="--icon-only" in sys.argv
SIGN_ONLY="--sign-only" in sys.argv
if ICON_ONLY or SIGN_ONLY:bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
else:bpy.ops.wm.read_factory_settings(use_empty=True)
OUT.mkdir(parents=True, exist_ok=True)
COLORS = {
    "Ivory": (.91,.89,.79), "Linen": (.96,.94,.86), "Sage": (.40,.57,.45),
    "SageDark": (.22,.36,.28), "Apricot": (.84,.49,.31), "Gold": (.76,.56,.23),
    "Ink": (.20,.27,.26), "Blue": (.41,.62,.64), "Skin": (.78,.56,.39),
    "Wood": (.59,.40,.27), "Clay": (.74,.55,.41), "Leaf": (.31,.49,.30),
    "Rose": (.68,.37,.38),
}
MATS = {}
for role, rgb in COLORS.items():
    mat=bpy.data.materials.get(role) or bpy.data.materials.new(role);mat.diffuse_color=(*rgb,1);mat.use_nodes=True
    node=mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value=(*rgb,1)
    node.inputs["Roughness"].default_value=.6
    node.inputs["Metallic"].default_value=.45 if role=="Gold" else 0
    MATS[role]=mat

# All source dimensions and sockets are specified in the Unity game frame.
# FBX's handedness conversion is applied to furniture, bones and sockets alike.
def u(p): return Vector((-p[0],-p[2],p[1]))
def material(ob,role,smooth=False):
    ob.data.materials.append(MATS[role])
    for face in ob.data.polygons:face.use_smooth=smooth
    return ob
def box(name,p,size,role,bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1,location=u(p));ob=bpy.context.object;ob.name=name
    ob.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    material(ob,role)
    if bevel:
        mod=ob.modifiers.new("Soft edges","BEVEL");mod.width=bevel;mod.segments=2
        ob.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
    return ob
def orb(name,p,size,role,segments=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=8,location=u(p))
    ob=bpy.context.object;ob.name=name;ob.scale=(size[0],size[2],size[1]);return material(ob,role,True)
def tube(name,a,b,radius,role,vertices=12):
    av,bv=u(a),u(b);direction=bv-av
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=direction.length,location=(av+bv)/2)
    ob=bpy.context.object;ob.name=name;ob.rotation_euler=direction.to_track_quat("Z","Y").to_euler()
    return material(ob,role,True)
def taper(name,a,b,r1,r2,role,vertices=14,depth_scale=1):
    """An open limb or torso segment narrowing from a to b. Joints are covered by rounded caps,
    so no flat cone ends show. depth_scale flattens the front-to-back section, as for a chest."""
    av,bv=u(a),u(b);direction=bv-av
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=direction.length,location=(av+bv)/2,end_fill_type="NOTHING")
    ob=bpy.context.object;ob.name=name
    if depth_scale!=1:ob.scale=(1,depth_scale,1)
    ob.rotation_euler=direction.to_track_quat("Z","Y").to_euler()
    return material(ob,role,True)
def socket(asset,name,p,forward=(0,0,1)):
    ob=bpy.data.objects.new(asset+"__"+name,None);bpy.context.collection.objects.link(ob)
    ob.location=u(p);ob.empty_display_size=.10
    # Socket position is authoritative; facing is assigned explicitly by the renderer.
    return ob
def plant(p,scale=1):
    x,y,z=p
    tube("Planter",(x,y,z),(x,y+.30*scale,z),.20*scale,"Clay")
    for i in range(5):
        a=i*2.399
        ob=orb("Leaf",(x+math.sin(a)*.11*scale,y+.55*scale,z+math.cos(a)*.11*scale),(.10*scale,.30*scale,.07*scale),"Leaf")
        ob.rotation_euler=(.2*math.sin(a),.25*math.cos(a),a)

def desk():
    box("Sculpted counter",(0,.47,0),(1.6,.94,.68),"Sage",.11)
    box("Floating ivory top",(0,1.00,0),(1.73,.10,.83),"Ivory",.09)
    box("Apricot inset",(0,.52,-.354),(1.40,.52,.035),"Apricot",.045)
    for x in (-.60,-.40,-.20,0,.20,.40,.60):box("Counter fluting",(x,.51,-.38),(.026,.45,.027),"Gold",.008)
    tube("Monitor stand",(-.36,1.05,.10),(-.36,1.27,.10),.035,"Ink")
    box("Reception monitor",(-.36,1.34,.13),(.42,.32,.065),"Ink",.03)
    box("Monitor screen",(-.36,1.34,.17),(.35,.25,.018),"Blue",.02)
    box("Payment tray",(.51,1.07,-.15),(.42,.04,.32),"Wood",.04)
    box("Visitor register",(.02,1.07,.08),(.24,.025,.32),"Linen",.015)
    tube("Counter bell",(.10,1.06,-.22),(.10,1.15,-.22),.07,"Gold")
    socket("ReceptionDesk","patient",(0,0,-.89))
    socket("ReceptionDesk","staff",(0,0,.70))
    socket("ReceptionDesk","cash",(.51,1.17,-.15))

def seat():
    for x in (-.23,.23):
        for z in (-.22,.22):tube("Seat foot",(x,.04,z),(x,.40,z),.035,"Wood")
    box("Seat cushion",(0,.40,0),(.61,.13,.58),"Apricot",.10)
    box("Padded back",(0,.73,.26),(.63,.57,.13),"Sage",.11)
    for x in (-.35,.35):
        tube("Arm support",(x,.36,.14),(x,.62,.14),.027,"Gold")
        box("Armrest",(x,.63,0),(.07,.08,.53),"Wood",.035)
    socket("Seat","patient",(0,0,0))

def treatment():
    for x in (-.25,.25):
        for z in (-.24,.24):tube("Chair frame",(x,.04,z),(x,.40,z),.042,"Ink")
    box("Treatment cushion",(0,.40,0),(.64,.14,.61),"Blue",.10)
    box("Treatment chair back",(0,.78,.28),(.65,.65,.14),"Blue",.11)
    box("Headrest",(0,1.11,.27),(.40,.20,.15),"Linen",.08)
    for x in (-.36,.36):box("Clinical armrest",(x,.63,-.02),(.11,.08,.51),"Ivory",.04)
    box("Clinical cabinet",(-.65,.50,.77),(.60,1.0,.52),"Ivory",.06)
    box("Cabinet sage face",(-.65,.51,.493),(.52,.81,.018),"Sage",.025)
    for y in (.28,.55,.82):box("Drawer handle",(-.65,y,.472),(.18,.025,.025),"Gold",.007)
    for x in (-.8,-.58):
        tube("Bottle",(x,1.01,.79),(x,1.16,.79),.05,"Blue")
        box("Bottle cap",(x,1.18,.79),(.07,.03,.07),"Linen",.009)
    tube("Task lamp",(.44,.07,.73),(.44,1.5,.73),.025,"Gold")
    tube("Lamp arm",(.44,1.50,.73),(.12,1.62,.46),.025,"Gold")
    orb("Exam lamp",(.12,1.61,.43),(.17,.09,.12),"Ivory")
    socket("TreatmentBay","patient",(0,0,0))
    socket("TreatmentBay","staff",(.83,0,-.06))

def bench():
    for x in (-.7,.7):box("Bench leg",(x,.19,0),(.07,.38,.50),"Gold")
    box("Waiting seat",(0,.4,0),(1.8,.12,.60),"Apricot",.08)
    box("Waiting back",(0,.74,.26),(1.82,.57,.13),"Sage",.08)
    socket("WaitingBench","seat0",(-.58,0,0));socket("WaitingBench","seat1",(0,0,0));socket("WaitingBench","seat2",(.58,0,0))

def cupboard():
    box("Supply cupboard",(0,.80,0),(1.50,1.60,.50),"Ivory",.07)
    for x in (-.38,.38):
        box("Cupboard door",(x,.83,-.267),(.70,1.37,.035),"Sage",.04)
        box("Cupboard handle",(x+(.24 if x<0 else -.24),.82,-.30),(.025,.21,.04),"Gold",.01)
    box("Folded linen",(0,1.64,0),(.58,.09,.35),"Linen",.025)

def action_points(kind,t):
    bob=.012*math.sin(t*math.tau) if kind=="Idle" else 0
    p={"root":(0,0,0),"pelvis":(0,.74+bob,0),"spine":(0,.93+bob,0),"chest":(0,1.10+bob,0),"neck":(0,1.24+bob,0),"head":(0,1.38+bob,0),"head_tip":(0,1.53+bob,0)}
    sitting=kind=="Sit"
    if sitting:
        for key in ("pelvis","spine","chest","neck","head","head_tip"):
            q=p[key];p[key]=(q[0],q[1]-.19,q[2])
    for side,label in ((-1,"L"),(1,"R")):
        wave=math.sin(t*math.tau+(math.pi if side>0 else 0))
        shoulder=(side*.225,p["chest"][1],0)
        elbow=(side*.27,p["chest"][1]-.23,0)
        hand=(side*.285,p["chest"][1]-.46,.015)
        hip=(side*.115,p["pelvis"][1]-.01,0)
        knee=(side*.115,.39,.015);ankle=(side*.115,.09,.015);toe=(side*.115,.07,.18)
        if kind=="Walk":
            knee=(side*.115,.41+max(0,wave)*.055,wave*.15)
            ankle=(side*.115,.09+max(0,wave)*.12,wave*.27)
            toe=(side*.115,ankle[1]-.02,ankle[2]+.17)
            elbow=(side*.27,.89,-wave*.12);hand=(side*.28,.67,-wave*.23)
        if kind in ("CheckIn","Treat"):
            elbow=(side*.29,.96,.12)
            hand=(side*.18,1.03+math.sin(t*math.tau+side)*.025,.39+math.sin(t*math.tau)*.025)
        if kind=="Call" and side>0:
            elbow=(.36,1.20,.05);hand=(.33+math.sin(t*math.tau)*.07,1.43,.13)
        if sitting:
            knee=(side*.115,.40,.32);ankle=(side*.115,.09,.33);toe=(side*.115,.07,.49)
            elbow=(side*.28,.70,.07);hand=(side*.20,.54,.20)
        p.update({"shoulder."+label:shoulder,"elbow."+label:elbow,"hand."+label:hand,"hand_tip."+label:(hand[0],hand[1]-.05,hand[2]+.05),
            "hip."+label:hip,"knee."+label:knee,"ankle."+label:ankle,"toe."+label:toe})
    return p

def human(name,uniform):
    rest=action_points("Idle",0)
    links=[("root","root","pelvis",None),("pelvis","pelvis","spine","root"),("spine","spine","chest","pelvis"),("chest","chest","neck","spine"),("neck","neck","head","chest"),("head","head","head_tip","neck")]
    for side in ("L","R"):
        links += [("upper_arm."+side,"shoulder."+side,"elbow."+side,"chest"),("forearm."+side,"elbow."+side,"hand."+side,"upper_arm."+side),("hand."+side,"hand."+side,"hand_tip."+side,"forearm."+side),
            ("upper_leg."+side,"hip."+side,"knee."+side,"pelvis"),("lower_leg."+side,"knee."+side,"ankle."+side,"upper_leg."+side),("foot."+side,"ankle."+side,"toe."+side,"lower_leg."+side)]
    bpy.ops.object.armature_add();rig=bpy.context.object;rig.name=name+"Rig"
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in list(rig.data.edit_bones):rig.data.edit_bones.remove(bone)
    for bone,a,b,parent in links:
        item=rig.data.edit_bones.new(bone);item.head=u(rest[a]);item.tail=u(rest[b])
        if parent:item.parent=rig.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    parts=[]
    def skin(ob,bone):
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.select_all(action="DESELECT");ob.select_set(True)
        bpy.ops.object.convert(target="MESH");bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        group=ob.vertex_groups.new(name=bone);group.add(list(range(len(ob.data.vertices))),1,"REPLACE");parts.append(ob)
    # Adult proportions, about six heads tall. Joints and clips are unchanged, so every
    # socket, seat and animation from the previous figures still lines up.
    nurse,receptionist,patient=name=="Nurse",name=="Receptionist",name=="Patient"
    trousers="Ink"
    # Head, face and hair.
    skin(orb("Face",(0,1.405,.012),(.102,.122,.108),"Skin",20),"head")
    skin(orb("Jaw",(0,1.345,.028),(.082,.062,.080),"Skin",16),"head")
    for x in (-.101,.101):skin(orb("Ear",(x,1.395,-.004),(.018,.034,.024),"Skin",10),"head")
    for x in (-.037,.037):
        skin(orb("Eye",(x,1.418,.103),(.012,.014,.007),"Ink",10),"head")
        skin(box("Brow",(x,1.447,.102),(.034,.008,.012),"Ink",.003),"head")
    skin(orb("Nose",(0,1.385,.116),(.017,.030,.020),"Skin",10),"head")
    skin(box("Mouth",(0,1.340,.100),(.036,.007,.010),"Clay",.003),"head")
    skin(orb("Sculpted hair",(0,1.468,-.010),(.110,.072,.114),"Ink",16),"head")
    skin(orb("Back hair",(0,1.410,-.052),(.106,.090,.074),"Ink",14),"head")
    skin(taper("Neck",(0,1.230,0),(0,1.330,.008),.047,.042,"Skin"),"neck")
    # Torso: shoulders broader than the waist.
    skin(taper("Tailored torso",(0,1.200,0),(0,1.000,0),.172,.150,uniform,20,.64),"chest")
    skin(taper("Waist",(0,1.005,0),(0,.840,0),.150,.146,uniform,20,.66),"spine")
    skin(orb("Chest top",(0,1.195,0),(.172,.055,.110),uniform,20),"chest")
    for x in (-.168,.168):skin(orb("Shoulder",(x,1.178,0),(.056,.052,.060),uniform,14),"chest")
    skin(taper("Hip",(0,.860,0),(0,.720,0),.146,.150,trousers,20,.68),"pelvis")
    skin(orb("Seat",(0,.725,0),(.150,.045,.100),trousers,18),"pelvis")
    skin(box("Belt",(0,.832,0),(.316,.032,.206),"Wood" if receptionist else trousers,.012),"pelvis")
    if nurse:
        skin(box("Scrub neckline",(0,1.200,.104),(.074,.052,.010),"Skin",.004),"chest")
        skin(box("Scrub chest pocket",(-.080,1.085,.107),(.078,.070,.010),uniform,.004),"chest")
        skin(box("Pocket pen",(-.098,1.130,.112),(.010,.050,.010),"Gold",.002),"chest")
    elif receptionist:
        skin(box("Shirt placket",(0,1.070,.110),(.052,.230,.010),"Linen",.004),"chest")
        for side in (-1,1):skin(box("Blazer lapel",(side*.052,1.130,.108),(.040,.150,.010),"SageDark",.004),"chest")
        skin(box("Shirt collar",(0,1.215,.090),(.090,.030,.030),"Linen",.008),"chest")
    else:
        skin(orb("Hood",(0,1.215,-.070),(.140,.060,.070),uniform,14),"chest")
        skin(box("Zip",(0,1.050,.110),(.012,.260,.008),"Linen",.003),"chest")
        skin(box("Front pocket",(0,.940,.098),(.180,.070,.012),uniform,.006),"spine")
    if not patient:skin(box("Name badge",(.080,1.110,.110),(.060,.040,.010),"Gold",.004),"chest")
    for side in ("L","R"):
        sleeve="Skin" if nurse else uniform
        skin(taper("upper_arm."+side,rest["shoulder."+side],rest["elbow."+side],.050,.042,uniform),"upper_arm."+side)
        if nurse:skin(orb("Sleeve cuff",rest["elbow."+side],(.047,.030,.047),uniform,12),"upper_arm."+side)
        skin(taper("forearm."+side,rest["elbow."+side],rest["hand."+side],.040,.031,sleeve),"forearm."+side)
        skin(orb("Elbow",rest["elbow."+side],(.041,.041,.041),sleeve,10),"forearm."+side)
        hand=rest["hand."+side]
        skin(orb("Hand",(hand[0],hand[1]-.035,hand[2]),(.030,.050,.026),"Skin",12),"hand."+side)
        skin(orb("Thumb",(hand[0]-(-.020 if side=="L" else .020),hand[1]-.020,hand[2]+.020),(.012,.025,.012),"Skin",8),"hand."+side)
        skin(taper("upper_leg."+side,rest["hip."+side],rest["knee."+side],.076,.056,trousers),"upper_leg."+side)
        skin(orb("Knee",rest["knee."+side],(.056,.056,.056),trousers,12),"lower_leg."+side)
        skin(taper("lower_leg."+side,rest["knee."+side],rest["ankle."+side],.054,.040,trousers),"lower_leg."+side)
        ankle=rest["ankle."+side]
        shoe="Linen" if nurse else "Wood" if receptionist else "Ink"
        skin(orb("Shoe",(ankle[0],.060,.060),(.052,.048,.120),shoe,14),"foot."+side)
        skin(box("Shoe sole",(ankle[0],.014,.060),(.104,.026,.236),"Ink" if nurse else "Clay",.010),"foot."+side)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in parts:ob.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name=name+"Skin"
    mesh.parent=rig;mesh.matrix_parent_inverse=rig.matrix_world.inverted()
    modifier=mesh.modifiers.new("Articulated clinic skeleton","ARMATURE");modifier.object=rig
    rig.animation_data_create()
    for kind in ("Idle","Walk","CheckIn","Treat","Sit","Call"):
        action=bpy.data.actions.new(name+"_"+kind);rig.animation_data.action=action
        for frame in range(1,34,4):
            bpy.context.scene.frame_set(frame);pose=action_points(kind,(frame-1)/32)
            matrices={}
            for bone,a,b,parent in links:
                rest_bone=rig.data.bones[bone]
                target_a,target_b=u(pose[a]),u(pose[b])
                delta=(rest_bone.tail_local-rest_bone.head_local).rotation_difference(target_b-target_a)
                matrix=delta.to_matrix().to_4x4() @ rest_bone.matrix_local
                matrix.translation=target_a;matrices[bone]=matrix
            for bone,a,b,parent in links:
                pb=rig.pose.bones[bone];rest_bone=rig.data.bones[bone]
                # Compute local channels from the desired parent pose, not yesterday's
                # evaluated dependency graph. This also keeps the seated head attached.
                basis=rest_bone.matrix_local.inverted()
                if parent:basis=basis @ rig.data.bones[parent].matrix_local @ matrices[parent].inverted()
                pb.matrix_basis=basis @ matrices[bone]
                pb.rotation_mode="QUATERNION"
                pb.keyframe_insert("location",frame=frame);pb.keyframe_insert("rotation_quaternion",frame=frame);pb.keyframe_insert("scale",frame=frame)
            bpy.context.view_layer.update()
        bpy.context.scene.frame_set(17);bpy.context.view_layer.update()
        expected=action_points(kind,.5)
        for bone,a,b,parent in links:
            error=(rig.pose.bones[bone].head-u(expected[a])).length
            assert error<.002, f"{name} {kind} {bone} detached from authored joint: {error}"
        action.use_fake_user=True
        track=rig.animation_data.nla_tracks.new();track.name=kind
        strip=track.strips.new(kind,1,action);strip.action_frame_start=1;strip.action_frame_end=33
        track.mute=True
    rig.animation_data.action=None
    for track in rig.animation_data.nla_tracks:track.mute=False
    return rig

def clinic_sign():
    curve=bpy.data.curves.new("Little Lifeline raised lettering","FONT")
    curve.body="Little Lifeline";curve.align_x="CENTER";curve.align_y="CENTER"
    curve.font=bpy.data.fonts.load(str(ROOT/"Unity/OrbitOrchard/Assets/IdleClinic/Resources/Fonts/ClinicDisplay.ttf"))
    curve.size=1;curve.extrude=.012;curve.bevel_depth=.002;curve.bevel_resolution=1;curve.resolution_u=5
    ob=bpy.data.objects.new("Clinic name lettering",curve);bpy.context.collection.objects.link(ob)
    bpy.ops.object.select_all(action="DESELECT");ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.object.convert(target="MESH")
    width=max(v.co.x for v in ob.data.vertices)-min(v.co.x for v in ob.data.vertices)
    scale=2.90/width
    for vertex in ob.data.vertices:
        p=vertex.co.copy();vertex.co=u((p.x*scale,p.y*scale,-p.z))
    material(ob,"Linen")

def export(name,make,character=False):
    collection=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(collection)
    bpy.context.view_layer.active_layer_collection=bpy.context.view_layer.layer_collection.children[collection.name]
    make()
    if not character:
        for ob in list(collection.objects):
            if ob.type!="MESH":continue
            bpy.ops.object.select_all(action="DESELECT");ob.select_set(True);bpy.context.view_layer.objects.active=ob
            bpy.ops.object.convert(target="MESH")
        for role in MATS:
            group=[o for o in collection.objects if o.type=="MESH" and o.data.materials[0].name==role]
            if not group:continue
            bpy.ops.object.select_all(action="DESELECT")
            for ob in group:ob.select_set(True)
            bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();bpy.context.object.name=name+"_"+role
        root=bpy.data.objects.new(name,None);collection.objects.link(root)
        for ob in list(collection.objects):
            if ob!=root:ob.parent=root
    else:root=next(o for o in collection.objects if o.type=="ARMATURE")
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.context.scene.frame_set(1)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+".fbx")),use_selection=True,object_types={"EMPTY","MESH","ARMATURE"},
        add_leaf_bones=False,axis_forward="-Z",axis_up="Y",bake_anim=character,bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=character,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0)
    if character:
        for track in root.animation_data.nla_tracks:track.mute=True
        root.animation_data.action=root.animation_data.nla_tracks[0].strips[0].action
    print("CLINIC_ASSET",name,(OUT/(name+".fbx")).stat().st_size,flush=True)
    return root

if SIGN_ONLY:
    for ob in list(bpy.data.objects):
        if ob.name.startswith("ClinicSign"):bpy.data.objects.remove(ob,do_unlink=True)
    root=export("ClinicSign",clinic_sign);root.location=u((6.6,0,10.2))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE),compress=True)
elif not ICON_ONLY:
    roots=[]
    for name,make in (("ReceptionDesk",desk),("TreatmentBay",treatment),("Seat",seat),("WaitingBench",bench),("Cupboard",cupboard),("Plant",lambda:plant((0,0,0))),("ClinicSign",clinic_sign)):
        roots.append(export(name,make))
    for name,uniform in (("Patient","Apricot"),("Receptionist","Sage"),("Nurse","Blue")):
        roots.append(export(name,lambda n=name,c=uniform:human(n,c),True))
    for i,root in enumerate(roots):root.location=u(((i%3)*3.3,0,(i//3)*3.4))
    bpy.context.scene.frame_set(1)
    SOURCE.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE),compress=True)
    print("CLINIC_COMPLETE",SOURCE,flush=True)

def render_icon():
    """A real geometry render: the clinic nurse waving outside a little clinic with a bold care cross,
    on a warm field that stands out on the Home Screen. The cross is green: the red cross on white is a protected emblem. No text or baked border."""
    for collection in bpy.data.collections:collection.hide_render=collection.name!="Nurse"
    root=bpy.data.objects["NurseRig"];root.location=u((.78,0,-.70));root.rotation_euler[2]=math.pi*1.10;root.scale=(1.32,1.32,1.32)
    root.animation_data.action=bpy.data.actions["Nurse_Call"]
    bpy.context.scene.frame_set(9)
    collection=bpy.data.collections.new("App icon sculpture");bpy.context.scene.collection.children.link(collection)
    bpy.context.view_layer.active_layer_collection=bpy.context.view_layer.layer_collection.children[collection.name]
    def paint(role,rgb,rough=.55):
        mat=bpy.data.materials.new(role);mat.use_nodes=True;node=mat.node_tree.nodes.get("Principled BSDF")
        node.inputs["Base Color"].default_value=(*rgb,1);node.inputs["Roughness"].default_value=rough;MATS[role]=mat
    paint("IconField",(1.0,.66,.24));paint("IconCoral",(.03,.42,.20),.4);paint("IconGlass",(.38,.66,.74),.2)
    paint("IconWall",(.97,.95,.88));paint("IconRoof",(.16,.42,.34))
    # The clinic: bright walls, a deep green roof slab, a glass door and a large illuminated cross.
    box("Clinic walls",(-.30,.78,.55),(1.90,1.56,1.10),"IconWall",.10)
    box("Clinic roof",(-.30,1.62,.55),(2.12,.16,1.30),"IconRoof",.07)
    box("Glass door",(-.12,.52,-.02),(.52,1.02,.05),"IconGlass",.03)
    box("Door frame",(-.12,1.07,-.02),(.62,.08,.07),"IconRoof",.02)
    for x in (-.92,.52):box("Window",(x,.98,-.02),(.40,.40,.05),"IconGlass",.03)
    box("Sign plate",(-.30,2.18,.40),(.98,.98,.14),"IconWall",.16)
    box("Care cross",(-.30,2.18,.31),(.72,.24,.08),"IconCoral",.05)
    box("Care cross",(-.30,2.18,.30),(.24,.72,.08),"IconCoral",.05)
    plant((-1.42,0,-.20),1.25);plant((.46,0,-.08),.9)
    box("Field",(0,-.10,0),(200,.15,200),"IconField",0)
    scene=bpy.context.scene;scene.render.engine="CYCLES";scene.cycles.samples=64
    scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG";scene.render.image_settings.color_mode="RGB";scene.render.film_transparent=False
    bpy.ops.object.camera_add(location=(3.2,7,3.6));camera=bpy.context.object
    camera.rotation_euler=(Vector((.12,.05,1.20))-camera.location).to_track_quat("-Z","Y").to_euler()
    camera.data.type="ORTHO";camera.data.ortho_scale=3.75;scene.camera=camera
    bpy.ops.object.light_add(type="AREA",location=(2,5,7));key=bpy.context.object
    key.data.energy=900;key.data.shape="DISK";key.data.size=5
    scene.world=bpy.data.worlds.new("Warm clinic afternoon");scene.world.use_nodes=True
    scene.world.node_tree.nodes.get("Background").inputs[0].default_value=(1.0,.84,.58,1)
    scene.world.node_tree.nodes.get("Background").inputs[1].default_value=.75
    scene.view_settings.view_transform="Standard";scene.view_settings.exposure=-.15
    scene.render.filepath=os.environ.get("CLINIC_ICON_OUT") or str(OUT.parents[2]/"AppIcon.png")
    bpy.ops.render.render(write_still=True)
    print("CLINIC_ICON",scene.render.filepath,flush=True)

if "--icon" in sys.argv or ICON_ONLY:render_icon()
