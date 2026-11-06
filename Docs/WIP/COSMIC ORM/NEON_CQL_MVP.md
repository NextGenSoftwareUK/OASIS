# Neon CQL MVP

The explicit Neon teaching source uses the existing PostgreSQL family
through the Npgsql client with VerifyFull TLS. Its native authority is
NeonOASIS and it accepts only bounded UUID catalogues. Activation verifies
one public record on an approved direct compute hostname. The regular
Neon storage adapter retains its pooler and proxy behavior.

All 15 provider tests passed without skips at Core
`69b81d68655e42ab090995cad541bc0703d58cf3`. The provider TRX SHA-256 is
`6ed1d1e7d209d0d69a03b7b5aef25f48e8b311d1e3a80059335d1eece1ace4d0`.
An approved Neon database with a dedicated SQL reader, native verification,
and a live visitor run are still required.
[Neon server identity verification](https://neon.com/blog/avoid-mitm-attacks-with-psql-postgres-16).

WEB4 reuses the PostgreSQL-family configuration and validates
the direct Neon hostname with the same provider predicate. The
protected settings are `CQL_NEON_QUERY_HOST`, `CQL_NEON_QUERY_READER_PASSWORD`
and `CQL_NEON_QUERY_PROBE_ID`, using `cosmic_reader` on `oasis`.
All 17 protected-settings checks passed. Eight Neon, nine CockroachDB and
eight TimescaleDB WEB4 admission tests passed without skips at Core
`2b1b991d070c3c90c1ade36c6b4ab4309d6e655a`. The admission TRX SHA-256 is
`2f2f356cf020ab93418ed20514e7778219b4c5b352357e5dccd7015cc5ed449b`.
