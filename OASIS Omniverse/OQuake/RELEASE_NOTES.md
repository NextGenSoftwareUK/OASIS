# OQuake – Release notes

All notable changes and releases are listed here. Version and build are defined in **oquake_version.txt**.

---

## OQuake 1.0 (Build 1)

**First release of OQuake.**

- Quake integrated with the OASIS STAR API for cross-game features in the OASIS Omniverse.
- Based on **vkQuake** (Vulkan). Keys collected in OQuake can open doors in **ODOOM** and vice versa.
- Cross-game keys: OQuake **silver_key** ↔ ODOOM red; OQuake **gold_key** ↔ ODOOM blue/yellow.
- STAR API integration (`oquake_star_integration.c/h`), vkQuake host/pr_ext patches, build and run scripts.
- Documentation: WINDOWS_INTEGRATION.md, CREDITS_AND_LICENSE.md, README.md.

By NextGen World Ltd. Full credit to [vkQuake](https://github.com/Novum/vkQuake) (Novum, GPL-2.0).

---

## vkQuake (base engine)

OQuake is based on **vkQuake**. vkQuake is a Vulkan port of id Software’s Quake, based on QuakeSpasm. See [vkQuake](https://github.com/Novum/vkQuake) for upstream release notes and version history.
