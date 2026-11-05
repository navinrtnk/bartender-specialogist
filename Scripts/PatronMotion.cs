// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>Engine-independent timing and arm geometry shared by every patron.</summary>
public static class PatronMotion
{
    public readonly record struct SipPose(float Reach, float Grip, float Lift);
    public readonly record struct ArmPose(Quaternion Upper, Quaternion Lower, Quaternion Wrist);

    public static float Wave(float time) => Window(time % 22, 2.3f, 3.2f, 5.8f, 6.9f);

    public static SipPose Sip(float time)
    {
        var cycle = time % 24;
        return new(Window(cycle, 2, 2.7f, 7.6f, 8.3f),
            Window(cycle, 2.65f, 3, 7.1f, 7.6f), Window(cycle, 3.1f, 4.25f, 5.9f, 7));
    }

    public static float Window(float time, float start, float raised, float lowered, float end) =>
        Ease((time - start) / (raised - start)) * (1 - Ease((time - lowered) / (end - lowered)));

    private static float Ease(float value)
    {
        var t = Mathf.Clamp(value, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    public static ArmPose SolveArm(Vector3 shoulder, Vector3 elbow, Vector3 wrist, Vector3 target, Quaternion handRotation)
    {
        var ab = elbow - shoulder;
        var bc = wrist - elbow;
        var upperLength = ab.Length();
        var lowerLength = bc.Length();
        if (upperLength < .01f || lowerLength < .01f)
            throw new ArgumentException("Arm segments must be at least one centimetre long.");

        var offset = target - shoulder;
        var direction = offset.IsZeroApprox() ? (wrist - shoulder).Normalized() : offset.Normalized();
        if (direction.IsZeroApprox()) direction = ab.Normalized();
        var maxReach = upperLength + lowerLength - .002f;
        var minReach = Mathf.Min(maxReach, Mathf.Max(.04f, Mathf.Abs(upperLength - lowerLength) + .002f));
        var distance = Mathf.Clamp(offset.Length(), minReach, maxReach);
        var hand = shoulder + direction * distance;
        var axis = ab - direction * ab.Dot(direction);
        // A target on the shoulder/elbow line still needs a stable bend plane.
        if (axis.LengthSquared() < 1e-10f)
            axis = direction.Cross(Mathf.Abs(direction.Y) < .9f ? Vector3.Up : Vector3.Right);
        axis = axis.Normalized();
        var along = (ab.LengthSquared() - bc.LengthSquared() + distance * distance) / (2 * distance);
        var height = Mathf.Sqrt(Mathf.Max(0, ab.LengthSquared() - along * along));
        var bentElbow = shoulder + direction * along + axis * height;
        var upper = new Quaternion(ab.Normalized(), (bentElbow - shoulder).Normalized()).Normalized();
        var lower = new Quaternion(bc.Normalized(), (hand - bentElbow).Normalized()).Normalized();
        return new(upper, (upper.Inverse() * lower).Normalized(), (lower.Inverse() * handRotation).Normalized());
    }
}
