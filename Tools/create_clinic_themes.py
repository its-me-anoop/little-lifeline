"""Author modular finishes and all nineteen theme material samples."""
import bpy,sys,math,json,random
from pathlib import Path
from mathutils import Vector
ROOT=Path('/Users/anoopjose/Projects/little-lifeline');OUT=ROOT/'assets/clinic-reference/game-library';catalog=json.loads((OUT/'catalog.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
folder=OUT/'themes';folder.mkdir(exist_ok=True);collection=None;root=None;materials={}
def palette(theme):
    result={}
    for role,hexcolor in theme['colors'].items():
        rgb=tuple(int(hexcolor[i:i+2],16)/255 for i in [0,2,4]);mat=bpy.data.materials.new(f"{theme['name']}::{role}");mat.diffuse_color=(*rgb,1);mat.use_nodes=True
        node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*rgb,1);node.inputs['Roughness'].default_value=.62
        if role=='Metal':node.inputs['Metallic'].default_value=.65;node.inputs['Roughness'].default_value=.3
        mat.asset_mark();result[role]=mat
    return result

def box(name,xyz,size,role,rotation=0):
    bpy.ops.mesh.primitive_cube_add(size=1);ob=bpy.context.object;ob.name=name;ob.parent=root;ob.location=xyz;ob.scale=size;ob.rotation_euler.z=rotation
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(ob.users_collection):c.objects.unlink(ob)
    collection.objects.link(ob);ob.data.materials.append(materials[role]);b=ob.modifiers.new('Soft edge','BEVEL');b.width=.006;b.segments=2;return ob

def floor(pattern):
    box('Floor module',(0,0,.02),(2,2,.04),'Floor')
    if pattern==0:
        for at in [-.5,0,.5]:box('Tile grout',(at,0,.043),(.012,2,.005),'Pattern');box('Tile grout',(0,at,.043),(2,.012,.005),'Pattern')
    elif pattern==1:
        for x in range(4):
            for y in range(4):
                if (x+y)%2==0:box('Checker tile',(-.75+x*.5,-.75+y*.5,.044),(.49,.49,.007),'Pattern')
    elif pattern==2:
        for y in range(8):
            box('Plank seam',(0,-.875+y*.25,.045),(2,.01,.007),'Pattern')
            for x in [-.65,.25]:box('Staggered plank end',(x+(y%2)*.25,-.75+y*.25,.046),(.01,.24,.007),'Pattern')
    elif pattern==3:
        for slope in [-1,1]:
            for c in [-1.5,-1,-.5,0,.5,1,1.5]:
                pts=[]
                for x in [-1,1]:
                    y=slope*x+c
                    if -1<=y<=1:pts.append((x,y))
                for y in [-1,1]:
                    x=(y-c)/slope
                    if -1<=x<=1:pts.append((x,y))
                if len(pts)>=2:
                    a,b=pts[0],max(pts,key=lambda p:(p[0]-pts[0][0])**2+(p[1]-pts[0][1])**2);length=math.hypot(b[0]-a[0],b[1]-a[1])
                    if length>.02:box('Diagonal lattice',((a[0]+b[0])/2,(a[1]+b[1])/2,.046),(length,.012,.005),'Pattern',math.atan2(b[1]-a[1],b[0]-a[0]))
    elif pattern==4:
        for side in [-1,1]:box('Floor border',(side*.82,0,.045),(.12,1.76,.005),'Pattern');box('Floor border',(0,side*.82,.045),(1.76,.12,.005),'Pattern')
        box('Centre diamond',(0,0,.046),(.50,.50,.005),'Trim',math.pi/4)
    elif pattern==5:
        rng=random.Random(12)
        for i in range(50):box('Terrazzo chip',(rng.uniform(-.95,.95),rng.uniform(-.95,.95),.046),(rng.uniform(.035,.08),rng.uniform(.025,.07),.004),'Pattern' if i%3 else 'Trim',rng.random()*math.pi)
    else:
        for y in [-.75,-.15,.45]:box('Wide band',(0,y,.045),(2,.3,.005),'Pattern')

def wall(finish):
    box('Wall module',(0,1.02,.995),(2,.08,1.9),'Wall');box('Skirting',(0,.966,.095),(2,.03,.10),'Trim')
    if finish in [1,3]:
        box('Lower wall finish',(0,.969,.43),(2,.025,.77),'Wainscot');box('Dado rail',(0,.942,.84),(2,.025,.04),'Trim')
    if finish==2:
        for x in [-.9+i*.25 for i in range(8)]:box('Wallpaper stripe',(x,.969,1.05),(.085,.02,1.70),'Wainscot')
    if finish==3:
        for z in [.24,.44,.64]:box('Wall tile grout',(0,.950,z),(2,.006,.008),'Pattern')
        for x in [-.8+i*.2 for i in range(9)]:box('Wall tile grout',(x,.949,.43),(.008,.006,.77),'Pattern')
    if finish==4:
        for x in [-.65,0,.65]:
            box('Panel inset',(x,.968,.96),(.46,.02,1.24),'Wainscot')
            for dx in [-.24,.24]:box('Panel upright',(x+dx,.947,.96),(.025,.026,1.3),'Trim')
            for z in [.32,1.60]:box('Panel rail',(x,.947,z),(.50,.026,.025),'Trim')
    box('Crown trim',(0,.97,1.90),(2,.04,.075),'Trim')

def new_asset(name):
    global collection,root
    collection=bpy.data.collections.new(name);scene.collection.children.link(collection);root=bpy.data.objects.new(name,None);collection.objects.link(root);root.asset_mark()

def export(name):
    bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
    for ob in root.children:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
    bpy.data.libraries.write(str(folder/(name+'.blend')),{collection},fake_user=True)

def label(text,xyz):
    bpy.ops.object.text_add(location=xyz);ob=bpy.context.object;ob.data.body=text;ob.data.size=.15;ob.data.align_x='CENTER';ob.data.materials.append(materials['AccentDark'])

# Every palette has a miniature room sample; materials retain all 13 named theme roles.
for i,theme in enumerate(catalog['themes']):
    materials=palette(theme);name=f"Theme{theme['id']:02d}";new_asset(name);root['theme_name']=theme['name'];root['floor_pattern']=i%7;root['wall_finish']=i%5
    floor(i%7);wall(i%5)
    # Mini desk and upholstered chair make the fabric, wood and metal roles visible.
    box('Theme sample worktop',(.18,.33,.77),(1.0,.43,.06),'Wood')
    for x in [-.22,.58]:box('Table legs',(x,.33,.4),(.05,.32,.74),'Metal')
    box('Theme chair seat',(-.30,-.34,.48),(.52,.51,.10),'Accent')
    box('Theme chair back',(-.30,-.11,.75),(.52,.065,.48),'Accent')
    for x in [-.50,-.10]:
        for y in [-.53,-.15]:box('Chair leg',(x,y,.24),(.04,.04,.44),'Metal')
    box('Art frame',(.25,.953,1.39),(.56,.035,.40),'Metal');box('Art paper',(.25,.927,1.39),(.49,.012,.33),'Fabric');box('Art motif',(.25,.917,1.39),(.20,.006,.20),'Warm',.4)
    export(name);root.location=((i%5-2)*3.1,-(i//5)*3.7,0);label(theme['name'],(root.location.x,root.location.y-1.23,.04))
# Modular finishes are independently selectable and need only material reassignment for each theme.
materials=palette(catalog['themes'][0]);finishes=[]
for i,title in enumerate(catalog['floor_patterns']):
    new_asset(f'FloorPattern{i:02d}');floor(i);export(root.name);root.location=((i%4-1.5)*2.5,-17-(i//4)*3,0);finishes.append(root.name)
for i,title in enumerate(catalog['wall_finishes']):
    new_asset(f'WallFinish{i:02d}');wall(i);export(root.name);root.location=((i%5-2)*2.5,-24,0);finishes.append(root.name)
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.background_type='WORLD';scene.world=bpy.data.worlds.new('Theme gallery');scene.world.color=(.86,.87,.88)
bpy.ops.object.camera_add(location=(0,-24,27));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-5.1,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=20;scene.camera=camera
scene.render.resolution_x=2000;scene.render.resolution_y=1900;scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'previews/Themes.blend'))
scene.render.filepath=str(OUT/'previews/Themes.png');bpy.ops.render.render(write_still=True)
# Separate finish contact image shows all seven floors and five walls.
camera.location=(0,-34,22);camera.rotation_euler=(Vector((0,-20.8,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=16
scene.render.resolution_y=1200;scene.render.filepath=str(OUT/'previews/Finishes.png');bpy.ops.render.render(write_still=True)
(OUT/'themes/palettes.json').write_text(json.dumps({'themes':catalog['themes'],'floor_patterns':catalog['floor_patterns'],'wall_finishes':catalog['wall_finishes']},indent=2))
print('THEMES_COMPLETE: 19 palettes, 7 floor patterns, 5 wall finishes',flush=True)
