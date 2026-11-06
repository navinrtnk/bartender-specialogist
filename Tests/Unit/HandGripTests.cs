// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using Xunit;

namespace BartenderSpecialogist.Tests;

public class HandGripTests
{
    [Fact]
    public void RightScoopGripIsOverhandWithThumbTowardTheBowl()
    {
        var grip = BartenderHands.Grip(Transform3D.Identity, false, GripShape.Scoop);
        // This rig's right thumb points along +X, its fingers along +Z,
        // and the back of the hand along +Y in the open rest pose.
        Assert.True((grip.Basis * Vector3.Right).Dot(Vector3.Forward) > .99f);
        Assert.True((grip.Basis * Vector3.Up).Dot(Vector3.Up) > .99f);
        Assert.True(grip.Origin.X < 0);
        Assert.Equal(1, grip.Basis.Determinant(), .00001f);
    }

    [Theory]
    [InlineData(GripShape.Bottle)]
    [InlineData(GripShape.Glass)]
    public void RightHandKeepsItsThumbTowardTheRim(GripShape shape)
    {
        var grip = BartenderHands.Grip(Transform3D.Identity, false, shape);
        Assert.True((grip.Basis * Vector3.Right).Dot(Vector3.Up) > .99f);
        Assert.True(grip.Origin.Z > grip.Origin.X);
        Assert.Equal(1, grip.Basis.Determinant(), .00001f);
    }

    [Fact]
    public void RightThumbStaysUpWhenHoldingTheInvertedShakerCap()
    {
        var cap = new Transform3D(new Basis(Vector3.Back, Mathf.Pi), Vector3.Zero);
        var grip = BartenderHands.Grip(cap, false, GripShape.Cap);
        Assert.True((grip.Basis * Vector3.Right).Dot(Vector3.Up) > .99f);
        Assert.True(grip.Origin.X > 0);
        Assert.Equal(1, grip.Basis.Determinant(), .00001f);
    }
}
