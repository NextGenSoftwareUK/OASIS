# STAR API C# Native Wrapper (C/C++ Compatible)

This project ports the C++ STAR API wrapper to C# while preserving the same C ABI entry points used by existing C and C++ game integrations.

## Binary Compatibility

- Export names match the original wrapper: `star_api_init`, `star_api_authenticate`, `star_api_has_item`, etc.
- Calling convention is `cdecl`.
- Struct layouts match `star_api.h`.
- `star_api.h` is included in this folder for direct use by existing game code.

## Performance Strategy

- Uses `UnmanagedCallersOnly` exports (no COM or reverse P/Invoke marshaling glue).
- Built as NativeAOT for direct native DLL loading and fast startup.
- Uses a shared `HttpClient` instance and minimal allocation interop conversions.

## Build (Native DLL + Import Library)

From repo root:

```bash
dotnet publish "OASIS Omniverse/STARAPIClient/STARAPIClient.csproj" \
  -c Release \
  -r win-x64 \
  -p:PublishAot=true \
  -p:SelfContained=true \
  -p:NoWarn=NU1605
```

Outputs:

- `OASIS Omniverse/STARAPIClient/bin/Release/net8.0/win-x64/publish/star_api.dll`
- `OASIS Omniverse/STARAPIClient/bin/Release/net8.0/win-x64/native/star_api.lib`

Drop `star_api.dll` in place of the existing native wrapper DLL and keep using the same `star_api.h`/import-library workflow.

## One-click publish + deploy

Use the helper script to publish and copy artifacts into the game integration folders:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/publish_and_deploy_star_api.ps1"
```

Optional smoke test compile/run (requires `cl.exe` or `gcc.exe`):

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/publish_and_deploy_star_api.ps1" -RunSmokeTest
```

## VS Build Tools assisted smoke test

If `cl.exe` is not on PATH, use the helper below. It discovers Visual Studio C++ tools via `vswhere`, loads `vcvars64.bat`, compiles the smoke test, and can run it:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/compile_smoke_test_with_msvc.ps1" -Run
```

