# Polkadot Assets implementation and remaining API work

## Proven implementation stage

2026-10-18 follow-up: `submitAssetPreparation` now uses the existing verify → durable owner acknowledgement → exact-original one-shot submission pipeline; both native and asset write operations require the owned channel before starting network/process work. `assetOutcome` independently verifies the signed original and reads its canonical finalized events, using a checkpoint-derived bounded search. Shared finality, fee parsing and original-range helpers replace duplicated readers. Pool acceptance is explicitly NOT finality. Unknown outcomes retain the original identity and must never be replayed.

The asset receipt preserves requested and observed amounts separately. Exactly one matching `Assets.Issued`/`Assets.Burned` event must name the original asset/account. Short burns, dust over-burns and zero effects cannot become exact success. FRAME transaction fees remain explicitly partial coverage (`frame-transaction-fee-only-not-all-asset-native-outlay`); this does not certify deposits, refunds, sponsorship or shared NetFinal accounting for assets.

Real retained-ledger SDK verification minted `1250001` baseunits (transaction `0x814fc80149484ad8b7dfcda3010ba04fe2387f8360f0807de41e2c300a984964`) and burned `500001` (transaction `0x713a93d179343fbf146d09b5ef8bf1fd59f8beb459bc286270f7a3cff23e719d`). Independently observed balance AND total-supply deltas matched each finalized event. Fixed, flushed completion evidence remains under `TestResults/PolkadotCurrent`; an unresolved pending original fences subsequent runs. No new runtime/download/asset/ledger was created. Reuse with `Scripts/TestHosts/Run-ReusablePolkadotNativeTests.ps1 -Suite AssetSdk`; the default `Native` suite is separate. SDK117/117, focused C#70/70, native regression2/2 and packaging9/9 passed. These scoped checks do NOT prove hosted authentication, real avatar-wallet binding, ordinary C# mint/burn, all runtime variants or V2 certification.

The retained official local runtime includes `pallet_assets`; no new runtime/SDK/install was required. `asset-operation.mjs` prepares and independently verifies real signed Assets `mint`/`burn` SCALE calls. It reads existing asset details/metadata, uses the asset's own decimals (not native decimals), checks issuer/admin signer role, and preserves exact positive u128 units. Unsupported memo input is rejected, not discarded. Burn preparation rejects insufficient balance and a remainder below the asset's minimum that would cause extra dust burning. These checks are a snapshot, not a future-execution guarantee.

Native and asset operations now share the SAME explicit signer lifetime, original SCALE signing, versioned signing document, cryptographic verification and fee estimation helpers. Intent authorization stays separate: native verifier only permits its native keep-alive/memo shape, asset verifier only exact Assets mint/burn. Changing/removing an outer protocol label cannot make one call family authorize another. Generic cryptographic verification alone never authorizes a submission.

Preparation protocol: `oasis.polkadot.asset-operation.preparation.v1`. Bridge operations `prepareAssetOperation` and `verifyAssetPreparation` remain read-only/signing operations under the hard read-only RPC provider. They DO NOT submit or grant spending. The separately owned `submitAssetPreparation` operation submits only the persisted original; ordinary API implementations are not replaced yet.

## One retained local asset fixture

Same ledger/genesis/runtime/SDK as [native tests](POLKADOT_PROVIDER_NATIVE_TESTS.md). Asset ID `424242`, name `OASIS Fixture`, symbol `OFT`, precision6, minimum balance1. Public local Alice atomically creates it, sets metadata, seeds `10000000`asset baseunits to the public02seed actor, then sets issuer/admin/freezer to that actor. No public funds. Create/metadata calls reserve native deposits: the FRAME transaction fee is NOT all reserved/deposited native outlay.

Bootstrap is create-only and fsync-before-broadcast with one exact original. A collision or pending original stops it. Reuse verifies original bytes, identical expected call, canonical finalized receipt, live roles and metadata; never destroys/replaces the asset or mints another seed balance per run. Original block8/transaction `0xabe5403d2dab41f67a687dbb5d76e0c4bc35250c69319c8af99dbb0efbc5463c` is retained in `local-asset-bootstrap-pending.json.completed.json`. Source/receipt hashes are in release gates. The ordinary native-only fee reader rejects this mixed asset/deposit fixture.

From the existing SDK directory, with the same node resumed as documented in the native guide:

```powershell
$env:OASIS_POLKADOT_LOCAL_NATIVE='1'
$env:OASIS_POLKADOT_LOCAL_ASSET_JOURNAL='C:\Source\OASIS\TestResults\PolkadotCurrent\local-asset-bootstrap-pending.json'
node bootstrap-local-asset.mjs
node test-local-asset-preparation.mjs
```

The preparation test signs/verifies `1.250001`tokens as `1250001`baseunits and burn `0.000001`as1baseunit, rejects11malformed/cross-family/wrong-role/precision/insufficient/missing-asset cases, and independently checks balance and supply unchanged. NO mint/burn preparation is submitted by that test. SDK103checks pass, and actual native sync/async transfer+fee checks still pass after the shared signing refactor. Node stopped after all terminal checks; same fixture retained.

## Required next ordinary API work — NOT complete

1. Replace old `MintToken`/`BurnToken` unsigned JSON paths. Current code ignores typed `Amount`, guesses native precision, invents a burn amount/account and can mistake HTTP200 for execution. Preparation does not certify those methods.
2. Reuse existing avatar-wallet/KeyManager authorization and encryption, not private keys hidden in metadata, first-key guessing or configured service-key fallback. MintedByAvatarId defaulting/override authorization belongs to existing authenticated API normalization (logged avatar; privileged overrides), not a guessed global avatar in the provider. Bind actual signer to the admitted expected canonical account. Burn uses explicit typed amount and defined holder authority, never an empty account/default amount.
3. Canonicalize existing typed token/recipient fields and documented metadata aliases at one request boundary; reject conflicts. Native Assets addresses are canonical numeric asset IDs, not EVM contract addresses. No implicit new asset creation inside mint.
4. Reuse shared original persistence, one-shot submission, finality and reconciliation machinery with explicit asset intent validation. Do not weaken native-only owner/fee validation to admit arbitrary calls. Preserve original IDs and never resubmit unknown outcomes.
5. Verify exact `Assets.Issued`/`Assets.Burned` event asset/account/amount before API logical success, not just System dispatch success. **Burn is best-effort and may burn LESS than requested or MORE because of dust.** Preparation balance checks cannot eliminate concurrent changes. A mismatch requires reconciliation of actual effects, never replay. Asset role semantics differ from ERC20 holder-directed burn: native pallet burn is admin-controlled.
6. Review asset account/deposit/refund/native outlay and sponsored fee limits before extending shared NetFinal fee coverage. Native-only fee reader MUST remain closed to asset calls until appropriate full coverage is proven. Ordinary API/Mongo owner/billing/hosted tests, assets transfer and other relevant interfaces remain required.

Primary protocol references: [Assets calls and burn semantics](https://paritytech.github.io/polkadot-sdk/master/pallet_assets/pallet/enum.Call.html), [asset roles](https://paritytech.github.io/polkadot-sdk/master/pallet_assets/struct.AssetDetails.html). Real local metadata is authoritative for supported method/types/decimals; public-network/runtime variants remain unproven.
