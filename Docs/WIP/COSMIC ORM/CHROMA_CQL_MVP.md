# Chroma CQL MVP — in progress

30 October 2026. Inventory remains red; native/restart and Mongo-paired C#/HTTP/browser acceptance pass.

## Verified save ownership

- Holon IDs/provider keys are staged in a runtime-typed JSON document, then
  published to the caller only after the native HTTP write succeeds.
- Failed/unknown writes preserve caller identity and existing provider keys;
  no replay occurs. Derived fields and exact decimal JSON survive serialization.
- HolonManager retains change-tracking acceptance ownership.
- Batch saves expose errors, count acknowledged writes, and honour stop/continue.
- Unsupported history rejects before identity allocation or network I/O.
- Dedicated injected HttpClient remains caller-owned; the original constructor stays.

Bounded leased Windows .NET10 build + tests: **12 passed,0 failed,0 skipped**.
[Tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaSaveOwnershipTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/save-controls.trx`.
SHA256 `CB9D69E2418F8C43BC6D76A90A92BAFE50D4BD55A6E9EA4F0FA3C25E9E0105AA`.
Provider and test cache-only restores succeeded, no download. Initial no-restore
provider build correctly rejected missing assets; explicit cache restore fixed it.
These are injected-HTTP controls, not native database certification.

## Next acceptance

Scoped breadth milestone complete. Continue remaining red provider identities;
full Chroma parity and the documented gaps stay open.

## Remaining gaps

Collection resolution is now read-only and validates the explicit schema;
pre-existing unmarked collections require deliberate audit/migration. Generic read helpers
can hide HTTP errors as missing data; list bounds/response-ID ownership and
malformed JSON need admission checks. Shared upserts lack request/response bounds
and runtime serialization on other write paths. Avatar saves still need caller
ownership review. Children, history, conditional writes, replication, migrations,
full EF parity, cloud/auth and vector similarity remain unverified. No native
receipt/CAS guarantee is inferred from an HTTP success.

[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Bounded CQL controls — 30 October 2026

Shared bounded-identity compiler bound specifically to Chroma: trusted complete
catalogue of at most100 GUIDs, captured identity membership/Boolean expressions,
and stable admitted-key pagination. Broader predicates/order/graphs reject before
native execution. No vector-similarity or unbounded scan capability is advertised.
One tenant/database/activated collection UUID `/get` request, explicit ids,
include documents, limit selected+1 and offset0. Querying never creates collections.
Native parallel IDs/documents require equal lengths, bounded counts, canonical
unique selected IDs and matching payload IDs. Missing/deleted rows are omitted;
malformed rows fail the whole page. Existing trusted catalogue grants remain the
read authority (creator mismatch alone does not revoke an explicit catalogue grant).
Linked15second deadline,4MiB response/1MiB document bounds, cancellation and final
page budget are checked before publishing. These are adapter budgets, not native
server limits or guarantees of storage transactionality.

**38 passed,0 failed,0 skipped**, including prior12 save controls,9 compiler/adapter
cases and17 adversarial native-response cases. Initial test assumed catalogue
access meant creator-only; corrected that test against the existing explicit
`AuthoritativeRecords` contract, with no Core/permission behavior change.
Evidence: `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/cql-controls.trx`.
SHA256 `91191B86039335EED8696F846FA6EABBA7A23CB05D2F19B266CFC94BAF192567`.
[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaCqlTests.cs)
and [response tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaNativeQueryTests.cs).
Cache-only restores/bounded provider build pass. Real native/paired acceptance
remains red; no hub or inventory green change.

## Explicit collection admission — 30 October 2026

Pinned official [Chroma1.5.9 collection model](https://github.com/chroma-core/chroma/blob/1.5.9/rust/types/src/collection.rs)
provides UUID/name/tenant/database/metadata/dimension. Resolve each of
`oasis_avatars`, `oasis_avatar_details`, `oasis_holons` by native GET-by-name;
no create/upsert/get_or_create during activation or collection resolution.
Require canonical nonempty UUID, exact configured name/tenant/database and explicit
metadata `cosmic_storage_schema="chroma-doc-v1"`, `cosmic_embedding_dimension=1`.
Native dimension may be null for an empty collection or1 after its first embedding;
other dimensions reject. This declares the provider's existing one-scalar embedding
contract; it does not certify arbitrary native index/schema settings.

A15second deadline/256KiB streamed schema bound apply. Activation clears cached
IDs and active state before fresh validation, clearing partial cache on failure.
Provision these collections deliberately with the above metadata; do not simply
relabel existing incompatible data. Existing unmarked collections are refused and
require an explicit audit/migration. No automatic destructive replacement.

**50 passed,0 failed,0 skipped**, including12 new schema/admission tests.
[Schema tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaSchemaTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/schema-controls.trx`.
SHA256 `FE1D2FCC2364F7CC6AE19B80B0483DCDE20C6796A5A18CC62984914849D25D9E`.
Native startup/schema/restart and Mongo-paired demos remain next; inventory stays red.

## Native original-source verification

Official free Chroma1.5.9 linuxamd64 image pinned to
`sha256:abcce7c335e2dab9f11ef629296f7309b09cb19ae4b34da32ac7e34ff5773140`;
image IDa221f3391b5eabc5b0b293b65958a28f10d7015be67e4d8cc33a5e4e60e6f3ca,
642,494,577B expanded. One retained source `/var/tmp/oasis-chroma-cql`, container
`oasis-chroma-cql`, host loopback18002->8000,2CPU/2GiB. Plaintext/no-auth local
scope only, no public/cloud/security certification. No telemetry configured.
Native `/version` returns1.0.0, hard-coded in the pinned Rust frontend; exact image
digest verifies release identity. Initial mistaken version assertion stopped0
before collection creation; same source/identity resumed, never replaced.

Original three collection UUIDs and descriptions survive restart exactly:
- avatars04a2abe4-a436-4c1a-a756-d95b3d7e2ebf
- holons30519c99-a701-4ab0-a254-3d4b70364257
- avatar details817552a3-084b-4e3d-8d11-b453392c390f

[Native test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests/ChromaCqlNativeTests.cs)
passes1first+1restart/0failed/0skipped: original3records+1deliberate replacement,
obsolete derived field removed from stored document, all3/filtered2/missing0,
stable paging/current updated head/budget rejection, exact original stored JSON
hashes and complete collection values retained. No reseed/replay or new baseline.

First test exposed unordered schema `keys`/`metadata` serialization. Recovered
exact original schema preimage by enumerating48order-only permutations; its SHA256
matches retained original E20DA21BE200677B6FF46542DA78AB3760CEBC0A71BC86546396070FC1F1AF24.
Original baseline remains unchanged. Test now verifies that original preimage hash
and compares all schema values with object-property order canonicalized; array order
and document bytes remain exact. This is a test representation correction.

Evidence under `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/`:
- native-first.trx SHA25616a0d0ababb0ef60ce727242b1158305408eed4281122591d1de61abf1649178
- native-restart.trx SHA256bcea39cdb67ed977041e986888fa1d0ed25e02f7933e7494ca24f2b71d85d4d1

Source311,674B retained with seed, original receipts/schema/baseline; no pending
intent, owned service stopped with native exit0. Rerun with
`wsl -u root -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-chroma-linux.py`
after the bounded leased integration build. Environment creation helper refuses
existing source; never rerun it to replace this fixture. Mongo-paired proof remains
next, so inventory/hub count stays unchanged at61 scoped provider proofs.

## Mongo-paired demo acceptance

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Chroma -Federated -Serve`.
Browser: `http://127.0.0.1:5150/`. Use `-VerifyOnly` instead of `-Serve` for the
bounded automated verification. C# teaching code is shared in
[IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs);
[paired integration example](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests/ChromaMongoCqlNativeTests.cs)
shows provider construction, explicit trusted catalogues and HolonManager execution.

All=Chroma3+Mongo2=5 physical copies; subset=Chroma2+Mongo2=4. Matching IDs retain
provider provenance. Native Chroma uses one bounded multi-ID get; Mongo executes
its own native query. No distributed snapshot or general content filtering claim.
SDK+HTTP tests reject forged actors/unknown presets and return503 without rows
when Chroma is deactivated. Actual browser tab65 verified both buttons and closed
after its owned stop action. Screenshot:
`C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/browser.jpg` (49,628B).

Native-first/restart/Mongo-init/federation/native-postpair each1pass/0failed/0skip.
Final paired TRX SHA256453fa1a43ea05e413919a704896e689fae7e013b08b8c737dee922aa3f20c601;
postpair SHA256f50c5ccbb2780e4239b4634924c11a40225d4a9b79fa9313b142f3b2bd668268.
Files remain in worktree TestResults/ChromaCqlCurrent. Hub routing passes60paired
profiles/120plans,102paired rejections; listeners unchanged.
Temporary database `cosmic_cql_mongo_chroma_demo` was absent before the owned
journal, verified after use and dropped; pending journal cleared. Original Chroma
source311,674B/seed/schema/receipts unchanged; Chroma native exit0, Mongo and
teaching server stopped, ports18002/27039/5150 not listening. Inventory now62/226
linked scoped proofs. This milestone does not close full ORM/CQL parity.
