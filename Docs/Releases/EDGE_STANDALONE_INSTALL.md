# OASIS Edge Runtime — standalone compiled libraries

This distribution is for .NET applications, not Unity projects. Select the ZIP matching your OS/architecture: Windows x64, Linux x64/ARM64 or macOS x64/ARM64.

The archive contains compiled Edge Runtime, Edge ONET Runtime and Native Integrated Endpoint Edge assemblies, their managed dependencies and RID-selected native SQLite assets. It does not contain Unity C# scripts, Unity `.meta` files, test reports, logs or NuGet archives.

## Installation and use

Use a .NET 8-compatible host application. This is a library distribution, not a standalone executable or a self-contained .NET installation. Keep the dependency and native-file layout intact. Prefer the standalone NuGet packages for automatic dependency resolution:

```sh
dotnet add package NextGenSoftware.OASIS.Edge.Runtime
dotnet add package NextGenSoftware.OASIS.Edge.ONET.Runtime
```

Provide a stable device ID, authenticated avatar ID, writable persistent SQLite path, authenticated transport and host lifecycle integration. See the [Edge Runtime guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime/README.md) and [Edge ONET guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime/README.md).

Check `OASISResult<T>` error codes/messages, pending synchronization and conflict status. Local persistence is not proof of remote convergence. Configure secure offline-session storage together with a pinned host grant validator. Do not embed secrets in public DNA or log tokens/private keys.

## Separate Unity distribution

Unity integration is released under `OASIS-Edge-Unity-vX.Y.Z`, with Unity scripts, import metadata and plugins. `NextGenSoftware.OASIS.Edge.Unity` is its distinct NuGet wrapper; `HoloOASIS.Unity` remains a provider adapter and is not the whole integration product.

Platform archive compilation/packaging checks are not proof of every external service, physical Android/iOS execution, Unity IL2CPP or two-device convergence. See the dated [release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) for actual verification and outstanding work.
