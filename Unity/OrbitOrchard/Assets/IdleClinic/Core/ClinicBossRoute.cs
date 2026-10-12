using System;
using System.Collections.Generic;

namespace IdleClinic.Core
{
    /// <summary>Where the boss walks during the opening, on the starter clinic floor in metres (x, z). Zones: 0 reception,
    /// 1 nursing room, 2 waiting room, 3 office. The opening's task timers and the animation share these routes, so the boss
    /// always walks at one pace, arrives before the work starts, and helping can never make the boss jump ahead.</summary>
    public static class ClinicBossRoute
    {
        /// <summary>A brisk walk: quicker than visitors (1.35 m/s) and staff (1.7 m/s), because the boss is hurrying to open.</summary>
        public const double MetresPerSecond = 2.4;

        /// <summary>Where the boss stands to work in a zone.</summary>
        public static (double X, double Z) Point(int zone)
            => zone == 3 ? (-4.08, 7.15) : zone == 2 ? (3.15, .4) : zone == 1 ? (-4.20, 2.05) : (-3.85, .15);

        /// <summary>The corridor route between two zones: out of the office or nursing room, through the hub by the care
        /// doorway, and in again. Starts and ends at the zones' work points.</summary>
        public static List<(double X, double Z)> Points(int from, int to)
        {
            var route = new List<(double X, double Z)> { Point(from) };
            if (from == to) { route.Add(Point(to)); return route; }
            if (from == 3) { route.Add((-4.08, 5.55)); route.Add((.675, 5.55)); }
            if (from == 1) { route.Add((-4.20, 1.50)); route.Add((-4.20, .4)); }
            route.Add((.675, .4));
            if (to == 3) { route.Add((.675, 5.55)); route.Add((-4.08, 5.55)); }
            if (to == 1) { route.Add((-4.20, .4)); route.Add((-4.20, 1.50)); }
            route.Add(Point(to));
            return route;
        }

        public static double Length(int from, int to)
        {
            var route = Points(from, to); var total = 0d;
            for (var i = 1; i < route.Count; i++)
            {
                var dx = route[i].X - route[i - 1].X; var dz = route[i].Z - route[i - 1].Z;
                total += Math.Sqrt(dx * dx + dz * dz);
            }
            return total;
        }

        /// <summary>Ticks the walk takes at <see cref="MetresPerSecond"/>; none when the boss is already there.</summary>
        public static int TravelTicks(int from, int to)
            => from == to ? 0 : (int)Math.Ceiling(Length(from, to) * ClinicRules.TicksPerSecond / MetresPerSecond);

        /// <summary>The longest walk between any two zones.</summary>
        public static int MaximumTravelTicks
        {
            get
            {
                var most = 0;
                for (var from = 0; from <= 3; from++) for (var to = 0; to <= 3; to++) most = Math.Max(most, TravelTicks(from, to));
                return most;
            }
        }
    }
}
