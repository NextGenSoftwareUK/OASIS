# Quick Start – ODOOM & OQuake STAR API

Short path to building and running ODOOM and OQuake with the STAR API. For full setup (repos, tools, config), use **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)**.

## 1. Prerequisites

- **OASIS** repo at e.g. `C:\Source\OASIS-master`
- **UZDoom** at e.g. `C:\Source\UZDoom` (for ODOOM)
- **vkQuake** at e.g. `C:\Source\vkQuake` (for OQuake)
- **quake-rerelease-qc** at e.g. `C:\Source\quake-rerelease-qc` (for OQuake)
- Visual Studio with C++ tools, CMake, Python 3, Vulkan SDK (OQuake), .NET SDK (APIs/STARAPIClient)

See [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) for install details.

## 2. Credentials

**PowerShell:**

```powershell
$env:STAR_USERNAME = "your_username"
$env:STAR_PASSWORD = "your_password"
```

Or API key:

```powershell
$env:STAR_API_KEY = "your_api_key"
$env:STAR_AVATAR_ID = "your_avatar_id"
```

## 3. Local APIs (optional)

From OASIS repo root:

```batch
Scripts\start_web4_and_web5_apis.bat
```

Use `oasisstar.json` in each game’s **build** folder with `http://localhost:5555` (WEB4) and `http://localhost:5556` (WEB5). For live APIs, set URLs to `https://oasisweb4.one/api` and `https://oasisweb4.one/star/api` (see [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)).

## 4. Build ODOOM

From `OASIS Omniverse\ODOOM\`:

```batch
BUILD ODOOM.bat
```

Run: `build\ODOOM.exe` (put your WAD in `build\` or configure path). Set **UZDOOM_SRC** in the script if UZDoom is not at `C:\Source\UZDoom`.

## 5. Build OQuake

From OASIS repo root, in **Developer Command Prompt for VS**:

```batch
"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"
```

Set **VKQUAKE_SRC** (and **OQUAKE_BASEDIR** / **QUAKE_ENGINE_EXE** if needed). Run: `OASIS Omniverse\OQuake\build\OQUAKE.exe` with Quake data (e.g. id1, pak files). See [OQuake/WINDOWS_INTEGRATION.md](OQuake/WINDOWS_INTEGRATION.md).

## 6. Test

- **ODOOM**: Pick up a keycard; check console for STAR API messages. Inventory overlay: `I`.
- **OQuake**: Pick up keys/ammo; open inventory with `I`. After quit/reload, ammo totals should match what you collected (e.g. shells × pickups).

## Checklist

- [ ] Repos cloned (OASIS-master, UZDoom, vkQuake, quake-rerelease-qc)
- [ ] Tools installed (VS C++, CMake, Python 3, Vulkan SDK, .NET SDK)
- [ ] Credentials set (`STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID`)
- [ ] ODOOM built and run
- [ ] OQuake built and run
- [ ] `oasisstar.json` in each game’s `build\` folder (local or live URLs)
- [ ] Console shows Beamin (SSO) or API key auth success; inventory (`I`) syncs

## Next steps

- Full setup and config: [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)
- Integration details: [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)
- ODOOM: [ODOOM/README.md](ODOOM/README.md) | OQuake: [OQuake/README.md](OQuake/README.md)
