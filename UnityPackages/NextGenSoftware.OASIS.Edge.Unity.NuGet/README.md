# OGEngineClient + Edge + Unity

This is the **OGEngineClient + Edge + Unity** distribution, not just an Edge Runtime adapter. It bundles the compiled Edge-enabled game client (`NextGenSoftware.OGEngine.Client.Edge.dll`), its shared client dependencies, OASIS Edge Runtime/ONET libraries, and Unity integration scripts and platform plugins. You do not need to install a separate OGEngineClient GitHub ZIP alongside it.

The NuGet ID remains `NextGenSoftware.OASIS.Edge.Unity`; the GitHub release family is `OASIS-Edge-Unity-vX.Y.Z`. These names identify the complete Unity distribution. For non-Unity applications, choose standalone Edge or the RemoteOnly/Edge-enabled OGEngineClient distributions instead.

## Install into Unity

Install/download this NuGet package and locate `tools/unity/com.nextgensoftware.oasis.edge.tgz`. In Unity Package Manager, choose **Add package from tarball** and select that file. NuGet installation alone does not register a package with Unity Package Manager.

The tarball contains compiled managed/native plugins and Unity C# integration scripts, `.meta` import settings, package manifest and lifecycle integration. Unity compiles its integration scripts; `.meta` files control stable asset identities and platform import settings. Do not remove import metadata or mix these plugin assemblies with another incompatible Edge package graph.

`HoloOASIS.Unity` is a separate provider adapter, not a substitute for this complete Unity integration distribution. For non-Unity .NET applications, use the standalone runtime NuGet libraries and standalone Windows/Linux/macOS GitHub archives instead.

## Configuration and security

Use the host's authenticated avatar, stable device identity, persistent storage path and supported transport/platform integrations. Secure offline grants require protected platform storage and pinned host validation keys. Never ship production DNA, private keys or tokens in a public client. Inspect actual error codes and synchronization status instead of treating offline local writes as confirmed hosted execution.

## Compatibility and verification

Use the platform integration and Unity version supported by the included package documentation. Native plugins must match the target architecture. Physical Android/iOS execution, two-device convergence and Unity IL2CPP behavior require separate acceptance; package publication is not proof of those tests. Component licenses continue to apply to their respective binaries/source; the wrapper does not relicense dependencies.

[Release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/WIP/Release and Status/OASIS_RELEASE_STATUS.md) · [Detailed Edge guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime/README.md)
