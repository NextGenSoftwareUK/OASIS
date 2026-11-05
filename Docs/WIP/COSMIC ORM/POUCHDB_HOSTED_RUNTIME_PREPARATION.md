# PouchDB persistent WEB4 source preparation

5 November 2026. Native source/runtime and supervision verified on Windows;
Docker packaging and Linux SDK qualification pass. Shipping CQL
enrollment and hosted/browser acceptance remain open. Counts stay
103 local/60 hosted HTTP/28 hosted browser. First LocalFile rollout remains the
priority; its existing Railway session still returns Unauthorized.

## Purpose and source contract

Prepare the real PouchDB teaching source for the existing WEB4 Node runtime and
persistent volume. No new service, account or paid resource is provisioned.
Deployment packaging/flags are opt-in and not enabled. The module uses PouchDB9.0.0,
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
WEB4 startup remains a separate required gate; do not relabel the
failed initial HTTP run as success.

## Next implementation

1. Add explicit query-only Core tariff and WEB4 read-profile/four-preset admission;
   advance the parent gitlink and manifest together. Process supervisor, packaging
   and Linux SDK qualification already pass below.
2. Verify native CQL3/2/paging and manager/Mongo/HTTP against the owned source,
   then the full WEB4 final-stage load and exact dependency/image.
3. After the first LocalFile mount/image gate is resolved, review PouchDB source
   placement/settings separately, then readiness and live native/federation/
   privacy/replica/browser acceptance. Local runtime proof is not hosted coverage.

[Native module](../../../Docker/host-sandbox-pouchdb.cjs) ·
[Explicit source qualifier](../../../Scripts/TestHosts/Qualify-HostedPouchDbSource.cjs) ·
[Existing accepted local source](POUCHDB_CQL_MVP.md).

## Process supervision and packaging preparation

The existing native supervisor now supports explicit `OASIS_SANDBOX_POUCHDB=true`
and `OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON`. It generates a per-child instance ID;
readiness verifies source digest, instance, SDK9, database, count3 and LevelDB.
Only bounded startup connection failures may be polled, not HTTP/authentication,
identity, malformed data or user queries. WEB4 starts only after native readiness;
unexpected source exit stops the API. The default flag remains false.

The child requires the authoritative sandbox DNA flag and complete listener
ownership before any native initialization. Its fixed production location is
`/data/COSMIC/SandboxPouchDB`; `/data` must already exist as an actual directory,
and a symlinked data root/parent rejects. This check is not mount qualification;
the existing operator volume/mount gate still applies. Parent creation does not
adopt an unacknowledged source. Shutdown observes the HTTP listener and vendor
daemon, verifies source, then closes native handles even if validation fails.

36 scoped bundle/readiness/supervisor/package/CLI controls pass (zero skips).
Five selected shared-supervisor/DNA regressions pass; five unrelated cases were
outside that filter and were not rerun. Entrypoint shell syntax passes using
existing Git sh. Logs: `TestResults/ProviderBuildCurrent/pouchdb-hosted-supervision-*`
and `pouchdb-shared-supervisor-regression.log`.

Retained real-SDK supervision-e9403cc3686644a1951a4229ed979a13 passes ready/foreign
instance refusal and graceful shutdown, with two native stores closed. It uses an
in-process simulated SIGTERM on Windows, not proof of Linux OS signal delivery.
ACK SHA256 `c6186f2e588dab5ac0e0c341a911f08691b93db0c80ecf318ef551f57adbbde0`.
Separate supervision-55e674da43e44ec39193e09871651cf5 injects a wrong receipt in
memory only; shutdown reports exactly that validation error and still closes all
native handles. Disk/source receipts remain unchanged. ACK SHA256
`ca03f9041146d548b570d6996b8ba3c9fdd85501befbea8ecd43019138c36368`.
Both processes are absent;13800 is closed. No record writes or new datastore.

The copied Windows lock had no registry archive integrity fields. Initial package
control failed34/35, retained in the original controls log. A lock-only npm
attempt did not fill those fields. The dedicated metadata capture fetched244
official exact-version responses (666309bytes), no archives/installations.
All247 package paths/versions remain unchanged; every entry now has an official
HTTPS archive URL and integrity. One extraneous parent-package metadata entry
was removed; allDbs1.1.1 is explicit. Source lock SHA256
`66127b293295e7b62286d9e5e583e0da602f409637e35acd5ec9685d89481912`.
Metadata request/response receipts and registry-pins.ack.json stay in the existing
candidate cache. No second local node_modules/runtime was installed.

WEB4's `provider_pouch` Docker target reuses its existing pinned official Node
image, installs that exact SDK graph, runs36controls and the real Linux source
probe. The probe refuses an existing /data, claims an owned disposable build
directory, runs the actual production child CLI twice, checks native records and
unchanged receipts, delivers real SIGTERM, awaits child/stdout closure and native
close ACKs, then removes only its verified owned directory. It publishes no image
or deployment. Source SDK CI is path-filtered on this task branch, with reusable
GHA cache. The later full WEB4 image reuses that cache and checks final-stage
native module loading; CQL enrollment must be included before that full build.
### Actual Linux qualification

[Run42094454362](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42094454362)
and job127480746394 completed SUCCESS at exact source
`bd5fed9beb8e8f421787bde7ca0f4fa4354cefe8`. Logs independently show36tests,
36passes,0failures,0skips. Actual probe receipt:

- Nodev24.19.0 / PouchDB9.0.0 / native LevelDB.
- Three fresh records and three retained records after a separate process restart.
- Original revisions unchanged; both actual native children exited through real
  Linux SIGTERM with two native stores closed each, no escalation.
- Verified owned disposable build-stage /data removed after children/stdio closed.

This closes the SDK-stage fresh startup/restart/OS-signal gate. It does not prove
the full WEB4 final-stage dependency load, CQL enrollment, persistent Development
mount or deployed visitor/browser behavior. No image was published or deployed.
Compact log excerpts/run identity are retained in `hosted-preparation/linux-sdk-ci.json`,
SHA256 `627e32e115611b5d22423b6dc6948a675752a07583a27dbc7902f5c24b88a93e`.
Full logs remain on the exact GitHub job; no image layers were downloaded locally.

### Retained source through the .NET CQL adapter

The actual PouchDB adapter used WEB4's shipping HTTP source guard against the
same retained LevelDB database. The guard requires the exact source digest and
PouchDB SDK version on every response. The test returned 3 full rows, 2 filtered,
1 paged and 0 for an unadmitted ID. An independent exact-ID request returned a
native 404. The original payload hashes, record revisions and source receipt
were unchanged before and after; both native stores closed.

Local phase `hosted-cql-e9c6add29b464e999cbedc7c19728711`: 1 pass, zero
skips, no writes; TRX SHA-256
`2ff905aefa1fed225ed2d4a91fcc50e96aa1e27444ba50829a5160d98c82cb2e`.
The first attempt's failed expression construction is retained separately.
WEB4 registration, source response guards and admission for all four presets
passed 23 focused tests with zero skips at Core
`c8f8278dd885cf76298fac1c2ed4a3843d2a2ac1`. The exact API TRX SHA-256 is
`e89bc2e3dd8bce4ce46a4a72474c256d0369914e846c04df0cd7ee15c2755601`.
Full image and deployed verification remain open.

The paired check should use the same guarded retained source and the existing
recovery Mongo replica set. Reuse the established manager and HTTP assertions,
with a separately owned disposable Mongo database and its own intent and
cleanup proof. Do not replay the accepted local PouchDB source or its browser
cohort. Publish a SHA-tagged qualification image only after the paired check;
the first live WEB4 rollout is still the qualified LocalFile image.

The paired run passed at the committed Core pin. The actual PouchDB and Mongo
providers returned 5 full and 4 subset rows through the manager and local HTTP
host. The host rejected a forged actor and unknown preset, and returned 503
without rows when the source was inactive. The original PouchDB source receipt
remained unchanged and both native stores closed. The temporary Mongo database
was removed after verifying its three records and collection ownership.
The test used the local federation host. Deployed WEB4 HTTP and browser proof
remain separate.

Phase `hosted-paired-c44d97334e4443f398754067a2750877`, 1 pass and zero
skips. TRX SHA-256
`bd9d2b21566edce24ce2768f1b4e695778c56017cdced1dc45a4ec6c4970d4ab`.
Mongo cleanup receipt SHA-256
`2d9a92446c228093e10ee8350a361ec2ac0d5a5e90b071c7cd0bfe7170d6fe0e`.

### Settings rehearsal

`Scripts/Sandbox/Prepare-HostedPouchDbConfiguration.cjs` takes a reviewed
settings snapshot, the qualified bundle, its native qualification receipt and a
new output path. It writes a patch for review without contacting Railway. The
patch contains the opt-in flag, original bundle bytes, source digest, four
presets, a query-only zero-cost tariff and the public catalogue entry for
exactly the three source IDs. The existing sandbox DNA, unrelated tariffs and
catalogues are preserved. An existing PouchDB registration requires explicit
reconciliation.

The rehearsal used the retained bundle and a synthetic baseline. It produced
six setting values and four presets, and rejected an existing registration,
disabled sandbox DNA, missing free tariff and altered bundle. The in-memory
patch SHA-256 was
`9bb8bebf2ca4295902b7bf84e1b3191e2f2f87398dac0424a5f0948fe29b1284`.
Live settings review requires the actual Development snapshot after LocalFile.
The CLI produced the same digest with the real qualification receipt. It
refused to replace the existing output and produced no output with an invalid
receipt. The synthetic files were removed after checking their exact paths.

### Full WEB4 image qualification

[Run 42103803801](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42103803801)
completed successfully for source
`419a41cf2000cd4ebdb5a9b21e69dfc6f004eaf3`. The extra tag equals the full
source SHA. Job 127513245685 completed the final-stage PouchDB 9.0.0,
LevelDB and express-pouchdb load, then published only that commit tag at
`ghcr.io/nextgensoftwareuk/oasis-web4@sha256:e54119f06009280f77d455de16af99ca3bf46af56205a9bf7fe2dd0f7ba1db9a`.
Compact log evidence is in `hosted-preparation/full-image-419a41cf2.qualified.json`,
SHA-256 `0b5aae6847b9c2be50b60b61d878d78d68c5741feaf5f390fa4ba695c2d93e9b`.
This image requires scope review, registry pull authorization and a separate
Development rollout after the qualified LocalFile image.

### Live acceptance entry point

After the LocalFile rollout and the separate PouchDB image, settings and
persistent mount review, use `Scripts/Sandbox/Publish-HostedPouchDbConfiguration.py`
to prepare the six values against current Development settings. It reuses the
protected settings snapshot and per-key journal. Only the public bundle,
digest and presets are exported to `hosted-preparation/prepared-review.json`.

Set the reviewed release commit, immutable image digest, Railway executable
and exact single instance using the same environment variables as the
[LocalFile rollout](LOCALFILE_CQL_MVP.md). Run `verify-mount` on the PouchDB
publisher before its explicit `apply` command. Applying settings stages the
six values without deploying. Review the resulting settings deployment
separately, then set its ID in `COSMIC_CQL_SETTINGS_DEPLOYMENT_ID` and run
`verify-settings`. It checks the same release and writable mount before and
after comparing the complete protected settings. The publisher rehearsal
used the retained bundle and mocked mount observation. It passed the
six-value comparison and refused a missing digest and an unverified mount.

Use the public review with the existing hosted verifier:

```powershell
python -B Scripts/Sandbox/Verify-HostedQuerySource.py PouchDBOASIS --pouchdb-reviewed-settings $reviewedPatch --readiness
```

Readiness uses the retained visitor. The verifier binds the reviewed patch to
the native qualification digest and the exact source IDs, compares all four
discovered presets and refuses duplicate IDs. After readiness succeeds, use
`--prepare` in place of `--readiness` for the scoped HTTP acceptance.
That run verifies native rows, private Mongo snapshots, 5/4 paired rows, SQLite
and Neo4j replicas and independent visitor privacy. Browser verification is a
separate gate. Twelve focused local controls passed, including refusal before
cohort creation when discovery differs. No hosted acceptance has run.
