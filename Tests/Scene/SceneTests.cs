// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist.Tests;

public static class SceneTests
{
    public static async Task Run(Node3D scene, FirstPersonController player, SceneHud hud)
    {
        await UiTests.Run(scene, player, hud);
        var tables = scene.GetNode("FourTables").GetChildCount();
        var bottles = scene.GetNode("BackBar/TopShelf").GetChildren().OfType<Node3D>().Count(n => n.GetNodeOrNull("BottleBody") != null)
            + scene.GetNode("BackBar/BottomShelf").GetChildren().OfType<Node3D>().Count(n => n.GetNodeOrNull("BottleBody") != null);
        var recessedIce = scene.GetNode<Node3D>("IceWell/BasinFloor").GlobalPosition.Y < MargaritaStation.WorktopHeight - .25f;
        var clearCounter = new[] { "MargaritaStation", "FreshPrep", "MargaritaIngredients", "IceWell", "IceScoop" }
            .All(path => scene.GetNode<Node3D>(path).GlobalPosition.Z > MargaritaStation.CustomerEdgeZ + .3f);
        var mesh = scene.GetNode<MeshInstance3D>("MargaritaStation/LargeShakerTin/TinBody").Mesh;
        var arrays = mesh.SurfaceGetArrays(0);
        var vertices = arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Godot.Mesh.ArrayType.Normal].AsVector3Array();
        var sideIndex = Array.FindIndex(vertices, v => v.Y > .05f && v.Y < .18f);
        var outwardNormals = sideIndex >= 0 && normals[sideIndex].Dot(new Vector3(vertices[sideIndex].X, 0, vertices[sideIndex].Z)) > 0;
        var floor = (StandardMaterial3D)scene.GetNode<MeshInstance3D>("Architecture/PhotographicOakFloor").MaterialOverride;
        var maps4k = floor.AlbedoTexture?.GetWidth() == 4096 && floor.NormalTexture?.GetWidth() == 4096;
        var customer = scene.GetNode<SeatedCustomer>("SeatedCustomer");
        var guests = scene.GetNode("BackgroundPatrons").GetChildren().OfType<PatronActor>().ToArray();
        var actors = guests.Prepend(customer).ToArray();
        var fittedCustomers = actors.Length == 6 && actors.All(a => a.SkinMesh.Skin != null &&
            a.Rig.GetBoneCount() > 150 && a.Rig.FindBone("neck01") >= 0 &&
            ((StandardMaterial3D)a.ClothingMesh.GetSurfaceOverrideMaterial(0)).NormalTexture?.GetWidth() >= 2048);
        var well = scene.GetNode<Node3D>("WellRail");
        var wellBottles = well.GetChildren().OfType<Node3D>().Count(n => n.HasNode("BottleBody"));
        var railBelowIce = well.GlobalPosition.Y < .5f &&
            well.GetChildren().OfType<Node3D>().Where(n => n.HasNode("BottleBody")).All(n => n.GlobalPosition.X > .3f && n.GlobalPosition.X < 1.5f);
        var glassStacks = scene.GetNode("CleanGlassStacks").GetChildCount() == 8;
        var washArea = scene.GetNode<Node3D>("WashSink").Position.X < -2 &&
            scene.GetNode<Node3D>("Dishwasher").Position.X < -3;
        var groupDrinks = guests.Count(a => a.HasNode("Cosmopolitan")) == 3 && guests.Count(a => a.HasNode("OldFashioned")) == 2;
        var iceCubes = scene.GetNode("IceWell/IcePile").GetChildren().OfType<Node3D>().Where(n => n.HasNode("MeltedShell")).ToArray();
        var detailedIce = iceCubes.Length == IceDetails.CubeCount && iceCubes.All(n => n.HasNode("CloudyCore")) &&
            scene.GetNode<MultiMeshInstance3D>("IceWell/IcePile/TrappedAirBubbles").Multimesh.InstanceCount == IceDetails.CubeCount * 10;
        var labelsLoaded = new[] { "TopShelf", "BottomShelf" }.SelectMany(s => scene.GetNode($"BackBar/{s}").GetChildren())
            .OfType<Node3D>().Where(n => n.HasNode("BottleBody")).All(n =>
                ((StandardMaterial3D)n.GetNode<MeshInstance3D>("PrintedCurvedLabel").MaterialOverride).AlbedoTexture?.GetWidth() == 1536);
        var spring = scene.GetNode<MeshInstance3D>("MargaritaStation/HawthorneStrainer/CoiledSpringWire").Mesh;
        var detailedTools = spring.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Length > 3000 &&
            scene.GetNode<MultiMeshInstance3D>("MargaritaStation/MatDrainageLugs").Multimesh.InstanceCount == 1320;
        var animations = CheckAnimations(actors, customer, player);
        Check("four tables", tables == 4);
        Check("ten display bottles", bottles == 10);
        Check("first-person camera", player.Camera.Current);
        Check("recessed ice and clear customer counter", recessedIce && clearCounter);
        Check("outward tool normals", outwardNormals);
        Check("4K material maps", maps4k);
        Check("detailed labels and tools", labelsLoaded && detailedTools);
        Check("six skinned customers", fittedCustomers);
        Check("ice shells, cores and bubbles", detailedIce);
        Check("six well bottles below the ice", wellBottles == 6 && railBelowIce);
        Check("eight clean glasses", glassStacks);
        Check("left-side wash area", washArea);
        Check("three cosmopolitans and two old fashioneds", groupDrinks);
        Check("waving, smiling, sipping and gaze tracking", animations);
        GD.Print("SCENE_TESTS passed=14");
    }

    private static bool CheckAnimations(PatronActor[] actors, PatronActor customer, FirstPersonController player)
    {
        foreach (var actor in actors) actor.SetProcess(false);
        var sipSeen = new HashSet<PatronActor>();
        var waveSeen = false;
        var smileSeen = false;
        var previous = new Dictionary<PatronActor, Vector3>();
        var maxStep = 0f;
        for (var frame = 0; frame <= 26 * 60; frame++)
            foreach (var actor in actors)
            {
                actor.Evaluate(frame / 60f, 1f / 60);
                var wrist = actor.Rig.GetBoneGlobalPose(actor.Rig.FindBone("wrist_R")).Origin;
                if (frame > 60 && previous.TryGetValue(actor, out var last)) maxStep = Mathf.Max(maxStep, last.DistanceTo(wrist));
                previous[actor] = wrist;
                if (actor.IsBarPatron)
                {
                    waveSeen |= actor.WaveAmount > .95f && wrist.Y > 1.65f;
                    smileSeen |= actor.SmileAmount > .5f && actor.Rig.GetBonePoseRotation(actor.Rig.FindBone("levator02_L")).AngleTo(Quaternion.Identity) > .01f;
                }
                else if (actor.SipAmount > .95f)
                {
                    var cosmo = actor.HasNode("Cosmopolitan");
                    var drink = actor.GetNode<Node3D>(cosmo ? "Cosmopolitan" : "OldFashioned");
                    var mouth = actor.Rig.GlobalTransform * (actor.Rig.GetBoneGlobalPose(actor.Rig.FindBone("head")) * new Vector3(0, -.045f, .134f));
                    var rim = drink.GlobalTransform * new Vector3(0, cosmo ? .157f : .106f, cosmo ? -.048f : -.035f);
                    if (rim.DistanceTo(mouth) < .025f && drink.Position.Y > 1.15f) sipSeen.Add(actor);
                }
            }
        var gaze = true;
        var minAlignment = 1f;
        foreach (var x in new[] { -3.6f, 0, 3.6f })
        {
            player.Position = new(x, FirstPersonController.StartPosition.Y, 2.55f);
            for (var frame = 0; frame < 180; frame++) customer.Evaluate(9 + frame / 60f, 1f / 60);
            foreach (var eyeName in new[] { "eye_L", "eye_R" })
            {
                var eye = customer.Rig.GlobalTransform * customer.Rig.GetBoneGlobalPose(customer.Rig.FindBone(eyeName));
                var alignment = eye.Basis.Z.Normalized().Dot((player.Camera.GlobalPosition - eye.Origin).Normalized());
                minAlignment = Mathf.Min(minAlignment, alignment);
                gaze &= alignment > .98f;
            }
        }
        player.ResetView();
        GD.Print($"ANIMATION_CHECK wave={waveSeen} smile={smileSeen} sipping_guests={sipSeen.Count} max_wrist_step={maxStep:F4} gaze_alignment={minAlignment:F4}");
        return waveSeen && smileSeen && sipSeen.Count == 5 && maxStep < .04f && gaze;
    }
    private static void Check(string name, bool condition)
    {
        if (!condition) throw new InvalidOperationException($"Scene test failed: {name}");
        GD.Print($"PASS scene/{name}");
    }
}
