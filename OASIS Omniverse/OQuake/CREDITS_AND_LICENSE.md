# OQuake – Credits and License

## OQuake is based on vkQuake

**OQuake** is **based on vkQuake** with the OASIS STAR API integrated for cross-game features in the OASIS Omniverse. Full credit for the underlying engine goes to the **vkQuake** project.

### Full credit to vkQuake

- **vkQuake** – [https://github.com/Novum/vkQuake](https://github.com/Novum/vkQuake)  
  vkQuake is a Vulkan port of id Software’s Quake, based on QuakeSpasm. It is developed by **Novum** and contributors. We use vkQuake as the base for OQuake and give full credit to the vkQuake authors and contributors.

- **vkQuake repository:** [https://github.com/Novum/vkQuake](https://github.com/Novum/vkQuake)  
- **vkQuake license:** GNU General Public License v2.0 (GPL-2.0). See the license file in the vkQuake repository (e.g. COPYING or LICENSE).

### What OASIS adds

The OASIS Omniverse integration adds:

- STAR API integration code (`oquake_star_integration.c`, `oquake_star_integration.h`, `oquake_version.h`)
- vkQuake-specific glue (`vkquake_oquake/pr_ext_oquake.c`, `apply_oquake_to_vkquake.ps1`)
- Build and run scripts (`BUILD_OQUAKE.bat`, `RUN OQUAKE.bat`)
- Documentation in this folder

These integration files are copied into your vkQuake source tree; you build from the vkQuake repository with our integration applied.

### Your obligations

When you build or distribute OQuake (or any build that includes vkQuake source):

1. **Give credit** to vkQuake (Novum) and comply with the vkQuake license (GPL-2.0).
2. If you distribute binaries, you must comply with the GPL (e.g. provide or offer corresponding source, keep license notices intact).
3. Do not remove or obscure vkQuake’s copyright or license notices from the vkQuake source code.

This document and the credit notices in our documentation do not replace your obligation to comply with the vkQuake (GPL-2.0) license when using or distributing code derived from vkQuake.

**License check:** vkQuake is GPL-2.0. Our integration is additive (we do not redistribute vkQuake’s source; we provide files you copy into your vkQuake clone). Giving clear credit and linking to vkQuake’s repo and license, plus complying with GPL when you distribute any build that includes vkQuake code, is the standard way to stay in compliance. If you redistribute OQuake binaries, you must also satisfy GPL-2.0 (e.g. offer corresponding source, keep notices intact).
