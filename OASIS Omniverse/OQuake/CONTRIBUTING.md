# Contributing to OQuake

Thank you for your interest in contributing to OQuake. OQuake is Quake (vkQuake-based) with OASIS STAR API integration, by NextGen World Ltd.

## How to contribute

- **Bug reports and feature requests:** Open an issue in the OASIS repository (or the channel used by the OQuake/OASIS team). Include steps to reproduce, your environment (Windows, Vulkan SDK, Visual Studio, vkQuake path), and OQuake version (from `oquake_version.txt` or in-game).
- **Code and documentation:** Submit a pull request (or patch) against the OASIS repo. Changes to integration code in `OASIS Omniverse\OQuake\` (e.g. `oquake_star_integration.c`, build scripts, docs) are welcome. Changes to vkQuake itself should follow upstream [vkQuake](https://github.com/Novum/vkQuake) contribution guidelines where applicable.
- **License:** By contributing, you agree that your contributions will be compatible with the project license. OQuake builds on vkQuake (GPL-2.0). See [CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md) and [LICENSE](LICENSE).

## Development setup

1. Clone the OASIS repo. Have QuakeC tree (e.g. quake-rerelease-qc) and vkQuake clone (e.g. `C:\Source\vkQuake`).
2. Set `QUAKE_SRC` and `VKQUAKE_SRC` in **BUILD_OQUAKE.bat** if your paths differ.
3. Run **BUILD_OQUAKE.bat** from a **Developer Command Prompt for Visual Studio** so MSBuild is in PATH. Install Vulkan SDK for vkQuake build.
4. Edit integration sources in `OASIS Omniverse\OQuake\`; re-run the build script to copy and rebuild.

## Code style

- Match the existing style in the integration and build scripts (C, PowerShell, batch).
- Keep credits and license notices intact; do not remove vkQuake attribution.

## Questions

For questions about the OASIS STAR API or the OASIS Omniverse, use the channels or contacts provided by the OASIS project.
