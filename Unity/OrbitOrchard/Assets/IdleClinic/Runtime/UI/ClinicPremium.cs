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
            // Achievements, completed skipped lessons, today's goals and the login reward: everything waiting behind the gem button.
            claimableGoals = saves.RewardsReady(DateTimeOffset.UtcNow);
            gemLabel.text = Money(profile.premium.gems);
            gemBadge.text = claimableGoals.ToString();
            gemBadge.style.display = claimableGoals > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Today's goal definition (day, ids, targets, claims) is part of the key, so an open Today panel rebuilds at midnight even
        // when every count and balance is unchanged.
        private string GemDockKey() => gemTab + ":" + shopExtrasOpen + ":" + saves.DailyDefinitionKey(DateTimeOffset.UtcNow) + ":" + profile.daily?.loginDay + ":" + saves.BoostActive(DateTimeOffset.UtcNow) + ":" + saves.OfflineLimitHours + ":"
            + profile.premium.gems + ":" + claimableGoals + ":" + profile.premium.milestonesClaimed.Count
            + ":" + saves.HasUnlock(ClinicUnlocks.ExtraBuilder) + ":" + (profile.guide?.skipped.Count ?? 0) + ":" + (profile.guide?.workFinishedEarly ?? 0)
            + ":" + string.Join(";", apple?.Products.Select(p => p.id + "=" + p.price) ?? Enumerable.Empty<string>())
            + ":" + apple?.StoreStatus + ":" + (apple?.IsPurchasing ?? false) + ":" + (apple?.IsRestoring ?? false);

        private void ToggleGems()
        {
            var open = !gemsOpen;
            CloseContext();
            gemsOpen = open;
            dockKey = ""; UpdateReadouts();
        }

        private enum GemTab { Today, Goals, Shop }
        private GemTab gemTab = GemTab.Today;

        private void BuildGemsDock()
        {
            var tabs = Box(dock, "segmented gem-tabs");
            foreach (GemTab tab in Enum.GetValues(typeof(GemTab)))
            {
                var chosen = tab;
                Action pick = () => { gemTab = chosen; dockKey = ""; UpdateReadouts(); };
                var button = new Button(pick) { name = "gem-tab-" + tab.ToString().ToLowerInvariant() };
                button.AddToClassList("segment"); button.AddToClassList("gem-tab"); button.EnableInClassList("segment-selected", tab == gemTab);
                Text(button, tab.ToString(), "segment-label", true);
                var ready = saves == null ? 0 : tab == GemTab.Today ? saves.DailyReady(DateTimeOffset.UtcNow) : tab == GemTab.Goals ? saves.GoalsReady() : 0;
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
            var goalDay = profile.daily.day;
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
                    var collect = IconButton(row2, ClinicGlyph.Gem, "Collect " + ClinicDaily.Describe(goal) + ": " + goal.GemReward + " gems and " + Money(goal.CoinReward) + " coins", () => ClaimDaily(id, goalDay), "collect-small");
                    collect.Q<ClinicIcon>().RemoveFromHierarchy();
                    Text(collect, "Collect", "collect-label", true);
                }
                else if (claimed) { var check = new ClinicIcon(ClinicGlyph.Check, 22, LeafInk); check.style.marginLeft = 10; row2.Add(check); }
                else RewardPair(row2, goal.GemReward, goal.CoinReward, false);
            }
            var bonus = Box(content, "bonus-row"); bonus.pickingMode = PickingMode.Ignore;
            Text(bonus, saves.DailyBonusPaid ? "Today's bonus of " + ClinicDaily.AllGoalsGemBonus + " gems is collected. New goals tomorrow."
                : "Finish all " + goals.Count + " for " + ClinicDaily.AllGoalsGemBonus + " bonus gems. Goals stay fixed until midnight (UTC).", "bonus-text");
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
            BuildSkippedLessons(content);
            if (readyGoals.Count == 0 && upcoming.Count == 0) Text(content, "Every goal is complete.", "gem-note");
            else if (claimed.Count > 0)
            {
                var done = Box(content, "goals-done"); done.pickingMode = PickingMode.Ignore;
                done.Add(new ClinicIcon(ClinicGlyph.Check, 18, LeafInk));
                Text(done, claimed.Count + (claimed.Count == 1 ? " goal complete" : " goals complete"), "gem-note");
            }
        }

        /// <summary>Optional lessons the player skipped: collect them once actually done, or put them back in the guide.</summary>
        private void BuildSkippedLessons(VisualElement content)
        {
            var skipped = saves.SkippedGuideSteps();
            if (skipped.Count == 0) return;
            Section(content, "Optional lessons", "Skipped · nothing spent");
            foreach (var step in skipped)
            {
                var id = step.Id; var done = saves.IsGuideStepDone(step);
                var row = Box(content, "goal-row lesson-row"); row.EnableInClassList("goal-ready", done);
                var words = Box(row, "goal-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, step.Title, "gem-goal-title", true);
                Text(words, done ? "Done. Collect the reward." : step.FreeAlternative, "offer-detail");
                if (done)
                {
                    var collect = IconButton(row, ClinicGlyph.Gem, "Collect " + step.GemReward + " gems for " + step.Title, () => ClaimSkippedLesson(id), "collect-small");
                    collect.Q<ClinicIcon>().Tint = GemOnInk;
                    Display(Text(collect, "+" + step.GemReward, "collect-label", true));
                }
                else
                {
                    var resume = IconButton(row, ClinicGlyph.Goal, "Put " + step.Title + " back in the guide", () => ResumeLesson(id), "soft-action");
                    resume.name = "resume-" + id;
                    resume.Q<ClinicIcon>().Tint = LeafInk;
                    Text(resume, "Add back", "soft-action-label", true);
                }
            }
        }

        private void ClaimSkippedLesson(string id)
        {
            var step = ClinicGuide.Find(id);
            if (!saves.ClaimGuideStep(id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That reward could not be collected.", 6); return; }
            RebindAfterCommit(); Feedback(1); clinicAudio.PlayReward();
            Notify("+" + step.GemReward + " gems · " + step.Title, 4);
        }

        private void ResumeLesson(string id)
        {
            if (!saves.ResumeGuideStep(id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That lesson could not be added back.", 6); return; }
            RebindAfterCommit();
            var current = saves.CurrentGuideStep();
            Notify(current != null && current.Id == id ? "Back in your guide: " + current.Title : "Added back. It returns after your current lesson.", 5);
        }

        private bool shopExtrasOpen;

        private Image ShopArt(VisualElement parent, string asset, string classes)
        {
            var image = new Image { image = Resources.Load<Texture2D>("Shop/" + asset), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            foreach (var name in classes.Split(' ')) image.AddToClassList(name);
            parent.Add(image);
            return image;
        }

        private void BuildShopTab(VisualElement content)
        {
            dock.AddToClassList("shop-dock");
            if (shopExtrasOpen)
            {
                var back = IconButton(content, ClinicGlyph.Chevron, "Back to gem packs", () => { shopExtrasOpen = false; dockKey = ""; UpdateReadouts(); }, "shop-back");
                back.name = "shop-back";
                Text(back, "Back to gem packs", "shop-back-label", true);
                BuildShopExtras(content);
            }
            else
            {
                BuildBuilderSection(content);
                BuildFreeGemsRow(content);
                Section(content, "Gems", "Gems never expire");
                var packs = apple == null ? new List<AppleProduct>() : apple.Products.Where(p => ClinicGemPacks.IsGemPack(p.id)).OrderBy(p => ClinicGemPacks.GemsFor(p.id)).ToList();
                if (packs.Count == 0)
                {
                    Text(content, apple == null || string.IsNullOrEmpty(apple.StoreStatus) ? "Gem packs are loading from the App Store." : apple.StoreStatus, "gem-note");
                    if (apple != null) apple.LoadProducts();
                }
                else
                {
                    var list = Box(content, "shop-pack-list");
                    foreach (var pack in packs)
                    {
                        var product = pack.id;
                        var gems = ClinicGemPacks.GemsFor(product);
                        var button = IconButton(list, ClinicGlyph.Gem, "Buy " + gems + " gems for " + pack.price + ". Opens Apple's purchase confirmation.", () => BuyGems(product), "shop-pack-row");
                        button.name = "shop-pack-" + gems;
                        button.Q<ClinicIcon>().RemoveFromHierarchy();
                        var asset = product == ClinicGemPacks.Small ? "GemsSmall" : product == ClinicGemPacks.Medium ? "GemsMedium" : product == ClinicGemPacks.Large ? "GemsLarge" : "GemsExtraLarge";
                        ShopArt(button, asset, "shop-pack-art");
                        Display(Text(button, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " gems", "shop-pack-amount", true));
                        var price = Box(button, "shop-cash-price"); price.pickingMode = PickingMode.Ignore;
                        Text(price, pack.price, "shop-cash-label", true);
                        button.SetEnabled(!apple.IsPurchasing);
                        if (pack == packs[packs.Count - 1]) button.AddToClassList("shop-pack-row-last");
                    }
                }
                var extras = IconButton(content, ClinicGlyph.Coin, "Browse coins and boosts", () => { shopExtrasOpen = true; dockKey = ""; UpdateReadouts(); }, "shop-extras-link");
                extras.name = "shop-coins-boosts";
                extras.Q<ClinicIcon>().RemoveFromHierarchy();
                ShopArt(extras, "CoinsBoosts", "shop-extras-art");
                var words = Box(extras, "offer-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, "Coins & boosts", "shop-link-title", true);
                Text(words, "More ways to progress your clinic", "offer-detail");
                extras.Add(new ClinicIcon(ClinicGlyph.Chevron, 20, LeafInk));
            }
            var footer = Box(content, "shop-footer");
            Text(footer, "Prices come from the App Store in your currency. Buying opens Apple's confirmation; nothing is charged unless you confirm there. Gems stay with this clinic on this device.", "gem-note");
            if (apple != null)
            {
                var restore = IconButton(footer, ClinicGlyph.Restore, "Restore existing purchases", RequestRestore, "text-link");
                restore.name = "shop-restore-purchases";
                restore.Q<ClinicIcon>().RemoveFromHierarchy();
                Text(restore, "Restore purchases", "text-link-label", true);
                restore.SetEnabled(!restoreRequested && !apple.IsRestoring);
            }
        }

        private void BuildShopExtras(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            Section(content, "Coins", "Instant · added to this clinic's wallet");
            var coins = Box(content, "shop-grid shop-grid-1");
            foreach (var pack in ClinicShopOffers.CoinPacks)
            {
                var offer = pack; var amount = saves.CoinPackAmount(pack);
                GemOffer(coins, ClinicGemSpendKind.CoinPack, offer.Id, ClinicGlyph.Coin, Money(amount) + " coins", "Instant · " + offer.Visits + " visits at today's fee", offer.Gems, "shop-extra-offer");
            }
            Section(content, "Boosts", saves.BoostActive(now) ? "2× active · " + TimeLabel(saves.BoostRemaining(now).TotalSeconds) + " left" : "Real time, even while closed");
            var boosts = Box(content, "shop-grid shop-grid-1");
            foreach (var boost in ClinicShopOffers.Boosts)
            {
                var offer = boost;
                GemOffer(boosts, ClinicGemSpendKind.Boost, offer.Id, ClinicGlyph.Bolt, "2× collections", offer.Hours + (offer.Hours == 1 ? " hour" : " hours") + " · every collection doubled", offer.Gems, "shop-extra-offer");
            }
            foreach (var tier in ClinicShopOffers.OfflineTiers)
            {
                if (saves.OfflineLimitHours >= tier.Hours) continue;
                var offer = tier;
                var shelf = Box(content, "shop-grid shop-grid-1");
                GemOffer(shelf, ClinicGemSpendKind.OfflineUpgrade, offer.Id, ClinicGlyph.Clock, offer.Name, "Permanent · now " + saves.OfflineLimitHours + " hours, tills hold " + Money(saves.OfflineCoinCap), offer.Gems, "shop-extra-offer");
                break;
            }
        }

        /// <summary>A gem offer. Tapping it only opens the confirmation sheet, which states the benefit, duration and price;
        /// nothing is spent until the player confirms there.</summary>
        private Button GemOffer(VisualElement shelf, ClinicGemSpendKind kind, string id, ClinicGlyph glyph, string title, string detail, long gems, string style)
        {
            var button = IconButton(shelf, glyph, title + (detail == null ? "" : ", " + detail) + ". " + gems + " gems. Opens a confirmation.", () => RequestGemSpend(kind, id),
                "offer-card " + style);
            var priceOnly = style.Contains("offer-price-only");
            var icon = button.Q<ClinicIcon>();
            icon.Tint = glyph == ClinicGlyph.Coin ? CoinInk : glyph == ClinicGlyph.Bolt ? GemInk : LeafInk;
            if (glyph == ClinicGlyph.Coin || priceOnly) icon.RemoveFromHierarchy();
            if (!priceOnly)
            {
                var words = Box(button, "offer-words"); words.pickingMode = PickingMode.Ignore;
                var titleLabel = Text(words, title, "offer-title", true);
                if (glyph == ClinicGlyph.Coin) Display(titleLabel);
                if (detail != null) Text(words, detail, "offer-detail");
            }
            var price = Box(button, "price-chip"); price.pickingMode = PickingMode.Ignore;
            price.Add(new ClinicIcon(ClinicGlyph.Gem, 14, GemInk));
            Display(Text(price, gems.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "price-chip-value", true));
            button.name = "shop-offer-" + id;
            // Short of gems still opens the sheet, which shows the shortfall and the free way to earn more.
            var shortfall = Math.Max(0, gems - profile.premium.gems);
            if (shortfall > 0 && !priceOnly) Text(button.Q(className: "offer-words"), "Need " + Money(shortfall) + " more gems", "shop-shortfall");
            return button;
        }

        /// <summary>Free gems stay visible in the shop: what is ready now and what today's goals still offer.</summary>
        private void BuildFreeGemsRow(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            var ready = saves.FreeGemsReady(now); var left = saves.FreeGemsLeftToday(now);
            var row = IconButton(content, ClinicGlyph.Goal, "Free gems: " + ready + " ready now, " + left + " more from today's goals", () => OpenGemTab(ready > 0 && saves.GoalsReady() > 0 && saves.DailyReady(now) == 0 ? GemTab.Goals : GemTab.Today), "shop-free-row");
            row.name = "shop-free-gems";
            row.Q<ClinicIcon>().Tint = LeafInk;
            var words = Box(row, "offer-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, "Free gems", "shop-link-title", true);
            Text(words, (ready > 0 ? Money(ready) + " ready to collect · " : "") + Money(left) + " more from today's goals", "offer-detail");
            row.Add(new ClinicIcon(ClinicGlyph.Chevron, 20, LeafInk));
        }

        private void ClaimLogin()
        {
            var streak = saves.NextStreakDay(DateTimeOffset.UtcNow);
            if (!saves.ClaimLoginReward(DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "The reward could not be collected.", 6); return; }
            RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1);
            LaunchGemsFrom(overlay.WorldToLocal(dock.worldBound.center), 6);
            Notify("Day " + streak + " reward collected", 4);
        }

        private void ClaimDaily(string id, int day)
        {
            // The button belongs to the list drawn for its day; after midnight it rebuilds instead of collecting.
            if (!saves.ClaimDailyGoal(id, day, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "The reward could not be collected.", 6); dockKey = ""; UpdateReadouts(); return; }
            RebindAfterCommit(); clinicAudio.PlayReward(); Feedback(1);
            LaunchGemsFrom(overlay.WorldToLocal(dock.worldBound.center), 5);
            LaunchCoinsFrom(world.GetRoomPoint(ClinicRoom.Reception));
            Notify("Daily goal collected", 3);
        }

        private void BuildBuilderSection(VisualElement content)
        {
            var owned = saves.HasUnlock(ClinicUnlocks.ExtraBuilder);
            var hero = Box(content, "shop-builder-hero"); hero.name = "shop-builder-hero";
            ShopArt(hero, "BuilderHero", "shop-builder-art").scaleMode = ScaleMode.ScaleAndCrop;
            var copy = Box(hero, "shop-builder-copy"); copy.pickingMode = PickingMode.Ignore;
            copy.EnableInClassList("shop-builder-copy-owned", owned);
            Text(copy, owned ? "Your building" : "Build more.", "shop-builder-headline", true);
            Text(copy, owned ? "team is ready." : "Wait less.", "shop-builder-headline", true);
            Text(copy, "Second builder", "shop-builder-tag", true);
            Text(copy, "Build two rooms at once.", "shop-builder-benefit");
            Text(copy, owned ? "Permanently unlocked." : "Yours permanently.", "shop-builder-benefit");
            if (owned)
            {
                Text(hero, "Owned · works in all your clinics", "shop-builder-owned", true);
                return;
            }
            var choices = Box(hero, "shop-builder-choices");
            var product = apple?.Products.FirstOrDefault(p => p.id == ClinicUnlocks.ExtraBuilderProduct);
            var cash = IconButton(choices, ClinicGlyph.Upgrade, product == null ? "Second builder price is loading" : "Buy a second builder for " + product.price + ", keep forever. Opens Apple's purchase confirmation.", () => { if (product != null) BuyGems(product.id); }, "shop-builder-cash");
            cash.name = "shop-builder-store";
            cash.Q<ClinicIcon>().RemoveFromHierarchy();
            Text(cash, product?.price ?? "Loading…", "shop-cash-label", true);
            cash.SetEnabled(product != null && !apple.IsPurchasing);
            var gem = GemOffer(choices, ClinicGemSpendKind.ExtraBuilder, ClinicUnlocks.ExtraBuilder, ClinicGlyph.Gem, "Second builder", null, ClinicUnlocks.ExtraBuilderGems, "offer-price-only shop-builder-gems");
            gem.name = "shop-builder-gems";
            Text(gem.Q(className: "price-chip"), "gems", "price-chip-value", true);
            var shortage = Math.Max(0, ClinicUnlocks.ExtraBuilderGems - profile.premium.gems);
            if (shortage > 0) Text(hero, "Need " + Money(shortage) + " more gems", "shop-builder-shortfall");
            Text(hero, "Choose one: gems, or the App Store price (Apple confirms)", "shop-payment-note");
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

        /// <summary>Finish construction early. Opens the confirmation sheet unless it is free (the last minute).</summary>
        private void SkipConstruction(int jobId)
        {
            var job = State.Construction.Find(c => c.Id == jobId);
            if (job == null) return;
            lastFinishRequestRoom = job.Room;
            RequestGemSpend(ClinicGemSpendKind.FinishWork, jobId.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
            Notify("Opening Apple's purchase confirmation…");
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
