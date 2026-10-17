# HyperDrive V2 native-chain fees and recovery

Updated: 2026-10-17. This is a living implementation and verification report, not a claim of production readiness.

2026-10-17 recorded-evidence recovery: Core778cba0225561f16b2cf1b8273048bb9e875e890 persists `ObservedOutcome` atomically before final aggregation/queueing, freezes that logical outcome and prevents new provider attempts afterward. The worker always composes one built-in `RecordedUsageRecoveryResolver` with injected resolvers. It uses existing canonical accounting only when logical outcome and complete receipts are already durable; missing outcome/measurement stays unresolved. It never queries or executes a provider, invents quotes, infers API success from successful transactions, or changes stored native components. Known internal operations recover only their explicit service catalogue price. Custom lookup resolvers must not claim an already-ready operation; overlapping owners remain an explicit conflict, not ordered fallback.

Core107/107pass11s/0skip (five new real-Mongo cases: two native logical outcomes, two incomplete cases, one explicit catalogue case), default worker/no custom resolver, restart recovery/native0.07USD total, immutable outcome and no late provider work. Existing native evidence now always requires an admitted attempt even if an endpoint policy was not marked measured. Core both targets+WEB4build0errors; WEB4authority81/81pass17s, both runners exit0/27029closed. The interface extension requires custom `IUsageOutboxStore` implementations to support durable outcome capture; the sole in-repo Mongo implementation is updated. Worker wiring is production code on this branch, not a hosted-deployment claim. Native lookup, price sources, historical authority and hosted acceptance remain unimplemented/unproven gates; sponsorship remains disabled.

2026-10-17 explicit REST protocol: both Core HTTP settlement overloads now send receipts containing any typed native evidence only to `POST /api/subscription/usage/settle-native-v1`. Ordinary receipts retain `/api/subscription/usage/settle`. No downgrade/retry to ordinary settlement occurs when an old host returns404. Both actual WEB4 controller actions delegate to one authenticated implementation, which rejects body/route protocol mismatches. A missing native route leaves native outbox evidence/amounts durable in deadletter for reconciliation, not zero usage or permission to resubmit the provider.

Core102/102pass10s; WEB4 authority81/81pass17s (includes real compiled controller in in-process HTTP host:2routes x2bodytypes x2authentication states, service delegate spy; real Mongo ledger covered separately). Both runners exit0, no skips; owned27029closed. Initial Core100pass2fail102 came from injecting404 after completion had already settled; moved injection before completion and asserted the expected reconciliation error. An intermediate103-case run included the controller test in Core; moved that test into WEB4's suite so Core has no upper-layer artifact dependency. Final controller host uses supported generic HostBuilder; already-cached TestHost10.0.0 reused via local-only restore, no downloads. Core both frameworks and WEB4 build0errors; Core e4516260b4acb53f4e9b4e3d825bc03794c21a9e.

The artifact test requires the existing WEB4 Debug/net10.0 build and rejects a controller DLL older than its source; it loads dependencies from that retained output, not copied binaries or a substitute controller. It does not boot the full production Startup pipeline, use real price quotes, register native recovery resolvers, exercise other public transports, or certify hosted deployment. Those remain explicit release gates. Source inspection found no generic usage-ledger HTTP middleware registration in WEB4 Startup; full middleware/native-admission acceptance is still required rather than inferred from this controller test.

Latest controlled receipt integration2026-10-16 (Core c4017fefd99e92ee320f601f6a6acd0c2a23ed37): optional `UsageProviderReceipt.NativeFeeValuations` retains signed native components. Core completion/recovery and WEB4 validation use one central `UsageReceiptAccounting.OperationCost`; native costs/rebates net once across the operation, floored at zero. Ordinary API/AI/service costs remain nonnegative and are not erased by excess native rebates. Per-receipt `ActualCostUsd` is only a nonnegative native subtotal when typed native evidence exists; the final native charge is computed from retained signed components, NOT by adding those subtotals.

Native identity binds enclosing provider/receipt and operation UUID, bounded denomination evidence, signed exact units, valuation provenance/time and value. New Core/WEB4 fingerprints bind native evidence; the no-native legacy hash path is unchanged (Core golden-encoding test). Mongo mapping is owned by the outbox: explicit standard UUID representation and string-encoded UTC valuation time preserve sub-millisecond provenance through durable round trips. Generic provider-measurement cost-delta corrections cannot replace immutable native evidence; existing audited administrative billing adjustments remain separate.

Verification: Core full owned95/95pass10s (includes13 pure accounting/fingerprint cases and2 native outbox restart cases); WEB4 authority80/80pass16s with concurrent eight-way same-operation duplicate settlements, zero floor, provenance mutation rejection and global cross-operation receipt reuse rejection. Core both targets and actual WEB4 build0errors/warnings remain. Initial Core93pass2fail95 exposed unspecified Mongo UUID encoding; fixed mapping, final95green. Initial authority80green/full runnerexit0; strengthened authority80green but runnerexit1 because owned Mongo shutdown exceeded10s. Subsequent authoritative process/port inspection confirmed owned Mongo gone/27029closed; unrelated6840 left untouched. Teardown timing remains a harness limitation, not a claimed fully clean runner gate.

No native source price feed, cryptographic resolver registration, hosted/rolling-version acceptance or spending authorization is supplied by these tests. Valuations are synthetic authorized-service fixtures, not real market quotes. Deploy/verify WEB4 authority support before enabling any consumer to emit native evidence: old authorities do not bind new optional evidence in fingerprints. Mint sponsorship stays disabled until those gates pass; no public fees were paid.

Latest ownership correction2026-10-16: `NativeOperationFeeAccounting`, its value types and seven tests moved from provider-shared storage into Core `Services/Subscriptions` and `Tests/SubscriptionUsage`. There is one implementation, no provider compatibility shim, and central billing need not depend on a provider library. Arithmetic/policy unchanged; Core7/7pass13ms, remaining Shared298/298pass1s (the previous305 consisted of these298 plus the moved7). Core net10.0 and full netstandard2.1/net10.0 builds0errors; warnings remain. Core commit07364fa4b26ec600b92626e1bde3f70bfb19a23d. Older references below to a Shared calculator describe its historical location; the current owner is central Core subscription accounting. Production valuation/receipt contracts/ledger integration remain pending.

## Approved billing policy — controlled accounting integrated; production acceptance pending

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
| Sui | Official SDK retains exact signed MIST net fee, including storage rebate; avoids double-counting nonrefundable storage fee. Recovery binds receipt gas payer to original signed gas owner, independently of transaction sender. Central aggregation/ledger and recorded-evidence worker recovery now tested separately from the recorded native SDK/localnet cases. | Authoritative fiat valuation, native receipt lookup owner/host integration and hosted acceptance. |
| Cosmos | Observed ante deduction retains exact denomination/amount and checks signed declaration; prepared recovery binds signed granter/explicit payer/default signer. General reader validates default payer against first supported signer key. SDK53 and earlier owned C#139 passed; these are different recorded revisions/scopes, not one combined certification. | Native original-outcome uncertainty remains; refunds/net-cost coverage, complete signer types, production valuation/resolver/host integration. |
| Hedera | Mirror indexed charged fees include duplicate submission fees without replacing the original outcome. Signed preparation recovery requires independently authorized payer key. | Funded consensus/storage/NFT acceptance, complete query-cost accounting, production valuation/resolver/host integration. Existing funding limitation remains; do not infer free SDK queries. |
| TRON | Solidified fee mapping binds original transaction hash and contract owner; exact SUN total and resource components retained. | Original uncertain transaction evidence remains unresolved; full native accepted-operation recovery/host/valuation verification. |
| EVM wrappers | Retained local Ethereum31337 evidence covers real SDK/contract operations; latest186tests,59contract assertions and4shared Web3 cases pass. | Those runs do not certify public-network gas costs, actual L2 fee semantics, or production settlement. |

Native receipt verification is not fiat pricing, billing authorization, or completed settlement.

2026-10-16 TRON prepared fee recovery added: provider delegates read-only recovery to existing native SDK client under its lifecycle gate, without storage activation/probe. Reuses shared immutable attempt snapshot, existing `TronPreparationVerifier.VerifyWithAccountAuthority`, native hash/finality/fee mapper and payer binding. Requires an independently verified **original** account-permission snapshot from a trusted recovery owner. The method does not authenticate arbitrary caller-supplied account JSON or fetch today's permissions and pretend they were historical authority. Production historical authority provenance/resolver integration remains missing; do not expose this input directly as an untrusted user authorization API. Configured fee ceiling remains enforced; changed ceilings may require explicit operator review rather than inferred approval.

Current controlled verification: full TRON317/317pass3s/0skip, owned Mongo initializer186ms, producer zero errors. Existing seven prepared-storage cases now recover exact hash/owner/17SUN using real SDK signatures and controlled solidified RPC; invalid account and substituted original hash are rejected before extra fee lookup, broadcast count unchanged. First implementation317pass3s, then strengthened negative-lookup assertions317pass3s. Neither run proves native-chain consensus or resolves the retained uncertain original transaction. No public funds or old transaction resubmission.

## Current receipt integration and remaining release gaps

2026-10-16 recovery identity hardening: Cosmos/Sui/Hedera now capture one shared `NativePreparedAttemptSnapshot` before the first verification await. Previously the subsequent receipt lookup re-read mutable `attempt.ProviderRequestId`, allowing the identity to change during verification. The immutable snapshot binds provider/configured chain, original request ID and signing envelope once; existing `UsageNativePreparation.ValidateAndMeasure` remains the canonical envelope validator. Chain-specific signature verification, payer checks and native receipt parsing remain provider-owned. This does not add fiat pricing or settlement. Shared305/305pass1s includes three new snapshot cases; focused Sui recovery3/3pass753ms and Hedera2/2pass2s. Full native/hosted reruns not implied by these focused checks.

Generic `ActualCostUsd` remains nonnegative. Signed native evidence now has an explicit typed path, netted centrally as described above; never discard negative native components or treat the sum of floored per-receipt summaries as the final charge. Missing outcome/price/authority still requires reconciliation, not a zero-cost receipt. The earlier positive-only receipt integration gap is closed in controlled Core/WEB4 tests, not yet verified in deployed hosts.

The worker now includes a concrete recorded-evidence resolver for already-complete durable measurements and an explicit logical outcome. Production native receipt lookup/authority and authoritative valuation sources are still missing; the recorded resolver cannot substitute for them. No reservation may be settled from an assumed native outcome or by replaying an uncertain transaction.

### Coordinated integration owners (source audited 2026-10-16)

The latest controlled integration above implements the receipt/hash/aggregation/validation work in this ownership map. These rows retain the invariants to preserve, not a claim that every listed code change is still pending. Native provider-measurement deltas are explicitly rejected rather than silently altering signed evidence. Production resolver/valuation/host acceptance remains open.

| Owner | Required coordinated change; current invariant to preserve |
|---|---|
| Core `Services/Subscriptions/UsageExecutionContracts.cs` | Receipt validation/hash and operation aggregation: retain exact signed native valuation evidence without allowing arbitrary negative generic API/AI usage; preserve existing receipt fingerprints when no native evidence is present. |
| Core `Services/Subscriptions/MongoUsageOutboxStore.cs`, `ResolveAsync` | Recovery currently requires total cost equal the sum of nonnegative receipts. Preserve every persisted attempt's exact receipt binding and immutable recorded evidence while introducing the final operation zero floor. Updating only `Aggregate` would still fail recovery. |
| WEB4 `Services/Subscription/UsageLedgerValidation.cs` | Validation and settlement fingerprint must agree with Core accounting and bind all new evidence; reject a caller-supplied total that differs from verified operation accounting. |
| WEB4 `Services/Subscription/MongoSubscriptionUsageRepository.cs` | Atomic settlement, globally unique receipt attribution and corrections must stay consistent. `CorrectAsync` currently prevents each receipt and the operation from becoming negative. A native rebate cannot be implemented as a generic negative correction; recompute the operation from retained evidence and its zero floor. |
| Core `Services/Subscriptions/Web4UsageSettlementWorker.cs` and actual hosts | Built-in recorded-evidence resolver recovers complete measurements with a durable logical outcome; injected owners handle other eligible cases. Missing native lookup/pricing authority remains recovery-required, never free usage. Hosted registration/acceptance remains open. |

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
