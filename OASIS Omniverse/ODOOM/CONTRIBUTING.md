# Contributing to ODOOM

Thank you for your interest in contributing to ODOOM. ODOOM is a fork of UZDoom with OASIS STAR API integration, by NextGen World Ltd.

## How to contribute

- **Bug reports and feature requests:** Open an issue in the OASIS repository (or the channel used by the ODOOM/OASIS team). Include steps to reproduce, your environment (Windows version, Visual Studio, UZDoom path), and ODOOM version (from `odoom_version.txt` or in-game).
- **Code and documentation:** Submit a pull request (or patch) against the OASIS repo. Changes to integration code in `OASIS Omniverse\ODOOM\` (e.g. `uzdoom_star_integration.cpp`, build scripts, docs) are welcome. Changes to the UZDoom engine itself should follow upstream [UZDoom](https://github.com/UZDoom/UZDoom) contribution guidelines where applicable.
- **License:** By contributing, you agree that your contributions will be compatible with the project license. ODOOM builds on UZDoom (GPL-3.0). See [CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md) and [LICENSE](LICENSE).

## Development setup

1. Clone the OASIS repo and have a UZDoom clone (e.g. `C:\Source\UZDoom`).
2. Set `UZDOOM_SRC` in **BUILD ODOOM.bat** if your path differs.
3. Run **BUILD ODOOM.bat** to copy integration files, apply branding, and build. Use a **Developer Command Prompt for Visual Studio** so CMake and MSBuild are in PATH.
4. Edit integration sources in `OASIS Omniverse\ODOOM\`; re-run the build script to copy and rebuild.

## Code style

- Match the existing style in the integration and build scripts (C++, PowerShell, batch).
- Keep credits and license notices intact; do not remove UZDoom attribution.

## Questions

For questions about the OASIS STAR API or the OASIS Omniverse, use the channels or contacts provided by the OASIS project.
