# NEAR transaction-status observation

2026-10-19. Scoped correction, not whole-provider certification.

## Invariant and implementation

An unavailable/cancelled/malformed observation must not report a completed transfer. `BridgeTransactionStatus.Completed` is enum zero; the former empty OASISResult therefore exposed Completed even when IsError was true. The public status method now initializes Pending, preserves the actual exception and passes caller cancellation to the SDK observation.

The actual near-api-js transaction-status call now requests `waitUntil: NONE`, reporting current known state instead of waiting for finality of an unknown transaction. Submission methods remain FINAL and are not cancelled/replayed by this change. Explicit finality/result validation and exact original transaction ID matching remain required for Completed or Failed.

Read-only status observations have a30second linked deadline. Cancellation stops and awaits only that owned Node process tree, drains its redirected streams, and returns an error/Pending; internal deadline expiry becomes TimeoutException, not evidence of chain failure. Other mutation calls retain their existing process behavior. General mutation lost-outcome journaling and process lifecycle remain separate gaps.

## Evidence

- Rebuilt provider and test consumers using retained caches, no restore/download.
- Focused outcome/observation suite12/12passed, zero skips246ms. New cases: pre-cancelled public observation; controlled SDK unavailable response; actual live controlled Node process cancellation, exact child PID gone afterward. Controlled fixtures are not native SDK evidence.
- Actual retained near-sandbox2.6.2 node, same ledger/chain/contract. Native read-only test1/1passed139ms: original `G8hXdSqWJXTVa9pr2VeztYZv7kEeJaXei9w7kmBHogWS` is still unavailable; real SDK returns `Unknown transaction` and provider reports IsError/Pending. No transaction submitted, replayed or invented as Failed. This test supplements rather than replaces the existing finalized-failed-original assertion, which remains unresolved.
- Initial Windows native attempt lacked bridge-output node_modules and failed before SDK dispatch. Reused source bridge's existing junction to the retained pinned dependency install; no package installation. Native assertion initially expected raw RPC code UNKNOWN_TRANSACTION; corrected to the actual official SDK Unknown transaction message, preserving exact original ID/error/Pending requirements.
- Focused TRX SHA256 `C8E65F4A974E7A9419A41E7753F5D7379856274DDFB3D7D736962A9E4895D73A`; native status TRX SHA256 `D64216EFD16A2A32D1BFFC4080C3E32529E6D32FBC548E24426E6677341B2245`. Fixed reports: TestResults/NearCurrent.
- Exact retained node PID893535 verified against its executable and stopped by SIGINT; original process handle exited0. No ledger reset, redeployment, new environment, download or public spending.

## Remaining gates

Whole focused regression suite57/57passed, zero skips2seconds (excludes legacy mainnet template class; includes separate uncommitted burn tests, so not a burn release gate). TRX SHA256 `95DBC1DABCA6CF8872FDE34A84A301303A098AAEAE971610EE961D3308496DCA`. Dependency manifest/gitlinks valid; Development0ahead/0behind before scoped integration.

Current full storage/burn native gate remains12pass/5fail: four unpaged contract scans exceed view gas, and the old finalized-failed original cannot be read from this node. Do not mark the original transaction Failed just because it is unavailable. Need bounded native record pagination and a truthful retained failed-outcome fixture/archive strategy. Pending token-burn/key-projection changes remain uncommitted; neither this status correction nor its single native test certifies those changes, every provider interface, fees, V2 routing or hosted deployment.
