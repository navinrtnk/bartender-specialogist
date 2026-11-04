// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public static class BackBar
{
    // Original bottle and label designs, with text-only references to real brands.
    private static readonly DetailedBottle.Spec[] TopShelf =
    [
        new("PATRÓN", "patron", "TEQUILA", "93b39b", "e2e7d5", .27f, .46f, 4, .85f, Cork:true),
        new("GREY GOOSE", "grey-goose", "VODKA", "d4e0e3", "e0ebeb", .15f, .62f),
        new("TANQUERAY", "tanqueray", "GIN", "376549", "c9d1a2", .24f, .52f),
        new("WOODFORD RESERVE", "woodford", "BOURBON", "d4d2b9", "78380d", .25f, .47f, 3.6f, .7f, true, true),
        new("ZACAPA 23", "zacapa", "RUM", "d1c4a3", "60280b", .20f, .55f, Amber:true)
    ];

    private static readonly DetailedBottle.Spec[] BottomShelf =
    [
        new("JOSE CUERVO", "cuervo", "TEQUILA", "d9ce94", "c8a34b", .17f, .54f, Amber:true),
        new("SMIRNOFF", "smirnoff", "VODKA", "e0ebeb", "e0ebeb", .15f, .56f),
        new("GORDON’S", "gordons", "GIN", "697d43", "d4d9bc", .21f, .53f, 4),
        new("JACK DANIEL’S", "jack-daniels", "WHISKEY", "cfc7a9", "793e11", .22f, .55f, 5, .9f, Amber:true),
        new("BACARDÍ", "bacardi", "RUM", "c7d3b6", "e0e6cf", .17f, .56f)
    ];

    public static void Build(Node3D root, ScenePalette p)
    {
        var back = Group(root, "BackBar");
        RoundedBox(back, "BackBarCabinet", new(7.7f, .87f, .5f), new(0, .45f, 4.16f), p.Walnut, .018f);
        RoundedBox(back, "BackBarWorktop", new(7.82f, .06f, .67f), new(0, .91f, 4.12f), p.Quartz, .01f);
        for (var i = 0; i < 6; i++)
        {
            var x = -3.2f + i * 1.28f;
            RoundedBox(back, "CabinetDoor", new(1.22f, .71f, .035f), new(x, .45f, 3.89f), p.Oak, .01f);
            RoundedBox(back, "RecessedDoorPanel", new(1.1f, .59f, .014f), new(x, .45f, 3.867f), p.Walnut, .006f);
            Rod(back, "CabinetPull", new(x - .1f, .72f, 3.82f), new(x + .1f, .72f, 3.82f), .008f, p.Brass);
            foreach (var side in new[] { -.1f, .1f })
                Rod(back, "PullStandoff", new(x + side, .72f, 3.86f), new(x + side, .72f, 3.82f), .008f, p.Brass);
        }
        RoundedBox(back, "DisplaySurround", new(4.0f, 2.13f, .16f), new(0, 2.08f, 4.44f), p.Walnut, .02f);
        var grout = ScenePalette.Matte("243730");
        Box(back, "DisplayBacking", new(3.78f, 1.96f, .13f), new(0, 2.08f, 4.32f), grout);
        var tile = ScenePalette.Matte("29483f", .34f);
        tile.ClearcoatEnabled = true; tile.Clearcoat = .3f; tile.ClearcoatRoughness = .3f;
        for (var row = 0; row < 6; row++)
            for (var col = 0; col < 12; col++)
                RoundedBox(back, "GlazedTile", new(.304f, .306f, .016f), new(-1.705f + col * .31f, 1.29f + row * .316f, 4.242f), tile, .007f);
        // Cabinet joinery and continuous warm shelf lighting add depth without filling the bar with extra bottles.
        foreach (var x in new[] { -1.94f, 1.94f })
        {
            RoundedBox(back, "DisplayStile", new(.085f, 2.04f, .35f), new(x, 2.08f, 4.17f), p.Oak, .008f);
            Box(back, "BrassInlay", new(.008f, 1.91f, .012f), new(x, 2.08f, 3.988f), p.Brass);
        }
        BuildShelf(back, p, "TopShelf", "TOP SHELF", 2.23f, TopShelf);
        BuildShelf(back, p, "BottomShelf", "BOTTOM SHELF", 1.25f, BottomShelf);
        foreach (var x in new[] { -1.3f, 0, 1.3f })
            foreach (var y in new[] { 1.93f, 2.96f })
                back.AddChild(new OmniLight3D
                {
                    Name = "WarmBottleLight",
                    Position = new(x, y, 3.7f),
                    LightColor = new("ffe3af"),
                    LightEnergy = .1f,
                    LightSpecular = 0,
                    OmniRange = 1.6f,
                    OmniAttenuation = 1.5f,
                    ShadowEnabled = false
                });
        Text(back, "BackBarTitle", "BARTENDER SPECIALOGIST", new(0, 3.25f, 4.49f), .0017f, new("315d55"), 52, true);
    }

    private static void BuildShelf(Node root, ScenePalette p, string name, string title, float y, DetailedBottle.Spec[] bottles)
    {
        var shelf = Group(root, name);
        RoundedBox(shelf, "OakShelf", new(3.88f, .07f, .56f), new(0, y, 4.06f), p.Oak, .008f);
        Box(shelf, "BrassShelfLip", new(3.88f, .017f, .012f), new(0, y + .014f, 3.774f), p.Brass);
        Box(shelf, "ShelfLightDiffuser", new(3.65f, .012f, .026f), new(0, y - .044f, 3.88f), ScenePalette.Glow("ffe3b1", .8f));
        for (var i = 0; i < bottles.Length; i++)
        {
            var x = -1.4f + i * .7f;
            DetailedBottle.Build(shelf, p, bottles[i], new(x, y + .04f, 4.04f), true);
            RoundedBox(shelf, "SpiritPlaque", new(.38f, .057f, .004f), new(x, y - .054f, 3.78f), p.Brass, .002f);
            Text(shelf, "SpiritCaption", bottles[i].Spirit, new(x, y - .053f, 3.774f), .0005f, new("26382f"), 36, true);
        }
        Text(shelf, "ShelfCaption", title, new(0, y + .75f, 4.22f), .0008f, new("d9c298"), 32, true);
    }
}
