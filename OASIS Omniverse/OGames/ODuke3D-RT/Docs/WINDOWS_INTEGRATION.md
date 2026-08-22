# Windows Integration Guide for ODuke3D-RT (Duke-RT + STAR API)

This guide covers building ODuke3D-RT on Windows: Duke Nukem 3D with Vulkan ray tracing and full OASIS STAR cross-game integration.

## Credits and license

**ODuke3D-RT is based on [Duke-RT](https://github.com/fgsfdsfgs/duke-rt)** (GPL-2.0), a Vulkan ray-tracing modification of **EDuke32** (Jonathon Fowler, Richard Gobeille, contributors, GPL-2.0). Duke Nukem 3D game data is property of Gearbox Software / 3D Realms.

## Prerequisites

1. **Visual Studio 2019+** with C++ workload
2. **CMake 3.15+** in PATH
3. **Vulkan SDK** — https://vulkan.lunarg.com/sdk/home (required for Duke-RT ray tracing)
4. **ODuke3D-RT source** (Duke-RT fork) at `C:\Source\ODuke3D-RT`
5. **STAR API credentials**

Ensure your GPU supports **Vulkan ray tracing** (NVIDIA RTX or AMD RDNA 2+).

## Step 1: Build OGEngineClient

```powershell
cd C:\Source\OASIS-master
dotnet publish "OASIS Omniverse\OGEngineClient\OGEngineClient.csproj" `
    -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
```

## Step 2: Set Environment Variables

```batch
set STAR_USERNAME=your_oasis_username
set STAR_PASSWORD=your_oasis_password
```

## Step 3: Build

```batch
"OASIS Omniverse\ODuke3D-RT\BUILD_ODUKE3DRT.bat"
```

Output: `C:\Source\ODuke3D-RT\build-vs2019-win64\Release\eduke32.exe`.

Or manually:

```powershell
.\Scripts\COPY_TO_DUKERT_AND_BUILD.ps1 -DukeRTSrc "C:\Source\ODuke3D-RT"
```

## Step 4: Add Engine Hooks

See [INTEGRATION_INSTRUCTIONS.md](INTEGRATION_INSTRUCTIONS.md). Hook locations are identical to ODuke3D — only the function prefix changes from `ODuke3D_STAR_` to `ODuke3DRT_STAR_`.

## Step 5: Run

Place `duke3d.grp` (Duke Nukem 3D data) at `C:\Duke3D\duke3d.grp`, then:

```batch
RUN_ODUKE3DRT.bat
```

Or:

```batch
C:\Source\ODuke3D-RT\build-vs2019-win64\Release\eduke32.exe -j C:\Duke3D
```

Console should show:

```
[DUKE3D-RT] OASIS STAR API: Authenticated. Cross-game keys enabled.
[DUKE3D-RT] ODuke3D-RT 1.0.0 initialised.
```

## Vulkan / Ray Tracing Troubleshooting

- **Vulkan device not found** — Ensure the Vulkan SDK is installed and your GPU driver is up to date.
- **Ray tracing not available** — Duke-RT requires a GPU with `VK_KHR_ray_tracing_pipeline`. Fallback to ODuke3D (classic rendering) if your GPU does not support it.
- **Build fails on Vulkan headers** — Ensure `VULKAN_SDK` environment variable is set after installing the Vulkan SDK.

## OASIS Controls

| Key | Action |
|-----|--------|
| **I** | Toggle OASIS Inventory popup |
| **Q** | Toggle OASIS Quest popup |
| **Esc** | Close popup |
| **↑ / ↓** | Navigate items |
| **U** | Use selected item |
| **A** | Send to Avatar |
| **C** | Send to Clan |

**Credits:** ODuke3D-RT is based on [Duke-RT](https://github.com/fgsfdsfgs/duke-rt) and [EDuke32](https://eduke32.com) (GPL-2.0). Duke Nukem 3D © Gearbox Software / 3D Realms.
