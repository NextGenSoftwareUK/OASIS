# Building ODOOM and OQuake on Linux and macOS

Unix (Linux and macOS) equivalents of the Windows batch files **BUILD ODOOM.bat** and **BUILD_OQUAKE.bat**. For a full step-by-step guide, see **[Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md)** or **[Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md)**.

## Quick start

From the **OASIS Omniverse** directory (or from the game folders):

```bash
# Build and run ODOOM (UZDoom + OASIS)
cd "OASIS Omniverse/ODOOM"
./BUILD_ODOOM.sh          # build only
./BUILD_ODOOM.sh run      # build and launch
./RUN_ODOOM.sh            # run (builds if needed)

# Build and run OQuake (vkQuake + OASIS)
cd "OASIS Omniverse/OQuake"
./BUILD_OQUAKE.sh         # build only
./BUILD_OQUAKE.sh run     # build and launch
./RUN_OQUAKE.sh           # run (builds if needed)
```

## Prerequisites

### All

- **.NET 8 SDK** – for building STARAPIClient (NativeAOT).
- **PowerShell Core (pwsh)** – optional but recommended; used to patch UZDoom and vkQuake. Install: `sudo apt install powershell` or from [PowerShell GitHub](https://github.com/PowerShell/PowerShell).
- **CMake, build-essential** – for UZDoom and vkQuake.

### ODOOM

- **UZDoom source** – clone [UZDoom](https://github.com/coelckers/UZDoom) (or GZDoom/UZDoom) to e.g. `$HOME/Source/UZDoom`.
- Set `UZDOOM_SRC` if your path is different:
  ```bash
  export UZDOOM_SRC=/path/to/UZDoom
  ./BUILD_ODOOM.sh
  ```
- **Python 3** – for `create_odoom_face_pk3.py` (optional).
- Put **doom2.wad** in `ODOOM/build/` (or same directory as the ODOOM binary).

### OQuake

- **quake-rerelease-qc** – QuakeC source tree at e.g. `$HOME/Source/quake-rerelease-qc`. Set `QUAKE_SRC` if needed.
- **vkQuake** – clone [vkQuake](https://github.com/Novum/vkQuake) to e.g. `$HOME/Source/vkQuake`. Set `VKQUAKE_SRC` if needed.
- **Meson and Ninja** – `sudo apt install meson ninja-build` (or equivalent).
- **Vulkan SDK** – required to build vkQuake.
- **Quake game data** – id1 with pak0.pak, pak1.pak. Set `OQUAKE_BASEDIR` to your Quake install (e.g. Steam: `$HOME/.steam/steam/steamapps/common/Quake`).

## STAR API (shared library)

The **STARAPIClient** is built and deployed automatically when you run either game’s build script. To build and deploy it alone (e.g. for development):

```bash
cd "OASIS Omniverse"
# Linux and macOS (auto-detects OS and architecture):
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh
# Optional: force rebuild
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -ForceBuild
# macOS: override runtime (e.g. osx-arm64 or osx-x64)
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -Runtime osx-arm64
```

On **Linux** the client produces **libstar_api.so** (or **star_api.so**). On **macOS** it produces **libstar_api.dylib** (or **star_api.dylib**). These are copied into the ODOOM and OQuake folders and into their `build` output.

## Environment variables

| Variable           | Default (example)              | Description                    |
|--------------------|--------------------------------|--------------------------------|
| `UZDOOM_SRC`       | `$HOME/Source/UZDoom`         | UZDoom source tree             |
| `QUAKE_SRC`        | `$HOME/Source/quake-rerelease-qc` | QuakeC source               |
| `VKQUAKE_SRC`      | `$HOME/Source/vkQuake`        | vkQuake source tree            |
| `OQUAKE_BASEDIR`   | Steam Quake path              | Quake game data (id1, pak files) |
| `STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID` | – | OASIS auth (cross-game) |

## Scripts summary

| Script | Purpose |
|--------|--------|
| `ODOOM/BUILD_ODOOM.sh` | Build ODOOM (Linux/macOS). Options: `run`, `nosprites`. |
| `ODOOM/RUN_ODOOM.sh`   | Run ODOOM (builds if missing). |
| `OQuake/BUILD_OQUAKE.sh` | Build OQuake (Linux/macOS). Options: `run`, `batch`. |
| `OQuake/RUN_OQUAKE.sh`   | Run OQuake (builds if missing). |
| `STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` | Build and deploy STAR API for Linux or macOS (auto-detects). Options: `-ForceBuild`, `-Runtime`. |
| `STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh` | Wrapper that calls the unix script (Linux). |

## Sprites (ODOOM)

The Windows build can regenerate OQuake key/monster sprites from Quake PAK files and PowerShell. On Linux, **BUILD_ODOOM.sh** does not run sprite generation by default. If the UZDoom tree already has the required sprites (e.g. from a Windows build or from copying a pre-patched tree), the build will succeed. Otherwise you may need to generate or copy sprites once; see the Windows **BUILD ODOOM.bat** and the `wadsrc/static/sprites` requirements (OQKGA0, OQKSA0, OQW1A0, OQH1A0, OQM1A1, etc.).

## Troubleshooting

- **“UZDoom source not found”** – Set `UZDOOM_SRC` or clone UZDoom to the default path.
- **“libstar_api.so missing”** – Run the STAR API deploy script; ensure .NET 8 and a successful `dotnet publish` for `linux-x64`.
- **“pwsh not found”** – Install PowerShell Core so the UZDoom/vkQuake patch scripts can run; or patch the engine manually (see Windows integration docs).
- **vkQuake build fails** – Install Vulkan SDK, meson, ninja; ensure `VKQUAKE_SRC` points at a full vkQuake clone.
- **OQuake can’t find game data** – Set `OQUAKE_BASEDIR` to the directory that contains the `id1` folder and pak files.
