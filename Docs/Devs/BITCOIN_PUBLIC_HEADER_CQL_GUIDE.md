# Bitcoin public header CQL demo

This read-only profile turns a small, explicitly configured Bitcoin block-hash catalogue into public holons. It runs through the same HolonManager CQL planner and LINQ adapter as the other providers. It does not sign transactions or save holons onto Bitcoin.

## Run the C# console demo

From the COSMIC worktree, after building the existing console project:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- --bitcoin-public LocalState/COSMIC/HostedBitcoinPublicSourceCurrent/source.json
```

The commented example is [BitcoinPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/BitcoinPublicWalkthrough.cs). Its shared [NativeKeySetWalkthrough.cs](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs) shows the query construction, explicit provider scope, owner-or-public access and deferred LINQ execution.

The retained sample selects mainnet heights 0, 1 and 2. The first query returns all three configured hashes; the second uses `Contains` over two hashes and returns two records. No source records are manufactured or written.

## Query contract

- Explicit provider scope: `ProviderType.BitcoinOASIS`.
- Query metadata key: `BitcoinBlockHash`, canonical lowercase 64-character hex without `0x`.
- Equality or bounded membership over one to eight configured hashes; bounded Skip/Take after complete native selection and access checks.
- Arbitrary scans, sorting and unsupported predicates are rejected by capability-aware planning.
- Each row exposes name, description, hash, genesis, parent, Merkle root, timestamp and compact difficulty bits. Exposed metadata does not imply arbitrary filtering is supported.

Each selected header is exactly 80 bytes. The source recomputes double-SHA256, checks its declared proof of work against the mainnet maximum target, and requires the API to report best-chain membership. Genesis is pinned and verified for each read. These checks do not independently validate accumulated chain work, difficulty transitions, transaction inclusion or full Bitcoin consensus.

## Configuration and safety

Use the retained source file rather than regenerating its owner. The exact schema is `schema` (1), `rpcUrl` (`https://blockstream.info/api/`), `genesisHash`, `blockHashes` and `sourceOwner` (nonempty GUID). Unknown fields, credentials and malformed hashes are rejected. Production JSON loading uses the existing guarded HTTP handler. The profile remains separate from canonical signed Bitcoin storage.

Activation verifies the configured catalogue. Cancellation, a malformed response, an orphaned header or any incomplete selection returns an explicit error with no partial rows, including when the requested output window is empty.

## Current availability

Native controlled and real-public CQL/C# evidence is tracked in [Bitcoin status](../WIP/COSMIC%20ORM/BITCOIN_PUBLIC_SOURCE_STATUS.md). Local HTTP/Mongo federation now passes native3/2, paired5/4 and private isolation. Hosted application/settings rollout and browser verification are unfinished gates. Bitcoin must not appear as a live sandbox provider until those gates pass. The planned hosted demo includes one ready Mongo-paired preset.

Protocol reference: [Blockstream Esplora API](https://github.com/Blockstream/esplora/blob/master/API.md).
