"""Room equipment models: twenty pieces per room, each in ten versions from basic to advanced (see ClinicGear).

Run with Blender -b --python Tools/create_room_gear.py [-- --preview OUTDIR] [-- --room Reception ...].
Writes Clinic/Models/<Room>Gear/Gear01.fbx ... Gear20.fbx; every file holds roots Gear01_V1 ... Gear01_V10 stacked at the
origin, and the game shows the one matching the owned version. --preview renders contact images instead of exporting.
"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import gear_kit
import gear_items_firstaid, gear_items_reception, gear_items_waiting, gear_items_consultation, gear_items_pharmacy
import gear_items_office, gear_items_staffroom, gear_items_store

ROOMS = [("Reception", gear_items_reception), ("FirstAid", gear_items_firstaid), ("Waiting", gear_items_waiting),
         ("Consultation", gear_items_consultation), ("Pharmacy", gear_items_pharmacy),
         ("Office", gear_items_office), ("StaffRoom", gear_items_staffroom), ("Store", gear_items_store)]
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
preview = args[args.index("--preview") + 1] if "--preview" in args else None
only = [a for a in args[args.index("--room") + 1:] if not a.startswith("--")] if "--room" in args else None
for room, module in ROOMS:
    if only and room not in only: continue
    gear_kit.export_room(room + "Gear", module.ITEMS, module.FOOT, preview)
print("GEAR_COMPLETE", flush=True)
