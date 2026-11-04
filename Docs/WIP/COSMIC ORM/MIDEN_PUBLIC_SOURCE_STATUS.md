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
