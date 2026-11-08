# Urbit COSMIC / CQL MVP status

Updated 8 November 2026. Goal active and incomplete; user priority-chain demos
remain first. No new public profile/card/count or Development deployment.

## Current gate

Actual native book, authenticated SDK, private CQL, shared V2 manager and the
shipping WEB4 MVC route pass full3/subset2 before and after the same owned native
pier restart. Owner/IDs/contact and overlay data remain exact; anonymous,
forged-field, foreign-actor and inactive-source paths reject without additional
native reads. API projection hash is preserved; owned resource cleanup verified.
Identity validation and usage outbox are controlled test fixtures. These tests
are not production JWT, durable billing or live/browser acceptance.

Actual Mongo-backed native federation is **accepted**:
[42529114754](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42529114754),
frozen parent834f713a091ebc2d91250cf95cad4e80c54b0d0b/Coree62ec44.
Both exact native phase tests executed/passed1/1 with zero skipped. Shipping MVC
federation returns5/4 with independent permissions, provenance and receipts;
unrelated canonical copy excluded. Foreign403/inactive503 read neither source.
Canonical BSON and federation projections remain identical across the same pier
restart. Canonical copies seeded3before/0after; owned database absent afterwards.
Mongo container/two labelled volumes and native container/pier/internal network
removed; restart/final exits0, no OOM. This does not qualify production JWT,
durable billing, public HTTPS registration, live deployment or browser use.

## Accepted evidence

Artifacts below were downloaded and digest/counter/outcome checked in memory;
only compact safe receipts were retained locally. Counts describe their scope.

| Gate | Accepted result | Run / artifact / SHA256 |
|---|---|---|
| SDK/private CQL/shared-manager controls |65/65, zero skipped |42518331256 /13632455738 /c506482a1d1f22de1558755be9c679c2571c2f15a51a134645c054cb03d9fba2 |
| Private WEB4 registration/compiler |30/30, zero skipped, source d4edadf7f (earlier Core pin) |42522330369 /13633868589 /2bac5248c9090130b374cb31be0e6ab8debdc31117a555e1826036e4ee07c4f6 |
| Native SDK/CQL/manager/restart |Native3/2, foreign0, anonymous403, source hashes/cleanup |42519994312 /13632864832 /79328082767f9afe196a6f2eb8af8e52604d6313260a707536b758f5a0667544 |
| Actual native WEB4 MVC/restart |1/1 each phase, source6981acb6b/Coree62ec44 |42525613719 /13635911089 /1c2806b86aa9947353b67509fbc15e87287f50ec33285f37dcde5541fbef6312 |
| Refactored admitted/MVC/federation controls |7/7, zero skipped, source834f713a0; Mongo/native replies controlled |42528767151 /13636726128 /5c593977ab7987c8283d82fd608ae9674d3049959befebdfac44897106a4e08b |
| Owned Mongo topology/cleanup |Pinned replica, internal-only, exit0/no OOM, two volumes/network removed; no native ship/data seed |42528796075 /13636343596 /5156b6c7a0403b6bc9d0b18cf8db156701a1043b1187455cc26d810c24e3cd77 |
| Actual native Mongo + Urbit MVC federation/restart |HTTP5/4,1/1 each phase, zero skipped; canonical hashes, one-time seed and cleanup |42529114754 /13637565973 /193927a9c5c4eccd4eead26802117685100c04ab8484c50903b22907089d718f |

Native WEB4 API projection SHA256 before/after:
`df6d5ce459ad17015f4f32ef5df8331531f40597d231dc2fa9a67ad09ba6a80f`.
Both native phase TRXs executed/passed the exact native fact with no skips.
Eight namespace ownership/exposure controls pass; child commands enter only the
owned internal network and drop to the runner UID/GID, with no published ports.

## Contract and boundaries

- Current contacts-v1 routes and tagged JSON; no legacy contact-store fallback.
  Book pages are contact and overlay separately, never merged implicitly.
- Owner-bound virtual views, no imports/saves/automatic replication. Stable IDs
  use owner/provider/source/native CID/persistent connection identity, not URL.
- One explicit catalogue of1–32unique bounded CIDs; complete bounded identity/IN,
  Boolean and paging reads. Source15s deadline, native64KiB per point, JSON depth16,
  selected text16384chars and complete mapped output4MiB. No whole-book scans.
- Missing and existing blank pages both return `[{},{}]` natively. Empty defaults
  fail with CQL_NATIVE_IDENTITY_AMBIGUOUS; do not invent absence or phantom holons.
- Private source rejects foreign actors even with forged direct SDK catalogues.
  WEB4's keyed policy additionally rejects foreign actors/authorities before reads.
- Explicit server configuration only: CQL_URBIT_QUERY_ORIGIN (credential-free
  public HTTPS), SHIP, SESSION_TOKEN, OWNER, CONNECTION_ID and PAGE_IDS (JSON).
  All absent enrolls nothing; partial/invalid configuration fails before enrollment.
  Duplicate policies are rejected; browser fields cannot grant access.
- Guarded DNS/HTTPS, no redirects/proxies/cookie mutation, exact selected point GETs
  only. No login, writes, full-book reads or caller credentials in that transport.
  Native loopback qualification is separate from public HTTPS hosting/registration.
- Coree62ec44 adds Urbit only to explicit query-tariff eligibility. It does not
  relax permission, pricing, measurement, signing or canonical-write rules.

## Remaining gates

1. Native federation artifact accepted against every assertion above; retain its
   compact receipt and exact source rather than replaying the accepted cohort.
2. Durable source hosting and demo authorization. No new hosting spend is
   authorized. Private-book policy is not an all-visitor/public teaching grant.
3. Exact dependency/image/runtime qualification and a permitted Development batch;
   no third8November push, production/master promotion or moved candidate tags.
4. Live authenticated visitor flow, reuse/replicas/cleanup and actual browser proof.
5. Blank-page existence contract, wider CQL, canonical provider CRUD/full interfaces,
   imported/virtual/hybrid persistence and EF/NHibernate parity remain incomplete.

Package qualification preparation: the explicit `urbit_reader` input requires
SDK/book/query symbols, WEB4 registration and the actual project/runtime/direct
API dependency graph from the image, in addition to existing full-package checks.
The actual Node probe passes15 controlled positive/negative cases in6 local test
methods; these are packaging controls, not image/runtime acceptance. No image
build or Development publication is implied by these controls.

## Files and reusable resources

- Provider: Providers/Other/NextGenSoftware.OASIS.API.Providers.UrbitOASIS/
  UrbitContactsV1Client.cs and UrbitContactBookQuerySource.cs.
- WEB4: Services/Cql/CqlUrbitReadSourceRegistration.cs, existing CqlV1Compiler,
  Startup/read-profile lifecycle and keyed ICqlReadAccessPolicy.
- Reused Core.UnitTests: CqlUrbitAdmittedTests.cs, CqlUrbitFederationAdmittedTests.cs,
  CqlUrbitNativeMvcTests.cs and CqlUrbitNativeFederationTests.cs.
- Cloud scripts: Scripts/TestHosts/qualify_urbit_contacts_install.py,
  qualify_urbit_contacts_book.py, qualify_urbit_contacts_http.py,
  qualify_urbit_web4_native.py, qualify_urbit_federation_native.py,
  owned_urbit_http_namespace.py and owned_urbit_mongo.py.
- Three immutable runtime/desk/kernel caches stay cloud-only. No local .NET,
  Docker image, native runtime or test database installed. Test sessions stay in
  memory/one child-only variable, never arguments/reports/user or machine env.
- Parent work/cosmic-cql-ton-public and Core work/cosmic-cql-decentraland pushed.
  Core gitlink and manifeste62ec44 match; unrelated working edits preserved.
  Do not replay accepted original live cohorts or re-save copies for queue order.

## Detailed history and primary protocol sources

[Archived checkpoints, failures, pins and primary-source links](Archive/URBIT_CQL_MVP_HISTORY_2026-11-08.md)
retain the full earlier record. Historical active/unqualified statements there
are dated observations, not the current status. Git archive blob SHA256 (LF):
`3d2cdd98647c0bd2de96dcf17d714e3b4a4592564190d4d5a4502c04231e59c0`.

[Current programme status](CURRENT_STATUS.md) ·
[Recovery laptop cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md) ·
[One owner-action list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
