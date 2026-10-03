using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class CostPolicyTests
    {
        [Test]
        public void EveryUpgradeCostsDoubleThePreviousOne()
        {
            ICostPolicy policy = new DoublingCostPolicy(10);
            Assert.AreEqual(10, policy.CostOfUpgrade(0));
            Assert.AreEqual(20, policy.CostOfUpgrade(1));
            Assert.AreEqual(40, policy.CostOfUpgrade(2));
            Assert.AreEqual(80, policy.CostOfUpgrade(3));
        }

        [Test]
        public void CostsStopGrowingInsteadOfOverflowing()
        {
            ICostPolicy policy = new DoublingCostPolicy(10);
            Assert.AreEqual(long.MaxValue, policy.CostOfUpgrade(200));
            Assert.Greater(policy.CostOfUpgrade(60), policy.CostOfUpgrade(59));
        }

        [Test]
        public void BaseCostComesFromTheConstructor()
        {
            Assert.AreEqual(300, new DoublingCostPolicy(75).CostOfUpgrade(2));
        }
    }
}
