# TON public-source qualification

Updated 1 November 2026. Research only; no TON CQL or hosted admission claim.

The existing TON provider is a candidate after Arweave's hosted rollout. Exactly two anonymous bounded GET requests to toncenter.com succeeded with HTTP200 and protocol ok=true: masterchain info (557bytes) and explicit historical header10000000 (859bytes). Existing guarded HTTP/Core/PowerShell runtime reused; no wallet, account, source owner, database or environment created. Requests were separated by1.1seconds for the documented anonymous1RPS limit; no retry or alternate endpoint.

Evidence stays outside Docs: TestResults/TonPublicSourceCandidateCurrent/shape-observation.json and two public response files. Identity response SHA256ccca9d9f6d0c75218577c57c7bbe7ce195935f4b7deccb67fa461e88be294f7e; header SHA25620849b0ad32e148b902e3428f361983f46116971430e5813cf14761944cecf00.

## Contract observations and next gates

- Historical header reports workchain-1, shard -9223372036854775808, seqno10000000 and global_id-239. Its logical times are decimal strings; sequence/time/hash-short fields are JSON integer tokens. Preserve exact domains, canonical padded32byte base64 hashes and complete requested block identity.
- Mainnet identity init root/file hashes match the official public global-config zero-state hashes. However, masterchain-info init.shard reports string0 while the official zero-state shard is numeric -9223372036854775808. Record this discrepancy; do not claim complete descriptor equality or silently normalize it. Establish the intended state-ID contract before implementation.
- Qualify three explicit historical headers and a two-key selection, requested root/file identity and parent relationships. Do not discover/scan arbitrary chain history or create a node/wallet.
- Then add capability-aware read-only CQL, controlled/native tests, spaced shared-manager C# demo, API/tariff admission, local HTTP/Mongo federation/privacy and actual candidate checks before any hosted release.
- RPC-reported headers do not independently verify consensus. No native transaction or full ORM/HyperDrive completion is implied.

Primary contracts: [masterchain info](https://docs.ton.org/api/v2/rpc/get-masterchain-info), [header](https://docs.ton.org/api/v2/rpc/get-block-header), [official Mainnet config](https://ton.org/global-config.json). [Current task](CURRENT_STATUS.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Three native historical headers observed

Headers10000000/10000001/10000002 all report Mainnet global_id-239/masterchain-1 and exact requested sequence numbers. Returned parent descriptors match consecutive selected block IDs, and root/file hashes are canonical32byte padded base64; logical-time strings are exact UInt64 decimal values. Protocol ok=true and HTTP200 for all three. This is native wire evidence, not CQL or independent proof verification.
- header-10000001: 860bytes; SHA25643f159f14d80097d32f1848c3a3c53f0aca8bb57a4f4c4c9632d802d7bf91266.
- header-10000002: 860bytes; SHA2562e0f5b287c691c1f5469411752829bb949f6a5715df7bddeb8ea9e303a32f7d5.
The official tonlib source confirms masterchain info converts a zeroStateIdExt into its HTTP-facing BlockIdExt representation; the exact conversion overload remains to inspect before declaring the shard discrepancy resolved. Official source SHA2566d2688e8b00555b1d2539735df47cd59f190b976ad390d069c92c527d00b015c,306736bytes, inspected in memory only.

## Identity representation resolved; native3/2 complete

The official tonlib conversion overload explicitly represents zeroStateIdExt as BlockIdExt with shard0/seqno0 while retaining workchain/root/file hashes. This explains the observed init descriptor without normalizing it into a real masterchain block. A future adapter must validate the explicit wire representation plus the pinned Mainnet workchain/root/file anchors, and independently validate real header shard/seqno/root/file/global_id. This is an upstream representation contract, not independent consensus verification.

Both explicit two-record subset requests included documented root_hash/file_hash parameters and returned payloads exactly equal to their original historical headers. Native3/2 wire qualification now passes; CQL remains unimplemented. TestResults/TonPublicSourceCandidateCurrent/wire-qualification.json records source hash and conversion line6037. [Official conversion implementation](https://github.com/ton-blockchain/ton/blob/master/tonlib/tonlib/TonlibClient.cpp).
- subset-10000000: 859bytes; SHA256da859821f9ef0150a279b771130634a5ca55145ee066881f161c765820d4114a.
- subset-10000002: 859bytes; SHA256b22dd7affda8775210c9ed73c0bdebcb21c415bf29d043f53652d35e7773023d.

Implementation has not started: the large proposed editing command was rejected by automatic tool review with only "blocked by policy", no detailed reason. Subsequent git status and exact target absence confirm no TON source/project edits or environments were created. This is agent-owned tooling work; no David action. Continue with small reviewable source edits after the Arweave hosted gate.

## Exact header contract implemented and tested

Smaller apply_patch source edits passed tool review. TonPublicHeaderReference validates pinned Mainnet zero-state wire identity, the complete requested block descriptor, predecessor shape, native UInt32 fields/UInt64 logical-time strings and duplicate JSON properties.23controlled tests and1decoder test over the original native identity/three headers/two pinned subset responses pass,0skips. No new HTTP requests. This is the decoder foundation, not a connected CQL adapter, so implementation/hosted counts are unchanged.
- TestResults\TonPublicSourceCandidateCurrent\contract-controls.trx: 23passed, 35629bytes, SHA256b32a0899e24afce880ea0b2cdf98683d7805affd6f08f4ca5e0501a2e7cf3e45.
- TestResults\TonPublicSourceCandidateCurrent\contract-captured.trx: 1passed, 2791bytes, SHA2569a9955e0619aa070f2d69d0613e293f5346bb807735a5dc3402972a5e2fdebec.
Next: connect this tested decoder to bounded read-only native key selection with one endpoint-wide anonymous pacing owner, then shared planner/manager tests and spaced demo before API/hosted admission. Keep Arweave rollout separate.

## CQL candidate: controls pass, live HTTP429 gate remains red

Bounded native root-hash selection now uses complete configured sequence/root/file descriptors, the tested decoder, capability admission, deterministic paging after validation and a process-wide1.1second request-start gate.12new controlled query checks pass,0skips (previous green decoder tests not rerun). Actual native activation failed on a selected-header request with HTTP429 after Mainnet identity passed; no automatic retry, alternate endpoint or native-success claim. Preserve original failed response evidence. Implementation/hosted totals remain40/37 until the separate Arweave milestone updates hosted totals.

The candidate is isolated on work/cosmic-cql-ton-public and must not be merged/deployed as qualified TON support. Next establish an evidenced admissible free native endpoint/access policy before another explicit live check; then finish manager/CLI/API/hosted gates. No David request has been made; this is agent-owned provider qualification.
- TestResults\TonPublicSourceCandidateCurrent\cql-controls.trx: 12passed/0failed, 19892bytes, SHA25685c654599ef9b201d277d4524e5cf7f0d46be02e79d50f375058148a3731c0a8.
- TestResults\TonPublicSourceCandidateCurrent\cql-native.trx: 0passed/1failed, 4564bytes, SHA25645215a8d8cca58b6d5d0bd1a116729ac820ea0476d826eda14cc9340e81f6187.

Failure-location correction: original stack ReadAsync line240 in candidate2764d64e1 is the getBlockHeader request; identity validation is line231 and had returned. Earlier summary wording incorrectly called this the identity request. Raw failure/TRX are unchanged; derived summary corrected. Existing generic Web3 signed-provider scaffolding is not proof of TON-native storage; the independent read candidate grants no signed-write certification.
