using System;
using System.Globalization;
using IdleClinic.Core;
using IdleClinic.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>Every gem spend opens this sheet first: what it gives, how long it lasts, the price and the balance after.
    /// Nothing is spent until Spend is tapped. The sheet covers the world and panels, so scrolling, pinching or a second
    /// tap underneath cannot buy anything, and the tap that opened it cannot also confirm it.</summary>
    public sealed partial class ClinicApp
    {
        private const double GemConfirmGuardSeconds = .35;
        private ClinicGemSpendFlow gemFlow;
        private VisualElement gemConfirm, gemConfirmCard;
        // The facts scroll inside the card when a compact screen cannot fit them; heading and actions stay in view.
        private ScrollView gemConfirmBody;
        private string gemConfirmBanner;
        private double gemConfirmOpenedAt, gemConfirmRefreshAt;
        private bool gemConfirmWorking;

        private bool GemConfirmOpen => ClinicModalPolicy.IsOpen(gemConfirm);
        /// <summary>True when a control outside the open sheet must not act (defence in depth behind the backdrop and masking).</summary>
        private bool ModalBlocks(VisualElement element) => GemConfirmOpen && !ClinicModalPolicy.Allows(gemConfirm, element);

        private void BuildGemConfirm()
        {
            gemFlow = new ClinicGemSpendFlow((kind, key) => saves.QuoteGemSpend(kind, key, DateTimeOffset.UtcNow),
                (quote, maxGems) => saves.SpendGemsFor(quote.Kind, quote.Key, maxGems, DateTimeOffset.UtcNow, quote.Location));
            gemConfirm = Box(root, "gem-confirm"); gemConfirm.name = "gem-confirm";
            // Tapping the dimmed area around the card cancels; it never confirms.
            gemConfirm.RegisterCallback<ClickEvent>(e => { if (e.target == gemConfirm) CancelGemSpend(); });
            gemConfirmCard = Box(gemConfirm, "gem-confirm-card"); gemConfirmCard.name = "gem-confirm-card";
            gemConfirm.style.display = DisplayStyle.None;
        }

        /// <summary>Show what a gem spend would do. A free last-minute finish spends nothing and happens straight away.</summary>
        private void RequestGemSpend(ClinicGemSpendKind kind, string key)
        {
            if (saves == null || gemFlow == null) return;
            if (saves.HasPendingOfflineProgress) { Notify("Saving your return first…"); return; }
            var quote = gemFlow.Begin(kind, key);
            if (!quote.Available) { gemFlow.Cancel(); Notify(quote.Unavailable, 5); return; }
            if (!quote.NeedsConfirmation) { FinishGemSpend(gemFlow.Confirm(gemFlow.Token, out var current), current); return; }
            CancelWorldGesture();
            gemConfirmOpenedAt = Time.unscaledTimeAsDouble;
            gemConfirmRefreshAt = gemConfirmOpenedAt + .5;
            RenderGemConfirm(null);
            gemConfirm.style.display = DisplayStyle.Flex;
            ApplySafeArea();
            // Screen readers move to the sheet; everything behind it is masked until it closes.
            AnnounceScreenChange(true);
        }

        private void CancelGemSpend()
        {
            gemFlow?.Cancel();
            HideGemConfirm();
        }

        private void HideGemConfirm()
        {
            if (gemConfirm == null) return;
            var wasOpen = GemConfirmOpen;
            gemConfirm.style.display = DisplayStyle.None;
            gemConfirmCard.Clear(); gemConfirmBody = null; gemConfirmBanner = null;
            if (wasOpen) AnnounceScreenChange(false);
        }

        /// <summary>An interruption (backgrounding, losing focus, changing clinic) withdraws an open confirmation: when the
        /// player returns, nothing can be confirmed against a clinic or price they have not looked at again.</summary>
        private void WithdrawGemConfirm()
        {
            if (!GemConfirmOpen) return;
            CancelGemSpend();
        }

        /// <summary>Keep the open sheet true: a falling finish price is shown; work that completed on its own closes it.</summary>
        private void UpdateGemConfirm()
        {
            if (!GemConfirmOpen || gemConfirmWorking || Time.unscaledTimeAsDouble < gemConfirmRefreshAt) return;
            gemConfirmRefreshAt = Time.unscaledTimeAsDouble + .5;
            var before = gemFlow.Pending?.Gems ?? 0;
            switch (gemFlow.Refresh())
            {
                case ClinicGemSpendRefresh.NothingOpen: HideGemConfirm(); break;
                case ClinicGemSpendRefresh.NoLongerAvailable: HideGemConfirm(); Notify("That offer has changed or finished. No gems were used.", 5); break;
                case ClinicGemSpendRefresh.PriceRose: RenderGemConfirm("The price is now " + Gems(gemFlow.Pending.Gems) + ". Check it, then confirm again."); break;
                case ClinicGemSpendRefresh.PriceFell: RenderGemConfirm("The price fell from " + Gems(before) + "."); break;
                // Same price, but a shown fact changed: time left on a boost, or the balance after a delivered purchase.
                case ClinicGemSpendRefresh.DetailsChanged: RenderGemConfirm(gemConfirmBanner); break;
            }
        }

        private void RenderGemConfirm(string banner)
        {
            var quote = gemFlow.Pending;
            gemConfirmCard.Clear();
            if (quote == null) { HideGemConfirm(); return; }
            var token = gemFlow.Token;
            gemConfirmBanner = banner;

            var heading = Box(gemConfirmCard, "gem-confirm-heading");
            var tile = Box(heading, "icon-tile gem-confirm-tile"); tile.pickingMode = PickingMode.Ignore; tile.name = "gem-confirm-badge";
            // The second builder shows the builder himself (a small original icon matching the Store's builder card).
            var builder = quote.Kind == ClinicGemSpendKind.ExtraBuilder ? Resources.Load<Texture2D>("Shop/BuilderBadge") : null;
            if (builder != null)
            {
                tile.AddToClassList("gem-confirm-builder");
                var art = new Image { image = builder, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                art.AddToClassList("gem-confirm-builder-art"); tile.Add(art);
            }
            else tile.Add(new ClinicIcon(GlyphFor(quote.Kind), 26, GemInk));
            var titles = Box(heading, "gem-confirm-titles"); titles.pickingMode = PickingMode.Ignore;
            Text(titles, "CONFIRM GEM SPEND", "eyebrow", true);
            Display(Text(titles, quote.Title, "gem-confirm-title", true));
            IconButton(heading, ClinicGlyph.Close, "Cancel, spend nothing", CancelGemSpend, "round-control close-control").name = "gem-confirm-close";

            gemConfirmBody = new ScrollView(ScrollViewMode.Vertical) { name = "gem-confirm-body", horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            gemConfirmBody.AddToClassList("gem-confirm-body"); gemConfirmCard.Add(gemConfirmBody);
            BindTouchCaptureLifecycle(gemConfirmBody.contentContainer); BindTouchCaptureLifecycle(gemConfirmBody.contentViewport);
            var body = gemConfirmBody.contentContainer;
            if (!string.IsNullOrEmpty(banner)) Text(body, banner, "gem-confirm-banner", true).name = "gem-confirm-banner";
            var facts = Box(body, "gem-confirm-facts"); facts.pickingMode = PickingMode.Ignore;
            Fact(facts, ClinicGlyph.Check, "You get", quote.Benefit);
            Fact(facts, ClinicGlyph.Clock, "Lasts", quote.Duration);
            if (!string.IsNullOrEmpty(quote.Detail)) Text(body, quote.Detail, "gem-confirm-detail");

            var cost = Box(body, "gem-confirm-cost"); cost.pickingMode = PickingMode.Ignore;
            var price = Box(cost, "gem-confirm-price"); price.pickingMode = PickingMode.Ignore;
            price.Add(new ClinicIcon(ClinicGlyph.Gem, 22, GemInk));
            Display(Text(price, quote.Gems.ToString("N0", CultureInfo.InvariantCulture), "gem-confirm-amount", true)).name = "gem-confirm-amount";
            Text(cost, quote.Shortfall > 0 ? "You have " + Gems(quote.Balance) + ". You need " + Gems(quote.Shortfall) + " more."
                : "You have " + Gems(quote.Balance) + " · " + Gems(quote.Balance - quote.Gems) + " after", "gem-confirm-balance").name = "gem-confirm-balance";

            var actions = Box(gemConfirmCard, "gem-confirm-actions");
            var cancel = IconButton(actions, ClinicGlyph.Close, "Cancel, spend nothing", CancelGemSpend, "gem-confirm-cancel");
            cancel.name = "gem-confirm-cancel"; cancel.Q<ClinicIcon>().RemoveFromHierarchy();
            Text(cancel, "Cancel", "gem-confirm-cancel-label", true);
            if (quote.Shortfall > 0)
            {
                // Short of gems: the free route first, then the shop. Nothing can be spent from here.
                var earn = IconButton(actions, ClinicGlyph.Goal, "Earn free gems from today's goals", () => { CancelGemSpend(); OpenGemTab(GemTab.Today); }, "gem-confirm-earn");
                earn.name = "gem-confirm-earn"; earn.Q<ClinicIcon>().Tint = LeafInk;
                Text(earn, "Earn free gems", "gem-confirm-earn-label", true);
                var shop = IconButton(body, ClinicGlyph.Gem, "See gem packs", () => { CancelGemSpend(); OpenShop(); }, "text-link gem-confirm-shop");
                shop.name = "gem-confirm-shop"; shop.Q<ClinicIcon>().RemoveFromHierarchy();
                Text(shop, "See gem packs", "text-link-label", true);
            }
            else
            {
                var label = "Spend " + Gems(quote.Gems);
                var spend = IconButton(actions, ClinicGlyph.Gem, label + " on " + quote.Title, () => ConfirmGemSpend(token), "gem-confirm-spend");
                spend.name = "gem-confirm-spend"; spend.Q<ClinicIcon>().Tint = GemOnInk;
                Text(spend, label, "gem-confirm-spend-label", true);
            }
            Unstyle(gemConfirmCard);
            LimitGemConfirm();
        }

        /// <summary>Fit the sheet inside the safe area: the facts scroll if they must, Cancel and Spend never leave the screen.</summary>
        private float gemConfirmMaxHeight = 600;
        private void LimitGemConfirm()
        {
            if (gemConfirmCard == null) return;
            var cardMax = Math.Max(240f, gemConfirmMaxHeight);
            gemConfirmCard.style.maxHeight = cardMax;
            // Heading, actions and card padding take about 170 points; the rest is for the facts.
            if (gemConfirmBody != null) gemConfirmBody.style.maxHeight = Math.Max(96f, cardMax - 170f);
        }

        /// <summary>A screen reader hears the whole quote, in the order the sheet shows it.</summary>
        private string GemConfirmSummary() => gemFlow?.Pending == null ? "" : (gemConfirmBanner == null ? "" : gemConfirmBanner + " ") + gemFlow.Pending.Summary;

        private void Fact(VisualElement parent, ClinicGlyph glyph, string label, string value)
        {
            var row = Box(parent, "gem-confirm-fact"); row.pickingMode = PickingMode.Ignore;
            row.Add(new ClinicIcon(glyph, 18, LeafInk));
            var words = Box(row, "gem-confirm-fact-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, label, "gem-confirm-fact-label", true);
            Text(words, value, "gem-confirm-fact-value");
        }

        private void ConfirmGemSpend(int token)
        {
            // The tap that opened the sheet, or a double tap, must not also confirm it.
            if (gemConfirmWorking || Time.unscaledTimeAsDouble - gemConfirmOpenedAt < GemConfirmGuardSeconds) return;
            gemConfirmWorking = true;
            try { FinishGemSpend(gemFlow.Confirm(token, out var current), current); }
            finally { gemConfirmWorking = false; }
        }

        private void FinishGemSpend(ClinicGemSpendOutcome outcome, ClinicGemQuote quote)
        {
            switch (outcome)
            {
                case ClinicGemSpendOutcome.Spent:
                    HideGemConfirm();
                    RebindAfterCommit();
                    Feedback(1);
                    ShowGemSpendResult(quote);
                    break;
                case ClinicGemSpendOutcome.PriceChanged:
                    RenderGemConfirm("The price is now " + Gems(quote.Gems) + ". Nothing was spent. Check it, then confirm again.");
                    break;
                case ClinicGemSpendOutcome.NotEnoughGems:
                    RenderGemConfirm(null);
                    break;
                case ClinicGemSpendOutcome.NotSaved:
                    RenderGemConfirm((saves.Error ?? "Your clinic could not be saved.") + " No gems were used. Try again.");
                    break;
                case ClinicGemSpendOutcome.Unavailable:
                    HideGemConfirm();
                    Notify("No gems were used. " + quote?.Unavailable, 6);
                    dockKey = ""; UpdateReadouts();
                    break;
            }
        }

        private void ShowGemSpendResult(ClinicGemQuote quote)
        {
            switch (quote.Kind)
            {
                case ClinicGemSpendKind.FinishWork:
                {
                    clinicAudio.PlayEvent(ClinicEventKind.ConstructionCompleted);
                    var room = LastFinishedRoom();
                    if (room.HasValue) { Celebrate(world.GetRoomPoint(room.Value), 2.6f, "Room ready!"); ShowRoomReady(room.Value); }
                    RefreshWelcomeJobs();
                    break;
                }
                case ClinicGemSpendKind.Decoration:
                    clinicAudio.PlayReward();
                    if (Enum.TryParse<ClinicRoom>(quote.Key, out var decorated)) Celebrate(world.GetRoomPoint(decorated), 1.8f, "Decorated!");
                    Notify(quote.Benefit, 5);
                    break;
                case ClinicGemSpendKind.CoinPack:
                    clinicAudio.PlayReward();
                    LaunchCoinsFrom(world.GetRoomPoint(ClinicRoom.Reception));
                    Notify(quote.Benefit, 5);
                    break;
                case ClinicGemSpendKind.ExtraBuilder:
                    clinicAudio.PlayReward();
                    Notify("Your second builder is ready. Two rooms can now build at once.", 5);
                    break;
                default:
                    clinicAudio.PlayReward();
                    Notify(quote.Title + ": " + quote.Duration.ToLowerInvariant(), 5);
                    break;
            }
        }

        private ClinicRoom? lastFinishRequestRoom;
        private ClinicRoom? LastFinishedRoom() => lastFinishRequestRoom;

        private void OpenGemTab(GemTab tab)
        {
            gemTab = tab; shopExtrasOpen = false;
            if (!gemsOpen) ToggleGems(); else { dockKey = ""; UpdateReadouts(); }
        }

        private static ClinicGlyph GlyphFor(ClinicGemSpendKind kind) => kind == ClinicGemSpendKind.CoinPack ? ClinicGlyph.Coin
            : kind == ClinicGemSpendKind.Boost ? ClinicGlyph.Bolt : kind == ClinicGemSpendKind.OfflineUpgrade ? ClinicGlyph.Clock
            : kind == ClinicGemSpendKind.Decoration ? ClinicGlyph.Plant : ClinicGlyph.Upgrade;

        private static string Gems(long value) => value.ToString("N0", CultureInfo.InvariantCulture) + (value == 1 ? " gem" : " gems");
    }
}
