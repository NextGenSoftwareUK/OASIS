# ODOOM – Credits and License

## ODOOM is a fork of UZDoom

**ODOOM** is a **fork of UZDoom** with the OASIS STAR API integrated for cross-game features in the OASIS Omniverse. Full credit for the underlying engine goes to the **UZDoom** project.

### Full credit to UZDoom

- **UZDoom** – [https://github.com/UZDoom/UZDoom](https://github.com/UZDoom/UZDoom)  
  UZDoom is a feature-rich port for Doom engine games, based on GZDoom. We use the UZDoom source as the base for ODOOM and give full credit to the UZDoom authors and contributors.

- **UZDoom repository:** [https://github.com/UZDoom/UZDoom](https://github.com/UZDoom/UZDoom)  
- **UZDoom license:** GNU General Public License v3.0 (GPL-3.0). See the [LICENSE](https://github.com/UZDoom/UZDoom/blob/trunk/LICENSE) file in the UZDoom repository.

### What OASIS adds

The OASIS Omniverse integration adds:

- STAR API integration code (`uzdoom_star_integration.cpp`, `uzdoom_star_integration.h`, `odoom_branding.h`, `patch_uzdoom_engine.ps1`)
- Build and run scripts (`BUILD ODOOM.bat`, `RUN ODOOM.bat`)
- Documentation in this folder

These integration files are copied into your UZDoom source tree; you build from the UZDoom repository with our patches applied.

### Your obligations

When you build or distribute ODOOM (or any build that includes UZDoom source):

1. **Give credit** to UZDoom and comply with the [UZDoom license (GPL-3.0)](https://github.com/UZDoom/UZDoom/blob/trunk/LICENSE).
2. If you distribute binaries, you must comply with the GPL (e.g. provide or offer corresponding source, keep license notices intact).
3. Do not remove or obscure UZDoom’s copyright or license notices from the UZDoom source code.

This document and the credit notices in our documentation do not replace your obligation to comply with the UZDoom (GPL-3.0) license when using or distributing code derived from UZDoom.

**License check:** UZDoom is GPL-3.0. Our integration is additive (we do not redistribute UZDoom’s source; we provide files you copy into your UZDoom clone). Giving clear credit and linking to UZDoom’s repo and license, plus complying with GPL when you distribute any build that includes UZDoom code, is the standard way to stay in compliance. If you redistribute ODOOM binaries, you must also satisfy GPL-3.0 (e.g. offer corresponding source, keep notices intact).
