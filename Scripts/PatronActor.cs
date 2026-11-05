// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;
using static BartenderSpecialogist.PatronMotion;

namespace BartenderSpecialogist;

/// <summary>Shared skinned patron: damped gaze, facial pose units, two-bone arms and timed social gestures.</summary>
public partial class PatronActor : Node3D
{
    public Skeleton3D Rig { get; private set; } = null!;
    public MeshInstance3D SkinMesh { get; private set; } = null!;
    public MeshInstance3D ClothingMesh { get; private set; } = null!;
    public PatronActor[] ConversationPartners { get; set; } = [];
    public bool IsBarPatron { get; private set; }
    public float WaveAmount { get; private set; }
    public float SipAmount { get; private set; }
    public float SmileAmount { get; private set; }
    public Vector3 GazeTarget { get; private set; }
    public float AnimationTime => (float)_clock;
    private static Texture2D? _pores;
    private Node3D _visual = null!;
    private Node3D? _drink;
    private bool _cosmo;
    private float _phase;
    private double _clock;
    private int _neck, _head, _eyeL, _eyeR;
    private Arm _left = null!, _right = null!;
    private readonly Dictionary<int, Dictionary<string, Quaternion>> _expressions = new();
    private readonly List<(int Bone, Vector3 Axis, float Bend)> _rightFingers = new();
    private Quaternion _neckLook = Quaternion.Identity, _headLook = Quaternion.Identity, _lookL = Quaternion.Identity, _lookR = Quaternion.Identity;
    private readonly Vector3 _cupRest = new(-.235f, 1.1475f, .49f);

    private sealed record Arm(int Upper, int Lower, int Wrist, Vector3 Shoulder, Vector3 Elbow, Vector3 Hand);

    public void Build(ScenePalette p, string model, bool barPatron, bool cosmo = false, float phase = 0, string tint = "ffffff")
    {
        IsBarPatron = barPatron; _cosmo = cosmo; _phase = phase;
        _visual = GD.Load<PackedScene>($"res://Assets/Customer/Avatars/{model}.glb").Instantiate<Node3D>();
        _visual.Name = "Visual"; AddChild(_visual);
        Rig = _visual.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        _neck = Bone("neck01"); _head = Bone("head"); _eyeL = Bone("eye.L"); _eyeR = Bone("eye.R");
        _left = MakeArm("L"); _right = MakeArm("R");
        _pores ??= new NoiseTexture2D
        {
            Width = 512,
            Height = 512,
            AsNormalMap = true,
            BumpStrength = .13f,
            Noise = new FastNoiseLite { Seed = 229, Frequency = .42f, FractalOctaves = 2 }
        };
        foreach (var mesh in _visual.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if (mesh.Mesh.SurfaceGetMaterial(i) is not StandardMaterial3D original) continue;
                var material = (StandardMaterial3D)original.Duplicate();
                material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
                if (mesh.Name == "Skin")
                {
                    SkinMesh = mesh; material.Roughness = .58f;
                    material.SubsurfScatterEnabled = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
                    material.SubsurfScatterStrength = .07f; material.SubsurfScatterSkinMode = true;
                    material.NormalEnabled = true; material.NormalTexture = _pores; material.NormalScale = .12f;
                }
                if (mesh.Name == "Clothing") { ClothingMesh = mesh; material.AlbedoColor = new(tint); material.Roughness = .88f; }
                if (mesh.Name == "Eyes") { material.Roughness = .17f; material.MetallicSpecular = .7f; }
                if (mesh.Name == "Teeth") material.Roughness = .4f;
                mesh.SetSurfaceOverrideMaterial(i, material);
            }
        }
        LoadExpressions();
        for (var i = 0; i < Rig.GetBoneCount(); i++)
        {
            var name = Rig.GetBoneName(i).ToString();
            if (!name.StartsWith("finger") || !name.EndsWith("_R")) continue;
            var children = Rig.GetBoneChildren(i);
            var forward = children.Length > 0 ? Rest(children[0]) - Rest(i) : Rest(i) - Rest(Rig.GetBoneParent(i));
            var bend = name.StartsWith("finger1") ? .38f : cosmo && name.StartsWith("finger2") ? .48f : .75f;
            _rightFingers.Add((i, forward.Normalized().Cross(Vector3.Down).Normalized(), bend));
        }
        if (!barPatron)
        {
            _drink = BarGlassware.Cocktail(this, p, cosmo);
            _drink.Position = _cupRest;
        }
        AddShoes(p, model.StartsWith("woman"));
        Evaluate(0, 1);
    }

    private int Bone(string name)
    {
        var id = Rig.FindBone(name.Replace('.', '_'));
        if (id < 0) throw new InvalidOperationException($"Patron rig is missing {name}.");
        return id;
    }
    private Vector3 Rest(int bone) => Rig.GetBoneGlobalRest(bone).Origin;
    private Arm MakeArm(string side)
    {
        var a = Bone("upperarm01." + side); var b = Bone("lowerarm01." + side); var c = Bone("wrist." + side);
        return new(a, b, c, Rest(a), Rest(b), Rest(c));
    }

    private void LoadExpressions()
    {
        var data = Json.ParseString(Godot.FileAccess.GetFileAsString("res://Assets/Customer/expression-poses.json")).AsGodotDictionary();
        foreach (var group in data)
            foreach (var entry in group.Value.AsGodotDictionary())
            {
                var id = Rig.FindBone(entry.Key.AsString());
                if (id < 0) continue;
                if (!_expressions.TryGetValue(id, out var poses)) _expressions[id] = poses = new();
                var q = entry.Value.AsGodotArray();
                poses[group.Key.AsString()] = new Quaternion((float)q[0], (float)q[1], (float)q[2], (float)q[3]).Normalized();
            }
    }

    public override void _Process(double delta)
    {
        var step = Mathf.Min((float)delta, .1f); _clock += step;
        Evaluate((float)_clock, step);
    }

    /// <summary>Also used by deterministic animation checks; no alternate staged animation.</summary>
    public void Evaluate(float time, float delta)
    {
        var t = time + _phase;
        _visual.Position = new(0, Mathf.Sin(t * 1.4f) * .0012f, 0);
        var sip = IsBarPatron ? default : Sip(t);
        SipAmount = sip.Lift;
        var speaking = !IsBarPatron && Mathf.Sin(t * .46f) > 0;
        var phrase = speaking ? (.5f + .5f * Mathf.Sin(t * 1.7f)) * (1 - SipAmount) : 0;
        GazeTarget = GetViewport().GetCamera3D()?.GlobalPosition ?? GlobalPosition + new Vector3(0, 1.72f, 3);
        if (!IsBarPatron && ConversationPartners.Length > 0)
        {
            var partner = ConversationPartners[(int)(t / 7) % ConversationPartners.Length];
            GazeTarget = partner.GlobalTransform * new Vector3(0, 1.57f, 0);
        }
        TrackGaze(GazeTarget, delta, phrase * Mathf.Sin(t * 2.1f) * .018f);
        WaveAmount = IsBarPatron ? Wave(time) : 0;
        SmileAmount = IsBarPatron ? .12f + WaveAmount * .48f + .12f * Window(time % 17, 10, 11, 13, 14) : .18f + phrase * .13f;
        var blinkPhase = (t + Mathf.Sin(t * .17f) * .35f) % 4.7f;
        var blink = Window(blinkPhase, 0, .055f, .10f, .22f);
        AnimateFace(SmileAmount * (1 - SipAmount * .7f), blink, speaking ? (.07f + .10f * Mathf.Abs(Mathf.Sin(t * 7.1f))) * phrase : WaveAmount * .04f, SipAmount * .16f);
        var waveHand = new Vector3(-.39f, 1.75f, .14f) + new Vector3(Mathf.Sin(time * 9) * .018f, 0, 0);
        var waveRotation = new Quaternion(Vector3.Back, Mathf.Sin(time * 9) * .18f) * new Quaternion(Vector3.Right, -Mathf.Pi / 2);
        if (IsBarPatron)
        {
            var restOffset = new Vector3(0, -.018f, .095f);
            PoseArm(_right, (_right.Hand + restOffset).Lerp(waveHand, WaveAmount), Quaternion.Identity.Slerp(waveRotation, WaveAmount));
            PoseArm(_left, _left.Hand + restOffset, Quaternion.Identity);
            CurlFingers(0);
        }
        else
        {
            PoseArm(_left, _left.Hand + new Vector3(Mathf.Sin(t * 1.3f) * .012f, phrase * .025f, Mathf.Sin(t * .8f) * .008f), Quaternion.Identity);
            AnimateSip(sip);
        }
    }

    private void TrackGaze(Vector3 target, float delta, float nod)
    {
        var local = Rig.GlobalTransform.AffineInverse() * target - Rest(_head);
        var yaw = Mathf.Atan2(local.X, local.Z);
        var pitch = -Mathf.Atan2(local.Y, new Vector2(local.X, local.Z).Length());
        var neck = Basis.FromEuler(new Vector3(Mathf.Clamp(pitch * .35f, -.18f, .20f), Mathf.Clamp(yaw * .45f, -.48f, .48f), 0)).GetRotationQuaternion();
        var head = Basis.FromEuler(new Vector3(Mathf.Clamp(pitch * .55f + nod, -.28f, .28f), Mathf.Clamp(yaw * .55f, -.68f, .68f), 0)).GetRotationQuaternion();
        _neckLook = _neckLook.Slerp(neck.Normalized(), 1 - Mathf.Exp(-5 * delta)).Normalized();
        _headLook = _headLook.Slerp(head.Normalized(), 1 - Mathf.Exp(-6 * delta)).Normalized();
        Rig.SetBonePoseRotation(_neck, _neckLook); Rig.SetBonePoseRotation(_head, _headLook);
        AimEye(_eyeL, target, delta, ref _lookL); AimEye(_eyeR, target, delta, ref _lookR);
    }

    private void AimEye(int eye, Vector3 target, float delta, ref Quaternion rotation)
    {
        var parent = Rig.GetBoneGlobalPose(Rig.GetBoneParent(eye));
        var local = parent.Basis.Inverse() * ((Rig.GlobalTransform.AffineInverse() * target) - Rig.GetBoneGlobalPose(eye).Origin);
        var desired = Basis.FromEuler(new Vector3(Mathf.Clamp(-Mathf.Atan2(local.Y, new Vector2(local.X, local.Z).Length()), -.40f, .40f),
            Mathf.Clamp(Mathf.Atan2(local.X, local.Z), -.48f, .48f), 0)).GetRotationQuaternion();
        rotation = rotation.Slerp(desired.Normalized(), 1 - Mathf.Exp(-16 * delta)).Normalized(); Rig.SetBonePoseRotation(eye, rotation);
    }

    private void AnimateFace(float smile, float blink, float talk, float pucker)
    {
        foreach (var (bone, poses) in _expressions)
        {
            var rotation = Quaternion.Identity;
            foreach (var (name, q) in poses)
            {
                var amount = name switch { "Smile" => smile, "BlinkL" or "BlinkR" => blink, "Talk" => talk, "Pucker" => pucker, _ => 0 };
                rotation *= Quaternion.Identity.Slerp(q, amount);
            }
            Rig.SetBonePoseRotation(bone, rotation.Normalized());
        }
    }

    private void AnimateSip(SipPose sip)
    {
        var mouth = Rig.GetBoneGlobalPose(_head) * new Vector3(0, -.045f, .134f);
        var rim = _cosmo ? .157f : .106f;
        var tilt = SipAmount * (_cosmo ? -.18f : -.32f);
        var sipRotation = new Quaternion(Vector3.Right, tilt);
        var atMouth = mouth + new Basis(sipRotation) * new Vector3(0, -rim, _cosmo ? .048f : .035f);
        var cupPosition = _cupRest.Lerp(atMouth, SipAmount);
        cupPosition.Y += Mathf.Sin(SipAmount * Mathf.Pi) * .045f;
        var cupRotation = sipRotation;
        _drink!.Transform = new(new Basis(cupRotation), cupPosition);
        var handRotation = cupRotation * new Quaternion(Vector3.Right, _cosmo ? -.90f : -1.15f);
        var gripPoint = cupPosition + new Basis(cupRotation) * new Vector3(0, _cosmo ? .062f : .052f, _cosmo ? -.006f : -.034f);
        var wrist = gripPoint - new Basis(handRotation) * new Vector3(0, .010f, .075f);
        PoseArm(_right, _right.Hand.Lerp(wrist, sip.Reach), Quaternion.Identity.Slerp(handRotation, sip.Reach));
        CurlFingers(sip.Grip);
    }

    private void PoseArm(Arm arm, Vector3 hand, Quaternion handRotation)
    {
        var pose = SolveArm(arm.Shoulder, arm.Elbow, arm.Hand, hand, handRotation);
        Rig.SetBonePoseRotation(arm.Upper, pose.Upper);
        Rig.SetBonePoseRotation(arm.Lower, pose.Lower);
        Rig.SetBonePoseRotation(arm.Wrist, pose.Wrist);
    }

    private void CurlFingers(float amount)
    {
        foreach (var (bone, axis, bend) in _rightFingers)
            Rig.SetBonePoseRotation(bone, new Quaternion(axis, amount * bend));
    }

    private void AddShoes(ScenePalette p, bool female)
    {
        var leather = ScenePalette.Matte("272722", .4f);
        foreach (var side in new[] { -1f, 1f })
        {
            var shoe = Group(_visual, "LeatherShoe", new(side * .16f, .35f, .43f));
            RoundedBox(shoe, "Outsole", new(.115f, .023f, .255f), new(0, .012f, 0), p.Ink, .011f);
            Sphere(shoe, "LeatherUpper", new(.112f, .092f, .25f), new(0, .048f, 0), leather);
            RoundedBox(shoe, "Heel", new(.10f, .034f, .073f), new(0, .017f, -.074f), p.Ink, .008f);
            if (female) Rod(shoe, "Strap", new(-.043f, .084f, -.015f), new(.043f, .084f, -.015f), .003f, leather);
            else for (var i = 0; i < 4; i++) Rod(shoe, "Lace", new(-.027f, .088f - i * .004f, -.046f + i * .015f), new(.027f, .088f - i * .004f, -.046f + i * .015f), .0014f, p.Ink);
        }
    }

}
