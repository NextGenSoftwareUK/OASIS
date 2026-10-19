# Polkadot finalized wallet history

## Implementation and scope

`GetTransactions` and `GetTransactionsAsync` now read real finalized blocks/events with the pinned Polkadot SDK. They no longer call the nonstandard `/accounts/{address}/transactions` REST route, assume10decimals, truncate to Int64, or report an HTTP/RPC failure as a successful empty list. The existing `IGetWeb3TransactionsRequest.WalletAddress`, `FromBlock` and `ToBlock` contract is reused; no second request schema/indexer/client installation was added.

The selected inclusive range contains at most128blocks. Start defaults to0; omitted end means the captured finalized snapshot height ONLY if that range fits the limit. Otherwise the call fails with instructions to supply an explicit range. It never silently substitutes a recent slice. Unavailable/future/reversed/oversized ranges, pruned storage, invalid metadata/events, broken parent linkage or changed selected/snapshot anchors fail the entire read; no partially populated result is returned. Production SDK work retains its existing owned45second deadline.

The reader returns:

- Native `Balances.Transfer` events involving the wallet.
- `Assets.Transferred`, `Assets.Issued` and `Assets.Burned` effects involving the wallet, including incoming effects whose signer is someone else. Issuance is not invented as a debit from the issuer; burn holder and admin signer remain distinct.
- Other wallet-signed extrinsics with their original identity and top-level System dispatch result, but without an invented monetary amount.

This is **not** an arbitrary-pallet balance index, NFT/internal-contract-effects explorer or complete unbounded wallet index. Unrecognized incoming pallet effects are not indexed. Memos and fees are not interpreted as monetary history effects. `NativeExecutionResult` describes top-level System dispatch, not every nested call; actual events are retained even if that dispatch fails, rather than assuming every failure rolled all effects back. Other provider interfaces and full V2 certification remain open.

## Shared response evidence

The existing Core `IWalletTransaction`/`WalletTransaction` now have optional additive members:

| Field | Meaning |
|---|---|
| `AmountEvidence` | `applied-event` for exact event-observed units; `not-interpreted` when no financial interpretation is supplied. `requested` is available to request-based writers. Null preserves legacy semantics and must not imply an observed effect. |
| `NativeEvent` | Actual interpreted event, such as `assets.Transferred`; null for opaque signed calls. |
| `NativeBlockHash`, `NativeBlockNumber` | Exact canonical block location. |
| `NativeExtrinsicIndex`, `NativeEventIndex` | Extrinsic and global event positions; event index null for the opaque signed-extrinsic row. |

Default interface members return null, so older implementers do not need new members. Interface-typed web JSON serialization and an old-shape implementer are tested. Other providers can reuse these fields without inheriting Polkadot-specific logic; they are not automatically relabeled as event-proven.

`NativeTransactionId` remains the real extrinsic hash. A separate deterministic OASIS GUID includes genesis/block/extrinsic hash/event position, so two effects from one batch do not collapse into one row. `RawAmount` preserves exact u128 event units; `AmountUnit` is `native-base-unit` or `asset:<id>:base-unit`, not a fiat quote. Native precision comes from chain properties; asset precision from that block's asset metadata. Existing `double Amount` is approximate display data. Opaque rows have null raw units and `AmountEvidence=not-interpreted`; their legacy display0 is **not** evidence of zero balance change or fee.

Timestamps come from actual historical `Timestamp.Now` storage, never local current time. The installed SDK17.0.2 marks `chain_getBlock(hash)` historic and swaps its registry by block hash; `api.at(hash)` reads historical storage metadata. Controlled tests cover error paths, but no induced real runtime-upgrade/archive migration is claimed. Native/asset fee settlement remains separate. Shared dispatch parsing and u128/display conversion are reused by receipt/balance/history code.

## Current evidence (2026-10-18)

- SDK155/155, zero skips; controlled C#137/137, including interface JSON/default-member compatibility, errors rather than empty results, large raw amounts, distinct effect GUIDs, status/provenance and range binding. Current controlled TRX SHA256 `E84E64A324BD7E11B85FA0B6BF58595AF3EB763AC003D1F4DEC7D2619082A810`.
- Real retained local ledger4/4rows27s: ordinary sync AND async APIs find existing finalized native/asset transfer/mint/burn events, including incoming non-signer effects, exact units/precision/timestamp/native IDs, stable GUIDs and typed event/block locations. Also independently re-reads the retained native receipt's dispatch/fee after shared parser extraction. No signing, broadcast, funding, reset or bootstrap. Current `polkadot-local-history-current.trx` SHA256 `C10FA15389A34711BDDF3AA0B2BACE8DA1F6C88823DC3A358D8EC75EB748335A`.
- Current PUBLIC Polkadot read-only SDK check: finalized block33491559/hash`0x54852b15cc1f9c02b5c87154d63b6df78cb04780fae4f9a99779aede16a29522`, pinned genesis`0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3`. One block's actual metadata/history query, zero rows for the public unfunded fixture address. This proves successful bounded current-public-runtime reading, NOT a found public transfer, funded mutation, full wallet index or hosted HTTP authorization/billing.
- Core builds both netstandard2.1/net10.0 using cached dependencies (254existing warnings/0errors); provider build succeeds. Packaging syntax/import/pin verifier and9regression tests pass. Source/output `wallet-history.mjs` SHA256 `85EDBC1AB5EB3571491ADBFEE32D4E14D41F8E60E9A170CDD4456ACCEE69F6F3` match.

## Reuse and run

### Exact finalized transaction status

`GetTransactionStatusAsync(transactionHash, blockHash, cancellationToken)` uses the same pinned SDK and canonical finalized extrinsic/event reader. This is the efficient choice when the original receipt supplies a block hash. Exact top-level `system.ExtrinsicSuccess` maps to Completed and `system.ExtrinsicFailed` to Failed; native fee events are not required for this general status lookup. Nested batch/contract success, cross-chain completion and billing settlement are separate facts.

The original `GetTransactionStatusAsync(transactionHash, cancellationToken)` signature remains available. Standard Substrate RPC does not provide a transaction-hash index: it searches backward through one frozen finalized ancestry, checking block/hash/height linkage, under the existing45second process deadline/caller cancellation. Older public-chain transactions may require an explicit block to finish within that bound. There is no silently truncated successful lookback or invented REST index.

Malformed identity, missing/ambiguous transaction, noncanonical/unfinalized block, unavailable archival data, missing terminal event, cancellation or timeout yields `IsError=true`, preserving the exception and an unresolved Pending value. Callers MUST check IsError: Pending here is not proof of pool acceptance. Even complete finalized-snapshot absence does not prove rejection/expiry or authorize resubmission. No false NotFound or Completed is returned just because an RPC response contains a result property.

The History suite now reads both status modes for the retained existing native/asset-transfer/mint/burn originals in addition to its history/fee assertions. It requires no new signing, funding or fixture ledger. Controlled SDK tests cover finalized failure and malformed/missing/pruned/orphan outcomes; the native run proves successful finalized lookup, not a new real failed-dispatch transaction or full hosted/public-chain certification.

Resume the SAME runtime/ledger using the [existing operator guide](POLKADOT_PROVIDER_NATIVE_TESTS.md), then:

```powershell
./Scripts/TestHosts/Run-ReusablePolkadotNativeTests.ps1 -Suite History
```

This suite holds the same exclusive lease and pending-original fences as every mutation suite, but deliberately skips bootstrap. It reads the existing flushed completion records; it does not create new assets/accounts/ledgers or clear pending originals. Fixed compact reports are overwritten on verified reruns, not accumulated in new directories.

The opt-in `SdkBridge/test-public-wallet-history.mjs` uses the SAME installed SDK and a45second process deadline. With `OASIS_POLKADOT_READONLY_NATIVE=1`, run it from the existing SDK directory; it has no keys/signing/submission and only reads one finalized public block. Restore the prior process environment afterward. No new environment/download is needed.

Hosted WEB4 route/auth/wallet/billing verification, remaining provider interfaces, public runtime-upgrade/history retention behavior and NuGet/deployment certification are separate release gates, not implied by these scoped passes. Core commit, parent gitlink and Railway manifest must advance together.

Core evidence contract is committed/pushed on its existing Development branch as `129cb435a23629a8599ff8de7ab1600169b74a84`; parent gitlink and manifest are advanced together with this provider batch. No direct `GetTransactions`/`GetWeb3TransactionsRequest` call was found in the WEB4 WebAPI source search: this batch implements the provider interface, not a new hosted history route. Authorized HTTP/manager composition, quota/paid-read ownership and range/evidence propagation remain explicit work, not assumed complete.
