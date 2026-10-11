"""Generate the Little Lifeline clinic reference scene in Blender."""
from pathlib import Path
import math, random, sys
import bpy
from mathutils import Vector

OUT = Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else Path(__file__).parent
EXTERIOR='--exterior' in sys.argv
BUILDING_ONLY='--building-only' in sys.argv or EXTERIOR
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
random.seed(12)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
colors={'cream':(.96,.86,.65),'wall':(.055,.34,.32),'white':(.98,.96,.86),'blue':(.015,.39,.86),'wood':(.73,.38,.15),'ink':(.028,.055,.10),'metal':(.25,.36,.42),'grass':(.28,.56,.075),'leaf':(.055,.35,.045),'paving':(.65,.66,.59),'road':(.07,.105,.15),'glass':(.065,.62,.82),'pink':(.98,.10,.29),'yellow':(1,.58,.025),'coral':(.95,.18,.075),'lavender':(.42,.14,.74),'mint':(.12,.70,.42),'leaf_light':(.21,.53,.04)}
mats={}
colors['wall']=(.86,.79,.65)
colors['cream']=(.22,.31,.38)
colors['grout']=(.34,.43,.49)
for name,c in colors.items():
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*c,1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65
    mats[name]=m
storefront=mats['glass'].copy();storefront.name='Transparent storefront glass'
storefront.diffuse_color=(.30,.73,.83,.18)
glass_node=storefront.node_tree.nodes['Principled BSDF']
glass_node.inputs['Base Color'].default_value=(.30,.73,.83,1)
glass_node.inputs['Alpha'].default_value=.18
glass_node.inputs['Roughness'].default_value=.08
glass_node.inputs['Transmission Weight'].default_value=.4
if hasattr(storefront,'surface_render_method'):storefront.surface_render_method='DITHERED'
mats['storefront']=storefront
groups={}
def group(name):
    if name not in groups:
        groups[name]=bpy.data.collections.new(name);scene.collection.children.link(groups[name])
    global current
    current=groups[name]
def finish(o,name,mat):
    o.name=name;o.data.materials.append(mats[mat])
    for c in list(o.users_collection):c.objects.unlink(o)
    current.objects.link(o)
    return o
def box(name,x,y,z,w,d,h,mat,bevel=.03):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z));o=bpy.context.object;o.scale=(w,d,h)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);finish(o,name,mat)
    if bevel:
        b=o.modifiers.new('Rounded edges','BEVEL');b.width=bevel;b.segments=2
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
def cyl(name,x,y,z,r,h,mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=h,location=(x,y,z));return finish(bpy.context.object,name,mat)
def ball(name,x,y,z,r,mat):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=r,location=(x,y,z));return finish(bpy.context.object,name,mat)
def text(name,label,x,y,z,size=.26,mat='ink'):
    bpy.ops.object.text_add(location=(x,y,z));o=bpy.context.object;o.data.body=label;o.data.size=size;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.extrude=.001;return finish(o,name,mat)
def plant(x,y,s=.7):
    cyl('Plant pot',x,y,.22*s,.22*s,.44*s,'wood')
    for i in range(5):
        a=i*2.4;o=ball('Plant leaves',x+.16*s*math.cos(a),y+.16*s*math.sin(a),.58*s,.26*s,'leaf');o.scale.z=1.5
def chair(x,y,angle=0):
    parts=[]
    for dx in [-.24,.24]:
        for dy in [-.22,.22]:parts.append(cyl('Chair leg',x+dx,y+dy,.23,.025,.46,'metal'))
    parts.append(box('Chair seat',x,y,.48,.66,.62,.12,'ink'))
    parts.append(box('Chair back',x,y+.27,.80,.66,.10,.62,'ink'))
    for o in parts:
        dx,dy=o.location.x-x,o.location.y-y;o.location.x=x+dx*math.cos(angle)-dy*math.sin(angle);o.location.y=y+dx*math.sin(angle)+dy*math.cos(angle);o.rotation_euler.z=angle
def desk(x,y,w=1.8):
    box('Desk top',x,y,.81,w,.85,.10,'wood')
    for dx in [-w/2+.12,w/2-.12]:box('Desk support',x+dx,y,.39,.12,.70,.78,'white')
    box('Monitor',x,y+.1,1.10,.44,.08,.32,'ink');box('Screen',x,y+.055,1.10,.38,.015,.25,'blue');box('Keyboard',x,y-.21,.88,.40,.16,.025,'ink')
def wall_h(x1,x2,y):
    box('Wall', (x1+x2)/2,y,.95,x2-x1,.18,1.90,'wall')
    box('Wall cap',(x1+x2)/2,y,1.915,x2-x1+.015,.205,.045,'metal',.012)
def wall_v(x,y1,y2):
    box('Wall',x,(y1+y2)/2,.95,.18,y2-y1,1.90,'wall')
    box('Wall cap',x,(y1+y2)/2,1.915,.205,y2-y1+.015,.045,'metal',.012)
def door(x,y,w=1):
    box('Door threshold',x,y,.025,w,.15,.05,'blue')
    main=w>1.5
    height=1.95 if main else 1.85
    for side in [-1,1]:box('Entrance frame' if main else 'Room door jamb',x+side*(w/2+.035),y,height/2,.075,.22,height,'wood')
    box('Door lintel',x,y,height+.035,w+.18,.23,.075,'wood')
    if main:
        for side in [-1,1]:
            px=x+side*w/4
            box('Entrance glazed door',px,y-.025,.96,w/2-.075,.055,1.65,'storefront',.035)
            for dx in [-w/4+.04,w/4-.04]:box('Entrance door vertical trim',px+dx,y-.065,.96,.045,.075,1.7,'white',.015)
            for z in [.15,.9,1.8]:box('Entrance door horizontal trim',px,y-.07,z,w/2-.045,.085,.045,'white',.01)
            cyl('Entrance pull handle',x+side*.11,y-.13,1.02,.023,.38,'yellow')
        box('Entrance canopy',x,y-.40,2.15,w+1.05,1.25,.14,'ink',.09)
        box('Canopy gold lip',x,y-1.01,2.15,w+1.06,.045,.08,'yellow',.02)
        box('Entrance sign backing',x,y-.54,2.39,w+.5,.09,.38,'white',.04)
        label=text('Welcome signage','LITTLE LIFELINE',x,y-.595,2.38,.18,'wall');label.rotation_euler=(math.pi/2,0,0)
        for side in [-1,1]:
            ball('Entrance warm light',x+side*(w/2+.2),y-.20,1.54,.085,'yellow')
            plant(x+side*(w/2+.5),y-.7,.8)
        box('Entrance welcome mat',x,y-.68,.11,w+.2,.85,.025,'blue',.06)
        label=text('Welcome mat label','WELCOME',x,y-.70,.135,.18,'white')
    else:
        # Hinged room doors sit partly open into their rooms.
        angle=math.radians(55);px=x-w/2+math.cos(angle)*w/2;py=y+math.sin(angle)*w/2
        leaf=box('Open room door',px,py,.92,w-.07,.055,1.76,'blue',.035);leaf.rotation_euler.z=angle
        pane=box('Room door glass',px,py-.034,1.20,w*.67,.016,.90,'glass',.02);pane.rotation_euler.z=angle
        knob=ball('Room door handle',x-w/2+math.cos(angle)*(w-.16),y+math.sin(angle)*(w-.16),.94,.035,'yellow')
    # Thin arc shows door swing while leaving the opening unobstructed.
    curve=bpy.data.curves.new('Door swing','CURVE');curve.dimensions='3D';curve.bevel_depth=.009
    spl=curve.splines.new('POLY');spl.points.add(20)
    for i,p in enumerate(spl.points):
        a=i/20*math.pi/2;p.co=(x-w/2+w*math.cos(a),y+w*math.sin(a),.045,1)
    o=bpy.data.objects.new('Door swing',curve);current.objects.link(o);curve.materials.append(mats['metal'])
group('01 Site')
box('Landscaped site',-6.5,-2,-.25,35.4,25,.4,'grass',.15)
box('Clinic apron',0,0,-.05,18.8,12.5,.15,'paving')
box('Clinic floor',0,0,.04,17.6,10.6,.12,'cream')
box('Public road',-6.5,-11.5,-.04,35.4,6,.14,'road')
box('Public sidewalk',-6.5,-7.9,0,35.4,1.1,.15,'paving')
box('Main entrance path',-.7,-6.7,.02,2.1,3,.12,'paving')
for x in range(-23,10,4):box('Road marking',x,-11.5,.04,1.8,.08,.01,'white',0)
for x in [-21,-8,6]:
    for y,angle in [(-10,math.pi/2),(-13,-math.pi/2)]:
        arrow=text('Main street direction','↑',x,y,.045,.65,'white');arrow.rotation_euler.z=angle
for y in [-8.65,-14.35]:box('Street edge line',-6.5,y,.04,35.1,.045,.01,'white',0)
for x in [i*.6-8.4 for i in range(29)]:box('Tile grout',x,0,.105,.012,10.4,.005,'grout',0)
for y in [i*.6-5.1 for i in range(18)]:box('Tile grout',0,y,.105,17.4,.012,.005,'grout',0)
group('02 Clinic shell and windows')
wall_h(-8.8,8.8,5.3);wall_v(-8.8,-5.3,5.3);wall_v(8.8,-5.3,5.3)
for left,right in [(-8.8,-1.7),(.3,8.8)]:
    mid=(left+right)/2
    box('Transparent front glazing',mid,-5.3,.99,right-left,.028,1.82,'storefront',.005)
    for z in [.085,1.92]:box('Storefront horizontal frame',mid,-5.3,z,right-left,.075,.055,'metal',.012)
    count=math.ceil((right-left)/1.6)
    for j in range(count+1):box('Slim storefront mullion',left+(right-left)*j/count,-5.3,1,.045,.07,1.86,'metal',.01)
door(-.7,-5.3,2)
for y in [-3.3,0,3.2]:
    for x in [-8.79,8.79]:box('Blue side window',x,y,1.23,.20,1.65,.80,'glass')
for x in [-7,-4.5,-1.5,1.4,4.2,7]:
    box('Rear window',x,5.29,1.25,1.6,.21,.80,'glass')
for x in [-5.2,-2.7,-.1,4.3]:wall_v(x,1.5,5.3)
for a,b in [(-8.8,-6.5),(-5.5,-5.2),(-5.2,-4.5),(-3.5,-2.7),(-2.7,-2.2),(-1.2,-.1),(-.1,.5),(1.5,4.3),(4.3,5.1),(6.1,8.8)]:wall_h(a,b,1.5)
for x in [-6,-4,-1.7,1,5.6]:door(x,1.5)
group('03 Nursing rooms')
for x,label in [(-7,'Nursing Room 1'),(2.8,'Nursing Room 2')]:
    box('Treatment bed base',x,3.3,.45,.9,2,.55,'white');box('Mattress',x,3.3,.77,.85,1.95,.16,'white');box('Bed blanket',x,2.9,.87,.88,.85,.04,'blue');box('Pillow',x,3.95,.88,.68,.40,.13,'white')
    cyl('Clinician stool',x+1,2.7,.5,.25,.14,'ink');cyl('Stool column',x+1,2.7,.25,.035,.4,'metal')
    box('Medical trolley',x+1,4.3,.58,.55,.45,.12,'metal');box('Supply cabinet',x+1,4.8,.4,.55,.35,.8,'white')
    text(label,label+'\n4.0 × 3.6 m',x,2,.13,.23);plant(x+1,4.6,.65)
group('04 Utilities and accessible toilet')
box('Utility cabinet',-4.9,4,.55,.40,1.7,1.1,'white');box('Utility sink',-3.1,2.2,.75,.5,.5,.16,'metal')
text('Utility label','Cleaner / Utilities\n2.5 × 2.4 m',-4,3,.13,.20)
box('Toilet cistern',-1.1,4.7,.62,.48,.30,.85,'white');o=ball('Toilet bowl',-1.1,4.2,.35,.35,'white');o.scale=(.85,1.2,.7)
box('Toilet basin',-2.25,4.6,.75,.5,.42,.15,'white');box('Accessible grab rail',-.45,4.3,.8,.05,.8,.05,'metal')
text('Toilet label','Accessible Toilet\n2.6 × 2.4 m',-1.4,2.5,.13,.21)
group('05 Office')
desk(6.5,3.8,2);chair(6.5,4.5,math.pi);chair(6,2.1);chair(7,2.1)
box('Office bookcase',8.3,3.8,.65,.35,1.7,1.3,'wood')
for i in range(9):box('Office book',8.1,3.15+i*.15,.92,.23,.085,.32,['blue','pink','yellow'][i%3])
text('Office label','Office\n4.0 × 3.6 m',6.5,2.9,.13,.24);plant(4.9,4.6)
group('06 Reception and queue')
box('Reception counter',2,-2,.55,4.4,.65,1.1,'wood');box('White counter face',2,-2.34,.57,4,.035,.68,'white')
for x in [-.15,4.15]:box('Counter return',x,-1.65,.55,.25,1.3,1.1,'wood')
for x in [1,3]:
    chair(x,-1.05,math.pi);box('Reception monitor',x,-2,1.28,.55,.08,.32,'ink')
for x in [0,1,2,3,4,5]:
    cyl('Queue post',x,-3.55,.42,.045,.78,'metal');cyl('Queue base',x,-3.55,.08,.13,.06,'ink')
    if x<5:box('Queue belt',x+.5,-3.55,.72,1,.035,.07,'blue')
text('Reception label','Reception · 2 staff',2,-.4,.13,.30)
text('Queue label','Queue · 10 people',2.5,-4.1,.13,.23)
box('Clinic identity board',6.65,-1.25,.15,3.25,1.9,.12,'white');text('Clinic identity','Community\nCare Clinic',6.65,-1.25,.23,.35,'blue')
chair(6.4,-4.1);chair(8,-4.1)
group('07 Waiting area')
for y in [-1.2,-2.2,-3.65,-4.65]:
    for x in [-6.5,-5.7,-4.9,-4.1,-3.3]:chair(x,y)
text('Waiting label','Waiting Area · 20 seats',-5,-2.9,.13,.25)
text('Hall label','Gallery / Hall · 2.4 m wide',-.6,.75,.13,.27)
for x,y in [(-8,-4.7),(-8,.5),(-2.5,-4.6),(-.8,-2.9),(7.9,-2.8),(3.7,.5),(-3,1)]:plant(x,y)
group('08 Parking - 20 spaces')
box('Car park',-16.05,0,-.025,13.5,14.3,.15,'road')
for lane_x,role,arrow in [(-17.85,'ENTRY','↑'),(-14.25,'EXIT','↓')]:
    box('Separate '+role+' driveway',lane_x,-7.80,.095,3.2,1.85,.06,'road',.015)
    for j in range(8):box(role+' zebra stripe',lane_x-1.38+j*.395,-7.90,.132,.22,.86,.012,'white',0)
    text(role+' arrow',arrow,lane_x,-6.64,.065,.85,'white')
    text(role+' road label',role,lane_x,-6.05,.065,.26,'white')
    for side in [-1,1]:box(role+' tactile landing',lane_x+side*1.82,-7.90,.10,.35,.95,.035,'yellow',.015)
box('Driveway separation island',-16.05,-7.55,.14,.40,1.9,.20,'paving',.09)
box('Median planting',-16.05,-7.15,.29,.30,.70,.18,'leaf',.07)
for x in [-19.53,-12.57]:box('Driveway outer curb',x,-7.81,.13,.12,1.38,.12,'paving',.035)
for side in [-1,1]:
    x=-16.05+side*5.0
    for i in range(10):
        y=-6.35+i*1.4
        box('Parking bay divider',x,y-.69,.058,3.1,.045,.014,'white',0)
        num=i+1 if side==-1 else i+11
        text('Bay '+str(num),str(num),x-side*1.12,y,.065,.22,'white')
        if num==1:
            box('Accessible parking surface',x,y,.061,3,1.32,.01,'blue',0);text('Accessible bay','ACCESS',x,y,.08,.24,'white');continue
        if num in [7,16]:continue
        carmat=['coral','yellow','blue','lavender','mint','white'][num%6]
        box('Car body',x,y,.43,2.65,1.12,.5,carmat,.18);box('Car cabin',x,y,.79,1.40,1.04,.42,'glass',.13)
        box('Car roof',x,y,.99,.8,.95,.09,carmat,.08)
        for dx in [-.87,.87]:
            for dy in [-.55,.55]:
                o=cyl('Car wheel',x+dx,y+dy,.27,.24,.12,'ink');o.rotation_euler.x=math.pi/2
for y in [-3,3]:
    text('Traffic entry arrow','↑',-17.85,y,.065,.85,'white')
    text('Traffic exit arrow','↓',-14.25,y,.065,.85,'white')
for y in [-4,-1,2,5]:box('Parking aisle divider',-16.05,y,.065,.06,1.1,.012,'white',0)
group('09 Landscaping')
for x in [-23.4,-9.3,10]:
    for y in [-6,-3,0,3,6]:
        cyl('Tree trunk',x,y,.8,.13,1.6,'wood')
        for k,(dx,dy,dz) in enumerate([(0,0,0),(.35,.1,.15),(-.25,.25,.05)]):ball('Tree canopy',x+dx,y+dy,1.8+dz,.7,'leaf_light' if k==1 else 'leaf')
for y in [-6.4,6.4]:
    for x in [-7,-5,-3,2,4,6,8]:
        box('Raised garden bed',x,y,.14,1.7,.65,.30,'paving')
        for j in range(9):ball('Garden foliage',x+random.uniform(-.7,.7),y+random.uniform(-.23,.23),.4,.18,'leaf')
        for j in range(7):ball('Flowers',x+random.uniform(-.7,.7),y+random.uniform(-.23,.23),.55,.09,['pink','yellow','lavender'][j%3])
for x,y in [(-1.8,-5.9),(.5,-5.9),(-9.3,-7),(-23.4,-7),(-9.3,7),(-23.4,7)]:
    cyl('Bollard',x,y,.38,.065,.75,'metal');ball('Bollard lamp',x,y,.76,.085,'white')
group('10 Presentation')
text('Entrance label','MAIN ENTRANCE',-.7,-6.2,.10,.23)
text('Site title','LITTLE LIFELINE  /  STARTER CLINIC',-3,-8,.10,.32)
bpy.ops.object.camera_add(location=(18,-34,40));cam=bpy.context.object;cam.name='Clinic overview';cam.rotation_euler=(Vector((-6.5,-1.5,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=44;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(0,-4,20));bpy.context.object.data.energy=2600;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=15
bpy.ops.object.light_add(type='SUN',location=(0,0,15));bpy.context.object.rotation_euler=(.4,-.5,-.3);bpy.context.object.data.energy=2
scene.world=bpy.data.worlds.new('Clinic sky');scene.world.color=(.55,.65,.75)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene['reference']='Community Care Clinic Site Plan.png'
scene['design_notes']='Roofless furnished reference model. Labelled room dimensions guide layout; site proportions and small fixtures are estimates. Walls shortened for cutaway visibility. 20 waiting seats and 20 parking bays.'
# Pack a copy of the reference for convenient review inside Blender.
ref=Path('/Users/anoopjose/Downloads/Community Care Clinic Site Plan.png')
if ref.exists():bpy.data.images.load(str(ref)).pack()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
if BUILDING_ONLY:
    for key in list(groups):
        if key.startswith(('03','04','05','06','07') if EXTERIOR else ('03','04','05','06','07','08','09')):
            for ob in list(groups[key].objects):bpy.data.objects.remove(ob,do_unlink=True)
            bpy.data.collections.remove(groups[key])
    if not EXTERIOR:
        for ob in list(groups['01 Site'].objects):
            if not ob.name.startswith(('Clinic apron','Clinic floor','Tile grout','Main entrance path')):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.objects.remove(bpy.data.objects.get('Site title'),do_unlink=True)
    group('11 Room labels')
    for label,x,y in [('Nursing Room 1',-7,3.3),('Cleaner / Utilities',-4,3.3),('Accessible Toilet',-1.4,3.3),('Nursing Room 2',2,3.3),('Office',6.5,3.3),('Gallery / Hall',0,.5),('Waiting Area',-5,-2.7),('Reception',2,-2.2)]:text('Room label',label,x,y,.13,.25,'white')
    if not EXTERIOR:
        cam.location=(15,-22,27);cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=23
    scene['design_notes']='Building-first blockout from clinic reference: floor, cutaway walls, partitions, door openings and blue window panels. Furniture and site assets deferred.'
if EXTERIOR:scene['design_notes']='Clinic building with 20-bay parking, cars, accessible bay, crossing, front street, sidewalk, entry path, garden beds, trees and bollards. Interior furnishing deferred. Site proportions estimated from reference.'
stem='little-lifeline-clinic-exterior' if EXTERIOR else ('little-lifeline-clinic-building' if BUILDING_ONLY else 'little-lifeline-clinic-site')
import importlib.util
people_spec=importlib.util.spec_from_file_location('clinic_patients',Path(__file__).with_name('create_patients.py'))
people_module=importlib.util.module_from_spec(people_spec);people_spec.loader.exec_module(people_module)
patient_rigs=people_module.build(OUT) if EXTERIOR else []
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(stem+'.blend')))
scene.render.filepath=str(OUT/(stem+'.png'));bpy.ops.render.render(write_still=True)
if patient_rigs:people_module.preview(OUT,patient_rigs)
print('CLINIC_COMPLETE',len(scene.objects))
