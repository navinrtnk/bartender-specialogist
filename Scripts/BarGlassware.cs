// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Physical glass walls, stacking ledges, rims and complete table drinks.</summary>
public static class BarGlassware
{
    public static Node3D Tumbler(Node root, ScenePalette p, Vector3 position, string name = "Tumbler")
    {
        var glass = Group(root, name, position);
        Vessel(glass, "GlassWall", [new(0,0),new(.030f,0),new(.033f,.005f),new(.035f,.017f),
            new(.043f,.097f),new(.044f,.104f),new(.043f,.106f),new(.041f,.106f),
            new(.040f,.099f),new(.031f,.015f),new(0,.013f)], Vector3.Zero, p.Glass);
        Ring(glass, "PolishedRim", .0425f, .0011f, new(0, .105f, 0), p.Glass);
        Ring(glass, "StackingLedge", .0375f, .0012f, new(0, .047f, 0), p.Glass);
        Ring(glass, "HeavyBase", .031f, .002f, new(0, .007f, 0), p.Glass);
        for (var i = 0; i < 12; i++)
        {
            var a = i * Mathf.Tau / 12;
            Tube(glass, "MoldedFlute", [new(Mathf.Cos(a)*.034f,.018f,Mathf.Sin(a)*.034f),
                new(Mathf.Cos(a)*.0395f,.075f,Mathf.Sin(a)*.0395f)], .0006f, p.Glass, 6);
        }
        return glass;
    }

    public static Node3D Cocktail(Node root, ScenePalette p, bool cosmo)
    {
        if (!cosmo)
        {
            var drink = Tumbler(root, p, Vector3.Zero, "OldFashioned");
            var amber = ScenePalette.GlassMaterial("a65516", .76f);
            Cylinder(drink, "Bourbon", .032f, .038f, .051f, new(0, .040f, 0), amber, 64);
            Ring(drink, "Meniscus", .038f, .0008f, new(0, .066f, 0), amber);
            var ice = ScenePalette.GlassMaterial("d1e6e7", .21f);
            var cube = RoundedBox(drink, "LargeIceCube", new(.046f, .041f, .046f), new(0, .061f, 0), ice, .008f);
            cube.RotationDegrees = new(9, 28, 4);
            var peel = ScenePalette.Matte("d88a32", .5f);
            Tube(drink, "OrangePeel", [new(-.026f, .074f, -.015f), new(-.012f, .079f, -.026f), new(.009f, .075f, -.026f), new(.023f, .066f, -.014f)], .0032f, peel, 8);
            return drink;
        }
        var coupe = Group(root, "Cosmopolitan");
        Vessel(coupe, "CrystalCoupe", [new(0,0),new(.037f,0),new(.040f,.003f),new(.038f,.006f),
            new(.007f,.009f),new(.0034f,.016f),new(.0034f,.092f),new(.012f,.097f),
            new(.029f,.109f),new(.045f,.134f),new(.055f,.155f),new(.054f,.158f),
            new(.0525f,.156f),new(.043f,.135f),new(.027f,.111f),new(.01f,.101f),new(0,.101f)], Vector3.Zero, p.Glass);
        Ring(coupe, "FinePolishedLip", .054f, .001f, new(0, .157f, 0), p.Glass);
        var pink = ScenePalette.GlassMaterial("ad294d", .80f);
        Vessel(coupe, "CranberryCocktail", [new(0,.102f),new(.012f,.103f),new(.028f,.113f),new(.042f,.136f),
            new(.047f,.145f),new(0,.145f)], Vector3.Zero, pink);
        Ring(coupe, "LiquidMeniscus", .047f, .0007f, new(0, .145f, 0), pink);
        var zest = ScenePalette.Matte("e9bb53", .45f);
        Tube(coupe, "LemonTwist", [new(.042f, .15f, 0), new(.050f, .162f, .003f), new(.058f, .163f, .012f), new(.059f, .15f, .018f)], .0018f, zest, 8);
        return coupe;
    }
}
