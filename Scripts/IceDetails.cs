// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Melted ice shells with internal frost volumes and batched trapped-air inclusions.</summary>
public static class IceDetails
{
    public const int CubeCount = 220;

    public static void Fill(Node3D basin)
    {
        var pile = Group(basin, "IcePile");
        var shell = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/Shaders/IceSurface.gdshader"), RenderPriority = 2 };
        var core = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/Shaders/IceCloud.gdshader"), RenderPriority = 0 };
        var shapes = Enumerable.Range(0, 8).Select(MeltedCube).ToArray();
        var coreShape = new SphereMesh { Radius = .5f, Height = 1, RadialSegments = 20, Rings = 12 };
        var air = ScenePalette.GlassMaterial("e7f5f5", .48f);
        air.Roughness = .17f;
        air.CullMode = BaseMaterial3D.CullModeEnum.Back;
        var bubbles = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new SphereMesh { Radius = .5f, Height = 1, RadialSegments = 8, Rings = 4 },
            InstanceCount = CubeCount * 10
        };
        var random = new RandomNumberGenerator { Seed = 7001 };
        var index = 0;
        for (var layer = 0; layer < 4; layer++)
            for (var row = 0; row < 5; row++)
                for (var col = 0; col < 11; col++)
                {
                    var size = random.RandfRange(.077f, .095f);
                    var position = new Vector3(Mathf.Clamp(-.441f + col * .088f + (layer % 2 == 0 ? -.018f : .018f) + random.RandfRange(-.019f, .019f), -.431f, .431f),
                        -.273f + layer * .071f + random.RandfRange(-.020f, .020f),
                        Mathf.Clamp(-.220f + row * .109f + random.RandfRange(-.025f, .025f), -.217f, .217f));
                    // The center of the pile rises slightly, leaving the rim and scoop cradle clear.
                    position.Y += layer == 3 ? .027f * (1 - Mathf.Abs(col - 5) / 6f) : 0;
                    var cube = Group(pile, $"IceCube{index:000}", position);
                    cube.Rotation = new(random.RandfRange(-.65f, .65f), random.RandfRange(0, Mathf.Tau), random.RandfRange(-.55f, .55f));
                    cube.Scale = new(size, size * random.RandfRange(.85f, 1), size);
                    var exterior = Mesh(cube, "MeltedShell", shapes[index % shapes.Length], Vector3.Zero, shell);
                    exterior.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                    var cloud = Mesh(cube, "CloudyCore", coreShape, new(random.RandfRange(-.055f, .055f), -.02f, random.RandfRange(-.04f, .04f)), core);
                    cloud.Scale = new(random.RandfRange(.6f, .8f), random.RandfRange(.6f, .85f), random.RandfRange(.55f, .8f));
                    cloud.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                    for (var b = 0; b < 10; b++)
                    {
                        var point = new Vector3(random.RandfRange(-.28f, .28f), random.RandfRange(-.3f, .3f), random.RandfRange(-.28f, .28f));
                        var diameter = random.RandfRange(.007f, .019f);
                        var local = new Transform3D(Basis.FromScale(new(diameter, diameter * random.RandfRange(1, 3.3f), diameter)), point);
                        bubbles.SetInstanceTransform(index * 10 + b, cube.Transform * local);
                    }
                    index++;
                }
        pile.AddChild(new MultiMeshInstance3D
        {
            Name = "TrappedAirBubbles",
            Multimesh = bubbles,
            MaterialOverride = air,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
    }

    private static ArrayMesh MeltedCube(int variant)
    {
        var surface = new SurfaceTool(); surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        const int cuts = 9;
        foreach (var normal in new[] { Vector3.Right, Vector3.Left, Vector3.Up, Vector3.Down, Vector3.Back, Vector3.Forward })
        {
            var u = Mathf.Abs(normal.Y) > .5f ? Vector3.Right : Vector3.Up;
            var v = normal.Cross(u);
            for (var x = 0; x < cuts; x++)
                for (var y = 0; y < cuts; y++)
                {
                    Add(x, y); Add(x, y + 1); Add(x + 1, y);
                    Add(x + 1, y); Add(x, y + 1); Add(x + 1, y + 1);
                }
            void Add(int x, int y)
            {
                var point = normal * .5f + u * (x / (float)cuts - .5f) + v * (y / (float)cuts - .5f);
                var inner = point.Clamp(Vector3.One * -.30f, Vector3.One * .30f);
                point = inner + (point - inner).Normalized() * .20f;
                var a = variant * 1.71f;
                point += new Vector3(Mathf.Sin(point.Y * 7 + a) * Mathf.Sin(point.Z * 8 + a) * .015f,
                    Mathf.Sin(point.X * 8 + a) * Mathf.Cos(point.Z * 7 - a) * .018f,
                    Mathf.Cos(point.X * 9 - a) * Mathf.Sin(point.Y * 8 + a) * .014f);
                surface.SetUV(new(x / (float)cuts, y / (float)cuts));
                surface.AddVertex(point);
            }
        }
        surface.GenerateNormals(); surface.GenerateTangents();
        surface.Index();
        return surface.Commit();
    }
}
