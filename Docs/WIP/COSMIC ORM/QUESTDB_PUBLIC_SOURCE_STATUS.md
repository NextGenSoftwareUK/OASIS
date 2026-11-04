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
The WEB4 registration remains local and uncommitted pending that result.
