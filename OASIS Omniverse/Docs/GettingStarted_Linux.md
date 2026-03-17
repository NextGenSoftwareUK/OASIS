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

# For OQuake: Meson, Ninja, Vulkan, glslang (glslangValidator for shader compilation)
sudo apt install -y meson ninja-build libvulkan-dev vulkan-tools glslang-tools

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
| **Vulkan SDK / libvulkan-dev, vulkan-tools** | Build and run vkQuake |
| **glslang-tools** | Provides `glslangValidator`; required by vkQuake meson build for shader compilation |
| **Python 3** | ODOOM `create_odoom_face_pk3.py` and (with **Pillow**) anorak face texture `prepare_odoom_face_texture.py` on Linux. Install Pillow via `sudo apt install python3-pil` (recommended) or use a venv and `pip install Pillow`. |

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

   On Linux, the anorak HUD face (OASFACE.png) is prepared by a Python fallback; if you see a warning about face processing, install Pillow: `sudo apt install python3-pil` (recommended on Debian/Ubuntu). If your system uses an externally-managed Python (PEP 668), either install `python3-pil` or use a venv: `python3 -m venv .venv && .venv/bin/pip install Pillow`, then run the build with `PYTHON3_EXE=.venv/bin/python3 ./BUILD_ODOOM.sh` so the face script uses that Python.

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

### 5.1 Prerequisites for OQuake (Linux)

- **meson** and **ninja** — required to build vkQuake. Install with:
  ```bash
  sudo apt install -y meson ninja-build
  ```
- **Vulkan** and **glslang** — vkQuake uses Vulkan and needs the Vulkan dev package (for Meson’s `dependency('vulkan')`) and `glslangValidator` for shaders. Install with:
  ```bash
  sudo apt install -y libvulkan-dev vulkan-tools glslang-tools
  ```
  If the build fails with **"Dependency vulkan not found"**, install `libvulkan-dev` (provides `vulkan.pc` for pkg-config/Meson). If you see **"Program 'glslangValidator' not found"**, install `glslang-tools`.
- **Opus (optional)** — If you see **"Run-time dependency opus found: NO"** or **"opusfile found: NO"**, vkQuake will still build but without Opus audio support. To enable it: `sudo apt install -y libopus-dev libopusfile-dev`.
- **vkQuake source** — clone to e.g. `~/Source/vkQuake` and set `VKQUAKE_SRC` if not using the default.
- **Quake game data** — e.g. Steam Quake at `~/.steam/steam/steamapps/common/Quake` with `id1/pak0.pak`, `pak1.pak`. Set `OQUAKE_BASEDIR` if your game data is elsewhere; this path is also used to copy **face_anorak.png** into `id1/gfx/` for the OASIS HUD face when beamed in.

### 5.2 Build steps

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

   If you see **"meson/ninja not found"**, install them (see 5.1). If you see a warning about **face_anorak.png** and **"Cannot find drive 'C'"**, the apply script was given a Windows path; ensure `OQUAKE_BASEDIR` is set to your Linux Quake game data path (e.g. the Steam path above) so the script can copy the anorak face into `id1/gfx/`.

5. Run:

   ```bash
   ./RUN_OQUAKE.sh
   ```

Or build and run:

```bash
./BUILD_OQUAKE.sh run
```

**Cross-game keys (optional):** set `STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID` for OASIS auth.

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
| **face_anorak / OASFACE.png failed** (pwsh "Windows.Win32.PInvoke" on Linux) | Normal on Linux; the build uses a Python fallback. Install Pillow: `sudo apt install python3-pil` (Debian/Ubuntu). If you see "externally-managed-environment", use a venv: `python3 -m venv .venv && .venv/bin/pip install Pillow`, or use `python3-pil`. Then re-run `./BUILD_ODOOM.sh`. If no Pillow, the build continues and uses existing `textures/OASFACE.png` if present. |
| **"meson/ninja not found"** | Install: `sudo apt install -y meson ninja-build`. Then re-run `./BUILD_OQUAKE.sh`. |
| **"glslangValidator not found"** | Install: `sudo apt install -y glslang-tools`. Then re-run `./BUILD_OQUAKE.sh`. |
| **"Dependency vulkan not found"** (Meson) | Install the Vulkan dev package: `sudo apt install -y libvulkan-dev`. Then remove the vkQuake build dir and re-run: `rm -rf "$VKQUAKE_SRC/build"` and `./BUILD_OQUAKE.sh`. |
| **"opus found: NO" / "opusfile found: NO"** | Optional. Build continues without Opus audio. To enable: `sudo apt install -y libopus-dev libopusfile-dev`, then `rm -rf "$VKQUAKE_SRC/build"` and `./BUILD_OQUAKE.sh`. |
| **vkQuake build fails** | Install Vulkan SDK (`libvulkan-dev`, `vulkan-tools`), `meson`, `ninja`, and `glslang-tools`. Ensure `VKQUAKE_SRC` points at a full vkQuake clone. |
| **OQuake can't find game data** | Set `OQUAKE_BASEDIR` to the directory that contains the `id1` folder and pak files. |
| **"face_anorak.png ... Cannot find drive 'C'"** | Set `OQUAKE_BASEDIR` to your Linux Quake game path (e.g. `$HOME/.steam/steam/steamapps/common/Quake`) so the apply script can copy the anorak face into `id1/gfx/`. |

For more detail, see [LINUX_BUILD.md](../LINUX_BUILD.md) and [STARAPIClient/README.md](../STARAPIClient/README.md).
