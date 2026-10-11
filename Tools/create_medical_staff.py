"""Create four original rigged clinical staff, using the established cast style."""
import sys
from pathlib import Path
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_patients as cast

original=Path(cast.__file__).read_text()
original=original.replace('def build(out,receptionists=False):','def build(out,receptionists=False,medical=False):')
marker="    namespace={'bpy':bpy"
pos=original.index(marker)
patch='''    if medical:
        clothing_start=selected.index('    skin(orb("Sweater chest"')
        clothing_end=selected.index('    skin(orb("Trouser hips"',clothing_start)
        selected=selected[:clothing_start]+MEDICAL_CLOTHING+selected[clothing_end:]
'''
original=original[:pos]+patch+original[pos:]
original=original.replace("    folder=out/'characters';folder.mkdir(exist_ok=True)",'''    if medical:
        variants=[('Nurse_01_Man_TealScrubs',(.72,.46,.29),(.04,.43,.44),(.075,.045,.03)),('Nurse_02_Woman_BlueScrubs',(.43,.23,.12),(.13,.32,.66),(.027,.019,.018)),('Doctor_01_Man_WhiteCoat',(.89,.64,.43),(.91,.94,.94),(.23,.13,.065)),('Doctor_02_Woman_WhiteCoat',(.64,.40,.24),(.91,.94,.94),(.07,.035,.02))]
        poses=[(0,0,0)]*4
    folder=out/'characters';folder.mkdir(exist_ok=True)
    if medical:folder=folder/'medical-staff';folder.mkdir(exist_ok=True)''')
original=original.replace("        if receptionists:extra.update", "        if medical:extra.update({'Denim':shirt if index<2 else (.06,.12,.20),'Scrub':(.04,.34,.38),'Metal':(.45,.52,.57)})\n        if receptionists:extra.update")
original=original.replace("rig['role']='Receptionist' if receptionists else 'Patient / pedestrian'", "rig['role']=('Nurse' if index<2 else 'Doctor') if medical else ('Receptionist' if receptionists else 'Patient / pedestrian')")
original=original.replace("'Receptionists - two suited characters' if receptionists else", "'Medical staff - nurses and doctors' if medical else 'Receptionists - two suited characters' if receptionists else")
original=original[:original.index("if __name__=='__main__':")]
clothing='''    skin(orb("Clinical tunic chest",(0,1.077,.008),(.195,.178,.112),uniform,28),"chest")
    skin(orb("Clinical tunic waist",(0,.918,.008),(.165,.131,.108),uniform,28),"spine")
    if VARIANT<2:
        for side in (-1,1):
            skin(tube("Scrub V neckline",(0,1.161,.129),(side*.067,1.237,.106),.009,"Linen",12),"chest")
        skin(box("Scrub chest pocket",(-.107,1.086,.127),(.064,.057,.012),uniform,.008),"chest")
        skin(box("Pocket pen",(-.097,1.111,.139),(.008,.058,.006),"Ink",.003),"chest")
        skin(box("Scrub lower pocket",(.092,.930,.121),(.093,.070,.013),uniform,.012),"spine")
    else:
        skin(box("Undershirt",(0,1.089,.127),(.106,.255,.014),"Scrub",.018),"chest")
        for side in (-1,1):
            lapel=box("White coat lapel",(side*.070,1.133,.145),(.056,.236,.014),uniform,.010)
            lapel.rotation_euler.y=side*.30;skin(lapel,"chest")
            skin(box("Coat lower pocket",(side*.101,.845,.120),(.087,.074,.018),uniform,.010),"pelvis")
            skin(box("Coat skirt panel",(side*.092,.797,.01),(.176,.207,.214),uniform,.025),"pelvis")
        for height in [.991,.932]:skin(orb("Coat button",(0,height,.144),(.009,.009,.005),"Metal",12),"spine")
    skin(box("Staff identity badge",(.096,1.104,.140),(.067,.045,.012),"Linen",.006),"chest")
    skin(box("Badge role stripe",(.096,1.111,.148),(.050,.009,.003),"Blue" if VARIANT<2 else "Rose",.002),"chest")
    # Stethoscope draped around the neck, with curved tubing and a silver chestpiece.
    for side in (-1,1):
        points=[(side*.068,1.233,.099),(side*.108,1.161,.144),(side*.109,1.044,.150),(side*.070,1.001,.154),(0,.992,.155)]
        for a,b in zip(points,points[1:]):skin(tube("Stethoscope tubing",a,b,.0055,"Ink",12),"chest")
    skin(tube("Stethoscope stem",(0,.992,.155),(.026,.961,.157),.005,"Metal",12),"chest")
    skin(orb("Stethoscope chestpiece",(.026,.951,.160),(.021,.023,.007),"Metal",20),"chest")
'''
namespace={'__name__':'medical_cast','MEDICAL_CLOTHING':clothing}
exec(compile(original,'medical_cast','exec'),namespace)
out=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.read_factory_settings(use_empty=True)
rigs=namespace['build'](out,medical=True)
# Use the shared studio render, redirecting its output to the medical cast folder.
import inspect
preview_source=inspect.getsource(cast.preview)
preview_source=preview_source.replace("folder=out/'characters'/'receptionists' if receptionists else out/'characters'", "folder=out/'characters'/'medical-staff'")
preview_source=preview_source.replace("stem='little-lifeline-receptionists' if receptionists else 'little-lifeline-patient-cast'", "stem='little-lifeline-medical-staff'")
preview_source=preview_source.replace("camera.data.ortho_scale=8.7", "camera.data.ortho_scale=6.0")
preview_source=preview_source.replace("'Patient cast - six playable variants'", "'Medical staff - two nurses and two doctors'")
exec(compile(preview_source,'medical_preview','exec'),namespace)
namespace['preview'](out,rigs)
print('MEDICAL_STAFF_COMPLETE')
