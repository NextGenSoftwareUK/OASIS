# OASIS Edge and game-client distributions

These are four distinct product families. Providers are modular NuGet components, bundled only where the selected product needs them; they do not each require a separate GitHub product release.

| Product | GitHub tag family | Installation payload | NuGet role |
|---|---|---|---|
| Edge Runtime — standalone | `OASIS-Edge-Runtime-vX.Y.Z` | Compiled runtime/ONET/native endpoint libraries and RID-native SQLite; no Unity integration | `NextGenSoftware.OASIS.Edge.Runtime`, `NextGenSoftware.OASIS.Edge.ONET.Runtime` and provider/dependency packages |
| OGEngineClient — RemoteOnly | `OGEngineClient-RemoteOnly-vX.Y.Z` | Managed hosted API client and NativeAOT game library; embedded Edge disabled explicitly | Existing general client package is not automatically proof of a RemoteOnly-specific binary profile |
| OGEngineClient + Edge | `OGEngineClient-Edge-vX.Y.Z` | Managed/native game client plus local/offline Edge dependency closure | Client and reusable Edge libraries; exact profile is explicit in GitHub artifacts |
| OGEngineClient + Edge + Unity | `OASIS-Edge-Unity-vX.Y.Z` | Unity integration scripts, `.meta` importer settings, managed/native plugins and editor integration | Distinct `NextGenSoftware.OASIS.Edge.Unity` wrapper; includes the UPM tarball under tools/unity |

## Choosing a package

For a non-Unity .NET application, choose standalone Edge and use its [Runtime guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime/README.md) or [ONET guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime/README.md). Standalone archives require a .NET 8-compatible host; they are libraries, not applications.

For game engines integrating through `ogengine.h`, choose the appropriate OGEngineClient profile. `Managed` contains .NET 10 client assemblies and dependencies; `Native` contains a platform-specific NativeAOT shared library and matching header. RemoteOnly uses hosted services and excludes the Edge client assembly. Edge enables local/offline composition; authentication, persistent device/avatar scope and host lifecycle integration remain required.

For Unity, choose the separate Unity distribution. Its NuGet wrapper is a delivery channel for the UPM tarball, not automatic Unity Package Manager registration. Import the tarball in Unity Package Manager, or install the expanded Unity package according to its README. Preserve `.meta` platform import settings. The Unity product includes the OGEngineClient Edge integration, not just the standalone runtime.

HoloOASIS.Edge and EdgeSQLiteOASIS stay individually consumable provider packages. Holochain-enabled integration requires its documented conductor/hApp/platform setup; do not assume every standalone archive includes a conductor.

## Platform and verification contract

Standalone and both OGEngineClient profiles require all five supported archives before GitHub publication: Windows x64, Linux x64/ARM64, macOS x64/ARM64. Linux ARM64 NativeAOT builds use a native ARM64 runner. Unity has its own platform-plugin/import model rather than five interchangeable standalone archives.

Compilation, archive presence and publication are separate from physical gameplay/mobile/Unity IL2CPP acceptance. Consult actual evidence and known limitations; never equate a framework target with verified platform behavior. All libraries/providers still require their appropriate native services, credentials and configuration. Keep secrets out of public templates and logs.

## Historical release clarification

OGEngineClient 2.0.2 was Edge-enabled: its exact submodule source defaulted `OGEngineIncludeEdge=true`, and the parent release workflow did not override it. It published one managed archive with `PublishAot=false`, not an explicitly labelled RemoteOnly product or a complete five-platform NativeAOT release.

Edge Runtime 1.1.0 was originally published as Unity-shaped payload. It is a legacy transitional release, not proof that standalone DLL archives have shipped. New standalone 1.2.0 and distinct Unity 1.1.0 runs are recorded as pending until verified live.

## Related releases and operations

[Release matrix with actual version links](<../Release and Status/OASIS_RELEASE_STATUS.md>) · [Workflow operations and recovery guide](<../Release and Status/RELEASE_WORKFLOW_OPERATIONS.md>) · [Standalone installation](../../Releases/EDGE_STANDALONE_INSTALL.md) · [Unity NuGet README](../../../UnityPackages/NextGenSoftware.OASIS.Edge.Unity.NuGet/README.md)

For the explicit cross-platform OGEngine profile path, run **Release — OGEngineClient**, choose Publish, Manual and an unused stable `manual_version`. This dispatches both profile matrices. Its empty-manual-version legacy/global path still awaits integration with the new split products; use the explicit path until the guide records that repair as verified.
