// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
using Godot;

namespace BartenderSpecialogist;

public partial class SeatedCustomer : PatronActor
{
    public void Build(ScenePalette palette)
    {
        Name = "SeatedCustomer";
        Position = new(.95f, 0, -1.02f);
        Build(palette, "bar_man", true);
    }
}
