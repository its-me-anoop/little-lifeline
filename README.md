# Little Lifeline

A miniature 3D clinic you grow one room at a time.

Little Lifeline is a Unity idle management game for iPhone and iPad. Patients arrive by car or taxi, check in at reception, see a doctor, receive first aid and collect medication. Collect their payments, hire staff and upgrade rooms, workstations and training. Fully upgrading the starter clinic opens a larger doctors clinic, with its own staff, six room tiers and a shared wallet.

- Progress saves atomically on the device, with a recoverable backup. Offline earnings accrue for up to eight hours; construction continues for the whole absence.
- There are no advertisements or subscriptions.

## Open the game

Use **Unity 6000.3.24f1**, with iOS Build Support and an activated license. Add `Unity/OrbitOrchard` in Unity Hub. Select **Idle Clinic → Prepare project**, then **Idle Clinic → Open game scene**, and enter Play mode.

The game lives in `Assets/IdleClinic`. `Assets/OrbitOrchard` holds the shared platform layer the clinic builds on: Apple services (StoreKit 2, Game Center), input setup, the iOS build and export scripts, and the native Swift/Objective-C++ bridge. The Unity project folder and Apple bridge keep their earlier Orbit Orchard names.

## Validate

Run the Unity Test Runner's **EditMode** suite, then exercise the game in Play mode. From the repository root, with no other Editor using this project:

```bash
LIFELINE_EDITOR='/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity'
mkdir -p build
"$LIFELINE_EDITOR" -batchmode -projectPath "$PWD/Unity/OrbitOrchard" \
  -buildTarget iOS -runTests -testPlatform EditMode \
  -testResults "$PWD/build/clinic-tests.xml" -logFile "$PWD/build/clinic-tests.log"
```

Release tooling has its own tests: `python3 -m unittest discover -s Tools/tests`.

## Release

Bundle `com.flutterly.gravitile`, minimum iOS 18. The latest verified delivery is **3.3 (17)** to Internal TestFlight; see the [doctors clinic release record](docs/doctors-clinic-release.md). Earlier clinic releases are recorded in [3.1](docs/idle-clinic-release.md) and [3.2](docs/idle-clinic32-release.md). Local signing is described in [clinic-local-signing.md](docs/clinic-local-signing.md), and App Store copy is in [docs/appstore](docs/appstore/README.md).

Export the committed Unity source, package its generated Xcode project, then use the [release workflow](.github/workflows/release.yml). A successful upload command is separate from a processed, available TestFlight build.

## Art and privacy

Display type is Baloo 2; body type is Atkinson Hyperlegible. Font licenses are bundled in `Assets/IdleClinic/Resources/Fonts`. Models come from `Tools/create_clinic_assets.py` and the Blender source `assets/idle-clinic-game.blend`; audio is synthesized by `Tools/create_clinic_audio.py`.

Progress stays on the device. Apple handles purchases and optional Game Center participation. The project includes no advertising or tracking SDK.
