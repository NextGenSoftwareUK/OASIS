Superseded by [current guide](../../../Devs/POLKADOT_PUBLIC_METADATA_CQL_GUIDE.md); historical deployment notes, not current readiness.

# Polkadot public header CQL

This explicit read representation queries a retained catalogue of public Polkadot block headers. Native CQL3/2 is verified. C# walkthrough/CLI3/2 and local HTTP/Mongo5/4/isolation also pass; remaining hosted/browser acceptance is tracked in [current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md); do not infer hosted availability from a local test.

## C# walkthrough

Use the existing stable catalogue after the normal bounded build. The command creates no wallet, synthetic records or database fixture.

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --polkadot-public LocalState/COSMIC/HostedPolkadotPublicSourceCurrent/source.json
```

[PolkadotPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/PolkadotPublicWalkthrough.cs) selects three configured hashes, then two, using the shared [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs). That pipeline uses CosmicLinq and HolonManager's planner/executor. Source keys are block hashes, distinct from projected OASIS IDs. Do not regenerate the source owner: it contributes to stable native mapping IDs.

## Source contract

`PolkadotPublicHeaderMetadataSource.FromJson` accepts exactly schema1, rpcUrl, genesisHash, blockHashes and sourceOwner. Use1–8 unique canonical lowercase32byte hashes, Polkadot's configured genesis and a stable nonempty owner. Endpoint must be anonymous HTTPS at root, with no user info, query or fragment. The retained source explicitly uses rpc.polkadot.io; no runtime endpoint fallback or retry exists.

**PolkadotBlockHash** supports equality and bounded membership over that catalogue. Unknown hashes and unsupported scans/sorts/predicates fail. All selected records validate before visibility/paging; a bad final record fails the query even if the requested page is empty. No partial rows escape on cancellation/error.

Each query verifies the genesis response, reads the RPC-reported finalized header and verifies its content hash, then verifies each selected header's content hash, height not above the reported finalized height and canonical hash at its number. Header identity is BLAKE2b-256 over SCALE encoding, including all digest logs; duplicate fields, noncanonical values, excessive digest counts/depth and responses over256KiB fail. No block bodies, runtimeWASM or storage scans are requested.

Content hashing verifies identity. Canonical history and finality remain assertions from the configured RPC; this is not an independently verified GRANDPA light client or state inclusion proof. These native projections do not prove COSMIC holons are stored on-chain.

Metadata: PolkadotBlockHash, PolkadotGenesisHash, PolkadotBlockNumber, PolkadotParentHash, PolkadotStateRoot, PolkadotExtrinsicsRoot, PolkadotReportedFinalizedHash and PolkadotReportedFinalizedNumber. Numbers remain exact decimal strings. Rows are public active roots, no children, using the shared NativeRecordHolonMapper.

## WEB4 admission

Optional `CQL_POLKADOT_PUBLIC_SOURCE_JSON` registers a lazy read representation separately from canonical signed storage. Independent query permission/tariff, query-only enrollment, activation and capability checks are still required. It grants no writes or replication authority. Mixed unsigned-read/signed-write roles for one provider remain rejected by one admission factory; preserve the fence.

If metadata encryption is enabled, the specific query key must be explicitly allowed by the trusted query policy. Do not globally reserve it as unencrypted system metadata. Hosted configuration/presets and browser acceptance remain pending, and the public playground must not advertise an unavailable source.

[Scope and evidence](../WIP/COSMIC%20ORM/POLKADOT_PUBLIC_SOURCE_READINESS.md). Primary contracts: [RPC](https://polkadot.js.org/docs/asset-hub-polkadot/rpc/), [header SCALE layout](https://github.com/paritytech/polkadot-sdk/blob/master/substrate/primitives/runtime/src/generic/header.rs), [genesis identity](https://standards.chainagnostic.org/CAIPs/caip-13).

Local HTTP pairing uses two normal SDK create-only private Mongo snapshots, server-derived IDs and provenance links. The owner sees5/4physical copies; another actor sees public headers and zero private snapshots. Hosted preparation/reuse and replication remain separate pending gates.

## Staged configuration and presets

`Publish-HostedPolkadotPublicConfiguration.py` validates only the verified rpc.polkadot.io catalogue and adds the source variable, independent free query permission/tariff and four presets. It rejects credentials, wrong genesis/keys/endpoint, paid reads, existing role overlap and missing encryption grants; canonical DNA is unchanged.

The pending combined rollout uses the already prepared `Publish-HostedGoogleEthereumConfiguration.py with-polkadot` candidate, composing all six strict source validators into8values/24new presets. Do not prepare blindly over existing files or apply separate candidates from one old host snapshot. Explicit `apply-with-polkadot` is for the reviewed matching application/Core79d7859 rollout; no apply/deployment occurred. Never apply it to frozen6bb. Hosted discovery must pass permission, price, activation and capability checks before advertising live examples.
