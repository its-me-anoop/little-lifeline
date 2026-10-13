using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicBodyMotionTests
    {
        [Test] public void ATurnIsGradualAndNeverOvershoots()
        {
            var start=Quaternion.LookRotation(Vector3.forward);
            var half=ClinicTurn.Step(start,Vector3.back,.1f,false);
            Assert.That(Quaternion.Angle(start,half),Is.EqualTo(ClinicTurn.DegreesPerSecond*.1f).Within(.01f));
            var done=ClinicTurn.Step(start,Vector3.back,5f,false);
            Assert.That(Vector3.Dot(done*Vector3.forward,Vector3.back),Is.GreaterThan(.9999f));
        }
        [Test] public void ASnapArrivesAtOnceAndNoDirectionKeepsTheHeading()
        {
            var start=Quaternion.LookRotation(Vector3.left);
            Assert.That(Vector3.Dot(ClinicTurn.Step(start,Vector3.right,0,true)*Vector3.forward,Vector3.right),Is.GreaterThan(.9999f));
            Assert.That(Quaternion.Angle(start,ClinicTurn.Step(start,Vector3.zero,1,false)),Is.LessThan(.001f));
        }
        [Test] public void ATurnIgnoresHeightDifferences()
        {
            var r=ClinicTurn.Step(Quaternion.identity,new Vector3(1,5,0),5,false);
            Assert.That((r*Vector3.forward).y,Is.EqualTo(0).Within(.0001f));
        }
        [Test] public void AReachableTargetIsMetWithBothBonesKeepingTheirLength()
        {
            var shoulder=new Vector3(0,1.1f,0);var target=new Vector3(.1f,.8f,.25f);
            var elbow=ClinicTwoBone.Elbow(shoulder,.24f,.25f,target,Vector3.back+Vector3.right,out var hand);
            Assert.That(Vector3.Distance(hand,target),Is.LessThan(.001f));
            Assert.That(Vector3.Distance(shoulder,elbow),Is.EqualTo(.24f).Within(.001f));
            Assert.That(Vector3.Distance(elbow,target),Is.EqualTo(.25f).Within(.001f));
        }
        [Test] public void AnUnreachableTargetStretchesTheArmTowardsIt()
        {
            var shoulder=Vector3.zero;var target=new Vector3(0,0,2);
            ClinicTwoBone.Elbow(shoulder,.24f,.25f,target,Vector3.up,out var hand);
            Assert.That(hand.z,Is.EqualTo(.489f).Within(.002f));Assert.That(Mathf.Abs(hand.x)+Mathf.Abs(hand.y),Is.LessThan(.001f));
        }
        [Test] public void TheElbowBendsTowardsThePole()
        {
            var elbow=ClinicTwoBone.Elbow(Vector3.zero,.3f,.3f,new Vector3(0,0,.4f),Vector3.down,out _);
            Assert.That(elbow.y,Is.LessThan(-.1f));
        }
    }
}
