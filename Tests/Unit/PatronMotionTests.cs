// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using Xunit;
using static BartenderSpecialogist.PatronMotion;

namespace BartenderSpecialogist.Tests;

public class PatronMotionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2.3f, 0)]
    [InlineData(3.2f, 1)]
    [InlineData(5, 1)]
    [InlineData(6.9f, 0)]
    [InlineData(21, 0)]
    public void WaveRaisesHoldsAndReturnsToRest(float time, float expected) =>
        Assert.Equal(expected, Wave(time), .00001f);

    [Theory]
    [InlineData(0)]
    [InlineData(2.7f)]
    [InlineData(4.5f)]
    [InlineData(6.3f)]
    [InlineData(8.4f)]
    public void SocialCyclesRepeatWithoutDrifting(float time)
    {
        Assert.Equal(Wave(time), Wave(time + 22), .00001f);
        var next = Sip(time + 24);
        var current = Sip(time);
        Assert.Equal(current.Reach, next.Reach, .00001f);
        Assert.Equal(current.Grip, next.Grip, .00001f);
        Assert.Equal(current.Lift, next.Lift, .00001f);
    }

    [Fact]
    public void GlassIsReachedAndGrippedThroughoutLiftAndLowering()
    {
        for (var frame = 0; frame <= 24 * 120; frame++)
        {
            var sip = Sip(frame / 120f);
            Assert.InRange(sip.Lift, 0, 1);
            Assert.InRange(sip.Grip, 0, 1);
            Assert.InRange(sip.Reach, 0, 1);
            if (sip.Lift > .001f)
            {
                Assert.Equal(1, sip.Reach, .00001f);
                Assert.Equal(1, sip.Grip, .00001f);
            }
        }
        Assert.Equal(new SipPose(0, 0, 0), Sip(0));
        Assert.Equal(new SipPose(1, 1, 1), Sip(5));
        Assert.Equal(new SipPose(0, 0, 0), Sip(9));
    }

    [Fact]
    public void WaveStartsAndStopsWithNearZeroVelocity()
    {
        foreach (var edge in new[] { 2.3f, 3.2f, 5.8f, 6.9f })
            Assert.InRange(Mathf.Abs(Wave(edge + .001f) - Wave(edge - .001f)) / .002f, 0, .001f);
    }

    [Theory]
    [InlineData(.2f, -.25f, .25f)]
    [InlineData(-.4f, .1f, -.1f)]
    [InlineData(0, -.4f, 0)]
    public void ArmReachesTargetAndPreservesHandOrientation(float x, float y, float z)
    {
        var shoulder = Vector3.Zero;
        var elbow = new Vector3(0, -.3f, 0);
        var wrist = new Vector3(0, -.3f, .3f);
        var target = new Vector3(x, y, z);
        var orientation = Quaternion.FromEuler(new(.2f, -.4f, .15f));
        var pose = SolveArm(shoulder, elbow, wrist, target, orientation);
        var actual = shoulder + pose.Upper * (elbow - shoulder) + (pose.Upper * pose.Lower) * (wrist - elbow);
        Assert.True(actual.DistanceTo(target) < .00001f, $"Hand missed its target: {actual} vs {target}");
        var actualOrientation = pose.Upper * pose.Lower * pose.Wrist;
        Assert.True((actualOrientation * Vector3.Forward).DistanceTo(orientation * Vector3.Forward) < .00001f);
        Assert.True((actualOrientation * Vector3.Up).DistanceTo(orientation * Vector3.Up) < .00001f);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 10, 0)]
    [InlineData(0, -.6f, 0)]
    public void UnreachableTargetsProduceFiniteNormalizedRotations(float x, float y, float z)
    {
        var pose = SolveArm(Vector3.Zero, new(0, -.3f, 0), new(0, -.3f, .3f), new(x, y, z), Quaternion.Identity);
        foreach (var q in new[] { pose.Upper, pose.Lower, pose.Wrist })
        {
            Assert.True(q.IsFinite());
            Assert.True(q.IsNormalized());
        }
        var hand = pose.Upper * new Vector3(0, -.3f, 0) + (pose.Upper * pose.Lower) * new Vector3(0, 0, .3f);
        Assert.InRange(hand.Length(), .03999f, .59801f);
    }

    [Fact]
    public void ShortValidSegmentsDoNotInvertReachLimits()
    {
        var pose = SolveArm(Vector3.Zero, new(0, -.01f, 0), new(0, -.01f, .01f), Vector3.Zero, Quaternion.Identity);
        Assert.True(pose.Upper.IsFinite() && pose.Lower.IsFinite() && pose.Wrist.IsFinite());
    }

    [Fact]
    public void BrokenRigReportsInvalidSegmentInsteadOfProducingNan() =>
        Assert.Throws<ArgumentException>(() => SolveArm(Vector3.Zero, Vector3.Zero, Vector3.Right, Vector3.Up, Quaternion.Identity));
}
