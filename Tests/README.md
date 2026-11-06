# Tests

Run `./scripts/run.ps1 -Test` on Windows with Godot 4.7.2 .NET and the .NET 8 SDK installed. Exports run the same checks before packaging.

- **Unit tests:** xUnit tests exercise the production gesture timing and arm solver without starting Godot. They cover sip/grip ordering, wave easing, repeat cycles, hand orientation, unreachable targets, and invalid rigs. Run them alone with `dotnet test Tests/Unit/BartenderSpecialogist.Tests.csproj`.
- **UI tests:** real viewport input clicks both HUD buttons at two landscape sizes and exercises B/R/H, key-repeat handling, hidden controls, and touch-look/release. These run in Godot, including headless mode. For a rendered run: `godot --path . -- --test`.
- **Scene checks:** validate the station layout, loaded texture/detail assets, six rigged patrons, eye tracking across the aisle, continuous waves, and all five guests bringing their glasses to their mouths.
- **Margarita:** the production recipe is tested with multiple tequila brands/styles, missing ingredients, incorrect spirits, step ordering, the one-minute drink cycle, and the five-drink shift limit. The engine test uses real Space/arrow/Esc input to decline/accept orders, select bottles, make the drink, use both glass-menu options, serve, collect, load the dishwasher, and start another shift. Sampled scooping/pouring/shaking/straining poses check wrist-to-prop contact.
- **Hands and reach:** grip tests check the right thumb's direction for the scoop, bottles, glasses, and inverted shaker cap. Engine checks verify thumb opposition on the actual rig, order-taking from the workstation, talking distance, and the closer reach needed to serve a glass.
- **Progress updates:** checks cover malformed recipe definitions, invalid elapsed time, hint updates through serving and cleanup, and allocation limits for repeated recipe and sipping queries.

Failures return a nonzero exit code and name the failed check. Unit test reports go to `.godot/test-results/`. Test packages are development dependencies and are not shipped with the game. Unit tests use the [official xUnit v3 setup](https://xunit.net/docs/getting-started/v3/cmdline).

The README GIF is an offline capture. A short version 1.1 test at 1440 × 810 on the development machine's Intel integrated GPU measured 9–10 fps in Forward+; a Compatibility capture reached 36 fps after startup. These are local observations, not a performance guarantee or a benchmark of this refactor. UI input tests do not replace visual review or full desktop/mobile device testing.
