# OQuake engine recommendation

For OQuake (Quake + OASIS STAR API) we recommend **vkQuake** as the engine.

## Why vkQuake

- **Vulkan** – Best performance and modern graphics.
- **Widely used** – Most popular Quake source port; good documentation and community.
- **2021 rerelease** – Supports the official Quake rerelease content.
- **Extension builtins** – Has `pr_ext.c` and an extension-style builtin system, so the two OQuake STAR builtins (`ex_OQuake_OnKeyPickup`, `ex_OQuake_CheckDoorAccess`) can be added cleanly.
- **BUILD_OQUAKE.bat** – Script can clone vkQuake, copy OQuake/STAR files into `Quake/`, build, and copy **OQUAKE.exe** and **star_api.dll** into `OASIS Omniverse\OQuake\build\`.

## Alternatives

- **Ironwail** – OpenGL, also top-tier; good if you prefer OpenGL over Vulkan.
- **QuakeSpasm** – Broader compatibility, simpler; may need more work to add OQuake builtins.

See **ENGINE_COMPARISON.md** for a short comparison of vkQuake, QuakeSpasm, and Ironwail.

## Using vkQuake with OQuake

1. Set **VKQUAKE_SRC** in BUILD_OQUAKE.bat (e.g. `C:\Source\vkQuake`).
2. Run **BUILD_OQUAKE.bat** (from **Developer Command Prompt for VS 2022** so MSBuild and Vulkan SDK are found).
3. Follow **vkquake_oquake\VKQUAKE_OQUAKE_INTEGRATION.md** for host.c, pr_ext.c, and project changes (the apply script copies files; you still add sources and link star_api.lib).
4. Run **RUN OQUAKE.bat** to launch with your Quake game data (e.g. Steam basedir).
