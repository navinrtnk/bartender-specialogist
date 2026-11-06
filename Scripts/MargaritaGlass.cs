// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public partial class MargaritaGlass : Node3D
{
    private MeshInstance3D _liquid = null!, _meniscus = null!;
    private Node3D _ice = null!;
    public void Build(ScenePalette p)
    {
        Name = "MargaritaGlass";
        BarGlassware.Tumbler(this, p, Vector3.Zero);
        var drink = ScenePalette.GlassMaterial("cbd17b", .74f);
        _liquid = Cylinder(this, "LimeCocktail", .031f, .039f, .065f, new(0, .0465f, 0), drink, 64);
        _meniscus = Ring(this, "Meniscus", .038f, .0008f, new(0, .079f, 0), drink);
        _ice = Group(this, "ServingIce");
        var iceMaterial = ScenePalette.GlassMaterial("dceced", .27f);
        for (var i = 0; i < 4; i++)
        {
            var cube = RoundedBox(_ice, "MeltedCube", new(.028f, .029f, .03f), new((i % 2 - .5f) * .031f, .035f + i / 2 * .027f, (i % 2 - .5f) * .014f), iceMaterial, .006f);
            cube.RotationDegrees = new(i * 13, i * 43, i * 7);
        }
        SetFill(0);
        _ice.Visible = false;
    }
    public void PrepareIce() => _ice.Visible = true;
    public void SetFill(float fraction)
    {
        fraction = Mathf.Clamp(fraction, 0, 1);
        _liquid.Visible = _meniscus.Visible = fraction > .001f;
        _liquid.Scale = new(1, Mathf.Max(.001f, fraction), 1);
        _liquid.Position = new(0, .014f + .0325f * fraction, 0);
        _meniscus.Position = new(0, .014f + .065f * fraction, 0);
    }
}
