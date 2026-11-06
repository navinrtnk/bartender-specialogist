// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public static class WashStation
{
    public static void Build(Node root, ScenePalette p)
    {
        var sink = Group(root, "WashSink", new(-2.7f, MargaritaStation.WorktopHeight, .95f));
        RoundedBox(sink, "BasinFloor", new(.94f, .024f, .56f), new(0, -.27f, 0), p.BasinSteel, .012f);
        foreach (var x in new[] { -.48f, .48f })
        {
            RoundedBox(sink, "BasinSide", new(.018f, .26f, .58f), new(x, -.13f, 0), p.BasinSteel, .007f);
            RoundedBox(sink, "RolledSinkRim", new(.035f, .015f, .64f), new(x, .006f, 0), p.ToolSteel, .006f);
        }
        foreach (var z in new[] { -.29f, .29f })
        {
            RoundedBox(sink, "BasinEnd", new(.96f, .26f, .018f), new(0, -.13f, z), p.BasinSteel, .007f);
            RoundedBox(sink, "RolledSinkRim", new(1, .015f, .035f), new(0, .006f, z), p.ToolSteel, .006f);
        }
        Cylinder(sink, "Drain", .027f, .027f, .004f, new(0, -.255f, 0), p.Ink, 40);
        Ring(sink, "DrainFlange", .030f, .003f, new(0, -.252f, 0), p.ToolSteel);
        for (var i = -2; i <= 2; i++)
            Rod(sink, "DrainStrainer", new(i * .008f, -.250f, -.02f), new(i * .008f, -.250f, .02f), .0012f, p.ToolSteel);
        Cylinder(sink, "FaucetMount", .030f, .030f, .016f, new(0, .015f, -.31f), p.ToolSteel);
        var path = new Vector3[49];
        for (var i = 0; i < path.Length; i++)
        {
            var t = i / (float)(path.Length - 1);
            path[i] = new(0, .03f + .33f * Mathf.Sin(t * Mathf.Pi * .82f), -.31f + t * .24f);
        }
        Tube(sink, "GooseneckFaucet", path, .013f, p.ToolSteel, 16);
        Cylinder(sink, "Aerator", .016f, .016f, .021f, path[^1] - new Vector3(0, .009f, 0), p.ToolSteel, 32);
        foreach (var side in new[] { -1f, 1f })
        {
            Cylinder(sink, "ValveBase", .027f, .027f, .022f, new(side * .17f, .015f, -.31f), p.ToolSteel);
            Rod(sink, "LeverValve", new(side * .17f, .035f, -.31f), new(side * .25f, .052f, -.31f), .007f, p.ToolSteel);
            Cylinder(sink, "ValveMarker", .006f, .006f, .001f, new(side * .17f, .028f, -.31f), ScenePalette.Matte(side < 0 ? "aa4941" : "4c7b9a"), 24);
        }

        var washer = Group(root, "Dishwasher", new(-3.55f, 0, 1.005f));
        foreach (var x in new[] { -.30f, .30f })
            RoundedBox(washer, "CabinetSide", new(.025f, .79f, .68f), new(x, .46f, 0), p.BasinSteel, .008f);
        RoundedBox(washer, "CabinetBack", new(.59f, .79f, .025f), new(0, .46f, -.33f), p.BasinSteel, .008f);
        foreach (var y in new[] { .08f, .845f }) RoundedBox(washer, "CabinetPanel", new(.59f, .025f, .68f), new(0, y, 0), p.BasinSteel, .008f);
        for (var i = 0; i < 10; i++)
            Rod(washer, "GlassRack", new(-.25f + i * .055f, .42f, -.25f), new(-.25f + i * .055f, .42f, .25f), .002f, p.ToolSteel);
        var door = Group(washer, "Door", new(0, .13f, .355f));
        RoundedBox(door, "BrushedDoor", new(.585f, .60f, .032f), new(0, .30f, 0), p.Steel, .010f);
        RoundedBox(door, "Gasket", new(.598f, .625f, .012f), new(0, .30f, -.015f), p.Rubber, .008f);
        RoundedBox(door, "ControlPanel", new(.585f, .10f, .025f), new(0, .66f, -.001f), p.Ink, .006f);
        RoundedBox(door, "RecessedHandle", new(.36f, .041f, .025f), new(0, .578f, .029f), p.Ink, .009f);
        Rod(door, "DoorPull", new(-.17f, .578f, .058f), new(.17f, .578f, .058f), .009f, p.ToolSteel);
        Text(door, "Status", "READY", new(-.13f, .66f, .015f), .00028f, new("a6c4a1"), 28);
        foreach (var x in new[] { .12f, .20f })
        {
            var button = Cylinder(door, "ControlButton", .013f, .013f, .004f, new(x, .66f, .016f), p.ToolSteel, 32);
            button.RotationDegrees = new(90, 0, 0);
        }
        for (var i = 0; i < 9; i++)
            RoundedBox(washer, "ToeVent", new(.037f, .005f, .003f), new(-.23f + i * .057f, .12f, .344f), p.Ink, .001f);
        foreach (var x in new[] { -.24f, .24f })
            foreach (var z in new[] { -.24f, .24f })
                Cylinder(washer, "AdjustableFoot", .019f, .019f, .065f, new(x, .04f, z), p.Rubber, 24);
    }
}
