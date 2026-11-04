// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

public partial class SceneHud : CanvasLayer
{
    public Button TurnButton { get; private set; } = null!;

    public void Build(FirstPersonController player)
    {
        Name = "SceneHud";
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var title = new Label { Text = "BARTENDER SPECIALOGIST", Position = new(32, 28) };
        title.AddThemeFontSizeOverride("font_size", 23);
        title.AddThemeColorOverride("font_color", new Color("21443a"));
        title.AddThemeColorOverride("font_shadow_color", new Color(1, 1, 1, .55f));
        title.AddThemeConstantOverride("shadow_offset_x", 1);
        title.AddThemeConstantOverride("shadow_offset_y", 1);
        title.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(title);
        var subtitle = new Label
        {
            Text = "THE NEIGHBORHOOD BAR", Position = new(33, 60),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        subtitle.AddThemeFontSizeOverride("font_size", 12);
        subtitle.AddThemeColorOverride("font_color", new Color("406052"));
        root.AddChild(subtitle);

        var buttons = new HBoxContainer { OffsetLeft = -330, OffsetTop = 30, OffsetRight = -32, OffsetBottom = 74 };
        buttons.AnchorLeft = buttons.AnchorRight = 1;
        buttons.AddThemeConstantOverride("separation", 10);
        root.AddChild(buttons);
        TurnButton = AddButton(buttons, "Turn around", player.TurnAround);
        AddButton(buttons, "Reset view", player.ResetView);

        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AnchorLeft = panel.AnchorRight = .5f;
        panel.AnchorTop = panel.AnchorBottom = 1;
        panel.OffsetLeft = -440; panel.OffsetRight = 440;
        panel.OffsetTop = -67; panel.OffsetBottom = -24;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("183b35e8"), CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 10, ContentMarginBottom = 10
        });
        root.AddChild(panel);
        var hints = new Label
        {
            Text = DisplayServer.IsTouchscreenAvailable()
                ? "LEFT DRAG  move     •     RIGHT DRAG  look     •     Turn around to see the bottle shelves"
                : "WASD  move     •     CLICK + MOUSE / ARROWS  look     •     B  turn     •     R  reset     •     H  hide UI     •     ESC  cursor",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hints.AddThemeFontSizeOverride("font_size", 14);
        hints.AddThemeColorOverride("font_color", new Color("f1ead8"));
        panel.AddChild(hints);
        player.ToggleHud += () => Visible = !Visible;
    }

    private static Button AddButton(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, CustomMinimumSize = new(140, 42), FocusMode = Control.FocusModeEnum.None };
        var normal = new StyleBoxFlat
        {
            BgColor = new Color("faf7eee8"), BorderColor = new Color("d8d3c2"),
            BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color("e4eadc");
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeColorOverride("font_color", new Color("21443a"));
        button.AddThemeColorOverride("font_hover_color", new Color("21443a"));
        button.AddThemeColorOverride("font_pressed_color", new Color("21443a"));
        button.AddThemeFontSizeOverride("font_size", 15);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
