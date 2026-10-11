import bpy,math,re
from pathlib import Path
from mathutils import Vector
OUT=Path('/Users/anoopjose/Projects/little-lifeline/assets/clinic-reference/game-library/previews')
for path in sorted(OUT.glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene
    if path.stem=='Themes':
        for ob in scene.objects:
            if ob.name.startswith(('FloorPattern','WallFinish')):
                ob.hide_render=True
                for child in ob.children:child.hide_render=True
        scene.camera.data.ortho_scale=17.5;scene.camera.location=(0,-24,23);scene.camera.rotation_euler=(Vector((0,-5.9,.6))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.resolution_x=2100;scene.render.resolution_y=1800
    else:
        roots=[ob for ob in scene.objects if ob.type=='EMPTY' and '_V' in ob.name]
        count=len(roots)//(2 if path.stem.endswith('equipment') else 1);rows=math.ceil(count/5);cy=-(rows-1)*3.6/2
        if not scene.get('preview_layout_version'):
            for ob in scene.objects:
                if ob.type=='FONT':
                    ob.location.x=-ob.location.x;ob.location.y=2*cy-ob.location.y;ob.rotation_euler.z=math.pi;ob.data.size=.20;ob.data.body=re.sub(r'(?<=[a-z])(?=[A-Z])',' ',ob.data.body)
            for root in roots:
                root.location.x=-root.location.x;root.location.y=2*cy-root.location.y
                if path.stem.endswith('equipment'):root.location.x+=.245 if root.name.endswith('_V1') else -.245
                bpy.context.view_layer.update();coords=[ob.matrix_world@Vector(p) for ob in root.children if ob.type=='MESH' for p in ob.bound_box]
                extent=max(max(p[j] for p in coords)-min(p[j] for p in coords) for j in range(3));factor=(1.2 if path.stem.endswith('equipment') else 1.45)/max(extent,.01);root.scale*=factor
            scene['preview_layout_version']=2
        camera=scene.camera;camera.location=(0,cy+18,17);camera.rotation_euler=(Vector((0,cy,.50))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=21.3
        scene.render.resolution_x=2200;scene.render.resolution_y=1500 if rows<=4 else 1800
    scene.display.shading.light='FLAT';scene.display.shading.show_shadows=False;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.display.shading.background_type='WORLD';scene.world.color=(.91,.92,.94)
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    bpy.ops.wm.save_as_mainfile(filepath=str(path));scene.render.filepath=str(path.with_suffix('.png'));bpy.ops.render.render(write_still=True)
    print('POLISHED',path.stem,flush=True)
print('PREVIEWS_COMPLETE')
