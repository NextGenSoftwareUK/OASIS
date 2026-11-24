# QuestDB public demo qualification

Updated2November2026. Native/CQL/hosted/browser acceptance not yet run.

## Contract and free access

Official QuestDB docs describe demo.questdb.io as read-only. Retained REST and
demo-schema source at questdb/documentation commit
ead9ae6f8656e45793f018eee243a4b81d7b1895 lives with SHAmanifest under
LocalState/COSMIC/QuestDbPublicQualificationCurrent/sdk.

[REST contract](https://questdb.com/docs/connect/compatibility/rest-api/)
now documents /api/v1/sql/execute and /execute; /exec is deprecated. Choose one
explicit qualified endpoint/version; never silently switch on errors. JSON uses
columns/dataset/count, with schema/type metadata separate from row arrays.

[Demo data schema](https://questdb.com/docs/cookbook/demo-data-schema/)
distinguishes simulated FX data from real crypto trades. Native price columns
are DOUBLE, so do not claim exact financial decimal arithmetic or reinterpret
binary floating values as lossless money. The initial candidate can use native
symbol metadata (BTC-USDT/ETH-USDT/SOL-USDT), excluding price/amount data entirely.

## Next gate

Qualify the documented endpoint with bounded fixed read-only SQL: native build
identity and three/two selected existing symbols. Verify exact response schema,
unique keys/counts and error behavior. Do not treat generated long_sequence rows
as native data acceptance. Preserve first failure/denial; no fallback endpoint,
new service, account or installation. No native call or CQL grant yet.

The existing PGWire storage provider is separate and remains unchanged; public
SQL query acceptance would not certify owned holon writes or full ORM parity.
ClickHouse rollout remains immediate priority.

## Original native gate - 2 November 2026

Fiveofflinecontrols pass. FirstGET to canonical/api/v1/sql/execute forSELECTbuild() returnedHTTP403/705bytes, SHA256dec3d13427dff18664fcc42853ae2464fd0ee60f769615fd356edd610ef33367. Explicit browser-signature denial present. Remaining3/2symbolqueries notsent. No nativebuild/record/CQLacceptance. Exclusiveintent/report/compresseddenial/SHAmanifest durable QuestDbPublicQualificationCurrent; raw TestResults/QuestDbPublicSourceCurrent/response-1.json. No retry/alternateheaders/transport/route/proxy or account/runtime install. Legitimate permitted access is unresolved; do not relabel generatedrows/otherdatabase/publicAPI as QuestDBproof. Other breadth work remains, no owneraction requested.

## Recovery laptop local native fixture — 4 November 2026

The official standalone Windows QuestDB 10.1.0 runtime returned the expected
build identity through the documented REST SQL route. An isolated synthetic
symbol table returned exactly BTC-USDT, ETH-USDT and SOL-USDT in the full query,
and BTC-USDT and SOL-USDT in the membership query. This establishes native
local REST 3/2 and supplies a repeatable input for the CQL adapter. The public
demo gateway denial remains recorded separately. CQL and hosted checks are open.

Evidence: TestResults/QuestDbRecoveryLaptopCurrent/native-rest.json,
SHA256 57DC336C1E41F6137D2D29B2CF8C6791286A756948324B0BC201DB42DFB88514.

Local bounded CQL symbol adapter passed 1/1 against QuestDB 10.1.0 with
zero skips, asserting exact 3/2 provider keys via the shared query planner.
The adapter is separate from owned storage. WEB4 registration, HTTP/Mongo
federation, public gateway qualification and hosted acceptance remain open.

WEB4 enrollment requires a separate host-managed read-only storage profile
with explicit activation and disposal. The current planner adapter is not
eligible for that registry until it implements this contract. The native
REST client now applies a single 15-second deadline to headers and body reads.

The symbol source now implements the shared storage and explicit read-only
activation interfaces. Activation reads every enrolled symbol. The real
QuestDB lifecycle and exact CQL 3/2 integration test passed 1/1 with zero skips.
Host configuration and admission remain open.

The opt-in WEB4 registration built successfully. The focused admission test
failed at StorageQueryTariff.Capture before planning any native or Mongo
queries. Neither the parent-pinned Core d6ae5e7 nor the shared checkout
5415961 lists QuestDB in SupportedProviders. Extend that canonical unsigned
read list on the COSMIC Core branch, rerun the focused test against the
selected Core, and advance the parent gitlink and Railway manifest together.
The opt-in WEB4 registration was subsequently committed in `7cbc15ba3`.
It reads `CQL_QUESTDB_SYMBOL_SOURCE_JSON` and remains disabled until a
Development source is reviewed and configured.

Core task commit 58c7cc9e7e4bd65c2bb8d8336b4af364b03c9370 adds QuestDB to
the canonical unsigned read list. It was created with an isolated Git index
while preserving the shared detached checkout at 5415961. Admission must be
verified with the Core task commit before the parent pin is advanced.

Exact Core 58c7cc9 passed the focused WEB4 admission test (1/1). All four
QuestDB native and QuestDB/Mongo presets planned, and the zero-price tariff
was accepted only for the explicit query-only enrollment. The shared Core
checkout was restored to 5415961. Local HTTP execution and hosted acceptance
remain open.

Exact Core 58c7cc9 local loopback HTTP federation passed 1/1: native 3/2,
paired 5/4, foreign private 0 and foreign public 3. Two SDK-owned Mongo saves
and nine controlled receipts verified. The test injects the local source and
does not establish hosted endpoint, replication or browser acceptance.

### Hosted source gate

The WEB4 registration uses the shared public-destination HTTP guard. To enroll
QuestDB on Development, first qualify a permitted public HTTPS SQL endpoint
serving the enrolled cql_demo_symbols table and exact SYMBOL projection. The
local fixture and the previously denied public demo endpoint are separate.
After qualification, prepare the query-only tariff, public catalogue and
four presets for the exact deployment. Run the hosted native and Mongo
flow, then verify reuse, foreign privacy and exact replicas before card
publication. No hosted source has been selected.

The shipping profile expects exactly `schema`, `endpoint`, `sourceOwner`
and `symbols`. Use schema 1 and the reviewed HTTPS
`/api/v1/sql/execute` endpoint without credentials, query parameters or a
fragment in the URL. Its read requests use the `cql_demo_symbols` table.
Qualify the target with only the synthetic teaching dataset and restrict
the endpoint's SQL permissions independently of the WEB4 profile. The
WEB4 adapter bounds its own SQL and validates exact returned symbols.
That does not restrict direct callers of the endpoint. Verify that
unrelated tables and mutations are unavailable before exposing the fixture.

The recovery laptop retains the pinned QuestDB 10.1.0 runtime at
`LocalState/COSMIC/RecoveryLaptopQuestDB1010`. The local HTTP fixture's
`LocalState/COSMIC/QuestDbMongoHttpCurrent/fixture.cleaned.json` records
`databaseAbsent: true`. Reuse the pinned runtime for a local replay. For a
new, isolated qualification database, seed the same table
shape and teaching symbols with a separate provisioning account:

```sql
CREATE TABLE cql_demo_symbols (symbol SYMBOL);
INSERT INTO cql_demo_symbols VALUES ('BTC-USDT'), ('ETH-USDT'), ('SOL-USDT');
```

Use the query endpoint to verify the `SYMBOL` column type and exact keys
before preparing WEB4 settings. This schema is for a new fixture. Preserve
the original hosted gateway denial and prior local evidence.

With a reviewed HTTPS source configuration, run the guarded shipping
profile qualification from this checkout:

```powershell
$env:OASIS_QUESTDB_HOSTED_SOURCE_CONFIG = $reviewedSourceConfigPath
$project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests'
Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-questdb-https -Operation {
    dotnet test $project --filter 'FullyQualifiedName~HostedProfileReturnsExactFullAndSubsetKeys' /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'QuestDB HTTPS qualification failed' }
}
```

Require one passed test with zero skips before preparing Development
settings. The test activates the configured source and queries the exact
full and subset symbol sets through the guarded HTTPS adapter.

11 November UTC: the generic QuestDB CI job started a blank service, but Development activation validates three explicitly provisioned WAL/DEDUP tables. The workflow now provisions those tables in its disposable service before activation. Exact Development candidate `a39046a91f` is queued for focused CI. Development integration and hosted CQL remain open. [QuestDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43029367152)

11 November UTC: QuestDB focused CI passed the fixture provisioning step and generic storage integration tests on exact candidate `a39046a91f`. The companion workflow passed. Development subsequently advanced to `ff0092d851` with NEAR contract changes and no change to the QuestDB paths, workflow or dependency manifest. QuestDB is ready for the next verified Development batch. Hosted CQL acceptance remains open. [QuestDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43029367152)

11 November UTC: Development `b22d4667ba` contains the verified QuestDB CI schema provisioning and ArangoDB native restart workflows. The ArangoDB workflow is manually dispatched. QuestDB hosted CQL and ArangoDB Mongo federation and hosted CQL remain open. This batch changes CI workflows only. [QuestDB focused CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43029367152), [ArangoDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031012725)

11 November UTC: standard Development QuestDB CI passed schema provisioning and all four generic storage tests. The fifth test, NativeBoundedQueriesAndOriginalJsonSurviveRestart, failed before querying because the generic job does not provision its required retained source. The next CI fixture must cover initial seed and restart of the same owned state. Hosted CQL remains open. [Development QuestDB job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031594342/job/130559744184)

11 November UTC: isolated QuestDB restart diagnostic 6f84ff1fee is published against Development 165e8abaee. It provisions the three storage tables in a disposable pinned QuestDB 10.1.0 container, runs the existing CQL test before and after restarting the same container, and checks the original snapshot and cleanup. Hosted CQL remains open.

11 November UTC: the first restart diagnostic passed the initial native test and then stopped at shutdown verification. The original retained runner accepts JVM SIGTERM exit 143 only with native engine and listener closure events and no shutdown failure. The corrected CI candidate a130241a9a applies that contract. [Initial run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43033256552)

11 November UTC: the corrected QuestDB native CI passed before and after restarting the same pinned 10.1.0 container, one test per phase and zero skips. Both shutdowns satisfied the native engine and listener closure checks. The retained laptop source was untouched. The verified workflow is ready for the next Development batch. Hosted CQL acceptance remains open. [QuestDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43033757190)

11 November UTC: Development `73e0d4bb08` contains the verified ArangoDB MongoDB federation and QuestDB restart workflows. Both are manually dispatched. This batch changes only CI. Hosted HTTPS source admission and live browser checks remain open. [ArangoDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031787640) · [QuestDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43033757190)

11 November UTC: candidate 880937b05f routes generic ArangoDB and QuestDB cases to the standard provider matrix and calls the dedicated native fixture workflows from the same pipeline. Diagnostic 37a10ab14d exercises all four jobs. Both fixture workflows retain manual dispatch. Development integration awaits the combined result.

11 November UTC: combined database CI passed all four jobs, covering generic ArangoDB and QuestDB plus their dedicated restart and federation fixture tests. Candidate `880937b05f` is ready for the next Development batch. Hosted CQL remains open for both providers. [Combined CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43034639175)

11 November UTC: Development `1c7e670e8d` integrates the verified ArangoDB and QuestDB CI routing. The generic matrix runs the four storage tests for each provider and calls the dedicated fixture workflows for native restart and federation coverage. The three-file patch is identical to the one qualified by all four focused jobs. Hosted CQL remains open. [Combined database CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43034639175)

11 November UTC: the standard Development pipeline at `1c7e670e8d` passed all four ArangoDB and QuestDB jobs, including both generic storage jobs and both dedicated native fixture jobs. This verifies the routing in the regular pipeline. Hosted CQL acceptance remains open. [Development CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43036514678)
