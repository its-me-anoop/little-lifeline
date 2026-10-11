# Free progression and pacing: 4.3 (23) development

Measured, not estimated. Every number below comes from the engine-free Core simulation used by the game, driven by
scripted players in `Tests/Core/ClinicOpeningPacingTests.cs` and `Tests/Core/ClinicProgressionPacingTests.cs`. Simulated
play is not a device play-test; it shows what the rules allow, not how it feels on a phone.

## Opening (boss tutorial)

A scripted player answers each prompt after 1.5 seconds and taps nothing else. "Helps" also taps *Help the boss* every
0.6 seconds while the boss works.

| | 4.3 (22) | 4.3 (23), waits | 4.3 (23), helps |
|---|---|---|---|
| First patient arrives | 42.0 s | 29.0 s | 15.6 s |
| First coins in the wallet | 62.0 s | 49.0 s | 35.1 s |
| Clinic open (receptionist hired) | 115.0 s | 84.0 s | 60.5 s |
| Time with nothing to tap | 100.0 s | 69.0 s | 45.5 s |
| First choice of two affordable improvements | 115.0 s | 84.0 s | 60.5 s |

Change: the office's clean and set-up still show the rhythm (6 s and 8 s); later rooms repeat it in 4 s and 5 s (all
were 8 s and 10 s). *Help the boss* is optional: each tap takes 1.5 s off the current task and never skips it. Order,
boss, rooms and the check-in moment are unchanged. A save made mid-task with the old timers loads and finishes as saved.

Remaining passive time is mostly the boss's first check-in (about 17 s) and the first queue forming (about 11 s); both
are left as they are, because they are the opening's "watch it work" moments.

## Starter clinic, free player

The scripted free player collects every till and buys the cheapest open coin improvement whenever it can pay. It never
spends gems, has one builder and ignores goal coin rewards, so it is slightly slower than a real free player.

**Continuous play:** first renovation 0.1 h · clinic level 3 0.3 h · level 5 0.9 h · level 10 4.5 h · level 15 17.7 h ·
level 20 66.3 h · starter complete 78.6 h (760 purchases).

**Three 10-minute sessions a day** (8:00, 13:00, 20:00; offline earnings capped at 8 h and the till limit between them):
level 5 day 1 · level 10 day 4 · level 15 day 11 · level 20 day 35 · starter complete day 42 (21 active hours).

Purchases per session: day 1 35/92/59 · day 2 39/25/29 · day 3 22/13/24 · day 7 1/10/14 · day 14 1/10/2 · day 30 1/9/1.

### Where free players wait

Longest wait for the next affordable improvement in continuous play: first hour 1.0 min · hours 1–10 43.5 min · after
10 h 228 min. Of the 78.6 hours, 50 waits are longer than 15 minutes and add up to 72.9 hours:

| Cause (continuous play, waits over 15 min) | Total |
|---|---|
| Nothing to buy while a renovation or office upgrade builds (one builder, 2-hour cap; the next room's new equipment arrives with it) | 56.6 h |
| Saving for an improvement that is already open | 15.8 h |
| Not attributed (both checks false at the 30-second sample) | 0.5 h |

Over the whole run, 58.3 hours pass with nothing purchasable while the builder works and 17.9 hours saving. The two
longest saving waits are 228 min before *Renovate first aid to room 20* and 164 min before *Upgrade the office to clinic
level 20*; most other long waits are a single 120-minute build.

In session play most of that building happens while the player is away, so the cost shows up as short morning and
evening sessions with one purchase each from the second week.

### What changed for free players in 4.3 (23)

- **Clear next step instead of an unexplained wait.** When the guide has no lesson left, or during the long "open the
  doctors clinic" lesson, the card names the improvement being saved for, the coins held against its price and an
  estimate from the measured earning rate, or what the builder is finishing and when.
- **Every purchase says what it changed** (for example *Treatment 12.4s → 11.6s*) and the next step with its price.
- **Daily goals can always be met.** A spending goal is only set when enough improvements remain. A fully upgraded
  starter clinic previously received an unachievable spending goal on 249 of 365 days (so the all-goals bonus was out
  of reach as well); now on none. Goals are fixed for the day, so upgrades cannot move them.
- **Optional gem lessons no longer block the free guide.**

### Balance left unchanged, deliberately

Prices, income, build times and the offline limit are unchanged: existing saves keep their prices, and the frozen-formula
tests still hold. The measurements point to one lever for the owner to decide: the late 2-hour build cap with a
single builder is the main source of "nothing to do" time. Options, none applied: a lower cap for late starter tiers
under a new rules version (with migration), or letting the next room's equipment pieces be bought while it is being
built. Neither would touch the gem or App Store offers.

### Free gems

From daily play: login streak average 12.9 + daily goals 11.4 + all-goals bonus 10 ≈ **34 gems a day**. One-time:
5 at start, 265 from the guide, 1,200 from achievements. Finish-now prices: 2 min 4 gems, 10 min 12, 30 min 26, 1 h 44,
2 h 73; the last minute is free.

## Reproduce

Unity: run the EditMode tests `ClinicOpeningPacingTests` and `ClinicProgressionPacingTests`; the numbers are written to
the test log. Both are deterministic.
