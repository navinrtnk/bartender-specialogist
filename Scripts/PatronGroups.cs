// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;
using static BartenderSpecialogist.Geometry;

namespace BartenderSpecialogist;

public static class PatronGroups
{
    public static void Build(Node root, ScenePalette palette)
    {
        var group = Group(root, "BackgroundPatrons");
        var specs = new[]{
            (Name:"GuestJames",Model:"gentleman_a",Table:0,Seat:0,Phase:7.0f,Tint:"d4e4ed"),
            (Name:"GuestDaniel",Model:"gentleman_b",Table:0,Seat:1,Phase:1.0f,Tint:"f4dfd5"),
            (Name:"GuestAmelia",Model:"woman_a",Table:2,Seat:0,Phase:3.8f,Tint:"fff2e8"),
            (Name:"GuestMaya",Model:"woman_b",Table:2,Seat:1,Phase:11.0f,Tint:"c7e0e6"),
            (Name:"GuestNia",Model:"woman_c",Table:2,Seat:2,Phase:17.0f,Tint:"e1c6d9")};
        var patrons = new List<(PatronActor Actor, int Table)>();
        foreach (var spec in specs)
        {
            var patron = new PatronActor
            {
                Name = spec.Name,
                Position = BarInterior.TablePositions[spec.Table] + BarInterior.ChairOffset(spec.Table, spec.Seat) + new Vector3(0, -.35f, 0),
                Rotation = new(0, BarInterior.ChairAngle(spec.Table, spec.Seat) + Mathf.Pi, 0)
            };
            group.AddChild(patron);
            patron.Build(palette, spec.Model, false, spec.Table == 2, spec.Phase, spec.Tint);
            patrons.Add((patron, spec.Table));
        }
        foreach (var (actor, table) in patrons)
        {
            actor.ConversationPartners = patrons.Where(p => p.Table == table && p.Actor != actor).Select(p => p.Actor).ToArray();
            actor.Evaluate(0, 1);
        }
    }
}
