using System;
using System.Globalization;
using IdleClinic.Core;

namespace IdleClinic.Services
{
    /// <summary>Gem spending with an explicit, confirmed price. Every quote states the amount, the exact benefit and how long it
    /// lasts; every spend refuses a price above the one the player confirmed and commits atomically or not at all.</summary>
    public sealed partial class ClinicProfileStore
    {
        public ClinicGemQuote QuoteGemSpend(ClinicGemSpendKind kind, string key, DateTimeOffset now)
        {
            var q = new ClinicGemQuote { Kind = kind, Key = key, Balance = Profile?.premium?.gems ?? 0 };
            if (Profile?.premium == null) return Unavailable(q, "Your clinic is still loading.");
            q.Location = Profile.activeLocation;
            var active = Profile.ActiveState;
            var place = ClinicRules.IsDoctors(active) ? "doctors clinic" : "starter clinic";
            switch (kind)
            {
                case ClinicGemSpendKind.CoinPack:
                {
                    var pack = Array.Find(ClinicShopOffers.CoinPacks, p => p.Id == key);
                    if (pack == null) return Unavailable(q, "That pack is not available.");
                    var fee = Math.Max(50, ClinicRules.VisitFee(active));
                    q.Title = pack.Name; q.Gems = pack.Gems;
                    q.Benefit = "+" + N(CoinPackAmount(pack)) + " coins in your " + place + " wallet";
                    q.Duration = "Instant · one time";
                    q.Detail = "Sized to your visit fee now: " + pack.Visits + " visits at " + N(fee) + " coins.";
                    break;
                }
                case ClinicGemSpendKind.Boost:
                {
                    var boost = Array.Find(ClinicShopOffers.Boosts, b => b.Id == key);
                    if (boost == null) return Unavailable(q, "That boost is not available.");
                    var left = BoostRemaining(now);
                    q.Title = "Double collections"; q.Gems = boost.Gems;
                    q.Benefit = "Every coin you collect is matched once more";
                    q.Duration = boost.Hours + (boost.Hours == 1 ? " hour" : " hours") + " of real time from now"
                        + (left > TimeSpan.Zero ? ", added to the " + Clock(left.TotalSeconds) + " already left" : "");
                    q.Detail = "It counts down even while the app is closed. Coins collected before it starts are not doubled.";
                    break;
                }
                case ClinicGemSpendKind.OfflineUpgrade:
                {
                    var tier = Array.Find(ClinicShopOffers.OfflineTiers, o => o.Id == key);
                    if (tier == null) return Unavailable(q, "That upgrade is not available.");
                    if (OfflineLimitHours >= tier.Hours) return Unavailable(q, "You already earn for " + OfflineLimitHours + " hours while away.");
                    var capAfter = ClinicRules.OfflineCoinCap(active, Math.Max(OfflineCoinMultiplier, tier.CoinMultiplier));
                    q.Title = tier.Name; q.Gems = tier.Gems;
                    q.Benefit = "Earn for up to " + tier.Hours + " hours away (now " + OfflineLimitHours + "). Counters hold " + N(capAfter) + " coins (now " + N(OfflineCoinCap) + ")";
                    q.Duration = "Permanent · every clinic";
                    q.Detail = "Building always continues for your whole absence, with or without this.";
                    break;
                }
                case ClinicGemSpendKind.ExtraBuilder:
                    if (HasUnlock(ClinicUnlocks.ExtraBuilder)) return Unavailable(q, "Your second builder is already working for you.");
                    q.Title = "Second builder"; q.Gems = ClinicUnlocks.ExtraBuilderGems;
                    q.Benefit = "Build or renovate two rooms at the same time";
                    q.Duration = "Permanent · every clinic";
                    q.Detail = "Also offered on the App Store. You only need it once.";
                    break;
                case ClinicGemSpendKind.Decoration:
                {
                    if (!Enum.TryParse<ClinicRoom>(key, out var room) || !Enum.IsDefined(typeof(ClinicRoom), room) || active.Room(room) == null || !active.Room(room).Built)
                        return Unavailable(q, "That room cannot be decorated.");
                    var gems = ClinicRules.DecorationGemCost(active, room);
                    if (gems <= 0) return Unavailable(q, "This room is fully decorated.");
                    // The exact fees, with the game's own rounding: the added amount is stated in coins, never as a percentage.
                    var before = ClinicRules.VisitFee(active);
                    var after = ClinicStats.VisitFeeAfterDecor(active, room);
                    q.Title = "Decor for " + RoomLabel(room); q.Gems = gems;
                    q.Benefit = "Visit fee " + N(before) + " → " + N(after) + " coins (+" + N(after - before) + (after - before == 1 ? " coin" : " coins") + " a visit)";
                    q.Duration = "Permanent";
                    q.Detail = "Optional. Nothing in the clinic needs decor.";
                    break;
                }
                case ClinicGemSpendKind.FinishWork:
                {
                    var job = int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? active.Construction.Find(c => c.Id == id) : null;
                    if (job == null) return Unavailable(q, "That work has already finished.");
                    var room = active.Room(job.Room);
                    q.Title = "Finish " + (room.Built ? RoomLabel(job.Room) + " renovation" : "the " + RoomLabel(job.Room).ToLowerInvariant()) + " now";
                    q.Gems = ClinicPremiumRules.SkipCost(active, job);
                    q.Benefit = room.Built ? "Room " + job.TargetTier + " is ready now, exactly as if you waited" : "Ready now, exactly as if you waited";
                    q.Duration = "Saves " + Clock(ClinicPremiumRules.RemainingSeconds(active, job));
                    q.Detail = "The price falls as the timer runs. The last minute is free.";
                    break;
                }
                default: return Unavailable(q, "That is not available.");
            }
            q.Available = true;
            return q;
        }

        /// <summary>Spend gems for a confirmed offer, at no more than <paramref name="maxGems"/>. Nothing is spent unless the
        /// result is durably saved.</summary>
        public bool SpendGemsFor(ClinicGemSpendKind kind, string key, long maxGems, DateTimeOffset now, ClinicLocation? quotedIn = null)
        {
            var quote = QuoteGemSpend(kind, key, now);
            if (!quote.Available) { Error = quote.Unavailable; return false; }
            if (quotedIn.HasValue && quotedIn.Value != quote.Location) { Error = "You moved to another clinic. Nothing was spent."; return false; }
            if (quote.Gems > maxGems) { Error = "The price is now " + quote.Gems + " gems. Nothing was spent."; return false; }
            switch (kind)
            {
                case ClinicGemSpendKind.CoinPack: return BuyCoinPack(key, now);
                case ClinicGemSpendKind.Boost: return BuyBoost(key, now);
                case ClinicGemSpendKind.OfflineUpgrade: return BuyOfflineUpgrade(key, now);
                case ClinicGemSpendKind.ExtraBuilder: return UnlockExtraBuilderWithGems(now);
                case ClinicGemSpendKind.Decoration: return BuyDecoration((ClinicRoom)Enum.Parse(typeof(ClinicRoom), key), now);
                case ClinicGemSpendKind.FinishWork: return SkipConstruction(int.Parse(key, CultureInfo.InvariantCulture), now);
                default: Error = "That is not available."; return false;
            }
        }

        private static ClinicGemQuote Unavailable(ClinicGemQuote quote, string reason)
        {
            quote.Available = false; quote.Unavailable = reason; quote.Title = quote.Title ?? ""; quote.Benefit = quote.Benefit ?? ""; quote.Duration = quote.Duration ?? "";
            return quote;
        }

        private static string N(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private static string RoomLabel(ClinicRoom room) => room == ClinicRoom.FirstAid ? "First aid" : room == ClinicRoom.Waiting ? "Waiting room"
            : room == ClinicRoom.Consultation ? "Consultations" : room.ToString();

        private static string Clock(double seconds)
        {
            if (seconds < 60) return Math.Ceiling(Math.Max(0, seconds)) + "s";
            var minutes = (long)Math.Ceiling(seconds / 60);
            if (minutes < 60) return minutes + "m";
            return minutes % 60 == 0 ? minutes / 60 + "h" : minutes / 60 + "h " + minutes % 60 + "m";
        }
    }
}
