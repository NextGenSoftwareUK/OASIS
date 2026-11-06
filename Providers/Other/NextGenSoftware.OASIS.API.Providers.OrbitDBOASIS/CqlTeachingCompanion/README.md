# OrbitDB CQL teaching companion

This Node service opens one reviewed local OrbitDB address with
`KeyValueIndexed`. The manifest must identify the public teaching store.
Run it in the same network namespace as the CQL host with a persistent
database directory and a protected `OASIS_ORBITDB_CQL_READER_TOKEN`.

`node serve-teaching.mjs <directory> <address> <port>` binds only to
`127.0.0.1`. The sole read route is
`GET /oasis/cql/holon?id=<canonical-guid>` with the bearer reader token.
It returns public live payloads without embedded children, 403 for
private records and JSON null for deleted or missing records.

Use Node 22 or later and the pinned npm lock. Install dependencies with
`npm ci --ignore-scripts` in one dedicated workspace. On Windows, choose
a short fixture path because LevelDB uses nested files beneath the
OrbitDB address. The native probe is
`node verify-teaching.mjs <empty-fixture-path>`. It starts and stops its
own process on port 54869 and checks persisted data across restarts.
With an already built test project and a results directory as extra
arguments, it also runs the exact CQL integration test.

This scoped service opens one indexed local store. Multi-peer recovery,
general document-store operations and hosted enrollment require separate
qualification.

[OrbitDB database types](https://github.com/orbitdb/orbitdb/blob/main/docs/DATABASES.md).
