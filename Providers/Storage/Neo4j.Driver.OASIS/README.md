# OASIS Neo4j driver connection-creation patch

Pinned upstream: Neo4j driver 4.3.2, commit
`5a40b591107331131875fbcd3d7f28fe60504d95`. See UPSTREAM.json for original file
hashes and the archive identity. LICENSE and NOTICE are retained.

The signed cached driver can close an established connection but misses a
connection negotiating its initial handshake. This source build registers that
transport before initialization and includes it in pool shutdown. Creation keeps
ownership of pool-size accounting. Socket close also marks an unopened client
closed so initialization cannot silently reopen that owner.

The public assembly identity must match the cached shipping signed driver:
`Neo4j.Driver, Version=4.3.2.2, Culture=neutral, PublicKeyToken=b646bc66d277ac07`.
`CompatibilityPublicKey.snk` contains only its public key; provenance is recorded
in `COMPATIBILITY_IDENTITY.json`. The original upstream `IdentityPubKey.snk` is
preserved for provenance but has a different identity and is not used to sign.
No private signing key is included. Informational/file versions identify this as
an OASIS patch, not an official upstream release. No new NuGet dependency is
required on net10.0. The assembly is built from source, never patched in place.

The ten cold/warm native relay controls pass against the real server, including
caller-only cancellation before held response release. The consumer driver hash
is verified by the reusable runner. All four CQL query shapes are covered;
the full 259 native CQL and 465 provider unit tests pass without skips. Further
acceptance requires other connection phases, deadlines and non-CQL regressions.

The follow-on connection-creation patch passes owner cancellation through DNS,
cache waits, native TCP connect and TLS authentication. Shutdown and resource
publication share one gate, preventing a cancelled owner from opening a later
socket. Driver-specific tests use the existing upstream friend identity and the
shipping public key. Six controls pass, including held DNS, cache cancellation,
closed-owner rejection and a real TLS ClientHello held at loopback port17789.
Full native acceptance of this follow-on patch is tracked separately in the
COSMIC ORM descendant contract; earlier counts above retain their original scope.

Run the driver tests with the existing .NET10 SDK and cached dependencies:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner 'neo-driver-tests' -Operation {
    dotnet test Providers/Storage/TestProjects/Neo4j.Driver.OASIS.Tests/Neo4j.Driver.OASIS.Tests.csproj `
        --no-restore /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'Driver tests failed.' }
}
```

Repository producer/consumer builds must use the shared build lease and bounded
workers. The reusable Neo4j runner verifies the exact assembly identity and
producer/consumer hashes before starting any native fixture. Do not copy DLLs
manually or regenerate provenance hashes without reviewing every source change.
