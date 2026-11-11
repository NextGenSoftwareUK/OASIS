# CQL branch integration status

## Core tariff reconciliation, 11 November 2026

Development pins Core 9c0aa3851a and the COSMIC branch pins 812d3a000d.
The exact StorageQueryTariff provider set has 112 entries on Development
and 170 on the feature pin. Every Development entry is present on the
feature pin. The 58 additional entries include Firebase, Infura, TRON
and Urbit. These are explicit tariff eligibility entries. Hosted grants,
provider activation and native evidence remain separate requirements.

The two Core trees also differ across 40 files, including HyperDrive
replication. Preserve Development Core for the scoped GOMap merge,
which already includes GOMap tariff eligibility. Reconcile additional
tariff entries with their reviewed reader scopes before advancing the
shared Core pin. Keep parent gitlink and deployment manifest in sync.

## Current Development coordination

Review Development and HyperDrive every 30 minutes. Batch verified, non-overlapping COSMIC changes and leave at least 30 minutes between Development pushes. Check the latest shared push and the Railway WEB4 queue before integrating. Defer routine pushes while the preceding WEB4 release is queued. Use work/cosmic-cql-ton-public for feature work.

## Accepted Development history merged — 8 November2026

Parent task branch work/cosmic-cql-ton-public merge
`6e2cccbd1077fb669ac1077a71fc9a51df065a50` includes accepted Development
`88c378c1e08ee270365b760c9d3abc29b629a0f4`;0commits behind after fetch/merge.
Core task branch work/cosmic-cql-decentraland merge
`05d049a0228b93b22ba16c9775bfb4edcf71146a` includes the accepted parent Core
`81862b2450c1d4bd0f3e8853523c1b13b8a257d3`. Moving Core Development54159614
was inspected but not pulled into the accepted dependency by assumption.
Both existing task branches were pushed. No new branch/worktree or deployment.

Read-only merge analysis found22parent conflicts and one Core conflict. Core's
162-provider admission set includes all98accepted entries; all text outside the
set was identical. The complete Core source tree of05d049a is byte-identical to
eebf39bf. Parent nonconflicting paths already equalled the task tree. Manual
conflict review retained newer task packaging/owned-runtime/configuration and
metadata-fixture changes, all accepted provider registrations and the separate
six-reader qualification workflow. It preserves the empty image-tag default
that fixed the prior alias incident. Historical branch-trigger variants do not
replace exact-source manual qualification or move any prepared candidate.

Parent merge changes exactly two entries: Core gitlink and its matching manifest
pin. The canonical sync tool ran with --source index against a temporary index,
so unrelated STAR/WEB6 working heads were not swept in. Manifest/seven-Dockerfile/
gitlink validation passes. Temporary indices removed; unrelated real index
entries and four pre-existing dirty working files hash-checked unchanged.
Original immutable TRON/MapLibre/four-provider candidates were not moved.

The first preparation helper stopped on a Python bytes/encode mistake after the
owned manifest synchronization and before real index/ref mutation. The same
intent was continued after validating the exact pending manifest, index entries,
branch heads and protected working-file hashes; original intent remains.
Compact source/intent/application evidence is under
LocalState/COSMIC/RecoveryLaptopResume20261107/*development*merge*.json.
No accepted native source was replayed. Source-tree identity and exact changed
paths establish that this merge adds history and a content-identical dependency
reference, not new runtime behavior. Cloud exact-seven-checkout run42502482108/job128798029363
PASSED. Artifact13626635364/538bytes SHA256
`e250967d2c173957a3d5387f9ef5e2197c6563958d08289b47e249eea8ee8147`
was inspected in memory: source6e2cccbd, all seven actual checkout HEADs equal
the manifest, including Core05d049a; liveDeployment=false. Compact receipt
railway-history-merge-pins-qualified.json retained; no ZIP extraction. No additional Development push8November or master promotion.

## Historical checkpoint — 5 November2026

Fetched `origin/Development` before this continuation. Development remains
`b718f2879` (2November, serialized failed-source recovery); task branch includes
all its changes. At the start of this MinIO preparation batch the task branch
was0behind/562ahead. No force push, additional worktree or environment was created.

The parent task gitlink selects Core
`ece17341fa3cea3e2c36dae08fec106effa12b76`; Development selects
`54159614ebdb879c8636eed5fae60be97dbbfff3`. The shared Core working checkout
is restored to5415961 after each leased qualification. Local chat inventory
confirmed no other active OASIS chat before temporary checkout of the exact
candidate dependency. Other contributors' dirty
cleanup/actions/STAR/Core state is preserved.

Do not bulk-promote the task history to synchronize branches: pending scopes include
owner-gated runtime/account access and unverified
hosted enrollments. Prepare the next scoped integration batch with its exact
Core dependency, API build and deployment evidence, excluding incomplete work.
Move dependency gitlinks and Railway manifest pins together and validate them
before any Development publication, per the repository policy. Source branch
and website catalogue publication are separate from an API deployment.

MinIO hosted preparation adds one scoped Core commit on top of the previous
7b17894c task pin. Its query tariff admission and shipping API presets/configuration
pass17checks, provider CQL/lifecycle13checks, and native qualifier build0errors.
Core branch is published and remote SHA verified; parent gitlink/manifest advance
together. Owned TLS source, image/settings deployment and hosted acceptance remain
open; this is not authorization for bulk Development publication. The earlier
Elasticsearch local native capacity gate is closed by its recorded local proof.

The qualified LocalFile image/mount/deployment gate remains the first live API
priority. Railway CLI access was rechecked this continuation and remains
unauthorized. Its existing owner action is unchanged; no fresh deployment,
production promotion, reviewed settings write or permission bypass occurred.
See [current deployment evidence](LOCALFILE_CQL_MVP.md) and
[current programme state](CURRENT_STATUS.md).


## Scoped Urbit Core candidate after accepted-history synchronization

Coree62ec44cfc7d80640195ae4a6e538908015ea61e adds exactly one Urbit entry to the explicit read-query tariff admission set on existing work/cosmic-cql-decentraland. Core branch/remote equality checked before editing; unrelated ProviderType edits preserved. Parent gitlink and manifest move together using index sync. This advances the task candidate beyond the byte-identical05d049a history merge; it does not change the accepted Development baseline or any immutable candidate tag. Required admitted-component controls follow at this exact pin. No third8November Development push.

Coree62ec44 admitted endpoint candidate now passes exact-source3/3 component controls42523100057 at parent ec1f2baf1; hashed artifact13634475851 inspected. Full private registration/compiler30/30 proof42522330369 is retained at its earlier source/pin. No new Development push or immutable candidate-tag change; native WEB4 HTTP/image/live gates remain open.

Native WEB4 MVC42525613719 and actual Mongo + Urbit federation42529114754 now pass at Coree62ec44, frozen parent6981acb6b and834f713a0 respectively. Both native phase TRXs1/1 each, zero skipped; hashed artifacts and exact owned cleanup inspected. Native federation5/4 and source/projection hashes survive the same pier restart. Controlled identity/outbox only; public HTTPS hosting, exact deployment image, production JWT/durable billing and live/browser gates remain open. No third8November Development push or immutable tag change. See URBIT_CQL_MVP.md for portable evidence digests.

Corrected immutable full image1c9c399042272489e582b0646e16052d478382d1/Coree62ec44, digestsha256:37e966a8d0c47e8242844f254947ab74e7a7cfe6d8371d7bd9e45735c7f31e0f, now passes exact package/TRON+Urbit and tightened Docker-health/API/native startup42534822963. Artifact13638493286 hashed/inspected; running healthy gate, final actual probe0, graceful exit0/no OOM, exact cloud cleanup. Original green-job/missing-curl candidate54be8845a rejected and preserved unchanged; root fixed in shipped Docker health command using its existing Node runtime. Only Docker health/verifier/tests/docs changed since accepted Urbit native source, no CQL API/provider/Core changes or native cohort replay. No new Development push/alias move/live enrollment; hosting/authorization and remaining live/browser gates still required.

11 November UTC: parent OASIS uses work/cosmic-cql-ton-public. API Core is a separate repository; its legacy work/cosmic-cql-decentraland remote tip 0f8ce6d and Core Development f723427 have 94 and 63 exclusive commits respectively by ancestry. The old Core tariff support set has 58 entries absent from Development, including TRON, and the Development set already includes PlanetScale and GOMap. Tariff support by itself does not enroll a hosted reader. Review unique changes against source and release evidence before transferring them. Preserve uncommitted changes in ProviderType, HolonManager-RequestContext and PreparedStorageExecution. No branch or deployment changed during this audit.
