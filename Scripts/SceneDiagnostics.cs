// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>Optional scene checks and captures; normal play does not enter these paths.</summary>
public static class SceneDiagnostics
{
    public static async void Run(Node3D scene, FirstPersonController player, SceneHud hud)
    {
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("--smoke-test") || args.Contains("--test"))
        {
            try
            {
                await Tests.SceneTests.Run(scene, player, hud);
                scene.GetTree().Quit();
            }
            catch (Exception error)
            {
                GD.PushError(error.ToString());
                scene.GetTree().Quit(1);
            }
            return;
        }
        if (args.Contains("--no-hud")) hud.Visible = false;
        var capture = args.FirstOrDefault(a => a.StartsWith("--capture="));
        if (capture == null && !args.Contains("--movie-view")) return;
        if (args.Contains("--back")) player.TurnAround();
        if (args.Contains("--wide-back")) player.Position = new(0, FirstPersonController.StartPosition.Y, 1.78f);
        if (args.Contains("--workstation-detail"))
        {
            player.Position = new(-.25f, FirstPersonController.StartPosition.Y, 2.14f);
            player.Camera.LookAt(new(-.25f, .98f, 1.03f), Vector3.Up);
        }
        if (args.Contains("--prep-detail"))
        {
            player.Position = new(-2.55f, FirstPersonController.StartPosition.Y, 1.75f);
            player.Camera.LookAt(new(-2.55f, .94f, 1.03f), Vector3.Up);
        }
        if (args.Contains("--customer-detail"))
        {
            player.Position = new(.55f, FirstPersonController.StartPosition.Y, 1.67f);
            player.Camera.Fov = 38;
            player.Camera.LookAt(new(.95f, 1.43f, -.88f), Vector3.Up);
        }
        if (args.Contains("--women-detail"))
        {
            player.Position = new(2.45f, FirstPersonController.StartPosition.Y, 1.67f);
            player.Camera.Fov = 35;
            player.Camera.LookAt(new(3.55f, 1.0f, -3.7f), Vector3.Up);
        }
        if (args.Contains("--men-detail"))
        {
            player.Position = new(-2.45f, FirstPersonController.StartPosition.Y, 1.67f);
            player.Camera.Fov = 35;
            player.Camera.LookAt(new(-3.55f, 1.0f, -3.7f), Vector3.Up);
        }
        if (args.Contains("--wash-detail"))
        {
            player.Position = new(-2.7f, FirstPersonController.StartPosition.Y, 2.3f);
            player.Camera.Fov = 65;
            player.Camera.LookAt(new(-2.95f, .65f, 1.0f), Vector3.Up);
        }
        if (args.Contains("--rail-detail"))
        {
            player.Position = new(1.15f, FirstPersonController.StartPosition.Y, 2.4f);
            player.Camera.Fov = 64;
            player.Camera.LookAt(new(1.1f, .65f, 1.05f), Vector3.Up);
        }
        if (args.Contains("--ice-detail"))
        {
            player.Position = new(.9f, FirstPersonController.StartPosition.Y, 1.7f);
            player.Camera.Fov = 44;
            player.Camera.LookAt(new(.9f, .79f, .96f), Vector3.Up);
        }
        if (args.Contains("--tools-detail"))
        {
            player.Position = new(-.85f, FirstPersonController.StartPosition.Y, 1.7f);
            player.Camera.Fov = 52;
            player.Camera.LookAt(new(-.85f, .99f, 1.03f), Vector3.Up);
        }
        if (capture == null) return;
        Viewport target = scene.GetViewport();
        if (args.Contains("--capture-4k"))
        {
            // The offscreen 4K reference path is validated with Compatibility. Forward+
            // continues to render normally at the window/display's native resolution.
            if (RenderingServer.GetCurrentRenderingMethod() != "gl_compatibility")
            {
                GD.PushError("For offscreen 4K capture, launch with --rendering-method gl_compatibility.");
                scene.GetTree().Quit(1);
                return;
            }
            // Render the same game viewport at native 4K, scaled down only for the host window.
            // Readback captures the original 3840x2160 render, not the monitor's scaled image.
            hud.Visible = false;
            target.UseTaa = false;
            target.Msaa3D = Viewport.Msaa.Disabled;
            scene.GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            scene.GetWindow().ContentScaleSize = new(3840, 2160);
        }
        for (var i = 0; i < 64; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
        var at = args.FirstOrDefault(a => a.StartsWith("--capture-at="));
        if (at != null && float.TryParse(at["--capture-at=".Length..], System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            while (scene.GetNode<SeatedCustomer>("SeatedCustomer").AnimationTime < seconds)
                await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
        await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var path = capture["--capture=".Length..];
        var frame = target.GetTexture().GetImage();
        var result = frame.SavePng(path);
        GD.Print($"CAPTURE {result} {frame.GetWidth()}x{frame.GetHeight()}: {path}");
        scene.GetTree().Quit(result == Error.Ok ? 0 : 1);
    }
}
