"""Two furnished nursing stations with adjustable treatment chair-beds."""
import math,sys
from pathlib import Path
import bpy
from mathutils import Vector
OUT=Path(sys.argv[sys.argv.index('--')+1]);folder=OUT/'furniture';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
scene=bpy.context.scene
# Replace only an earlier run of these stations, if present.
old=bpy.data.collections.get('14 Nursing stations')
if old:
    for ob in list(old.objects):bpy.data.objects.remove(ob,do_unlink=True)
    bpy.data.collections.remove(old)
collection=bpy.data.collections.new('14 Nursing stations');scene.collection.children.link(collection)
mats={}
for name,c in {'Ivory':(.88,.86,.78),'Teal':(.035,.38,.39),'Blue':(.12,.30,.57),'Metal':(.40,.48,.51),'Dark':(.02,.035,.055),'White':(.96,.95,.90),'Coral':(.81,.19,.13),'Glass':(.29,.66,.73)}.items():
    mat=bpy.data.materials.new('Nursing '+name);mat.use_nodes=True;mat.diffuse_color=(*c,1);node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*c,1);node.inputs['Roughness'].default_value=.48
    if name=='Metal':node.inputs['Metallic'].default_value=.55
    mats[name]=mat
root=None

def link(ob,name,material=None,parent=None):
    ob.name=name
    for coll in list(ob.users_collection):coll.objects.unlink(ob)
    collection.objects.link(ob)
    if material:ob.data.materials.append(mats[material])
    ob.parent=parent if parent is not None else root
    return ob

def empty(name,xyz,parent=None):
    ob=bpy.data.objects.new(name,None);collection.objects.link(ob);ob.parent=parent if parent is not None else root;ob.location=xyz;return ob

def box(name,xyz,size,material,parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1);ob=link(bpy.context.object,name,material,parent);ob.location=xyz;ob.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=ob.modifiers.new('Rounded edges','BEVEL');mod.width=.025;mod.segments=3;ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return ob

def cyl(name,xyz,radius,depth,material,parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=radius,depth=depth);ob=link(bpy.context.object,name,material,parent);ob.location=xyz;return ob

def hinge_driver(pivot,bed,angle):
    driver=pivot.driver_add('rotation_euler',0).driver;driver.expression=f'recline*{angle}'
    variable=driver.variables.new();variable.name='recline';variable.type='SINGLE_PROP';variable.targets[0].id=bed;variable.targets[0].data_path='["chair_mode"]'

station_roots=[]
for index,(x,color,mode) in enumerate([(-7,'Teal',.70),(2,'Blue',0.0)],1):
    root=None;root=empty(f'NursingStation_{index}',(x,3.3,.12));station_roots.append(root)
    root['contents']='Convertible chair-bed, stool, rolling supply trolley, storage, sink, monitor, privacy screen'
    bed=empty(f'Station{index}_ConvertibleChairBed',(-.32,.10,0));bed['chair_mode']=mode
    bed.id_properties_ui('chair_mode').update(min=0,max=1,description='0 = flat treatment bed; 1 = upright chair; values between = reclined')
    box('Chair bed floor base',(0,0,.08),(.84,1.08,.16),'Dark',bed)
    cyl('Hydraulic lift',(0,0,.36),.14,.46,'Metal',bed)
    box('Lift chassis',(0,0,.64),(.80,.85,.17),'Ivory',bed)
    box('Seat upholstery',(0,0,.81),(.85,.72,.16),color,bed)
    back=empty('Adjustable backrest hinge',(0,.35,.80),bed);hinge_driver(back,bed,math.radians(65))
    box('Backrest support',(0,.47,-.035),(.85,.97,.08),'Ivory',back)
    box('Backrest cushion',(0,.47,.035),(.84,.97,.13),color,back)
    box('Adjustable head cushion',(0,.86,.13),(.66,.25,.105),'White',back)
    leg=empty('Adjustable legrest hinge',(0,-.35,.80),bed);hinge_driver(leg,bed,math.radians(56))
    box('Legrest cushion',(0,-.39,.035),(.80,.79,.14),color,leg)
    box('Footrest',(0,-.81,-.006),(.73,.15,.08),'Ivory',leg)
    for side in [-1,1]:
        cyl('Arm support',(side*.48,0,.87),.022,.35,'Metal',bed)
        box('Padded armrest',(side*.48,0,1.05),(.16,.69,.09),color,bed)
    box('Bed side control panel',(.52,-.23,.74),(.055,.21,.12),'Ivory',bed)
    for y in [-.27,-.21]:box('Control button',(.553,y,.76),(.008,.027,.024),'Blue',bed)
    # Clinician stool at the head-side corner; clear space beside the patient.
    cyl('Stool seat',(.84,.80,.54),.25,.10,color)
    cyl('Stool gas lift',(.84,.80,.28),.035,.47,'Metal')
    for j in range(5):
        a=j*math.tau/5;box('Stool star foot',(.84+.10*math.cos(a),.80+.10*math.sin(a),.09),(.35,.04,.035),'Metal').rotation_euler.z=a
        cyl('Stool caster',(.84+.24*math.cos(a),.80+.24*math.sin(a),.05),.045,.07,'Dark')
    # Rear storage and handwash station sit against the back wall.
    box('Supply base cabinet',(-.89,1.60,.42),(1.12,.48,.84),'Ivory')
    box('Cabinet countertop',(-.89,1.60,.87),(1.17,.52,.055),'White')
    for dx in [-.27,.27]:
        box('Cabinet door',(-.89+dx,1.345,.44),(.51,.025,.70),'Ivory')
        box('Cabinet handle',(-.89+dx,1.318,.62),(.15,.028,.018),'Metal')
    box('Sink rim',(-1.15,1.60,.91),(.42,.33,.045),'Metal')
    box('Sink basin',(-1.15,1.60,.938),(.32,.23,.016),'Dark')
    cyl('Tap riser',(-1.15,1.76,1.04),.021,.25,'Metal')
    box('Tap spout',(-1.15,1.68,1.16),(.04,.18,.04),'Metal')
    cyl('Soap bottle',(-.64,1.59,1.00),.05,.20,'Glass')
    box('Soap pump',(-.64,1.59,1.11),(.08,.04,.025),'White')
    box('Upper supply cabinet',(-.91,1.72,1.47),(1.13,.22,.45),'Ivory')
    box('Medical cross vertical',(-.91,1.596,1.47),(.05,.025,.21),'Coral')
    box('Medical cross horizontal',(-.91,1.590,1.47),(.20,.026,.05),'Coral')
    # Mobile treatment trolley and diagnostic screen.
    for z in [.18,.50,.83]:box('Trolley shelf',(1.05,1.15,z),(.57,.43,.055),'Metal')
    for dx in [-.25,.25]:
        for dy in [-.18,.18]:
            cyl('Trolley upright',(1.05+dx,1.15+dy,.49),.018,.69,'Metal')
            cyl('Trolley wheel',(1.05+dx,1.15+dy,.07),.048,.08,'Dark')
    box('Supply tray',(1.04,1.15,.89),(.41,.28,.065),'White')
    for dx in [-.12,0,.12]:cyl('Supply bottle',(1.05+dx,1.17,.99),.035,.15,'Glass')
    box('Monitor stem',(.79,1.61,1.07),(.035,.035,.36),'Metal')
    box('Patient monitor',(.79,1.60,1.30),(.48,.06,.30),'Dark')
    box('Monitor screen',(.79,1.562,1.30),(.42,.012,.25),'Blue')
    for j,h in enumerate([.04,.08,.15,.09,.05,.12]):box('Monitor vital trace',(.62+j*.055,1.551,1.25+h/2),(.032,.008,h),'White')
    # Slim privacy panel follows the outside room wall.
    box('Privacy screen',(-1.52,.0,1.14),(.045,1.20,1.58),'White')
    for y in [-.59,.59]:cyl('Privacy frame',(-1.52,y,.98),.025,1.88,'Metal')
    box('Clinical waste bin',(1.35,-.55,.23),(.28,.28,.46),'Ivory')
    box('Waste bin lid',(1.35,-.55,.48),(.30,.30,.035),color)
# One existing original nurse per station.
root=None
with bpy.data.libraries.load(str(OUT/'characters/medical-staff/little-lifeline-medical-staff.blend'),link=False) as (src,dst):dst.objects=[n for n in src.objects if n.startswith('Nurse_')]
for ob in dst.objects:
    if ob:collection.objects.link(ob)
rigs=sorted([o for o in dst.objects if o and o.type=='ARMATURE'],key=lambda o:o.name)
for rig,x in zip(rigs,[-6.15,2.85]):
    rig.location=(x,3.05,.12);rig.rotation_euler.z=-math.pi/2
    rig.animation_data.action=bpy.data.actions.get(rig.name.removesuffix('Rig')+'_Idle')
# Remove the old floor labels where furniture now stands.
for ob in list(scene.objects):
    if ob.type=='FONT' and ob.data.body in ['Nursing Room 1','Nursing Room 2']:bpy.data.objects.remove(ob,do_unlink=True)
scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
# Standalone furniture library preserves the adjustable hinges and their controls.
bpy.data.libraries.write(str(folder/'little-lifeline-nursing-stations.blend'),{collection},fake_user=True)
for root in station_roots:
    def descendants(ob):
        result=[]
        for child in ob.children:result.append(child);result.extend(descendants(child))
        return result
    bpy.ops.object.select_all(action='DESELECT')
    for ob in [root]+descendants(root):ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(folder/(root.name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
# Preview both furnished rooms with an overhead view.
cam=scene.camera;cam.location=(-2.4,-5,15);cam.rotation_euler=(Vector((-2.4,3.5,.7))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=15.0
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1800;scene.render.resolution_y=950;scene.render.resolution_percentage=100
scene.render.filepath=str(folder/'little-lifeline-nursing-stations.png');bpy.ops.render.render(write_still=True)
print('NURSING_STATIONS_COMPLETE')
