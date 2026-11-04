// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

/// <summary>4K CC0 scanned surfaces, with original procedural metal and rubber microstructure.</summary>
public sealed class ScenePalette
{
    private static Texture2D? _brushedRoughness;
    public StandardMaterial3D Plaster { get; } = CleanPlaster();
    public StandardMaterial3D Ceiling { get; } = Matte("d9d7cf");
    public StandardMaterial3D Teal { get; } = Pbr("brown_leather", "56716a", 1.8f, .75f);
    public StandardMaterial3D DarkTeal { get; } = Matte("203b36", .38f);
    public StandardMaterial3D Ink { get; } = Matte("222724");
    public StandardMaterial3D Oak { get; } = Pbr("white_oak_veneer", "d2b294", .65f, .58f);
    public StandardMaterial3D Walnut { get; } = Pbr("white_oak_veneer", "77614a", .65f, .58f);
    public StandardMaterial3D FloorWood { get; } = Pbr("wood_floor", "b7a28c", .42f, .7f);
    public StandardMaterial3D Quartz { get; } = CounterStone();
    public StandardMaterial3D Floor { get; } = Matte("9b958a", .8f);
    public StandardMaterial3D Brass { get; } = BrushedMetal("b6a17b", .31f);
    public StandardMaterial3D Steel { get; } = BrushedMetal("b9bfc0", .23f);
    public StandardMaterial3D ToolSteel { get; } = MachinedSteel();
    public StandardMaterial3D BasinSteel { get; } = BrushedMetal("8e999b", .34f);
    public StandardMaterial3D Rubber { get; } = Matte("202725", .94f);
    public StandardMaterial3D Paper { get; } = Matte("e2ddce");
    public StandardMaterial3D Cloth { get; } = WovenCloth();
    public StandardMaterial3D Glass { get; } = GlassMaterial("eaf4ef", .14f);
    public StandardMaterial3D Leaf { get; } = Matte("425334");
    public StandardMaterial3D LimeSkin { get; } = Citrus("77923d");

    public static StandardMaterial3D Matte(string hex, float roughness = .76f) => new()
    {
        AlbedoColor = new Color(hex),
        Roughness = roughness
    };

    public static StandardMaterial3D Metal(string hex, float roughness) => new()
    {
        AlbedoColor = new Color(hex),
        Metallic = .95f,
        Roughness = roughness
    };

    public static StandardMaterial3D GlassMaterial(string hex, float opacity) => new()
    {
        AlbedoColor = new Color(new Color(hex), opacity),
        Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass,
        Roughness = .07f,
        Metallic = 0,
        MetallicSpecular = .7f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled
    };

    public static StandardMaterial3D Glow(string hex, float energy = 1f) => new()
    {
        AlbedoColor = new Color(hex),
        EmissionEnabled = true,
        Emission = new Color(hex),
        EmissionEnergyMultiplier = energy
    };

    private static StandardMaterial3D Pbr(string asset, string tint, float scale, float roughness)
    {
        var path = $"res://Assets/Materials/{asset}/";
        return new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>(path + "albedo.jpg"),
            AlbedoColor = new Color(tint),
            NormalEnabled = true,
            NormalTexture = GD.Load<Texture2D>(path + "normal.jpg"),
            NormalScale = .4f,
            RoughnessTexture = GD.Load<Texture2D>(path + "roughness.jpg"),
            Roughness = roughness,
            RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red,
            Uv1Triplanar = true,
            Uv1WorldTriplanar = true,
            Uv1TriplanarSharpness = 8,
            Uv1Scale = Vector3.One * scale,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic
        };
    }

    private static StandardMaterial3D CleanPlaster()
    {
        var material = Pbr("white_plaster_02", "dcd8ce", .65f, .95f);
        // Fresh interior paint keeps the scan's surface relief without its outdoor discoloration.
        material.AlbedoTexture = null;
        material.NormalScale = .13f;
        return material;
    }

    private static StandardMaterial3D CounterStone()
    {
        var material = Pbr("marble_01", "e3e2dd", .35f, .5f);
        // Use the unbroken part of the stone scan, excluding its original paving joints.
        material.Uv1Triplanar = false;
        material.Uv1WorldTriplanar = false;
        material.Uv1Scale = new(.36f, .36f, 1);
        material.Uv1Offset = new(.06f, .06f, 0);
        return material;
    }

    private static StandardMaterial3D BrushedMetal(string color, float roughness)
    {
        var material = Metal(color, roughness);
        material.RoughnessTexture = _brushedRoughness ??= CreateBrushedRoughness();
        material.RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red;
        material.Uv1Triplanar = true;
        material.Uv1WorldTriplanar = true;
        material.Uv1Scale = new(3, 3, 3);
        material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        return material;
    }

    private static Texture2D CreateBrushedRoughness()
    {
        var noise = new FastNoiseLite { Seed = 177, Frequency = .18f, FractalOctaves = 2 };
        var image = Image.CreateEmpty(512, 512, false, Image.Format.Rgb8);
        for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
            {
                var value = .8f + noise.GetNoise2D(x * .025f, y * 4) * .12f;
                image.SetPixel(x, y, new Color(value, value, value));
            }
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    private static StandardMaterial3D Citrus(string color)
    {
        var material = Matte(color, .47f);
        material.NormalEnabled = true;
        material.NormalTexture = new NoiseTexture2D
        {
            Width = 256,
            Height = 256,
            AsNormalMap = true,
            BumpStrength = 1.4f,
            Noise = new FastNoiseLite { Seed = 91, Frequency = .3f }
        };
        material.NormalScale = .3f;
        return material;
    }

    private static StandardMaterial3D MachinedSteel()
    {
        var material = BrushedMetal("c1c6c7", .22f);
        var grain = material.RoughnessTexture.GetImage();
        grain.BumpMapToNormalMap(.8f);
        grain.GenerateMipmaps();
        material.NormalEnabled = true;
        material.NormalTexture = ImageTexture.CreateFromImage(grain);
        material.NormalScale = .22f;
        material.AnisotropyEnabled = true;
        material.Anisotropy = .35f;
        return material;
    }

    private static StandardMaterial3D WovenCloth()
    {
        var image = Image.CreateEmpty(512, 512, false, Image.Format.Rgb8);
        for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
            {
                var height = .5f + .13f * Mathf.Sin(x * Mathf.Tau / 8) * Mathf.Sin(y * Mathf.Tau / 8);
                image.SetPixel(x, y, new Color(height, height, height));
            }
        image.BumpMapToNormalMap(.35f);
        image.GenerateMipmaps();
        var material = Matte("d5cebb", .93f);
        material.NormalEnabled = true;
        material.NormalTexture = ImageTexture.CreateFromImage(image);
        material.NormalScale = .35f;
        material.Uv1Scale = new(3, 2, 1);
        material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        return material;
    }
}
