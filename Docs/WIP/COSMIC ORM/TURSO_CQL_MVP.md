# Turso CQL MVP

The Turso teaching profile reads public Holons by canonical ID through
the provider's existing Hrana HTTP pipeline. It sends one bound key in
each exact SQL read, checks the typed native projection and deletion state,
and validates payload identity and visibility before paging. Activation
checks a known public record. The profile admits a reviewed HTTPS Turso
database with a dedicated read token.

All ten provider cases passed with zero skips at Core
`9f2f07150cb39f81a75fac0837f38d31d510e724`. The TRX SHA-256 is
`c2007a19583576c8a3d9fe7c510d1803f1d16f9d39ff20ec1c459b1ae9f0362d`.
These tests use controlled HTTP replies.

The Linux qualification job passed against the verified libSQL server
0.24.32 release and a disposable loopback database. It proved real CQL
3/2/0 result counts and private-row denial with one native test passing
at `f1cee2ef802f0d50024091d463925a50bfd1239f`.
Native TRX SHA-256 is
`8ef47f85edfa889ea348eb137ec0d337400da2ca4a2a97dc25822cc90118667a`.
[Native qualification run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42226352790/job/127920731043).
[Hrana v2 protocol](https://github.com/tursodatabase/libsql/blob/main/docs/HTTP_V2_SPEC.md).

The opt-in WEB4 candidate binds `CQL_TURSO_QUERY_ORIGIN`,
`CQL_TURSO_QUERY_PROBE_ID` and a protected
`CQL_TURSO_QUERY_READER_TOKEN`. All 28 protected-settings tests passed.
Nine WEB4 admission checks and nine Convex regressions passed with zero
skips at Core `fb544f634f9daef20d028c76fec58eb8b9da21bf`. The admission
TRX SHA-256 is
`30d25a9237605c294a046270a65ea33f267afbd61c8e413602f68b796398a9e4`.
Managed Turso and live federation remain open. LocalFile remains first
for the live rollout.

The `TursoHostedCqlNative` integration test compiled at that Core pin.
Build log SHA-256 is
`2be0504a2ecb62856193bd894f97d8ee39fbc626fc8c8a80f92dda80cab8d3a3`.
Set `COSMIC_TURSO_HOSTED_CQL_SOURCE` to the reviewed descriptor with
`Origin`, `Actor`, `LiveIds`, `DeletedId` and `MissingId`. The test
uses the protected reader to check managed CQL 3/2/0 and descriptor
stability. Credentialed execution remains open.
