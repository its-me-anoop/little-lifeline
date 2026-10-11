"""Furnish the director's office while retaining the existing clinic scene."""
import math,sys
from pathlib import Path
import bpy
from mathutils import Vector
OUT=Path(sys.argv[sys.argv.index('--')+1]);folder=OUT/'furniture';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
scene=bpy.context.scene
old=bpy.data.collections.get('16 Boss office')
if old:
    for ob in list(old.objects):bpy.data.objects.remove(ob,do_unlink=True)
    bpy.data.collections.remove(old)
coll=bpy.data.collections.new('16 Boss office');scene.collection.children.link(coll)
mats={}
for name,c in {'Walnut':(.22,.105,.048),'Ivory':(.89,.86,.78),'Black':(.009,.009,.011),'Teal':(.035,.29,.30),'Gold':(.64,.43,.15),'Metal':(.32,.38,.41),'Blue':(.09,.23,.38),'Paper':(.97,.95,.87),'Leaf':(.12,.33,.12),'Clay':(.48,.23,.15),'Coral':(.68,.19,.12)}.items():
    m=bpy.data.materials.new('Office '+name);m.use_nodes=True;m.diffuse_color=(*c,1);node=m.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*c,1);node.inputs['Roughness'].default_value=.45
    if name in ['Gold','Metal']:node.inputs['Metallic'].default_value=.65
    mats[name]=m
root=None

def finish(ob,name,material,parent=None):
    ob.name=name
    for c in list(ob.users_collection):c.objects.unlink(ob)
    coll.objects.link(ob);ob.data.materials.append(mats[material]);ob.parent=parent or root;return ob

def box(name,xyz,size,mat,parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1);ob=finish(bpy.context.object,name,mat,parent);ob.location=xyz;ob.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=ob.modifiers.new('Soft edges','BEVEL');b.width=.025;b.segments=3;ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return ob

def cyl(name,xyz,r,h,mat,parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=h);ob=finish(bpy.context.object,name,mat,parent);ob.location=xyz;return ob

def sphere(name,xyz,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12);ob=finish(bpy.context.object,name,mat);ob.location=xyz;ob.scale=scale;return ob
root=bpy.data.objects.new('BossOffice_Furniture',None);coll.objects.link(root);root.location=(6.65,3.5,.12)
box('Walnut executive desktop',(0,0,.81),(2.25,.90,.09),'Walnut')
box('Desk modesty panel',(0,-.25,.41),(1.95,.07,.68),'Walnut')
box('Desk gold edge',(0,-.46,.80),(2.20,.018,.025),'Gold')
for x in [-.88,.88]:
    box('Ivory drawer pedestal',(x,.03,.37),(.40,.73,.74),'Ivory')
    for z in [.15,.36,.58]:
        box('Drawer face',(x,.405,z),(.35,.02,.18),'Ivory')
        box('Brass drawer pull',(x,.427,z+.035),(.14,.025,.015),'Gold')
box('Desk leather blotter',(.0,.14,.871),(.82,.49,.017),'Black')
box('Monitor stand',(0,-.19,.89),(.29,.19,.024),'Black')
box('Monitor stem',(0,-.20,1.04),(.04,.04,.29),'Metal')
box('Monitor housing',(0,-.22,1.19),(.61,.05,.37),'Black')
box('Monitor display',(0,-.186,1.19),(.56,.012,.32),'Blue')
box('Keyboard',(0,.24,.892),(.44,.15,.024),'Black')
box('Mouse',(.34,.25,.90),(.06,.09,.035),'Black')
box('Director nameplate base',(.76,-.24,.89),(.43,.16,.06),'Black')
box('Director nameplate',(.76,-.322,.94),(.41,.025,.095),'Gold')
bpy.ops.object.text_add();label=finish(bpy.context.object,'Director desk nameplate','Black');label.location=(.76,-.339,.91);label.rotation_euler=(math.pi/2,0,0);label.data.body='DIRECTOR';label.data.align_x='CENTER';label.data.size=.058;label.data.extrude=.0007
box('Document folder',(-.77,.10,.882),(.33,.40,.027),'Teal')
box('Documents',(-.77,.10,.90),(.29,.35,.013),'Paper')
cyl('Pen pot',(.83,.25,.96),.045,.16,'Gold')
for dx in [-.02,0,.02]:cyl('Pen',(.83+dx,.25,1.08),.006,.18,'Black')
# Executive chair faces the front of the office; visitor chairs face the desk.
def chair(name,x,y,angle,executive=False):
    seatroot=bpy.data.objects.new(name,None);coll.objects.link(seatroot);seatroot.parent=root;seatroot.location=(x,y,0);seatroot.rotation_euler.z=angle
    color='Black' if executive else 'Teal'
    box(name+' seat',(0,0,.48),(.65,.64,.14),color,seatroot)
    box(name+' back',(0,.27,.90 if executive else .80),(.65,.12,.79 if executive else .54),color,seatroot)
    if executive:
        cyl('Executive chair gas lift',(0,0,.25),.04,.44,'Metal',seatroot)
        for j in range(5):
            a=j*math.tau/5;foot=box('Executive star base',(.13*math.cos(a),.13*math.sin(a),.075),(.45,.035,.035),'Metal',seatroot);foot.rotation_euler.z=a
            cyl('Executive caster',(.30*math.cos(a),.30*math.sin(a),.045),.045,.065,'Black',seatroot)
        box('Chair headrest',(0,.25,1.31),(.40,.13,.15),'Black',seatroot)
    else:
        for dx in [-.27,.27]:
            for dy in [-.24,.24]:box('Visitor chair leg',(dx,dy,.23),(.045,.045,.46),'Metal',seatroot)
    for side in [-1,1]:
        box('Chair armrest',(side*.35,0,.68),(.065,.62,.055),'Walnut',seatroot)
        box('Chair arm support',(side*.35,.22,.59),(.035,.035,.22),'Metal',seatroot)
chair('Executive chair',0,1.10,0,True)
chair('Visitor chair one',-.63,-1.03,math.pi)
chair('Visitor chair two',.50,-1.03,math.pi)
# Storage follows the room side walls, leaving the entrance clear.
box('Storage credenza',(-1.97,.78,.40),(.45,1.35,.80),'Walnut')
box('Credenza ivory top',(-1.97,.78,.83),(.49,1.40,.05),'Ivory')
for y in [.37,.78,1.19]:box('Credenza drawer handle',(-1.727,y,.63),(.023,.19,.018),'Gold')
box('Bookcase back',(1.76,.78,.78),(.09,1.25,1.56),'Walnut')
for y in [.15,1.41]:box('Bookcase side',(1.63,y,.78),(.38,.07,1.56),'Walnut')
for z in [.05,.52,1.02,1.53]:box('Bookcase shelf',(1.63,.78,z),(.39,1.30,.055),'Walnut')
for level in [.57,1.07]:
    for j in range(8):box('Office book',(1.61,.30+j*.105,level+.15),(.27,.070,.29),['Blue','Teal','Coral','Ivory'][j%4])
# Framed qualification on the back wall.
box('Certificate gold frame',(-.5,1.682,1.41),(.77,.035,.47),'Gold')
box('Certificate paper',(-.5,1.659,1.41),(.68,.012,.39),'Paper')
for z,w in [(1.50,.41),(1.42,.50),(1.37,.43)]:box('Certificate text line',(-.5,1.650,z),(w,.006,.006),'Black')
cyl('Certificate seal',(-.5,1.649,1.30),.035,.006,'Gold').rotation_euler.x=math.pi/2
cyl('Plant pot',(-1.88,1.17,.97),.13,.28,'Clay')
for j in range(5):
    a=j*math.tau/5;sphere('Office plant leaf',(-1.88+.10*math.cos(a),1.17+.10*math.sin(a),1.26),(.11,.09,.24),'Leaf')
# Place the original black-suited boss beside the executive desk.
with bpy.data.libraries.load(str(OUT/'characters/boss/little-lifeline-boss.blend'),link=False) as (src,dst):dst.objects=[n for n in src.objects if n.startswith('Boss_')]
for ob in dst.objects:
    if ob:coll.objects.link(ob)
rig=next(o for o in dst.objects if o and o.type=='ARMATURE');rig.location=(7.65,4.38,.12);rig.rotation_euler.z=0
rig.animation_data.action=bpy.data.actions.get(rig.name.removesuffix('Rig')+'_Idle')
for ob in list(scene.objects):
    if ob.type=='FONT' and ob.data.body=='Office':bpy.data.objects.remove(ob,do_unlink=True)
scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
bpy.data.libraries.write(str(folder/'little-lifeline-boss-office.blend'),{coll},fake_user=True)
def descendants(ob):
    result=[]
    for child in ob.children:result.append(child);result.extend(descendants(child))
    return result
bpy.ops.object.select_all(action='DESELECT')
for ob in [root]+descendants(root):ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(folder/'BossOffice_Furniture.fbx'),use_selection=True,object_types={'EMPTY','MESH','OTHER'},axis_forward='-Z',axis_up='Y',bake_anim=False)
cam=scene.camera;cam.location=(11,-3,10);cam.rotation_euler=(Vector((6.65,3.35,.7))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=6.9
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.filepath=str(folder/'little-lifeline-boss-office.png');bpy.ops.render.render(write_still=True)
print('BOSS_OFFICE_COMPLETE')
