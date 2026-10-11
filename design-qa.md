# Shop redesign QA — option 2

Source visual truth: `/Users/anoopjose/.codex/generated_images/01a10578-f17d-78c3-bf01-f0f1c14752ad/exec-b6e78fcf-6250-4874-8ac0-63224bed3f38.png`

Implementation: native Unity iPhone app, not a web prototype. Existing production game scope takes precedence over the skill's fresh web/mobile template defaults.

## First comparison

- Source: 851 × 1848 pixels, normalized to 440 × 956.
- Portrait implementation: `build/shop-redesign-portrait-first.png`, 1320 × 2868 pixels, logical viewport 440 × 956, density 3.
- Combined comparison: `build/shop-comparison-first.png`, source on left, implementation on right, each 440 × 956.
- Duo: `build/shop-redesign-duo-first.png`, 1398 × 2034, folded viewport 466 × 678, density 3.
- State: Shop overview, second builder unowned, loaded native product catalogue. Actual simulator balances and USD storefront prices differ from the mock's INR samples; these are intentional live values, not design errors. The native portrait device's camera island is OS-owned.

## Findings and fixes

- **P2 — headline line spacing:** the single multiline display-font label separates the lines too far. Replaced with two bounded bold labels.
- **P2 — catalogue density:** 64-point rows, a tall navigation row, and excess hero height push the secondary navigation and restore control below the portrait viewport. Reduced gem rows to 52 points, tightened the fixed header/tabs, and bounded the hero copy.
- **P2 — hero crop:** full-body builder artwork appears too small beside the offer. Changed the measured artwork slot to crop consistently with the source's closer composition.
- **P2 — secondary illustration:** the Coins & boosts row requires its own coin/hourglass artwork. Generated a matching asset rather than substituting an approximate drawing.

## Fidelity surfaces

- Typography: existing rounded game fonts; purchase text, amounts, and shortfall stay legible. Headline uses the bold body face to match the mock's stronger weight and eliminate display-font paragraph spacing.
- Layout: builder-first hierarchy and grouped illustrated gem rows are implemented. The body remains scrollable on the shorter Duo viewport; header and close control remain fixed.
- Colors: existing ivory, sage, forest green and ruby palette; unaffordable offers retain readable contrast, with explicit shortfall copy.
- Imagery: original transparent builder and four distinct gem illustrations; imported at 512/256 pixels for their small screen slots. Existing game world is preserved.
- Copy: existing product quantities and payment options; no unsupported Best value label, discounts, or new bundles. Owned/loading states have appropriate wording.

## Verification

613 tests passed on the initial implementation; 56 UI and premium tests passed after subsequent data-refresh changes. Simulator export and Xcode build succeeded. Manual touch navigation is pending because the Mac is locked. No real-money purchase was made.

## Compact comparison

`build/shop-comparison-compact.png` compares the same reference and the compact portrait capture at 440 × 956. Headline line spacing and original imagery are corrected; the four gem rows and Coins & boosts link fit. P2: restore control remains below the bounded body viewport. Reduced hero copy minimum height by 20 points, secondary-row padding and margins, and section spacing. Final spacing changes compiled and exported successfully, but their screenshot could not be verified because the simulator failed during audio startup.

Final compact code tests: 56 passed, 0 failed (`build/clinic-shop-compact-tests.xml`). Compact Unity export and Xcode build succeeded. An earlier attempt to overwrite an existing export crashed inside Unity’s Xcode scripting bridge; a fresh generated export directory succeeded without source changes.

The Duo runtime returned black captures after installing, so only the isolated QA device was rebooted. Capture verification remains blocked. The final portrait build also aborted during native audio initialization before loading the UI. Two crash reports show `AURemoteIO::Start`, `_ReportRPCTimeout`, and `FMOD::OutputCoreAudio::startCallback`; retries, running one QA game, and restarting the QA simulator sound service did not resolve it. This is a runtime verification blocker, not evidence that the final spacing fix was rendered successfully.

## Final status

- Final Unity export: succeeded (`build/clinic-shop-final-export.log`).
- Final Xcode simulator build: succeeded (`build/clinic-shop-final-build.log`).
- Last tested code: 56 UI and premium tests passed; subsequent changes are USS spacing only. Initial full suite: 613 passed.
- Last successfully rendered preview: `build/shop-redesign-portrait-compact.png`.
- Last valid same-input comparison: `build/shop-comparison-compact.png`.
- Final spacing adjustment: compiled, visual verification pending. Do not claim its footer visibility is verified.
- Manual navigation and confirmation: pending. Computer control was blocked by the locked Mac and subsequently failed to start its native pipe. No real-money purchase was made.
- Existing clinic state and original simulator were preserved; this redesign has not been uploaded to TestFlight.

Overall QA remains blocked by runtime capture and manual interaction checks. The remaining visual issue from the last verified capture is the footer below the body viewport; a source fix is implemented but unverified. No P0 or P1 design issues were found in that verified capture.

final result: blocked
