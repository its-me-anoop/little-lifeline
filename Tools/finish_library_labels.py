import bpy,re,math,json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
OUT=Path('/Users/anoopjose/Projects/little-lifeline/assets/clinic-reference/game-library/previews')
for path in sorted(OUT.glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene;cam=scene.camera;fonts=[o for o in scene.objects if o.type=='FONT']
    if path.stem=='Themes':
        roots=sorted([o for o in scene.objects if o.type=='EMPTY' and re.fullmatch(r'Theme\d+',o.name)],key=lambda o:o.name)
        for i,root in enumerate(roots):root.location.y=-(i//5)*4.5
        cam.data.ortho_scale=20.5;cam.rotation_euler=(Vector((0,-6.45,.6))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.resolution_x=2200;scene.render.resolution_y=1800
        groups=[[r] for r in roots]
    else:
        roots=[o for o in scene.objects if o.type=='EMPTY' and '_V' in o.name];groups=[]
        for i in range(len(fonts)):
            groups.append([r for r in roots if re.search(rf'_{i+1:02d}_V\d+$',r.name)])
    dark=bpy.data.materials.new('Preview caption ink');dark.diffuse_color=(.025,.035,.045,1)
    bpy.context.view_layer.update()
    def walk(root):
        children=list(root.children)
        return children+[sub for child in children for sub in walk(child)]
    # Pair each caption with its original item index (Text, Text.001 etc.).
    fonts.sort(key=lambda o:0 if o.name=='Text' else int(o.name.rsplit('.',1)[1]))
    for font,group in zip(fonts,groups):
        if not group:continue
        points=[world_to_camera_view(scene,cam,ob.matrix_world@Vector(p)) for r in group for ob in walk(r) if ob.type=='MESH' for p in ob.bound_box]
        u=(min(p.x for p in points)+max(p.x for p in points))/2;v=min(p.y for p in points)-.025
        font.parent=cam;font.matrix_parent_inverse.identity();font.location=((u-.5)*cam.data.ortho_scale,(v-.5)*cam.data.ortho_scale*scene.render.resolution_y/scene.render.resolution_x,-2);font.rotation_euler=(0,0,0);font.data.size=.17;font.data.align_x='CENTER';font.data.materials.clear();font.data.materials.append(dark)
    bpy.ops.wm.save_as_mainfile(filepath=str(path));scene.render.filepath=str(path.with_suffix('.png'));bpy.ops.render.render(write_still=True);print('LABELLED',path.stem,flush=True)
print('LABELS_COMPLETE')
