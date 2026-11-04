// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Fitted CC0 anatomical skin, hands and clothing in one baked seated reference pose.</summary>
public partial class SeatedCustomer : Node3D
{
    private Node3D _chest = null!;
    private double _elapsed;

    public void Build(ScenePalette p)
    {
        Name = "SeatedCustomer";
        Position = new(.95f, 0, -1.02f);
        _chest = Group(this, "UpperBody");
        // All meshes use the same meter-scale coordinates and skinning transforms.
        // Neck/chest geometry continues inside the fitted shirt collar.
        var head = Group(_chest, "AnatomicalHead");
        var skin = ScenePalette.Matte("ffffff", .58f);
        skin.AlbedoTexture = GD.Load<Texture2D>("res://Assets/Customer/skin.png");
        skin.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        skin.SubsurfScatterEnabled = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
        skin.SubsurfScatterStrength = .07f;
        skin.SubsurfScatterSkinMode = true;
        skin.NormalEnabled = true;
        skin.NormalTexture = new NoiseTexture2D
        {
            Width = 512,
            Height = 512,
            AsNormalMap = true,
            BumpStrength = .13f,
            Noise = new FastNoiseLite { Seed = 229, Frequency = .42f, FractalOctaves = 2 }
        };
        skin.NormalScale = .12f;
        Mesh(head, "FaceAndNeck", GD.Load<Godot.Mesh>("res://Assets/Customer/head.obj"), Vector3.Zero, skin);
        Mesh(_chest, "AnatomicalHands", GD.Load<Godot.Mesh>("res://Assets/Customer/hands.obj"), Vector3.Zero, skin);

        var clothing = ScenePalette.Matte("ffffff", .88f);
        clothing.AlbedoTexture = GD.Load<Texture2D>("res://Assets/Customer/clothing-albedo.png");
        clothing.NormalEnabled = true;
        clothing.NormalTexture = GD.Load<Texture2D>("res://Assets/Customer/clothing-normal.png");
        clothing.NormalScale = .7f;
        clothing.AOEnabled = true;
        clothing.AOTexture = GD.Load<Texture2D>("res://Assets/Customer/clothing-ao.png");
        clothing.AOLightAffect = .35f;
        clothing.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        clothing.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(_chest, "FittedClothing", GD.Load<Godot.Mesh>("res://Assets/Customer/clothing.obj"), Vector3.Zero, clothing);

        var hair = ScenePalette.Matte("ffffff", .75f);
        hair.AlbedoTexture = GD.Load<Texture2D>("res://Assets/Customer/hair.png");
        hair.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
        hair.AlphaScissorThreshold = .4f;
        hair.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(head, "Hair", GD.Load<Godot.Mesh>("res://Assets/Customer/hair.obj"), Vector3.Zero, hair);
        foreach (var feature in new[] { "brows", "lashes" })
        {
            var strands = (StandardMaterial3D)hair.Duplicate();
            strands.AlbedoTexture = GD.Load<Texture2D>($"res://Assets/Customer/{feature}.png");
            Mesh(head, feature, GD.Load<Godot.Mesh>($"res://Assets/Customer/{feature}.obj"), Vector3.Zero, strands);
        }
        var eyes = ScenePalette.Matte("ffffff", .17f);
        eyes.AlbedoTexture = GD.Load<Texture2D>("res://Assets/Customer/eyes.png");
        Mesh(head, "Eyes", GD.Load<Godot.Mesh>("res://Assets/Customer/eyes.obj"), Vector3.Zero, eyes);

        var leather = ScenePalette.Matte("272722", .4f);
        foreach (var side in new[] { -1f, 1f })
        {
            var shoe = Group(this, "LeatherShoe", new(side * .16f, .35f, .43f));
            RoundedBox(shoe, "RubberOutsole", new(.115f, .023f, .255f), new(0, .012f, 0), p.Ink, .011f);
            Sphere(shoe, "LeatherUpper", new(.112f, .092f, .25f), new(0, .048f, 0), leather);
            RoundedBox(shoe, "StackedHeel", new(.10f, .034f, .073f), new(0, .017f, -.074f), p.Ink, .008f);
            for (var i = 0; i < 4; i++)
                Rod(shoe, "Lace", new(-.027f, .088f - i * .004f, -.046f + i * .015f),
                    new(.027f, .088f - i * .004f, -.046f + i * .015f), .0014f, p.Ink);
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        _chest.Position = new(0, Mathf.Sin((float)_elapsed * 1.4f) * .0012f, 0);
    }
}
