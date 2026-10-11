using System.Text.RegularExpressions;

namespace IdleClinic.Services
{
    /// <summary>The app identity a local QA simulator preview may use instead of the App Store one. Only the simulator QA
    /// export applies it; release, TestFlight and device builds always use <see cref="Production"/>.</summary>
    public static class ClinicQaIdentity
    {
        public const string Production = "com.flutterly.gravitile";
        private static readonly Regex Allowed = new Regex("^com\\.flutterly\\.gravitile\\.(qa|review|preview)[a-z0-9]*$");

        /// <summary>The requested QA identity if it is a separate, well-formed QA identifier; otherwise null (use production).</summary>
        public static string Validate(string requested)
            => string.IsNullOrEmpty(requested) || requested == Production || !Allowed.IsMatch(requested) ? null : requested;
    }
}
