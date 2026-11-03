# TON local TVM recovery and legacy boundary

Reviewed 2 November2026. Red source-recovery scope; no new native acceptance.

Current TONOASIS inherits Web3CoreOASISBaseProvider and accepts EVM arguments
without an expected chain identity. That is not native TON TVM storage.
Retained pin237403a75 has a different native local TVM implementation: Tact
contract, process worker, state journal and shared encrypted key-value backend.
It explicitly does not implement public-network signing/storage/wallet/NFT or
full HyperDrive parity. The existing public-header source is separate and stays.

Do not silently replace the legacy EVM wrapper and call its API compatible.
Per David's legacy rule, leave that code unchanged and document its limitations.
Next recover the native local TVM as an explicitly named local source with its
own options/backend, preserving existing legacy/public profiles and avoiding
registration as public TON/default authority. Adapt the teaching test/launcher
closure to that exact native class, use retained SDK/compiled state where valid,
and keep current native/hosted gates red until actually verified. No SDK restore,
node/ledger/key/contract replay was performed in this review.

## Reproduced adjacent V2 defect fixed

The new shared EVM CQL implementation advertised authenticated private reads
without a positive configured chain identity. Its private decryption context
requires that identity. A new planner test reproduced the contradiction (1fail).
QueryCapabilities now exposes no fields/features until that prerequisite is met.
17owned-CQL controlled tests pass after the fix, including actual V2 Arbitrum
factory and production ABI/crypto with controlled transport. No native replay.
This fixes the V2 admission boundary; the legacy TON wrapper remains unchanged.

Original before/after TRX and hash manifest:
LocalState/COSMIC/LauncherRecoveryCurrent/web3-v2-chain-admission-evidence.json.
Coverage remains local94/hosted60 and5missing published launcher paths.

## Explicit native-source recovery milestone

TonLocalTvmStorageSource now owns native local TVM storage/CQL and lifecycle.
TonSandboxStorageOptions/worker/backend require an exact admitted local source;
private storage encrypts canonical records and rejects inactive access. Legacy
TONOASIS.cs/DNA/public header sources untouched. Four controlled cases pass,
provider/native+federated consumers compile and PS/JS syntax pass. Existing SDK/
compiler install reused. Next native contract compilation/disposable fixture
acceptance;not yet performed, no hosted promotion. See TON_CQL_MVP.md.
