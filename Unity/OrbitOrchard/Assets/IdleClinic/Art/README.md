# Idle Clinic art

These are original, local Blender assets for the fixed clinic. They do not reuse the former train scene or an image backdrop.

Source: `assets/idle-clinic-game.blend`. Regenerate furniture and all character clips with:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python Tools/create_clinic_assets.py
```

Regenerate the 1024px opaque nurse-and-care-bag icon without touching FBXs:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python Tools/create_clinic_assets.py -- --icon-only
```

`Patient`, `Receptionist`, and `Nurse` each contain one skinned mesh, an 18-bone skeleton, and six baked actions: `Idle`, `Walk`, `CheckIn`, `Treat`, `Sit`, and `Call`. The scoped Unity importer uses Legacy clips; the pooled presentation layer samples them using simulation time. Reduced Motion freezes cyclic poses while preserving essential patient and staff travel.

### Characters (4.3 (23) art pass)

The three characters are built by `Tools/clinic_characters.py` (called from `create_clinic_assets.py`): one continuous, smoothly skinned body per model (lofted torso, neck and limbs that bend at the joints, at most two bone influences per vertex), a sculpted head with eye whites, irises, brows, nose, lips and ears, a hair cap, role clothing (hoodie, blazer with shirt and lapels, scrubs with a V-neck and matching trousers) and shoes. The walk keeps the 1.08 m cycle but plants the stance foot, rolls heel to toe, bends knee and elbow (two-bone IK with fixed limb lengths), bobs and sways the pelvis and counter-swings the arms; idle breathes and shifts weight. Bone names, rest positions, clip names and lengths, sockets and the seated pelvis height are unchanged; the generator still asserts every joint against the authored pose. About 2,950 vertices and 7 to 10 flat-colour materials per model (no textures).

Regenerate only the characters (furniture FBXs and the `.blend` are left alone):

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python Tools/create_clinic_assets.py -- --characters-only
```

`--classic-characters` rebuilds the previous rigid-part figures from the original `human()` for comparison. `CLINIC_MODELS_OUT=<folder>` writes the FBXs elsewhere for review.

`Resources/ClinicCharacterStyles.json` is shared by the game and the tools: the torso surface that badges and training marks lie on, hair and trouser colours per visitor (`Hair` and `Trousers` material slots are tinted at runtime), the headwear (curls, buns, ponytail, scarves, cap, silver hair, beard, spectacles), bodywear (backpack, doctor's coat, pharmacist badge) and the departure dressing. `Tools/qa/render_characters.py <fbx-folder> <out> [label]` renders review previews from the FBXs and that file: close-up, face, the game's camera angle, a walk cycle, every action, and the full visitor and staff wardrobe. They are Blender source-art previews, not Unity or device screenshots.

Furniture contains authored empty sockets named `Asset__patient`, `Asset__staff`, and `ReceptionDesk__cash`. The renderer reads these imported transforms for occupancy and payment origins. The asset conversion applies the same coordinate transform to geometry, skeletons, and sockets. Do not correct an apparent mirror by moving actors independently of their furniture; inspect imported transforms and geometry first.

`AssetsPreview.png` and `CarePosePreview.png` are Blender source-art previews, not Unity or device runtime screenshots. `IconProof-before-tonal-pass.png` preserves an intermediate render. The final shipped icon is `AppIcon.png`.

The nine FBXs total approximately 3.1 MB. They use material colors rather than downloaded textures. The world shares materials and primitive meshes and pools skinned actors. No Blender process used for these assets operates on the user's open GUI scene.
