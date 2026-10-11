"""Create the original Little Lifeline clinic director."""
import sys
from pathlib import Path
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_patients as cast
source=Path(cast.__file__).read_text()
source=source[:source.index("if __name__=='__main__':")]
source=source.replace("variants=[('Receptionist_01_Man_NavySuit',(.72,.46,.29),(.045,.08,.15),(.075,.045,.03)),('Receptionist_02_Woman_CharcoalSuit',(.43,.23,.12),(.11,.095,.135),(.027,.019,.018))]", "variants=[('Boss_ClinicDirector',(.64,.40,.25),(.009,.009,.010),(.15,.105,.075))]")
source=source.replace("folder=folder/'receptionists'", "folder=folder/'boss'").replace("out/'characters'/'receptionists'", "out/'characters'/'boss'")
source=source.replace("'little-lifeline-receptionists'", "'little-lifeline-boss'")
source=source.replace("'Receptionists - two suited characters'", "'Boss - clinic director'").replace("'Receptionist cast - tailored suits'", "'Boss - clinic director portrait'")
source=source.replace("'Receptionist' if receptionists", "'Clinic boss' if receptionists")
source=source.replace("'Tie':(.80,.20,.17)", "'Tie':(.73,.48,.11)")
source=source.replace('"Receptionist name badge"', '"Director name badge"')
source=source.replace('"Badge blue accent",(-.102,1.120,.138),(.048,.008,.003),"Blue"', '"Director badge gold accent",(-.102,1.120,.138),(.048,.008,.003),"Gold"')
# Gold lapel pin and fine silver temple highlights distinguish the director.
source=source.replace('    skin(box("Receptionist name badge"', '    skin(box("Receptionist name badge"')
needle='    skin(box("Director name badge"'
source=source.replace(needle, '''    skin(orb("Gold lapel pin",(.098,1.189,.148),(.012,.014,.006),"Gold",16),"chest")
    for side in (-1,1):
        skin(orb("Silver temple",(side*.130,1.489,.013),(.022,.048,.043),"Metal",20),"head")
    skin(box("Director name badge"''')
source=source.replace("'Dress shoe':(.035,.036,.043)", "'Dress shoe':(.035,.036,.043),'Metal':(.48,.49,.50)")
source=source.replace("camera.data.ortho_scale=3.8", "camera.data.ortho_scale=2.7")
source=source.replace("scene.render.resolution_x=1800;scene.render.resolution_y=900", "scene.render.resolution_x=1200;scene.render.resolution_y=1200")
namespace={'__name__':'boss_cast'}
exec(compile(source,'boss_cast','exec'),namespace)
out=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.read_factory_settings(use_empty=True)
rigs=namespace['build'](out,True)
namespace['preview'](out,rigs,True)
print('BOSS_COMPLETE')
