namespace IdleClinic.Progression
{
    public interface ICostPolicy
    {
        /// <summary>The price of the upgrade after <paramref name="upgradesAlreadyBought"/> others.</summary>
        long CostOfUpgrade(int upgradesAlreadyBought);
    }

    /// <summary>Every upgrade costs double the one before it, whichever room it is for.</summary>
    public sealed class DoublingCostPolicy : ICostPolicy
    {
        private readonly long baseCost;
        public DoublingCostPolicy(long baseCost) { this.baseCost = baseCost; }

        public long CostOfUpgrade(int upgradesAlreadyBought)
        {
            var cost = baseCost;
            for (var i = 0; i < upgradesAlreadyBought; i++)
            {
                if (cost > long.MaxValue / 2) return long.MaxValue;
                cost *= 2;
            }
            return cost;
        }
    }
}
