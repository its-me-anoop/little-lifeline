using System;
using System.IO;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicOpeningProfileTests
    {
        [Test]
        public void OfficeUpgradeSurvivesProfileReloadAndFinishesOffline()
        {
            string directory=Path.Combine(Path.GetTempPath(),"clinic-office-"+Guid.NewGuid());
            var now=new DateTimeOffset(2026,10,4,10,0,0,TimeSpan.Zero);
            try
            {
                var store=new ClinicProfileStore(directory);var profile=store.LoadClinic(now);
                var sim=new ClinicSimulation(profile.state);
                for(var i=0;i<30&&sim.State.OpeningStep!=ClinicOpeningStep.Complete;i++)
                {
                    var step=sim.State.OpeningStep;
                    if(ClinicSimulation.OpeningManual(step)||step==ClinicOpeningStep.HireNurse||step==ClinicOpeningStep.HireReceptionist)
                        sim.ContinueOpening(step);
                    sim.Advance(30);
                }
                ClinicSimulation.TryGrantReward(sim.State,100000);
                Assert.That(sim.UpgradeOffice().Success,Is.True);sim.Advance(1);
                Assert.That(store.Save(profile,now),Is.True,store.Error);
                var expected=JsonUtility.ToJson(profile.state);var spent=profile.state.TotalSpent;
                profile=new ClinicProfileStore(directory).LoadClinic(now);
                Assert.That(JsonUtility.ToJson(profile.state),Is.EqualTo(expected));
                store=new ClinicProfileStore(directory);profile=store.LoadClinic(now.AddHours(1));
                Assert.That(profile.state.OfficeLevel,Is.EqualTo(2));
                Assert.That(profile.state.TotalSpent,Is.EqualTo(spent));
                Assert.That(profile.state.Room(ClinicRoom.Reception).Tier,Is.EqualTo(1));
                Assert.That(ClinicSimulation.IsValidState(profile.state),Is.True);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        [Test]
        public void NewProfileIsEmptyAndSavedTaskResumesWithoutResettingOrSkipping()
        {
            string directory=Path.Combine(Path.GetTempPath(),"clinic-opening-"+Guid.NewGuid());
            var now=new DateTimeOffset(2026,10,4,10,0,0,TimeSpan.Zero);
            try
            {
                var store=new ClinicProfileStore(directory);var profile=store.LoadClinic(now);
                Assert.That(profile.state.OpeningEnabled,Is.True);Assert.That(profile.state.Staff,Is.Empty);
                var sim=new ClinicSimulation(profile.state);sim.ContinueOpening(ClinicOpeningStep.CleanOffice);sim.Advance(3);
                Assert.That(store.Save(profile,now.AddSeconds(3)),Is.True,store.Error);
                var expected=JsonUtility.ToJson(profile.state);
                store=new ClinicProfileStore(directory);profile=store.LoadClinic(now.AddSeconds(3));
                Assert.That(JsonUtility.ToJson(profile.state),Is.EqualTo(expected));
                profile=new ClinicProfileStore(directory).LoadClinic(now.AddMinutes(10));
                Assert.That(profile.state.OfficeCleaned,Is.True);Assert.That(profile.state.OfficeLevel,Is.Zero);
                Assert.That(profile.state.OpeningStep,Is.EqualTo(ClinicOpeningStep.SetUpOffice));
                Assert.That(profile.state.Patients,Is.Empty);Assert.That(profile.state.ReceptionDesks,Is.Empty);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
