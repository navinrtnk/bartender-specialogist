# Customer asset sources

These graphical assets are **CC0-1.0**, separately from the project's MIT code.

- `head.obj`, `hands.obj`: exposed head, neck, upper chest, and hands cropped from MakeHuman's `hm08` base mesh. [Original mesh](https://github.com/makehumancommunity/makehuman/blob/master/makehuman/data/3dobjs/base.obj).
- `skin.png`: `middleage_lightskinned_male_diffuse.png` from the MakeHuman core assets.
- `hair.obj`, `hair.png`: MakeHuman core `short04` hair mesh and texture.
- `eyes.obj`, `eyes.png`: MakeHuman core high-poly eyes and brown eye texture. The mesh is aligned to the base mesh eye joints, with the shader-specific blue cornea marker shell removed for standard PBR rendering.
- `clothing.obj`, `clothing-*.png`: fitted `male_casualsuit01` shirt and jeans, with diffuse, normal, and ambient occlusion maps.
- `brows.obj`, `brows.png`, `lashes.obj`, `lashes.png`: fitted `eyebrow001` and `eyelashes01` core assets.

The skin and hair come from the [MakeHuman system assets CC0 pack](https://files.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip). The eyes mesh is available in the [core repository](https://github.com/makehumancommunity/makehuman/tree/master/makehuman/data/eyes).

Original asset authors include the MakeHuman Team, Data Collection AB, Joel Palmius, and Jonas Hauquier. See the [core asset license](https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.ASSETS.md) and [MakeHuman licensing statement](https://www.makehumancommunity.org/about/license.html).

`scripts/fetch_customer.py` downloads only the required input files and verifies the SHA-256 checksums in [sources.json](sources.json). Inputs remain in the ignored `.tools/customer-source` directory.

`scripts/prepare_customer.py` records the original fitting, seated pose, skinning, coordinate transforms, and subdivision. The garment is fitted to the anatomical base through its CC0 fitting data, then skin and clothing receive the same seated transforms using MakeHuman's CC0 default rig/weight data. The neck extends into the shirt collar. Only exposed skin is exported; no unclothed full body, MakeHuman application code, or runtime rig is included.
