# Getting Started — Windows

This guide walks you through building and running **ODOOM**, **OQuake**, and **STARAPIClient** on **Windows**.

---

## 1. Prerequisites

Install the following (if not already installed):

| Tool | Purpose | Where to get it |
|------|----------|------------------|
| **Visual Studio 2022** | Build UZDoom, vkQuake; C++ workload | [Visual Studio](https://visualstudio.microsoft.com/) — include "Desktop development with C++" |
| **.NET 8 SDK** | Build STARAPIClient (NativeAOT) | [.NET Downloads](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **CMake** | Configure UZDoom | [cmake.org](https://cmake.org/download/) or via Visual Studio |
| **PowerShell** | Run patch and deploy scripts | Built-in (Windows PowerShell 5.1 or PowerShell Core 7+) |
| **Python 3** | ODOOM face pk3, optional sprite scripts | [python.org](https://www.python.org/downloads/) |
| **Vulkan SDK** | Build vkQuake | [Vulkan SDK](https://vulkan.lunarg.com/sdk/home) |
| **Git** | Clone repositories | [git-scm.com](https://git-scm.com/) |

Optional for ODOOM sprites/editor:

- **Ultimate Doom Builder** (for sprite generation and map editing)
- **Quake** (Steam or GOG) — for PAK0/PAK1 when generating OQuake sprites in ODOOM

---

## 2. Clone repositories

Use a single parent folder (e.g. `C:\Source\`). The batch files expect these paths by default; you can edit the scripts to use your own.

```cmd
mkdir C:\Source
cd C:\Source

git clone <OASIS-repo-url> OASIS-master
git clone https://github.com/coelckers/UZDoom.git UZDoom
git clone https://github.com/Novum/vkQuake.git vkQuake
git clone <quake-rerelease-qc-repo-url> quake-rerelease-qc
```

Replace `<OASIS-repo-url>` and `<quake-rerelease-qc-repo-url>` with your actual repo URLs.

---

## 3. Build STARAPIClient (shared library for ODOOM & OQuake)

STARAPIClient is built and deployed automatically when you build ODOOM or OQuake. To build or deploy it alone:

1. Open **Command Prompt** or **PowerShell**.
2. Go to the OASIS Omniverse folder:

   ```cmd
   cd C:\Source\OASIS-master\OASIS Omniverse
   ```

3. Run:

   ```cmd
   BUILD_AND_DEPLOY_STAR_CLIENT.bat
   ```

   This publishes the STAR API for **win-x64** and copies `star_api.dll`, `star_api.lib`, and `star_api.h` into the ODOOM and OQuake folders.

   To force a full rebuild:

   ```cmd
   powershell -ExecutionPolicy Bypass -File "STARAPIClient\Scripts\publish_and_deploy_star_api.ps1" -ForceBuild
   ```

---

## 4. Build ODOOM

1. Open **Developer Command Prompt for VS 2022** (or **x64 Native Tools Command Prompt**).
2. Go to ODOOM:

   ```cmd
   cd C:\Source\OASIS-master\OASIS Omniverse\ODOOM
   ```

3. Run:

   ```cmd
   "BUILD ODOOM.bat"
   ```

   When prompted:
   - **Full clean/rebuild [C] or incremental [I]?** — Choose **I** for a normal build (or **C** for a clean rebuild).
   - **Regenerate sprites/icons [Y/N]?** — Choose **Y** if you have Ultimate Doom Builder and Quake PAKs and want OQuake keys/monsters in Doom; otherwise **N** for a faster build.

4. When the build finishes, the game is at:

   `ODOOM\build\ODOOM.exe`

5. Put **doom2.wad** in the `ODOOM\build` folder (or same folder as ODOOM.exe).

**Run ODOOM:**

```cmd
"RUN ODOOM.bat"
```

Or double‑click `RUN ODOOM.bat`. It will build first if needed, then launch.

**Options:**

- `BUILD ODOOM.bat run` — Incremental build and launch (no prompts).
- `BUILD ODOOM.bat nosprites` — Skip sprite/icon regeneration.

---

## 5. Build OQuake

1. Ensure **Vulkan SDK** is installed and that you have **Quake game data** (e.g. Steam: `C:\Program Files (x86)\Steam\steamapps\common\Quake` with `id1\pak0.pak`, `pak1.pak`).
2. Open **Developer Command Prompt for VS 2022**.
3. Go to OQuake:

   ```cmd
   cd C:\Source\OASIS-master\OASIS Omniverse\OQuake
   ```

4. Run:

   ```cmd
   BUILD_OQUAKE.bat
   ```

   Choose **I** for incremental (or **C** for full clean) when asked.

5. Output is at:

   `OQuake\build\OQUAKE.exe`

**Run OQuake:**

```cmd
RUN OQUAKE.bat
```

Or:

```cmd
BUILD_OQUAKE.bat run
```

The batch file uses the default Quake path; edit the script if your Quake install is elsewhere.

---

## 6. Environment variables (optional)

For OASIS auth (cross-game login):

- **STAR_USERNAME** and **STAR_PASSWORD**, or  
- **STAR_API_KEY** and **STAR_AVATAR_ID**

Set them in the environment or in your config (e.g. `oasisstar.json`).

---

## 7. Custom paths

If your clones are not under `C:\Source\`:

- **ODOOM:** Edit `ODOOM\BUILD ODOOM.bat` and set `UZDOOM_SRC`, and optionally `QUAKE_PAK0` / `QUAKE_PAK1` for sprite generation.
- **OQuake:** Edit `OQuake\BUILD_OQUAKE.bat` and set `QUAKE_SRC`, `VKQUAKE_SRC`, and the Quake install path if needed.

---

## 8. Build everything (no prompts)

From `OASIS Omniverse`:

```cmd
BUILD EVERYTHING.bat
```

This builds and deploys STARAPIClient, then builds ODOOM and OQuake with no prompts and does not launch. Use `RUN ODOOM.bat` and `RUN OQUAKE.bat` to run the games.

---

## 9. Troubleshooting

| Issue | What to do |
|-------|------------|
| **"UZDoom source not found"** | Set `UZDOOM_SRC` in `BUILD ODOOM.bat` to your UZDoom clone path. |
| **"star_api.dll not found"** | Run `BUILD_AND_DEPLOY_STAR_CLIENT.bat` from `OASIS Omniverse` first. |
| **"Vulkan SDK not found"** | Install Vulkan SDK and restart the command prompt. |
| **CMake or MSBuild errors** | Use **Developer Command Prompt for VS 2022** (or x64 Native Tools). |
| **Missing doom2.wad** | Copy `doom2.wad` into `ODOOM\build\`. |
| **OQuake can't find game data** | Edit `RUN OQUAKE.bat` / `BUILD_OQUAKE.bat` and set the Quake install path. |

For more detail, see [DEVELOPER_ONBOARDING.md](../DEVELOPER_ONBOARDING.md), [ODOOM/WINDOWS_INTEGRATION.md](../ODOOM/WINDOWS_INTEGRATION.md), and [STARAPIClient/README.md](../STARAPIClient/README.md).
