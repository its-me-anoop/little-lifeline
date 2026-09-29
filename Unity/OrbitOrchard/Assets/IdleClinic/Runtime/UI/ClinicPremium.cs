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
            var icon = button.Q<ClinicIcon>(); icon.Tint = GemInk; icon.style.width = 24; icon.style.height = 24;
            gemLabel = Display(Text(button, "0", "gem-value", true));
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
            var tabs = Box(dock, "segmented gem-tabs");
            foreach (GemTab tab in Enum.GetValues(typeof(GemTab)))
            {
                var chosen = tab;
                Action pick = () => { gemTab = chosen; pendingConfirm = null; dockKey = ""; UpdateReadouts(); };
                var button = new Button(pick) { name = "gem-tab-" + tab.ToString().ToLowerInvariant() };
                button.AddToClassList("segment"); button.AddToClassList("gem-tab"); button.EnableInClassList("segment-selected", tab == gemTab);
                Text(button, tab.ToString(), "segment-label", true);
                var ready = saves == null ? 0 : tab == GemTab.Today ? saves.DailyReady(DateTimeOffset.UtcNow) : tab == GemTab.Goals ? saves.ClaimableMilestones().Count() : 0;
                if (ready > 0) Text(button, ready.ToString(), "tab-badge", true);
                tabs.Add(button);
                RegisterAccessibleButton(button, tab + (ready > 0 ? ", " + ready + " ready" : ""), pick);
            }
            var content = new ScrollView(ScrollViewMode.Vertical) { name = "clinic-gems-content", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Hidden };
            content.AddToClassList("bounded-dock-content"); dock.Add(content);
            BindTouchCaptureLifecycle(content.contentContainer); BindTouchCaptureLifecycle(content.contentViewport);
            if (gemTab == GemTab.Today) BuildTodayTab(content);
            else if (gemTab == GemTab.Goals) BuildGoalsTab(content);
            else BuildShopTab(content);
        }

        private VisualElement Section(VisualElement parent, string title, string note = null)
        {
            var head = Box(parent, "section-head"); head.pickingMode = PickingMode.Ignore;
            Text(head, title, "gem-section", true);
            if (note != null) Text(head, note, "section-note");
            return head;
        }

        private void BuildTodayTab(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            var streak = saves.NextStreakDay(now); var ready = saves.LoginRewardReady(now);
            Section(content, "Daily streak", ready ? "Day " + streak + " is ready" : "Come back tomorrow to keep it going");
            var row = Box(content, "streak-row");
            for (var day = 1; day <= 7; day++)
            {
                var done = day < streak || day == streak && !ready;
                var tile = Box(row, "streak-day"); tile.EnableInClassList("streak-done", done); tile.EnableInClassList("streak-next", day == streak && ready);
                tile.EnableInClassList("streak-week", day == 7 && !done);
                if (done) tile.Add(new ClinicIcon(ClinicGlyph.Check, 18, LeafInk));
                else Display(Text(tile, ClinicDaily.StreakReward(day).ToString(), "streak-gems", true));
                Text(tile, "Day " + day, "streak-number", true);
            }
            if (ready)
            {
                var claim = IconButton(content, ClinicGlyph.Gem, "Collect day " + streak + " reward", ClaimLogin, "collect-wide");
                var icon = claim.Q<ClinicIcon>(); icon.RemoveFromHierarchy();
                Text(claim, "Collect day " + streak, "collect-label", true);
                RewardPair(claim, ClinicDaily.StreakReward(streak), ClinicDaily.StreakCoins(streak, State), true);
            }

            var goals = saves.DailyGoals(now);
            var finished = goals.Count(g => saves.IsDailyDone(g));
            Section(content, "Today's goals", finished + " of " + goals.Count + " done");
            foreach (var goal in goals)
            {
                var id = goal.Id; var claimed = saves.IsDailyClaimed(id); var done = saves.IsDailyDone(goal);
                var progress = Math.Min(goal.Target, saves.DailyProgress(goal));
                var row2 = Box(content, "goal-row"); row2.EnableInClassList("goal-ready", done && !claimed);
                var words = Box(row2, "goal-words"); words.pickingMode = PickingMode.Ignore;
                var top = Box(words, "goal-top"); top.pickingMode = PickingMode.Ignore;
                Text(top, ClinicDaily.Describe(goal), "gem-goal-title", true);
                Text(top, claimed ? "Collected" : progress.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " / " + goal.Target.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "goal-count", true);
                var bar = Box(words, "meter meter-leaf"); bar.EnableInClassList("meter-gem", done && !claimed);
                Box(bar, "meter-fill").style.width = Length.Percent(100f * progress / Math.Max(1, goal.Target));
                if (done && !claimed)
                {
                    var collect = IconButton(row2, ClinicGlyph.Gem, "Collect " + ClinicDaily.Describe(goal) + ": " + goal.GemReward + " gems and " + Money(goal.CoinReward) + " coins", () => ClaimDaily(id), "collect-small");
                    collect.Q<ClinicIcon>().RemoveFromHierarchy();
                    Text(collect, "Collect", "collect-label", true);
                }
                else if (claimed) { var check = new ClinicIcon(ClinicGlyph.Check, 22, LeafInk); check.style.marginLeft = 10; row2.Add(check); }
                else RewardPair(row2, goal.GemReward, goal.CoinReward, false);
            }
            var bonus = Box(content, "bonus-row"); bonus.pickingMode = PickingMode.Ignore;
            Text(bonus, "Finish all three for " + ClinicDaily.AllGoalsGemBonus + " bonus gems. New goals every day.", "bonus-text");
            var dots = Box(bonus, "bonus-dots"); dots.pickingMode = PickingMode.Ignore;
            for (var i = 0; i < goals.Count; i++) Box(dots, "bonus-dot" + (i < finished ? " bonus-dot-done" : ""));
        }

        /// <summary>Gems, then coins, stacked on the right of a goal or inline on a collect button.</summary>
        private void RewardPair(VisualElement parent, long gems, long coins, bool onButton)
        {
            var pair = Box(parent, onButton ? "reward-inline" : "reward-stack"); pair.pickingMode = PickingMode.Ignore;
            var g = Box(pair, "reward-item"); g.Add(new ClinicIcon(ClinicGlyph.Gem, onButton ? 17 : 14, onButton ? GemOnInk : GemInk));
            Display(Text(g, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), onButton ? "reward-on-button" : "reward-gems", true));
            if (coins <= 0) return;
            var c = Box(pair, "reward-item"); c.Add(new ClinicIcon(ClinicGlyph.Coin, onButton ? 17 : 14, onButton ? GemOnInk : CoinInk));
            Display(Text(c, Money(coins), onButton ? "reward-on-button" : "reward-coins", true));
        }

        private void BuildGoalsTab(VisualElement content)
        {
            var claimed = profile.premium.milestonesClaimed;
            var readyGoals = saves.ClaimableMilestones().ToList();
            // Ready first, so a reward is never below the fold.
            if (readyGoals.Count > 0) Section(content, "Ready to collect");
            foreach (var goal in readyGoals)
            {
                var id = goal.Id;
                var row = Box(content, "goal-row goal-ready");
                row.Add(new ClinicIcon(ClinicGlyph.Goal, 22, GemInk));
                Text(row, goal.Title, "gem-goal-title goal-fill", true);
                var button = IconButton(row, ClinicGlyph.Gem, "Collect " + goal.GemReward + " gems for " + goal.Title, () => ClaimGoal(id), "collect-small");
                button.Q<ClinicIcon>().Tint = GemOnInk;
                Display(Text(button, "+" + goal.GemReward, "collect-label", true));
            }
            var upcoming = ClinicMilestones.All.Where(m => !claimed.Contains(m.Id) && !readyGoals.Contains(m)).Take(UpcomingGoals + 2).ToList();
            if (upcoming.Count > 0) Section(content, "Coming up");
            foreach (var goal in upcoming)
            {
                var row = Box(content, "goal-row");
                row.Add(new ClinicIcon(ClinicGlyph.Goal, 22, LeafInk));
                Text(row, goal.Title, "gem-goal-title goal-fill", true);
                RewardPair(row, goal.GemReward, 0, false);
            }
            if (readyGoals.Count == 0 && upcoming.Count == 0) Text(content, "Every goal is complete.", "gem-note");
            else if (claimed.Count > 0)
            {
                var done = Box(content, "goals-done"); done.pickingMode = PickingMode.Ignore;
                done.Add(new ClinicIcon(ClinicGlyph.Check, 18, LeafInk));
                Text(done, claimed.Count + (claimed.Count == 1 ? " goal complete" : " goals complete"), "gem-note");
            }
        }

        private void BuildShopTab(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            Section(content, "Gems");
            var packs = apple == null ? new List<AppleProduct>() : apple.Products.Where(p => ClinicGemPacks.IsGemPack(p.id)).OrderBy(p => ClinicGemPacks.GemsFor(p.id)).ToList();
            if (packs.Count == 0)
            {
                Text(content, apple == null || string.IsNullOrEmpty(apple.StoreStatus) ? "Gem packs are loading from the App Store." : apple.StoreStatus, "gem-note");
                if (apple != null) apple.LoadProducts();
            }
            else
            {
                var shelf = Box(content, "shop-grid shop-grid-2");
                foreach (var pack in packs)
                {
                    var product = pack.id; var gems = ClinicGemPacks.GemsFor(product);
                    var button = IconButton(shelf, ClinicGlyph.Gem, "Buy " + gems + " gems for " + pack.price, () => BuyGems(product), "gem-pack");
                    var icon = button.Q<ClinicIcon>(); icon.Tint = GemInk; icon.style.width = 30; icon.style.height = 30;
                    var words = Box(button, "pack-words"); words.pickingMode = PickingMode.Ignore;
                    Display(Text(words, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "gem-pack-amount", true));
                    Text(words, pack.price, "gem-pack-price", true);
                    if (product == ClinicGemPacks.Medium) { button.AddToClassList("gem-pack-best"); Text(button, "Best value", "best-value", true); }
                    button.SetEnabled(!apple.IsPurchasing);
                }
            }

            Section(content, "Coins");
            var coins = Box(content, "shop-grid shop-grid-3");
            foreach (var pack in ClinicShopOffers.CoinPacks)
            {
                var offer = pack; var amount = saves.CoinPackAmount(pack);
                GemOffer(coins, offer.Id, ClinicGlyph.Coin, Money(amount), null, offer.Gems, () => saves.BuyCoinPack(offer.Id, DateTimeOffset.UtcNow),
                    "+" + Money(amount) + " coins", "offer-coins");
            }

            Section(content, "Speed ups", saves.BoostActive(now) ? "2× active · " + TimeLabel(saves.BoostRemaining(now).TotalSeconds) + " left" : null);
            var boosts = Box(content, "shop-grid shop-grid-2");
            foreach (var boost in ClinicShopOffers.Boosts)
            {
                var offer = boost;
                GemOffer(boosts, offer.Id, ClinicGlyph.Bolt, "2× collections", offer.Hours + (offer.Hours == 1 ? " hour" : " hours"), offer.Gems,
                    () => saves.BuyBoost(offer.Id, DateTimeOffset.UtcNow), "Double collections for " + offer.Hours + (offer.Hours == 1 ? " hour" : " hours"), "offer-boost");
            }
            foreach (var tier in ClinicShopOffers.OfflineTiers)
            {
                if (saves.OfflineLimitHours >= tier.Hours) continue;
                var offer = tier;
                var shelf = Box(content, "shop-grid shop-grid-1");
                GemOffer(shelf, offer.Id, ClinicGlyph.Clock, offer.Name, "Now " + saves.OfflineLimitHours + " hours · tills hold " + Money(saves.OfflineCoinCap), offer.Gems,
                    () => saves.BuyOfflineUpgrade(offer.Id, DateTimeOffset.UtcNow), offer.Name + " unlocked", "offer-wide");
                break;
            }
            BuildBuilderSection(content);
            var footer = Box(content, "shop-footer");
            Text(footer, "Gems never expire and are saved with this clinic on this device.", "gem-note");
            if (apple != null)
            {
                var restore = IconButton(footer, ClinicGlyph.Restore, "Restore existing purchases", RequestRestore, "text-link");
                restore.name = "shop-restore-purchases";
                restore.Q<ClinicIcon>().RemoveFromHierarchy();
                Text(restore, "Restore", "text-link-label", true);
                restore.SetEnabled(!restoreRequested && !apple.IsRestoring);
            }
        }

        /// <summary>A gem purchase button. Spends of 50 gems or more ask for a second tap before anything is spent.</summary>
        private void GemOffer(VisualElement shelf, string id, ClinicGlyph glyph, string title, string detail, long gems, Func<bool> buy, string done, string style)
        {
            var confirming = pendingConfirm == id && Time.unscaledTimeAsDouble < pendingConfirmUntil;
            var button = IconButton(shelf, glyph, title + (detail == null ? "" : ", " + detail) + " for " + gems + " gems", () =>
            {
                if (gems >= ClinicShopOffers.ConfirmAtGems && !(pendingConfirm == id && Time.unscaledTimeAsDouble < pendingConfirmUntil))
                { pendingConfirm = id; pendingConfirmUntil = Time.unscaledTimeAsDouble + 4; dockKey = ""; UpdateReadouts(); return; }
                pendingConfirm = null;
                if (!buy()) { Notify(saves.Error ?? "That could not be bought.", 6); dockKey = ""; UpdateReadouts(); return; }
                RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1); Notify(done, 4);
            }, "offer-card " + style + (confirming ? " offer-confirm" : ""));
            var priceOnly = style.Contains("offer-price-only");
            var icon = button.Q<ClinicIcon>();
            icon.Tint = confirming ? GemOnInk : glyph == ClinicGlyph.Coin ? CoinInk : glyph == ClinicGlyph.Bolt ? GemInk : LeafInk;
            if (glyph == ClinicGlyph.Coin || priceOnly) icon.RemoveFromHierarchy();
            if (!priceOnly || confirming)
            {
                var words = Box(button, "offer-words"); words.pickingMode = PickingMode.Ignore;
                var titleLabel = Text(words, confirming ? (priceOnly ? "Confirm" : "Tap again to confirm") : title, "offer-title", true);
                if (glyph == ClinicGlyph.Coin && !confirming) Display(titleLabel);
                if (detail != null && !confirming) Text(words, detail, "offer-detail");
            }
            var price = Box(button, "price-chip"); price.pickingMode = PickingMode.Ignore;
            price.Add(new ClinicIcon(ClinicGlyph.Gem, 14, GemInk));
            Display(Text(price, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "price-chip-value", true));
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
            if (saves.HasUnlock(ClinicUnlocks.ExtraBuilder))
            {
                Text(content, "Two builders: two rooms can build or renovate at once.", "gem-note");
                return;
            }
            var row = Box(content, "builder-row");
            row.Add(new ClinicIcon(ClinicGlyph.Upgrade, 24, LeafInk));
            var words = Box(row, "offer-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, "Second builder", "offer-title", true);
            Text(words, "Two rooms build at once, in both clinics", "offer-detail");
            GemOffer(row, ClinicUnlocks.ExtraBuilder, ClinicGlyph.Gem, "Second builder", null, ClinicUnlocks.ExtraBuilderGems,
                () => saves.UnlockExtraBuilderWithGems(DateTimeOffset.UtcNow), "Your second builder is ready. Two rooms can now build at once.", "offer-price-only");
            var product = apple?.Products.FirstOrDefault(p => p.id == ClinicUnlocks.ExtraBuilderProduct);
            if (product != null)
            {
                var buy = IconButton(row, ClinicGlyph.Upgrade, "Buy a second builder for " + product.price + ", keep forever", () => BuyGems(product.id), "offer-price-only store-price");
                buy.Q<ClinicIcon>().RemoveFromHierarchy();
                Text(buy, product.price, "price-chip-value", true);
                buy.SetEnabled(!apple.IsPurchasing);
            }
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

        private void BuildSkipButton(VisualElement parent, ClinicConstructionState job)
        {
            var id = job.Id;
            GemActionButton(parent, "Finish now with gems", "Finish now", () => SkipConstruction(id), () => SkipCost(id), "gem-action gem-action-wide", readouts);
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
