using IdleClinic.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.Tests
{
    /// <summary>Review 8c9d3fa: while the gem confirmation is open, nothing behind it is readable or actionable.</summary>
    public sealed class ClinicModalPolicyTests
    {
        [Test] public void AnOpenSheetMasksEveryControlBehindItAndAClosedOneMasksNothing()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var owner = new GameObject("Clinic modal policy test");
            try
            {
                var document = owner.AddComponent<UIDocument>(); document.panelSettings = settings;
                var root = document.rootVisualElement;
                Assert.That(root.panel, Is.Not.Null);
                var hud = new Button { name = "hud" }; root.Add(hud);
                var dock = new VisualElement(); var buy = new Button { name = "buy" }; dock.Add(buy); root.Add(dock);
                var sheet = new VisualElement { name = "gem-confirm" };
                var card = new VisualElement(); var spend = new Button { name = "spend" }; var facts = new Label("facts");
                card.Add(facts); card.Add(spend); sheet.Add(card); root.Add(sheet);

                sheet.style.display = DisplayStyle.None;
                Assert.That(ClinicModalPolicy.Allows(sheet, hud), Is.True);
                Assert.That(ClinicModalPolicy.Allows(sheet, buy), Is.True);

                sheet.style.display = DisplayStyle.Flex;
                Assert.That(ClinicModalPolicy.Allows(sheet, hud), Is.False, "HUD controls are hidden while confirming.");
                Assert.That(ClinicModalPolicy.Allows(sheet, buy), Is.False, "Panel purchases behind the sheet cannot be activated.");
                Assert.That(ClinicModalPolicy.Allows(sheet, spend), Is.True);
                Assert.That(ClinicModalPolicy.Allows(sheet, facts), Is.True, "The sheet's facts are readable.");
                Assert.That(ClinicModalPolicy.Allows(sheet, null), Is.False);

                sheet.style.display = DisplayStyle.None;
                Assert.That(ClinicModalPolicy.Allows(sheet, buy), Is.True, "Closing restores everything.");
                Assert.That(ClinicModalPolicy.Allows(null, buy), Is.True);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(settings); }
        }
    }
}
