namespace IdleClinic.Core
{
    public enum ClinicAwayReport { None, Line, Card }

    /// <summary>How to tell the player about time away: nothing for a quick app switch, a line for a short break, the full card after longer.</summary>
    public static class ClinicAwayPolicy
    {
        public const double LineSeconds = 30;
        public const double CardSeconds = 120;

        public static ClinicAwayReport For(double elapsedSeconds)
            => elapsedSeconds < LineSeconds ? ClinicAwayReport.None : elapsedSeconds < CardSeconds ? ClinicAwayReport.Line : ClinicAwayReport.Card;
    }
}
