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
