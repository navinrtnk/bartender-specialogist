// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public static class MargaritaStation
{
    public const float WorktopHeight = .9f;
    public const float CustomerEdgeZ = .54f;
    private const float Counter = WorktopHeight + .012f;

    public static void Build(Node3D root, ScenePalette p)
    {
        BuildBench(root, p);
        var station = Group(root, "MargaritaStation", new(-.85f, Counter, 1.03f));
        HeroProps.Tools(station, p);
        var prep = Group(root, "FreshPrep", new(-2.55f, Counter, 1.03f));
        HeroProps.Prep(prep, p);
        var ingredients = Group(root, "MargaritaIngredients", new(-3.5f, .64f, 1.44f));
        RoundedBox(ingredients, "SpeedRailBase", new(.64f, .025f, .19f), new(.14f, -.013f, 0), p.Steel);
        RoundedBox(ingredients, "SpeedRailLip", new(.64f, .12f, .025f), new(.14f, .04f, .10f), p.Steel);
        DetailedBottle.Build(ingredients, p, new("Blanco tequila", "well-tequila", "TEQUILA", "c7d5bd", "e4e8d6", .10f, .29f), new(0, 0, -.025f), pourSpout: true);
        DetailedBottle.Build(ingredients, p, new("Orange liqueur", "triple-sec", "LIQUEUR", "a98952", "e5dfc1", .10f, .29f, 3.5f), new(.22f, 0, -.025f), pourSpout: true);
        HeroProps.Salt(prep, p, new(.46f, 0, 0));
        BuildIceWell(root, p);
        HeroProps.Towel(root, p, new(2.15f, Counter + .008f, 1.17f));
    }

    private static void BuildBench(Node root, ScenePalette p)
    {
        var bench = Group(root, "BartenderBench");
        // Four separate top sections leave a real opening into the ice basin.
        RoundedBox(bench, "LeftWorktop", new(4.3f, .045f, .78f), new(-1.75f, WorktopHeight - .0225f, .95f), p.Steel);
        RoundedBox(bench, "RightWorktop", new(2.5f, .045f, .78f), new(2.65f, WorktopHeight - .0225f, .95f), p.Steel);
        RoundedBox(bench, "IceWellFarBridge", new(1, .045f, .1f), new(.9f, WorktopHeight - .0225f, .61f), p.Steel);
        RoundedBox(bench, "IceWellNearBridge", new(1, .045f, .1f), new(.9f, WorktopHeight - .0225f, 1.29f), p.Steel);
        RoundedBox(bench, "RearSplash", new(7.8f, .1f, .025f), new(0, .95f, .57f), p.Steel);
        RoundedBox(bench, "FrontApron", new(7.8f, .16f, .04f), new(0, .79f, 1.32f), p.BasinSteel);
        foreach (var x in new[] { -3.7f, -.1f, 1.8f, 3.7f })
        {
            Cylinder(bench, "AdjustableLeg", .024f, .024f, .77f, new(x, .43f, 1.18f), p.Steel);
            Cylinder(bench, "RubberFoot", .029f, .029f, .055f, new(x, .055f, 1.18f), p.Rubber);
        }
        RoundedBox(bench, "UnderBenchShelf", new(7.4f, .025f, .5f), new(0, .23f, .99f), ScenePalette.Matte("454e4c", .9f));
    }

    private static void BuildIceWell(Node root, ScenePalette p)
    {
        var ice = Group(root, "IceWell", new(.9f, WorktopHeight, .95f));
        var wetSteel = (StandardMaterial3D)p.BasinSteel.Duplicate();
        wetSteel.AlbedoColor = new("586469");
        wetSteel.Roughness = .58f;
        RoundedBox(ice, "BasinFloor", new(.99f, .025f, .57f), new(0, -.33f, 0), wetSteel, .012f);
        foreach (var x in new[] { -.5f, .5f })
        {
            RoundedBox(ice, "BasinSide", new(.018f, .32f, .59f), new(x, -.16f, 0), wetSteel);
            RoundedBox(ice, "RimSide", new(.033f, .014f, .64f), new(x, .006f, 0), p.Steel, .005f);
        }
        foreach (var z in new[] { -.29f, .29f })
        {
            RoundedBox(ice, "BasinEnd", new(1, .32f, .018f), new(0, -.16f, z), wetSteel);
            RoundedBox(ice, "RimEnd", new(1.03f, .014f, .033f), new(0, .006f, z), p.Steel, .005f);
        }
        Cylinder(ice, "Drain", .03f, .03f, .005f, new(0, -.313f, 0), p.Ink);
        Ring(ice, "DrainSurround", .034f, .004f, new(0, -.309f, 0), p.Steel);
        IceDetails.Fill(ice);

        // A separate dry cradle keeps the scoop handle clear of the ice.
        var scoop = Group(root, "IceScoop", new(1.66f, WorktopHeight + .055f, 1.02f));
        RoundedBox(scoop, "DryCradle", new(.14f, .015f, .25f), new(0, -.043f, .025f), p.Rubber, .006f);
        var surface = new SurfaceTool();
        surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for (var i = 0; i < 32; i++)
        {
            var a = -Mathf.Pi / 2 + i * Mathf.Pi / 32;
            var b = a + Mathf.Pi / 32;
            var left = new Vector3(Mathf.Sin(a) * .048f, -Mathf.Cos(a) * .035f, -.09f);
            var right = new Vector3(Mathf.Sin(b) * .048f, -Mathf.Cos(b) * .035f, -.09f);
            var depth = new Vector3(0, 0, .145f);
            surface.AddVertex(left); surface.AddVertex(right); surface.AddVertex(left + depth);
            surface.AddVertex(right); surface.AddVertex(right + depth); surface.AddVertex(left + depth);
            surface.AddVertex(new(0, 0, .055f)); surface.AddVertex(left + depth); surface.AddVertex(right + depth);
        }
        surface.GenerateNormals();
        var scoopMetal = (StandardMaterial3D)p.Steel.Duplicate();
        scoopMetal.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(scoop, "ScoopBowl", surface.Commit(), Vector3.Zero, scoopMetal);
        foreach (var side in new[] { -1f, 1f })
            Tube(scoop, "RolledScoopRim", [new(side * .048f, 0, -.09f), new(side * .048f, 0, .055f)], .0018f, p.ToolSteel, 8);
        var nose = new Vector3[25];
        for (var i = 0; i < nose.Length; i++)
        {
            var a = -Mathf.Pi / 2 + i * Mathf.Pi / (nose.Length - 1);
            nose[i] = new(Mathf.Sin(a) * .048f, -Mathf.Cos(a) * .035f, -.09f);
        }
        Tube(scoop, "RoundedScoopMouth", nose, .0016f, p.ToolSteel, 8);
        Rod(scoop, "ScoopHandle", new(0, -.009f, .055f), new(0, .005f, .23f), .012f, p.Steel);
        Ring(scoop, "HandleHangingEye", .013f, .003f, new(0, .005f, .232f), p.Steel).RotationDegrees = new(90, 0, 0);
    }

}
