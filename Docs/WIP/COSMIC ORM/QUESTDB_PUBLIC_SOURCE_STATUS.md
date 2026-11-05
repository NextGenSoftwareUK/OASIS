# QuestDB public demo qualification

Local native REST, CQL and WEB4 HTTP/Mongo acceptance passed on the recovery
laptop. Hosted endpoint qualification and hosted browser acceptance remain open.

Current local launcher address is `http://127.0.0.1:5134/`, independently
browser-verified after correcting the initial5120/Valkey overlap. New-address
consumer `interactive-3fe0c37e75ef49ee9843f35d16ba2a94` passes1/1, original source
unchanged, exact Mongo fixture removed and owned services stopped. See the
[address correction](LOCAL_DEMO_ADDRESS_CORRECTION.md) for publication status.

## Local browser continuation — 5 November 2026

The remaining local browser flow now passes using the admitted10.1.0 linked
runtime (174files/128242521bytes, retained-inventory manifest SHA256
`3e53e13800098a9b341d83449d608a391978136f2d49bb52a71e2900ed8b0bff`).
No new runtime/download. The prior disposable HTTP fixture had been removed;
the separate, explicitly initialized `RecoveryLaptopQuestDB1010/browser-source`
retains three synthetic symbols in `cql_demo_symbols`. Historical HTTP cohort
IDs/receipts and the original public gateway denial remain untouched.

Initial native source bootstrap and new manager/Mongo/HTTP preset flow pass1/1.
Retained restart with interactive browser flow passes1/1. Actual browser full
selection returns5rows (QuestDB3/Mongo2); subset returns4 (QuestDB2/Mongo2),
showing identical canonical IDs across physical provider copies and
`MovingProviderReads`. Unknown/forged presets400 and inactive503 pass. Stop
button observed, exact3-record Mongo fixture verified/removed, native QuestDB
shutdown hook reports complete/exit130, Mongo native shutdown/storage-close
exit12, and all29000/27029/5120listeners closed. Native source hash unchanged:
`6383b7a66ea48807bf1130e340688695e59b46bd832c7db00dc14eac01ba81a1`.

Runtime-root evidence: `initial-33a5c5e1aa764f8384325150e8f48280` and
`interactive-080e30d52ad14302a0cd27aa87de5a61` retain original logs/TRXs/acceptance
and native shutdown receipts. `browser-acceptance.json` records actual CUA
observations, not screenshots. Two source-admission controls exercise changed
actor/network configuration and missing admission; exact source/configuration
SHA admission is required before subsequent startup and after the consumer.

The shared teaching host now allows trusted explicit CQL presets for its full
and subset buttons, rather than imposing ID membership on a metadata-only
native source. No error-time alternative query path. Its default identity
presets remain the default when no explicit CQL preset is supplied.

This closes a local browser gap, not another provider/profile: counts remain
100local/60hostedHTTP/28hostedbrowser. Public local catalogue card/guide is next.
Hosted HTTPS restrictions/enrollment, public gateway qualification, PGWire
holon storage and full ORM parity remain open. See the
[local launcher guide](../../Devs/QUESTDB_CQL_LOCAL_DEMO.md).

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
