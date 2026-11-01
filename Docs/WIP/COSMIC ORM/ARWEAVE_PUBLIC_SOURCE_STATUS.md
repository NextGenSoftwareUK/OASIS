# Arweave public-source qualification

Updated 1 November 2026. Research only; not admitted to CQL or hosted demo counts.

The existing Arweave provider is the candidate after Cardano hosted acceptance. Two bounded anonymous reads against https://arweave.net returned HTTP200: `/info` (292 bytes) identifies `arweave.N.1`; `/block/height/269512` (919 bytes) supplies a historical header. No account, wallet, database, transaction or source owner was created.

Evidence: `TestResults/ArweavePublicSourceCandidateCurrent/shape-observation.json`; info SHA256 `7cdf0be7c9c275ff41dc968ed9f1925117fff24feaff959a3e17ae4617d3bacf`; block SHA256 `ba1f75e157154733d85ffd1a8ed53d45249820e6347963223200b1fb0e51e668`. Raw responses remain outside Docs.

## Next qualification

- Establish one explicit historical wire-format contract and network identity before implementation. Numeric wire types vary by era: the observed difficulty fields are decimal strings, while pool/size fields are JSON integer tokens. Preserve exact values without floating-point conversion.
- Qualify three explicit headers and a two-key subset; validate requested identity, complete sets, deterministic ordering, bounded responses and malformed-input rejection. Do not download the full block index.
- Only then implement the shared CQL read-only path, native tests/demo, API admission, four presets, private Mongo preparation, isolation/reuse/replicas and hosted acceptance.
- Gateway-reported headers do not independently prove consensus or owned native writes. No CQL, HyperDrive or browser completion is claimed.

[Native HTTP contract](https://docs.arweave.org/developers/arweave-node-server/http-api). [Current status](CURRENT_STATUS.md). [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
