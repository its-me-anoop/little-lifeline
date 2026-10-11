"""Package original progression models and author decorations, amenities and theme samples."""
import sys,math,json,random,re
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path('/Users/anoopjose/Projects/little-lifeline');OUT=ROOT/'assets/clinic-reference/game-library'
sys.path.insert(0,str(ROOT/'Tools'))
import gear_kit as kit
from gear_kit import box,orb,tube,disc,pole
catalog=json.loads((OUT/'catalog.json').read_text());kit.MODELS=OUT
# All additional models use the same named material roles as the game's gear renderer.
def frame(k):
    box((0,.48,0),(.72,.88,.055),k.trim);box((0,.48,-.034),(.64,.80,.018),'Paper')
def plant(k):
    disc(0,.14,0,.17,.28,'Terracotta');disc(0,.285,0,.15,.015,'Soil')
    for j in range(5+k.v//3):
        a=j*2.4;orb((.10*math.sin(a),.52+j*.015,.10*math.cos(a)),(.15,.30,.11),'Leaf')
def botanical(k):
    frame(k)
    for j in range(3):
        x=(j-1)*.13;tube((x,.16,-.053),(x,.73,-.053),.009,'Leaf')
        for n in range(3):orb((x+(-1 if n%2 else 1)*.055,.28+n*.15,-.065),(.12,.09,.018),k.accent)
def mirror(k):
    orb((0,.5,0),(.70,.70,.055),k.trim);orb((0,.5,-.036),(.60,.60,.012),'Chrome')
def clock(k):
    orb((0,.45,0),(.56,.56,.055),k.trim);orb((0,.45,-.036),(.48,.48,.013),'Paper')
    tube((0,.45,-.05),(0,.62,-.05),.012,'Ink');tube((0,.45,-.05),(.12,.38,-.05),.012,'Ink')
    for j in range(12):
        a=j*math.tau/12;orb((.21*math.sin(a),.45+.21*math.cos(a),-.05),(.018,.018,.008),'Ink')
def shelf(k):
    box((0,.23,0),(.83,.065,.25),'Walnut')
    for j in range(3+k.v//3):box((-.29+j*.085,.36,.015),(.055,.23,.13),k.accent if j%2 else 'Linen')
    disc(.27,.36,0,.075,.20,'Clay');orb((.27,.54,0),(.17,.17,.16),'Leaf')
def vase(k):
    orb((0,.17,0),(.22,.32,.22),k.body)
    for j in range(3+k.v//3):
        x=(j-(3+k.v//3-1)/2)*.04;tube((0,.27,0),(x,.53,0),.006,'Leaf');orb((x,.54,0),(.085,.08,.08),k.accent if j%2 else 'Rose')
def rug(k):
    box((0,.01,0),(1.7,.02,1.15),k.body,.01)
    for x in [-.77,.77]:box((x,.024,0),(.06,.006,1.04),k.trim,.001)
    for z in [-.49,.49]:box((0,.025,z),(1.6,.006,.06),k.trim,.001)
    for j in range(2+k.v//2):box((-.65+j*.16,.026,0),(.05,.006,.70),k.accent,.001)
def pendant(k):
    pole(0,0,.50,.014,k.trim,y0=.5);orb((0,.5,0),(.40,.20,.40),k.body);disc(0,.43,0,.13,.018,'LampLight')
def sconce(k):
    box((0,.40,.055),(.13,.35,.04),k.trim);tube((0,.35,.04),(0,.40,-.14),.02,k.trim);orb((0,.48,-.14),(.24,.28,.22),'LampLight')
def tablelamp(k):
    disc(0,.025,0,.13,.05,k.trim);pole(0,0,.39,.02,k.trim);orb((0,.42,0),(.32,.22,.32),'LampLight')
def award(k):
    box((0,.06,0),(.26,.12,.22),'Walnut');tube((0,.12,0),(0,.24,0),.025,'Gold');orb((0,.31,0),(.18,.22,.18),'Gold')
    for side in [-1,1]:orb((side*.11,.31,0),(.10,.14,.08),'Gold')
def donation(k):
    box((0,.18,0),(.35,.36,.26),k.body);box((0,.365,0),(.19,.008,.03),'Ink');box((0,.22,-.136),(.24,.17,.012),'Paper')
    box((0,.22,-.148),(.04,.12,.008),'Rose');box((0,.22,-.151),(.12,.04,.008),'Rose')
def books(k):
    for j in range(3+k.v//2):box((0,.025+j*.045,0),(.32,.04,.25),k.accent if j%2 else 'Blue')
def healthposter(k):
    frame(k);orb((0,.62,-.049),(.14,.14,.012),k.accent);box((0,.43,-.050),(.18,.23,.012),k.accent)
    for y in [.22,.15]:box((0,y,-.052),(.40,.01,.007),'Ink',.001)
def directions(k):
    box((0,.38,0),(.82,.36,.05),k.body)
    for y in [.31,.45]:
        box((-.03,y,-.03),(.42,.035,.009),k.accent);box((.24,y,-.03),(.12,.035,.009),k.trim).rotation_euler.y=.7
        box((.24,y-.055,-.03),(.12,.035,.009),k.trim).rotation_euler.y=-.7
def tissue(k):
    box((0,.09,0),(.29,.18,.21),k.body);box((0,.184,0),(.16,.008,.045),'Ink');box((0,.23,0),(.09,.10,.013),'Paper')
def sanitizer(k):
    box((0,.19,0),(.13,.26,.09),k.body);box((0,.32,0),(.11,.026,.10),k.trim);box((0,.19,-.054),(.08,.10,.008),'Paper')
def marking(k):
    box((0,.005,0),(.65,.01,.65),k.body,.01)
    for side in [-1,1]:orb((side*.12,.014,0),(.095,.01,.30),k.accent)
def curtains(k):
    tube((-.6,1.4,0),(.6,1.4,0),.025,k.trim)
    for side in [-1,1]:
        for j in range(4):box((side*(.35+j*.065),.73,.01),(.06,1.28,.045),k.accent,.025)
def acoustic(k):
    box((0,.48,0),(.76,.95,.08),k.body)
    for j in range(8):box((-.32+j*.09,.48,-.06),(.04,.87,.035),k.accent,.007)
def terrarium(k):
    box((0,.08,0),(.38,.16,.25),'Walnut');orb((0,.27,0),(.36,.39,.24),'Acrylic');orb((0,.21,-.06),(.21,.22,.14),'Leaf')
DECOR=[('BotanicalPrint',botanical),('RoundMirror',mirror),('WallClock',clock),('DisplayShelf',shelf),('FlowerVase',vase),('FloorPlanter',plant),('BorderRug',rug),('PendantLight',pendant),('WallSconce',sconce),('TableLamp',tablelamp),('CareAward',award),('DonationBox',donation),('BookStack',books),('HealthPoster',healthposter),('DirectionSign',directions),('TissueBox',tissue),('SanitizerDispenser',sanitizer),('QueueFloorMarker',marking),('Curtains',curtains),('AcousticPanel',acoustic),('Terrarium',terrarium)]
# Shared amenities have the game's three amenity levels.
def toilet(k):
    orb((0,.35,0),(.42,.46,.52),'Linen');orb((0,.51,-.06),(.54,.17,.66),'Linen');orb((0,.60,-.06),(.37,.025,.46),'Ink');box((0,.75,.30),(.52,.70,.18),'Linen')
    box((.17,1.11,.30),(.08,.024,.06),'Chrome')
    tube((.42,.30,.25),(.42,.88,.25),.022,'Chrome');tube((.42,.88,.25),(.42,.88,-.35),.022,'Chrome')
    if k.v>=2:box((-.43,.62,.16),(.08,.16,.10),k.accent)
    if k.v>=3:box((-.43,.72,.15),(.025,.02,.08),'Chrome')
def basin(k):
    box((0,.40,0),(.65,.80,.43),'Oak');box((0,.84,0),(.71,.08,.49),'Quartz');box((0,.89,-.03),(.43,.012,.25),'Ink')
    tube((0,.86,.16),(0,1.08,.16),.02,'Chrome');tube((0,1.08,.16),(0,1.08,-.01),.02,'Chrome');box((0,1.45,.20),(.63,.58,.025),'Chrome')
    if k.v>=2:sanitizer(k)
    if k.v>=3:box((.5,1.16,.19),(.22,.28,.14),'BrushedSteel')
def baby(k):
    box((0,.9,.10),(.70,.50,.16),'Ivory');box((0,.66,-.18),(.70,.08,.60),'Ivory');box((0,.715,-.18),(.05,.025,.50),k.accent)
def cord(k):
    tube((0,.12,0),(0,1.80,0),.009,'Crimson');orb((0,.15,0),(.06,.10,.04),'Crimson');box((0,1.81,0),(.08,.08,.03),'Paper')
def vending(k):
    box((0,.85,0),(.90,1.70,.65),k.body);box((-.11,1.05,-.34),(.55,.89,.025),'Glass');box((0,.26,-.35),(.64,.22,.03),'Ink')
    for row in range(k.v):
        for col in range(3):tube((-.29+col*.18,.72+row*.27,-.37),(-.29+col*.18,.86+row*.27,-.37),.038,k.accent if row%2 else 'Blue')
    box((.31,1.04,-.35),(.12,.25,.03),'Ink');box((.31,1.13,-.375),(.10,.08,.009),'ScreenUI');box((.31,.96,-.376),(.07,.07,.012),'Gold')
def gate(k):
    box((0,.50,0),(.25,1,.28),k.body);box((1.25,.93,0),(2.55,.075,.07),'Linen')
    for j in range(7):box((.25+j*.34,.936,-.04),(.17,.075,.012),'Crimson')
    if k.v>=2:box((0,1.17,0),(.18,.30,.13),'Ink');box((0,1.19,-.075),(.12,.12,.012),'ScreenUI')
    if k.v>=3:orb((0,1.40,0),(.10,.10,.10),'Glow')
def bench(k):
    box((0,.46,0),(1.45,.12,.47),'Oak');box((0,.72,.22),(1.45,.40,.065),'Oak')
    for x in [-.56,.56]:box((x,.23,0),(.055,.46,.40),'BrushedSteel')
    if k.v>=2:
        for x in [-.76,.76]:box((x,.66,0),(.04,.04,.48),'BrushedSteel')
    if k.v>=3:box((0,.53,0),(1.35,.04,.42),'Fabric')
def bins(k):
    for j in range(k.v):
        x=j*.32;box((x,.30,0),(.26,.60,.26),k.body);box((x,.62,0),(.28,.04,.28),['Sage','Blue','Apricot'][j]);box((x,.42,-.14),(.12,.12,.012),'Paper')
AMENITIES=[('AccessibleToilet',toilet),('Washstand',basin),('BabyChangingStation',baby),('EmergencyPullCord',cord),('VendingMachine',vending),('ParkingBarrier',gate),('GardenBench',bench),('RecyclingStation',bins)]
# Generic FBX exporter keeps each upgrade as a separately named root at the same origin.
def export_variants(category,name,make,count):
    roots=[]
    for v in range(1,count+1):
        root,col=kit.build_version(f'{name}_V{v}',make,v,{},0);root['upgrade_level']=v;root['asset_category']=category;root.asset_mark();roots.append(root)
    bpy.ops.object.select_all(action='DESELECT')
    for root in roots:
        root.select_set(True)
        for ob in root.children:ob.select_set(True)
    bpy.context.view_layer.objects.active=roots[0]
    dest=OUT/category;dest.mkdir(exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(dest/(name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
    bpy.data.libraries.write(str(dest/(name+'.blend')),{r.users_collection[0] for r in roots},fake_user=True)
    kit.clear()
    print('NEW_ASSET',category,name,count,flush=True)
for name,make in DECOR:export_variants('decorations',name,make,10)
for name,make in AMENITIES:export_variants('amenities',name,make,3)
catalog['decorations']=[{'name':n,'file':f'decorations/{n}.fbx','versions':10,'roots':[f'{n}_V{v}' for v in range(1,11)]} for n,_ in DECOR]
catalog['amenities']=[{'name':n,'file':f'amenities/{n}.fbx','levels':3} for n,_ in AMENITIES]
# Contact sheets render 20 recognizable objects instead of overlapping every upgrade.
def setup_camera(target,scale,width,height):
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
    scene.display.shading.background_type='WORLD';scene.world=bpy.data.worlds.new('Gallery world');scene.world.color=(.83,.86,.88)
    bpy.ops.object.camera_add(location=(target[0]+2,target[1]+16,target[2]+18));cam=bpy.context.object;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale;scene.camera=cam
    scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.resolution_percentage=100

def label(text,x,y,z,size=.17):
    bpy.ops.object.text_add(location=(x,y,z));ob=bpy.context.object;ob.data.body=text;ob.data.size=size;ob.data.align_x='CENTER';ob.data.materials.append(kit.MATS['Ink'])

def sheet(entries,name,versions):
    kit.clear();saved=[]
    for i,(display,make,foot,index) in enumerate(entries):
        for offset,v in enumerate(versions):
            root,col=kit.build_version(f'{name}_{i+1:02d}_V{v}',make,v,foot,index)
            # Each contact cell has an independent true-scale source root, with a scaled preview display.
            bpy.context.view_layer.update();coords=[ob.matrix_world@Vector(p) for ob in root.children if ob.type=='MESH' for p in ob.bound_box]
            extent=max(max(p[j] for p in coords)-min(p[j] for p in coords) for j in range(3));root.scale=(min(1,1.6/max(extent,.01)),)*3
            root.location=(-i%5*0,0,0) if False else ((i%5-2)*3.9+offset*.95-(len(versions)-1)*.475,-(i//5)*3.6,0)
            root.asset_mark();saved.append(root)
        label(display,(i%5-2)*3.9,-(i//5)*3.6+1.1,.025,.15)
    setup_camera((0,-5.5,.5),22,2200,1600)
    previews=OUT/'previews';previews.mkdir(exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(previews/(name+'.blend')))
    bpy.context.scene.render.filepath=str(previews/(name+'.png'));bpy.ops.render.render(write_still=True);print('PREVIEW',name,flush=True)
# Existing model recipes provide the exact Basic and Advanced silhouettes of the game.
rooms=['Reception','FirstAid','Waiting','Consultation','Pharmacy','Office','StaffRoom','Store']
for room,module_name in zip(rooms,['reception','firstaid','waiting','consultation','pharmacy','office','staffroom','store']):
    module=__import__('gear_items_'+module_name);names=[e['name'] for e in catalog['equipment'] if e['room']==room]
    sheet([(n,f,module.FOOT,i) for i,(n,f) in enumerate(zip(names,module.ITEMS),1)],room+'-equipment',[1,10])
sheet([(n,f,{},0) for n,f in DECOR],'Decorations',[5])
sheet([(n,f,{},0) for n,f in AMENITIES],'Amenities',[3])
(OUT/'catalog.json').write_text(json.dumps(catalog,indent=2))
print('EQUIPMENT_AND_DECOR_COMPLETE',flush=True)
