# Sui OASIS Move package

Source modules: `storage`, `nft`, `token`, `token_escrow`. Storage holds opaque serialized records; avatar and metadata indexing belong to the shared provider layer, not dedicated `create_avatar`/`get_avatar_by_email` Move functions. The former README described functions that do not exist.

## Reusable local verification

Use retained `/var/tmp/oasis-runtimes/sui-1.81.1/sui` in WSL. `Move.toml` uses the retained local framework through `deps/sui-framework`; do not download another framework or reset the node. From this package directory:

```sh
/var/tmp/oasis-runtimes/sui-1.81.1/sui move test --threads 2
/var/tmp/oasis-runtimes/sui-1.81.1/sui move build
```

Both reuse `build/`. These are local VM tests/builds, not publication or consensus proof. Publication, object creation and native mutations require separate admitted original transactions and fee verification. Do not replace the retained deployment during these checks.

## Per-deposit token custody ABI

`token_escrow` is separate from aggregate `token::lock`/`unlock`. It handles only this package's `token::TOKEN`, not arbitrary coins or native SUI.

- `create` creates a shared protocol-1 `Escrow` registry and deposit table. Select a reviewed registry explicitly; never automatically replace one on lookup failure.
- `lock` takes a 32-byte original deposit ID and positive `Coin<TOKEN>`. The SDK must split exact requested units, not submit the whole balance. Custody stores `Balance<TOKEN>` without changing supply.
- `release` takes original ID and exact expected amount, requires recorded owner and returns only that deposit as a coin. The SDK must transfer the result to the same owner in the transaction.
- IDs use SHA256 of original OASIS GUID `N` format (lowercase hexadecimal UTF8). Raw 32-byte hashes are the on-chain keys, distinct from coin/object IDs.
- Released records retain owner/amount, `released=true` and zero funds permanently. Re-release and ID reuse reject. No admin withdrawal or record deletion exists.
- Active funds must exactly equal the positive recorded amount. A digest alone is insufficient: verify original registry/deposit/owner/amount/release state, retaining original transaction evidence on readback failure.

SDK bootstrap is explicit `tokenEscrowCreate`; it invokes `token_escrow::create`, requires exactly one newly created shared registry in the original receipt, then checks matching version/previous transaction/protocol and empty table. It never runs as a lock fallback or automatically changes configuration. C#library entry points are `CreateTokenEscrowRegistry(Guid avatar)` / `CreateTokenEscrowRegistryAsync(Guid avatar)`, returning `SuiEscrowRegistryResponse` with original transaction hash, object ID, creator and protocol. They require the actual avatar wallet and no preselected registry; they are NOT new WEB4 HTTP endpoints. Caller/host must authorize that avatar and native spend. Publication/creation require separate reviewed admission and fee evidence; host authorization and actual live escrow execution remain pending.

Status2026-10-17: all18package tests pass. Subsequent checkpoints added SDK/C#deposit selection, encrypted-wallet facade and offline signed-byte tests; see the repository release-gates report for current evidence. The new registry module is not yet deployed to the retained live fixture. Existing live storage routing is distinct from escrow consensus acceptance.
