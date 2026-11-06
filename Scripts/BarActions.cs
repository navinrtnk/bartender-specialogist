// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Timed prop trajectories. Hands solve against the same rigid grip frames every frame.</summary>
public partial class BarActions : Node3D
{
    private FirstPersonController _player = null!;
    private ScenePalette _p = null!;
    private BartenderHands _hands = null!;
    private Node3D _tin = null!, _cap = null!, _strainer = null!, _scoop = null!, _iceBin = null!, _stackGlass = null!, _door = null!, _washer = null!;
    private readonly Dictionary<Node3D, (Node Parent, Transform3D Pose)> _homes = [];
    private Action<float>? _frame;
    private Action? _finish;
    private float _elapsed, _duration;
    private Vector3 _cameraHome, _lean;
    private Quaternion _lookHome;
    private Basis _actionBasis;
    private Node3D? _right;
    private GripShape _rightGrip;
    private float _rightReach = 1, _rightClose = 1, _leftReach = 1;
    private MeshInstance3D _stream = null!;
    private Node3D _shakerIce = null!, _scoopIce = null!;
    private Vector3[] _scoopCubeHomes = [];
    private Node3D[] _tools = [], _scoopCubes = [], _shakerCubes = [];
    public bool Busy => _frame != null;
    public bool HoldingShaker { get; private set; }
    public bool HoldingGlass { get; private set; }
    public bool StowedShaker { get; private set; }
    public MargaritaGlass? Glass { get; private set; }
    public bool CanReach(Node3D item, GripShape grip) => _hands.ReachLean(item, false, grip).Length() <= 1.1f;
    private Camera3D Camera => _player.Camera;
    public void Build(Node3D scene, FirstPersonController player, ScenePalette p)
    {
        Name = "BarActions";
        _player = player;
        _p = p;
        _tin = scene.GetNode<Node3D>("MargaritaStation/LargeShakerTin");
        _cap = scene.GetNode<Node3D>("MargaritaStation/SmallShakerTin");
        _strainer = scene.GetNode<Node3D>("MargaritaStation/HawthorneStrainer");
        _scoop = scene.GetNode<Node3D>("IceScoop");
        _iceBin = scene.GetNode<Node3D>("IceWell");
        _stackGlass = scene.GetNode<Node3D>("CleanGlassStacks/Stack1Glass4");
        _washer = scene.GetNode<Node3D>("Dishwasher");
        _door = _washer.GetNode<Node3D>("Door");
        _tools = [_tin, _cap, _strainer, _scoop];
        foreach (var item in _tools)
            Remember(item);
        _hands = new BartenderHands();
        _hands.Build(Camera);
        _shakerIce = Group(_tin, "AddedIce");
        _scoopIce = Group(_scoop, "ScoopedIce");
        var iceMaterial = ScenePalette.GlassMaterial("d7e9e9", .3f);
        foreach (var root in new[] { _shakerIce, _scoopIce })
        {
            for (var i = 0; i < 6; i++)
                RoundedBox(root, "Cube", new(.021f, .022f, .024f), new((i % 2 - .5f) * .027f, root == _shakerIce ? .025f + i / 2 * .024f : -.01f, (i / 2 - 1) * .02f), iceMaterial, .005f);
            root.Hide();
        }
        _stream = Cylinder(this, "PourStream", .003f, .004f, 1, Vector3.Zero, ScenePalette.GlassMaterial("e1dfb7", .64f), 12);
        _stream.Hide();
        _scoopCubes = _scoopIce.GetChildren().OfType<Node3D>().ToArray();
        _shakerCubes = _shakerIce.GetChildren().OfType<Node3D>().ToArray();
        _scoopCubeHomes = _scoopCubes.Select(n => n.Position).ToArray();
    }
    private void Remember(Node3D node)
    {
        if (!_homes.ContainsKey(node))
            _homes[node] = (node.GetParent(), node.Transform);
    }
    private void Loan(Node3D node)
    {
        Remember(node);
        if (node.GetParent() != this)
            node.Reparent(this, true);
        node.Show();
    }
    private void Return(Node3D node)
    {
        var home = _homes[node];
        if (node.GetParent() != home.Parent)
            node.Reparent(home.Parent);
        node.Transform = home.Pose;
        node.Show();
    }
    public static float Ease(float t)
    {
        t = Mathf.Clamp(t, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
    private static float Ramp(float p, float a, float b) => Ease((p - a) / (b - a));
    private Transform3D View(Vector3 at, Vector3 rotation = default) => new Transform3D(Busy ? _actionBasis : Basis.FromEuler(new(0, Camera.Rotation.Y, 0)), Camera.GlobalPosition) * new Transform3D(Basis.FromEuler(rotation), at);
    private Transform3D Upright(Transform3D pose)
    {
        pose.Origin.Y = Mathf.Max(1.02f, pose.Origin.Y);
        return pose;
    }
    private Transform3D ShakerHold()
    {
        var pose = Upright(View(StowedShaker ? new(-.28f, -.78f, -.25f) : new(-.18f, -.35f, -.42f)));
        if (StowedShaker)
            pose.Origin.Y = Mathf.Max(1.16f, pose.Origin.Y);
        return pose;
    }
    private Transform3D GlassHold() => Upright(View(new(.18f, -.35f, -.42f)));
    private void Lean(float amount)
    {
        Camera.Position = _cameraHome + _player.GlobalBasis.Inverse() * _lean * amount;
        if (HoldingShaker)
            _tin.GlobalTransform = ShakerHold();
    }
    private void LookAtWork(Vector3 focus, float amount)
    {
        var direction = (focus - Camera.GlobalPosition).Normalized();
        var desired = Quaternion.FromEuler(new(Mathf.Asin(direction.Y), Mathf.Atan2(-direction.X, -direction.Z), 0));
        Camera.Quaternion = _lookHome.Slerp(desired.Normalized(), amount).Normalized();
    }
    private void Begin(float seconds, Node3D source, bool left, GripShape grip, Action<float> frame, Action finish)
    {
        if (Busy)
            throw new InvalidOperationException("An action is already running.");
        _player.ControlsLocked = true;
        _cameraHome = Camera.Position;
        _lookHome = Camera.Quaternion;
        _actionBasis = Basis.FromEuler(new(0, _lookHome.GetEuler().Y, 0));
        _lean = _hands.ReachLean(source, left, grip).LimitLength(1.1f);
        _elapsed = 0;
        _duration = seconds;
        _frame = frame;
        _finish = finish;
    }
    public void Advance(double delta)
    {
        _right = null;
        _leftReach = _rightReach = _rightClose = 1;
        if (HoldingShaker)
            _tin.GlobalTransform = ShakerHold();
        if (HoldingGlass && Glass != null)
        {
            Glass.GlobalTransform = GlassHold();
            _right = Glass;
            _rightGrip = GripShape.Glass;
        }
        if (_frame != null)
        {
            _elapsed += (float)delta;
            var p = Mathf.Clamp(_elapsed / _duration, 0, 1);
            _frame(p);
            if (p >= 1)
            {
                var finish = _finish;
                _frame = null;
                _finish = null;
                Camera.Position = _cameraHome;
                Camera.Quaternion = _lookHome;
                _stream.Hide();
                finish?.Invoke();
            }
        }
        _hands.Visible = Busy || (HoldingShaker && !StowedShaker) || HoldingGlass;
        if (_hands.Visible)
        {
            _hands.Hand(true, HoldingShaker && !StowedShaker ? _tin : null, GripShape.Shaker, _leftReach);
            _hands.Hand(false, _right, _rightGrip, _rightReach, _rightClose);
        }
    }
    private void Pick(Node3D item, bool left, GripShape grip, Action done)
    {
        var from = item.GlobalTransform;
        Loan(item);
        Begin(1.8f, item, left, grip, p =>
        {
            Lean(Ramp(p, 0, .2f) * (1 - Ramp(p, .4f, 1)));
            var to = left ? ShakerHold() : View(new(.26f, -.80f, -.22f));
            item.GlobalTransform = from.InterpolateWith(to, Ramp(p, .35f, .85f));
            if (left)
            {
                HoldingShaker = true;
                _leftReach = Ramp(p, 0, .3f);
            }
            else
            {
                _right = item;
                _rightGrip = grip;
                _rightReach = Ramp(p, 0, .3f);
                _rightClose = Ramp(p, .22f, .35f);
            }
        }, done);
    }
    public void TakeShaker(Action done) => Pick(_tin, true, GripShape.Shaker, () => Pick(_cap, false, GripShape.Glass, () =>
    {
        _cap.Hide();
        Pick(_strainer, false, GripShape.Strainer, () => { _strainer.Hide(); done(); });
    }));

    public void Ice(Action done)
    {
        var from = _scoop.GlobalTransform;
        Loan(_scoop);
        var dip = new Transform3D(Basis.Identity, _iceBin.GlobalPosition + new Vector3(.35f, 0, 0));
        var dipLean = _hands.ReachLeanAt(dip, false, GripShape.Scoop);
        var sourceLean = Vector3.Zero;
        Begin(3.2f, _scoop, false, GripShape.Scoop, p =>
        {
            _lean = p < .45f ? sourceLean.Lerp(dipLean, Ramp(p, .25f, .45f)) : p < .68f ? dipLean * (1 - Ramp(p, .45f, .68f)) : sourceLean * Ramp(p, .68f, .76f);
            Lean(Ramp(p, 0, .2f) * (1 - Ramp(p, .9f, 1)));
            _tin.GlobalTransform = ShakerHold().InterpolateWith(Upright(View(new(0, -.42f, -.42f))), Ramp(p, .2f, .45f) * (1 - Ramp(p, .72f, .9f)));
            var over = new Transform3D(Basis.FromEuler(new(-1.1f, 0, 0)), _tin.GlobalTransform * new Vector3(0, .24f, .03f));
            var pose = p < .45f ? from.InterpolateWith(dip, Ramp(p, .25f, .45f)) : p < .7f ? dip.InterpolateWith(over, Ramp(p, .45f, .65f)) : over.InterpolateWith(from, Ramp(p, .73f, .9f));
            _scoop.GlobalTransform = pose;
            _scoopIce.Visible = p > .4f && p < .70f;
            _shakerIce.Visible = p >= .70f;
            var drop = Mathf.Clamp((p - .58f) / .12f, 0, 1);
            for (var i = 0; i < _scoopCubeHomes.Length; i++)
                _scoopCubes[i].GlobalPosition = (_scoop.GlobalTransform * _scoopCubeHomes[i]).Lerp(_tin.GlobalTransform * _shakerCubes[i].Position, drop * drop);
            _right = _scoop;
            _rightGrip = GripShape.Scoop;
            _rightReach = Ramp(p, 0, .22f) * (1 - Ramp(p, .92f, 1));
            _rightClose = Ramp(p, .18f, .25f) * (1 - Ramp(p, .9f, .96f));
            LookAtWork(_tin.GlobalTransform * new Vector3(0, .12f, 0), Ramp(p, .4f, .52f) * (1 - Ramp(p, .7f, .9f)));
        }, () => { _scoopIce.Hide(); Return(_scoop); done(); });
        sourceLean = _lean;
    }
    private void Stream(Vector3 from, Vector3 to)
    {
        _stream.Show();
        _stream.GlobalPosition = (from + to) / 2;
        _stream.Quaternion = new Quaternion(Vector3.Up, (from - to).Normalized());
        _stream.Scale = new(1, from.DistanceTo(to), 1);
    }
    public void Ingredient(Node3D bottle, Action done)
    {
        var from = bottle.GlobalTransform;
        Loan(bottle);
        var closure = bottle.GetNodeOrNull<Node3D>("Closure");
        var closed = closure?.Visible ?? false;
        var h = bottle.HasMeta("height") ? (float)bottle.GetMeta("height") : .25f;
        Begin(3.8f, bottle, false, GripShape.Bottle, p =>
        {
            Lean(Ramp(p, 0, .18f) * (1 - Ramp(p, .35f, .52f)) + Ramp(p, .72f, .82f) * (1 - Ramp(p, .94f, 1)));
            var mouth = _tin.GlobalTransform * new Vector3(0, .23f, 0);
            var pouring = new Transform3D(Basis.FromEuler(new(0, 0, 1.8f)), mouth + new Vector3(h * .97f, .08f, 0));
            var pose = p < .35f ? from : p < .55f ? from.InterpolateWith(pouring, Ramp(p, .35f, .55f)) : p < .72f ? pouring : pouring.InterpolateWith(from, Ramp(p, .72f, .90f));
            bottle.GlobalTransform = pose;
            if (closure != null)
                closure.Visible = p < .3f || p > .92f ? closed : false;
            _right = bottle;
            _rightGrip = GripShape.Bottle;
            _rightReach = Ramp(p, 0, .25f) * (1 - Ramp(p, .94f, 1));
            _rightClose = Ramp(p, .18f, .3f) * (1 - Ramp(p, .91f, .96f));
            if (p > .55f && p < .72f)
                Stream(bottle.GlobalTransform * new Vector3(0, h, 0), _tin.GlobalTransform * new Vector3(0, .16f, 0));
            else
                _stream.Hide();
            LookAtWork(_tin.GlobalTransform * new Vector3(0, .13f, 0), Ramp(p, .35f, .52f) * (1 - Ramp(p, .70f, .88f)));
        }, () => { if (closure != null) closure.Visible = closed; Return(bottle); done(); });
    }
    public void Shake(Action done)
    {
        _cap.Show();
        var from = View(new(.26f, -.8f, -.22f), new(0, 0, Mathf.Pi));
        Begin(5.8f, _tin, false, GripShape.Cap, p =>
        {
            var seal = new Transform3D(Basis.FromEuler(new(0, 0, Mathf.Pi)), new(0, .325f, 0));
            var strength = Ramp(p, .25f, .33f) * (1 - Ramp(p, .72f, .80f));
            var centered = View(new(Mathf.Sin(_elapsed * 12) * .075f * strength, -.26f + Mathf.Sin(_elapsed * 12 + .4f) * .035f * strength, -.42f + Mathf.Sin(_elapsed * 12) * .04f * strength), new(0, 0, .35f * strength));
            _tin.GlobalTransform = ShakerHold().InterpolateWith(centered, Ramp(p, .15f, .25f) * (1 - Ramp(p, .8f, 1)));
            var sealedPose = _tin.GlobalTransform * seal;
            _cap.GlobalTransform = p < .23f ? from.InterpolateWith(sealedPose, Ramp(p, 0, .23f)) : p < .8f ? sealedPose : sealedPose.InterpolateWith(from, Ramp(p, .8f, 1));
            _right = _cap;
            _rightGrip = GripShape.Cap;
            _rightReach = Ramp(p, 0, .12f) * (1 - Ramp(p, .94f, 1));
            _rightClose = .95f * (1 - Ramp(p, .92f, .98f));
            LookAtWork(_tin.GlobalTransform * new Vector3(0, .16f, 0), Ramp(p, 0, .2f) * (1 - Ramp(p, .85f, 1)));
        }, () => { _cap.Hide(); done(); });
    }
    private void CreateGlass()
    {
        if (Glass != null)
            return;
        Glass = new MargaritaGlass();
        AddChild(Glass);
        Glass.Build(_p);
        Glass.GlobalTransform = _stackGlass.GlobalTransform;
        _stackGlass.Hide();
    }
    public void GrabGlass(Action done)
    {
        CreateGlass();
        var from = Glass!.GlobalTransform;
        Begin(1.8f, Glass, false, GripShape.Glass, p =>
        {
            Lean(Ramp(p, 0, .2f) * (1 - Ramp(p, .4f, 1)));
            Glass.GlobalTransform = from.InterpolateWith(GlassHold(), Ramp(p, .35f, .9f));
            _right = Glass;
            _rightGrip = GripShape.Glass;
            _rightReach = Ramp(p, 0, .3f);
            _rightClose = Ramp(p, .2f, .35f);
        }, () => { HoldingGlass = true; done(); });
    }
    public void Strain(Action done)
    {
        CreateGlass();
        HoldingGlass = false;
        var glassFrom = Glass!.GlobalTransform;
        var onBench = new Transform3D(Basis.Identity, new Vector3(2.05f, .9f, .76f));
        _strainer.Show();
        var strainerFrom = View(new(.26f, -.8f, -.22f));
        Begin(6.2f, Glass, false, GripShape.Glass, p =>
        {
            Lean(Ramp(p, 0, .17f) * (1 - Ramp(p, .8f, 1)));
            Glass.GlobalTransform = p < .2f ? glassFrom.InterpolateWith(onBench, Ramp(p, 0, .2f)) : p < .93f ? onBench : onBench.InterpolateWith(GlassHold(), Ramp(p, .93f, 1));
            Glass.PrepareIce();
            var tinPour = new Transform3D(Basis.FromEuler(new(0, 0, -1.12f)), onBench.Origin + new Vector3(-.18f, .18f, 0));
            var pourStrength = Ramp(p, .3f, .43f) * (1 - Ramp(p, .68f, .8f));
            _tin.GlobalTransform = ShakerHold().InterpolateWith(tinPour, pourStrength);
            if (p > .8f)
                _tin.GlobalTransform = _tin.GlobalTransform.InterpolateWith(View(new(-.28f, -.78f, -.25f)), Ramp(p, .8f, 1));
            var strainerOn = _tin.GlobalTransform * new Transform3D(Basis.Identity, new(-.055f, .191f, 0));
            _strainer.GlobalTransform = strainerFrom.InterpolateWith(strainerOn, Ramp(p, .18f, .3f) * (1 - Ramp(p, .78f, .86f)));
            if (p < .2f || p > .86f)
            {
                _right = Glass;
                _rightGrip = GripShape.Glass;
                if (p > .86f)
                {
                    _rightReach = Ramp(p, .86f, .92f);
                    _rightClose = Ramp(p, .9f, .93f);
                }
            }
            else
            {
                _right = _strainer;
                _rightGrip = GripShape.Strainer;
            }
            Glass.SetFill(Ramp(p, .44f, .68f));
            if (p > .44f && p < .68f)
                Stream(_tin.GlobalTransform * new Vector3(.035f, .19f, 0), Glass.GlobalPosition + new Vector3(0, .075f, 0));
            else
                _stream.Hide();
            LookAtWork(onBench.Origin + Vector3.Up * .13f, Ramp(p, 0, .2f) * (1 - Ramp(p, .8f, 1)));
        }, () => { _strainer.Hide(); _shakerIce.Hide(); HoldingGlass = true; StowedShaker = true; _tin.Hide(); done(); });
        _lean = _hands.ReachLeanAt(onBench, false, GripShape.Glass).LimitLength(1.1f);
    }
    public void Serve(Vector3 at, Action done)
    {
        var from = Glass!.GlobalTransform;
        var destination = new Transform3D(Basis.Identity, at);
        Begin(2.4f, Glass, false, GripShape.Glass, p =>
        {
            Lean(Ramp(p, 0, .25f) * (1 - Ramp(p, .8f, 1)));
            Glass.GlobalTransform = from.InterpolateWith(destination, Ramp(p, .18f, .72f));
            _right = Glass;
            _rightGrip = GripShape.Glass;
            _rightReach = 1 - Ramp(p, .82f, 1);
            _rightClose = 1 - Ramp(p, .72f, .82f);
        }, () => { HoldingGlass = false; done(); });
        _lean = _hands.ReachLeanAt(destination, false, GripShape.Glass).LimitLength(1.1f);
    }
    public void Collect(Action done)
    {
        var from = Glass!.GlobalTransform;
        Begin(2.2f, Glass, false, GripShape.Glass, p =>
        {
            Lean(Ramp(p, 0, .2f) * (1 - Ramp(p, .4f, 1)));
            Glass.GlobalTransform = from.InterpolateWith(GlassHold(), Ramp(p, .35f, .9f));
            _right = Glass;
            _rightGrip = GripShape.Glass;
            _rightReach = Ramp(p, 0, .3f);
            _rightClose = Ramp(p, .2f, .35f);
        }, () => { HoldingGlass = true; done(); });
    }
    public void Wash(Action done)
    {
        var from = Glass!.GlobalTransform;
        var rack = _washer.GlobalTransform * new Transform3D(Basis.Identity, new(-.12f, .43f, .12f));
        Begin(3.8f, Glass, false, GripShape.Glass, p =>
        {
            Lean(Ramp(p, 0, .22f) * (1 - Ramp(p, .78f, 1)));
            _door.Rotation = new(Mathf.Pi / 2 * Ramp(p, 0, .18f) * (1 - Ramp(p, .78f, 1)), 0, 0);
            Glass.GlobalTransform = from.InterpolateWith(rack, Ramp(p, .2f, .58f));
            _right = Glass;
            _rightGrip = GripShape.Glass;
            _rightReach = 1 - Ramp(p, .62f, .75f);
            _rightClose = 1 - Ramp(p, .57f, .64f);
        }, () =>
        {
            Glass.QueueFree();
            Glass = null;
            HoldingGlass = HoldingShaker = StowedShaker = false;
            foreach (var item in _tools)
                Return(item);
            _shakerIce.Hide();
            _stackGlass.Show();
            done();
        });
        _lean = _hands.ReachLeanAt(rack, false, GripShape.Glass).LimitLength(1.1f);
    }
}
