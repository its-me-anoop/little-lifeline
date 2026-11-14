using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>A quick app switch is not "time away": only real absences are reported to the player.</summary>
    public sealed class ClinicAwayPolicyTests
    {
        [TestCase(0.0, ClinicAwayReport.None)]
        [TestCase(12.0, ClinicAwayReport.None)]
        [TestCase(29.9, ClinicAwayReport.None)]
        [TestCase(30.0, ClinicAwayReport.Line)]
        [TestCase(119.0, ClinicAwayReport.Line)]
        [TestCase(120.0, ClinicAwayReport.Card)]
        [TestCase(36000.0, ClinicAwayReport.Card)]
        public void ReportDependsOnHowLongThePlayerWasAway(double seconds, ClinicAwayReport expected)
            => Assert.That(ClinicAwayPolicy.For(seconds), Is.EqualTo(expected));
    }
}
