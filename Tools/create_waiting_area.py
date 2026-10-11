"""Install ten original waiting-room chairs into the current clinic scene."""
import math,sys
from pathlib import Path
import bpy
from mathutils import Vector
OUT=Path(sys.argv[sys.argv.index('--')+1]);folder=OUT/'furniture';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
scene=bpy.context.scene
old=bpy.data.collections.get('15 Waiting area - ten chairs')
if old:
    for ob in list(old.objects):bpy.data.objects.remove(ob,do_unlink=True)
    bpy.data.collections.remove(old)
collection=bpy.data.collections.new('15 Waiting area - ten chairs');scene.collection.children.link(collection)
mats={}
for name,c in {'Teal':(.045,.36,.37),'Blue':(.12,.27,.45),'Oak':(.43,.24,.105),'Metal':(.27,.34,.39)}.items():
    mat=bpy.data.materials.new('Waiting '+name);mat.diffuse_color=(*c,1);mat.use_nodes=True
    node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*c,1);node.inputs['Roughness'].default_value=.65 if name in ['Teal','Blue'] else .4
    if name=='Metal':node.inputs['Metallic'].default_value=.65
    mats[name]=mat

def box(name,xyz,size,material,parent):
    bpy.ops.mesh.primitive_cube_add(size=1);ob=bpy.context.object;ob.name=name;ob.location=xyz;ob.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for coll in list(ob.users_collection):coll.objects.unlink(ob)
    collection.objects.link(ob);ob.parent=parent;ob.data.materials.append(mats[material])
    b=ob.modifiers.new('Rounded edges','BEVEL');b.width=.035;b.segments=4;ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return ob
chairs=[]
for row,y in enumerate([.1,-2.2]):
    for i in range(5):
        root=bpy.data.objects.new(f'WaitingChair_{row*5+i+1:02d}',None);collection.objects.link(root);root.location=(-7.65+i*.96,y,.12);root.rotation_euler.z=math.pi;root['seat_height_metres']=.47;root['width_metres']=.76;chairs.append(root)
        color='Teal' if row==0 else 'Blue'
        box('Seat frame',(0,0,.40),(.65,.65,.07),'Oak',root)
        box('Upholstered seat',(0,-.015,.475),(.64,.64,.13),color,root)
        back=box('Upholstered backrest',(0,.285,.81),(.65,.13,.57),color,root);back.rotation_euler.x=math.radians(-7)
        for side in [-1,1]:
            box('Armrest',(side*.355,0,.675),(.065,.66,.07),'Oak',root)
            for yleg in [-.24,.25]:
                box('Chair leg',(side*.28,yleg,.225),(.045,.045,.45),'Metal',root)
            box('Armrest upright',(side*.35,.22,.55),(.045,.045,.27),'Metal',root)
# Remove the previous floor label under the new chairs.
for ob in list(scene.objects):
    if ob.type=='FONT' and ob.data.body=='Waiting Area':bpy.data.objects.remove(ob,do_unlink=True)
scene['waiting_seat_count']=10
scene['waiting_area_layout']='Two rows of five chairs, with 1.6m clear aisle between rows and an open approach from the main entrance.'
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'little-lifeline-clinic-exterior.blend'))
bpy.data.libraries.write(str(folder/'little-lifeline-waiting-area.blend'),{collection},fake_user=True)
# A reusable chair FBX centred at ground level.
chair=chairs[0];original_location=chair.location.copy();chair.location=(0,0,0);bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
for ob in [chair]+list(chair.children):ob.select_set(True)
bpy.context.view_layer.objects.active=chair
bpy.ops.export_scene.fbx(filepath=str(folder/'WaitingChair.fbx'),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
chair.location=original_location;bpy.context.view_layer.update()
cam=scene.camera;cam.location=(-9,-10,10);cam.rotation_euler=(Vector((-5.65,-.9,.45))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=8.8
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(folder/'little-lifeline-waiting-area.png');bpy.ops.render.render(write_still=True)
print('WAITING_AREA_COMPLETE: 10 chairs')
