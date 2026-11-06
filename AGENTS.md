# Project preferences

- The user manages source control manually. Do not initialize repositories, create commits, configure Git, or add source-control configuration unless explicitly requested.
- When asked to optimize, simplify readable code and remove duplication while preserving the current gameplay, graphics, controls, layout, and implemented features. Do not trade visual quality for fewer lines or add unnecessary abstractions or dependencies.
- After optimization, remove obsolete project build outputs when permitted, perform a fresh build and relevant scene/render checks, and provide a versioned ZIP. Use `VERSION` as the version source; honor a user-specified version or increment it for a new release. Report any blocked cleanup accurately.
- Keep the README concise, limited to implemented features, and illustrated with exactly one current screenshot or GIF. Preserve original creator credit, the MIT license, and third-party notices.
- Each new drink needs recipe-specific unit tests. Test allowed liquor categories independently of brands/styles, required ingredients, step ordering, and the serve/cleanup lifecycle.
