# Bitcoin public source readiness

Updated 1 November 2026. Native CQL/C#/CLI and local HTTP/Mongo pairing passed; hosted rollout and browser acceptance remain incomplete.

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

## API / local HTTP acceptance

Lazy explicit `CQL_BITCOIN_PUBLIC_SOURCE_JSON` read-profile registration, independent permission/free read tariff and API compiler scope are implemented. No signed-write grant or wallet DNA change. Actual protected publisher candidate has three settings/four presets; its exact public export passes the shipping planner with real Mongo capabilities. Three pure configuration controls and one generic website preparation contract pass.

Local loopback HTTP proves native3/2 and Mongo-paired5/4, two SDK-owned private saves, nine query receipts, foreign-private0/public3, exact physical IDs/provider/visibility. Billing authentication is controlled fixture infrastructure; this is not production billing, browser or replication acceptance. The exact marker-verified database was dropped, source configuration unchanged, owned Mongo stopped and port27029 absent. Durable intent/save ACKs/cleanup confirmation remain under LocalState/COSMIC/BitcoinMongoHttpCurrent; do not rerun the successful fixture.

- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-admission.trx`: 2 passed, zero skips; SHA256 `386384f23e900f4bb660c1bceecc350e31742771dee5bd878c01942a3bfe4a45`.
- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-prepared.trx`: 1 passed, zero skips; SHA256 `204d873053b4aee7fb3125b44b463661a47b12fdac7fd302e411e25078857738`.
- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-tariff.trx`: 1 passed, zero skips; SHA256 `5ba63e939e822360c442c08ce121511678fd825a01b172932c0e7aee01599d20`.
- `TestResults\BitcoinPublicSourceCandidateCurrent\bitcoin-mongo-http.trx`: 1 passed, zero skips; SHA256 `4089b2c02e8585c137e41bd3f4d976b39bb23dbb9456cc4827c64d0619035ad3`.

Core task7317ba1 is pushed and verified remotely. Parent gitlink/dependency manifest advance together. Protected rollout before/candidate snapshots are retained under LocalState/COSMIC/HostedSourceRolloutCurrent; nothing applied yet.

## Next acceptance work

Application19ebbfe2c/Core7317ba1 published to Development, remote refs verified; exact joba64660e1-9676-405d-beaa-a0b8de327278 queued. Wait for that successful deployment, then apply the guarded three-setting candidate and journal one settings rollout. Use a new hosted acceptance cohort for independent visitor isolation and SQLite/Neo4j replica checks. Expose the hosted presets and local walkthrough links in the website, then verify browser interaction separately.

Bitcoin is not yet in the live provider count: totals remain26 scoped hosted HTTP and5 browser proofs. Native adapter implementation count and candidate tariff catalogue are29; deployed catalogue remains28 until the matching rollout succeeds.
