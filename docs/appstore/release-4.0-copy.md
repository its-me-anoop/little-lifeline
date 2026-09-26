# Little Lifeline 4.0: store copy draft

This is a draft for the 4.0 release, which adds gem packs, the first in-app purchases in this game. Do not copy it into `listing.json`, `review-notes.txt` or `privacy-policy.md` until the 4.0 build is selected for review. `listing.json` drives the What's New workflow for the version that is currently live.

## Replacements in the description

Replace the final paragraph:

> Play without an account or advertisements. The clinic simulation works offline. Optional gem packs finish construction early; every room, upgrade and clinic can be earned with coins alone. Little Lifeline is a fictional management game and does not provide medical advice.

Add this section after OPEN A SECOND CLINIC:

> GOALS AND GEMS
> Reach milestones as your clinic grows, from your first treatment to a fully staffed doctors clinic, and collect free gems for each one. Players updating from earlier versions can collect rewards for what they have already achieved. Spend gems to finish a renovation now instead of waiting.

## What's New in 4.0

> Goals and gems arrive at your clinic.
>
> Collect free gems for milestones you have reached, including ones from before this update. Use gems to finish renovations and construction early, or wait as before. Your clinic, coins and upgrades carry over unchanged.

## In-app purchases (App Store Connect)

| Product ID | Type | Reference name | Display name | Gems | Price (USD tier) |
|---|---|---|---|---|---|
| `com.flutterly.gravitile.gems.small` | Consumable | Gems 80 | Handful of Gems | 80 | 0.99 |
| `com.flutterly.gravitile.gems.medium` | Consumable | Gems 450 | Pouch of Gems | 450 | 4.99 |
| `com.flutterly.gravitile.gems.large` | Consumable | Gems 1000 | Chest of Gems | 1,000 | 9.99 |
| `com.flutterly.gravitile.gems.xl` | Consumable | Gems 2800 | Vault of Gems | 2,800 | 24.99 |
| `com.flutterly.gravitile.builder` | Non-consumable | Second Builder | Second Builder | — | 2.99 |

All five were created in App Store Connect on 2026-09-26 with en-US names and descriptions, prices, all 175 regions and review notes. Their Apple IDs are 6816446408 (small), 6816447805 (medium), 6816449165 (large), 6816451990 (xl) and 6816453112 (builder). They still need a review screenshot.

Each needs a localized description (for example "80 gems to finish construction early."), a review screenshot of the Gems & goals shop, and review notes. Attach all five to the 4.0 version. The Second Builder lets two rooms build or renovate at once; it is restorable and can also be unlocked for 400 gems earned in play. The amounts are defined in `ClinicGemPacks.cs` and must match these names. The local test catalogue is `Unity/OrbitOrchard/Assets/OrbitOrchard/Plugins/iOS/Configuration~/Clinic.storekit`.

## Review notes: replace the "Apple services" paragraph

> Version 4.0 adds optional consumable gem packs (four products listed above). Gems finish timed construction early and never expire. They are also awarded free for gameplay milestones, which appear under the gem counter next to the coin wallet. Every room, upgrade and clinic remains reachable with game coins alone; gems buy no exclusive items or income. To test: tap the gem counter to open Gems & goals, collect a milestone reward, then start a room renovation and choose Finish. Gems are stored in the on-device save with each purchase's transaction ID. A transaction is finished only after its gems are saved, and refunded purchases remove the unspent gems. Restore Purchases still synchronises the earlier non-consumable ownership. Consumable gems cannot be restored after the app is deleted, and the shop says that gems are saved on this device. There are no advertisements, subscriptions or random paid rewards.

## Privacy policy: replace the StoreKit paragraph

> The app uses Apple's StoreKit service to offer optional gem packs and to check previously verified purchase ownership. When you buy gems, Apple processes the payment and sends the app a transaction record. The app keeps each transaction's identifier in your on-device save so a purchase is applied once, and removes unspent gems if Apple reports a refund. Choosing **Restore purchases** asks Apple to synchronise eligible earlier purchases and may prompt for Apple Account authentication. Gems are part of your local save: deleting the app deletes them, and we cannot restore them from a developer account.

Update the effective date when it is published.

## Questionnaire and App Privacy

- **Age-rating questionnaire:** there are still no random paid rewards, no gambling and no advertising. In-app purchases are declared automatically.
- **App Privacy answers:** unchanged. The developer collects no data, and purchase processing is Apple's.
