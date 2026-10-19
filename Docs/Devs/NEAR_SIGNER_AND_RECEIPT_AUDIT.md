# NEAR signer and receipt audit

## Verified NFT batch — 2026-10-19

NFT send/burn previously ignored the caller's owner signing credentials and used the service account. They now resolve the actual owner from native contract state and select that account, including named accounts. Caller private keys are forwarded to the official SDK; supplied public keys must match SDK-derived keys. Missing independent-owner keys and conflicting senders fail without substituting service credentials. Used request fields are captured before awaited activation/reads. Default service signing remains available only when the NFT actually belongs to that configured account.

Transfers verify actual native ownership equals the requested recipient before recording auxiliary wallet history. They no longer overwrite a mismatching readback with the desired owner. Burns require an existing NFT and verify its native absence after the acknowledged call. Burn supports explicit TokenId and the existing NFTTokenAddress-as-ID alias, rejecting conflicting identities. These operations use the existing custom OASIS contract, not a claim of generic NEP171 compatibility.

New shared `NativeTransactionFollowupException` implements the existing Core reconciliation marker and exposes the original acknowledged receipt and cause. NEAR uses it when post-acknowledgement verification or auxiliary history fails. Core's existing reconciliation inspector discovers the receipt through outer exceptions; no new retry or routing implementation. The shared type can be reused by other providers, but no other provider was silently migrated in this batch.

## Evidence

- Focused NEAR47/47pass2seconds/zero skipped;8new cases cover post-transfer ownership mismatch, post-burn persistence, missing NFT, public-key mismatch, missing independent key, conflicting IDs and explicit seed rejection. Controlled Node fixture is IPC boundary testing only: no SDK/crypto/network/persistence in that fixture. Not the full legacy default-mainnet template unit class.
- Shared412/412pass3seconds/zero skipped;4new cases verify cause/provider/original receipt propagation and refusal of absent receipt IDs.
- Current retained official SDK/native17/17pass165seconds/zero skipped. Existing NFT lifecycle now funds an independent account with0.1LOCAL testNEAR, transfers its received NFT back using its real private/public keys, and burns another NFT minted to that independent owner using explicit TokenId and owner credentials. Independent native reads prove resulting service ownership and NFT absence. Existing custom-token independent-holder test still passes. No public gas/funds/account service used.
- NEAR producer/native consumer DLLs both `4758024A6C8D2533A62AFFB03C8E2791D19412779B1B0596E9EABCEFF9194712`.
- Fixed reports: focused `F585E7936E5A7BFB7D61B03A3C6D7564FA633229FF2E6924B45FA02AA9F3058C`; native `848D5D400A2A4211B54A8EE5EC2534121CB2906AEC6D0F1EE9186012A11CB4BA`; shared `31E2700BAC6F9F12E073211C0936E59527619671A427D048AEE3374D13937CEA`.

## Explicit remaining gaps / security priorities

1. **Custom-token BurnToken still ignores explicit owner credentials.** It must not be treated as verified independent-owner burning. Resolve its native holder/account input contract and prevent service-credential substitution. Do not infer this NFT fix covers it.
2. **SDK lost-outcome/durable journaling remains open.** The new shared error only fences failures AFTER an actual transaction ID was acknowledged. A process/network failure before the SDK exposes that ID still needs durable prepared/submitted original evidence and reconciliation. It does not become safe to repeat because no receipt was returned.
3. **Mnemonic signing remains unimplemented.** NFT seed phrases now fail explicitly instead of being silently ignored. Implement through an appropriately pinned real library before claiming this input is supported; never silently choose a different key.
4. Public-key verification is not avatar/API authorization or sponsored-spending admission. Named-account key permissions remain enforced by the native SDK/chain. Full admission/revocation/multikey/fee/mutation-race coverage is not proven.
5. Existing token/NFT lock flags are not newly certified ID-bound escrow, cross-chain settlement or durable bridge recovery. Full NEP interoperability, all relevant interfaces/options, field history, fees and hosted/all-provider V2 acceptance remain open.
6. GenerateKeyPair still invokes generic KeyHelper before replacing NEAR fields; audit and remove any unrelated Bitcoin/SegWit projection rather than publishing an unrelated address. Current native proof covers actual NEAR public/private/implicit fields only.

## Reuse and fees

Same retained2.6.2runtime/ledger/contract and7.3.1SDK; no download/install/clone/reset/redeployment. Current and retained contract source SHA256 both `2A00FAFE8CD8C198A1F37B2496DF13903A8CE609F70FCDFF2D7A712B8CF48FA5`; cached WASM `6BC5B84CC31190A9C1EB89FEFABF45925477CAB4DFE46857CB11E6E2A9D7CB62` matches bootstrap. Reused compiled output and existing run-evidence entry point with exclusive environment lease and unchanged native code-hash checks, rather than recompiling unchanged source. Initial shell PATH quoting failed before the verifier started; corrected to direct WSL env/absolute executable, not a new node/environment.

Verified owned node PID679421 received SIGINT only after terminal test success; original session exited0, process/3030/24567 absent, all RocksDB closed. Ledger/credentials/original evidence retained. Post-run C73038942208bytes free; no large cleanup claim. Mutation gas/storage costs and auxiliary history writes exist; this local proof is not a public fee estimate, budget enforcement proof or production billing acceptance. SOLID authentication pending work is separate and excluded from this batch.
