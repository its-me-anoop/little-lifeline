using System;
using System.Collections.Generic;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Services;
using OrbitOrchard.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>Gems, goals and the gem shop. Every gem change commits through the profile store first.</summary>
    public sealed partial class ClinicApp
    {
        private static readonly Color GemInk = new Color(.62f, .21f, .35f);
        private const int UpcomingGoals = 3;
        private bool gemsOpen;
        private Label gemLabel, gemBadge;
        private int claimableGoals;
        private readonly Queue<AppleGemTransaction> pendingGems = new Queue<AppleGemTransaction>();

        private void BuildGemControl(VisualElement parent)
        {
            var button = IconButton(parent, ClinicGlyph.Gem, "Gems and goals", ToggleGems, "gem-balance",
                () => profile.premium.gems + " gems" + (claimableGoals > 0 ? ", " + claimableGoals + " rewards ready" : ""));
            button.name = "clinic-gems";
            button.Q<ClinicIcon>().Tint = GemInk;
            gemLabel = Text(button, "0", "gem-value", true);
            gemBadge = Text(button, "", "gem-badge", true);
        }

        private void UpdateGemReadout()
        {
            if (gemLabel == null) return;
            saves.EnsureDay(DateTimeOffset.UtcNow);
            if (pendingConfirm != null && Time.unscaledTimeAsDouble >= pendingConfirmUntil) { pendingConfirm = null; dockKey = ""; }
            claimableGoals = saves.ClaimableMilestones().Count() + saves.DailyReady(DateTimeOffset.UtcNow);
            gemLabel.text = Money(profile.premium.gems);
            gemBadge.text = claimableGoals.ToString();
            gemBadge.style.display = claimableGoals > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private string GemDockKey() => gemTab + ":" + pendingConfirm + ":" + profile.daily?.claimed.Count + ":" + profile.daily?.loginDay + ":" + saves.BoostActive(DateTimeOffset.UtcNow) + ":" + saves.OfflineLimitHours + ":"
            + profile.premium.gems + ":" + claimableGoals + ":" + profile.premium.milestonesClaimed.Count
            + ":" + (apple?.Products.Count(p => ClinicGemPacks.IsGemPack(p.id)) ?? 0) + ":" + (apple?.IsPurchasing ?? false);

        private void ToggleGems()
        {
            var open = !gemsOpen;
            CloseContext();
            gemsOpen = open;
            dockKey = ""; UpdateReadouts();
        }

        private enum GemTab { Today, Goals, Shop }
        private GemTab gemTab = GemTab.Today;
        private string pendingConfirm;
        private double pendingConfirmUntil;

        private void BuildGemsDock()
        {
            var tabs = Box(dock, "gem-tabs");
            foreach (GemTab tab in Enum.GetValues(typeof(GemTab)))
            {
                var chosen = tab;
                var button = new Button(() => { gemTab = chosen; pendingConfirm = null; dockKey = ""; UpdateReadouts(); }) { text = tab.ToString(), name = "gem-tab-" + tab.ToString().ToLowerInvariant() };
                button.AddToClassList("gem-tab"); button.EnableInClassList("gem-tab-selected", tab == gemTab);
                var ready = tab == GemTab.Today ? saves.DailyReady(DateTimeOffset.UtcNow) : tab == GemTab.Goals ? saves.ClaimableMilestones().Count() : 0;
                if (ready > 0) Text(button, ready.ToString(), "tab-badge", true);
                tabs.Add(button);
                RegisterAccessibleButton(button, tab + (ready > 0 ? ", " + ready + " ready" : ""), () => { gemTab = chosen; dockKey = ""; UpdateReadouts(); });
            }
            var content = new ScrollView(ScrollViewMode.Vertical) { name = "clinic-gems-content", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Hidden };
            content.AddToClassList("bounded-dock-content"); dock.Add(content);
            BindTouchCaptureLifecycle(content.contentContainer); BindTouchCaptureLifecycle(content.contentViewport);
            if (gemTab == GemTab.Today) BuildTodayTab(content);
            else if (gemTab == GemTab.Goals) BuildGoalsTab(content);
            else BuildShopTab(content);
        }

        private void BuildTodayTab(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            Text(content, "Daily streak", "gem-section");
            var streak = saves.NextStreakDay(now); var ready = saves.LoginRewardReady(now);
            var row = Box(content, "streak-row");
            for (var day = 1; day <= 7; day++)
            {
                var done = day < streak || day == streak && !ready;
                var dot = Box(row, "streak-day"); dot.EnableInClassList("streak-done", done); dot.EnableInClassList("streak-next", day == streak && ready);
                Text(dot, day.ToString(), "streak-number", true);
                Text(dot, "+" + ClinicDaily.StreakReward(day), "streak-gems");
            }
            if (ready)
            {
                var claim = IconButton(content, ClinicGlyph.Gem, "Collect day " + streak + " reward", ClaimLogin, "gem-goal gem-goal-ready");
                Text(claim, "Day " + streak + " reward", "gem-goal-title");
                GemAmount(claim, "Collect +" + ClinicDaily.StreakReward(streak));
            }
            else Text(content, "Come back tomorrow to keep your streak going.", "gem-note");

            Text(content, "Today's goals", "gem-section");
            foreach (var goal in saves.DailyGoals(now))
            {
                var id = goal.Id; var claimed = saves.IsDailyClaimed(id); var done = saves.IsDailyDone(goal);
                var progress = Math.Min(goal.Target, saves.DailyProgress(goal));
                VisualElement row2;
                if (done && !claimed) { var b = IconButton(content, ClinicGlyph.Goal, "Collect " + ClinicDaily.Describe(goal), () => ClaimDaily(id), "gem-goal gem-goal-ready"); row2 = b; }
                else { row2 = Box(content, "gem-goal"); row2.Add(new ClinicIcon(claimed ? ClinicGlyph.Check : ClinicGlyph.Goal, 22)); }
                var words = Box(row2, "guide-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, ClinicDaily.Describe(goal), "gem-goal-title");
                var bar = Box(words, "daily-bar"); var fill = Box(bar, "daily-fill");
                fill.style.width = Length.Percent(100f * progress / Math.Max(1, goal.Target));
                Text(words, claimed ? "Collected" : progress.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " / " + goal.Target.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "gem-note");
                GemAmount(row2, (done && !claimed ? "Collect " : "") + "+" + goal.GemReward + " · " + Money(goal.CoinReward) + " coins");
            }
            Text(content, "Finish all three for a bonus " + ClinicDaily.AllGoalsGemBonus + " gems. New goals every day.", "gem-note");
        }

        private void BuildGoalsTab(VisualElement content)
        {
            var claimed = profile.premium.milestonesClaimed;
            var readyGoals = saves.ClaimableMilestones().ToList();
            foreach (var goal in readyGoals)
            {
                var id = goal.Id;
                var button = IconButton(content, ClinicGlyph.Goal, "Collect " + goal.GemReward + " gems for " + goal.Title, () => ClaimGoal(id), "gem-goal gem-goal-ready");
                Text(button, goal.Title, "gem-goal-title");
                GemAmount(button, "Collect +" + goal.GemReward);
            }
            var upcoming = ClinicMilestones.All.Where(m => !claimed.Contains(m.Id) && !readyGoals.Contains(m)).Take(UpcomingGoals + 2).ToList();
            foreach (var goal in upcoming)
            {
                var row = Box(content, "gem-goal");
                row.Add(new ClinicIcon(ClinicGlyph.Goal, 22));
                Text(row, goal.Title, "gem-goal-title");
                GemAmount(row, "+" + goal.GemReward);
            }
            if (readyGoals.Count == 0 && upcoming.Count == 0) Text(content, "Every goal is complete.", "gem-note");
        }

        private void BuildShopTab(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            Text(content, "Gems", "gem-section");
            var packs = apple == null ? new List<AppleProduct>() : apple.Products.Where(p => ClinicGemPacks.IsGemPack(p.id)).OrderBy(p => ClinicGemPacks.GemsFor(p.id)).ToList();
            if (packs.Count == 0)
            {
                Text(content, apple == null || string.IsNullOrEmpty(apple.StoreStatus) ? "Gem packs are loading from the App Store." : apple.StoreStatus, "gem-note");
                if (apple != null) apple.LoadProducts();
            }
            else
            {
                var shelf = Box(content, "gem-shelf");
                foreach (var pack in packs)
                {
                    var product = pack.id; var gems = ClinicGemPacks.GemsFor(product);
                    var button = IconButton(shelf, ClinicGlyph.Gem, "Buy " + gems + " gems for " + pack.price, () => BuyGems(product), "gem-pack");
                    button.Q<ClinicIcon>().Tint = GemInk;
                    if (product == ClinicGemPacks.Medium) Text(button, "Best value", "best-value", true);
                    Text(button, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "gem-pack-amount", true);
                    Text(button, pack.price, "gem-pack-price");
                    button.SetEnabled(!apple.IsPurchasing);
                }
            }

            Text(content, "Coins", "gem-section");
            var coins = Box(content, "gem-shelf");
            foreach (var pack in ClinicShopOffers.CoinPacks)
            {
                var offer = pack; var amount = saves.CoinPackAmount(pack);
                GemOffer(coins, offer.Id, ClinicGlyph.Coin, Money(amount) + " coins", offer.Gems, () => saves.BuyCoinPack(offer.Id, DateTimeOffset.UtcNow),
                    "+" + Money(amount) + " coins");
            }

            Text(content, "Boosts", "gem-section");
            if (saves.BoostActive(now)) Text(content, "Double collections active · " + TimeLabel(saves.BoostRemaining(now).TotalSeconds) + " left. Every collection pays twice.", "gem-note");
            var boosts = Box(content, "gem-shelf");
            foreach (var boost in ClinicShopOffers.Boosts)
            {
                var offer = boost;
                GemOffer(boosts, offer.Id, ClinicGlyph.Upgrade, offer.Name, offer.Gems, () => saves.BuyBoost(offer.Id, DateTimeOffset.UtcNow), "Double collections for " + offer.Hours + (offer.Hours == 1 ? " hour" : " hours"));
            }

            Text(content, "Upgrades", "gem-section");
            Text(content, "Offline: up to " + saves.OfflineLimitHours + " hours, and the tills hold " + Money(saves.OfflineCoinCap) + " coins while you're away.", "gem-note");
            var upgrades = Box(content, "gem-shelf");
            foreach (var tier in ClinicShopOffers.OfflineTiers)
            {
                if (saves.OfflineLimitHours >= tier.Hours) continue;
                var offer = tier;
                GemOffer(upgrades, offer.Id, ClinicGlyph.Clock, offer.Name, offer.Gems, () => saves.BuyOfflineUpgrade(offer.Id, DateTimeOffset.UtcNow), offer.Name + " unlocked");
                break;
            }
            BuildBuilderSection(content);
            Text(content, "Gems never expire and are saved with this clinic on this device.", "gem-note");
        }

        /// <summary>A gem purchase button. Spends of 50 gems or more ask for a second tap before anything is spent.</summary>
        private void GemOffer(VisualElement shelf, string id, ClinicGlyph glyph, string title, long gems, Func<bool> buy, string done)
        {
            var confirming = pendingConfirm == id && Time.unscaledTimeAsDouble < pendingConfirmUntil;
            var button = IconButton(shelf, glyph, title + " for " + gems + " gems", () =>
            {
                if (gems >= ClinicShopOffers.ConfirmAtGems && !(pendingConfirm == id && Time.unscaledTimeAsDouble < pendingConfirmUntil))
                { pendingConfirm = id; pendingConfirmUntil = Time.unscaledTimeAsDouble + 4; dockKey = ""; UpdateReadouts(); return; }
                pendingConfirm = null;
                if (!buy()) { Notify(saves.Error ?? "That could not be bought.", 6); dockKey = ""; UpdateReadouts(); return; }
                RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1); Notify(done, 4);
            }, "gem-pack" + (confirming ? " gem-confirm" : ""));
            if (glyph == ClinicGlyph.Gem) button.Q<ClinicIcon>().Tint = GemInk;
            Text(button, confirming ? "Tap again to confirm" : title, "gem-pack-amount gem-offer-title", true);
            GemAmount(button, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
            button.SetEnabled(profile.premium.gems >= gems);
        }

        private void ClaimLogin()
        {
            var streak = saves.NextStreakDay(DateTimeOffset.UtcNow);
            if (!saves.ClaimLoginReward(DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "The reward could not be collected.", 6); return; }
            RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1);
            LaunchGemsFrom(overlay.WorldToLocal(dock.worldBound.center), 6);
            Notify("Day " + streak + " reward collected", 4);
        }

        private void ClaimDaily(string id)
        {
            if (!saves.ClaimDailyGoal(id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "The reward could not be collected.", 6); return; }
            RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1);
            LaunchGemsFrom(overlay.WorldToLocal(dock.worldBound.center), 5);
            LaunchCoinsFrom(world.GetRoomPoint(ClinicRoom.Reception));
            Notify("Daily goal collected", 3);
        }

        private void BuildBuilderSection(VisualElement content)
        {
            Text(content, "Builders", "gem-section");
            if (saves.HasUnlock(ClinicUnlocks.ExtraBuilder))
            {
                Text(content, "Two builders: two rooms can build or renovate at once.", "gem-note");
                return;
            }
            Text(content, "One builder works on one room at a time. A second builder lets two rooms build at once, in both clinics.", "gem-note");
            var shelf = Box(content, "gem-shelf");
            var product = apple?.Products.FirstOrDefault(p => p.id == ClinicUnlocks.ExtraBuilderProduct);
            if (product != null)
            {
                var buy = IconButton(shelf, ClinicGlyph.Upgrade, "Buy a second builder for " + product.price, () => BuyGems(product.id), "gem-pack builder-pack");
                Text(buy, "Second builder", "gem-pack-amount", true);
                Text(buy, product.price + " · keep forever", "gem-pack-price");
                buy.SetEnabled(!apple.IsPurchasing);
            }
            GemOffer(shelf, ClinicUnlocks.ExtraBuilder, ClinicGlyph.Upgrade, "Second builder", ClinicUnlocks.ExtraBuilderGems,
                () => saves.UnlockExtraBuilderWithGems(DateTimeOffset.UtcNow), "Your second builder is ready. Two rooms can now build at once.");
        }

        /// <summary>An App Store unlock the player owns is copied into the save so it also works offline.</summary>
        private void CheckPurchasedUnlocks()
        {
            if (!ready || saves == null || apple == null || saves.HasPendingOfflineProgress) return;
            if (!apple.Owns(ClinicUnlocks.ExtraBuilderProduct) || saves.HasUnlock(ClinicUnlocks.ExtraBuilder)) return;
            if (!saves.RecordPurchasedUnlock(ClinicUnlocks.ExtraBuilder, DateTimeOffset.UtcNow)) return;
            RebindAfterCommit();
            clinicAudio.PlayReward(); Feedback(1);
            Notify("Your second builder is ready. Two rooms can now build at once.", 5);
        }

        private void GemAmount(VisualElement parent, string text)
        {
            var amount = Box(parent, "gem-amount");
            amount.Add(new ClinicIcon(ClinicGlyph.Gem, 16, GemInk));
            Text(amount, text, "gem-amount-value", true);
        }

        private void BuildSkipButton(VisualElement parent, ClinicConstructionState job)
        {
            var id = job.Id;
            var button = IconButton(parent, ClinicGlyph.Gem, "Finish now with gems", () => SkipConstruction(id), "skip-button",
                () => SkipLabel(id));
            button.Q<ClinicIcon>().Tint = GemInk;
            var label = Text(button, "", "skip-label", true);
            readouts.Add(() => label.text = SkipLabel(id));
        }

        private string SkipLabel(int jobId)
        {
            var job = State.Construction.Find(c => c.Id == jobId);
            var cost = job == null ? 0 : ClinicPremiumRules.SkipCost(State, job);
            return cost == 0 ? "Finish" : "Finish · " + cost;
        }

        private void SkipConstruction(int jobId)
        {
            if (saves.HasPendingOfflineProgress) { Notify("Saving your return first…"); return; }
            var job = State.Construction.Find(c => c.Id == jobId);
            if (job == null) return;
            var cost = ClinicPremiumRules.SkipCost(State, job);
            if (profile.premium.gems < cost)
            {
                Notify("You need " + cost + " gems. Goals and the gem shop are here.", 5);
                ToggleGems();
                return;
            }
            var room = job.Room;
            if (!saves.SkipConstruction(jobId, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That work could not be finished.", 6); return; }
            RebindAfterCommit();
            clinicAudio.PlayEvent(ClinicEventKind.ConstructionCompleted);
            Feedback(1);
            Celebrate(world.GetRoomPoint(room), 2.6f, "Room ready!");
            Notify(RoomName(room) + " is ready");
        }

        private void ClaimGoal(string id)
        {
            var goal = ClinicMilestones.Find(id);
            if (!saves.ClaimMilestone(id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That reward could not be collected.", 6); return; }
            RebindAfterCommit();
            Feedback(1);
            clinicAudio.PlayReward();
            Notify("+" + goal.GemReward + " gems · " + goal.Title);
        }

        private void BuyGems(string productId)
        {
            if (apple == null || apple.IsPurchasing) return;
            Notify("Opening the App Store…");
            apple.Purchase(productId);
        }

        private void OnGemTransaction(AppleGemTransaction transaction)
        {
            pendingGems.Enqueue(transaction);
            ProcessGemTransactions();
        }

        /// <summary>Record each delivered transaction, then let StoreKit finish it. Unsaved ones stay queued and unfinished.</summary>
        private void ProcessGemTransactions()
        {
            if (!ready || saves == null || saves.HasPendingOfflineProgress) return;
            var changed = false;
            while (pendingGems.Count > 0)
            {
                var transaction = pendingGems.Peek();
                var finish = ClinicGemPacks.Apply(saves, transaction.transactionId, transaction.productId, transaction.revoked, DateTimeOffset.UtcNow, out var outcome);
                if (!finish && outcome == ClinicPurchaseGrant.NotSaved) break;
                pendingGems.Dequeue();
                if (!finish) continue;
                apple.FinishTransaction(transaction.transactionId);
                if (outcome != ClinicPurchaseGrant.Granted) continue;
                changed = true;
                if (transaction.revoked) Notify("A refunded gem purchase was removed", 6);
                else { Feedback(1); clinicAudio.PlayReward(); Notify("+" + ClinicGemPacks.GemsFor(transaction.productId) + " gems added. Thank you!", 5); }
            }
            if (changed) RebindAfterCommit();
        }

        private void RebindAfterCommit()
        {
            profile = saves.Profile;
            BindSimulations();
            dockKey = "";
            UpdateReadouts();
        }
    }
}
