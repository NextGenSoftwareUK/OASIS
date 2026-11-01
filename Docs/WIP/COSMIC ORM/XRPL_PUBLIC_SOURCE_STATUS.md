# XRPL public header source

Updated1November2026. Readiness passed; CQL adapter, HTTP/Mongo and hosted/browser acceptance remain red.

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

## Next implementation

Implement `XrplPublicHeaderMetadataSource` in the existing XRPLOASIS provider, separate from canonical signed memo storage. Exact planned source schema: schema1, rpcUrl, networkId0, ledgerHashes, sourceOwner. Query metadata key `XRPLLedgerHash` uses native uppercase64hex. Add equality/bounded membership and full-selection/error/access/window controls, then a spaced/commented C#/CLI walkthrough proving3/2 through HolonManager/LINQ. Next local HTTP/Mongo5/4, four shipping-planned presets, explicit unsigned-read admission, guarded hosted rollout/isolation/replicas and a live website card.

The existing provider's archive-account default is a placeholder; do not imply the public header profile configures signed memo persistence. Keep legacy implementation issues documented rather than altering legacy paths.

Counts remain29implemented query types,27hosted HTTP and5browser proofs. This preflight grants no CQL capability or permission.

## Primary protocol references

- [Official public servers](https://xrpl.org/docs/tutorials/public-servers).
- [Server info](https://xrpl.org/docs/references/http-websocket-apis/public-api-methods/server-info-methods/server_info).
- [Ledger header fields](https://xrpl.org/docs/references/protocol/ledger-data/ledger-header).
- [XRPLF native serialization/hash implementation](https://github.com/XRPLF/rippled/blob/develop/src/libxrpl/protocol/LedgerHeader.cpp) and [hash prefix](https://github.com/XRPLF/rippled/blob/develop/include/xrpl/protocol/HashPrefix.h).
