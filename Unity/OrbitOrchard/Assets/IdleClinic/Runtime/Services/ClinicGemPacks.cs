using System;
using System.Collections.Generic;

namespace IdleClinic.Services
{
    /// <summary>Permanent upgrades. The extra builder lets two rooms build or renovate at once.</summary>
    public static class ClinicUnlocks
    {
        public const string ExtraBuilder = "builder.extra";
        public const string ExtraBuilderProduct = "com.flutterly.gravitile.builder";
        public const long ExtraBuilderGems = 400;
    }

    /// <summary>Things gems buy in the shop. Coin packs scale with the active clinic's visit fee.</summary>
    public static class ClinicShopOffers
    {
        public sealed class CoinPack { public string Id, Name; public long Gems; public int Visits; }
        public sealed class Boost { public string Id, Name; public long Gems; public int Hours; }
        public sealed class OfflineTier { public string Id, Name; public long Gems; public int Hours; public double CoinMultiplier; }
        public static readonly CoinPack[] CoinPacks =
        {
            new CoinPack { Id = "coins.bag", Name = "Bag of coins", Gems = 20, Visits = 60 },
            new CoinPack { Id = "coins.chest", Name = "Chest of coins", Gems = 60, Visits = 200 },
            new CoinPack { Id = "coins.vault", Name = "Vault of coins", Gems = 150, Visits = 550 },
        };
        public static readonly Boost[] Boosts =
        {
            new Boost { Id = "boost.1h", Name = "Double collections · 1 hour", Gems = 30, Hours = 1 },
            new Boost { Id = "boost.4h", Name = "Double collections · 4 hours", Gems = 90, Hours = 4 },
        };
        /// <summary>Longer absences, and tills that hold more coins while you're away.</summary>
        public static readonly OfflineTier[] OfflineTiers =
        {
            new OfflineTier { Id = "offline.12h", Name = "12-hour offline earnings", Gems = 150, Hours = 12, CoinMultiplier = 1.5 },
            new OfflineTier { Id = "offline.24h", Name = "24-hour offline earnings", Gems = 400, Hours = 24, CoinMultiplier = 3 },
        };
        /// <summary>Spends at or above this ask for a second tap.</summary>
        public const long ConfirmAtGems = 50;
    }

    /// <summary>App Store gem packs. Product ids are permanent; change only the amounts of packs not yet sold.</summary>
    public static class ClinicGemPacks
    {
        public const string Small = "com.flutterly.gravitile.gems.small";
        public const string Medium = "com.flutterly.gravitile.gems.medium";
        public const string Large = "com.flutterly.gravitile.gems.large";
        public const string ExtraLarge = "com.flutterly.gravitile.gems.xl";

        private static readonly Dictionary<string, long> amounts = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            [Small] = 80,
            [Medium] = 450,
            [Large] = 1000,
            [ExtraLarge] = 2800
        };

        public static IReadOnlyDictionary<string, long> Amounts => amounts;
        public static bool IsGemPack(string productId) => productId != null && amounts.ContainsKey(productId);
        public static long GemsFor(string productId) => productId != null && amounts.TryGetValue(productId, out var gems) ? gems : 0;

        /// <summary>Record one delivered transaction. Returns true when StoreKit may finish it.</summary>
        public static bool Apply(ClinicProfileStore store, string transactionId, string productId, bool revoked, DateTimeOffset now, out ClinicPurchaseGrant outcome)
        {
            var gems = GemsFor(productId);
            outcome = revoked ? store.RevokePurchase(transactionId, gems, now) : store.GrantPurchase(transactionId, gems, now);
            return outcome == ClinicPurchaseGrant.Granted || outcome == ClinicPurchaseGrant.AlreadyGranted || outcome == ClinicPurchaseGrant.Refunded;
        }
    }
}
