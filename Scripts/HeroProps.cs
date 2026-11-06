// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

/// <summary>Close-view workstation props: thin walls, rolled edges, a real wire spring and cut citrus.</summary>
public static class HeroProps
{
    public static void Tools(Node3D station, ScenePalette p)
    {
        RoundedBox(station, "ServiceMat", new(1.5f, .016f, .5f), new(0, .006f, 0), p.Rubber, .006f);
        foreach (var z in new[] { -.239f, .239f })
            RoundedBox(station, "MoldedMatBorder", new(1.47f, .012f, .016f), new(0, .017f, z), p.Rubber, .005f);
        foreach (var x in new[] { -.731f, .731f })
            RoundedBox(station, "MoldedMatBorder", new(.016f, .012f, .47f), new(x, .017f, 0), p.Rubber, .005f);
        const int cols = 66, rows = 20;
        var pins = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new CylinderMesh { TopRadius = .0035f, BottomRadius = .004f, Height = .008f, RadialSegments = 8 },
            InstanceCount = cols * rows
        };
        for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
                pins.SetInstanceTransform(row * cols + col, new(Basis.Identity, new(-.7f + col * .0215f, .017f, -.205f + row * .0215f)));
        station.AddChild(new MultiMeshInstance3D { Name = "MatDrainageLugs", Multimesh = pins, MaterialOverride = p.Rubber });
        Tin(station, p, "LargeShakerTin", new(-.48f, .024f, -.055f), .19f, .047f);
        Tin(station, p, "SmallShakerTin", new(-.29f, .024f, -.075f), .15f, .039f);
        var jigger = Group(station, "DoubleJigger", new(-.045f, .025f, .035f));
        Vessel(jigger, "SpunSteelJigger", [new(.019f,0),new(.0185f,.002f),new(.008f,.039f),new(.0075f,.045f),
            new(.008f,.05f),new(.024f,.102f),new(.024f,.105f),new(.0225f,.105f),new(.0062f,.052f),
            new(.0062f,.044f),new(.0175f,.003f)], Vector3.Zero, p.Brass);
        Ring(jigger, "UpperRolledRim", .0233f, .001f, new(0, .104f, 0), p.Brass);
        Ring(jigger, "LowerRolledRim", .0185f, .001f, new(0, .002f, 0), p.Brass);
        Text(jigger, "MeasureEngraving", "60 mL", new(0, .084f, .0205f), .00022f, new("554b37"), 28);
        Strainer(station, p, new(.33f, .032f, .02f));
        Glass(station, p, new(.66f, .025f, -.035f));
    }

    public static Node3D Tin(Node root, ScenePalette p, string name, Vector3 position, float height, float rim)
    {
        var tin = Group(root, name, position);
        var bottom = rim * .72f;
        Vessel(tin, "TinBody", [new(0,0),new(bottom*.86f,0),new(bottom,.003f),new(bottom*1.025f,.008f),
            new(rim*.975f,height-.014f),new(rim,height-.005f),new(rim,height-.001f),new(rim*.98f,height+.001f),
            new(rim*.955f,height-.001f),new(rim*.955f,height-.007f),new(bottom*.96f,.011f),new(0,.009f)], Vector3.Zero, p.ToolSteel);
        Ring(tin, "RolledShakerLip", rim * .98f, .0015f, new Vector3(0, height, 0), p.ToolSteel);
        Ring(tin, "WeightedShakerHeel", bottom * .97f, .0017f, new Vector3(0, .003f, 0), p.ToolSteel);
        return tin;
    }

    private static void Strainer(Node root, ScenePalette p, Vector3 position)
    {
        var strainer = Group(root, "HawthorneStrainer", position);
        PerforatedPlate(strainer, p.ToolSteel);
        RoundedBox(strainer, "StampedHandle", new(.076f, .002f, .021f), new(-.019f, 0, 0), p.ToolSteel, .001f);
        Ring(strainer, "HandleHangingHole", .006f, .002f, new(-.062f, 0, 0), p.ToolSteel);
        var path = new Vector3[513];
        for (var i = 0; i < path.Length; i++)
        {
            var a = i * Mathf.Tau / (path.Length - 1);
            var coil = a * 40;
            var r = .043f + Mathf.Cos(coil) * .0032f;
            path[i] = new(.055f + Mathf.Cos(a) * r, .005f + Mathf.Sin(coil) * .0032f, Mathf.Sin(a) * r);
        }
        Tube(strainer, "CoiledSpringWire", path, .00075f, p.ToolSteel, 6);
        foreach (var side in new[] { -1f, 1f })
            RoundedBox(strainer, "RestingEar", new(.018f, .002f, .012f), new(.054f + side * .044f, 0, 0), p.ToolSteel, .001f);
    }

    private static void PerforatedPlate(Node root, Material steel)
    {
        // Each annular sector is triangulated around a real open hole, with a thin inner wall.
        var surface = new SurfaceTool(); surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        const float inner = .012f, outer = .036f, hole = .0023f, thickness = .001f;
        for (var cell = 0; cell < 12; cell++)
        {
            var angle = cell * Mathf.Tau / 12;
            var center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .025f;
            for (var i = 0; i < 32; i++)
            {
                var a = Direction(i); var b = Direction(i + 1);
                var ia = center + a * hole; var ib = center + b * hole;
                var oa = Boundary(a); var ob = Boundary(b);
                Triangle(ia, oa, ib, thickness, Vector3.Up); Triangle(oa, ob, ib, thickness, Vector3.Up);
                Triangle(ib, oa, ia, -thickness, Vector3.Down); Triangle(ib, ob, oa, -thickness, Vector3.Down);
                Vertex(ia, thickness, new(-a.X, 0, -a.Y)); Vertex(ib, thickness, new(-b.X, 0, -b.Y)); Vertex(ia, -thickness, new(-a.X, 0, -a.Y));
                Vertex(ib, thickness, new(-b.X, 0, -b.Y)); Vertex(ib, -thickness, new(-b.X, 0, -b.Y)); Vertex(ia, -thickness, new(-a.X, 0, -a.Y));
            }
            Vector2 Direction(int i) => new(Mathf.Cos(i * Mathf.Tau / 32), Mathf.Sin(i * Mathf.Tau / 32));
            Vector2 Boundary(Vector2 direction)
            {
                var b = center.Dot(direction);
                var distance = -b + Mathf.Sqrt(b * b - center.LengthSquared() + outer * outer);
                var discriminant = b * b - center.LengthSquared() + inner * inner;
                if (discriminant >= 0)
                {
                    var t = -b - Mathf.Sqrt(discriminant);
                    if (t > .00001f) distance = Mathf.Min(distance, t);
                }
                foreach (var side in new[] { -1f, 1f })
                {
                    var edgeAngle = angle + side * Mathf.Pi / 12;
                    var edge = new Vector2(Mathf.Cos(edgeAngle), Mathf.Sin(edgeAngle));
                    var normal = new Vector2(-edge.Y, edge.X);
                    var denominator = normal.Dot(direction);
                    if (Mathf.Abs(denominator) < .000001f) continue;
                    var t = -normal.Dot(center) / denominator;
                    if (t > .00001f && edge.Dot(center + direction * t) > 0) distance = Mathf.Min(distance, t);
                }
                return center + direction * distance;
            }
        }
        surface.GenerateTangents();
        surface.Index();
        Mesh(root, "PerforatedPlate", surface.Commit(), Vector3.Zero, steel);
        Cylinder(root, "StrainerSolidHub", inner, inner, thickness * 2, new(.055f, 0, 0), steel, 48);
        Vessel(root, "RolledPlateEdge", [new(outer, -thickness), new(.041f, -thickness), new(.041f, thickness), new(outer, thickness), new(outer, -thickness)], new(.055f, 0, 0), steel);
        void Triangle(Vector2 a, Vector2 b, Vector2 c, float y, Vector3 n) { Vertex(a, y, n); Vertex(b, y, n); Vertex(c, y, n); }
        void Vertex(Vector2 v, float y, Vector3 n)
        {
            surface.SetNormal(n); surface.SetUV(v * 10); surface.AddVertex(new(.055f + v.X, y, v.Y));
        }
    }

    public static void Salt(Node root, ScenePalette p, Vector3 position)
    {
        var salt = Group(root, "SaltRimmer", position);
        Vessel(salt, "GlazedCeramicDish", [new(0,0),new(.081f,0),new(.091f,.007f),new(.096f,.022f),
            new(.095f,.026f),new(.090f,.026f),new(.085f,.012f),new(0,.012f)], Vector3.Zero, p.Paper);
        var bed = ScenePalette.Matte("ede9df", .93f);
        Cylinder(salt, "SaltBed", .084f, .084f, .003f, new(0, .015f, 0), bed, 64);
        var grains = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = Vector3.One },
            InstanceCount = 700
        };
        var random = new RandomNumberGenerator { Seed = 429 };
        for (var i = 0; i < grains.InstanceCount; i++)
        {
            var a = random.RandfRange(0, Mathf.Tau); var r = Mathf.Sqrt(random.Randf()) * .081f;
            var size = random.RandfRange(.0005f, .0012f);
            var basis = Basis.FromEuler(new(random.Randf(), random.Randf(), random.Randf())).Scaled(new(size, size * .65f, size));
            grains.SetInstanceTransform(i, new(basis, new(Mathf.Cos(a) * r, .017f + random.RandfRange(0, .001f), Mathf.Sin(a) * r)));
        }
        salt.AddChild(new MultiMeshInstance3D { Name = "SaltCrystals", Multimesh = grains, MaterialOverride = p.Paper });
    }

    private static void Glass(Node root, ScenePalette p, Vector3 position)
    {
        var glass = Group(root, "RocksGlass", position);
        Vessel(glass, "ThickWalledGlass", [new(0,0),new(.035f,0),new(.039f,.004f),new(.04f,.012f),
            new(.043f,.098f),new(.044f,.103f),new(.043f,.105f),new(.0408f,.105f),
            new(.0398f,.099f),new(.0365f,.015f),new(0,.013f)], Vector3.Zero, p.Glass);
        Ring(glass, "PolishedGlassLip", .0425f, .0012f, new(0, .104f, 0), p.Glass);
        Ring(glass, "HeavyGlassBase", .037f, .003f, new(0, .006f, 0), p.Glass);
        for (var i = 0; i < 12; i++)
        {
            var a = i * Mathf.Tau / 12;
            Tube(glass, "CutGlassFlute", [new(Mathf.Cos(a)*.0397f,.017f,Mathf.Sin(a)*.0397f),
                new(Mathf.Cos(a)*.042f,.082f,Mathf.Sin(a)*.042f)], .00065f, p.Glass, 6);
        }
    }

    public static void Prep(Node3D prep, ScenePalette p)
    {
        RoundedBox(prep, "CuttingBoard", new(.62f, .025f, .4f), new(0, .014f, 0), p.Oak, .012f);
        // Routed board edge and juice groove are visible at arm's length.
        foreach (var z in new[] { -.166f, .166f })
            RoundedBox(prep, "RoutedJuiceGroove", new(.535f, .001f, .005f), new(0, .027f, z), p.Walnut, .0004f);
        foreach (var x in new[] { -.268f, .268f })
            RoundedBox(prep, "RoutedJuiceGroove", new(.005f, .001f, .33f), new(x, .027f, 0), p.Walnut, .0004f);
        Sphere(prep, "WholeLime", new(.078f, .063f, .065f), new(-.15f, .06f, -.07f), p.LimeSkin);
        LimeHalf(prep, p, new(-.02f, .028f, -.015f));
        Knife(prep, p, new(.13f, .031f, .1f));
        CitrusPress(prep, p, new(.13f, .03f, -.085f));
    }

    private static void LimeHalf(Node root, ScenePalette p, Vector3 position)
    {
        var lime = Group(root, "CutLimeHalf", position);
        const float r = .035f, h = .031f;
        Vessel(lime, "DimpledRind", [new(0, 0), new(r * .45f, .003f), new(r * .82f, .012f), new(r, .026f), new(r, h), new(0, h)], Vector3.Zero, p.LimeSkin);
        Cylinder(lime, "WhitePith", r * .94f, r * .94f, .001f, new(0, h + .0005f, 0), p.Paper, 64);
        var pulp = ScenePalette.Matte("c4d68d", .28f);
        Cylinder(lime, "JuicyCutFace", r * .87f, r * .87f, .001f, new(0, h + .0011f, 0), pulp, 64);
        for (var i = 0; i < 9; i++)
        {
            var a = i * Mathf.Tau / 9;
            Rod(lime, "CitrusMembrane", new(0, h + .0018f, 0), new(Mathf.Cos(a) * r * .88f, h + .0018f, Mathf.Sin(a) * r * .88f), .0005f, p.Paper);
        }
        var vesicles = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new SphereMesh { Radius = .0013f, Height = .0026f, RadialSegments = 6, Rings = 4 },
            InstanceCount = 90
        };
        for (var i = 0; i < 90; i++)
        {
            var a = i * 2.39996f;
            var radius = Mathf.Sqrt((i + .5f) / 90) * r * .81f;
            vesicles.SetInstanceTransform(i, new(Basis.FromScale(new(.7f, .26f, 1.5f)), new(Mathf.Cos(a) * radius, h + .0019f, Mathf.Sin(a) * radius)));
        }
        lime.AddChild(new MultiMeshInstance3D { Name = "PulpVesicles", Multimesh = vesicles, MaterialOverride = pulp });
        Sphere(lime, "CitrusSeed", new(.0025f, .001f, .006f), new(.01f, h + .002f, -.006f), p.Paper);
    }

    private static void Knife(Node root, ScenePalette p, Vector3 position)
    {
        var knife = Group(root, "ParingKnife", position);
        var blade = new SurfaceTool(); blade.Begin(Godot.Mesh.PrimitiveType.Triangles);
        Vector3[] outline = [new(-.105f, 0, -.004f), new(-.06f, .001f, -.013f), new(.047f, .001f, -.013f), new(.047f, 0, .013f), new(-.065f, 0, .009f)];
        for (var i = 1; i < outline.Length - 1; i++)
        {
            blade.SetNormal(Vector3.Up); blade.AddVertex(outline[0]);
            blade.SetNormal(Vector3.Up); blade.AddVertex(outline[i + 1]);
            blade.SetNormal(Vector3.Up); blade.AddVertex(outline[i]);
        }
        var steel = (StandardMaterial3D)p.ToolSteel.Duplicate(); steel.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(knife, "GroundKnifeBlade", blade.Commit(), Vector3.Zero, steel);
        Tube(knife, "HonedCuttingEdge", [outline[0], outline[4], outline[3]], .00045f, p.ToolSteel, 6);
        RoundedBox(knife, "CompositeHandle", new(.087f, .014f, .025f), new(.09f, .003f, 0), p.Ink, .005f);
        RoundedBox(knife, "SteelBolster", new(.008f, .014f, .025f), new(.047f, .003f, 0), p.ToolSteel, .002f);
        foreach (var x in new[] { .068f, .108f })
            Cylinder(knife, "HandleRivet", .002f, .002f, .0007f, new(x, .0105f, 0), p.ToolSteel, 16);
    }

    private static void CitrusPress(Node root, ScenePalette p, Vector3 position)
    {
        var press = Group(root, "HingedCitrusPress", position);
        Vessel(press, "HollowPressCup", [new(0,0),new(.015f,.001f),new(.03f,.012f),new(.043f,.031f),
            new(.043f,.034f),new(.040f,.034f),new(.027f,.014f),new(.013f,.004f),new(0,.004f)], Vector3.Zero, p.Brass);
        RoundedBox(press, "LowerSqueezeHandle", new(.145f, .014f, .025f), new(.104f, .024f, 0), p.Brass, .006f);
        var hinge = Cylinder(press, "HingePin", .006f, .006f, .066f, new(.042f, .04f, 0), p.ToolSteel, 24);
        hinge.RotationDegrees = new(90, 0, 0);
        var lid = Group(press, "OpenPressLid", new(.041f, .039f, 0));
        lid.RotationDegrees = new(0, 0, 32);
        Sphere(lid, "PressDome", new(.075f, .021f, .071f), new(-.042f, .001f, 0), p.Brass);
        RoundedBox(lid, "UpperSqueezeHandle", new(.145f, .012f, .024f), new(.065f, 0, 0), p.Brass, .005f);
        for (var i = 0; i < 5; i++)
        {
            var a = i * Mathf.Tau / 5;
            Cylinder(press, "JuiceDrainHole", .0016f, .0016f, .0005f, new(Mathf.Cos(a) * .012f, .0045f, Mathf.Sin(a) * .012f), p.Ink, 12);
        }
    }

    public static void Towel(Node root, ScenePalette p, Vector3 position)
    {
        var towel = Group(root, "FoldedBarTowel", position);
        RoundedBox(towel, "FoldedClothBody", new(.3f, .014f, .18f), new(0, .007f, 0), p.Cloth, .006f);
        var surface = new SurfaceTool(); surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        const int nx = 32, nz = 20;
        for (var x = 0; x < nx; x++)
            for (var z = 0; z < nz; z++)
            {
                Add(x, z); Add(x, z + 1); Add(x + 1, z);
                Add(x + 1, z); Add(x, z + 1); Add(x + 1, z + 1);
            }
        surface.GenerateNormals(); surface.GenerateTangents();
        surface.Index();
        Mesh(towel, "SoftFabricFolds", surface.Commit(), Vector3.Zero, p.Cloth);
        for (var band = 0; band < 3; band++)
        {
            var line = new Vector3[33];
            for (var i = 0; i < line.Length; i++) line[i] = Point(i, 2 + band * .7f) + new Vector3(0, .0007f, 0);
            Tube(towel, "WovenTowelStripe", line, .0007f, p.DarkTeal, 6);
        }
        void Add(int x, int z) { surface.SetUV(new(x / (float)nx, z / (float)nz)); surface.AddVertex(Point(x, z)); }
        Vector3 Point(float x, float z) => new(-.15f + x * .3f / nx, .016f + Mathf.Sin(x * .31f) * .0025f + Mathf.Sin(z * .35f + x * .12f) * .0017f, -.09f + z * .18f / nz);
    }
}
