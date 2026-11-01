# Bitcoin public source readiness

Updated 1 November 2026. Native CQL adapter, shared-manager C# walkthrough and actual CLI passed; HTTP/Mongo pairing and hosting remain incomplete.

## Verified scope

- Anonymous https://blockstream.info/api; six bounded GET requests, no account, source writes or installation.
- Heights 0, 1 and 2; pinned Bitcoin mainnet genesis, exact 80-byte headers, linked parent identities, recomputed double-SHA256 and pinned genesis-era difficulty/proof of work.
- Seven deterministic controls pass, including mutated identity, disconnected ancestry, malformed encoding, invalid signed target and self-consistent identity without valid work.
- This is a small public historical sample. It does not independently validate full Bitcoin consensus, current best-chain selection, transactions or OASIS persistence on Bitcoin.

## Evidence

- Raw report: `TestResults/BitcoinPublicSourceCandidateCurrent/readiness.json`, 2151 bytes; SHA256 `8f05efdf1c7ffa898d611404605588f7d8274d59c65b24867a83dd100c230892`.
- Regenerate preflight: `python -B Scripts/Sandbox/Probe-BitcoinPublicReadiness.py`.
- Controls: `python -B -m unittest discover -s Scripts/Sandbox -p test_bitcoin_public_readiness.py`.
- Primary API specification: [Blockstream Esplora](https://github.com/Blockstream/esplora/blob/master/API.md).

## CQL / developer demo milestone

The new read-only `BitcoinPublicHeaderMetadataSource` implements capability-aware equality and membership over one to eight configured `BitcoinBlockHash` values. It verifies native header identity/work, pins genesis and requires API-reported best-chain membership. Unsupported scans/sorting/predicates are rejected. Complete selection errors precede paging/publication; tests include empty-window failures, malformed/oversized/duplicate responses, unknown keys, cancellation, missing activation, budget, orphaned headers and invalid targets/work with matching identities.

The C# walkthrough and standalone CLI both returned3/2 through HolonManager/LINQ, using the retained source identity. [Developer guide](../../Devs/BITCOIN_PUBLIC_HEADER_CQL_GUIDE.md).

- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-source-final.trx`: 20 passed, zero skipped; SHA256 `8f2ff3db82b9d6e7e24e7a2f9204466964c5e86c97398e35f4d7cabd3887fc5a`.
- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-native.trx`: 1 passed, zero skipped; SHA256 `bb6b9c5e49b587a515b77898196bfe1b13033069aa5cdc12c7502a5eb78eb7b7`.
- Actual CLI output `TestResults\BitcoinPublicSourceCandidateCurrent/console-run.log`: full3/filter2, exit0; SHA256 `103eeb9f671716c1b1bd90271d0341fb8be606f732713952202336d3837ba4d3`.

Initial failures are preserved in `tests.log` (two obsolete pre-existing unit-test API assumptions) and `console-build.log` (new command used bare return in an integer-returning entry point). The assertions now use current enum/category contracts; the command returns0. Corrected test/build logs are separate. No implementation fallback or disabled test introduced.

## Next acceptance work

Add lazy API factory registration and explicit project reference, read-only tariff/admission, local HTTP/Mongo federation and four shipping-planned presets. Then guarded hosted rollout and independent visitor isolation/replication checks. Retain one ready Mongo-paired preset; browser proof is separate.

Bitcoin is not yet in the live provider count: totals remain26 scoped hosted HTTP and5 browser proofs. Native adapter implementation count is now29; deployed tariff catalogue remains28.
