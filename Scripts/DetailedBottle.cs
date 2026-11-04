// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Reusable hollow bottle with a shaped body, liquid volume, meniscus, foil and curved label.</summary>
public static class DetailedBottle
{
    public sealed record Spec(string Name, string Label, string Spirit, string Glass, string Liquid,
        float Width, float Height, float Section = 2, float Depth = 1, bool Cork = false, bool Amber = false);

    public static Node3D Build(Node root, ScenePalette p, Spec spec, Vector3 position, bool faceBack = false, bool pourSpout = false)
    {
        var bottle = Group(root, spec.Name.Replace("\n", " "), position);
        if (faceBack) bottle.RotationDegrees = new(0, 180, 0);
        var radius = spec.Width / 2;
        var h = spec.Height;
        var neck = radius * .29f;
        var coloredGlass = spec.Label is "tanqueray" or "gordons";
        var glass = ScenePalette.GlassMaterial(spec.Glass, coloredGlass ? .24f : .085f);
        glass.Roughness = .105f;
        glass.CullMode = BaseMaterial3D.CullModeEnum.Back;
        Vector2[] profile =
        [
            new(0, .004f), new(radius * .78f, .004f), new(radius * .94f, .009f), new(radius, .019f),
            new(radius, h * .55f), new(radius * .995f, h * .59f), new(radius * .97f, h * .63f),
            new(radius * .89f, h * .67f), new(radius * .70f, h * .71f), new(radius * .43f, h * .75f),
            new(neck, h * .78f), new(neck, h * .94f), new(neck * 1.1f, h * .95f), new(neck * 1.1f, h * .975f),
            new(neck * .78f, h * .975f), new(neck * .78f, h * .78f),
            new(radius * .40f, h * .735f), new(radius * .80f, h * .66f), new(radius * .96f, h * .59f),
            new(radius * .96f, .022f), new(0, .022f)
        ];
        Vessel(bottle, "BottleBody", profile, Vector3.Zero, glass, spec.Section, spec.Depth);
        var liquid = ScenePalette.GlassMaterial(spec.Liquid, spec.Amber ? .76f : .26f);
        liquid.Roughness = .075f;
        liquid.CullMode = BaseMaterial3D.CullModeEnum.Back;
        var level = h * .605f;
        Vessel(bottle, "LiquidVolume", [new(0,.022f), new(radius*.93f,.022f), new(radius*.947f,level-.003f),
            new(radius*.93f,level), new(0,level)], Vector3.Zero, liquid, spec.Section, spec.Depth);
        var meniscus = (StandardMaterial3D)glass.Duplicate();
        meniscus.AlbedoColor = new(new Color(spec.Liquid), .38f);
        var rim = Ring(bottle, "LiquidMeniscus", radius * .938f, .0013f, new(0, level, 0), meniscus);
        rim.Scale = new(1, 1, spec.Depth);

        // The bottom is visibly thicker than the wall, as in a real liquor bottle.
        var heel = Ring(bottle, "HeavyGlassHeel", radius * .92f, .0033f, new(0, .012f, 0), glass);
        heel.Scale = new(1, 1, spec.Depth);
        Ring(bottle, "NeckCollar", neck * 1.035f, .0015f, new(0, h * .85f, 0), p.Brass);
        var cap = Group(bottle, "Closure", new(0, h * .945f, 0));
        var capMaterial = spec.Cork ? p.Oak : spec.Spirit == "GIN" ? p.Brass : ScenePalette.Metal("3d3931", .38f);
        Vessel(cap, "BeveledCap", [new(0,0),new(neck*1.08f,0),new(neck*1.15f,.004f),
            new(neck*1.15f,h*.095f),new(neck*.96f,h*.105f),new(0,h*.105f)], Vector3.Zero, capMaterial);
        if (!spec.Cork)
        {
            // Embossed grip flutes, batched in one mesh rather than separate cap objects.
            var fluteMesh = new CylinderMesh { TopRadius = .00065f, BottomRadius = .00065f, Height = h * .077f, RadialSegments = 6 };
            var flutes = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = fluteMesh, InstanceCount = 36 };
            for (var i = 0; i < 36; i++)
            {
                var angle = i * Mathf.Tau / 36;
                flutes.SetInstanceTransform(i, new(Basis.Identity, new(Mathf.Cos(angle) * neck * 1.15f, h * .048f, Mathf.Sin(angle) * neck * 1.15f)));
            }
            cap.AddChild(new MultiMeshInstance3D { Name = "CapGripFluting", Multimesh = flutes, MaterialOverride = capMaterial });
            Ring(cap, "FoilSeam", neck * 1.15f, .001f, new(0, .004f, 0), p.Brass);
        }
        else Ring(cap, "CorkLip", neck * 1.12f, .002f, new(0, .005f, 0), p.Brass);

        var labelMaterial = ScenePalette.Matte("ffffff", .68f);
        labelMaterial.MetallicSpecular = .12f;
        labelMaterial.AlbedoTexture = GD.Load<Texture2D>($"res://Assets/Labels/{spec.Label}.svg");
        labelMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        LabelPatch(bottle, radius + .001f, spec.Depth, spec.Section, h * .345f, h * .38f, labelMaterial);
        // Small stamped detail is modeled separately from the printed label.
        var seal = Cylinder(bottle, "ShoulderSeal", radius * .12f, radius * .12f, .0014f,
            new(0, h * .615f, radius * spec.Depth * .978f), p.Brass, 32);
        seal.RotationDegrees = new(90, 0, 0);
        if (pourSpout)
        {
            cap.Visible = false;
            Cylinder(bottle, "SpoutRubber", neck * 1.04f, neck * .8f, .024f, new(0, h * .97f, 0), p.Rubber);
            var spout = Group(bottle, "MeasuredPourer", new(0, h, 0));
            Vessel(spout, "SteelPourer", [new(.008f, 0), new(.008f, .018f), new(.0048f, .055f), new(.0033f, .055f), new(.004f, .008f)],
                Vector3.Zero, p.Steel);
            spout.RotationDegrees = new(0, 0, -12);
            Rod(spout, "AirVent", new(.008f, .006f, 0), new(.012f, .025f, 0), .0018f, p.Steel);
        }
        return bottle;
    }

    private static void LabelPatch(Node root, float radius, float depth, float exponent, float centerY, float height, Material material)
    {
        var surface = new SurfaceTool();
        surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for (var i = 0; i < 48; i++)
        {
            Add(i, 0); Add(i, 1); Add(i + 1, 0);
            Add(i + 1, 0); Add(i, 1); Add(i + 1, 1);
        }
        surface.GenerateNormals();
        surface.GenerateTangents();
        surface.Index();
        Mesh(root, "PrintedCurvedLabel", surface.Commit(), Vector3.Zero, material);
        void Add(int col, int row)
        {
            var a = -.9f + col * 1.8f / 48;
            var x = Mathf.Sin(a);
            var z = Mathf.Cos(a);
            surface.SetUV(new(col / 48f, 1 - row));
            surface.AddVertex(new(Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 2 / exponent) * radius,
                centerY + (row - .5f) * height, Mathf.Pow(z, 2 / exponent) * radius * depth));
        }
    }
}
