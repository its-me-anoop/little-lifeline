using System;
using System.Globalization;

namespace IdleClinic.Core
{
    /// <summary>Formats how fast the clinic is earning for the heads-up display: "+4.2/s", or per minute while it is slow.</summary>
    public static class ClinicIncomeLabel
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string Format(double coinsPerSecond)
        {
            if (double.IsNaN(coinsPerSecond) || double.IsInfinity(coinsPerSecond) || coinsPerSecond < .01) return "";
            if (coinsPerSecond < 1) return "+" + Math.Max(1, (long)Math.Round(coinsPerSecond * 60)).ToString(Culture) + "/min";
            return "+" + Compact(coinsPerSecond) + "/s";
        }

        private static string Compact(double value)
        {
            if (value >= 1000000) return Trim(value / 1000000, "M");
            if (value >= 1000) return Trim(value / 1000, "K");
            if (value >= 9.95) return Math.Round(value).ToString("0", Culture);
            return Trim(value, "");
        }

        private static string Trim(double value, string suffix)
        {
            var text = value.ToString("0.0", Culture);
            if (text.EndsWith(".0", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 2);
            return text + suffix;
        }
    }
}
