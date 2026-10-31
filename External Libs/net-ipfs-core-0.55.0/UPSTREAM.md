# Vendored IPFS core

Source: https://github.com/richardschneider/net-ipfs-core
Release: v0.55.0, commit07fbb4392cd57a8ba771d2e5c3279a15d5ffbeae.
The upstream tag and published NuGet0.55.0 repository metadata agree.
Original67src files plus MIT LICENSE were verified against upstream Git blob hashes.

OASIS changes build configuration and adapts the obsolete Base16 API calls:
net10.0 and netstandard2.0 targets; published0.55.0.0 assembly identity; private
embedding without automatic NuGet pack; shared runtime dependency definitions.
Unused upstream development-only generator/analyzer/SourceLink tools are omitted.
The original source tree contains no .proto inputs. Multibase hex encoding uses
the existing Ipfs.HexString codec; decoding materializes the modern span into the
existing byte-array contract. Encoding alphabets and CID identities remain unchanged.

.NET10 recompiles against SimpleBase5.6.4, matching the shipping SolSharp dependency.
The netstandard2.0 SDK target retains SimpleBase1.3.1 (5.6.4 requires.NET8+).
These are explicit build-target contracts, without runtime parser selection or
fallback. The shipping API must use the.NET10 core build. The old NuGet binary
expects a Base58.Bitcoin field absent from5.6.4; source recompilation binds the
actual property while preserving CID encodings and catalogue identities.

The provider embeds this core DLL and the existing HTTP SDK; runtime dependencies
come from RuntimeDependencies.props. Retain the upstream license in distributions.
Proving shipping CID/native/hosted tests and remaining legacy dependency gaps are
tracked under Docs/WIP/COSMIC ORM in the parent repository.

Executable and shipping-test composition imports ../IpfsSdk.AppReferences.props
through the root Directory.Build.targets. This makes the source project the actual
restore graph owner; PrivateAssets on a library alone does not propagate that
selection into its consumer. Patched FileVersion0.55.0.1 records the private SDK
revision; public AssemblyVersion0.55.0.0 is preserved.
