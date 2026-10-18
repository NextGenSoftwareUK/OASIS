# Four-provider interface acceptance audit

Audited 2026-10-17 against current task source. Scope remains CosmosBlockChainOASIS, HashgraphOASIS, SuiOASIS and TRONOASIS. This is a source-contract audit, not a new native test run or completion certificate. Current report hashes and unresolved originals remain authoritative in [release gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md).

## What an implemented interface proves

All four declare blockchain storage, NFT, network and smart-contract capabilities and reuse the shared key-value storage base. Compilation proves required method signatures are available, not correct native behavior, every option, fee safety or V2 routing. Default interface methods need separate review; inherited implementation requires shared and provider-native evidence.

`Core/Interfaces/Providers/IOASISSmartContractProvider.cs` is currently an empty marker extending IOASISProvider, with no deploy, execute or query operations. Its declaration must NOT be reported as proof of generic smart-contract deployment/execution. Provider-specific SDK/contract code and tests must be assessed independently. Filling out a new public generic contract API is a separate design change, not something to silently invent to make this audit green.

`IOASISIdBoundTokenEscrowProvider` is also a marker, but has an explicit behavioral contract: existing LockToken/UnlockToken must honor deposit identity, configured escrow and exact amount, rejecting unsupported inputs. Ordinary lock-all acceptance does not prove it.

## Contract families and acceptance obligations

| Contract | Required behavior | Evidence needed before calling it complete |
|---|---|---|
| IOASISStorageProvider / inherited IOASISDBStorageProvider | Avatar/profile/detail and holon selectors, writes/deletes, children/version/type/masking/search/options | Enumerate actual signatures and overloads; real native stored state, encrypted private records/query indexes, positive/absence/ambiguity/deletion/error cases; shared controlled tests alone insufficient |
| IOASISBlockchainStorageProvider | Send/Mint/Burn/Lock/UnlockToken sync+async; GetBalance/GetTransactions sync+async; GenerateKeyPair sync+async; CreateAccountAsync/RestoreAccountAsync; WithdrawAsync/DepositAsync; GetTransactionStatusAsync | Exact native amounts/decimals, chain/signer/recipient identity, original receipts, success and charged failure, history pagination, cancellation/unknown outcome, no mutation replay; account key creation is not necessarily a funded on-chain account |
| IOASISNFTProvider | Send/Mint/Burn/Lock/UnlockNFT sync+async; WithdrawNFTAsync/DepositNFTAsync; LoadOnChainNFTData sync+async | Actual native ownership/metadata/custody and receipt identity; collection/batch/off-chain metadata where applicable; foreign-chain proof cannot be invented by a destination SDK |
| IOASISNETProvider | GetAvatarsNearMe and GetHolonsNearMe | Real persisted spatial matches, radius/type boundaries, absence/deletion/private-data handling; a local geographic filter is not peer-to-peer discovery |
| IOASISIdBoundTokenEscrowProvider | Existing token lock/unlock honors explicit identity/amount/escrow | Same original deposit recovered without replay, exact owner/amount/released balance plus bound native receipts |
| Prepared-native interfaces | Signed preparation, bounded aggregate admission, fee evidence/recovery | Durable admission before selected transport, verified all-fee maximum/actual fee coverage, original-only recovery, persisted outcome separate from receipt, hosted authority/settlement acceptance |

The NFT interface's SetCollectionSize/SetCollectionSizeAsync have explicit default unsupported implementations described as Solana/Metaplex collection management. None of these four overrides them. This is an explicit provider-specific non-capability, not silent success, and must remain visible in API capability documentation. It does not prove a generally supported collection-size operation on these chains; do not substitute fake success or manufacture transactions.

## Current native acceptance boundaries

| Provider | Current stronger evidence | Still unproven |
|---|---|---|
| Sui | Retained real V2 storage replication/selection/outage; prepared native HTTP/storage; latest four C# native asset cases with26originals; ID-bound escrow state and all4original fee receipts re-read | Exhaustive storage selectors/options/nearby/history/bridge/sync overload acceptance on current source, every V2 policy under native faults, production auth/prices/settlement/hosted routes. Retained escrow refresh is read-only, not a new lock/unlock execution |
| Cosmos | Four real leaf V2 primary/replica sync/async cases,120matching-record strategy reads and owned-node outage fallback; signed originals/actual ante deductions | Original retained unknown intent reconciliation; broader storage/asset/interface/native-fault matrix; ante-only fee evidence is not net-final settlement; hosted acceptance |
| Hedera | Actual official SDK construction/signatures/pre-transport admission and safe cost rejection over controlled transport; historical retained deployed runtime and fee receipt verified | Failed native read remains fenced; funded consensus storage/assets/V2 matrix, calibrated all-operation budgets and hosted billing. Controlled tests do not close these gates |
| TRON | Actual SDK/signatures and scoped historical native evidence | Both uncertain originals remain fenced; actual current native interface/V2 matrix, aggregate all-fee bound (Energy feeLimit alone is insufficient), recovery/hosted acceptance |

## Next verification work

2026-10-17 Cosmos final-fee implementation audit: actual SDK receipt currently proves exact observed ante deduction and payer/granter binding, not final post-handler refunds/tips. Reviewed per-chain maximum profile is not final-fee proof. Actual Cosmos reader + shared readiness checker tested for both plain/reviewedmaximum configuration, remaining ante-only/unactivated. Final-fee implementation must bind reviewed chain/app-version policy and original charged/refund/tip evidence, including charged failures and upgrades. Do not replace this gap with a NetFinal flag to make startup green. Primary source and specific existing code responsibilities documented in native fee accounting. No new native transaction or configuration change for this audit.

2026-10-17 current Sui retained ID-bound escrow check adds public GetTransactions sync+async and GetTransactionStatusAsync acceptance for all4originals, with exact raw SUI history vs real native fee receipts including positive burn-rebate credit.1/1pass52s. No new mutation; not exhaustive history pagination, all-asset history or every overload/option proof. Hedera original activation incident now4durable fee receipts via shared recovery/no native replay; logical outcome/settlement/routed native acceptance still unresolved. See current release journal for authoritative hashes instead of inferring completion from older source-table summaries.

2026-10-17 ownership audit: Sui SDK invocation already honors active shared ownership across all its declared mutations and rejects explicit replacement. Cosmos now does both (new replacement regression red-before/full187offline green-after). Hedera configured storage and TRON prepared storage create scoped backend/client views; public asset paths have separate explicit preparation facilities. Do not infer that arbitrary asset calls inside a storage lambda or every hosted asset route are automatically admitted; trace the actual endpoint/SDK owner and require pre-transport proof before enablement. No new native tests in this audit.

2026-10-17 Cosmos follow-up: current native routing4/4pass1m19s adds64sync/async Id/provider-key/metadata selector reads against native/file replicas, exact dispatch counters and unique leaf identities. Current matrix now184matching-record reads; original8native write/delete receipts remain verified. Broader records/options/native-fault/fee/hosted obligations remain separate.

2026-10-17 follow-up: Sui healthy V2 native acceptance now includes24profile/detail Id/username/email sync/async routed reads across native/file replicas, with exact selected-provider counters. This narrows the selector gap only; provider-key/metadata/versions/options and exhaustive direct/interface parity remain separate obligations. Final1/1pass2m40s; current release journal retains report hash.

1. Expand storage acceptance beyond current leaf/selected selector cases using the same retained ledgers and shared base, not provider-local duplicate implementations.
2. Verify each blockchain/NFT method family through real interfaces; retain exact source revision and native originals, and distinguish historic/current, sync wrappers/native operations and controlled/consensus evidence.
3. Resolve fenced originals through original-only inspection, never by replacement transactions, deleting journals or resetting ledgers. Successful unrelated Sui tests cannot close Cosmos/Hedera/TRON gates.
4. Complete native fee profiles/readers and host bootstrap/registration before enabling paid routes. Missing CoinGecko credentials and pending operator/design choices remain explicit dependencies.
5. Clean pinned-source WEB4–WEB10 publishes and hosted acceptance remain required before merge/deploy. Unrelated dirty middleware is not release-certified source.

Do not mark this checklist complete merely because source contains no TODO/NotImplementedException: inherited defaults, missing native coverage and unverified outcome/fee contracts are independently blocking.
