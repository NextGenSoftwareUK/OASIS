# vkQuake OQuake integration

Files here let you build **OQuake**: vkQuake with OASIS STAR API so cross-game keys (OQuake ↔ ODOOM) work.

| File | Purpose |
|------|--------|
| **pr_ext_oquake.c** | PF_ wrappers for `ex_OQuake_OnKeyPickup` and `ex_OQuake_CheckDoorAccess`. Add to vkQuake's Quake target. |
| **VKQUAKE_OQUAKE_INTEGRATION.md** | Step-by-step: copy files, edit host.c and pr_ext.c, add sources and link star_api. |
| **apply_oquake_to_vkquake.ps1** | Copies OQuake + STAR files into a vkQuake `Quake/` folder. Run from OQuake root or with `-VkQuakeSrc`. |

**Quick start**

1. Run `apply_oquake_to_vkquake.ps1` to copy files into your vkQuake tree.
2. Follow **VKQUAKE_OQUAKE_INTEGRATION.md** to patch host.c, pr_ext.c, and the build.
3. Build vkQuake and put `star_api.dll` next to the exe.

**BUILD_OQUAKE.bat** (in the parent folder) can run the apply script automatically when building vkQuake; you still need to apply the host.c/pr_ext.c/build edits once per tree.
