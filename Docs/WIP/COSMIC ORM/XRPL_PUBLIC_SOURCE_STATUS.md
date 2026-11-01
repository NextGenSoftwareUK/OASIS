# XRPL public header source

Updated1November2026. Native CQL/C#/CLI passed; HTTP/Mongo and hosted/browser acceptance remain red.

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

## Next implementation

Add explicit lazy API read-profile registration, independent unsigned-read admission/tariff, and local HTTP/Mongo5/4 plus isolation. Build/test four exact publisher presets, then guarded hosted rollout/replicas and a website card with live/local links. Browser verification remains separate. Preserve the original source owner/catalogue; never regenerate it or replay successful save cohorts.

The canonical provider's archive-account placeholder and signed memo persistence remain separate and uncertified by this public profile. Counts now30implemented query types (XRPL native-only),29deployed tariff types,27hosted HTTP and5browser proofs. No XRPL hosted permission or availability claim.

## Primary protocol references

- [Official public servers](https://xrpl.org/docs/tutorials/public-servers).
- [Server info](https://xrpl.org/docs/references/http-websocket-apis/public-api-methods/server-info-methods/server_info).
- [Ledger header fields](https://xrpl.org/docs/references/protocol/ledger-data/ledger-header).
- [XRPLF native serialization/hash implementation](https://github.com/XRPLF/rippled/blob/develop/src/libxrpl/protocol/LedgerHeader.cpp) and [hash prefix](https://github.com/XRPLF/rippled/blob/develop/include/xrpl/protocol/HashPrefix.h).
