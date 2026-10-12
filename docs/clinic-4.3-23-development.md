# Little Lifeline 4.3 (23): development notes and changelog

**Status: development checkpoint, not a release.** Branch `claude/next-dev-4.3-b23` in `its-me-anoop/little-lifeline`.
Nothing here was uploaded, submitted, deployed or merged, and no App Store Connect, StoreKit catalogue or billing setting
was touched. The inherited, previously uncommitted 4.3 work is commit `b9fc04e` ([baseline record](claude-baseline/README.md));
everything after it is new work for this build.

## Version

The original checkout's `ProjectSettings.asset` still says 4.2 (20), but the inherited work is the 4.3 feature set and
`build/LittleLifeline-4.3-22.xcarchive` in the original checkout is a 4.3 (22) archive for `com.flutterly.gravitile`,
created 4 October 2026. The next development build is therefore **4.3 (23)** (`bundleVersion` 4.3, iPhone build 23).
Whether 4.3 (22) reached TestFlight or the App Store was not checked from here; if the store already has a later
build, raise the build number before any upload.

## Changelog

### Guide and onboarding

- **Optional gem lessons can be skipped.** *Decorate reception* and *Finish a renovation early* are marked optional,
  explain the free alternative, and show **Skip · keep gems**. The guide carries on with the free lessons. Skipped
  lessons are listed under Gems & goals with **Resume**, and their gem reward can still be collected once done. Guide
  ids, order and rewards are unchanged; skips are saved.
- **The finish-early lesson means what it says.** It completed on any gem spend, so buying decor counted. It now counts
  only work finished early with gems. A save from before the fix keeps every claim; a player who had already spent gems
  but not collected the lesson keeps that credit once.
- **Progress on the guide card.** The opening shows its step (*OPEN YOUR CLINIC · 3 OF 12*) and each lesson its place in
  its clinic's chapter (*NEXT STEP · 3 OF 12*; the doctors clinic counts its own 10); VoiceOver reads the same.
- **Next-goal card.** When no lesson is left, or during the long *open the doctors clinic* lesson, the guide card names
  what to work towards: BUILDING (what the builder finishes and when), NEXT IMPROVEMENT (price, ready to buy or when the
  builder is free) or SAVING FOR (coins held against the price, an estimate from the measured earning rate, and a
  reminder to collect counters). Tapping it opens that room, amenity or the office.
- **Quicker opening, with the boss walking at a steady pace.** The office's clean and set-up take 6 s and 8 s. Each later
  room starts with the boss's walk there at one brisk pace (2.4 m/s; 4.3 (22) varied from 1.3 to 2.9 m/s), then 2.5 s of
  cleaning or 5 s of setting up (were 10 s). An optional **Pitch in** button takes 1.5 s off the work per
  tap, never off the walk, and never skips the task. A save made mid-task keeps its old timer. Measured with a scripted
  player: first patient 42.0 s → 34.2 s (21.0 s when helping), clinic open 115.0 s → 93.0 s (70.1 s). Details in
  [free progression](qa/free-progression-4.3-23.md).

### Opening story (separate checkpoint)

New games open with a short, lightly funny story. The player is the boss; **Maya**, a nurse in teal scrubs with her
chestnut hair in a bun (and a tin of biscuits), knocks on the door, asks for the boss's name, talks them through opening
the clinic and becomes the first nurse. The script is data, `Resources/ClinicStory.json`, read by the engine-free rules
in `Core/ClinicStory.cs`; the approved lines are used word for word.

- **Prologue at the door.** The boss waits just inside the front door. Maya walks up the pavement (3.4 s), taps on the
  glass (2.4 s) and comes in to the boss while her first two lines show; they turn to face each other. Tap the card to
  continue. *Sorry, I didn't catch your name?* opens the panel with a name field, **That's me** and **Just call me boss**.
  *Nice to meet you, {name}! Where do we start?* has two answers; either one sends them off together to clean the office,
  so an answer is also the action.
- **Together, in close-up (after the first review).** While Maya and the boss talk, the camera holds a close view of the
  two of them (about three times closer than the usual view), framed above the card or panel, and follows them as they
  walk. Maya keeps a step behind the boss from room to room until she is hired, is hired beside the boss in the nursing
  room, and the boss stays with her there while the first patients queue (the view widens to take in the queue). The boss
  says each answer aloud in a speech bubble and Maya replies in hers. Panning or zooming pauses the close view until the
  next line; the home button returns to it; once the clinic is open the camera eases back to the whole clinic. With Less
  motion the view is set for each line instead of following. The first task now starts with the walk from the door to the
  office (7 s, the same steady pace), so the story adds that walk; skipping keeps the old start in the office.
- **One line per step.** The guide card shows Maya (eyebrow *MAYA · 3 OF 12*, her line, then the task: *Clean reception ·
  tap to start*, or *You're cleaning · ready in 4s · tap to pitch in*). The opening panel repeats her line above the task,
  so the conversation stays visible with the card hidden. Hiring Maya is answered **You're hired, Maya.** or **Welcome
  aboard, Maya.**; recruiting the receptionist is answered **Hire a receptionist**. Step titles now speak to the player
  (*Clean your office*, *Check in your first patient*, *Recruit a receptionist*) in every game.
- **Remarks.** A speech bubble over Maya shows "…" while she has a line and her remarks for a few seconds as things
  happen: the reception bell, the first coins, being hired, the first **Pitch in** (once), the receptionist and the
  opening. The boss's answers appear over the boss first. They never need a tap and are not replayed for steps that
  finished while the app was closed.
- **Maya is the first nurse.** She is hired where she stands and walks to nursing station 1 as the same figure, keeping
  her look. Her station panel says *Maya · 4s per patient* and her training *Maya's training*. The first guide lesson
  after the opening is introduced by her: *Better kit means quicker care. Let's upgrade my equipment.*
- **Name.** Letters in any language with their accents, single spaces, hyphens and apostrophes, up to 14 characters,
  trimmed; empty or skipped means "boss". Characters the game's fonts cannot draw are left out (the fonts are Latin with
  accents), with a live preview: *Maya will call you Sam.* The name is saved only in the local save and can be changed
  in **Settings › Story**, which also shows the story so far as a chat log using the current name.
- **Skip story** sits above the card from the first line. Skipping keeps the plain opening (titles and reasons), the
  player stays "boss" and Maya is still the first nurse; the log keeps what was read.
- **Relaunch mid-story** resumes at the same line: the prologue position, name and answers are saved; step lines follow
  the saved opening step.
- **Only new games.** Saves from before the story have no story data and keep the plain opening and their first nurse's
  look. No price, reward or economy rule changed; the only timing change is the first task's walk from the door in
  games that tell the story.
- **No jumping.** Whenever the boss changes place between tasks (after hiring Maya in any game, or leaving the door) the
  boss now walks there at the opening's steady pace instead of appearing there.

### Feedback and progression

- **Every coin purchase says what it changed**, in the form *Treatment 12.0s → 11.2s* or *Visit fee 40 → 42 coins*
  (illustrative numbers), then suggests the next improvement with its price.
- Equipment panels show the service time before and after the next upgrade; the decor panel shows the exact visit fee
  before and after.
- **Daily goals are fixed for the day and always achievable.** Goals are planned once per day and saved, so upgrades,
  fee changes or opening the car park cannot move them. A spending goal is only set when enough coin improvements remain
  (a fully upgraded starter clinic used to get an impossible one on 249 of 365 days; now on none), with tips and
  check-in goals as alternatives. The all-goals bonus pays once per day, also across relaunches. Yesterday's goals cannot
  be collected after midnight.
- **Balance unchanged on purpose.** Prices, income, build times and the offline limit are as before. The measured free
  progression and the main source of waiting (one builder and the 2-hour build cap late in the starter clinic) are
  written up for the owner to decide: [free progression](qa/free-progression-4.3-23.md).

### Gems: clear, confirmed, optional

- **Every gem spend opens a confirmation sheet** with what you get (with numbers), how long it lasts, the price, your
  balance and the balance after, and **Cancel**. This covers coin packs, boosts, offline upgrades, the second builder,
  decor and finishing work early (20-gem and 30-gem offers were one tap before). A free last-minute finish needs no sheet.
- With too few gems the sheet shows the shortfall and a route to free gems instead of a spend button.
- The sheet stays truthful while open: a falling finish price is passed on, a rising price needs a new confirmation,
  work that finishes on its own closes the sheet with nothing spent, a boost's time left stays current. Rapid repeated
  taps spend once; opening another offer cancels the earlier confirmation; a failed save spends nothing and can be
  retried; the sheet closes when the app is paused or the player changes clinic, and a confirmation can only spend in
  the clinic it was made in.
- With VoiceOver, everything behind the sheet is hidden while it is open and the sheet's facts are read as one summary.
- Decor states the added coins per visit, not a percentage.
- Cash offers say they open Apple's purchase confirmation. Product ids, gem quantities (80, 450, 1,000, 2,800), the
  400-gem or cash second builder, localized prices, restore and the pending, cancelled, refunded and exactly-once grant
  handling are unchanged. No discounts, timers, scarcity or best-value claims were added.
- The shop shows today's free gems and where they come from.

### Characters (separate visual checkpoint)

- **One continuous, smoothly skinned body per character** instead of separate rigid pieces: torso, neck, arms and legs
  are lofted and bend at the joints (two bone influences per vertex), so there are no gaps at shoulders, hips or elbows.
- **Faces that read**: eye whites with irises, brows, a smaller nose, lips and ears, on a head shaped with a jaw and chin.
- **Hair and trousers per person**: new `Hair` and `Trousers` material slots, tinted by the game from
  `Resources/ClinicCharacterStyles.json` (black, chestnut, gold or silver hair; denim, chinos, grey, black or green
  trousers). Nurses' trousers now match their scrubs; doctors wear grey, everyone else dark trousers.
- **Clothing**: patients wear a hoodie (hood, drawstrings, pocket, ribbed hem and cuffs) with sneakers; receptionists a
  blazer with shirt, lapels, cuffs, belt and badge with leather shoes; nurses short-sleeved scrubs with a V-neck, pocket,
  pen and badge with clogs.
- **Headwear refitted to the new head** (curls, buns, ponytail, head scarves, visitor cap, silver hair, beard,
  spectacles) and bodywear (backpack, doctor's coat, pharmacist badge, staff training marks) laid on the torso surface;
  the visitor cap is for visitors only. The departure dressing and smile sit on the face.
- **Animation**: the walk keeps its 1.08 m cycle but plants the stance foot, rolls heel to toe, bends the knee and elbow
  (fixed limb lengths), bobs and sways the pelvis and counter-swings the arms; idle breathes and shifts weight; work
  poses lean in. Bones, rest pose, clip names and lengths, sockets and seated heights are unchanged.
- **Materials**: a soft sheen on skin, lips and hair; cloth stays matte. About 2,950 vertices and 7 to 10 flat-colour
  materials per model (was about 3,100 and 5 to 8); no textures, no paid or downloaded assets.

### Not changed

Scene layout: separate nursing stations, the open L-shaped reception and waiting area, the five-patient check-in
queue, chairs facing the nursing rooms, the glass front wall and angry departures are as inherited (the scene and room
presentation are untouched; presentation changes are limited to the characters, the boss's walk timing and Maya in the
opening story). Audio code is unchanged.

### Saves and migration

- New saved data: guide state (version 1: skipped lessons, work finished early, legacy credit) and the day's goal
  records with the bonus flag. Both are validated on load and before every write; tampered or oversized data is
  rejected rather than trusted.
- Older saves migrate on load: claims, baselines and today's progress are kept; a day already paid its bonus is never
  paid again.
- The opening story adds `story` to the profile (new games only: whether it is told, skipped and where, prologue line,
  the boss's name, answers as `lineId=reply`, when the player first pitched in). It is repaired on load (out-of-range
  values clamped, the name re-checked, malformed answers dropped) and never decides whether a save loads; older saves get
  an empty, switched-off story.
- Profile schema version is unchanged (4). An older build opening a 4.3 (23) save ignores and drops the new fields; on
  returning to 4.3 (23) the guide and today's goals are rebuilt from the claims, so a tester switching builds back and
  forth could see the finish-early lesson credited again once.

### Development and QA tooling

- `Tools/qa/run_editmode_tests.sh`: official Unity EditMode run with result XML, log, command, source commit, duration and
  exit code kept together.
- `Tools/qa/simulator_preview.sh`: fresh export, unsigned Debug simulator build under the QA identity
  `com.flutterly.gravitile.qa`, install on an owned QA simulator only, 25 runtime screenshots (including the story's name question, where to start, the office together,
  hiring Maya and the story so far), console output, crash
  reports, the simulator performance report with a summary (median, p95 and maximum frame time and memory per 30-second
  window; simulator only) and the QA saves.
- The QA identity applies only to the simulator export and is validated; release exports always use
  `com.flutterly.gravitile`, and the original identity is restored after export.
- Development-only launch switches (compiled out of release builds): `CLINIC_QA_OPENING`, `CLINIC_QA_PLAY`,
  `CLINIC_QA_GEMS`, `CLINIC_QA_AWAY`, `CLINIC_QA_CONFIRM` (for example `Boost:boost.1h`, `Decoration:Reception`,
  `FinishWork:first`), `CLINIC_QA_STORY` (`name`, `reply` or `skip`), `CLINIC_QA_STORY_NAME`, `CLINIC_QA_OPEN=opening`
  and `CLINIC_QA_OPEN=settings:story`, plus the inherited ones. `CLINIC_QA_OPENING` answers the prologue first, as taps
  would.
- `CLINIC_QA_DOCTORS` works again. It wrote a doctors clinic that validation rejected; it now completes the starter
  clinic through its real purchase rules (QA coins, building time skipped as time away skips it) and opens the doctors
  clinic through the same path as the entrance, so the save reloads and the shared wallet is consistent.

## Evidence

| Check | Source | Result |
|---|---|---|
| Unity EditMode, inherited baseline | `b9fc04e`, reviewer run | 613 passed, 0 failed, 0 skipped, exit 0 |
| Unity EditMode | `d84ac3e`, reviewer run | 638 passed, 0 failed, 0 skipped, exit 0 (276 s) |
| Unity EditMode | `8c9d3fa`, reviewer run with `run_editmode_tests.sh` | 657 passed, 0 failed, exit 0 (399.8 s) |
| Unity EditMode | `0ad88d8`, reviewer run | 678 total: 671 passed, **7 failed** (6 HUD accessibility fixtures: null gem-confirmation node; boss nursing door), exit 2 |
| Unity EditMode | `6da17d3` (7 fixes), reviewer run | 681 passed, 0 failed; native Xcode build succeeded |
| Unity EditMode | the character checkpoint `cfabfb6` | **pending** |
| Unity EditMode | the story checkpoint `656ec5b`, reviewer run | 735 passed, 0 failed, 0 skipped (416 s); simulator export and native Xcode build succeeded; 10 staged runtime screenshots |
| Unity EditMode | the staging pass (together, close-up) | **pending** (updated `ClinicStoryWorldTests`, `ClinicStoryHUDTests`, new Core entrance test; not run here) |
| Core and profile harness (.NET 8, NUnit shim; Linux) | the staging pass | 265 passed, 0 failed (subset; not a substitute for Unity) |
| Story UI checks | the staging pass | story presentation and opening panel compile against minimal API stand-ins; every changed C# file parses (Roslyn, Linux); not a Unity build |
| Simulator performance (not device evidence) | game `0ad88d8`, reviewer | 90 s sample: median 16.67 ms, p95 17.25 ms, max 48.66 ms; Unity 128 MB, managed 10 MB; startup 1532.73 ms |
| Simulator performance (not device evidence) | game `6da17d3` (run `d7e9727`), reviewer | median 16.67 ms, p95 17.25 ms, max 49.19 ms; first 30 s max 213.38 ms; Unity 127 MB, managed 10 MB |
| Release tooling `Tools/tests` | unchanged by this work | host baseline 105 passed (Blender Python 3.11); Linux Python 3.13: 92 passed, 1 skipped, 1 macOS-only `xcrun` error |
| Simulator export and Xcode build | `d84ac3e`, reviewer | succeeded; launch on the QA review simulator logged no FMOD or CoreAudio errors |
| Runtime screenshots, play-test, performance | current checkpoint | **pending** (`simulator_preview.sh`, reviewer) |

New tests: `ClinicGuideTests`, `ClinicDailyTests`, `ClinicOpeningPacingTests`, `ClinicProgressionPacingTests`,
`ClinicImprovementTests` (Core); `ClinicGuideSkipTests`, `ClinicDailyGoalTests`, `ClinicGemSpendTests`,
`ClinicQaDoctorsTests`, `ClinicStoryProfileTests` (profile); `ClinicModalPolicyTests`, `ClinicCharacterTests`,
`ClinicStoryHUDTests`, `ClinicStoryWorldTests` (editor); `ClinicStoryTests` (Core). They cover skipping and resuming lessons, legacy
claims, failed saves, frozen and achievable goals, midnight, the once-only bonus, every gem confirmation case above,
the QA identity rule, measured pacing, the QA doctors clinic, and the story: prologue, naming rules, answers starting
their task, skipping, relaunching mid-story, older saves, damaged story data, Maya's walk and hire, her name at her
station, the first lesson and the Settings log.

## Running it

```bash
# Unity EditMode tests (macOS, Unity 6000.3.24f1; no other Editor may have the project open)
Tools/qa/run_editmode_tests.sh

# Simulator preview on the owned QA simulator; LIFELINE_QA_FRESH=1 removes only the QA app's save first
LIFELINE_QA_UDID=C5FFBFF1-F652-4F82-A3B8-FA453B16610A LIFELINE_QA_FRESH=1 Tools/qa/simulator_preview.sh

# Release tooling tests, with the Python 3.11 used for the baseline (the default Python 3.9 cannot import them)
<python3.11> -m unittest discover -s Tools/tests
```

Evidence lands in `../claude-work/runs/<UTC>-<commit>-…` when run from the task workspace. Cash offers can only be
exercised by running the exported Xcode project from Xcode on the QA simulator: development exports attach the local
`Clinic.storekit` configuration to the scheme, so purchases there are local StoreKit test transactions. A plain
`simctl launch` has no StoreKit configuration, so gem packs do not load in those screenshots (the shop shows the App
Store status line instead).

## Outstanding defects and gaps

1. **No runtime verification by me yet.** The confirmation sheet on compact and short viewports, safe areas, the skip,
   help and next-goal controls, VoiceOver isolation and the welcome-back flow are tested only in code. The reviewer's
   simulator runs and screenshots are the evidence still needed.
2. Official Unity results for the character checkpoint are pending. `6da17d3` (the fixes for `0ad88d8`'s 7 Editor
   failures) passed 681 of 681 with a native build. The new characters are reviewed only in Blender previews so far
   (`Tools/qa/render_characters.py`); how they look and perform in the game on a phone is not yet verified.
3. Cash purchase flow not exercised: needs the Xcode run with the local StoreKit configuration (see above).
4. The earlier simulator CoreAudio/FMOD startup crash was not reproduced in the reviewer's `d84ac3e` launch and was not
   changed or fixed; it remains open until a run shows otherwise.
5. No `ClinicPerformanceRecorder` results for this build yet; the preview script copies the simulator report, which is
   not device evidence.
6. Free progression is measured with scripted players, not people. Late starter play is dominated by 2-hour builds with
   one builder; this is reported, not changed.
7. **The opening story is only partly runtime-verified.** `656ec5b` was seen in staged simulator screenshots (Maya's
   first line, the name question, where to start, the first lesson). The staging pass (the boss and Maya together, the
   close-up camera, the boss's speech bubble, walking instead of jumping) is not yet seen running. Also not yet seen: the name field with the iOS keyboard
   (Unity shows its own input line above the keyboard), the card and panel with long lines on a compact phone, the
   speech bubble, Maya's walk through the entrance and her waiting spot (chosen clear of the corridor lanes and the
   queue, not checked against every prop), and VoiceOver on the name field. Names in scripts the bundled fonts lack
   (for example Cyrillic or Japanese) are reduced to their Latin letters or to "boss"; a fallback font would lift this.
   Reading the prologue adds about 13 to 16 s before the first task (54 words at 200 to 250 words a minute) plus typing
   a name; see [free progression](qa/free-progression-4.3-23.md). Skip story removes it.
8. While the *open the doctors clinic* lesson is open, the card shows the next starter goal, marked *DOCTORS CLINIC ·*,
   instead of the lesson text (the lesson reward still appears when it is done). Worth a design check in play.
9. Switching a save between 4.3 (22) and 4.3 (23) can re-credit the finish-early lesson once (see migration).
10. Inherited: the default macOS Python 3.9 fails the release tooling tests on syntax; use Python 3.11 or later.
