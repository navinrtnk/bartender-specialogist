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
        var station = Group(root, "MargaritaStation", new(-.65f, Counter, 1.03f));
        HeroProps.Tools(station, p);
        var prep = Group(root, "FreshPrep", new(-1.82f, Counter, 1.03f));
        HeroProps.Prep(prep, p);
        var ingredients = Group(root, "MargaritaIngredients", new(-1.45f, WorktopHeight, 1.2f));
        MixerBottle(ingredients, p, "LimeJuice", "LIME JUICE", new(-.18f, 0, 0), "cbd68a");
        MixerBottle(ingredients, p, "SimpleSyrup", "SIMPLE SYRUP", Vector3.Zero, "e5d7ad");
        HeroProps.Salt(prep, p, new(0, 0, -.29f));
        BuildIceWell(root, p);
        BuildWellRail(root, p);
        WashStation.Build(root, p);
        var glasses = Group(root, "CleanGlassStacks", new(2.05f, Counter, 1.03f));
        for (var stack = 0; stack < 2; stack++)
            for (var level = 0; level < 4; level++)
                BarGlassware.Tumbler(glasses, p, new(stack * .25f, level * .063f, 0), $"Stack{stack + 1}Glass{level + 1}");
        HeroProps.Towel(root, p, new(2.75f, Counter + .008f, 1.17f));
    }

    private static void BuildWellRail(Node root, ScenePalette p)
    {
        var rail = Group(root, "WellRail", new(.9f, .34f, 1.44f));
        RoundedBox(rail, "StainlessBase", new(1.27f, .025f, .22f), new(0, -.013f, 0), p.Steel, .006f);
        RoundedBox(rail, "ProtectiveFrontLip", new(1.27f, .13f, .02f), new(0, .05f, .11f), p.Steel, .005f);
        foreach (var x in new[] { -.625f, .625f })
            RoundedBox(rail, "RailEnd", new(.02f, .13f, .22f), new(x, .05f, 0), p.Steel, .005f);
        foreach (var x in new[] { -.45f, .45f })
        {
            RoundedBox(rail, "ApronMount", new(.03f, .45f, .03f), new(x, .225f, -.115f), p.Steel, .004f);
            RoundedBox(rail, "SupportBracket", new(.03f, .025f, .24f), new(x, -.025f, 0), p.Steel, .004f);
        }
        DetailedBottle.Spec[] bottles = [
            new("Well Tequila","well-tequila","TEQUILA","c7d5bd","e4e8d6",.10f,.29f),
            new("Well Vodka","smirnoff","VODKA","e0ebeb","e0ebeb",.095f,.31f),
            new("Well Gin","gordons","GIN","697d43","d4d9bc",.105f,.29f,4),
            new("Well Whiskey","jack-daniels","WHISKEY","cfc7a9","793e11",.11f,.30f,5,.9f,Amber:true),
            new("Well Rum","bacardi","RUM","c7d3b6","e0e6cf",.10f,.31f),
            new("Triple Sec","triple-sec","LIQUEUR","a98952","e5dfc1",.10f,.29f)];
        for (var i = 0; i < bottles.Length; i++)
            DetailedBottle.Build(rail, p, bottles[i], new(-.50f + i * .2f, 0, -.015f), pourSpout: true);
    }

    private static void MixerBottle(Node parent, ScenePalette p, string name, string label, Vector3 at, string color)
    {
        var bottle = Group(parent, name, at);
        Vessel(bottle, "SqueezeBottle", [new(0, 0), new(.036f, 0), new(.041f, .012f), new(.041f, .16f), new(.025f, .18f), new(.018f, .19f), new(0, .19f)], Vector3.Zero, ScenePalette.Matte(color, .42f));
        Cylinder(bottle, "ScrewCap", .022f, .022f, .018f, new(0, .195f, 0), p.Ink);
        Cylinder(bottle, "Nozzle", .008f, .003f, .046f, new(0, .225f, 0), p.Paper);
        Text(bottle, "Label", label, new(0, .09f, .0415f), .00038f, new("253b31"), 28);
    }

    private static void BuildBench(Node root, ScenePalette p)
    {
        var bench = Group(root, "BartenderBench");
        // Separate sections leave genuine openings into the sink and ice basin.
        RoundedBox(bench, "LeftEndWorktop", new(.67f, .045f, .78f), new(-3.565f, WorktopHeight - .0225f, .95f), p.Steel);
        RoundedBox(bench, "LeftWorktop", new(2.57f, .045f, .78f), new(-.885f, WorktopHeight - .0225f, .95f), p.Steel);
        foreach (var z in new[] { .61f, 1.29f })
            RoundedBox(bench, "SinkBridge", new(1.06f, .045f, .10f), new(-2.7f, WorktopHeight - .0225f, z), p.Steel);
        RoundedBox(bench, "RightWorktop", new(1.7f, .045f, .78f), new(2.25f, WorktopHeight - .0225f, .95f), p.Steel);
        RoundedBox(bench, "IceWellFarBridge", new(1, .045f, .1f), new(.9f, WorktopHeight - .0225f, .61f), p.Steel);
        RoundedBox(bench, "IceWellNearBridge", new(1, .045f, .1f), new(.9f, WorktopHeight - .0225f, 1.29f), p.Steel);
        RoundedBox(bench, "RearSplash", new(7.8f, .1f, .025f), new(0, .95f, .57f), p.Steel);
        // Leave the undercounter appliance's controls and door clear of the apron.
        RoundedBox(bench, "FrontApron", new(6.32f, .16f, .04f), new(-.06f, .79f, 1.32f), p.BasinSteel);
        foreach (var x in new[] { -2.10f, -.1f, 1.8f, 3.0f })
        {
            Cylinder(bench, "AdjustableLeg", .024f, .024f, .77f, new(x, .43f, 1.18f), p.Steel);
            Cylinder(bench, "RubberFoot", .029f, .029f, .055f, new(x, .055f, 1.18f), p.Rubber);
        }
        var shelf = ScenePalette.Matte("454e4c", .9f);
        RoundedBox(bench, "UnderBenchShelf", new(2.1f, .025f, .5f), new(-1.05f, .23f, .99f), shelf);
        RoundedBox(bench, "RightUnderBenchShelf", new(2.8f, .025f, .5f), new(1.7f, .23f, .99f), shelf);
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
