// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

public partial class DrinkGameplay : Node3D
{
    public sealed record Target(string Id, string Name, Node3D Node, Func<Vector3> Center, float Radius, float Distance, Node3D? Extra = null);
    public DrinkSession Session { get; private set; } = null!;
    public DrinkHud Hud { get; private set; } = null!;
    public BarActions Actions { get; private set; } = null!;
    public Target? Highlighted { get; private set; }
    public bool FeedbackPlaying => _voice.Playing;
    public List<Target> Targets { get; } = [];
    private FirstPersonController _player = null!;
    private SeatedCustomer _customer = null!;
    private AudioStreamPlayer3D _voice = null!;
    private SceneHud _navigation = null!;
    private readonly List<MeshInstance3D> _outlined = [];
    private StandardMaterial3D _outline = null!;
    private Node3D[] _well = [];
    private bool _saidGreat;
    private Input.MouseModeEnum _beforeMenu;
    public void Build(Node3D scene, FirstPersonController player, SeatedCustomer customer, ScenePalette p)
    {
        Name = "DrinkGameplay";
        ProcessPriority = 20;
        _player = player;
        _customer = customer;
        _navigation = scene.GetNode<SceneHud>("SceneHud");
        Session = new(DrinkRecipe.Parse(Godot.FileAccess.GetFileAsString("res://Recipes/margarita.json")));
        _customer.BindOrder(Session);
        Hud = new DrinkHud();
        AddChild(Hud);
        Hud.Build(player);
        Hud.InteractionPressed += () => { if (!Actions.Busy) { if (Hud.MenuOpen) Hud.Choose(); else Interact(); } };
        Actions = new BarActions();
        AddChild(Actions);
        Actions.Build(scene, player, p);
        _outline = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new("62e4c5"), Grow = true, GrowAmount = .004f, CullMode = BaseMaterial3D.CullModeEnum.Front };
        var shaker = scene.GetNode<Node3D>("MargaritaStation/LargeShakerTin");
        Targets.Add(new("customer", "Customer", customer, () => customer.HeadPosition, .4f, 5.0f));
        Targets.Add(new("shaker", "Pick up shaker", shaker, () => shaker.GlobalPosition + Vector3.Up * .1f, .13f, 1.35f));
        var ice = scene.GetNode<Node3D>("IceWell");
        var scoop = scene.GetNode<Node3D>("IceScoop");
        Targets.Add(new("ice", "Scoop ice", ice, () => ice.GlobalPosition, .43f, 1.4f, scoop));
        var well = scene.GetNode<Node3D>("WellRail");
        Targets.Add(new("well", "Well liquors", well, () => well.GlobalPosition + Vector3.Up * .18f, .50f, 1.65f));
        _well = well.GetChildren().OfType<Node3D>().Where(n => n.HasNode("BottleBody")).ToArray();
        foreach (var id in new[] { "LimeJuice", "SimpleSyrup" })
        {
            var bottle = scene.GetNode<Node3D>("MargaritaIngredients/" + id);
            var kind = id == "LimeJuice" ? IngredientKind.LimeJuice : IngredientKind.SimpleSyrup;
            bottle.SetMeta("ingredient", kind.ToString());
            bottle.SetMeta("height", .248f);
            Targets.Add(new(id, id == "LimeJuice" ? "Prepared lime juice" : "Prepared simple syrup", bottle, () => bottle.GlobalPosition + Vector3.Up * .12f, .12f, 1.35f));
        }
        var glasses = scene.GetNode<Node3D>("CleanGlassStacks");
        Targets.Add(new("glasses", "Clean glasses", glasses, () => glasses.GlobalPosition + new Vector3(.12f, .15f, 0), .30f, 1.5f));
        var washer = scene.GetNode<Node3D>("Dishwasher");
        Targets.Add(new("dishwasher", "Dishwasher", washer, () => washer.GlobalPosition + new Vector3(0, .56f, .38f), .37f, 1.65f));
        foreach (var shelf in new[] { "TopShelf", "BottomShelf" })
            foreach (var bottle in scene.GetNode("BackBar/" + shelf).GetChildren().OfType<Node3D>().Where(n => n.HasNode("BottleBody") && n.GetMeta("ingredient", "").AsString() == "TEQUILA"))
                Targets.Add(new("tequila-" + shelf, bottle.Name.ToString(), bottle, () => bottle.GlobalPosition + Vector3.Up * .25f, .18f, 1.8f));
        _voice = new AudioStreamPlayer3D { Stream = GD.Load<AudioStream>("res://Assets/Audio/great-drink.wav"), Position = new(0, 1.5f, 0), UnitSize = 3, MaxDistance = 12 };
        _customer.AddChild(_voice);
    }
    public override void _Process(double delta) => Advance(delta);
    public void StopFeedback() => _voice.Stop();
    public override void _ExitTree()
    {
        if (IsInstanceValid(_voice))
        {
            _voice.Stop();
            _voice.Stream = null;
        }
    }
    public void Advance(double delta)
    {
        Session.Tick(delta);
        Actions.Advance(delta);
        _player.ControlsLocked = Actions.Busy || Hud.MenuOpen;
        _navigation.TurnButton.Disabled = _navigation.ResetButton.Disabled = _player.ControlsLocked;
        Hud.Update(delta, Session, _player.Camera, _customer.HeadPosition + Vector3.Up * .22f);
        if (Session.HasDone(DrinkAction.Serve) && Session.DrinkingSeconds >= 7 && !_saidGreat)
        {
            _saidGreat = true;
            Hud.Speech("Great drink!");
            _voice.Play();
            _customer.Speak((float)_voice.Stream.GetLength());
        }
        var target = Actions.Busy || Hud.MenuOpen ? null : FindTarget();
        if (target != Highlighted)
        {
            Highlighted = target;
            Outline(target);
        }
        var prompt = Actions.Busy || Hud.MenuOpen || Session.Drinking ? "" :
            target != null ? "SPACE  " + TargetPrompt(target) : Session.Can(DrinkAction.Shake) ? "SPACE  Seal and shake the shaker" : "";
        Hud.Prompt(prompt);
    }
    private Target? FindTarget()
    {
        var camera = _player.Camera;
        var forward = -camera.GlobalBasis.Z;
        Target? best = null;
        var score = float.MaxValue;
        var handlingGlass = Session.Can(DrinkAction.Serve) || Session.Can(DrinkAction.CollectGlass);
        foreach (var target in Targets)
        {
            if (target.Id == "customer" && Session.Drinking)
                continue;
            if (target.Id == "customer" && handlingGlass && _player.Position.Z > 1)
                continue;
            if (target.Id == "shaker" && !Session.Can(DrinkAction.TakeShaker))
                continue;
            if (target.Id == "ice" && !Session.Can(DrinkAction.Ice))
                continue;
            if (target.Id == "well" && !Session.Can(DrinkAction.Ingredient, IngredientKind.Tequila) && !Session.Can(DrinkAction.Ingredient, IngredientKind.TripleSec))
                continue;
            if (target.Id == "LimeJuice" && !Session.Can(DrinkAction.Ingredient, IngredientKind.LimeJuice))
                continue;
            if (target.Id == "SimpleSyrup" && !Session.Can(DrinkAction.Ingredient, IngredientKind.SimpleSyrup))
                continue;
            if (target.Id.StartsWith("tequila-") && !Session.Can(DrinkAction.Ingredient, IngredientKind.Tequila))
                continue;
            if (target.Id == "glasses" && !Session.Can(DrinkAction.Strain))
                continue;
            if (target.Id == "dishwasher" && !Session.Can(DrinkAction.WashGlass))
                continue;
            var offset = target.Center() - camera.GlobalPosition;
            var along = offset.Dot(forward);
            var distance = target.Id == "customer" && handlingGlass ? 2.0f : target.Distance;
            if (along <= 0 || offset.Length() > distance)
                continue;
            var miss = (offset - forward * along).Length() / target.Radius;
            if (miss < 1 && miss < score)
            {
                best = target;
                score = miss;
            }
        }
        return best;
    }
    private void Outline(Target? target)
    {
        foreach (var mesh in _outlined)
            if (IsInstanceValid(mesh))
                mesh.MaterialOverlay = null;
        _outlined.Clear();
        if (target == null)
            return;
        foreach (var root in new[] { target.Node, target.Extra }.OfType<Node3D>())
            foreach (var mesh in root.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Concat(root is MeshInstance3D self ? [self] : []))
            {
                if (target.Id == "ice" && root == target.Node && !mesh.Name.ToString().StartsWith("Rim"))
                    continue;
                mesh.MaterialOverlay = _outline;
                _outlined.Add(mesh);
            }
    }
    private string TargetPrompt(Target target)
    {
        if (target.Id != "customer")
            return target.Name;
        if (Session.Can(DrinkAction.Serve))
            return "Serve margarita";
        if (Session.Can(DrinkAction.CollectGlass))
            return "Collect empty glass";
        return Session.Asleep ? "Start a new shift" : Session.Active ? "Talk to customer" : "Take order";
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (Hud.MenuOpen)
            {
                if (key.PhysicalKeycode is Key.Up or Key.Left)
                    Hud.MoveSelection(-1);
                else if (key.PhysicalKeycode is Key.Down or Key.Right)
                    Hud.MoveSelection(1);
                else if (key.PhysicalKeycode == Key.Space)
                    Hud.Choose();
                else if (key.PhysicalKeycode == Key.Escape)
                {
                    Hud.Close();
                    Input.MouseMode = _beforeMenu;
                }
                else
                    return;
                GetViewport().SetInputAsHandled();
                _player.ControlsLocked = Hud.MenuOpen || Actions.Busy;
                return;
            }
            if (key.PhysicalKeycode == Key.Space)
            {
                if (!Actions.Busy)
                    Interact();
                GetViewport().SetInputAsHandled();
            }
        }
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && !Actions.Busy && !Hud.MenuOpen && Highlighted?.Id == "glasses")
        {
            Interact();
            GetViewport().SetInputAsHandled();
        }
    }
    private void Menu(string question, string[] choices, Action<int> select)
    {
        _beforeMenu = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Hud.Open(question, choices, i => { Input.MouseMode = _beforeMenu; select(i); });
        _player.ControlsLocked = true;
    }
    private void Done(DrinkAction step, IngredientKind? ingredient = null)
    {
        if (!Session.Complete(step, ingredient))
            throw new InvalidOperationException($"Completed action is out of recipe order: {step}");
    }
    public void Interact()
    {
        if (Actions.Busy)
            return;
        var target = Highlighted;
        if (Session.Can(DrinkAction.Shake) && target?.Id != "well")
        {
            Actions.Shake(() => Done(DrinkAction.Shake));
            return;
        }
        if (target == null)
            return;
        switch (target.Id)
        {
            case "customer":
                Customer();
                break;
            case "shaker":
                if (Session.Can(DrinkAction.TakeShaker))
                    Actions.TakeShaker(() => Done(DrinkAction.TakeShaker));
                else
                    NeedStep();
                break;
            case "ice":
                if (Session.Can(DrinkAction.Ice))
                    Actions.Ice(() => Done(DrinkAction.Ice));
                else
                    NeedStep();
                break;
            case "well":
                Menu("Choose a bottle", _well.Select(n => n.Name.ToString()).ToArray(), i => { Pour(_well[i]); });
                break;
            case "LimeJuice":
            case "SimpleSyrup":
                Pour(target.Node);
                break;
            case "glasses":
                Menu("Glass station", ["Grab a glass", "Pour into glass"], i =>
                {
                    if (!Session.Can(DrinkAction.Strain))
                    {
                        NeedStep();
                        return;
                    }
                    if (i == 0)
                    {
                        if (Session.GrabGlass())
                            Actions.GrabGlass(() => { });
                        else
                            Hud.Message("You already have a glass.");
                    }
                    else
                        Actions.Strain(() => Done(DrinkAction.Strain));
                });
                break;
            case "dishwasher":
                if (Session.Can(DrinkAction.WashGlass))
                    Actions.Wash(() => { Done(DrinkAction.WashGlass); _customer.ReleaseGlass(); });
                else
                    NeedStep();
                break;
            default:
                if (target.Id.StartsWith("tequila-"))
                    Pour(target.Node);
                break;
        }
    }
    private void Pour(Node3D bottle)
    {
        if (!Actions.CanReach(bottle, GripShape.Bottle))
        {
            Hud.Message("Step closer to that bottle, then try again.");
            return;
        }
        if (!Enum.TryParse<IngredientKind>(bottle.GetMeta("ingredient", "").AsString(), true, out var ingredient) || !Session.Can(DrinkAction.Ingredient, ingredient))
        {
            NeedStep();
            return;
        }
        Actions.Ingredient(bottle, () =>
        {
            Done(DrinkAction.Ingredient, ingredient);
            if (_well.Contains(bottle))
                Menu("Choose a bottle", _well.Select(n => n.Name.ToString()).ToArray(), i => Pour(_well[i]));
        });
    }
    private void NeedStep() => Hud.Message(Session.Drinking ? "Let the customer finish the drink." : Session.NextSteps.FirstOrDefault()?.Hint ?? "Take the customer's order first.");
    private void Customer()
    {
        if (Session.Can(DrinkAction.Serve))
        {
            Actions.Serve(_customer.GlassRestPosition, () => { Done(DrinkAction.Serve); _saidGreat = false; _customer.ReceiveGlass(Actions.Glass!); });
            return;
        }
        if (Session.Can(DrinkAction.CollectGlass))
        {
            _customer.ReleaseGlass();
            Actions.Collect(() => Done(DrinkAction.CollectGlass));
            return;
        }
        if (Session.Active)
        {
            NeedStep();
            return;
        }
        if (Session.Asleep)
        {
            Menu("Shift complete. Start a new shift?", ["Yes", "No"], i => { if (i == 0) { Session.ResetShift(); _customer.ReleaseGlass(); } });
            return;
        }
        Menu("I would like a margarita please.", ["Yes", "No"], i => { if (i == 0) Session.AcceptOrder(); });
    }
}
