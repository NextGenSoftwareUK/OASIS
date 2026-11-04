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
