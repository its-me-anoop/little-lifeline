import json,re,shutil
from pathlib import Path
ROOT=Path('/Users/anoopjose/Projects/little-lifeline');OUT=ROOT/'assets/clinic-reference/game-library';OUT.mkdir(parents=True,exist_ok=True)
source=(ROOT/'Unity/OrbitOrchard/Assets/IdleClinic/Core/ClinicGear.cs').read_text();section=source.split('private static readonly string[][] Names =')[1].split('private static readonly string[] VersionNames')[0]
rooms=['Reception','FirstAid','Waiting','Consultation','Pharmacy','Office','StaffRoom','Store']
rows=re.findall(r'new\[\]\s*\{([^}]+)\}',section)
versions=re.findall(r'"([^"]+)"',source.split('private static readonly string[] VersionNames')[1])
items=[]
for room,row in zip(rooms,rows):
    names=re.findall(r'"([^"]+)"',row);assert len(names)==20
    for i,name in enumerate(names,1):
        src=ROOT/f'Unity/OrbitOrchard/Assets/IdleClinic/Art/Resources/Clinic/Models/{room}Gear/Gear{i:02d}.fbx';assert src.exists()
        dst=OUT/f'equipment/{room}/Gear{i:02d}.fbx';dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
        items.append({'room':room,'id':i,'name':name,'file':str(dst.relative_to(OUT)),'versions':[{'level':v,'name':n,'root':f'Gear{i:02d}_V{v}'} for v,n in enumerate(versions,1)],'source':'Existing original Little Lifeline model'})
style=(ROOT/'Unity/OrbitOrchard/Assets/IdleClinic/Runtime/Presentation/ClinicRoomStyle.cs').read_text()
roles=['Floor','Pattern','Wall','Wainscot','Trim','Accent','AccentDark','Warm','Cool','Wood','Fabric','Rug','Metal']
themes=[]
for values,name in re.findall(r'P\(([^\n]+)\)\s*,?\s*//\s*([^\n]+)',style):
    colors=re.findall(r'"([0-9A-F]{6})"',values)
    if len(colors)==13:themes.append({'id':len(themes)+1,'name':name.strip(),'colors':dict(zip(roles,colors))})
assert len(themes)==19
catalog={'equipment_types':160,'equipment_versions':1600,'room_categories':rooms,'version_names':versions,'equipment':items,'themes':themes,'floor_patterns':['Square tiles','Checkerboard','Staggered planks','Diagonal lattice','Border and diamond','Terrazzo','Wide bands'],'wall_finishes':['Plain','Wainscot','Striped wallpaper','Tiled dado','Panel moulding'],'provenance':'Original local assets; no third-party models or purchases','runtime_status':'Prepared assets. The new Blender clinic and this decoration library are not yet wired to playable upgrade purchases.'}
(OUT/'catalog.json').write_text(json.dumps(catalog,indent=2))
print('CATALOG_COMPLETE',len(items),len(themes))
