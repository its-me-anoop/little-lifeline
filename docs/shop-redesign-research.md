# Little Lifeline shop redesign — 4 October 2026

Target: the user-provided Gems & goals / Shop screenshot, on iPhone. Outcome: make purchases understandable, make free progress easy to find, and give the shop the same character as the clinic.

## Screen review

1. Opening the sheet: the clinic and balance remain visible, but the title is generic and a transient offline-earnings toast covers the title/tabs. Keep the header compact; put offline collection in its own appropriate surface.
2. Browsing gem packs: amounts and localized prices are visible. All four packs have identical artwork and almost identical emphasis. Use distinct gem illustrations, explicit purchase buttons, and a clear gem-unit label.
3. Comparing value: 450 gems is marked Best value. In the supplied INR screenshot, price per gem is approximately ₹1.24 (80), ₹1.11 (450), ₹1.00 (1,000), and ₹0.89 (2,800). The label is unsupported for the medium pack. Do not hard-code a value claim across localized storefronts; omit it unless calculated from actual numeric storefront prices.
4. Spending gems: unavailable coin packs and boosts are faded so much that their names and prices are hard to read. Preserve readable text and explain the shortfall. A purchase control can be unavailable while the offer remains legible.
5. Permanent upgrades: the second builder is buried at the bottom among temporary boosts. Describe the permanent benefit explicitly and separate it from consumables. Distinguish gem cost from cash price.

This is a review of one supplied screenshot and the corresponding Unity code. It does not verify screen-reader behavior, touch targets on-device, purchasing, or competitor shops through live gameplay.

## Competition and current evidence

- **Idle Miner Tycoon / Kolibri**: the publisher describes prominent boost illustrations, themed offer artwork, and consistent palettes. This is a documented, established pattern from 2020, not evidence that its current shop is unchanged. [Offer-art process](https://www.kolibrigames.com/blog/can-we-talk-about-cheese/), [economy design](https://www.kolibrigames.com/blog/economy-in-free-to-play-mobile-games-part-2/).
- **Hospital Empire Tycoon / Codigames**: official guidance tells players to inspect offer contents, makes clear that some rewards remain earnable without spending, and explains showing unmet upgrade requirements. Apply that clarity to benefits, payment types, and unavailable states. [Special offers](https://codigames.helpshift.com/hc/en/19-hospital-empire-tycoon/faq/536-are-there-special-accident-offers-in-app-purchases/), [first tips](https://codigames.helpshift.com/hc/en/19-hospital-empire-tycoon/faq/533-how-to-play-hospital-empire-tycoon-first-tips/).
- **Current industry context**: Unity's 2026 report describes daily rewards/missions and continued use of boosters, cosmetics, and currency purchases. Its survey covers the broader games industry, not idle games alone. [2026 report](https://unity.com/resources/gaming-report).
- **Optional value exchange**: Unity's June 2026 discussion of rewarded advertising emphasizes opt-in offers with tangible rewards. This supports showing a clear earning path, but does not justify adding an ad system to this visual redesign. [Value exchange](https://unity.com/blog/rewarded-ads-how-value-exchange-shapes-mobile-monetization).

## Design interpretation

Use benefit-led offers, original clinic-themed illustrations, generous but efficient spacing, readable prices and unavailable states, and separate free rewards from paid purchases. These are design recommendations inferred from the evidence, not measured conversion claims.

Preserve existing product IDs, gem quantities, economy, purchase confirmation behavior, restore purchases, and localized StoreKit prices. Avoid invented discounts, countdowns, popularity claims, new bundles, ads, or rewards. INR prices in concepts are samples from the supplied screenshot.

Three concepts explore a gem-led storefront, a permanent-upgrade-led layout, and a focused category shop. They are visual concepts pending selection, not implemented Unity screens.
