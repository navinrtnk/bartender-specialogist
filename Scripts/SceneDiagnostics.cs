// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>Optional scene checks and captures; normal play does not enter these paths.</summary>
public static class SceneDiagnostics
{
    public static async void Run(Node3D scene, FirstPersonController player, SceneHud hud)
    {
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("--smoke-test"))
        {
            for (var i = 0; i < 3; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            var tables = scene.GetNode("FourTables").GetChildCount();
            var bottles = scene.GetNode("BackBar/TopShelf").GetChildren().OfType<Node3D>().Count(n => n.GetNodeOrNull("BottleBody") != null)
                + scene.GetNode("BackBar/BottomShelf").GetChildren().OfType<Node3D>().Count(n => n.GetNodeOrNull("BottleBody") != null);
            // Send actual viewport input, rather than invoking button handlers directly.
            var center = scene.GetViewport().GetFinalTransform() * hud.TurnButton.GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseMotion { Position = center, GlobalPosition = center });
            Input.ParseInputEvent(new InputEventMouseButton { Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = true });
            Input.ParseInputEvent(new InputEventMouseButton { Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = false });
            await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            var buttonWorks = Mathf.Abs(player.Camera.Rotation.Y) > 3;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.R, Keycode = Key.R, Pressed = true });
            await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            var resetWorks = player.Position.IsEqualApprox(FirstPersonController.StartPosition) && Mathf.IsZeroApprox(player.Camera.Rotation.Y);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.H, Keycode = Key.H, Pressed = true });
            await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame);
            var hudWorks = !hud.Visible;
            var recessedIce = scene.GetNode<Node3D>("IceWell/BasinFloor").GlobalPosition.Y < MargaritaStation.WorktopHeight - .25f;
            var clearCounter = new[] { "MargaritaStation", "FreshPrep", "MargaritaIngredients", "IceWell", "IceScoop" }
                .All(path => scene.GetNode<Node3D>(path).GlobalPosition.Z > MargaritaStation.CustomerEdgeZ + .3f);
            var mesh = scene.GetNode<MeshInstance3D>("MargaritaStation/LargeShakerTin").Mesh;
            var arrays = mesh.SurfaceGetArrays(0);
            var vertices = arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
            var normals = arrays[(int)Godot.Mesh.ArrayType.Normal].AsVector3Array();
            var sideIndex = Array.FindIndex(vertices, v => v.Y > .05f && v.Y < .18f);
            var outwardNormals = sideIndex >= 0 && normals[sideIndex].Dot(new Vector3(vertices[sideIndex].X, 0, vertices[sideIndex].Z)) > 0;
            var floor = (StandardMaterial3D)scene.GetNode<MeshInstance3D>("Architecture/PhotographicOakFloor").MaterialOverride;
            var maps4k = floor.AlbedoTexture?.GetWidth() == 4096 && floor.NormalTexture?.GetWidth() == 4096;
            var face = scene.GetNode<MeshInstance3D>("SeatedCustomer/UpperBody/AnatomicalHead/FaceAndNeck");
            var anatomicalHead = face.Mesh.GetSurfaceCount() > 0;
            var clothes = scene.GetNode<MeshInstance3D>("SeatedCustomer/UpperBody/FittedClothing");
            var connectedNeck = face.ToGlobal(face.Mesh.GetAabb().Position).Y < clothes.ToGlobal(clothes.Mesh.GetAabb().End).Y - .08f;
            var anatomicalHands = scene.GetNode<MeshInstance3D>("SeatedCustomer/UpperBody/AnatomicalHands").Mesh.GetSurfaceCount() > 0;
            var clothMaps = (StandardMaterial3D)clothes.MaterialOverride;
            var fittedCustomer = connectedNeck && anatomicalHands && clothMaps.NormalTexture?.GetWidth() >= 2048;
            var iceCubes = scene.GetNode("IceWell/IcePile").GetChildren().OfType<Node3D>().Where(n => n.HasNode("MeltedShell")).ToArray();
            var detailedIce = iceCubes.Length == IceDetails.CubeCount && iceCubes.All(n => n.HasNode("CloudyCore")) &&
                scene.GetNode<MultiMeshInstance3D>("IceWell/IcePile/TrappedAirBubbles").Multimesh.InstanceCount == IceDetails.CubeCount * 10;
            var labelsLoaded = new[] { "TopShelf", "BottomShelf" }.SelectMany(s => scene.GetNode($"BackBar/{s}").GetChildren())
                .OfType<Node3D>().Where(n => n.HasNode("BottleBody")).All(n =>
                    ((StandardMaterial3D)n.GetNode<MeshInstance3D>("PrintedCurvedLabel").MaterialOverride).AlbedoTexture?.GetWidth() == 1536);
            var spring = scene.GetNode<MeshInstance3D>("MargaritaStation/HawthorneStrainer/CoiledSpringWire").Mesh;
            var detailedTools = spring.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Length > 3000 &&
                scene.GetNode<MultiMeshInstance3D>("MargaritaStation/MatDrainageLugs").Multimesh.InstanceCount == 1320;
            var valid = tables == 4 && bottles == 10 && player.Camera.Current && scene.HasNode("SeatedCustomer") && buttonWorks && resetWorks && hudWorks && recessedIce && clearCounter && outwardNormals && maps4k && anatomicalHead && labelsLoaded && detailedTools && fittedCustomer && detailedIce;
            GD.Print($"SCENE_CHECK tables={tables} display_bottles={bottles} customer={scene.HasNode("SeatedCustomer")} camera_current={player.Camera.Current} turn_button={buttonWorks} reset_key={resetWorks} hide_ui_key={hudWorks} recessed_ice={recessedIce} clear_customer_counter={clearCounter} outward_normals={outwardNormals} maps_4k={maps4k} anatomical_head={anatomicalHead} labels_loaded={labelsLoaded} detailed_tools={detailedTools} neck_connected={connectedNeck} fitted_customer={fittedCustomer} detailed_ice={detailedIce}");
            scene.GetTree().Quit(valid ? 0 : 1);
            return;
        }
        var capture = args.FirstOrDefault(a => a.StartsWith("--capture="));
        if (capture == null) return;
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
        if (args.Contains("--no-hud")) hud.Visible = false;
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
        await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var path = capture["--capture=".Length..];
        var frame = target.GetTexture().GetImage();
        var result = frame.SavePng(path);
        GD.Print($"CAPTURE {result} {frame.GetWidth()}x{frame.GetHeight()}: {path}");
        scene.GetTree().Quit(result == Error.Ok ? 0 : 1);
    }
}
