// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

public enum GripShape
{
    Shaker, Bottle, Glass, Scoop, Strainer, Cap
}

/// <summary>Anatomical skinned sleeves/hands with fixed object grip frames and two-bone IK.</summary>
public partial class BartenderHands : Node3D
{
    private Skeleton3D _rig = null!;
    private Camera3D _camera = null!;
    private readonly Dictionary<bool, (int Upper, int Lower, int Wrist, Vector3 A, Vector3 B, Vector3 C)> _arms = [];
    private readonly List<(int Id, bool Left, Vector3 Axis, float Bend, Quaternion Opposition)> _fingers = [];
    public float LeftGripError { get; private set; }
    public float RightGripError { get; private set; }
    public void Build(Camera3D camera)
    {
        _camera = camera;
        camera.AddChild(this);
        Name = "BartenderHands";
        Position = new(0, -1.65f, -.10f);
        Rotation = new(0, Mathf.Pi, 0);
        var model = GD.Load<PackedScene>("res://Assets/Customer/Avatars/bartender_arms.glb").Instantiate<Node3D>();
        AddChild(model);
        _rig = model.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        foreach (var left in new[] { false, true })
        {
            var side = left ? "L" : "R";
            var a = _rig.FindBone("upperarm01_" + side);
            var b = _rig.FindBone("lowerarm01_" + side);
            var c = _rig.FindBone("wrist_" + side);
            _arms[left] = (a, b, c, Rest(a), Rest(b), Rest(c));
        }
        for (var i = 0; i < _rig.GetBoneCount(); i++)
        {
            var name = _rig.GetBoneName(i).ToString();
            if (!name.StartsWith("finger"))
                continue;
            var children = _rig.GetBoneChildren(i);
            var direction = children.Length > 0 ? Rest(children[0]) - Rest(i) : Rest(i) - Rest(_rig.GetBoneParent(i));
            var thumb = name.StartsWith("finger1");
            var left = name.EndsWith("_L");
            // Oppose the thumb across the palm; curling it like the four fingers
            // folds it back toward the wrist and makes the grip look inverted.
            var opposition = name.StartsWith("finger1-1")
                ? new Quaternion(Vector3.Down, left ? -.6f : .6f) : Quaternion.Identity;
            _fingers.Add((i, left, direction.Normalized().Cross(Vector3.Down).Normalized(), thumb ? .18f : .90f, opposition));
        }
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            mesh.ExtraCullMargin = 2;
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                var material = (StandardMaterial3D)mesh.Mesh.SurfaceGetMaterial(i).Duplicate();
                material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
                material.Roughness = mesh.Name == "Skin" ? .58f : .88f;
                if (mesh.Name == "Skin")
                {
                    material.SubsurfScatterEnabled = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
                    material.SubsurfScatterSkinMode = true;
                    material.SubsurfScatterStrength = .07f;
                }
                mesh.SetSurfaceOverrideMaterial(i, material);
            }
        }
        Visible = false;
    }
    private Vector3 Rest(int id) => _rig.GetBoneGlobalRest(id).Origin;
    public static Transform3D Grip(Transform3D item, bool left, GripShape shape, float? itemRadius = null, float? itemHeight = null)
    {
        var (radius, height) = shape switch
        {
            GripShape.Shaker => (.047f, .085f),
            GripShape.Bottle => (.045f, .12f),
            GripShape.Glass => (.043f, .05f),
            GripShape.Cap => (.039f, .075f),
            _ => (0f, 0f)
        };
        var side = (left ? -1 : 1) * (shape == GripShape.Cap ? -1 : 1);
        var at = new Vector3(side * (radius + .012f), height, .075f);
        if (shape == GripShape.Bottle)
            at = new((left ? -1 : 1) * ((itemRadius ?? radius) + .012f), itemHeight ?? height, .075f);
        var basis = new Basis(new Quaternion(Vector3.Back, side < 0 ? Mathf.Pi / 2 : -Mathf.Pi / 2)) * new Basis(new Quaternion(Vector3.Up, Mathf.Pi));
        if (shape == GripShape.Scoop)
        {
            // Right-hand overhand grip: thumb follows the handle toward the bowl,
            // with the four fingers wrapping underneath from the opposite side.
            at = new(-.065f, .027f, .145f);
            basis = new(Vector3.Forward, Vector3.Up, Vector3.Right);
        }
        if (shape == GripShape.Strainer)
        {
            at = new(-.02f, .026f, .075f);
            basis = new(new Quaternion(Vector3.Up, Mathf.Pi));
        }
        if (!left && shape is GripShape.Bottle or GripShape.Glass or GripShape.Cap)
        {
            var turn = new Basis(Vector3.Up, shape == GripShape.Cap ? .45f : -.45f);
            at = turn * at;
            basis = turn * basis;
        }
        return item * new Transform3D(basis, at);
    }
    public Vector3 ReachLean(Node3D item, bool left, GripShape shape)
    {
        return LeanTo(Grip(item, left, shape).Origin, left);
    }
    public Vector3 ReachLeanAt(Transform3D item, bool left, GripShape shape)
    {
        return LeanTo(Grip(item, left, shape).Origin, left);
    }
    private Vector3 LeanTo(Vector3 target, bool left)
    {
        var arm = _arms[left];
        var shoulder = _rig.GlobalTransform * arm.A;
        var reach = arm.A.DistanceTo(arm.B) + arm.B.DistanceTo(arm.C) - .025f;
        var offset = target - shoulder;
        return offset.Normalized() * Mathf.Max(0, offset.Length() - reach);
    }
    private static Transform3D Grip(Node3D item, bool left, GripShape shape) => Grip(item.GlobalTransform, left, shape,
        item.HasMeta("grip_radius") ? (float)item.GetMeta("grip_radius") : null,
        item.HasMeta("grip_height") ? (float)item.GetMeta("grip_height") : null);
    public void Hand(bool left, Node3D? item, GripShape shape = GripShape.Shaker, float reach = 1, float closure = 1)
    {
        var arm = _arms[left];
        var home = _camera.GlobalTransform * new Vector3(left ? -.25f : .25f, -.70f, -.25f);
        var homeRotation = _camera.GlobalBasis.GetRotationQuaternion() * new Quaternion(Vector3.Up, Mathf.Pi);
        var grip = item == null ? new Transform3D(new Basis(homeRotation), home) : Grip(item, left, shape);
        var target = home.Lerp(grip.Origin, item == null ? 0 : reach);
        var rotation = homeRotation.Slerp(grip.Basis.GetRotationQuaternion().Normalized(), item == null ? 0 : reach).Normalized();
        var local = _rig.GlobalTransform.AffineInverse() * target;
        var localRotation = (_rig.GlobalBasis.GetRotationQuaternion().Inverse() * rotation).Normalized();
        var pose = PatronMotion.SolveArm(arm.A, arm.B, arm.C, local, localRotation);
        _rig.SetBonePoseRotation(arm.Upper, pose.Upper);
        _rig.SetBonePoseRotation(arm.Lower, pose.Lower);
        _rig.SetBonePoseRotation(arm.Wrist, pose.Wrist);
        var error = item == null || reach < .99f || closure < .8f ? 0 : (_rig.GlobalTransform * _rig.GetBoneGlobalPose(arm.Wrist).Origin).DistanceTo(target);
        if (left)
            LeftGripError = error;
        else
            RightGripError = error;
        foreach (var (id, isLeft, axis, bend, opposition) in _fingers)
            if (left == isLeft)
                _rig.SetBonePoseRotation(id, item == null ? Quaternion.Identity :
                    Quaternion.Identity.Slerp(opposition, closure) * new Quaternion(axis, bend * closure));
    }
}
