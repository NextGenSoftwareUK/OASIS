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

Connection-owner cancellation now propagates through DNS, cache waits, native
TCP connect and TLS authentication. Shutdown and resource publication share one
gate, preventing a cancelled owner from opening a later socket. TCP shutdown
precedes disposal, preserving the original transport teardown behavior.

The committed patch passes267 native CQL tests, including 18 cold/warm caller
cancellation and actual 30-second deadline cases across all four query shapes.
Separate checkpoints pass 6 driver controls and 465 provider unit tests, all without
skips. The driver tests cover held DNS, cache cancellation, closed-owner rejection,
a real TLS ClientHello and the shipping assembly identity. Fresh hosted-demo and
non-CQL regression acceptance remain separate gates. Exact evidence is retained
in the COSMIC ORM descendant contract.

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
