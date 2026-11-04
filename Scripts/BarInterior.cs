// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public static class BarInterior
{
    public static void Build(Node3D root, ScenePalette p)
    {
        var room = Group(root, "Architecture");
        Box(room, "Foundation", new(12, .16f, 14), new(0, -.1f, -2.2f), p.Floor);
        Box(room, "PhotographicOakFloor", new(11.9f, .035f, 13.8f), new(0, 0, -2.2f), p.FloorWood);
        Box(room, "LeftWall", new(.2f, 3.6f, 14), new(-6, 1.8f, -2.2f), p.Plaster);
        Box(room, "RightWallBase", new(.2f, .9f, 14), new(6, .45f, -2.2f), p.Plaster);
        Box(room, "RightWallHeader", new(.2f, .5f, 14), new(6, 3.35f, -2.2f), p.Plaster);
        Box(room, "RightWallRear", new(.2f, 2.2f, 4.1f), new(6, 2, 2.65f), p.Plaster);
        Box(room, "RightWallFront", new(.2f, 2.2f, 1.1f), new(6, 2, -8.7f), p.Plaster);
        foreach (var z in new[] { -5.35f, -2.25f })
            Box(room, "WindowPier", new(.2f, 2.2f, .62f), new(6, 2, z), p.Plaster);
        Box(room, "FrontWall", new(12, 3.6f, .2f), new(0, 1.8f, -9.15f), p.Plaster);
        Box(room, "BackWall", new(12, 3.6f, .2f), new(0, 1.8f, 4.65f), p.Plaster);
        Box(room, "Ceiling", new(12, .15f, 14), new(0, 3.65f, -2.2f), p.Ceiling);
        foreach (var x in new[] { -5.86f, 5.86f })
            Box(room, "WallSkirting", new(.06f, .14f, 13.8f), new(x, .08f, -2.2f), p.Walnut);

        // Recessed daylight panels, framed as street-facing windows.
        var sky = ScenePalette.Glow("dceaf0", .3f);
        var street = ScenePalette.Matte("a6b7a5");
        for (var i = 0; i < 3; i++)
        {
            var z = -6.9f + i * 3.1f;
            foreach (var y in new[] { .94f, 3.06f })
                Box(room, "WindowFrameRail", new(.12f, .09f, 2.48f), new(5.85f, y, z), p.Walnut);
            foreach (var edge in new[] { -1.2f, 1.2f })
                Box(room, "WindowFrameStile", new(.12f, 2.2f, .08f), new(5.85f, 2, z + edge), p.Walnut);
            Box(room, "DaylightGlass", new(.13f, 2.02f, 2.29f), new(5.77f, 2, z), sky)
                .CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            Box(room, "DistantStreet", new(.14f, .38f, 2.27f), new(5.755f, 1.2f, z), street);
            Box(room, "WindowMullion", new(.17f, 2.08f, .045f), new(5.68f, 2, z), p.Walnut);
            Box(room, "WindowSill", new(.3f, .07f, 2.56f), new(5.66f, .89f, z), p.Quartz);
        }

        Box(room, "EntryDoorFrame", new(1.5f, 2.65f, .14f), new(0, 1.325f, -9), p.Walnut);
        Box(room, "EntryDoor", new(1.33f, 2.5f, .16f), new(0, 1.27f, -8.92f), p.Teal);
        Box(room, "EntryDoorGlass", new(1.08f, 1.56f, .02f), new(0, 1.7f, -8.82f), sky);
        Rod(room, "DoorHandle", new(.47f, .93f, -8.76f), new(.47f, 1.29f, -8.76f), .018f, p.Brass);
        Text(room, "FrontSign", "THE NEIGHBORHOOD", new(0, 3.02f, -8.95f), .0045f, new Color("315d55"));
        Text(room, "DoorSign", "COME ON IN", new(0, 1.68f, -8.78f), .0025f, new Color("315d55"), 40);

        for (var i = 0; i < 3; i++)
        {
            var z = -6.9f + i * 2.5f;
            Box(room, "ArtFrame", new(.1f, 1.12f, .82f), new(-5.83f, 2.1f, z), p.Walnut);
            Box(room, "ArtPaper", new(.12f, .99f, .69f), new(-5.81f, 2.1f, z), p.Paper);
            var art = Cylinder(room, "AbstractArt", .26f, .26f, .015f,
                new(-5.735f, 2.18f, z), i == 1 ? p.Teal : p.Brass);
            art.RotationDegrees = new(0, 0, 90);
            Box(room, "AbstractLine", new(.13f, .03f, .44f), new(-5.72f, 1.85f, z), p.Teal);
        }

        var bar = Group(root, "ServiceBar");
        Box(bar, "Cabinet", new(8.1f, 1.02f, .78f), new(0, .53f, -.02f), p.Walnut);
        for (var i = 0; i < 81; i++)
            Box(bar, "OakFluting", new(.061f, .93f, .065f), new(-4 + i * .1f, .54f, -.443f), p.Oak);
        Box(bar, "BrassToeKick", new(8.13f, .10f, .06f), new(0, .09f, -.45f), p.Brass);
        RoundedBox(bar, "QuartzCountertop", new(8.55f, .095f, 1.12f), new(0, 1.09f, -.02f), p.Quartz, .016f);
        Box(bar, "CounterEdge", new(8.55f, .012f, .016f), new(0, 1.045f, -.58f), p.Brass);
        Rod(bar, "CustomerFootRail", new(-3.96f, .27f, -.72f), new(3.96f, .27f, -.72f), .025f, p.Brass);
        for (var i = 0; i < 4; i++)
        {
            var x = -2.85f + i * 1.9f;
            Stool(bar, p, new(x, 0, -1.02f), $"BarStool{i + 1}");
            Rod(bar, "FootRailSupport", new(x, .24f, -.44f), new(x, .24f, -.72f), .017f, p.Brass);
            Pendant(root, p, new(x, 2.6f, -.05f));
        }

        var seating = Group(root, "FourTables");
        foreach (var x in new[] { -3.55f, 3.55f })
            foreach (var z in new[] { -3.7f, -6.65f })
                Table(seating, p, new(x, 0, z));
        Plant(root, p, new(-5.2f, 0, -8.3f));
        Plant(root, p, new(5.15f, 0, 2.9f));
    }

    private static void Stool(Node root, ScenePalette p, Vector3 position, string name)
    {
        var stool = Group(root, name, position);
        Cylinder(stool, "Base", .24f, .24f, .045f, new(0, .045f, 0), p.Ink);
        Cylinder(stool, "Stem", .033f, .033f, .7f, new(0, .4f, 0), p.Ink);
        Ring(stool, "Footrest", .18f, .012f, new(0, .32f, 0), p.Brass);
        Cylinder(stool, "SeatBase", .245f, .245f, .055f, new(0, .76f, 0), p.Walnut);
        Cylinder(stool, "SeatCushion", .245f, .235f, .07f, new(0, .81f, 0), p.Teal);
        Ring(stool, "UpholsteryPiping", .237f, .0015f, new(0, .837f, 0), p.DarkTeal);
        // Low backs leave a clear view from the bartender's eye height.
        RoundedBox(stool, "LowBack", new(.43f, .25f, .055f), new(0, .96f, -.21f), p.Teal, .022f);
    }

    private static void Table(Node root, ScenePalette p, Vector3 position)
    {
        var table = Group(root, "NeighborhoodTable", position);
        Cylinder(table, "PedestalBase", .31f, .27f, .055f, new(0, .05f, 0), p.Ink);
        Cylinder(table, "Pedestal", .045f, .045f, .69f, new(0, .4f, 0), p.Ink);
        Vessel(table, "RoundOakTop", [new(0, 0), new(.625f, 0), new(.64f, .008f), new(.64f, .043f), new(.63f, .055f), new(0, .055f)], new(0, .7425f, 0), p.Oak);
        Cylinder(table, "BudVase", .048f, .035f, .12f, new(0, .86f, 0), p.Paper);
        Rod(table, "VaseStem", new(0, .89f, 0), new(.02f, 1.09f, .01f), .006f, p.Leaf);
        Sphere(table, "VaseLeaf", new(.11f, .035f, .045f), new(.03f, 1.02f, .01f), p.Leaf);
        foreach (var side in new[] { -1f, 1f })
        {
            var chair = Group(table, "Chair", new(0, 0, side * .92f));
            RoundedBox(chair, "Seat", new(.48f, .065f, .46f), new(0, .46f, 0), p.Teal, .025f);
            RoundedBox(chair, "Back", new(.48f, .42f, .065f), new(0, .75f, side * .2f), p.Teal, .025f);
            var seam = new Vector3[65];
            for (var i = 0; i < seam.Length; i++)
            {
                var a = i * Mathf.Tau / (seam.Length - 1);
                var x = Mathf.Cos(a); var z = Mathf.Sin(a);
                seam[i] = new(Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), .4f) * .219f, .482f,
                    Mathf.Sign(z) * Mathf.Pow(Mathf.Abs(z), .4f) * .209f);
            }
            Tube(chair, "SeatPiping", seam, .0015f, p.DarkTeal, 6);
            foreach (var lx in new[] { -.18f, .18f })
                foreach (var lz in new[] { -.17f, .17f })
                    Rod(chair, "Leg", new(lx * 1.15f, .03f, lz * 1.15f), new(lx, .45f, lz), .018f, p.Oak);
        }
    }

    private static void Pendant(Node root, ScenePalette p, Vector3 position)
    {
        var lamp = Group(root, "Pendant", position);
        Cylinder(lamp, "BraidedCord", .004f, .004f, 1.05f, new(0, .525f, 0), p.Ink);
        Cylinder(lamp, "CeilingRose", .052f, .052f, .022f, new(0, 1.025f, 0), p.Brass);
        var enamel = ScenePalette.Matte("344d45", .3f);
        enamel.ClearcoatEnabled = true; enamel.Clearcoat = .6f; enamel.ClearcoatRoughness = .2f;
        Vessel(lamp, "SpunEnamelShade", [new(.225f,-.155f),new(.23f,-.148f),new(.226f,-.135f),
            new(.18f,-.09f),new(.12f,-.025f),new(.07f,.025f),new(.066f,.045f),
            new(.06f,.045f),new(.066f,.018f),new(.117f,-.03f),new(.175f,-.095f),new(.22f,-.14f)], Vector3.Zero, enamel);
        Ring(lamp, "RolledBrassShadeLip", .225f, .002f, new(0, -.15f, 0), p.Brass);
        Cylinder(lamp, "Diffuser", .205f, .205f, .008f, new(0, -.157f, 0), ScenePalette.Glow("fff1cf", .7f));
    }

    private static void Plant(Node root, ScenePalette p, Vector3 position)
    {
        var plant = Group(root, "PottedPlant", position);
        Cylinder(plant, "Pot", .23f, .3f, .44f, new(0, .24f, 0), p.Paper);
        Cylinder(plant, "Soil", .279f, .279f, .015f, new(0, .466f, 0), p.Walnut);
        for (var i = 0; i < 9; i++)
        {
            var angle = i * 2.4f;
            var end = new Vector3(Mathf.Cos(angle) * .26f, .8f + i * .08f, Mathf.Sin(angle) * .26f);
            Rod(plant, "Stem", new(0, .46f, 0), end, .009f, p.Leaf);
            var leaf = Sphere(plant, "Leaf", new(.22f, .48f, .07f), end, p.Leaf);
            leaf.RotationDegrees = new(0, Mathf.RadToDeg(angle), -25);
        }
    }
}
