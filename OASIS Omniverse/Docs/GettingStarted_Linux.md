# Getting Started — Linux

This guide walks you through building and running **ODOOM**, **OQuake**, and **STARAPIClient** on **Linux**. ODOOM and OQuake also support [Windows](GettingStarted_Windows.md) and [macOS](GettingStarted_Mac.md).

---

## 1. Prerequisites

Install the following (Debian/Ubuntu example; adjust for your distro).

**One-time install (ODOOM + OQuake + STARAPIClient NativeAOT, all deps):**

```bash
sudo apt update
sudo apt install -y build-essential binutils clang lld pkg-config cmake git dotnet-sdk-8.0 \
  libsdl2-dev libasound2-dev libglib2.0-dev libgtk-3-dev libvpx-dev \
  meson ninja-build libvulkan-dev vulkan-tools python3 powershell
```

If the STARAPIClient NativeAOT build fails to find the linker, set these before building (e.g. in `~/.bashrc`):

```bash
export DOTNET_CLANG_PATH=$(which clang)
export DOTNET_LINKER_PATH=$(which ld.lld)
```

**Or install in groups:**

```bash
# Base: C/C++ toolchain, linker, and .NET (required for STARAPIClient NativeAOT and UZDoom)
sudo apt update
sudo apt install -y build-essential binutils clang lld pkg-config cmake git dotnet-sdk-8.0

# ODOOM/UZDoom: SDL2, audio (ALSA), ZMusic/fluidsynth (glib, GTK), VPX (WebM)
sudo apt install -y libsdl2-dev libasound2-dev libglib2.0-dev libgtk-3-dev libvpx-dev

# Optional but recommended: PowerShell Core (for UZDoom/vkQuake patch scripts)
sudo apt install -y powershell

# For OQuake: Meson, Ninja, Vulkan
sudo apt install -y meson ninja-build libvulkan-dev vulkan-tools

# Python 3 (for ODOOM face pk3 script)
sudo apt install -y python3
```

| Tool | Purpose |
|------|----------|
| **.NET 8 SDK** | Build STARAPIClient (NativeAOT) |
| **build-essential** | gcc, g++, make; base C/C++ build |
| **binutils** | ld, as; required for NativeAOT linking |
| **clang, lld** | NativeAOT compiler/linker (use if gcc/ld fails); `ld.lld` from lld |
| **pkg-config** | Required by UZDoom CMake (SDL2, etc.) |
| **DOTNET_CLANG_PATH / DOTNET_LINKER_PATH** | Optional: point .NET to `clang` and `ld.lld` for NativeAOT |
| **libsdl2-dev** | UZDoom/ODOOM (SDL2) |
| **libasound2-dev** | UZDoom/ODOOM (ALSA) |
| **libglib2.0-dev, libgtk-3-dev** | UZDoom ZMusic/fluidsynth (glib-2.0, gtk+-3.0) |
| **libvpx-dev** | UZDoom (libvpx / VPX video codec) |
| **CMake** | Build UZDoom |
| **PowerShell Core (pwsh)** | Run patch scripts (optional; you can patch manually if needed) |
| **Meson, Ninja** | Build vkQuake |
| **Vulkan SDK / libvulkan-dev** | Build and run vkQuake |
| **Python 3** | ODOOM `create_odoom_face_pk3.py` |

---

## 2. Clone repositories

Use a common parent (e.g. `~/Source`). Scripts default to `$HOME/Source/<repo>`; you can override with environment variables.

```bash
mkdir -p ~/Source
cd ~/Source

git clone <OASIS-repo-url> OASIS-master
git clone https://github.com/coelckers/UZDoom.git UZDoom
git clone https://github.com/Novum/vkQuake.git vkQuake
git clone <quake-rerelease-qc-repo-url> quake-rerelease-qc
```

Replace the clone URLs with your actual OASIS and quake-rerelease-qc repos.

---

## 3. Build STARAPIClient (shared library for ODOOM & OQuake)

STARAPIClient is built and deployed automatically when you run the ODOOM or OQuake build scripts. To build or deploy it alone:

```bash
cd "/path/to/OASIS-master/OASIS Omniverse"
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh
```

Or the Linux-specific wrapper:

```bash
./STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh
```

This publishes for **linux-x64** and copies `libstar_api.so` (or `star_api.so`) and `star_api.h` into the ODOOM and OQuake folders.

If the build fails with "Platform linker ('clang' or 'gcc') not found" or linker errors, install `build-essential`, `binutils`, `clang`, and `lld`, then set:

```bash
export DOTNET_CLANG_PATH=$(which clang)
export DOTNET_LINKER_PATH=$(which ld.lld)
```

and run the build again (or add the exports to `~/.bashrc`).

Force a full rebuild:

```bash
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -ForceBuild
```

---

## 4. Build ODOOM

1. Go to ODOOM:

   ```bash
   cd "/path/to/OASIS-master/OASIS Omniverse/ODOOM"
   ```

2. Set `UZDOOM_SRC` if UZDoom is not in `$HOME/Source/UZDoom`:

   ```bash
   export UZDOOM_SRC=~/Source/UZDoom   # or your path
   ```

3. Build:

   ```bash
   ./BUILD_ODOOM.sh
   ```

4. Put **doom2.wad** in `ODOOM/build/` (or next to the ODOOM binary).

**Run ODOOM:**

```bash
./RUN_ODOOM.sh
```

Or build and run in one step:

```bash
./BUILD_ODOOM.sh run
```

**Options:**

- `./BUILD_ODOOM.sh nosprites` — Skip sprite regeneration (faster; use if sprites already exist in the UZDoom tree).

---

## 5. Build OQuake

1. Ensure you have **Quake game data** (e.g. Steam: `~/.steam/steam/steamapps/common/Quake` with `id1/pak0.pak`, `pak1.pak`).
2. Go to OQuake:

   ```bash
   cd "/path/to/OASIS-master/OASIS Omniverse/OQuake"
   ```

3. Set paths if needed (defaults shown):

   ```bash
   export QUAKE_SRC=~/Source/quake-rerelease-qc
   export VKQUAKE_SRC=~/Source/vkQuake
   export OQUAKE_BASEDIR="$HOME/.steam/steam/steamapps/common/Quake"
   ```

4. Build:

   ```bash
   ./BUILD_OQUAKE.sh
   ```

5. Run:

   ```bash
   ./RUN_OQUAKE.sh
   ```

Or build and run:

```bash
./BUILD_OQUAKE.sh run
```

---

## 6. Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| **UZDOOM_SRC** | `$HOME/Source/UZDoom` | UZDoom source tree |
| **QUAKE_SRC** | `$HOME/Source/quake-rerelease-qc` | QuakeC source tree |
| **VKQUAKE_SRC** | `$HOME/Source/vkQuake` | vkQuake source tree |
| **OQUAKE_BASEDIR** | `$HOME/.steam/steam/steamapps/common/Quake` | Quake game data (id1, paks) |
| **STAR_USERNAME** / **STAR_PASSWORD** or **STAR_API_KEY** / **STAR_AVATAR_ID** | — | OASIS auth (optional) |

---

## 7. Scripts summary

| Script | Purpose |
|--------|--------|
| `ODOOM/BUILD_ODOOM.sh` | Build ODOOM (STAR API + integration + UZDoom). Options: `run`, `nosprites`. |
| `ODOOM/RUN_ODOOM.sh` | Run ODOOM (builds if missing). |
| `OQuake/BUILD_OQUAKE.sh` | Build OQuake. Options: `run`, `batch`. |
| `OQuake/RUN_OQUAKE.sh` | Run OQuake (builds if missing). |
| `STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` | Build and deploy STAR API for Linux (and macOS). Option: `-ForceBuild`. |

---

## 8. Troubleshooting

| Issue | What to do |
|-------|------------|
| **"UZDoom source not found"** | Set `UZDOOM_SRC` or clone UZDoom to `~/Source/UZDoom`. |
| **"libstar_api.so missing"** | Run `./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` from OASIS Omniverse. |
| **"pwsh not found"** | Install PowerShell Core (`sudo apt install powershell`) or patch UZDoom/vkQuake manually. |
| **Missing OQ sprites** | Build once on Windows with sprite regen, or copy sprites into UZDoom `wadsrc/static/sprites/`; see ODOOM Windows build. |
| **vkQuake build fails** | Install Vulkan SDK and `meson`/`ninja`. Ensure `VKQUAKE_SRC` points at a full vkQuake clone. |
| **OQuake can't find game data** | Set `OQUAKE_BASEDIR` to the directory that contains the `id1` folder and pak files. |

For more detail, see [LINUX_BUILD.md](../LINUX_BUILD.md) and [STARAPIClient/README.md](../STARAPIClient/README.md).
