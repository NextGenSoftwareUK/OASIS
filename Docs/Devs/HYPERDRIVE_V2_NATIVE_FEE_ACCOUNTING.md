# HyperDrive V2 native-chain fees and recovery

Updated: 2026-10-16. This is a living implementation and verification report, not a claim of production readiness.

## Approved billing policy — calculator implemented; billing integration pending

The user approved netting native fees and storage rebates **within the same admitted operation**, with a minimum final charge of zero. There is no payout, carry-forward credit, or offset against another operation.

- Retain every original signed attempt and its independently verified native receipt, including charged failures and negative net fees.
- Sum fees and rebates for the operation before applying the zero floor. Do not discard negative receipts or clamp each attempt separately.
- Example in one denomination: fees 10 and rebate 3 produce charge 7. Fees 3 and rebate 10 produce charge 0, not a payout of 7.
- Another operation's costs cannot absorb the unused rebate.
- Keep denominations explicit. MIST, SUN, tinybar and Cosmos denomination amounts are not interchangeable numbers. Mixed currencies require authoritative, auditable valuation before a combined monetary charge can be established.
- An unavailable receipt, unknown outcome, absent price or unsupported payer attribution is unresolved evidence—not zero usage and not permission to release the reservation.

This approval does not itself authorize public-chain spending, paid cloud provisioning, a production deployment, a particular exchange-rate source, or a subscription allowance change.

## What is implemented and verified

2026-10-16: Shared `NativeOperationFeeAccounting.Calculate` now validates and snapshots caller-supplied valued evidence, rejects cross-operation and duplicate receipt/denomination attribution, sums signed USD fees/rebates with checked arithmetic and floors only the final operation charge. Exact native units, sign preservation and explicit UTC valuation provenance are required. Seven new cases pass within the full Shared suite: **302/302, zero skipped, 1 second**; producer build has zero errors (two existing package warnings). A draft test referenced a nonexistent helper; corrected before the first build/test, with no failed test execution.

This is a pure calculator, not a production valuation or settlement implementation. Synthetic fixture valuations do not prove native receipt verification, an authorized price feed, durable replay protection, or ledger integration. Duplicate detection is scoped to the supplied operation; durable cross-operation attribution remains the settlement owner's responsibility. Core/WEB4 positive-only receipt contracts and production recovery/valuation gates below remain open.

| Area | Evidence and scope | Remaining work |
|---|---|---|
| Sui | Official SDK retains exact signed MIST net fee, including storage rebate; avoids double-counting nonrefundable storage fee. Recovery binds receipt gas payer to original signed gas owner, independently of transaction sender. SDK30, C#107 and retained localnet four asset cases passed at their recorded revisions. | Operation-level rebate aggregation, authoritative fiat valuation, production resolver/host integration and hosted acceptance. |
| Cosmos | Observed ante deduction retains exact denomination/amount and checks signed declaration; prepared recovery binds signed granter/explicit payer/default signer. General reader validates default payer against first supported signer key. SDK53 and earlier owned C#139 passed; these are different recorded revisions/scopes, not one combined certification. | Native original-outcome uncertainty remains; refunds/net-cost coverage, complete signer types, production valuation/resolver/host integration. |
| Hedera | Mirror indexed charged fees include duplicate submission fees without replacing the original outcome. Signed preparation recovery requires independently authorized payer key. | Funded consensus/storage/NFT acceptance, complete query-cost accounting, production valuation/resolver/host integration. Existing funding limitation remains; do not infer free SDK queries. |
| TRON | Solidified fee mapping binds original transaction hash and contract owner; exact SUN total and resource components retained. | Original uncertain transaction evidence remains unresolved; full native accepted-operation recovery/host/valuation verification. |
| EVM wrappers | Retained local Ethereum31337 evidence covers real SDK/contract operations; latest186tests,59contract assertions and4shared Web3 cases pass. | Those runs do not certify public-network gas costs, actual L2 fee semantics, or production settlement. |

Native receipt verification is not fiat pricing, billing authorization, or completed settlement.

## Known accounting integration gap

Core usage receipts currently validate `ActualCostUsd` as nonnegative. Native Sui evidence can legitimately contain negative net units. Consequently, simply translating each native receipt into the existing positive-only billing receipt would lose rebates or reject valid evidence. The approved policy requires an explicit operation-scoped accounting integration; do not silently clamp each receipt or invent a zero-cost receipt.

The production `IUsageRecoveryResolver` implementation/registration and authoritative valuation source are not yet proven. The settlement worker and test resolvers are not substitutes for those missing production owners. No reservation may be settled from an assumed native outcome or by replaying an uncertain transaction.

### Coordinated integration owners (source audited 2026-10-16)

| Owner | Required coordinated change; current invariant to preserve |
|---|---|
| Core `Services/Subscriptions/UsageExecutionContracts.cs` | Receipt validation/hash and operation aggregation: retain exact signed native valuation evidence without allowing arbitrary negative generic API/AI usage; preserve existing receipt fingerprints when no native evidence is present. |
| Core `Services/Subscriptions/MongoUsageOutboxStore.cs`, `ResolveAsync` | Recovery currently requires total cost equal the sum of nonnegative receipts. Preserve every persisted attempt's exact receipt binding and immutable recorded evidence while introducing the final operation zero floor. Updating only `Aggregate` would still fail recovery. |
| WEB4 `Services/Subscription/UsageLedgerValidation.cs` | Validation and settlement fingerprint must agree with Core accounting and bind all new evidence; reject a caller-supplied total that differs from verified operation accounting. |
| WEB4 `Services/Subscription/MongoSubscriptionUsageRepository.cs` | Atomic settlement, globally unique receipt attribution and corrections must stay consistent. `CorrectAsync` currently prevents each receipt and the operation from becoming negative. A native rebate cannot be implemented as a generic negative correction; recompute the operation from retained evidence and its zero floor. |
| Core `Services/Subscriptions/Web4UsageSettlementWorker.cs` and actual hosts | Worker accepts injected resolvers but cannot discover native outcomes or prices itself. Registration of the worker alone does not supply a production resolver. No resolver means recovery-required, not free usage. |

Core paths above are relative to `OASIS Architecture/NextGenSoftware.OASIS.API.Core`; WEB4 paths are relative to `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`. This audit is not a claim those integration changes have been implemented. Existing dirty subscription middleware changes belong to separate work and were not staged into this fee change.

## Required acceptance before fee recovery is complete

1. Preserve original attempt identity, chain, signatures, payer and exact signed fee components.
2. Collect every physical attempt's terminal fee evidence, including failures and duplicates where charged.
3. Retain signed rebates until all same-operation evidence is aggregated; floor only the final charge.
4. Bind valuation source, rate/time, denomination and rounding policy to durable evidence. Missing valuation stays unresolved.
5. Prove idempotent settlement, restart recovery, concurrent-worker behavior and no cross-operation rebate leakage.
6. Prove actual host registration/admission and authorized spending policy; local synthetic-authority tests alone do not prove this.
7. Document provider/network-specific exclusions and run the required native/hosted gates before merge.

## Related living reports

- [Current release gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md): latest authoritative checkpoints, failed attempts and outstanding release requirements.
- [Provider I/O coverage](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md): implemented versus verified scope, SDK/runtime evidence and remaining provider gaps.
- [Workspace cleanup ledger](WORKSPACE_CLEANUP_LEDGER.md): retained environments, exact disposable cleanup targets and regeneration evidence.

These reports must remain synchronized as implementation progresses. Historical green subsets must not be presented as current, exhaustive provider or API certification.
