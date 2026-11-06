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

The Linux qualification job uses the verified libSQL server 0.24.32
release and a disposable loopback database. It checks the real
3/2/0 result counts and private-row denial. The job is scoped to the
task branch and does not deploy or touch an existing database.
[Hrana v2 protocol](https://github.com/tursodatabase/libsql/blob/main/docs/HTTP_V2_SPEC.md).

Managed Turso credentials, WEB4 admission and live federation remain
open. LocalFile remains first for the live rollout.
