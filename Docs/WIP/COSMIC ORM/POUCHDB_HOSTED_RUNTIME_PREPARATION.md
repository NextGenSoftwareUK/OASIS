# PouchDB persistent WEB4 source preparation

5 November 2026. Native source/runtime module verified on Windows; Docker,
shipping CQL enrollment and hosted/browser acceptance remain open. Counts stay
103 local/60 hosted HTTP/28 hosted browser. First LocalFile rollout remains the
priority; its existing Railway session still returns Unauthorized.

## Purpose and source contract

Prepare the real PouchDB teaching source for the existing WEB4 Node runtime and
persistent volume. No new service, account or paid resource is provisioned.
Deployment packaging/flags are not wired yet. The module uses PouchDB9.0.0,
express-pouchdb4.2.0, express4.21.2 and its pinned allDbs1.1.1 SDK plugin. Native
LevelDB stores are used; vendor middleware serves document data.

The bundle admits exactly three public synthetic root projections, with one
explicit actor, fixed oasis_holons database, unique GUIDs and verified base64/
SHA256 transfers. The projection has Id, CreatedByAvatarId, Name, Description,
IsPublic=true, string MetaData and empty Children. This deliberately small demo
dataset is not full provider/ORM serialization parity. Case-folded or escaped
duplicate fields, unknown fields, private records, foreign owners/IDs, revisions,
child graphs and invalid transfer encoding are rejected before native I/O.

One native SDK create per record is preceded by a durable intent and followed by
its first-generation revision/hash acknowledgement. The source directory cannot
be adopted without its matching global acknowledgement. Changed configuration,
missing native CURRENT/manifest, changed records/revisions or extra documents
fail; they are never reset, overwritten, repaired or reseeded automatically.
SDK bookkeeping may update native registry/log files; canonical records remain
bound to the original receipts. Proof covers normal process reopen, not power
loss, crash durability or distributed snapshots.

HTTP listens on loopback and admits only database-info/document GETs, including
the configured conflicts option. Writes, scans and other routes reject before
vendor dispatch. Responses retain vendor native rows/errors. Source digest,
per-listener instance and SDK-version headers support owned readiness checks.
Every native SDK store, including registry metadata, is tracked and closed.

## Failures preserved and fixed at their owners

Initial bootstrap acknowledged all three canonical writes but database discovery
returned vendor404/no_db_file. The allDbs plugin had been installed after the
source opened, missing its creation event. Install the supported vendor plugin
before opening the source, for both initial and retained paths.

After that correction, HTTP reads passed but close failed: bootstrap and HTTP
held independent wrappers over one native LevelDB store. Both now obtain the
single vendor getOrCreateDB cached SDK handle. No manual registry row, duplicate
record write, alternate read path or ignored close error was added.

Original failed phases are retained. All continuation used the original three
records and acknowledgements; initialization was not replayed.

## Evidence in the existing recovery environment

Node18.18.0 and the already qualified SDK cache were reused; no installation or
download. Candidate is separate from the accepted local source:
`LocalState/COSMIC/RecoveryLaptopPouchDBCql/hosted-preparation/`.

- Nineteen pure bundle guards pass, zero failures/skips; `unit-guards.log`.
- Initial `initial-d3d2e3c08e6c4f31b434082527f8264c` failure preserved.
- Read-only diagnostic `retained-b10f9cc7dd2d4899b75e1dce37db124e` captures the
  vendor database discovery response; both SDK stores closed.
- HTTP-success/close-failure `retained-1ed4bb8ab9b04b3c8fb2c6a63e38834d` retained.
- Corrected `retained-ec1dcf9fa503427599b112834bf59e40` passes: three native rows,
  matching original revisions/hashes, strict missing404, two denied writes, two
  denied unbounded routes and unchanged source. Zero record writes, two native
  stores closed. ACK SHA256
  `d81fd7411ad756c67d198e2ae798db67ac9260db8cfcbd890ac19fe93ce12b26`.
- Separate process restart `retained-370e71d5e65946dfada28cd783746a2f` also passes
  those checks plus three metadata-only source admission guards: unacknowledged
  directory, wrong digest and missing native store. No native database is created
  by those negative fixtures. Zero record writes, two native stores closed.
  ACK SHA256 `353262d7ff506ee8ef732b188cf6590ccdf944b9a39c6c96d00a7f516782aa2a`.
- Owned PIDs8676/1876 absent; loopback listeners63346/63379 absent independently.
  `native-source-qualified.json` records that audit. Retain all source and phases.
- Bundle SHA256 `6b53d414576f80c67b588151c33c1b65cfbc13575e5a07f7569eac56fbbca385`;
  original global source ACK SHA256
  `7aeab8c3cb8f9356f1bb6b787917e63e6c5920bed8c7d0dd5075e0456c47779c`.

Current repaired code has retained/restart proof on the original source. Complete
fresh-container startup remains a separate required gate; do not relabel the
failed initial HTTP run as success.

## Next implementation

1. Connect the owned source to the existing process supervisor with explicit
   sandbox-only configuration, listener ownership/readiness and graceful shutdown.
2. Package the pinned SDK in the WEB4 image and verify its actual Linux native
   adapter/fresh bootstrap. Reuse the existing image build workflow and resources.
3. Add explicit query-only Core tariff and WEB4 read-profile/four-preset admission;
   advance the parent gitlink and manifest together. Verify native CQL3/2/paging,
   manager/Mongo/HTTP behavior and the exact dependency/image.
4. After the first LocalFile mount/image gate is resolved, review PouchDB source
   placement/settings separately, then readiness and live native/federation/
   privacy/replica/browser acceptance. Local runtime proof is not hosted coverage.

[Native module](../../../Docker/host-sandbox-pouchdb.cjs) ·
[Explicit source qualifier](../../../Scripts/TestHosts/Qualify-HostedPouchDbSource.cjs) ·
[Existing accepted local source](POUCHDB_CQL_MVP.md).
