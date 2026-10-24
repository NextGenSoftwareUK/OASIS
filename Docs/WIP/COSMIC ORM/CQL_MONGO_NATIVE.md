# MongoDB native CQL implementation checkpoint

Current documentation: [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [three-provider demos](CQL_DEMO_RUNBOOK.md), [WEB4 v1 contract](CQL_V1_HTTP_CONTRACT.md). Historical checkpoints retain their original scope/counts.

20 October 2026. Final acceptance passes **Core 1,536/1,536; Shared 895/895; MongoDB unit 188/188 and selected native 84/84; SQLite native 1,042/1,042**, zero skips. MongoDB's three CQL cases are included in its selected native suite. Core and Shared hashes match across all consumers. Core a6d0865 is paired with the parent gitlink/manifest in this batch. Both retained fixtures were independently reconciled before removal. The full CQL roadmap remains open.

## Shared entry point

Typed-property prerequisite discovered by the expanded native test: Core previously rejected direct `CustomOASISProperty` predicates such as `row.Rating >= 7`. Seven focused cases failed with five safety controls passing. The shared compiler fix is committed as Core `a6d08658cae5643f92cb6f9b930fb723d93053e7`; both targets build and the complete Core suite passes 1,536/1,536 at the final committed epoch. MongoDB's direct annotated-member query, typed hydration and conditional custom-property write-back also pass natively. This belongs to the shared compiler, not a MongoDB-specific property rewrite.

The compiler reads trusted attribute metadata without executing getters or constructing attribute subclasses. Scalar annotated properties become their declared literal metadata key, including inherited/overridden markers; native provider capability admission still applies. Unmarked, read-only, canonical-shadowing and StoreAsJsonString properties are not implicitly translated. These paths inherit the explicit metadata missing/null/type semantics; they do not execute CLR constructors/getters to infer application defaults for absent keys. Broader runtime schema/default/alias semantics remain open.

MongoDB implements Core's existing `ICosmicQueryStorageProvider` interface. Applications build `CosmicQuery<T>`, plan against an already authorized target snapshot and call `HolonManager.ExecuteQueryAsync<T>` as in the [getting-started guide](CQL_GETTING_STARTED.md), selecting `ProviderType.MongoDBOASIS` and this provider's `CqlCapabilities`. The manager remains responsible for typed hydration, decryption, clean tracking and result publication. An adapter/plan grants no authorization.

## Native contract

The adapter admits against its actual immutable capability profile before repository access; caller-supplied broader capabilities cannot enable unsupported predicates. All filtering runs in MongoDB BSON expressions, with simple binary collation. User values and metadata path segments are literal BSON values, including dotted and dollar-prefixed keys. No JavaScript, raw query string execution or filtering of fetched rows is introduced.

Implemented translation covers AND/OR/NOT, boolean constants, canonical comparisons, bounded membership, ordinal Contains/StartsWith/EndsWith and typed metadata predicates. Canonical fields are Name, Description, Version, HolonType, IsActive, IsPublic and the supported identity/audit GUID fields. Metadata supports string, bool, Int32, Int64 and Guid, including selected nullable forms, existence and explicit null. Missing/wrong-type values are nonmatches for typed predicates; NOT complements the full predicate. Arrays and stored JSON text are not traversed as objects. The provider's existing document and root ArrayOfArrays dictionary representations share native path semantics.

Metadata decimal/double/enum predicates and mixed numeric comparisons remain unsupported and reject before I/O. Exact numeric storage improvements do not automatically certify native query semantics. Private encrypted metadata remains unsearchable; reserved and explicitly queryable clear metadata retain their normal storage behavior.

### UTC metadata date increment

`DateTime`/`DateTime?` metadata paths now admit all six comparisons and bounded membership. UTC literals are captured by Core; native translation compares Int64 ticks, reading the existing storage-owned `MetaDataExactUtcTicks` companion by its exact escaped JSON pointer. A record without that companion has only its actual BSON millisecond value, converted to ticks without claiming finer precision. This is one query over the existing storage representation; no date codec, receipt format or provider write path changes.

Only native BSON dates are in this typed domain. ISO-looking strings and numeric epochs are not inferred as dates. Missing/wrong-type values remain false for atomic predicates, explicit nullable null differs from non-null values, and NOT complements the whole predicate. String markers keep their ordinary semantics. Canonical audit-date fields, SQLite native dates and cross-provider date representation policy remain separate open requirements.

The original controlled red gate fails2/passes7; both failures are unsupported date capability. Final complete controlled193/193 and expanded native3/3 pass with matching Core/Shared/Mongo consumer artifacts in `TestResults/CosmicMongoCurrent/mongo-cql-date-final-artifacts.json`. Native vectors distinguish dates one tick apart, every comparison, pre-1970 submillisecond values, minimum/maximum DateTime, escaped pointer labels, explicit null/missing/wrong-type values, nullable membership and NOT in both dictionary layouts. Successful teardown removes the owned fixture and stops its runtime; the runner also bounded its owned Mongo log from12MB to a65KB tail. Red and final controlled reports are `mongo-cql-date-red.trx` and `mongo-cql-date-final-unit-full.trx`; native output remains `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`. This targeted gate does not claim a new full84-case native run; Core source and deployment pins are unchanged.

### GUID metadata increment

The translator now admits Guid/Guid? equality, inequality and membership. Canonical 36-character D text compares by GUID identity regardless of hex case; ordinary string markers stay ordinal. Native standard UUID subtype4 and CSharpLegacy subtype3 compare against independently bound byte representations, matching the provider's existing metadata reader. GUIDs in other text formats, malformed text and generic binary stay outside this typed domain. Explicit nullable null and missing/NOT semantics retain the scalar contract. Regex input and lowercase conversion are guarded by native BSON type checks.

The binary-domain guard uses inclusive 16-byte zero-to-FF bounds for each supported subtype. MongoDB's documented [BinData comparison order](https://www.mongodb.com/docs/manual/reference/bson-type-comparison-order/#bindata) is length, then subtype, then unsigned bytes; this excludes unrelated binary data without server JavaScript or fetching rows for filtering. Query node/depth/byte budgets still apply after membership expands each GUID into its three admitted representations.

Final complete controlled suite passes191/191 and expanded native CQL gate passes3/3, zero skips. The first red gate had two capability failures, one invalid test expression (captured array indexing, corrected to a captured scalar), and four passing controls; it must not be presented as three production defects. Native matrices exercise both dictionary layouts and 17 stored-value vectors across equality, inequality, nullable predicates, membership and NOT. Existing conditional write-back and payload-budget proofs also pass. The test restores its raw metadata probes before ordinary write-back; successful teardown removes the owned fixture and stops its runtime.

Reports: `TestResults/CosmicMongoCurrent/mongo-cql-guid-red.trx`, `mongo-cql-guid-paired-unit-full.trx`, and `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`. `TestResults/CosmicMongoCurrent/mongo-cql-guid-paired-artifacts.json` verifies identical Core/Shared/Mongo assemblies across controlled and native consumers. The runner rebuilt Shared after the initial controlled gate, so that earlier191-case epoch is retained separately and is not substituted for the final paired gate. Core source and deployment pins are unchanged. This targeted increment does not claim a new full84-case native regression or full-roadmap completion.

## Bounds and result handling

The adapter checks exclusive MongoDB targeting, result budget 1–10,000, cancellation, expression node/depth bounds, BSON depth and one-MiB compiled query size. Native queries have a 30-second server execution limit and stream batches of at most 16 records, admitting at most 64 MiB of projected BSON before typed conversion/publication. Count or byte overflow returns an error without partial rows. Cancellation is checked while streaming and mapping. Children are excluded from query projection; the native mapper returns leaf holons. The payload-overflow native proof passes with nine independently committed synthetic 8-MiB records and a query budget above their count. MongoDB 5.0 or later is required for the [getField expression](https://www.mongodb.com/docs/manual/reference/operator/aggregation/getfield/); the retained native runtime is 8.3.11.

Complex expression predicates may scan rather than use existing indexes. Index-aware planning, explain/cost admission, authorization/routing discovery, freshness/conflict policy, relationships/child-loading options, ordering/pagination, projection, aggregates and federation remain on the original roadmap.

## Verification

Follow-on comparison audit: new native vectors exposed an omitted null branch in MongoDB inequality translation. Canonical inequality now complements equality (including a null Name), while sparse typed metadata inequality excludes missing/wrong types but includes explicit null for nullable/string paths against a non-null literal. The existing SQLite scalar contract specifies this distinction. The original native regression is retained as `TestResults/SubscriptionUsageCurrent/mongo-cql-null-inequality-red-3D9F229406D9.trx`. After independently verifying/removing the exact five-row pre-write fixture, the corrected translator passes **188/188 controlled and 3/3 expanded native cases**, zero skips. Added vectors cover canonical string/integer/GUID/bool inequality, nullable metadata int/string nulls and negation. No Core/pin change or mutation replay is included.

This follow-on's reports are `TestResults/CosmicMongoCurrent/mongo-cql-null-inequality-unit-full.trx` and `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`; exact pre-write evidence is `mongo-cql-prewrite-semantics-reconciliation.json` in that native report directory. `TestResults/CosmicMongoCurrent/mongo-cql-null-inequality-artifacts.json` records matching unit/native Core `51A35ED9BDC5C1A1A36CF132B8F8F9195175096698D7A4556278E0E0AAF60270`, Shared `BA06C2AE583F81B846ECDD6EB24572A574F5A411B44B82EF205A62D14EACE862`, MongoDB `B0A11FFA9D89DD0FEACAE4522091FC7954A0743077240590322DF36D5491231E`. The broad five-suite totals elsewhere in this document retain their preceding cohort; this focused fix re-verifies all affected CQL cases and the complete Mongo unit suite.

Final reports: `TestResults/CosmicMongoCurrent/mongo-cql-paired-unit-full.trx` (188/188), `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx` (3/3), and `TestResults/CosmicMongoCurrent/cql-custom-property-committed-core-full.trx` (1,536/1,536). Controlled cases cover invalid plans/providers/limits, cancellation, absent repository admission, actual-profile rejection of an overbroad decimal-metadata plan, NUL field-name rejection, combined query-literal size bounds and complete translation before repository access.

The two native semantics matrices cover document and existing ArrayOfArrays metadata, large integers, explicit nullable predicates, null versus missing values, literal keys, Unicode/NUL strings, clean leaf tracking, no-op saves and conditional write-back retaining an unselected child. Direct annotated-member queries hydrate custom scalar properties while preserving canonical fields against metadata shadowing; edited custom properties save conditionally and become clean. The third case proves the streamed byte budget returns no partial rows and changes no persisted count.

Exact matching Mongo unit/native artifacts are in `TestResults/CosmicMongoCurrent/mongo-cql-paired-artifacts.json`: Core `51A35ED9BDC5C1A1A36CF132B8F8F9195175096698D7A4556278E0E0AAF60270`, Shared `134E43C6AB09A15FD69477777A863AE658701E1B1D426153F0BA4018A1F1F0ED`, MongoDB `3289BC432F37B343A5628792A013D2E138A862AAF05E0C7A1D5ACAC7627EBAC8`.

The subsequent unified regression cohort supersedes that initial Mongo-only artifact epoch. `TestResults/CosmicMongoCurrent/cql-final-regression-artifacts.json` records Core `51A35ED9BDC5C1A1A36CF132B8F8F9195175096698D7A4556278E0E0AAF60270`, Shared `7D6254118220A8438B0458FCDF72A6B31FC582EFE8CC2B3750FDFEA132BF9A4A`, MongoDB `73B2055C9A91183826D447BC4FD22ACD724BED72CAEF28CC9271BD8F3660767E`, SQLite `64255797DD8B403E2AFF92102DE2CAD6414592E3EE2B8FF534385BC5452AB6F2`. Core/Shared match across Core, Shared, Mongo unit/native and SQLite native consumers. Final additional reports: `TestResults/SharedCurrent/cql-custom-property-shared-full.trx` (895), `TestResults/CosmicMongoCurrent/mongo-cql-regression-paired-unit-full.trx` (188), `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` (84), and `TestResults/SQLiteCurrent/cql-custom-property-committed-native-full.trx` (1,042). Mongo's selected gate explicitly excludes primary-termination schedules and owned-fixture reconciliation, which retain their separate scope; test discovery independently confirmed all 84 selected cases, including CQL and no reconciliation cases.

Earlier failures are retained in place: the initial two-case report, oversized atomic seed rejection (native code 388), and direct typed-property compiler rejection. Independent majority reads first proved all activation collections empty after the rejected seed, then separately verified the five-row synthetic graph, full business/system metadata, native parent link, original version-3 root receipt/checksum and operation fence after the compiler failure. Declared ChildrenIds was validated as its separate DTO value, not mistaken for the native ParentHolonId graph owner. Only after complete checks were the exact owned fixtures removed. Evidence is in `mongo-cql-oversized-seed-reconciliation.json`, `mongo-cql-typed-compiler-reconciliation.json` and the reconciliation TRX under TestResults/SubscriptionUsageCurrent. Successful final tests removed their own fixtures and stopped the owned runtime. No blind reset or mutation replay was used.


## Native selected Min/Max

The typed SDK scalar API now supports Min and Max over admitted canonical or metadata paths. It reuses projection cell/domain compilation and the exact decoder, including nullable underlying types. Native `$min`/`$max` preserve the normalized comparison key and original typed value together; separate invalid/present reductions inspect every participating input. A malformed value cannot hide behind a valid winning value. Mandatory traversal errors stay outside source paging. Count and extrema use one cursor owner for membership snapshots, cancellation, remaining MaxTime and no-spill limits.

Final verification passes **124 native CQL tests and all 237 provider unit tests, zero skips**. The 33 new extrema cases cover all 19 reference vectors plus exact/empty/window/no-hydration, graph-output-cap independence, snapshot reparenting, actual-aggregate cancellation/reuse, traversal errors outside the window, UUID representations, duplicate metadata and ambiguous UTC precision. [Full contract and evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-minmax-slice).

Run the fixed reusable native CQL matrix under the shared build lease:

```powershell
& ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 `
    -Owner cosmic-mongo-cql `
    -Operation {
        & ./Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CosmicCql
    }
```

The runner requires the existing pinned MongoDB runtime and owns its fixed 27029 environment lifecycle. It now requires all 144 selected tests without skips, refuses occupied fixtures and retains uncertain write outcomes. Acknowledged fixtures are dropped by their existing owner; shared runtime/data caches are retained. Selected-scalar HTTP/demos and later aggregate operators remain separate roadmap gates.


## MongoDB numeric checkpoint — 23 October 2026

Native Sum/Average now use exact signed limb aggregation through the existing scalar read/snapshot/cancellation owner. The final Mongo suite passes **144 native tests and 245 unit tests, zero skips**;20 native and 8 decoder controls are new. Core and SQLite numerical support remain verified from their earlier checkpoints. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Neo4j numeric support, numeric HTTP/Decimal wire and demos, GroupBy, composition/federation, strict blocked-I/O interruption and the remaining roadmap gates are still open. This checkpoint is not full CQL/ORM completion.


## MongoDB grouped native read checkpoint - 23 October 2026

MongoDB now implements ICosmicQueryGroupedStorageProvider with independent grouped planning/readmission. It shares projection's bounded selected-row read owner for deadline/cancellation, snapshots, protected membership checks, traversal outcomes and buffering. Empty selected windows retain mandatory protected validation. Native cursor batch size is one. Counts and extrema reduce natively; integer Sum/Average use the existing exact Decimal128 signed-limb/shared Core normalization path. Source windows precede reduction; work and group sentinels reject excess without partial publication.

Verification: producer/consumer build, **260 unit tests and 147 native CQL regression tests passed, zero skips**. The native total includes three new grouped controls: all six aggregate families without hydration, ordered source windows and excess-group rejection. Evidence: TestResults/MongoCurrent/cql-grouped-read-owner-final-current.trx, cql-grouped-native-final-current.trx, cql-grouped-native-regression-current.trx; TestResults/CosmicCqlDemo/mongo-group-{read-rebuild,native-final,native-regression}.log. Producer/consumer hashes in TestResults/MongoCurrent/grouped-native-artifacts.json: Core6D193362, Shared996EE8E6, MongoB9500284. Owned fixture absent and retained Mongo process stopped after verified identity and admin shutdown.

This is initial native integration evidence. Expanded grouped composite/null/date/GUID/visibility/cancellation/overflow acceptance is still open, as are Neo4j native grouping, grouped HTTP/demos and post-group composition. The overall grouping gate remains unchecked.


## MongoDB expanded grouped acceptance - 23 October 2026

All **16 selected native grouping tests pass, zero skips** (13 new boundary cases plus the three initial native controls). This test-only batch requires no production change. Coverage now includes composite identities; explicit NULL/missing equivalence; empty nullable Min/Max/Sum/Average; exact cancelling Int64 totals and Decimal averages; true final overflow; 137 participating rows under one output-group budget; access filtering before malformed hidden values can participate; invalid participating keys/values; Unicode scalar ordering; GUID identity across string case; distinct 100 ns timestamp keys; protected traversal limits despite Take(0); pre-cancellation and subsequent reuse; payload exhaustion and zero remaining group/output budgets.

Evidence: TestResults/MongoCurrent/cql-grouped-boundaries-final-current.trx and TestResults/CosmicCqlDemo/mongo-group-boundaries-final.log (earlier11/15 controls also pass). The prior full147 native CQL regression/260 unit evidence remains the production-code checkpoint; no new full160-suite run is claimed. Strict mid-I/O interruption and grouped snapshot race controls remain separate acceptance work; this pre-cancel test does not prove them. Neo4j native grouping, grouped HTTP/demos and post-group composition remain open.


## MongoDB native grouped ordering - 24 October 2026

MongoDB now enrolls exact grouped ordering for supported key domains, Count/LongCount, Min/Max and integer Sum result domains. OrderBy/ThenBy operate on reduced selected columns; complete normalized group identity is the final ascending tie-breaker. Exact integer sum order recombines signed limbs in Decimal128: bounded totals occupy at most29 digits, within its34-digit exact domain. No Double conversion or client sorting is used. Private sort fields are removed before strict packet decoding. Source windows and global input-work proofs remain before sorting/output limits.

Native Average ordering remains explicitly unsupported until normalized Decimal output-order semantics are implemented; its unrounded native quotient cannot silently substitute for Core's rounded Decimal result. SQLite/Neo4j grouped ordering, HTTP result-order fields and post-group filters/pages remain open.

Verification: **262 Mongo unit tests and162 full native CQL tests passed, zero skips**. The18 selected grouped controls include new count/composite-identity ordering and adjacent Int64 sum values beyond2^53. Evidence: TestResults/MongoCurrent/cql-grouped-order-{unit,native,regression}-current.trx, group-order-artifacts.json; TestResults/CosmicCqlDemo/mongo-group-order-{build,native,regression}.log. The explanatory total-digit comment was corrected from28 to29 after verification; arithmetic is unchanged. Actual fixture database absent and owned Mongo process stopped after identity-verified admin teardown. This is Mongo ordering evidence, not all-provider post-group completion.


## MongoDB native grouped paging — 24 October 2026

MongoDB now enrolls grouped Skip/Take. An ordered $setWindowFields computes one-based group positions with complete normalized identity ties; a Boolean resultMember marks the composed result window. The complete-group limit sentinel stays independent of the requested page. Temporary position/sort fields are removed before strict decoding. Every bounded proof packet is decoded, including off-page domains and exact arithmetic; Core verifies native marker agreement and all-buffer invariants before constructing only selected results. Off-page rows consume global group/payload budgets, preserving the shared proof contract already used by SQLite.

Verification: **295 Mongo unit tests and168 full native CQL tests passed, zero skips**. Six new native cases cover selected ranks/complete proof, malformed off-page aggregate for Take0/Take1, overflow in a skipped group, excess complete-group budget on an empty page, and independent source/result windows. Two new unit controls cover rank/sentinel compilation and strict Boolean marker decoding/off-page validation. Existing native privacy, traversal, deadlines, snapshots and read-ownership gates remain green in this full cohort.

Evidence: TestResults/MongoCurrent/group-page-unit-current.trx and group-page-artifacts.json; TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx; TestResults/CosmicCqlDemo/mongo-group-page-{unit,native}.log. Actual23680terminal0. Producer/native copies match Core051A066408A38315DD1D60353D2F44A4C6A237AB414AA894A540C874B53A7FA4 and Mongo472F71B36C5AE638E0780BBFE08C5507154EF19AE3CACBA737076600A6F3690E. Tests acknowledged fixture database cleanup; runner-owned Mongo67060 exited and27029 listener verified absent afterward. Retained data/runtime are reusable. The runner's stale CosmicCql count144 is corrected to the verified168.

Neo4j paging, HTTP paging fields/demos, post-group filters and exact Average ordering remain open. This is Mongo paging certification, not full CQL/ORM completion.


## MongoDB native grouped filters — 24 October2026

MongoDB now enrolls grouped Where over selected keys, Count/LongCount, Min/Max and exact integer Sum, including nullable domains. Clauses support comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Integer sums use the same exact Decimal128 reduction expression as grouped ordering; bounded totals fit its precision. Decimal literals retain exact Decimal128 values. Native matching ranks count only accepted groups for paging, while complete ordered proof remains subject to domain/work/group/payload checks. No $match discards reduced proof. Average filtering remains explicitly unsupported pending normalized Decimal semantics.

The first full native run found one GUID mismatch: normalized grouping keys use32hex characters while ordinary GUID literals contain hyphens. Grouped filter literals now use the existing ComparableGuid owner, covering equality and membership consistently. No parallel encoding rule or fallback was added.

Verification: **385 Mongo unit tests and183 full native CQL tests passed, zero skips**, including15new native cases and3new unit controls. Coverage includes complete proof, aggregate combinations, matching-rank pages, nullable negation/fractional inequality, exact adjacent Int64 values and both limits, negative borrowing, binary64 subnormals, numeric/string membership, literal wildcard/quote text, null/empty suffix semantics, Boolean/GUID/enum/UTC-tick key equality+membership, excluded malformed/overflow groups, full group limits and explicit Average denial.

Evidence: TestResults/MongoCurrent/group-filter-unit-final-current.trx and group-filter-artifacts.json; TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx; TestResults/CosmicCqlDemo/mongo-group-filter-{unit-final,native-final}.log. Initial82274terminal1:182pass/1Guidred; failure evidence retained in MongoCurrent/group-filter-native-red-current.trx and mongo-group-filter-native.log. Final20476terminal0 after normalization fix and expanded key-membership assertions. Producer/native copies match Core318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73 and MongoDD4FB0C9AA25AF7481659D33D8A1F295750D52BD31E7147291686555236AD5E0; exact paths/measured bytes in artifact report. Fixtures acknowledged cleanup; runner-owned Mongo stopped and27029 listener verified absent. Runtime/data reused, no new environment/download/restore.

SQLite and MongoDB native grouped filters are verified. Neo4j filters, HTTP filters/demos and exact Average semantics remain open; full CQL/ORM completion is not claimed.

## MongoDB exact Average checkpoint — 24 October 2026

Actual test session62749 terminated successfully: **191 native CQL tests and427 provider unit tests passed, zero failures/skips**. Eight new native cases include524 packet-level comparisons against Core, count and Int64 limits, both signs, repeating fractions, halfway-even/odd rounding, coefficient-overflow rounding, all four integer input declarations, nullable membership/field comparisons, exact ordering and matching-group pages. Native normalization does not use server JavaScript or a binary64 intermediate.

Evidence: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`, `TestResults/MongoCurrent/average-final-unit-current.trx` and `average-artifacts.json`; logs under `TestResults/CosmicCqlDemo/mongo-average-{native,final-unit}.log`. Actual producer/native/unit copies match Core SHA256 `4C58BC9D...`, Shared `D6AFCA32...` and Mongo `CB31EA51...`; the artifact report contains exact paths, sizes and full hashes. The runner stopped owned27029 and acknowledged fixture teardown; retained runtime/data remain reusable. This certifies native Average comparisons for MongoDB. New live HTTP Average examples and Neo4j support remain outstanding; it is not full CQL/ORM completion.
