# Customer asset sources

These graphical assets and rig/pose data are **CC0-1.0**, separately from the project's MIT code.

- `Avatars/*.glb`: six seated, skinned patrons generated from MakeHuman's `hm08` base mesh, default skeleton, skin weights, and young adult macrodetail targets. Only exposed skin is exported: head, neck, upper chest, hands, and lower legs where required by skirts. `bartender_arms.glb` uses fitted sleeves and anatomical hands from the same CC0 data for the first-person rig.
- Skin atlases: middle-aged light-skinned male and young adult male/female core textures. The young adult shape targets provide the five background guests' body and facial proportions.
- Clothing: `male_casualsuit01` button-down shirt and jeans; `female_elegantsuit01` blouse and skirt, with diffuse, normal, and ambient occlusion maps.
- Hair: `short04`, `short02`, `long01`, `bob01`, and `braid01`; fitted `eyebrow001` and `eyelashes01` meshes and textures.
- Eyes and teeth: fitted core high-poly eyes, brown eye texture, and `teeth_base`. The eye mesh's shader-specific blue cornea marker shell is omitted for standard PBR rendering.
- `expression-poses.json`: selected CC0 facial pose units converted from the core `face-poseunits.bvh` and its frame mapping. These drive smiles, eyelids, and speaking motion on actual facial joints.

Inputs come from the [MakeHuman core repository](https://github.com/makehumancommunity/makehuman) and [system assets CC0 pack](https://files.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip). Original authors include the MakeHuman Team, Data Collection AB, Joel Palmius, and Jonas Hauquier. The [MakeHuman license](https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md) explicitly covers graphical data, skeletons, targets, and poses under CC0; see also its [licensing statement](https://www.makehumancommunity.org/about/license.html).

`scripts/fetch_customer.py` downloads and SHA-256 verifies inputs listed in [sources.json](sources.json), into the ignored `.tools/customer-source` directory. To regenerate the current models, install NumPy in a development Python environment, then run:

```sh
python scripts/fetch_customer.py
python scripts/prepare_avatars.py
```

The original project pipeline fits garments, hair, and eyes through barycentric fitting data, poses skin and clothing together, interpolates skin weights during subdivision, and exports glTF 2.0 rigs with eight joint influences per vertex. The game applies original procedural gaze, arm inverse kinematics, gestures, and drink handling; it contains no MakeHuman application code. Python and NumPy are development tools, not game runtime dependencies.
