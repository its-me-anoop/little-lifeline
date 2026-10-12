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

        // ---- Today and Goals, in the Store's style: a hero card that says what is ready and what comes next, then cards with
        // one clear state each (to do with its progress, ready with one collect button, collected with a check). The rewards
        // and their rules live in the profile store; nothing here changes what is paid or when.

        /// <summary>A hero card like the Store's builder card: two short headline lines, a dark tag, a line or two of detail
        /// and a picture on the right.</summary>
        private VisualElement GemHero(VisualElement content, string name, string art, string first, string second, string tag, string tagName, params string[] details)
        {
            var hero = Box(content, "gem-hero"); hero.name = name;
            if (art != null) ShopArt(hero, art, "gem-hero-art");
            var copy = Box(hero, "gem-hero-copy"); copy.pickingMode = PickingMode.Ignore;
            Text(copy, first, "gem-hero-headline", true);
            Text(copy, second, "gem-hero-headline", true);
            Text(copy, tag, "gem-hero-tag", true).name = tagName;
            foreach (var detail in details) if (!string.IsNullOrEmpty(detail)) Text(copy, detail, "gem-hero-detail");
            return hero;
        }

        private static string UntilMidnightUtc(DateTimeOffset now) => TimeLabel((now.UtcDateTime.Date.AddDays(1) - now.UtcDateTime).TotalSeconds);

        private void BuildTodayTab(VisualElement content)
        {
            var now = DateTimeOffset.UtcNow;
            var streak = saves.NextStreakDay(now); var ready = saves.LoginRewardReady(now);
            // Rewards cycle weekly; the strip shows where today sits in the week.
            var week = (Math.Max(1, streak) - 1) % 7 + 1;
            var resetIn = UntilMidnightUtc(now);
            var gems = ClinicDaily.StreakReward(streak); var coins = ClinicDaily.StreakCoins(streak, State);
            var hero = GemHero(content, "today-hero", week == 7 && ready ? "GemsLarge" : "GemsMedium",
                ready ? "Daily reward" : "Collected.", ready ? "is ready." : "Back tomorrow.",
                "Day " + week + " of 7", "today-streak",
                ready ? "+" + gems + " gems and " + Money(coins) + " coins." : "Tomorrow, day " + (week % 7 + 1) + ": +" + ClinicDaily.StreakReward(streak + 1) + " gems.",
                week < 7 || !ready ? "Day 7 pays " + ClinicDaily.StreakReward(7) + " gems." : "The best day of the week.");
            var strip = Box(hero, "today-strip"); strip.pickingMode = PickingMode.Ignore;
            for (var day = 1; day <= 7; day++)
            {
                var done = ready ? day < week : day <= week;
                var chip = Box(strip, "today-chip"); chip.pickingMode = PickingMode.Ignore;
                chip.EnableInClassList("today-chip-done", done); chip.EnableInClassList("today-chip-next", ready && day == week);
                chip.EnableInClassList("today-chip-week", day == 7 && !done);
                if (done) chip.Add(new ClinicIcon(ClinicGlyph.Check, 16, LeafInk) { pickingMode = PickingMode.Ignore });
                else Display(Text(chip, ClinicDaily.StreakReward(day).ToString(), "today-chip-gems", true));
                Text(chip, "Day " + day, "today-chip-day", true);
            }
            if (ready)
            {
                var claim = IconButton(hero, ClinicGlyph.Gem, "Collect day " + week + " reward: " + gems + " gems and " + Money(coins) + " coins", ClaimLogin, "collect-wide today-collect");
                claim.name = "today-collect";
                claim.Q<ClinicIcon>().RemoveFromHierarchy();
                Text(claim, "Collect day " + week, "collect-label", true);
                RewardPair(claim, gems, coins, true);
            }
            else
            {
                var status = Box(hero, "today-status"); status.pickingMode = PickingMode.Ignore;
                status.Add(new ClinicIcon(ClinicGlyph.Check, 18, LeafInk) { pickingMode = PickingMode.Ignore });
                Text(status, "Collected today · next reward in " + resetIn, "today-status-label", true).name = "today-collected";
            }

            var goals = saves.DailyGoals(now);
            var goalDay = profile.daily.day;
            var finished = goals.Count(g => saves.IsDailyDone(g));
            Section(content, "Today's goals", finished + " of " + goals.Count + " done · new in " + resetIn);
            foreach (var goal in goals) DailyGoalCard(content, goal, goalDay);
            DailyBonusCard(content, goals.Count, finished, resetIn);
        }

        private static ClinicGlyph DailyGlyph(ClinicDailyMeasure measure)
            => measure == ClinicDailyMeasure.Treatments ? ClinicGlyph.Nurse : measure == ClinicDailyMeasure.Collected ? ClinicGlyph.Coin
                : measure == ClinicDailyMeasure.Spent ? ClinicGlyph.Upgrade : measure == ClinicDailyMeasure.ParkingFees ? ClinicGlyph.Parking
                : measure == ClinicDailyMeasure.Tips ? ClinicGlyph.Vending : ClinicGlyph.Reception;

        /// <summary>One of today's goals: what to do, how far there is to go, and its reward; once done, one collect button;
        /// once collected, a check.</summary>
        private void DailyGoalCard(VisualElement content, ClinicDailyGoal goal, int goalDay)
        {
            var id = goal.Id; var claimed = saves.IsDailyClaimed(id); var done = saves.IsDailyDone(goal);
            var progress = Math.Min(goal.Target, saves.DailyProgress(goal));
            var card = Box(content, "daily-goal"); card.name = "daily-goal-" + id;
            card.EnableInClassList("daily-goal-ready", done && !claimed); card.EnableInClassList("daily-goal-claimed", claimed);
            var tile = Box(card, "icon-tile daily-goal-tile"); tile.pickingMode = PickingMode.Ignore;
            tile.Add(new ClinicIcon(DailyGlyph(goal.Measure), 22, done && !claimed ? GemInk : LeafInk) { pickingMode = PickingMode.Ignore });
            var words = Box(card, "goal-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, ClinicDaily.Describe(goal), "gem-goal-title", true);
            var counted = goal.Measure == ClinicDailyMeasure.Treatments || goal.Measure == ClinicDailyMeasure.CheckIns;
            string Amount(long value) => counted ? value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : Money(value);
            Text(words, claimed ? "Collected · reward added" : done ? "Done · collect +" + goal.GemReward + " gems and " + Money(goal.CoinReward) + " coins"
                : Amount(progress) + " of " + Amount(goal.Target) + " · " + Amount(goal.Target - progress) + " to go", "daily-goal-status");
            var bar = Box(words, "meter meter-thick daily-goal-meter"); bar.pickingMode = PickingMode.Ignore;
            bar.EnableInClassList("meter-gem", done && !claimed);
            Box(bar, "meter-fill").style.width = Length.Percent(100f * progress / Math.Max(1, goal.Target));
            if (done && !claimed)
            {
                var collect = IconButton(card, ClinicGlyph.Gem, "Collect " + ClinicDaily.Describe(goal) + ": " + goal.GemReward + " gems and " + Money(goal.CoinReward) + " coins", () => ClaimDaily(id, goalDay), "collect-small daily-collect");
                collect.name = "daily-collect-" + id;
                collect.Q<ClinicIcon>().Tint = GemOnInk;
                Display(Text(collect, "+" + goal.GemReward, "collect-label", true));
            }
            else if (claimed)
            {
                var check = Box(card, "daily-goal-check"); check.pickingMode = PickingMode.Ignore;
                check.Add(new ClinicIcon(ClinicGlyph.Check, 20, LeafInk) { pickingMode = PickingMode.Ignore });
            }
            else RewardPair(card, goal.GemReward, goal.CoinReward, false);
        }

        /// <summary>The bonus for finishing all of today's goals: one segment per goal, paid with the last one collected.</summary>
        private void DailyBonusCard(VisualElement content, int count, int finished, string resetIn)
        {
            var paid = saves.DailyBonusPaid;
            var card = Box(content, "bonus-card"); card.name = "daily-bonus"; card.pickingMode = PickingMode.Ignore;
            card.EnableInClassList("bonus-card-paid", paid);
            var top = Box(card, "bonus-top"); top.pickingMode = PickingMode.Ignore;
            var words = Box(top, "goal-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, paid ? "Bonus collected" : "All " + count + " goals bonus", "gem-goal-title", true);
            Text(words, paid ? "New goals in " + resetIn + "." : "Added with the last goal you collect. Goals stay fixed until midnight (UTC).", "daily-goal-status");
            var chip = Box(top, "bonus-chip"); chip.pickingMode = PickingMode.Ignore;
            chip.Add(new ClinicIcon(paid ? ClinicGlyph.Check : ClinicGlyph.Gem, 16, paid ? LeafInk : GemInk) { pickingMode = PickingMode.Ignore });
            Display(Text(chip, "+" + ClinicDaily.AllGoalsGemBonus, "bonus-chip-value", true));
            var segments = Box(card, "bonus-segments"); segments.pickingMode = PickingMode.Ignore;
            for (var i = 0; i < count; i++) Box(segments, "bonus-segment" + (i < finished ? " bonus-segment-done" : "")).pickingMode = PickingMode.Ignore;
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
            var total = ClinicMilestones.All.Count;
            var complete = ClinicMilestones.All.Count(m => claimed.Contains(m.Id));
            var upcoming = ClinicMilestones.All.Where(m => !claimed.Contains(m.Id) && !readyGoals.Contains(m)).Take(UpcomingGoals + 2).ToList();
            var waiting = readyGoals.Sum(m => m.GemReward);
            var hero = GemHero(content, "goals-hero", "GemsSmall",
                readyGoals.Count > 0 ? readyGoals.Count + (readyGoals.Count == 1 ? " reward" : " rewards") : upcoming.Count > 0 ? "Keep going." : "All done.",
                readyGoals.Count > 0 ? "to collect." : upcoming.Count > 0 ? "More gems ahead." : "Every goal met.",
                complete + " of " + total + " complete", "goals-tag",
                readyGoals.Count > 0 ? "+" + waiting + " gems waiting below." : upcoming.Count > 0 ? "Next: " + upcoming[0].Title + "." : null);
            var meter = Box(hero, "meter meter-thick gem-hero-meter"); meter.pickingMode = PickingMode.Ignore;
            Box(meter, "meter-fill").style.width = Length.Percent(100f * complete / Math.Max(1, total));
            // Ready first, so a reward is never below the fold.
            if (readyGoals.Count > 0) Section(content, "Ready to collect", "+" + waiting + " gems");
            foreach (var goal in readyGoals)
            {
                var id = goal.Id;
                var card = Box(content, "goal-card goal-claim"); card.name = "goal-ready-" + id;
                var tile = Box(card, "icon-tile goal-card-tile"); tile.pickingMode = PickingMode.Ignore;
                tile.Add(new ClinicIcon(ClinicGlyph.Goal, 22, GemInk) { pickingMode = PickingMode.Ignore });
                var words = Box(card, "goal-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, goal.Title, "gem-goal-title", true);
                Text(words, "Done · collect " + goal.GemReward + " gems", "daily-goal-status");
                var button = IconButton(card, ClinicGlyph.Gem, "Collect " + goal.GemReward + " gems for " + goal.Title, () => ClaimGoal(id), "collect-small");
                button.name = "goal-collect-" + id;
                button.Q<ClinicIcon>().Tint = GemOnInk;
                Display(Text(button, "+" + goal.GemReward, "collect-label", true));
            }
            if (upcoming.Count > 0) Section(content, "Up next", "Gems when done");
            for (var i = 0; i < upcoming.Count; i++)
            {
                var goal = upcoming[i];
                var card = Box(content, i == 0 ? "goal-card goal-next" : "goal-card goal-upcoming"); card.pickingMode = PickingMode.Ignore;
                if (i == 0) card.name = "goal-next";
                var tile = Box(card, "icon-tile goal-card-tile"); tile.pickingMode = PickingMode.Ignore;
                tile.Add(new ClinicIcon(ClinicGlyph.Goal, 20, LeafInk) { pickingMode = PickingMode.Ignore });
                var words = Box(card, "goal-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, goal.Title, "gem-goal-title", true);
                if (i == 0) Text(words, "Your next goal", "daily-goal-status");
                var chip = Box(card, "price-chip goal-reward-chip"); chip.pickingMode = PickingMode.Ignore;
                chip.Add(new ClinicIcon(ClinicGlyph.Gem, 14, GemInk) { pickingMode = PickingMode.Ignore });
                Display(Text(chip, "+" + goal.GemReward, "price-chip-value", true));
            }
            BuildSkippedLessons(content);
            if (readyGoals.Count == 0 && upcoming.Count == 0) Text(content, "Every goal is complete.", "gem-note");
            else if (complete > 0)
            {
                var done = Box(content, "goals-done"); done.pickingMode = PickingMode.Ignore;
                done.Add(new ClinicIcon(ClinicGlyph.Check, 18, LeafInk) { pickingMode = PickingMode.Ignore });
                Text(done, complete + (complete == 1 ? " goal complete" : " goals complete"), "gem-note");
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
