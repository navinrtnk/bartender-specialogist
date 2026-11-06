// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

public partial class DrinkHud : CanvasLayer
{
    public bool MenuOpen => _menu.Visible;
    public int Selected { get; private set; }
    public string[] Options { get; private set; } = [];
    public event Action? InteractionPressed;
    public bool SpeechVisible => _bubble.Visible;
    public string SpeechText => _speech.Text;
    public string RecipeText => _recipe.Text;
    private Label _prompt = null!, _recipe = null!, _message = null!;
    private PanelContainer _menu = null!, _bubble = null!, _recipePanel = null!;
    private VBoxContainer _choices = null!;
    private Label _question = null!, _speech = null!;
    private Action<int>? _chosen;
    private float _messageTime, _speechTime;
    private bool _hints = true;
    private DrinkSession? _displayedSession;
    private int _displayedRevision = -1;
    private string _promptText = "";
    public void Build(FirstPersonController player)
    {
        Name = "DrinkHud";
        Layer = 3;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        _recipePanel = new PanelContainer { Position = new(24, 100), Size = new(390, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _recipePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new("f5f1e9e8"), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12 });
        root.AddChild(_recipePanel);
        _recipe = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        _recipe.AddThemeColorOverride("font_color", new("1c4235"));
        _recipePanel.AddChild(_recipe);
        _prompt = new Label { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = .67f, AnchorBottom = .67f, OffsetLeft = -300, OffsetRight = 300, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        _prompt.AddThemeColorOverride("font_color", Colors.White);
        _prompt.AddThemeColorOverride("font_outline_color", new("102e26"));
        _prompt.AddThemeConstantOverride("outline_size", 7);
        _prompt.AddThemeFontSizeOverride("font_size", 23);
        root.AddChild(_prompt);
        var dot = new Label { Text = "·", AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = .5f, AnchorBottom = .5f, OffsetLeft = -4, OffsetTop = -16, MouseFilter = Control.MouseFilterEnum.Ignore };
        dot.AddThemeFontSizeOverride("font_size", 28);
        root.AddChild(dot);
        var interact = new Button { Text = "Interact", AnchorLeft = 1, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -178, OffsetRight = -28, OffsetTop = -130, OffsetBottom = -82, Visible = DisplayServer.IsTouchscreenAvailable(), FocusMode = Control.FocusModeEnum.None };
        interact.Pressed += () => InteractionPressed?.Invoke();
        root.AddChild(interact);
        _message = new Label { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = .78f, AnchorBottom = .78f, OffsetLeft = -430, OffsetRight = 430, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        _message.AddThemeColorOverride("font_color", Colors.White);
        _message.AddThemeColorOverride("font_outline_color", new("102e26"));
        _message.AddThemeConstantOverride("outline_size", 6);
        root.AddChild(_message);
        _menu = Panel(root);
        _menu.AnchorLeft = _menu.AnchorRight = .5f;
        _menu.AnchorTop = _menu.AnchorBottom = .5f;
        _menu.OffsetLeft = -280;
        _menu.OffsetRight = 280;
        _menu.OffsetTop = -180;
        _menu.OffsetBottom = 180;
        var list = new VBoxContainer();
        _menu.AddChild(list);
        _question = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center };
        _question.AddThemeFontSizeOverride("font_size", 23);
        list.AddChild(_question);
        _choices = new VBoxContainer();
        list.AddChild(_choices);
        var help = new Label { Text = "↑ ↓ / ← → select   •   SPACE confirm   •   ESC back", HorizontalAlignment = HorizontalAlignment.Center };
        help.AddThemeFontSizeOverride("font_size", 14);
        list.AddChild(help);
        _menu.Hide();
        _bubble = Panel(root);
        _speech = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _speech.AddThemeFontSizeOverride("font_size", 24);
        _bubble.AddChild(_speech);
        _bubble.Size = new(220, 70);
        _bubble.Hide();
        player.ToggleHud += () => { _hints = !_hints; _recipePanel.Visible = _hints; };
    }
    private static PanelContainer Panel(Control root)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new("183b35f5"), CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12, ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 18, ContentMarginBottom = 18 });
        root.AddChild(panel);
        return panel;
    }
    public void Prompt(string text)
    {
        if (_promptText == text)
            return;
        _promptText = text;
        _prompt.Text = text;
    }
    public void Message(string text)
    {
        _message.Text = text;
        _messageTime = 3.5f;
    }
    public void Speech(string text)
    {
        _speech.Text = text;
        _speechTime = 4;
        _bubble.Show();
    }
    public void Open(string question, string[] options, Action<int> chosen)
    {
        _question.Text = question;
        Options = options;
        Selected = 0;
        _chosen = chosen;
        foreach (var child in _choices.GetChildren())
        {
            _choices.RemoveChild(child);
            child.QueueFree();
        }
        for (var i = 0; i < options.Length; i++)
        {
            var index = i;
            var b = new Button { Text = options[i], CustomMinimumSize = new(0, 34), FocusMode = Control.FocusModeEnum.None };
            b.Pressed += () => { Selected = index; Choose(); };
            _choices.AddChild(b);
        }
        _menu.Show();
        Refresh();
    }
    public void MoveSelection(int direction)
    {
        Selected = (Selected + direction + Options.Length) % Options.Length;
        Refresh();
    }
    private void Refresh()
    {
        for (var i = 0; i < _choices.GetChildCount(); i++)
            _choices.GetChild<Button>(i).Text = (i == Selected ? "› " : "  ") + Options[i];
    }
    public void Choose()
    {
        var callback = _chosen;
        var selected = Selected;
        Close();
        callback?.Invoke(selected);
    }
    public void Close()
    {
        _menu.Hide();
        _chosen = null;
    }
    public void Update(double delta, DrinkSession session, Camera3D camera, Vector3 head)
    {
        _messageTime -= (float)delta;
        if (_messageTime <= 0)
            _message.Text = "";
        _speechTime -= (float)delta;
        _bubble.Visible = _speechTime > 0 && !camera.IsPositionBehind(head);
        _bubble.Position = camera.UnprojectPosition(head) - new Vector2(110, 85);
        if (ReferenceEquals(_displayedSession, session) && _displayedRevision == session.Revision)
            return;
        _displayedSession = session;
        _displayedRevision = session.Revision;
        var lines = session.Active ? session.Recipe.Steps.Where(s => s.Action <= DrinkAction.Serve).Select(s => (session.StepDone(s.Id) ? "✓ " : "○ ") + s.Hint) : [];
        _recipe.Text = session.Active ? session.Recipe.Name + "\n" + string.Join("\n", lines) + "\n\n" + (session.Drinking ? "Customer is enjoying the drink…" : string.Join(" / ", session.NextSteps.Select(s => s.Hint))) : session.Asleep ? "Shift complete — start a new shift at the customer." : "Aim at the customer and press SPACE to take an order from the workstation.";
    }
}
