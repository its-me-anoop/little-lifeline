"""Six patient variants using Little Lifeline's existing compatible skeleton."""
import ast, math
from pathlib import Path
import bpy
from mathutils import Vector, Matrix

def build(out,receptionists=False):
    source=Path('/Users/anoopjose/Projects/little-lifeline/Tools/create_clinic_assets.py').read_text()
    tree=ast.parse(source)
    names={'u','material','box','orb','tube','taper','action_points','human'}
    selected='\n\n'.join(ast.get_source_segment(source,n) for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names)
    selected=selected.replace('patient=name=="Patient"','patient=True')
    selected=selected.replace('nurse,receptionist,patient=name=="Nurse",name=="Receptionist",name=="Patient"','nurse,receptionist,patient=False,False,True')
    selected=selected.replace('    skin(taper("Neck",', '''    if VARIANT in (1,3,5):
        skin(orb("Long hair",(0,1.365,-.088),(.115,.145,.065),"Ink",16),"head")
    if VARIANT==3:
        skin(orb("Hair bun",(0,1.505,-.085),(.065,.065,.065),"Ink",14),"head")
    if VARIANT in (4,5):
        for eye_x in (-.037,.037):
            skin(box("Glasses frame",(eye_x,1.419,.112),(.049,.035,.008),"Gold",.006),"head")
    skin(taper("Neck",''')
    selected='\n'.join(line.replace('"Ink"','"Hair"') if 'hair' in line.lower() else line for line in selected.splitlines())
    selected=selected.replace('        sleeve="Skin" if nurse else uniform','        skin(orb("Shoulder joint",rest["shoulder."+side],(.064,.064,.064),uniform,14),"upper_arm."+side)\n        sleeve="Skin" if nurse else uniform')
    start=selected.index('    # Adult proportions')
    end=selected.index('    bpy.ops.object.select_all(action="DESELECT")',start)
    selected=selected[:start]+'''    trousers="Denim"
    # Soft adult proportions with a slightly larger head and clear facial features.
    skin(orb("Face",(0,1.405,.018),(.145,.172,.130),"Skin",32),"head")
    skin(orb("Chin",(0,1.305,.045),(.104,.070,.091),"Skin",24),"head")
    for side in (-1,1):
        skin(orb("Ear",(side*.141,1.401,.014),(.023,.039,.025),"Skin",16),"head")
        skin(orb("Cheek",(side*.086,1.370,.122),(.038,.032,.012),"Blush",20),"head")
        ex=side*.049
        skin(orb("Eye white",(ex,1.435,.136),(.025,.027,.009),"Linen",20),"head")
        skin(orb("Iris",(ex,1.434,.146),(.015,.020,.005),"Eye",20),"head")
        skin(orb("Pupil",(ex,1.434,.150),(.008,.012,.003),"Ink",16),"head")
        skin(orb("Eye sparkle",(ex-.005,1.444,.154),(.005,.006,.002),"Linen",12),"head")
        skin(tube("Soft eyebrow",(ex-.022,1.478,.133),(ex+.020,1.483,.133),.006,"Hair",12),"head")
    skin(orb("Nose",(0,1.392,.150),(.022,.025,.021),"Skin",20),"head")
    skin(tube("Smile left",(-.031,1.342,.136),(0,1.334,.144),.0045,"Lip",12),"head")
    skin(tube("Smile right",(0,1.334,.144),(.031,1.342,.136),.0045,"Lip",12),"head")
    skin(orb("Hair cap",(0,1.523,-.011),(.153,.093,.140),"Hair",28),"head")
    for i in range(4):
        skin(orb("Swept fringe",(-.092+i*.053,1.514-i*.009,.094),(.046,.056,.053),"Hair",20),"head")
    if VARIANT in (1,3,5):
        skin(orb("Soft bob",(0,1.403,-.072),(.156,.167,.099),"Hair",28),"head")
        for side in (-1,1):skin(orb("Face framing hair",(side*.131,1.40,.033),(.029,.119,.051),"Hair",20),"head")
    if VARIANT==3:skin(orb("Hair bun",(0,1.535,-.121),(.074,.074,.065),"Hair",20),"head")
    if VARIANT in (4,5):
        for side in (-1,1):
            ex=side*.049
            # Round open spectacle rims keep the eyes visible.
            for j in range(16):
                a=j*math.tau/16;b=(j+1)*math.tau/16
                skin(tube("Spectacle rim",(ex+.033*math.cos(a),1.435+.036*math.sin(a),.157),(ex+.033*math.cos(b),1.435+.036*math.sin(b),.157),.003,"Gold",8),"head")
        skin(tube("Glasses bridge",(-.016,1.443,.157),(.016,1.443,.157),.003,"Gold",8),"head")
    skin(taper("Neck",(0,1.205,0),(0,1.31,.012),.058,.050,"Skin",20),"neck")
    skin(orb("Sweater chest",(0,1.077,.012),(.207,.174,.128),uniform,28),"chest")
    skin(orb("Soft sweater waist",(0,.916,.014),(.177,.130,.126),uniform,28),"spine")
    skin(taper("Sweater hem",(0,.844,0),(0,.880,0),.180,.180,uniform,24,.72),"spine")
    skin(taper("Ribbed collar",(0,1.223,.008),(0,1.244,.008),.071,.067,"Linen",20),"chest")
    if VARIANT in (0,1,3):
        skin(orb("Hood",(0,1.215,-.100),(.150,.075,.077),uniform,24),"chest")
        for side in (-1,1):skin(tube("Hood drawstring",(side*.056,1.195,.127),(side*.061,1.104,.140),.004,"Linen",10),"chest")
        skin(box("Kangaroo pocket",(0,.937,.136),(.215,.092,.015),uniform,.027),"spine")
    else:
        for z in [1.15,1.06,.97]:skin(orb("Cardigan button",(0,z,.145),(.010,.010,.005),"Wood",12),"chest" if z>1 else "spine")
        skin(box("Cardigan seam",(0,1.049,.142),(.012,.250,.009),"Linen",.004),"chest")
    skin(orb("Trouser hips",(0,.765,0),(.175,.109,.115),trousers,28),"pelvis")
    for side in ("L","R"):
        skin(orb("Soft shoulder sleeve",rest["shoulder."+side],(.079,.086,.077),uniform,24),"upper_arm."+side)
        skin(taper("Upper sleeve",rest["shoulder."+side],rest["elbow."+side],.068,.057,uniform,20),"upper_arm."+side)
        skin(orb("Elbow sleeve",rest["elbow."+side],(.058,.061,.059),uniform,20),"forearm."+side)
        skin(taper("Lower sleeve",rest["elbow."+side],rest["hand."+side],.056,.044,uniform,20),"forearm."+side)
        hand=rest["hand."+side]
        skin(orb("Soft hand",(hand[0],hand[1]-.034,hand[2]),(.037,.058,.030),"Skin",20),"hand."+side)
        skin(orb("Thumb",(hand[0]+(.025 if side=="L" else -.025),hand[1]-.016,hand[2]+.016),(.016,.027,.018),"Skin",16),"hand."+side)
        skin(taper("Trouser thigh",rest["hip."+side],rest["knee."+side],.079,.064,trousers,24),"upper_leg."+side)
        skin(orb("Trouser knee",rest["knee."+side],(.064,.067,.065),trousers,20),"lower_leg."+side)
        skin(taper("Trouser lower leg",rest["knee."+side],rest["ankle."+side],.065,.048,trousers,24),"lower_leg."+side)
        ankle=rest["ankle."+side]
        skin(orb("Sneaker",(ankle[0],.060,.073),(.065,.055,.135),"Sneaker",24),"foot."+side)
        skin(box("Sneaker sole",(ankle[0],.018,.073),(.130,.028,.255),"Linen",.025),"foot."+side)
        for j in range(3):skin(box("Sneaker lace",(ankle[0],.109,.055+j*.023),(.070,.005,.008),"Linen",.003),"foot."+side)
''' + selected[end:]
    if receptionists:
        clothing_start=selected.index('    skin(orb("Sweater chest"')
        clothing_end=selected.index('    skin(orb("Trouser hips"',clothing_start)
        selected=selected[:clothing_start]+'''    skin(orb("Tailored blazer chest",(0,1.077,.008),(.195,.178,.112),uniform,28),"chest")
    skin(orb("Fitted blazer waist",(0,.918,.008),(.165,.131,.108),uniform,28),"spine")
    skin(box("White shirt front",(0,1.098,.128),(.122,.272,.012),"Linen",.025),"chest")
    for side in (-1,1):
        lapel=box("Notched blazer lapel",(side*.070,1.133,.145),(.053,.229,.014),uniform,.013)
        lapel.rotation_euler.y=side*.32
        skin(lapel,"chest")
        collar=box("Shirt collar",(side*.037,1.213,.140),(.045,.051,.014),"Linen",.009)
        collar.rotation_euler.y=side*.38;skin(collar,"chest")
        skin(box("Blazer welt pocket",(side*.108,.941,.117),(.068,.014,.010),"Denim",.006),"spine")
    for height in [.992,.932]:skin(orb("Blazer button",(0,height,.124),(.008,.009,.005),"Gold",12),"spine")
    skin(box("Receptionist name badge",(-.102,1.113,.130),(.067,.040,.012),"Linen",.006),"chest")
    skin(box("Badge blue accent",(-.102,1.120,.138),(.048,.008,.003),"Blue",.002),"chest")
    if VARIANT==0:
        skin(orb("Tie knot",(0,1.194,.151),(.016,.017,.009),"Tie",16),"chest")
        skin(taper("Tie",(0,1.178,.153),(0,1.048,.153),.014,.024,"Tie",16,.36),"chest")
    else:
        skin(taper("Silk neck scarf",(0,1.226,.01),(0,1.252,.01),.064,.060,"Tie",20),"chest")
        skin(box("Scarf end",(.052,1.167,.150),(.037,.087,.011),"Tie",.014),"chest")
''' +selected[clothing_end:]
        selected=selected.replace('"Sneaker sole"','"Dress shoe sole"').replace('"Sneaker lace"','"Dress shoe stitch"').replace('"Sneaker"','"Dress shoe"')
        selected=selected.replace('(.130,.028,.255),"Linen"','(.130,.028,.255),"Ink"').replace('(.070,.005,.008),"Linen"','(.070,.005,.008),"Ink"')
    namespace={'bpy':bpy,'math':math,'Vector':Vector,'Matrix':Matrix,'MATS':{}}
    exec(compile(selected,'existing_clinic_character_functions','exec'),namespace)
    palette={'Ivory':(.91,.89,.79),'Linen':(.96,.94,.86),'Sage':(.4,.57,.45),'SageDark':(.22,.36,.28),'Apricot':(.84,.49,.31),'Gold':(.65,.44,.13),'Ink':(.06,.055,.04),'Blue':(.14,.46,.8),'Skin':(.78,.56,.39),'Wood':(.59,.4,.27),'Clay':(.5,.2,.16),'Leaf':(.31,.49,.3),'Rose':(.68,.37,.38)}
    variants=[('Patient_01_Man_Coral',(.68,.39,.22),(.98,.24,.10),(.06,.04,.03)),('Patient_02_Woman_Lavender',(.40,.20,.105),(.54,.27,.86),(.025,.022,.02)),('Patient_03_Man_Mint',(.89,.64,.43),(.12,.64,.43),(.24,.13,.065)),('Patient_04_Woman_Sunflower',(.64,.40,.24),(.98,.62,.07),(.07,.035,.02)),('Patient_05_OlderMan_Blue',(.91,.70,.55),(.12,.37,.72),(.55,.57,.58)),('Patient_06_OlderWoman_Rose',(.49,.28,.16),(.85,.20,.42),(.58,.59,.56))]
    poses=[(-18,-4.4,.3),(-14,-4.8,-.3),(-8,-7.9,-math.pi/2),(-3,-7.9,math.pi/2),(-.7,-6.5,math.pi),(1,-3.8,math.pi)]
    if receptionists:
        variants=[('Receptionist_01_Man_NavySuit',(.72,.46,.29),(.045,.08,.15),(.075,.045,.03)),('Receptionist_02_Woman_CharcoalSuit',(.43,.23,.12),(.11,.095,.135),(.027,.019,.018))]
        poses=[(1,-1.1,0),(3,-1.1,0)]
    folder=out/'characters';folder.mkdir(exist_ok=True)
    if receptionists:folder=folder/'receptionists';folder.mkdir(exist_ok=True)
    collection=bpy.data.collections.new('Receptionists - two suited characters' if receptionists else '12 Patients - six rigged variants');bpy.context.scene.collection.children.link(collection)
    rigs=[]
    for index,(name,skin,shirt,hair) in enumerate(variants):
        namespace['VARIANT']=index
        extra={'Denim':[(.08,.13,.20),(.13,.105,.17),(.18,.20,.17),(.13,.18,.25),(.13,.17,.21),(.22,.16,.19)][index],'Eye':(.08,.13,.17),'Lip':tuple(c*.57 for c in skin),'Blush':tuple(min(1,c*1.02+v) for c,v in zip(skin,(.10,.0,.0))),'Sneaker':(.12,.17,.22)}
        if receptionists:extra.update({'Denim':shirt,'Tie':(.80,.20,.17),'Dress shoe':(.035,.036,.043)})
        for role,rgb in {**palette,**extra,'Skin':skin,'Apricot':shirt,'Hair':hair}.items():
            mat=bpy.data.materials.new(name+'_'+role);mat.diffuse_color=(*rgb,1);mat.use_nodes=True
            mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1)
            mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65
            if role in ('Skin','Blush'):mat.node_tree.nodes['Principled BSDF'].inputs['Subsurface Weight'].default_value=.06
            namespace['MATS'][role]=mat
        before=set(bpy.data.objects)
        rig=namespace['human'](name,'Apricot')
        for ob in set(bpy.data.objects)-before:
            for coll in list(ob.users_collection):coll.objects.unlink(ob)
            collection.objects.link(ob)
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
        for child in rig.children:child.select_set(True)
        bpy.context.view_layer.objects.active=rig
        bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,path_mode='AUTO')
        for track in rig.animation_data.nla_tracks:track.mute=True
        rig.animation_data.action=bpy.data.actions.get(name+'_Walk')
        x,y,yaw=poses[index];rig.location=(x,y,.12);rig.rotation_euler.z=yaw
        rig['role']='Receptionist' if receptionists else 'Patient / pedestrian';rig['variant']=index+1
        rigs.append(rig)
    bpy.context.scene.frame_set(9)
    return rigs

def preview(out,rigs,receptionists=False):
    scene=bpy.data.scenes.new('Receptionist cast - tailored suits' if receptionists else 'Patient cast - six playable variants');bpy.context.window.scene=scene
    scene.world=bpy.data.worlds.new('Character studio');scene.world.color=(.45,.5,.55)
    for i,rig in enumerate(rigs):
        scene.collection.objects.link(rig)
        for ob in rig.children:scene.collection.objects.link(ob)
        rig.location=((i-(len(rigs)-1)/2)*1.3,0,.02);rig.rotation_euler.z=0
        for track in rig.animation_data.nla_tracks:track.mute=True
        rig.animation_data.action=bpy.data.actions.get(rig.name.removesuffix('Rig')+'_Idle')
    scene.frame_set(1)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object
    floor_mat=bpy.data.materials.new('Warm studio ground');floor_mat.diffuse_color=(.82,.79,.73,1);floor.data.materials.append(floor_mat)
    bpy.ops.object.camera_add(location=(2.3,-11,3.2));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.86))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=8.7;scene.camera=camera
    if receptionists:camera.data.ortho_scale=3.8
    bpy.ops.object.light_add(type='AREA',location=(0,-4,7));bpy.context.object.data.energy=850;bpy.context.object.data.size=8
    bpy.ops.object.light_add(type='SUN',location=(0,0,6));bpy.context.object.data.energy=1.5;bpy.context.object.rotation_euler=(.4,-.4,-.2)
    scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.resolution_x=1800;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
    folder=out/'characters'/'receptionists' if receptionists else out/'characters'
    stem='little-lifeline-receptionists' if receptionists else 'little-lifeline-patient-cast'
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(stem+'.blend')))
    scene.render.filepath=str(folder/(stem+'.png'));bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    import sys
    output=Path(sys.argv[sys.argv.index('--')+1]);output.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    staff=build(output,True);preview(output,staff,True)
