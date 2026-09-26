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
        /// <summary>Rules 1–3: added to the reception payment of a patient who parked.</summary>
        public long ParkingFeePerLevel { get; }
        /// <summary>Rules 4: paid at the barrier as the car leaves, into the car park's own cash box.</summary>
        public long ParkingExitFeePerLevel { get; }
        public long VendingTipPerLevel { get; }
        public long ToiletTipPerLevel { get; }
        /// <summary>Offline coin limit, in visit fees; zero means unlimited.</summary>
        public int OfflineCapVisits { get; }

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
        /// <summary>Longest single construction; later tiers stop growing at this wait.</summary>
        public int MaximumConstructionSeconds { get; }

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
            ParkingExitFeePerLevel = 0;
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
            MaximumConstructionSeconds = int.MaxValue;
            AmenityBase = new long[] { 220, 140, 180, 260 };
            AmenityGrowth = new ClinicGrowth(2, 1);
            if (rulesVersion < 4) return;
            // 4.0 pacing: a brisk first hour (cheaper first purchases, 30-second renovations, an
            // affordable second nurse) that still climbs steeply, so later tiers are a real goal.
            // Income and service speed are unchanged, so no clinic earns less.
            HireGrowth = new ClinicGrowth(4, 1);
            AddStationBase = new long[] { 0, 120, 160, 130 };
            StationUpgradeBase = new long[] { 54, 66, 90, 72 };
            TrainingBase = new long[] { 48, 60, 84, 66 };
            UpgradeBase = new long[,]
            {
                { 36, 30, 24 },   // Reception
                { 48, 36, 24 },   // First aid
                { 21, 27, 24 },   // Waiting
                { 66, 54, 24 },   // Consultation
                { 57, 45, 24 }    // Pharmacy
            };
            UpgradeGrowth = new ClinicGrowth(17, 10);
            RenovationBase = new long[] { 108, 150, 72, 180, 132 };
            RenovationGrowth = new ClinicGrowth(3, 1);
            RenovationBaseSeconds = 30;
            RenovationTimeGrowth = new ClinicGrowth(4, 1);
            MaximumConstructionSeconds = 4 * 60 * 60;
            AmenityBase = new long[] { 130, 85, 110, 160 };
            ParkingFeePerLevel = 0;
            ParkingExitFeePerLevel = 10;
            OfflineCapVisits = 250;
        }

        /// <summary>Rules 1–3 share these values; earlier saves are migrated before they are priced.</summary>
        public static readonly ClinicBalanceTable V3 = new ClinicBalanceTable(3);
        public static readonly ClinicBalanceTable V4 = new ClinicBalanceTable(4);
    }

    public static class ClinicBalance
    {
        public const int CurrentRulesVersion = 4;

        public static ClinicBalanceTable For(int rulesVersion)
        {
            if (rulesVersion >= 1 && rulesVersion <= 3) return ClinicBalanceTable.V3;
            if (rulesVersion == 4) return ClinicBalanceTable.V4;
            throw new ArgumentOutOfRangeException(nameof(rulesVersion), rulesVersion, "No balance table exists for these rules.");
        }

        public static ClinicBalanceTable For(ClinicState state) => For(state?.RulesVersion ?? ClinicBalanceTable.V3.RulesVersion);
    }
}
