# Bartender Specialogist

Step behind the counter of a small neighborhood bar, built with C# and Godot 4.7.2 .NET.

**Original work by [Navin Ratnayake (@navinrtnk)](https://github.com/navinrtnk).** Copyright © 2026 Navin Ratnayake.

![The current bar and customer animations, captured offline](docs/bar-overview.gif)

Explore four tables and a detailed bartender station with liquor shelves, an ice well, glasses, margarita supplies, a sink, and a dishwasher. The customer at the counter follows your gaze, smiles, and waves. Five background guests sip cosmopolitans and old fashioneds while chatting.

Equipment and drinks are scenery; this is an explorable environment, with no drink-making or dialogue yet. The GIF is an offline capture, so it doesn't represent real-time frame rate.

## Run and test

Install **Godot 4.7.2 .NET** and the **.NET 8 SDK**, open `project.godot`, build, and press **F5**.

On Windows, `./scripts/run.ps1` launches the game. Add `-Compatibility` for simpler lighting, `-Test` for unit/UI/scene checks, or `-Export` for a fresh versioned Windows ZIP. See [test details](Tests/README.md).

Use WASD and mouse/arrows to explore, B to turn, R to reset, H to hide the HUD, and Esc to release the mouse. Touch controls are also supported.

Original code and procedural assets use the [MIT License](LICENSE); CC0 assets and spirit trademarks are covered in [third-party notices](THIRD_PARTY_NOTICES.md). Please credit and link to this original project when sharing a derivative.
