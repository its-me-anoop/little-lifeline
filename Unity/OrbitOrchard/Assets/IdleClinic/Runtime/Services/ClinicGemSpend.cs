using System;

namespace IdleClinic.Services
{
    /// <summary>Everything gems can buy. Each is priced and described by <see cref="ClinicProfileStore.QuoteGemSpend"/>.</summary>
    public enum ClinicGemSpendKind { CoinPack, Boost, OfflineUpgrade, ExtraBuilder, Decoration, FinishWork }

    /// <summary>What a gem spend costs right now and exactly what it gives, for the confirmation sheet.</summary>
    public sealed class ClinicGemQuote
    {
        public ClinicGemSpendKind Kind;
        /// <summary>Offer id, room name or construction job id.</summary>
        public string Key;
        public string Title;
        /// <summary>What the player receives, with numbers.</summary>
        public string Benefit;
        /// <summary>How long it lasts: instant, a stated real-time duration, or permanent.</summary>
        public string Duration;
        /// <summary>A short factual note: how it is sized, what happens while away, or the free alternative.</summary>
        public string Detail;
        public long Gems;
        public long Balance;
        public bool Available;
        public string Unavailable;
        public long Shortfall => Math.Max(0, Gems - Balance);
        /// <summary>Spending nothing (a free last-minute finish) needs no confirmation.</summary>
        public bool NeedsConfirmation => Gems > 0;
    }

    public enum ClinicGemSpendOutcome
    {
        Spent,
        /// <summary>No confirmation is open for this token: it was cancelled, replaced by another offer, or already used.</summary>
        Stale,
        /// <summary>The price went up; the sheet shows the new price and needs a fresh confirmation.</summary>
        PriceChanged,
        Unavailable,
        NotEnoughGems,
        /// <summary>The save could not be written. Nothing was spent; the same confirmation can be retried.</summary>
        NotSaved
    }

    public enum ClinicGemSpendRefresh { Unchanged, PriceFell, PriceRose, NoLongerAvailable, NothingOpen }

    /// <summary>One open gem confirmation at a time. Opening a different offer replaces it; each confirmation is a token that can
    /// spend at most once, never above the price the player saw. Engine-free, so its rules are tested without UI.</summary>
    public sealed class ClinicGemSpendFlow
    {
        private readonly Func<ClinicGemSpendKind, string, ClinicGemQuote> quote;
        private readonly Func<ClinicGemQuote, long, bool> spend;
        public ClinicGemQuote Pending { get; private set; }
        public int Token { get; private set; }
        public bool Busy { get; private set; }

        /// <param name="quote">Price and describe an offer from the current save.</param>
        /// <param name="spend">Durably spend for the offer at no more than the given price; false if nothing was spent.</param>
        public ClinicGemSpendFlow(Func<ClinicGemSpendKind, string, ClinicGemQuote> quote, Func<ClinicGemQuote, long, bool> spend)
        { this.quote = quote ?? throw new ArgumentNullException(nameof(quote)); this.spend = spend ?? throw new ArgumentNullException(nameof(spend)); }

        public ClinicGemQuote Begin(ClinicGemSpendKind kind, string key)
        {
            Token++;
            Pending = quote(kind, key);
            return Pending;
        }

        public void Cancel() { Pending = null; Token++; }

        /// <summary>Keep an open sheet truthful while it is shown: a falling finish price is passed on without asking again; a rise
        /// invalidates the confirmation; work that completed on its own closes the sheet with nothing spent.</summary>
        public ClinicGemSpendRefresh Refresh()
        {
            if (Pending == null || Busy) return ClinicGemSpendRefresh.NothingOpen;
            var current = quote(Pending.Kind, Pending.Key);
            if (!current.Available) { Pending = null; Token++; return ClinicGemSpendRefresh.NoLongerAvailable; }
            if (current.Gems > Pending.Gems) { Pending = current; Token++; return ClinicGemSpendRefresh.PriceRose; }
            var fell = current.Gems < Pending.Gems;
            Pending = current;
            return fell ? ClinicGemSpendRefresh.PriceFell : ClinicGemSpendRefresh.Unchanged;
        }

        public ClinicGemSpendOutcome Confirm(int token, out ClinicGemQuote current)
        {
            current = Pending;
            if (Busy || Pending == null || token != Token) return ClinicGemSpendOutcome.Stale;
            var seen = Pending;
            current = quote(seen.Kind, seen.Key);
            if (!current.Available) { Pending = null; Token++; return ClinicGemSpendOutcome.Unavailable; }
            if (current.Gems > seen.Gems) { Pending = current; Token++; return ClinicGemSpendOutcome.PriceChanged; }
            Pending = current;
            if (current.Shortfall > 0) return ClinicGemSpendOutcome.NotEnoughGems;
            Busy = true;
            try
            {
                if (!spend(current, seen.Gems)) return ClinicGemSpendOutcome.NotSaved;
                Pending = null; Token++;
                return ClinicGemSpendOutcome.Spent;
            }
            finally { Busy = false; }
        }
    }
}
