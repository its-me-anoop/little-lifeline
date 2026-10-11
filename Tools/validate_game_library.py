import bpy,json
from pathlib import Path
OUT=Path('/Users/anoopjose/Projects/little-lifeline/assets/clinic-reference/game-library');catalog=json.loads((OUT/'catalog.json').read_text())
checked=0
for entry in catalog['decorations']+catalog['amenities']:
    name=entry['name'];count=entry.get('versions',entry.get('levels'));path=OUT/entry['file'];blend=path.with_suffix('.blend')
    with bpy.data.libraries.load(str(blend),link=False) as (src,dst):
        for level in range(1,count+1):assert f'{name}_V{level}' in src.objects,(name,level)
    checked+=count
assert len(catalog['themes'])==19 and len(catalog['equipment'])==160
assert len(list((OUT/'equipment').glob('*/*.fbx')))==160
assert len(list((OUT/'themes').glob('Theme*.fbx')))==19
assert len(list((OUT/'themes').glob('FloorPattern*.fbx')))==7
assert len(list((OUT/'themes').glob('WallFinish*.fbx')))==5
print('LIBRARY_VALIDATED',checked,'new upgrade roots; 160 original equipment files; 19 themes; 12 modular finishes')
