# Bitcoin public source readiness

Updated 1 November 2026. Qualification passed; CQL adapter, demo and hosting remain incomplete.

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

## Next acceptance work

Implement a bounded Bitcoin header metadata CQL source with explicit configured hash scope and capability declarations, then native tests and readable C#/CLI demos. Follow with local HTTP/Mongo pairing, four shipping-planned presets, hosted admission and independent visitor isolation/replication checks. Retain one federated Mongo preset. Browser proof is separate.

Do not add Bitcoin to the live provider count until hosted evidence passes. Current totals remain 26 scoped hosted HTTP providers and 5 browser proofs.
