# ArcadeDB bounded CQL scope

Current checkpoint: native/restart, manager/Mongo/HTTP and local browser acceptance
pass. Local profile99 is verified; WEB4 local card8123df9 is published and
browser-verified. Hosted/full-provider
acceptance remains open. Earlier sections retain the native checkpoint history.

Initial native checkpoint,5 November2026. Provider/unit suite22/22 passes, zero skips; eighteen new CQL
cases and four metadata tests. Native provider bootstrap1/1, native reader1/1
and retained restart1/1 pass, each zero skips. Three runner readiness controls
pass. Integrated manager/Mongo/HTTP/browser acceptance remains open, so coverage
counts remain98local/60hosted HTTP/28hosted browser, with32browser gates open.

## Query contract

The adapter uses the existing native envelope: Holon.oasisId is the canonical
GUID string and Holon.doc stores default Pascal-case serialized Holon JSON.
Each selected ID becomes a parameterized read-only POST /api/v1/query/oasis:

```sql
SELECT oasisId, doc FROM Holon WHERE oasisId = :id LIMIT 2
```

The parameter is supplied in the JSON params map, never interpolated into SQL.
The shared compiler admits at most100explicit authorized IDs and bounded identity
algebra. Zero result rows means absent; two results reject a broken uniqueness
invariant. Envelope/stored IDs and read authority are checked, duplicate JSON
properties rejected, deleted records excluded, children stripped, and canonical
ordering/paging applied after filtering. HTTP errors, malformed/error envelopes,
duplicate IDs, wrong payloads and byte limits produce OASISResult errors without
partial rows. Responses are streamed under1MiB each/4MiB aggregate and a15second
overall query deadline. Default HTTP redirects are disabled. Historical reads
are explicitly unsupported; arbitrary predicates are not locally evaluated.

Read-only activation requires an explicit existing live teaching-record ID and
checks it through the same decoder. It never provisions a database or invokes
normal storage activation. The normal constructor's provider name/description
metadata was missing and is now set. The old unit suite called GetProviderVersion,
which is absent from the current provider contract; that obsolete test was
removed rather than adding a compatibility shim. Initial compile failure is
recorded here; no native tests were dispatched by that failed build.

## Native runtime and source

One official ArcadeDB26.11.1 base distribution (published4November2026) is retained
in LocalState/COSMIC/RecoveryLaptopArcade26111. It reuses the existing pinned
Temurin21.0.13+9 JRE in RecoveryLaptopSolr1010. ArcadeDB's server class is Java
major65(Java21). No second Java/system/PATH/service environment was installed.
The91 installed files match archive-derived manifest SHA256
 d37e2fec418708afb0e4b6e192dfd5a03bcc43d19612b99542724711c1b08c76.
Archive86,506,630bytes was verified against official GitHub asset SHA256
 f4307c06223c14efc42e526845a241d5252b36b74908b08eabf3383b9c7b6c84,
then removed after exact-path/hash/installed-file verification. Receipts remain.

The owned JVM uses low-ram profile,64MiB initial/256MiB maximum heap, HTTP bound
to127.0.0.1:24800, HA off and no optional protocol plugins. Runtime files remain
separate from the writable home/config/database. A generated root password is
protected with Windows DPAPI; plaintext is passed only in owned process/test
environments, not arguments or source metadata. Native logs were checked and
contain no plaintext password. This is an owned loopback teaching source, not a
hosted source or a claim of least-privilege server credential qualification.

The one explicit bootstrap checked oasis absent, flushed the intent before normal
provider activation, then saved three private live Holons and one deleted control
through the actual provider. Each save has its own original intent/ACK. The fifth
ID is an absent control. Acknowledged bootstrap and source identity are retained;
no create/save was replayed. Native read fixtures independently inspect the exact
native envelopes, return3/2/0 and a paged row, and compare all four stored document
hashes before/after each test. Retained restart reuses the same store and source.

Both owned server runs ended through the native shutdown API with result=ok and
exit0. Port24800 is closed. Readiness requires documented204 on /api/v1/ready;
only503/connection/timeout states are polled under a60second bound. A wrong success
body, authentication error, dead process or deadline fails readiness. The reusable
runner defaults to retained reads and requires a matching bootstrap ACK. It refuses
--initialize if the source/home already exists. All operations held the shared
provider lease and bounded .NET workers.

Exact TRX evidence:

- `bootstrap.trx`: 2882bytes, SHA256 `b6bc6a4c80c898b20295b3cf3322eab1bd23a9b445a1a1b344bea84c72318ba3`.

- `initial.trx`: 2885bytes, SHA256 `25331c83c69a6290acb799bb5c17bc5f04d0b1ed4705f33aca5360265c279cd3`.

- `retained-931c73452b364611b349b69ac53337d2.trx`: 2885bytes, SHA256 `7d5a2f79cd4a9f1bcb889e1b58122f9b434438f521a1fb3f17c693b357abbf2b`.

Compact native-evidence.json and process/runtime/source/bootstrap receipts remain
in the local state directory. Full CRUD/storage conformance, conditional writes,
ORM/HyperDrive and hosted enrollment remain open. Next: manager/Mongo/HTTP and
interactive local browser flow against this retained source, without reseeding.

Protocol/runtime references: [HTTP/JSON API](https://docs.arcadedb.com/arcadedb/reference/http-api/http),
[requirements](https://docs.arcadedb.com/arcadedb/reference/requirements),
[official release](https://github.com/ArcadeData/arcadedb/releases/tag/26.11.1).

## Integrated local acceptance — 5 November 2026

Actual manager/LINQ and Kestrel HTTP paired1/1 pass, zero skips. Full query
returns5physical rows(ArcadeDB3/Mongo2), subset4(2each), with exact provider
provenance and exclusion of the third unrelated Mongo record. Forged actor and
unknown preset return400; inactive native provider returns503 without rows.
The host accurately labels the authenticated native ArcadeDB source.

Local browser tab7 independently showed full5/subset4 JSON and MovingProviderReads,
then used Stop demo. Interactive runceeaaeafb7114c3ab245d9610c43de2f passed1/1,
zero skips. The runner verifies original ID/name/owner/parent/privacy/activity/
deleted-date values and all four native document hashes before/after. Absent
control remains absent; no ArcadeDB seed/save replay. The exact temporary Mongo
database was verified/dropped and confirmed absent; journal/ready markers are
removed. Owned ArcadeDB stops through native API with exit0; Mongo stop confirmed.
All24800/27029/5118 listeners are closed. No screenshot artifact files.

Four runner control tests pass, including original-value/missing-control checks.
The guide and single-environment launcher are ready. Local count99=3canonical+
57public/source+39otherlocal, including31historical recoveries. Hosted60HTTP and
28browser/open32 stay unchanged. Full storage/ORM and hosted enrollment remain
open. WEB4 local card publication is next.

- `paired.trx`: 8152bytes, SHA256 `60891ccee6c0d70dc9c3a1eba183792f32aa03fb72eac7ded2028936c6fa913d`.

- `interactive-ceeaaeafb7114c3ab245d9610c43de2f.trx`: 12326bytes, SHA256 `b4f09240b179137e80c4f0c897f346b09bed7c969c768d640d7e6a0839c78b0e`.

## Published WEB4 local card

Web4Site8123df9c227a0199b7c2e4cc63f7374feaf7d3a6 publishes card42,
ArcadeDB + MongoDB / Windows local. It explicitly labels the prepared local
source and states ArcadeDB is not enrolled on the hosted API. Its launch command
is Run-CosmicArcadeDBMongoDemo.ps1 -Serve, localhost5118. One demo at a time:
this port and the Mongo environment are shared with other teaching demos.
New launcher audit1/1present; existing accepted launcher environments not rerun.

All112 client tests pass, zero skips; GitHub Vercel status success. Public sandbox
HTML returns200 and matches the exact committed Git blob:210,868bytes, SHA256
f9aee60d1fa76818c0c3aa0c3259e73a812df7bdffb349967bde2715b4cad210.
Guide and C#source links are pinned to parentd0be32db900a08eb2438f663c326fd1e1154e258;
both raw sources return200 and match committed Git bytes. Guide SHA256
565717ab58e17d87ee2eba916960f16b3e380faf7431cfe19dc8b56a0611f0cc;
walkthrough SHA25611a55d640cd5b6e02e00c60e550d600d1c823cc5c97a308cb8922f9be7916867.
Signed-out live browser tab8 verified the card, local label and exact expanded
launcher command, without creating a hosted avatar/session or forwarding tokens.
No hosted API deployment, settings or source cohort changed. Hosted enrollment,
least-privilege credential qualification and full ORM/provider gates remain open.

LocalFile priority rechecked: retained Railway CLI whoami still Unauthorized.
The existing owner action remains pending; no duplicate action/account request.
