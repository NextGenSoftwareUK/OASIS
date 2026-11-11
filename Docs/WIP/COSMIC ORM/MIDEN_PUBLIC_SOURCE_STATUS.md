# Miden testnet public block-header source

4 November 2026. Native HTTP/2 gRPC against
`https://rpc.testnet.miden.io/miden.node.v1.NodeService/GetBlockHeaderByNumber`
returned HTTP 200 and grpc-status 0 for explicit block heights 1, 2, and 3.
The decoded heights matched all requests. Block 1's previous commitment matched
the public testnet genesis commitment
`280189b64c3a22c9d8e6e6c2cd567f58c528fee3b73724a6d855066642d7e567`.
The gRPC-Web probe returned HTTP 403. The native probe used the installed
.NET HTTP client and created no files or dependencies.

[Versioned node schema](https://github.com/0xMiden/node/blob/v0.18.2/proto/proto/rpc.proto)
and [testnet monitor](https://status.testnet.miden.io/) define the source and
network identity. This was a direct source probe without MMR proof verification.
The proposed CQL profile would expose only public testnet block headers. Use
typed protobuf decoding from the pinned schema, exact height membership, and
genesis identity validation. Verify native CQL, local WEB4 federation, and
hosted admission separately. The existing provider's REST storage contract,
private notes, and transaction submission are separate from this read scope.

The compiled generated .NET client passed a live testnet smoke for all three
headers. Typed decoding produced timestamps 1791297293, 1791297294, and
1791297297, respectively. The first header matched the pinned genesis
commitment. This check loaded the existing provider build and cached libraries
in PowerShell. It created no new runtime, build, or test output.

Before CQL admission, exercise the response-height mismatch path with a
controlled gRPC response. Require the genesis check during source activation.

The client now pins the official HTTPS testnet endpoint and uses the shared
native connection guard, with proxies and cookies disabled. The guarded build
passed and the live three-header smoke passed again, including the block 1
genesis comparison. The build reused the recovery ledger paths.

The typed client enforces the pinned genesis reference on every block 1
read. The guarded provider build passed and live heights 1, 2, and 3 passed.
Controlled typed inputs verified rejection of a wrong height, missing header,
and mismatched genesis commitment. Source activation still needs to request
block 1 even when it is outside the configured query catalogue.

The read-only CQL provider adapter builds and activates against the live
testnet. The activation smoke used heights 2 and 3, exercising the separate
block 1 identity check. The adapter projects public holons using stable IDs
and admits exact height equality and bounded membership. Compiler and WEB4
registration, native CQL execution tests, federation and hosted acceptance
remain open. Build outputs reuse the existing recovery ledger paths.

WEB4 compiler and query-only admission pass all four presets. Local WEB4
HTTP against real Miden testnet and the reused Mongo replica passed native
3/2 and federated 5/4, with zero foreign private rows, three public rows
visible to the foreign actor, and nine usage receipts. Two SDK writes were
acknowledged in the first run. The resume validated those exact writes
before querying and cleaning the owned database. The first report failed
because its output directory had not been provisioned. After provisioning
it, the same fixture passed and the replica was stopped. Hosted acceptance
remains pending.

The guarded Miden publisher validates the source digest and the local
HTTP/privacy and cleanup receipt before accessing Railway. Its configuration
tests verify the three allowed settings, four presets, preservation of write
admission, and refusal to mutate before the expected release is live.
Run Scripts/Sandbox/Publish-HostedMidenPublicConfiguration.py to prepare a
protected review only after LocalFile hosted acceptance. Apply requires a
matching deployment and the same reviewed source. Hosted verification is open.

## Development hosted HTTP acceptance — 7 November 2026

Original first cohort passed and is sealed on source88b896815/Core944d380,
settings8783d8eb. Native3/2, Mongo5/4, first2private saves, repeat0new/2existing,
foreign-private0/public3 and exact SQLite/Neo4j replicas2/2 passed. Original
owner/foreign credentials and two save ACKs retained; pending intents empty.
No native fixture replay or source writes. Browser acceptance remains open.
[Batch gates and limits](SIX_PUBLIC_READERS_HOSTED_BATCH.md).

`MidenOASIS-client-http-flow.json`: 30147B, SHA256
`5e877867b4d05daa0af864e9cfb42614fc535d552425f064a5f69087a1c99c8e`.

`MidenOASIS-isolation-http.json`: 5230B, SHA256
`558b960250e1111d129b47d202a7c66c77e0235213407198c98a0341a3d2743c`.

`MidenOASIS-native-http.json`: 11275B, SHA256
`e7b84a27b6463c5dc78994f23867d585dd68ad629949a887472978bc7896f8ab`.

11 November UTC: the live Development CQL Playground rendered three Miden testnet public block headers for heights 1-3 and two for heights 1-2. Both rows in the subset were requested by exact height. The retained disposable visitor was reused. Original-cohort private paired browser verification remains open.
