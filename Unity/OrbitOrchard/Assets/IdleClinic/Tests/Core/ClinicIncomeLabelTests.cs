using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The income readout is short, honest and silent until there is a rate worth showing.</summary>
    public sealed class ClinicIncomeLabelTests
    {
        [TestCase(0.0, "")]
        [TestCase(-3.0, "")]
        [TestCase(double.NaN, "")]
        [TestCase(0.004, "")]
        [TestCase(0.05, "+3/min")]
        [TestCase(0.5, "+30/min")]
        [TestCase(1.0, "+1/s")]
        [TestCase(4.24, "+4.2/s")]
        [TestCase(9.96, "+10/s")]
        [TestCase(37.4, "+37/s")]
        [TestCase(1250.0, "+1.3K/s")]
        [TestCase(2400000.0, "+2.4M/s")]
        public void FormatsTheRate(double perSecond, string expected) => Assert.That(ClinicIncomeLabel.Format(perSecond), Is.EqualTo(expected));
    }
}
