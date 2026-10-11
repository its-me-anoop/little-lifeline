using System;
using System.IO;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>Every gem spend shows its amount and benefit and waits for an explicit confirmation. Cancelling, a second tap,
    /// switching offers, a price rise, work that finished meanwhile, too few gems or a failed save never debit a gem.</summary>
    public sealed class ClinicGemSpendTests
    {
        private string directory, path;
        private DateTimeOffset now;
        private ClinicProfileStore store;
        private ClinicGemSpendFlow flow;
        private long start;

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-spend-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            now = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
            store = new ClinicProfileStore(directory); store.LoadClinic(now);
            ClinicGuideSkipTests.FinishOpening(new ClinicSimulation(store.Profile.state));
            ClinicSimulation.TryGrantReward(store.Profile.state, 500000);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(store.GrantPurchase("txn-test-1", 1000, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            start = store.Profile.premium.gems;
            flow = new ClinicGemSpendFlow((kind, key) => store.QuoteGemSpend(kind, key, now), (quote, max) => store.SpendGemsFor(quote.Kind, quote.Key, max, now, quote.Location));
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        /// <summary>Grow the clinic through ordinary office upgrades and renovations until <paramref name="room"/> can be renovated
        /// from <paramref name="fromTier"/>, then start that renovation. Early renovations finish inside the free minute.</summary>
        private int StartRenovation(ClinicRoom room, int fromTier = 5)
        {
            var sim = new ClinicSimulation(store.Profile.state);
            for (var guard = 0; guard < 50 && sim.State.Room(room).Tier < fromTier; guard++)
            {
                foreach (var each in sim.State.Rooms.ToList())
                    while (each.Tier < sim.State.OfficeLevel)
                    {
                        Assert.That(sim.Renovate(each.Kind).Success, Is.True, each.Kind + " to " + (each.Tier + 1));
                        sim.Advance(sim.State.Construction.Single().EndsTick / 10d, false);
                    }
                if (sim.State.Room(room).Tier >= fromTier) break;
                Assert.That(sim.UpgradeOffice().Success, Is.True);
                sim.Advance(sim.State.OfficeUpgradeEndsTick / 10d, false);
            }
            if (sim.State.OfficeLevel <= sim.State.Room(room).Tier)
            {
                foreach (var each in sim.State.Rooms.ToList())
                    while (each.Tier < sim.State.OfficeLevel) { sim.Renovate(each.Kind); sim.Advance(sim.State.Construction.Single().EndsTick / 10d, false); }
                Assert.That(sim.UpgradeOffice().Success, Is.True);
                sim.Advance(sim.State.OfficeUpgradeEndsTick / 10d, false);
            }
            Assert.That(sim.Renovate(room).Success, Is.True);
            Assert.That(ClinicPremiumRules.SkipCost(sim.State, sim.State.Construction.Single()), Is.GreaterThan(0), "This renovation outlasts the free minute.");
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            return sim.State.Construction.Single().Id;
        }

        [Test] public void EveryGemSpendIsQuotedWithItsAmountBenefitAndHowLongItLasts()
        {
            var job = StartRenovation(ClinicRoom.Reception);
            var quotes = new[]
            {
                store.QuoteGemSpend(ClinicGemSpendKind.CoinPack, ClinicShopOffers.CoinPacks[0].Id, now),
                store.QuoteGemSpend(ClinicGemSpendKind.Boost, ClinicShopOffers.Boosts[0].Id, now),
                store.QuoteGemSpend(ClinicGemSpendKind.OfflineUpgrade, ClinicShopOffers.OfflineTiers[0].Id, now),
                store.QuoteGemSpend(ClinicGemSpendKind.ExtraBuilder, ClinicUnlocks.ExtraBuilder, now),
                store.QuoteGemSpend(ClinicGemSpendKind.Decoration, ClinicRoom.FirstAid.ToString(), now),
                store.QuoteGemSpend(ClinicGemSpendKind.FinishWork, job.ToString(), now),
            };
            foreach (var quote in quotes)
            {
                Assert.That(quote.Available, Is.True, quote.Kind + ": " + quote.Unavailable);
                Assert.That(quote.Gems, Is.GreaterThan(0), quote.Kind.ToString());
                Assert.That(quote.Title, Is.Not.Empty); Assert.That(quote.Benefit, Is.Not.Empty); Assert.That(quote.Duration, Is.Not.Empty);
                Assert.That(quote.Balance, Is.EqualTo(start));
            }
            Assert.That(quotes[0].Benefit, Does.Contain(store.CoinPackAmount(ClinicShopOffers.CoinPacks[0]).ToString("N0", System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(quotes[1].Duration, Does.Contain("1 hour"));
            Assert.That(quotes[2].Duration, Does.Contain("Permanent"));
            Assert.That(quotes[3].Duration, Does.Contain("Permanent"));
            Assert.That(quotes[4].Duration, Does.Contain("Permanent"));
            Assert.That(quotes[5].Gems, Is.EqualTo(ClinicPremiumRules.SkipCost(store.Profile.state, store.Profile.state.Construction.Single())));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start), "Quoting spends nothing.");
        }

        [Test] public void NothingIsSpentUntilConfirmedAndCancellingSpendsNothing()
        {
            var quote = flow.Begin(ClinicGemSpendKind.Boost, "boost.1h");
            var token = flow.Token;
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start));
            Assert.That(store.BoostActive(now), Is.False);
            flow.Cancel();
            Assert.That(flow.Pending, Is.Null);
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Stale), "A cancelled confirmation cannot be replayed.");
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start));
            Assert.That(store.BoostActive(now), Is.False);
            Assert.That(quote.Gems, Is.EqualTo(30));
        }

        [Test] public void RapidRepeatedConfirmationsSpendExactlyOnce()
        {
            flow.Begin(ClinicGemSpendKind.CoinPack, "coins.bag");
            var token = flow.Token; var wallet = store.Profile.state.Wallet;
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            for (var tap = 0; tap < 5; tap++) Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Stale));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start - 20));
            Assert.That(store.Profile.state.Wallet, Is.EqualTo(wallet + store.CoinPackAmount(ClinicShopOffers.CoinPacks[0])));
        }

        [Test] public void SwitchingOffersInvalidatesTheEarlierConfirmation()
        {
            flow.Begin(ClinicGemSpendKind.Boost, "boost.4h"); var first = flow.Token;
            flow.Begin(ClinicGemSpendKind.CoinPack, "coins.bag"); var second = flow.Token;
            Assert.That(flow.Confirm(first, out _), Is.EqualTo(ClinicGemSpendOutcome.Stale));
            Assert.That(store.BoostActive(now), Is.False, "The offer the player moved away from is never bought.");
            Assert.That(flow.Confirm(second, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start - 20));
        }

        [Test] public void APriceRiseNeedsAFreshConfirmationAtTheNewPrice()
        {
            var quoted = flow.Begin(ClinicGemSpendKind.Decoration, "Reception").Gems;
            var token = flow.Token;
            Assert.That(new ClinicSimulation(store.Profile.state).Decorate(ClinicRoom.Reception).Success, Is.True, "Decor bought elsewhere raises the next price.");
            Assert.That(flow.Confirm(token, out var updated), Is.EqualTo(ClinicGemSpendOutcome.PriceChanged));
            Assert.That(updated.Gems, Is.GreaterThan(quoted));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start), "Nothing is spent at a price the player did not see.");
            Assert.That(flow.Confirm(flow.Token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start - updated.Gems));
        }

        [Test] public void AFallingFinishPriceChargesTheLowerAmountAndRefreshesTheSheet()
        {
            var job = StartRenovation(ClinicRoom.Reception);
            var quoted = flow.Begin(ClinicGemSpendKind.FinishWork, job.ToString()).Gems;
            var token = flow.Token;
            var sim = new ClinicSimulation(store.Profile.state);
            var construction = sim.State.Construction.Single();
            sim.Advance((construction.EndsTick - construction.StartedTick) / 20d, false);
            var lower = ClinicPremiumRules.SkipCost(sim.State, construction);
            Assert.That(lower, Is.LessThan(quoted));
            Assert.That(flow.Refresh(), Is.EqualTo(ClinicGemSpendRefresh.PriceFell));
            Assert.That(flow.Pending.Gems, Is.EqualTo(lower));
            Assert.That(flow.Token, Is.EqualTo(token), "A lower price keeps the player's confirmation valid.");
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            Assert.That(store.Profile.premium.gemsSpent, Is.EqualTo(lower));
            Assert.That(store.Profile.guide.workFinishedEarly, Is.EqualTo(1));
        }

        [Test] public void WorkThatFinishesWhileConfirmingCostsNothing()
        {
            var job = StartRenovation(ClinicRoom.Reception);
            flow.Begin(ClinicGemSpendKind.FinishWork, job.ToString()); var token = flow.Token;
            var sim = new ClinicSimulation(store.Profile.state);
            sim.Advance(sim.State.Construction.Single().EndsTick / 10d, false);
            Assert.That(sim.State.Construction, Is.Empty);
            Assert.That(flow.Refresh(), Is.EqualTo(ClinicGemSpendRefresh.NoLongerAvailable));
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Stale));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start));
        }

        [Test] public void TooFewGemsShowsTheShortfallAndNeverSpends()
        {
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.ExtraBuilder, ClinicUnlocks.ExtraBuilder, 400, now), Is.True, store.Error);
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.OfflineUpgrade, "offline.24h", 400, now), Is.True, store.Error);
            var balance = store.Profile.premium.gems;
            Assert.That(balance, Is.EqualTo(start - 800));
            var quote = flow.Begin(ClinicGemSpendKind.OfflineUpgrade, "offline.12h");
            Assert.That(quote.Available, Is.False, "Already covered by the longer upgrade.");
            var vault = flow.Begin(ClinicGemSpendKind.CoinPack, "coins.vault");
            Assert.That(vault.Shortfall, Is.Zero);
            Assert.That(flow.Confirm(flow.Token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent));
            var chest = flow.Begin(ClinicGemSpendKind.CoinPack, "coins.chest");
            Assert.That(chest.Shortfall, Is.EqualTo(60 - (start - 950)));
            Assert.That(flow.Confirm(flow.Token, out _), Is.EqualTo(ClinicGemSpendOutcome.NotEnoughGems));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start - 950));
        }

        [Test] public void AFailedSaveSpendsNothingAndTheSameConfirmationCanBeRetried()
        {
            flow.Begin(ClinicGemSpendKind.Boost, "boost.1h"); var token = flow.Token;
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.NotSaved));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start));
            Assert.That(store.BoostActive(now), Is.False);
            Assert.That(flow.Pending, Is.Not.Null, "The sheet stays open so the player can retry.");
            Directory.Delete(path + ".tmp");
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start - 30));
            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            Assert.That(reloaded.Profile.premium.gems, Is.EqualTo(start - 30), "The spend is durable.");
        }

        [Test] public void EveryStorePathRefusesAPriceAboveTheConfirmedMaximum()
        {
            var job = StartRenovation(ClinicRoom.Reception);
            var cases = new (ClinicGemSpendKind kind, string key)[]
            {
                (ClinicGemSpendKind.CoinPack, "coins.bag"), (ClinicGemSpendKind.Boost, "boost.1h"), (ClinicGemSpendKind.OfflineUpgrade, "offline.12h"),
                (ClinicGemSpendKind.ExtraBuilder, ClinicUnlocks.ExtraBuilder), (ClinicGemSpendKind.Decoration, "Reception"), (ClinicGemSpendKind.FinishWork, job.ToString()),
            };
            foreach (var (kind, key) in cases)
            {
                var price = store.QuoteGemSpend(kind, key, now).Gems;
                Assert.That(store.SpendGemsFor(kind, key, price - 1, now), Is.False, kind.ToString());
                Assert.That(store.Profile.premium.gems, Is.EqualTo(start), kind + " spent above its confirmed price.");
            }
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.CoinPack, "coins.unknown", start, now), Is.False);
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.FinishWork, "not-a-job", start, now), Is.False);
        }

        [Test] public void AnOpenBoostSheetKeepsItsTimeLeftCurrentWithoutAskingAgain()
        {
            // Review 8c9d3fa: same price and balance, but the shown time already left changes as the clock runs.
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.Boost, "boost.1h", 30, now), Is.True, store.Error);
            var opened = flow.Begin(ClinicGemSpendKind.Boost, "boost.1h"); var token = flow.Token;
            Assert.That(opened.Duration, Does.Contain("1h already left"));
            now = now.AddMinutes(2);
            Assert.That(flow.Refresh(), Is.EqualTo(ClinicGemSpendRefresh.DetailsChanged));
            Assert.That(flow.Pending.Duration, Does.Contain("58m already left"));
            Assert.That(flow.Token, Is.EqualTo(token), "Nothing the player agreed to got worse, so the confirmation stays valid.");
            Assert.That(flow.Refresh(), Is.EqualTo(ClinicGemSpendRefresh.Unchanged));
            Assert.That(flow.Confirm(token, out _), Is.EqualTo(ClinicGemSpendOutcome.Spent), store.Error);
            Assert.That(store.BoostRemaining(now).TotalMinutes, Is.EqualTo(118).Within(.01));
        }

        [Test] public void DecorStatesTheExactFeeAndCoinsAddedNotAPercentage()
        {
            var quote = store.QuoteGemSpend(ClinicGemSpendKind.Decoration, "Reception", now);
            var before = ClinicRules.VisitFee(store.Profile.state);
            var after = ClinicStats.VisitFeeAfterDecor(store.Profile.state, ClinicRoom.Reception);
            Assert.That(ClinicRules.VisitFee(store.Profile.state), Is.EqualTo(before), "Previewing changes nothing.");
            Assert.That(quote.Benefit, Is.EqualTo("Visit fee " + before + " → " + after + " coins (+" + (after - before) + (after - before == 1 ? " coin" : " coins") + " a visit)"));
            Assert.That(quote.Benefit, Does.Not.Contain("%"));
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.Decoration, "Reception", quote.Gems, now), Is.True, store.Error);
            Assert.That(ClinicRules.VisitFee(store.Profile.state), Is.EqualTo(after), "The preview was exact.");
        }

        [Test] public void AQuoteOnlyEverSpendsInTheClinicItWasMadeIn()
        {
            // Construction ids repeat between clinics, so a quote is bound to the clinic it was made in.
            var job = StartRenovation(ClinicRoom.Reception);
            var quoted = store.QuoteGemSpend(ClinicGemSpendKind.FinishWork, job.ToString(), now);
            Assert.That(quoted.Location, Is.EqualTo(ClinicLocation.StarterClinic));
            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.FinishWork, job.ToString(), quoted.Gems, now, ClinicLocation.DoctorsClinic), Is.False,
                "A confirmation made in the other clinic never spends here.");
            Assert.That(store.Error, Does.Contain("another clinic"));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(start));
            Assert.That(store.Profile.state.Construction.Count, Is.EqualTo(1));

            // The flow refuses when the player has moved between opening the sheet and confirming it.
            var location = ClinicLocation.StarterClinic; var spent = 0;
            var moving = new ClinicGemSpendFlow((kind, key) => new ClinicGemQuote { Kind = kind, Key = key, Title = "T", Benefit = "B", Duration = "D", Gems = 10, Balance = 100, Available = true, Location = location },
                (quote, max) => { spent++; return true; });
            moving.Begin(ClinicGemSpendKind.FinishWork, "0"); var token = moving.Token;
            location = ClinicLocation.DoctorsClinic;
            Assert.That(moving.Confirm(token, out var current), Is.EqualTo(ClinicGemSpendOutcome.Unavailable));
            Assert.That(current.Unavailable, Does.Contain("another clinic"));
            Assert.That(spent, Is.Zero);
            moving.Begin(ClinicGemSpendKind.FinishWork, "0");
            location = ClinicLocation.StarterClinic;
            Assert.That(moving.Refresh(), Is.EqualTo(ClinicGemSpendRefresh.NoLongerAvailable), "An open sheet closes when the clinic changes.");
        }

        [Test] public void TheScreenReaderSummaryReadsEveryFactTheSheetShows()
        {
            var quote = store.QuoteGemSpend(ClinicGemSpendKind.OfflineUpgrade, "offline.12h", now);
            foreach (var part in new[] { quote.Title, quote.Benefit, quote.Duration, quote.Detail, "Costs 150 gems", "leaving " + (start - 150).ToString("N0", System.Globalization.CultureInfo.InvariantCulture) })
                Assert.That(quote.Summary, Does.Contain(part));
            var poor = new ClinicGemQuote { Title = "T", Benefit = "B", Duration = "D", Gems = 90, Balance = 5 };
            Assert.That(poor.Summary, Does.Contain("need 85 gems more"));
        }

        [Test] public void OnlyAWellFormedSeparateIdentityCanReplaceTheAppStoreOneForQa()
        {
            Assert.That(ClinicQaIdentity.Validate(null), Is.Null);
            Assert.That(ClinicQaIdentity.Validate(""), Is.Null);
            Assert.That(ClinicQaIdentity.Validate(ClinicQaIdentity.Production), Is.Null);
            Assert.That(ClinicQaIdentity.Validate("com.example.other"), Is.Null);
            Assert.That(ClinicQaIdentity.Validate("com.flutterly.gravitile.qa;rm"), Is.Null);
            Assert.That(ClinicQaIdentity.Validate("com.flutterly.gravitile.qa"), Is.EqualTo("com.flutterly.gravitile.qa"));
            Assert.That(ClinicQaIdentity.Validate("com.flutterly.gravitile.review2"), Is.EqualTo("com.flutterly.gravitile.review2"));
        }

        [Test] public void FreeGemRoutesAreCountedHonestly()
        {
            var day = store.DailyGoals(now);
            var login = ClinicDaily.StreakReward(store.NextStreakDay(now));
            var ready = store.FreeGemsReady(now);
            Assert.That(ready, Is.GreaterThanOrEqualTo(login), "Today's login reward is free and ready.");
            Assert.That(store.FreeGemsLeftToday(now), Is.EqualTo(day.Sum(g => g.GemReward) + ClinicDaily.AllGoalsGemBonus), "Nothing done yet today.");
            Assert.That(store.ClaimLoginReward(now), Is.True, store.Error);
            Assert.That(store.FreeGemsReady(now), Is.EqualTo(ready - login));
            Assert.That(store.QuoteGemSpend(ClinicGemSpendKind.Boost, "boost.1h", now).Balance, Is.EqualTo(start + login), "Free gems count toward every offer.");
        }

        [Test] public void FreeLastMinuteFinishesNeedNoConfirmation()
        {
            var job = StartRenovation(ClinicRoom.Reception, 5);
            var sim = new ClinicSimulation(store.Profile.state);
            var construction = sim.State.Construction.Single();
            sim.Advance(Math.Max(0, (construction.EndsTick - sim.State.Tick) / 10d - 30), false);
            var quote = store.QuoteGemSpend(ClinicGemSpendKind.FinishWork, job.ToString(), now);
            Assert.That(quote.Gems, Is.Zero);
            Assert.That(quote.NeedsConfirmation, Is.False, "Nothing is spent, so nothing needs confirming.");
            Assert.That(store.QuoteGemSpend(ClinicGemSpendKind.Boost, "boost.1h", now).NeedsConfirmation, Is.True);
        }
    }
}
