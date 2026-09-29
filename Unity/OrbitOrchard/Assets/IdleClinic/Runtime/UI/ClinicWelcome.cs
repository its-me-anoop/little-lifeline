using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>Moments that deserve more than a toast: the report after time away, a finished room,
    /// and a running speed-up. Each stays readable until it is no longer true or the player closes it.</summary>
    public sealed partial class ClinicApp
    {
        private VisualElement welcomeCard, banner;
        private Label bannerTitle, bannerDetail;
        private double bannerUntil;
        private Button boostChip;
        private Label boostTime;
        private readonly List<Action> welcomeReadouts = new List<Action>();
        private static readonly Color GemOnInk = new Color(.99f, .93f, .95f);
        private static readonly Color BuildInk = new Color(.48f, .35f, .12f);

        private void BuildWelcome()
        {
            banner = Box(root, "ready-banner"); banner.pickingMode = PickingMode.Ignore; banner.name = "clinic-room-ready";
            var badge = Box(banner, "ready-badge"); badge.pickingMode = PickingMode.Ignore;
            badge.Add(new ClinicIcon(ClinicGlyph.Check, 24, new Color(.18f, .42f, .31f)));
            var words = Box(banner, "ready-words"); words.pickingMode = PickingMode.Ignore;
            bannerTitle = Display(Text(words, "", "ready-title", true));
            bannerDetail = Text(words, "", "ready-detail");
            banner.style.display = DisplayStyle.None;
            welcomeCard = Box(root, "welcome-card"); welcomeCard.name = "clinic-welcome";
            welcomeCard.style.display = DisplayStyle.None;
        }

        private void LayoutWelcome(Rect safe, float top)
        {
            var width = Math.Min(480, safe.width - 32);
            var left = safe.xMin + (safe.width - width) / 2;
            foreach (var card in new[] { welcomeCard, banner })
            {
                if (card == null) continue;
                card.style.left = left; card.style.width = width; card.style.top = top + 76;
            }
            // A panel takes the player's attention; the report steps aside rather than stacking under it.
            if (welcomeCard != null && dock.style.display == DisplayStyle.Flex) HideWelcome();
        }

        /// <summary>After time away: what was earned, where it is, and any building still under way.</summary>
        private void ShowWelcome(ClinicOfflineReport report)
        {
            if (welcomeCard == null) return;
            welcomeCard.Clear(); welcomeReadouts.Clear();
            var heading = Box(welcomeCard, "welcome-heading");
            var titles = Box(heading, "welcome-titles"); titles.pickingMode = PickingMode.Ignore;
            Display(Text(titles, "Welcome back", "welcome-title", true));
            Text(titles, "Your clinic kept caring for " + TimeLabel(report.elapsedSeconds), "welcome-detail");
            IconButton(heading, ClinicGlyph.Close, "Close welcome back", HideWelcome, "round-control close-control").name = "close-welcome";

            var coins = Box(welcomeCard, "welcome-coins"); coins.pickingMode = PickingMode.Ignore;
            coins.Add(new ClinicIcon(ClinicGlyph.Coin, 34, CoinInk));
            var coinWords = Box(coins, "welcome-coin-words"); coinWords.pickingMode = PickingMode.Ignore;
            Display(Text(coinWords, report.tillEarned.ToString("N0", CultureInfo.InvariantCulture) + " coins", "welcome-amount", true));
            Text(coinWords, report.coinCapped ? "Your tills filled up. Tap the counters to collect, and hold more with offline upgrades in the shop."
                : "Waiting on the counters. Tap them to collect.", "welcome-note");

            foreach (var job in State.Construction.OrderBy(c => c.EndsTick).ToList())
            {
                var id = job.Id; var room = State.Room(job.Room);
                var row = Box(welcomeCard, "welcome-job"); row.pickingMode = PickingMode.Ignore;
                var tile = Box(row, "icon-tile icon-tile-small tile-gold"); tile.pickingMode = PickingMode.Ignore;
                tile.Add(new ClinicIcon(ClinicGlyph.Upgrade, 20, BuildInk));
                var words = Box(row, "welcome-job-words"); words.pickingMode = PickingMode.Ignore;
                Text(words, room.Built ? RoomName(job.Room) + " to room " + (room.Tier + 1) : "Building the " + RoomName(job.Room).ToLowerInvariant(), "welcome-job-title", true);
                var left = Text(words, "", "welcome-note");
                var finish = GemActionButton(row, "Finish " + RoomName(job.Room) + " now with gems", "Finish", () => { SkipConstruction(id); RefreshWelcomeJobs(); },
                    () => SkipCost(id), "gem-action", welcomeReadouts);
                welcomeReadouts.Add(() =>
                {
                    var current = State.Construction.Find(c => c.Id == id);
                    row.style.display = current == null ? DisplayStyle.None : DisplayStyle.Flex;
                    if (current != null) left.text = TimeLabel((current.EndsTick - State.Tick) / (double)ClinicRules.TicksPerSecond) + " left";
                });
            }
            Text(welcomeCard, report.wasCapped ? "Offline earnings stop after " + report.limitHours + " hours. Longer limits are in the shop."
                : "Offline: up to " + report.limitHours + " hours of earnings", "welcome-footnote");
            welcomeCard.style.display = DisplayStyle.Flex;
            Unstyle(welcomeCard);
            UpdateWelcome();
        }

        private void RefreshWelcomeJobs() => UpdateWelcome();

        private void HideWelcome()
        {
            if (welcomeCard == null) return;
            welcomeCard.style.display = DisplayStyle.None;
            welcomeReadouts.Clear();
        }

        private void UpdateWelcome()
        {
            foreach (var update in welcomeReadouts.ToArray()) update();
            if (banner != null && banner.style.display == DisplayStyle.Flex && Time.unscaledTimeAsDouble > bannerUntil)
                banner.style.display = DisplayStyle.None;
            UpdateBoostChip();
        }

        /// <summary>A finished room names what changed: its new size and the upgrade limit it opens.</summary>
        private void ShowRoomReady(ClinicRoom kind)
        {
            if (banner == null) { Notify(RoomName(kind) + " is ready"); return; }
            var room = State.Room(kind);
            bannerTitle.text = "Room ready!";
            bannerDetail.text = room == null ? RoomName(kind) + " is ready"
                : room.Tier <= 1 ? RoomName(kind) + " is open"
                : RoomName(kind) + " is now room " + room.Tier + " · upgrade limit " + ClinicRules.ComponentCap(State, kind);
            banner.style.display = DisplayStyle.Flex;
            bannerUntil = Time.unscaledTimeAsDouble + 4;
            HideWelcome();
        }

        // A running speed-up stays visible, with its time left; tapping it opens the shop to extend it.
        private void BuildBoostChip(VisualElement parent)
        {
            boostChip = IconButton(parent, ClinicGlyph.Bolt, "Double collections active", OpenShop, "hud-chip boost-chip",
                () => boostTime == null ? "" : boostTime.text + " left");
            boostChip.name = "clinic-boost";
            boostChip.Q<ClinicIcon>().Tint = GemInk;
            Text(boostChip, "2× collections", "chip-label", true);
            boostTime = Display(Text(boostChip, "", "boost-time", true));
            boostChip.style.display = DisplayStyle.None;
        }

        private void UpdateBoostChip()
        {
            if (boostChip == null) return;
            var now = DateTimeOffset.UtcNow;
            var active = saves != null && saves.BoostActive(now);
            boostChip.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active) return;
            var left = saves.BoostRemaining(now);
            boostTime.text = left.TotalHours >= 1 ? ((int)left.TotalHours) + ":" + left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00")
                : left.Minutes + ":" + left.Seconds.ToString("00");
        }

        private void OpenShop()
        {
            gemTab = GemTab.Shop; pendingConfirm = null;
            if (!gemsOpen) ToggleGems(); else { dockKey = ""; UpdateReadouts(); }
        }

        private long SkipCost(int jobId)
        {
            var job = State.Construction.Find(c => c.Id == jobId);
            return job == null ? 0 : ClinicPremiumRules.SkipCost(State, job);
        }

        /// <summary>A raspberry button whose price is the gem count: label, gem, amount.</summary>
        private Button GemActionButton(VisualElement parent, string accessibleLabel, string label, Action action, Func<long> gems, string classes, List<Action> updates)
        {
            var button = IconButton(parent, ClinicGlyph.Gem, accessibleLabel, action, classes, () => gems() + " gems");
            var icon = button.Q<ClinicIcon>(); icon.RemoveFromHierarchy();
            Text(button, label, "gem-action-label", true);
            var price = Box(button, "gem-action-price"); price.pickingMode = PickingMode.Ignore;
            icon.Tint = GemOnInk; icon.style.width = 16; icon.style.height = 16; price.Add(icon);
            var amount = Display(Text(price, "", "gem-action-amount", true));
            Action refresh = () => { var cost = gems(); amount.text = cost == 0 ? "Free" : cost.ToString("N0", CultureInfo.InvariantCulture); };
            refresh(); updates.Add(refresh);
            return button;
        }
    }
}
