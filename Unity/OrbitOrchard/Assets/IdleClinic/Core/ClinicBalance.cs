using System;

namespace IdleClinic.Core
{
    /// <summary>A cost multiplier applied once per owned level: basis * (Numerator / Denominator)^level.</summary>
    public readonly struct ClinicGrowth
    {
        public long Numerator { get; }
        public long Denominator { get; }
        public ClinicGrowth(long numerator, long denominator) { Numerator = numerator; Denominator = denominator; }
    }

    /// <summary>Economy knobs for one rules version. Owned levels are stored progress; a table only prices
    /// the next purchase, service or payment, so a new version never takes anything a player already has.</summary>
    public sealed class ClinicBalanceTable
    {
        public int RulesVersion { get; }

        public long VisitFeeBase { get; }
        public int FirstAidFacilitiesFeePercent { get; }
        public int DecorationFeePercent { get; }
        public int ConsultationFacilitiesFeePercent { get; }
        public int PharmacyFacilitiesFeePercent { get; }
        public long ParkingFeePerLevel { get; }
        public long VendingTipPerLevel { get; }
        public long ToiletTipPerLevel { get; }

        public long WaitingRoomCost { get; }
        public int WaitingRoomBuildSeconds { get; }
        public long DoctorsClinicUnlockCost { get; }

        // Indexed by ClinicStaffRole.
        public long[] HireBase { get; }
        public ClinicGrowth ReceptionistHireGrowth { get; }
        public ClinicGrowth HireGrowth { get; }
        public long[] AddStationBase { get; }
        public ClinicGrowth AddStationGrowth { get; }
        public long[] StationUpgradeBase { get; }
        public ClinicGrowth StationUpgradeGrowth { get; }
        public long[] TrainingBase { get; }
        public ClinicGrowth TrainingGrowth { get; }
        public int[] ServiceBaseTicks { get; }
        public int RoomEquipmentSpeedPercent { get; }
        public int StationEquipmentSpeedPercent { get; }
        public int TrainingSpeedPercent { get; }

        // Indexed by ClinicRoom, then UpgradeTrack.
        public long[,] UpgradeBase { get; }
        public ClinicGrowth UpgradeGrowth { get; }
        public long[] RenovationBase { get; }
        public ClinicGrowth RenovationGrowth { get; }
        public int RenovationBaseSeconds { get; }
        public ClinicGrowth RenovationTimeGrowth { get; }

        // Indexed by ClinicAmenity.
        public long[] AmenityBase { get; }
        public ClinicGrowth AmenityGrowth { get; }

        private ClinicBalanceTable(int rulesVersion)
        {
            RulesVersion = rulesVersion;
            VisitFeeBase = 50;
            FirstAidFacilitiesFeePercent = 15;
            DecorationFeePercent = 5;
            ConsultationFacilitiesFeePercent = 10;
            PharmacyFacilitiesFeePercent = 10;
            ParkingFeePerLevel = 5;
            VendingTipPerLevel = 5;
            ToiletTipPerLevel = 2;
            WaitingRoomCost = ClinicRules.WaitingRoomCost;
            WaitingRoomBuildSeconds = ClinicRules.WaitingRoomBuildSeconds;
            DoctorsClinicUnlockCost = ClinicRules.DoctorsClinicUnlockCost;
            HireBase = new long[] { 100, 50, 150, 100 };
            ReceptionistHireGrowth = new ClinicGrowth(3, 1);
            HireGrowth = new ClinicGrowth(9, 1);
            AddStationBase = new long[] { 0, 180, 240, 200 };
            AddStationGrowth = new ClinicGrowth(5, 2);
            StationUpgradeBase = new long[] { 90, 110, 150, 120 };
            StationUpgradeGrowth = new ClinicGrowth(9, 5);
            TrainingBase = new long[] { 80, 100, 140, 110 };
            TrainingGrowth = new ClinicGrowth(9, 5);
            ServiceBaseTicks = new[] { 140, 180, 240, 120 };
            RoomEquipmentSpeedPercent = 15;
            StationEquipmentSpeedPercent = 10;
            TrainingSpeedPercent = 12;
            UpgradeBase = new long[,]
            {
                { 60, 50, 40 },   // Reception
                { 80, 60, 40 },   // First aid
                { 35, 45, 40 },   // Waiting
                { 110, 90, 40 },  // Consultation
                { 95, 75, 40 }    // Pharmacy
            };
            UpgradeGrowth = new ClinicGrowth(8, 5);
            RenovationBase = new long[] { 180, 250, 120, 300, 220 };
            RenovationGrowth = new ClinicGrowth(5, 2);
            RenovationBaseSeconds = 60;
            RenovationTimeGrowth = new ClinicGrowth(3, 1);
            AmenityBase = new long[] { 220, 140, 180, 260 };
            AmenityGrowth = new ClinicGrowth(2, 1);
        }

        /// <summary>Rules 1–3 share these values; earlier saves are migrated before they are priced.</summary>
        public static readonly ClinicBalanceTable V3 = new ClinicBalanceTable(3);
    }

    public static class ClinicBalance
    {
        public static ClinicBalanceTable For(int rulesVersion)
        {
            if (rulesVersion >= 1 && rulesVersion <= 3) return ClinicBalanceTable.V3;
            throw new ArgumentOutOfRangeException(nameof(rulesVersion), rulesVersion, "No balance table exists for these rules.");
        }

        public static ClinicBalanceTable For(ClinicState state) => For(state?.RulesVersion ?? ClinicBalanceTable.V3.RulesVersion);
    }
}
