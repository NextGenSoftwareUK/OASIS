# Decentraland Genesis City scene CQL MVP

The read profile resolves three reviewed Genesis City parcel pointers
through the Decentraland Foundation's production Catalyst scene API.
It validates that each current scene explicitly includes the selected
parcel and its declared base parcel. A stable CQL ID identifies the
parcel, while the active scene entity CID and deployment timestamp can
change when a scene is replaced.

The projection includes the parcel, base, public title, active entity
CID, deployment timestamp and parcel count. Scene assets and individual
profile data stay outside the projection.

| Exact parcel | Scene observed 6 November 2026 |
| --- | --- |
| `0,0` | Genesis Plaza |
| `-53,71` | METATOKYO2 |
| `-100,-50` | Road at -100,-50 |

[Decentraland pointer contract](https://docs.decentraland.org/contributor/content/pointers) ·
[Decentraland entity manifest contract](https://docs.decentraland.org/contributor/content/entities).

At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, native CQL
returned exact 3/2 rows from production Catalyst. Both rejection
controls passed for an absent requested parcel or absent base parcel.
Three checks passed with zero skips. The 5,848-byte TRX SHA-256 is
`f25f800b3680763f12460f2d2a33f635c7afbeb0bcbfd76239ea6732afd41bdf`.
WEB4 admission, local federation and hosted acceptance remain open.

### Core read admission verified

Core `f182d82` allows an explicit Decentraland query tariff. The focused
Core test passed 1/1. It accepted a read tariff with permission, rejected
it without permission, and rejected a write tariff. The parent gitlink
and manifest now pin that exact Core commit.

The wider WEB4 test run stopped during cached restore. `Temporalio` 1.20.0
and `NATS.Net` 3.2.0 were absent from this laptop's package cache. No WEB4
tests or local Mongo federation were run in that attempt. Evidence is
`TestResults/DecentralandCqlCurrent/core-admission.trx` and
`admission-restore.log`. Hosted enrollment remains open.

### Local WEB4 and MongoDB acceptance

The configured NuGet restore resolved the two missing dependencies. All 32
focused WEB4 admission cases passed. The exact Decentraland native HTTP
case passed with both fixture gates enabled. It read the reviewed public
Catalyst source at 3/2, returned 5/4 through MongoDB federation, and checked
foreign-private 0 and foreign-public 3. Two SDK saves were acknowledged.
The fixture database was verified absent and the owned listener stopped.

- `TestResults/DecentralandCqlCurrent/admission.trx` SHA-256 `1d848a36a214d9ee2a5d36d4b1b28d67cdec5b03d7f826222e94adebc86e7b0c`
- `TestResults/DecentralandCqlCurrent/native-http-gated.trx` SHA-256 `87710c05f793b33b8a6eaf8663a67d9854aeaa677190fed18e7ae3c7a66751ee`
- The original `native-http.trx` was skipped before fixture creation due to
  a missing opt-in variable and is preserved as a separate outcome.

Hosted enrollment, replica delivery and browser checks remain open.

### Hosted presets prepared from accepted identities

The original native fixture receipt identifies the reviewed teaching owner
and three scene IDs. Recomputing those IDs from the current WEB4 verifier
matched the original fixture exactly. The public descriptor is
`LocalState/COSMIC/ReviewedPublicMongoHttpQualification/DecentralandOASIS/hosted-source-descriptor.json`.
The four generated presets are in `TestResults/DecentralandCqlCurrent/hosted-presets.json`, SHA-256 `ead5e382079e9ae08e29b028139eb22c6d021af82620013f3c9243f691bf9455`.
No scene source or Mongo fixture was queried again.

Development remains at the accepted LocalFile source `c64adf474`. The
Decentraland task branch has a shared registration for eight additional
source profiles. Carry the Decentraland registration, explicit provider
compiler policy and read tariff into a scoped release and qualify that
release before staging the source owner, grant, tariff and presets.

### Scoped Development candidate in qualification

Candidate `89bb110f066da87ce82c259f24c435f46e45c011` descends directly from
accepted Development `c64adf474`. Its eight changed paths add only the
Decentraland adapter, explicit WEB4 registration and compiler admission,
project reference, and matching Core pin. Candidate Core
`72be1b4e89b8e8dc0a45907c157c43eb03158282` adds only the Decentraland read
tariff to deployed Core. Both exact refs are published on
`work/cosmic-decentraland-release-candidate`. All seven manifest/gitlink
pairs match. The isolated [image build](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42311038805)
uses the candidate SHA and its own tag. Development and live settings
remain at the accepted LocalFile baseline during qualification.

The protected Development settings candidate changes four values. It adds
the qualified source owner, four presets, the exact three public ID grants
and a zero-cost read-only tariff. The LocalFile grant and other settings
were preserved. No hosted values were written. The public review is
`TestResults/DecentralandCqlCurrent/protected-settings-review.json`. The
DPAPI baseline and candidate are under `LocalState/COSMIC/HostedSourceRolloutCurrent`
with the `decentraland-public` prefix. Review the exact deployed release
and full live baseline before applying through the shared journal.

The recovered laptop has no generic rehearsal visitor. Once the settings
deployment is verified, set `COSMIC_CQL_SETTINGS_DEPLOYMENT_ID` and the
exact commit and release ref as for LocalFile. Use `--readiness
--decentraland-deployment-visitor` with `--reviewed-exact-key-source` and
the retained Decentraland descriptor. Then use `--prepare` with the same
visitor flag and descriptor. Readiness creates the deployment-scoped
visitor. Preparation reuses it and refuses stale deployment evidence.
The 23 response-evidence and 39 exact-key configuration tests passed.

The first isolated build failed with CS0246 because its scoped tree omitted
`BoundedPublicKeyValueQuery.cs`. The revised candidate `d31713be` adds that
exact shared helper from the locally qualified source. The helper's existing
identity compiler and serializer are byte-identical to the task branch.
The Core pin remains `72be1b4e`. Follow the revised
[isolated build](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42311827689).
The failed run remains recorded. Development and hosted settings are still
on the accepted LocalFile baseline.

The revised image build passed for `d31713be`. The existing exact-key
publisher reproduced the protected candidate, and the live settings
matched its baseline immediately before promotion. Development advanced
by fast-forward to the verified candidate. Railway queued code deployment
`61ed6138-45ba-4f7d-ad6e-9a39479888ef`. Follow that exact job before
staging the four reviewed settings. No hosted values have been applied.

Code deployment `61ed6138` succeeded and returned HTTP health 200. The
shared publisher independently reproduced the protected settings, matched
the live baseline and acknowledged all four values with deploys skipped.
The same release was then redeployed with those settings as
`b9cb7f79-e9f8-407c-b3ff-30f9bc9945c1`. Observe that exact job and verify
settings before creating the deployment-scoped readiness visitor.

### Hosted HTTP acceptance passed

Settings deployment `b9cb7f79` at `d31713be` passed the health, protected
settings and discovery checks. Native CQL returned 3/2 scene records.
MongoDB federation returned 5/4 rows. Two private copies were created, and
repeat preparation reused both with zero new saves. The independent visitor
saw three public records and no private copies. SQLite and Neo4j each had
both expected replicas and denied the private copies to the other visitor.

The deployment-bound summary and original response hashes are in
`LocalState/COSMIC/ReviewedPublicMongoHttpQualification/DecentralandOASIS/hosted-http-qualified.json`.
Original HTTP reports are under `TestResults/HostedCqlIntegrationCurrent`.
The sandbox card and interactive browser proof remain open.

The [Decentraland sandbox card](https://web4.oasisomniverse.one/web4-sandbox.html#cql-decentraland-catalogue)
is live at Web4Site `46df63d`. It links the hosted presets and the verified
local C# query example. All 93 focused Node tests passed. Live HTML matched
the published blob at SHA-256
`686cec4adbd6a351028adac073a8c16ef3def6f72b23781aab621cc79b96b767`.
Interactive browser proof remains open.

11 November UTC: DecentralandOASIS public scene teaching presets returned 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. Original-owner paired browser verification remains open.
