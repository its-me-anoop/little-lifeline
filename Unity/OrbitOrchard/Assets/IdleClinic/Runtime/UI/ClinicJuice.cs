using System;
using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>Short feedback that confirms an action: amounts rise from where money was earned, rewards fly to their
    /// counter, and improvements announce themselves. Each effect signals a state change; with less motion
    /// they appear and fade in place.</summary>
    public sealed partial class ClinicApp
    {
        private const float FloatSeconds = .9f, GemFlightSeconds = .7f;
        private sealed class FloatingText { internal Label Label; internal Vector2 Start; internal float Age; }
        private sealed class GemFlight { internal ClinicIcon Icon; internal Vector2 Start; internal float Age, Delay, Arc; }
        private readonly List<FloatingText> floatingTexts = new List<FloatingText>();
        private readonly List<GemFlight> gemFlights = new List<GemFlight>();
        private double gemPulseUntil;

        private void FloatText(Vector3 worldPoint, string text, string style)
        {
            if (world == null || particles == null || floatingTexts.Count >= 12) return;
            var uv = world.WorldToViewport(worldPoint);
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
            var label = Text(particles, text, "float-text " + style, true);
            label.style.position = Position.Absolute;
            floatingTexts.Add(new FloatingText { Label = label, Start = OverlayPoint(uv) });
        }

        private void LaunchGemsFrom(Vector2 overlayPoint, int count)
        {
            gemPulseUntil = Time.unscaledTimeAsDouble + .65;
            if (particles == null || ReducedMotion) return;
            for (var i = 0; i < count && gemFlights.Count < 24; i++)
            {
                var icon = new ClinicIcon(ClinicGlyph.Gem, 20, GemInk);
                icon.style.position = Position.Absolute; icon.style.left = 0; icon.style.top = 0;
                particles.Add(icon);
                gemFlights.Add(new GemFlight { Icon = icon, Start = overlayPoint, Delay = i * .045f, Arc = (i % 2 == 0 ? 1 : -1) * (24 + i * 6) });
            }
        }

        private void UpdateJuice(float delta)
        {
            for (var i = floatingTexts.Count - 1; i >= 0; i--)
            {
                var item = floatingTexts[i]; item.Age += delta;
                var t = Mathf.Clamp01(item.Age / FloatSeconds);
                var rise = ReducedMotion ? 0 : (1 - Mathf.Pow(1 - t, 3)) * 46;
                item.Label.style.translate = new Translate(item.Start.x - 40, item.Start.y - 60 - rise);
                item.Label.style.opacity = t < .6f ? 1 : 1 - (t - .6f) / .4f;
                if (t >= 1) { item.Label.RemoveFromHierarchy(); floatingTexts.RemoveAt(i); }
            }
            var gemBalance = gemLabel?.parent;
            if (gemBalance == null) return;
            var remaining = (float)Math.Max(0, gemPulseUntil - Time.unscaledTimeAsDouble);
            gemBalance.style.scale = new Scale(Vector3.one * (ReducedMotion ? 1 : 1 + .06f * remaining / .65f));
            var target = overlay.WorldToLocal(gemBalance.LocalToWorld(new Vector2(22, gemBalance.contentRect.height / 2)));
            for (var i = gemFlights.Count - 1; i >= 0; i--)
            {
                var flight = gemFlights[i]; flight.Age += delta;
                var t = Mathf.Clamp01((flight.Age - flight.Delay) / GemFlightSeconds);
                var eased = 1 - Mathf.Pow(1 - t, 3);
                var p = Vector2.Lerp(flight.Start, target, eased) + new Vector2(Mathf.Sin(t * Mathf.PI) * flight.Arc, -Mathf.Sin(t * Mathf.PI) * 40);
                flight.Icon.style.translate = new Translate(p.x - 10, p.y - 10);
                flight.Icon.style.scale = new Scale(Vector3.one * (1 - .3f * t));
                if (t >= 1 || ReducedMotion) { flight.Icon.RemoveFromHierarchy(); gemFlights.RemoveAt(i); }
            }
        }

        /// <summary>The cash box a collection came from: a desk, the car park, the vending tip cup, the taxi stand or a pharmacy counter.</summary>
        private Vector3 CashPoint(ClinicEvent change)
            => ClinicCashPresentation.IsVendingCollection(change) ? world.GetVendingCashPoint()
                : ClinicCashPresentation.IsParkingCollection(change) ? world.GetParkingCashPoint()
                : ClinicCashPresentation.IsTaxiCollection(change) ? world.GetTaxiCashPoint()
                : ClinicCashPresentation.IsPharmacyCollection(change) ? world.GetPharmacyCashPoint(change.DeskId) : world.GetCashPoint(change.DeskId);

        private void Celebrate(Vector3 point, float radius, string text)
        {
            world.CelebrateUpgrade(point, radius);
            FloatText(point, text, "float-good");
        }

        /// <summary>Name what just happened, where it happened.</summary>
        private void AnnounceEvent(ClinicEvent change)
        {
            switch (change.Kind)
            {
                case ClinicEventKind.CashCollected:
                    var source = CashPoint(change);
                    FloatText(source, "+" + Money(change.Amount), "float-coins");
                    break;
                case ClinicEventKind.EquipmentUpgraded:
                    if (change.Item >= 0) Celebrate(world.GetGearPoint(change.Room, change.Item), 1.0f, "Upgraded!");
                    else Celebrate(world.GetRoomPoint(change.Room), 1.8f, "Upgraded!");
                    break;
                case ClinicEventKind.StationUpgraded:
                case ClinicEventKind.StaffTrained:
                    Celebrate(world.GetRoomPoint(change.Room), 1.4f, change.Kind == ClinicEventKind.StaffTrained ? "Trained!" : "Upgraded!"); break;
                case ClinicEventKind.NurseHired: case ClinicEventKind.ReceptionistHired:
                case ClinicEventKind.DoctorHired: case ClinicEventKind.PharmacistHired:
                    Celebrate(world.GetRoomPoint(change.Room), 1.4f, "Hired!"); break;
                case ClinicEventKind.StationAdded:
                    Celebrate(world.GetRoomPoint(change.Room), 1.6f, "New station!"); break;
                case ClinicEventKind.AmenityUpgraded:
                    Celebrate(world.GetAmenityPoint(change.Amenity), 1.2f, "Improved!"); break;
                case ClinicEventKind.ConstructionCompleted:
                    Celebrate(world.GetRoomPoint(change.Room), 2.6f, "Room ready!"); break;
                case ClinicEventKind.PharmacyFeePaid:
                    FloatText(world.GetPharmacyCashPoint(change.DeskId), "+" + Money(change.Amount), "float-coins"); break;
                case ClinicEventKind.TaxiFarePaid:
                    FloatText(world.GetTaxiCashPoint(), "+" + Money(change.Amount), "float-coins"); break;
                case ClinicEventKind.ParkingFeePaid:
                    FloatText(world.GetParkingCashPoint(), "+" + Money(change.Amount), "float-coins"); break;
            }
        }
    }
}
