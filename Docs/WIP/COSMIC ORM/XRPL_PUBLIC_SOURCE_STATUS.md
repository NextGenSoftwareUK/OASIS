# XRPL public header source

Updated1November2026. Native CQL/C#/CLI and local HTTP/Mongo passed; hosted/browser acceptance remain red.

## Verified qualification

- Officially listed full-history Mainnet endpoint `https://xrplcluster.com/`, four anonymous read-only JSON-RPC requests; no account, credentials, fees or installation.
- `server_info` explicitly reports network_id0. Three closed, validated ledger headers are selected from one validated anchor through parent hashes, with exact sequence decrement.
- Native SHA512Half header identities recompute from the LWR prefix and XRPLF fixed-width field order. Parent/header/envelope identities are checked.
- Seven deterministic controls pass: golden native hash, field mutation, validated/closed flags, requested identity, envelope sequence, unsigned widths/decimal forms and duplicate/noncanonical input.
- Network identity and validation remain reported by the authenticated public endpoint. This is not independent validator/UNL consensus, account-state proof or native OASIS persistence on XRPL.

## Retained state and evidence

- Stable planned source: `LocalState/COSMIC/HostedXrplPublicSourceCurrent/source.json`, 382bytes. Preserve sourceOwner and the original three ledger hashes; no regenerated cohort after cleanup.
- Raw qualification: `TestResults/XrplPublicSourceCandidateCurrent/readiness.json`, 3085bytes; SHA256 `a6da4c631e75220e471a51da832deb74cb8b5cea86e0ccfb6131a23d7f155cc1`.
- Qualification command: `python -B Scripts/Sandbox/Probe-XrplPublicReadiness.py`.
- Controls: `python -B -m unittest discover -s Scripts/Sandbox -p test_xrpl_public_readiness.py`.

## Native CQL milestone

`XrplPublicHeaderMetadataSource` now provides read-only equality and bounded membership over configured `XRPLLedgerHash` values. It checks Mainnet0, closed/validated flags, native SHA512Half, envelope/header identity and bounded exact unsigned values. No scans, source writes or partial selections; access/window/budget/cancellation/invalid-result checks pass.

The spaced/commented [C#/CLI walkthrough](../../Devs/XRPL_PUBLIC_HEADER_CQL_GUIDE.md) proves3/2 through the shared HolonManager/LINQ pipeline. Exact total drops remain decimal strings; no floating-point conversion.

- `TestResults\XrplPublicSourceCandidateCurrent\xrpl-source-corrected.trx`: 21passed, zero skips; SHA256 `d237133bd1fdda4f1dedfb5721103d0ae1f61d58f7e177e627c19c094df2f4ba`.
- `TestResults\XrplPublicSourceCandidateCurrent\xrpl-native.trx`: 1passed, zero skips; SHA256 `31fe246c221888f086ee80d27c00e5244252e65fb0d71b8c440bea48decb2911`.
- Actual CLI3/2, exit0; `TestResults\XrplPublicSourceCandidateCurrent/console-run.log` SHA256 `fb49147b137f12f8a658c00b6ae2b736711cd5c2200f930dd026b2e8cc72f328`.

Initial controlled run2passed/19failed because the new fixture cast the documented quoted ledger index directly to a JSON numeric value. The fixture now decodes the explicit decimal representation; corrected21pass. Original failure log/TRX retained. Production total_coins type is explicitly required to be a decimal string. Two obsolete existing unit-test API assumptions (enum wrapper comparison/nonexistent GetProviderVersion) were updated to current enum/category contracts; legacy provider implementation unchanged.

## API / local HTTP milestone

Explicit lazy `CQL_XRPL_PUBLIC_SOURCE_JSON` profile, compiler scope and independent unsigned-read permission/tariff are implemented. Three-setting protected candidate preserves wallet DNA and canonical write admission; three pure configuration controls and one generic website preparation control pass. The exact candidate's four presets pass the shipping planner with native and Mongo capabilities.

Local loopback HTTP proves native3/2, two private SDK Mongo snapshots, paired5/4, nine receipts and independent visitor public3/private0. IDs, provider labels, owner and visibility are checked. Auth/billing are fixture infrastructure; this is not production billing, browser or replica acceptance. Original config unchanged; exact marker-verified database dropped, owned Mongo stopped/27029 absent. Preserve LocalState/COSMIC/XrplMongoHttpCurrent intents/save ACKs/cleaned confirmation; never replay the successful fixture.

- `TestResults\XrplPublicSourceCandidateCurrent\xrpl-admission-prepared-corrected.trx`: 3passed, zero skips; SHA256 `10fb08b52a864ad51631c9c447ecc57f72fad99a864b3d7f8c28bf3c6d225fea`.
- `TestResults\XrplPublicSourceCandidateCurrent\xrpl-tariff.trx`: 1passed, zero skips; SHA256 `dc9896fd2de71b1eb5499958708c6c8b4d86dcc451d8ae2dbc9b1bb40e163605`.
- `TestResults\XrplPublicSourceCandidateCurrent\xrpl-mongo-http.trx`: 1passed, zero skips; SHA256 `4b7c350ce6aeb228a613e8d8c326317c02843227d7e78eab6938aa23c00c44d9`.

Initial admission run1passed/2failed because an owned line-ending serialization error left XRPL out of the compiled Core tariff list. Corrected the owning list to one canonical source-line change; all3API/prepared and1Core controls now pass. Original failure log/TRX retained, no bypass/disabled test.

Coree032d0a pushed and remotely verified; parent gitlink/dependency manifest advance together. Protected before/candidate state under LocalState/COSMIC/HostedSourceRolloutCurrent is prepared only, no settings applied.

## Exact next implementation

Publish the verified parent/Core batch, wait for exact matching deployment success, then guarded3setting apply and one journalled settings rollout. Run normal-visitor hosted acceptance once with original identities/save ACKs, verify isolation and SQLite/Neo4j replicas, then publish the staged website card/local links and inspect exact public HTML. Browser proof is separate.

Counts30implemented query types; candidate30/deployed29tariff types;27hosted HTTP and5browser proofs. No XRPL hosted permission, source write or signed memo persistence claim.

## Primary protocol references

- [Official public servers](https://xrpl.org/docs/tutorials/public-servers).
- [Server info](https://xrpl.org/docs/references/http-websocket-apis/public-api-methods/server-info-methods/server_info).
- [Ledger header fields](https://xrpl.org/docs/references/protocol/ledger-data/ledger-header).
- [XRPLF native serialization/hash implementation](https://github.com/XRPLF/rippled/blob/develop/src/libxrpl/protocol/LedgerHeader.cpp) and [hash prefix](https://github.com/XRPLF/rippled/blob/develop/include/xrpl/protocol/HashPrefix.h).

Hosted rollout progress: exact application9ee52c89 SUCCESS atc6e7f14ba/Coree032d0a. Guarded3setting apply acknowledged with whole candidate equality. One journalled settings rolloutcdc70f3a-4dea-4cb7-a881-306c478056bd BUILDING on samecommit. Protected originals/intents/outcome retained; no hosted visitor/save started yet.
