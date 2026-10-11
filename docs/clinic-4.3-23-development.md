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
- **Quicker opening.** The office's clean and set-up take 6 s and 8 s, later rooms' 4 s and 5 s (all were 8 s and 10 s),
  so the first room still teaches the rhythm. An optional **Help clear up / Help set up** button takes 1.5 s off the current task per tap and never skips
  it. A save made mid-task keeps its old timer. Measured with a scripted player: first patient 42.0 s → 29.0 s (15.6 s
  when helping), clinic open 115.0 s → 84.0 s (60.5 s). Details in [free progression](qa/free-progression-4.3-23.md).

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

### Not changed

Scene layout and presentation code: separate nursing stations, the open L-shaped reception and waiting area, the
five-patient check-in queue, chairs facing the nursing rooms, the glass front wall and angry departures are as inherited
(no file under `Runtime/Presentation` or `Scenes` changed). Audio code is unchanged.

### Saves and migration

- New saved data: guide state (version 1: skipped lessons, work finished early, legacy credit) and the day's goal
  records with the bonus flag. Both are validated on load and before every write; tampered or oversized data is
  rejected rather than trusted.
- Older saves migrate on load: claims, baselines and today's progress are kept; a day already paid its bonus is never
  paid again.
- Profile schema version is unchanged (4). An older build opening a 4.3 (23) save ignores and drops the new fields; on
  returning to 4.3 (23) the guide and today's goals are rebuilt from the claims, so a tester switching builds back and
  forth could see the finish-early lesson credited again once.

### Development and QA tooling

- `Tools/qa/run_editmode_tests.sh`: official Unity EditMode run with result XML, log, command, source commit, duration and
  exit code kept together.
- `Tools/qa/simulator_preview.sh`: fresh export, unsigned Debug simulator build under the QA identity
  `com.flutterly.gravitile.qa`, install on an owned QA simulator only, 20 runtime screenshots, console output, crash
  reports, the simulator performance report with a summary (median, p95 and maximum frame time and memory per 30-second
  window; simulator only) and the QA saves.
- The QA identity applies only to the simulator export and is validated; release exports always use
  `com.flutterly.gravitile`, and the original identity is restored after export.
- Development-only launch switches (compiled out of release builds): `CLINIC_QA_OPENING`, `CLINIC_QA_PLAY`,
  `CLINIC_QA_GEMS`, `CLINIC_QA_AWAY`, `CLINIC_QA_CONFIRM` (for example `Boost:boost.1h`, `Decoration:Reception`,
  `FinishWork:first`), plus the inherited ones.
- `CLINIC_QA_DOCTORS` works again. It wrote a doctors clinic that validation rejected; it now completes the starter
  clinic through its real purchase rules (QA coins, building time skipped as time away skips it) and opens the doctors
  clinic through the same path as the entrance, so the save reloads and the shared wallet is consistent.

## Evidence

| Check | Source | Result |
|---|---|---|
| Unity EditMode, inherited baseline | `b9fc04e`, reviewer run | 613 passed, 0 failed, 0 skipped, exit 0 |
| Unity EditMode | `d84ac3e`, reviewer run | 638 passed, 0 failed, 0 skipped, exit 0 (276 s) |
| Unity EditMode | `8c9d3fa`, reviewer run with `run_editmode_tests.sh` | 657 passed, 0 failed, exit 0 (399.8 s) |
| Unity EditMode | `757d56b` and later | **pending** |
| Core and profile harness (.NET 8, NUnit shim; Linux) | this checkpoint | 226 passed, 0 failed (subset; not a substitute for Unity) |
| Release tooling `Tools/tests` | unchanged by this work | host baseline 105 passed (Blender Python 3.11); Linux Python 3.13: 92 passed, 1 skipped, 1 macOS-only `xcrun` error |
| Simulator export and Xcode build | `d84ac3e`, reviewer | succeeded; launch on the QA review simulator logged no FMOD or CoreAudio errors |
| Runtime screenshots, play-test, performance | current checkpoint | **pending** (`simulator_preview.sh`, reviewer) |

New tests: `ClinicGuideTests`, `ClinicDailyTests`, `ClinicOpeningPacingTests`, `ClinicProgressionPacingTests`,
`ClinicImprovementTests` (Core); `ClinicGuideSkipTests`, `ClinicDailyGoalTests`, `ClinicGemSpendTests`,
`ClinicQaDoctorsTests` (profile); `ClinicModalPolicyTests` (editor). They cover skipping and resuming lessons, legacy
claims, failed saves, frozen and achievable goals, midnight, the once-only bonus, every gem confirmation case above,
the QA identity rule, measured pacing and the QA doctors clinic.

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
2. Official Unity results for `757d56b` and later are pending.
3. Cash purchase flow not exercised: needs the Xcode run with the local StoreKit configuration (see above).
4. The earlier simulator CoreAudio/FMOD startup crash was not reproduced in the reviewer's `d84ac3e` launch and was not
   changed or fixed; it remains open until a run shows otherwise.
5. No `ClinicPerformanceRecorder` results for this build yet; the preview script copies the simulator report, which is
   not device evidence.
6. Free progression is measured with scripted players, not people. Late starter play is dominated by 2-hour builds with
   one builder; this is reported, not changed.
7. While the *open the doctors clinic* lesson is open, the card shows the next goal instead of the lesson text (the lesson
   reward still appears when it is done). Worth a design check in play.
8. Switching a save between 4.3 (22) and 4.3 (23) can re-credit the finish-early lesson once (see migration).
9. Inherited: the default macOS Python 3.9 fails the release tooling tests on syntax; use Python 3.11 or later.
