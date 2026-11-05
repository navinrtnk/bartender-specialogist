// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist.Tests;

/// <summary>Exercises the real viewport and input routing, without calling UI handlers.</summary>
public static class UiTests
{
    public static async Task Run(Node3D scene, FirstPersonController player, SceneHud hud)
    {
        var window = scene.GetWindow();
        var originalSize = window.Size;
        player.SetProcess(false);
        try
        {
            player.ResetView();
            hud.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            foreach (var size in new[] { new Vector2I(1440, 810), new Vector2I(960, 540) })
            {
                window.Size = size;
                await Frames(scene);
                var viewport = scene.GetViewport().GetVisibleRect();
                Check($"buttons fit {size}", viewport.Encloses(hud.TurnButton.GetGlobalRect()) && viewport.Encloses(hud.ResetButton.GetGlobalRect()));
                Click(scene, hud.TurnButton);
                await Frames(scene);
                Check($"turn button {size}", Mathf.Abs(player.Camera.Rotation.Y) > 3);
                player.Position += Vector3.Right;
                Click(scene, hud.ResetButton);
                await Frames(scene);
                Check($"reset button {size}", player.Position.IsEqualApprox(FirstPersonController.StartPosition) && Mathf.IsZeroApprox(player.Camera.Rotation.Y));
            }
            PressKey(Key.B);
            await Frames(scene);
            Check("B turns the view", Mathf.Abs(player.Camera.Rotation.Y) > 3);
            PressKey(Key.R);
            await Frames(scene);
            Check("R resets the view", Mathf.IsZeroApprox(player.Camera.Rotation.Y));
            PressKey(Key.H);
            await Frames(scene);
            Check("H hides the HUD", !hud.Visible);
            PressKey(Key.H, echo: true);
            await Frames(scene);
            Check("held H does not flicker the HUD", !hud.Visible);
            Click(scene, hud.TurnButton);
            await Frames(scene);
            Check("hidden buttons do not receive clicks", Mathf.IsZeroApprox(player.Camera.Rotation.Y));
            Input.MouseMode = Input.MouseModeEnum.Visible;
            PressKey(Key.H);
            await Frames(scene);
            Check("H restores the HUD", hud.Visible);

            var point = scene.GetViewport().GetVisibleRect().Size * new Vector2(.75f, .5f);
            var screenPoint = scene.GetViewport().GetFinalTransform() * point;
            var before = player.Camera.Rotation;
            Input.ParseInputEvent(new InputEventScreenTouch { Index = 1, Pressed = true, Position = screenPoint });
            Input.ParseInputEvent(new InputEventScreenDrag { Index = 1, Position = screenPoint + new Vector2(24, -12), Relative = new(24, -12) });
            Input.ParseInputEvent(new InputEventScreenTouch { Index = 1, Pressed = false, Position = screenPoint });
            await Frames(scene);
            Check("right-side touch drag looks around", !player.Camera.Rotation.IsEqualApprox(before));
            var after = player.Camera.Rotation;
            Input.ParseInputEvent(new InputEventScreenDrag { Index = 1, Position = screenPoint, Relative = new(50, 0) });
            await Frames(scene);
            Check("released touch stops looking", player.Camera.Rotation.IsEqualApprox(after));
            GD.Print("UI_TESTS passed=14");
        }
        finally
        {
            window.Size = originalSize;
            player.ResetView();
            player.SetProcess(true);
            hud.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    private static async Task Frames(Node scene)
    {
        for (var i = 0; i < 3; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Click(Node scene, Button button)
    {
        var center = scene.GetViewport().GetFinalTransform() * button.GetGlobalRect().GetCenter();
        Input.ParseInputEvent(new InputEventMouseMotion { Position = center, GlobalPosition = center });
        foreach (var pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = pressed });
    }

    private static void PressKey(Godot.Key key, bool echo = false)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true, Echo = echo });
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) throw new InvalidOperationException($"UI test failed: {name}");
        GD.Print($"PASS ui/{name}");
    }
}
