# Getting Started — macOS

This guide walks you through building and running **ODOOM**, **OQuake**, and **STARAPIClient** on **macOS** (Intel and Apple Silicon). ODOOM and OQuake also support [Windows](GettingStarted_Windows.md) and [Linux](GettingStarted_Linux.md).

---

## 1. Prerequisites

Install the following (using [Homebrew](https://brew.sh/) if not already installed).

**One-time install (ODOOM + OQuake + STARAPIClient NativeAOT, all deps):**

```bash
# Install Homebrew (if needed): /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

brew install pkg-config cmake dotnet@8 sdl2 glib gtk+3 libvpx meson ninja vulkan-headers molten-vk python3 powershell
```

Xcode Command Line Tools (or full Xcode) provide `clang` for NativeAOT; install with `xcode-select --install` if needed. If the STARAPIClient NativeAOT build fails to find the linker, set (e.g. in `~/.zshrc` or `~/.bash_profile`):

```bash
export DOTNET_CLANG_PATH=$(which clang)
export DOTNET_LINKER_PATH=$(which ld)   # or path to ld64 on macOS
```

**Or install in groups:**

```bash
# Build tools and .NET
brew install pkg-config cmake dotnet@8

# ODOOM/UZDoom: SDL2, ZMusic/fluidsynth (glib, GTK), VPX (WebM)
brew install sdl2 glib gtk+3 libvpx

# Optional: PowerShell Core (for UZDoom/vkQuake patch scripts)
brew install powershell

# For OQuake: Meson, Ninja, Vulkan
brew install meson ninja vulkan-headers molten-vk

# Python 3 (for ODOOM face pk3 script)
brew install python3
```

| Tool | Purpose |
|------|----------|
| **.NET 8 SDK** | Build STARAPIClient (NativeAOT) for osx-x64 or osx-arm64 |
| **pkg-config** | Required by UZDoom CMake (SDL2, etc.) |
| **sdl2, glib, gtk+3, libvpx** | UZDoom/ODOOM (SDL2, ZMusic/fluidsynth, VPX) |
| **CMake** | Configure and build UZDoom |
| **PowerShell Core (pwsh)** | Run patch scripts (optional) |
| **Meson, Ninja** | Build vkQuake |
| **Vulkan / MoltenVK** | Build and run vkQuake on macOS |
| **Python 3** | ODOOM `create_odoom_face_pk3.py` |

On **Apple Silicon (M1/M2/M3)**, .NET and the STAR API build for **osx-arm64** automatically when you run the scripts.

---

## 2. Clone repositories

Use a common parent (e.g. `~/Source`). Scripts default to `$HOME/Source/<repo>`.

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

The script detects macOS and builds for **osx-arm64** (Apple Silicon) or **osx-x64** (Intel). It copies `libstar_api.dylib` (or `star_api.dylib`) and `star_api.h` into the ODOOM and OQuake folders.

Force a full rebuild:

```bash
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -ForceBuild
```

Override runtime explicitly:

```bash
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -Runtime osx-arm64
# or
./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -Runtime osx-x64
```

---

## 4. Build ODOOM

1. Go to ODOOM:

   ```bash
   cd "/path/to/OASIS-master/OASIS Omniverse/ODOOM"
   ```

2. Set `UZDOOM_SRC` if UZDoom is not in `$HOME/Source/UZDoom`:

   ```bash
   export UZDOOM_SRC=~/Source/UZDoom
   ```

3. Build:

   ```bash
   chmod +x BUILD_ODOOM.sh RUN_ODOOM.sh
   ./BUILD_ODOOM.sh
   ```

4. Put **doom2.wad** in `ODOOM/build/` (or next to the ODOOM binary).

**Run ODOOM:**

```bash
./RUN_ODOOM.sh
```

Or build and run:

```bash
./BUILD_ODOOM.sh run
```

**Options:**

- `./BUILD_ODOOM.sh nosprites` — Skip sprite regeneration.

---

## 5. Build OQuake

1. Ensure you have **Quake game data**. On macOS, Steam often installs to:
   `~/Library/Application Support/Steam/steamapps/common/Quake`  
   (with `id1/pak0.pak`, `pak1.pak`). The scripts use this path by default.

2. Go to OQuake:

   ```bash
   cd "/path/to/OASIS-master/OASIS Omniverse/OQuake"
   ```

3. Set paths if needed (defaults are for macOS):

   ```bash
   export QUAKE_SRC=~/Source/quake-rerelease-qc
   export VKQUAKE_SRC=~/Source/vkQuake
   export OQUAKE_BASEDIR="$HOME/Library/Application Support/Steam/steamapps/common/Quake"
   ```

4. Build:

   ```bash
   chmod +x BUILD_OQUAKE.sh RUN_OQUAKE.sh
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

| Variable | Default (macOS) | Description |
|----------|------------------|-------------|
| **UZDOOM_SRC** | `$HOME/Source/UZDoom` | UZDoom source tree |
| **QUAKE_SRC** | `$HOME/Source/quake-rerelease-qc` | QuakeC source tree |
| **VKQUAKE_SRC** | `$HOME/Source/vkQuake` | vkQuake source tree |
| **OQUAKE_BASEDIR** | `$HOME/Library/Application Support/Steam/steamapps/common/Quake` | Quake game data (id1, paks) |
| **STAR_USERNAME** / **STAR_PASSWORD** or **STAR_API_KEY** / **STAR_AVATAR_ID** | — | OASIS auth (optional) |

---

## 7. Scripts summary

| Script | Purpose |
|--------|--------|
| `ODOOM/BUILD_ODOOM.sh` | Build ODOOM (Linux/macOS). Options: `run`, `nosprites`. |
| `ODOOM/RUN_ODOOM.sh` | Run ODOOM (builds if missing). |
| `OQuake/BUILD_OQUAKE.sh` | Build OQuake (Linux/macOS). Options: `run`, `batch`. |
| `OQuake/RUN_OQUAKE.sh` | Run OQuake (builds if missing). |
| `STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` | Build and deploy STAR API for current OS (Linux or macOS). Options: `-ForceBuild`, `-Runtime osx-arm64|osx-x64|linux-x64`. |

---

## 8. Troubleshooting

| Issue | What to do |
|-------|------------|
| **"UZDoom source not found"** | Set `UZDOOM_SRC` or clone UZDoom to `~/Source/UZDoom`. |
| **"libstar_api.dylib missing"** | Run `./STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` from OASIS Omniverse. |
| **"pwsh not found"** | Install PowerShell Core (`brew install powershell`) or patch UZDoom/vkQuake manually. |
| **UZDoom/vkQuake build fails** | Ensure CMake, Meson, Ninja, and Vulkan/MoltenVK are installed. On Apple Silicon, use the arm64 SDKs. |
| **OQuake can't find game data** | Set `OQUAKE_BASEDIR` to your Quake install (e.g. Steam path above). |
| **NativeAOT or .NET errors on M1/M2** | Use the latest .NET 8 SDK; the script should select **osx-arm64** automatically. |

For more detail, see [STARAPIClient/README.md](../STARAPIClient/README.md) and the main [README.md](../README.md).
