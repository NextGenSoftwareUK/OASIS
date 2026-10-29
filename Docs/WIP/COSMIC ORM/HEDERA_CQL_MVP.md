# Hedera / Hashgraph CQL MVP - controls and public audit pass, native demo pending

29 October 2026. HashgraphOASIS implements ICosmicQueryStorageProvider over its
existing configured encrypted backend and official Hedera SDK path. No mirror-query
substitute, scan evaluator or fallback execution path was added.

## Scope and controls

Shared bounded-ID compilation binds exact HashgraphOASIS authority/catalogue:
ID equality/membership, Boolean algebra, paging, at most100 keys. Unsupported
metadata/content/order/children fail before I/O. Canonical record keys match saves;
existing backend owns encryption/signing/query policy. Any failure/cancellation
publishes no partial rows. Separate reads may observe different consensus moments.
Result and post-decode4MiB budgets apply; no underlying allocation/snapshot claim.

HederaCqlTests:8/8 zero skips,74ms. Seven compiler/catalogue/admission controls plus
one canonical-contract-source build check. Inactive local endpoints never resolve
storage credentials or submit a paid query. Report
`TestResults/HederaCqlCurrent/hedera-cql-controls.trx`, SHA256
`F7C8508E53790664C85851F0BFFD2A804BBAD44DD44DAE830905BEDB035F79EA`.
No native CQL3/2, restart or paired/browser behavior is proven yet.

## Free public-source audit

Public testnet mirror HTTP200 confirms account0.0.10993779 active, contract
0.0.11017885 active, backing file0.0.10994223. Observed test balance32485764tinybars
is indexed testnet information, not consensus funding/admission proof.
Protected identity file exists at LocalAppData/OASIS/ReusableProviderTests/Hedera/
testnet-identity.dpapi422bytes; it was not decrypted or recreated this milestone.
Historical TestResults/HederaRuntime/deployment-store.json is absent in this worktree.
Do not fabricate old deployment/signed-attempt history from public observations.

The exact native5484-byte runtime matches the canonical current shared Solidity
source and immutable owner using cached solc0.8.26, EVMparis. Runtime SHA256
`2b14201abfe8272fba1968b87af913847bfedc186f98564ccd21342bf752548c`;
canonical source SHA256
`70436f62a357a164233594fc560324e42981bda282d1f619737bc4e79d61a762`.
Public report `TestResults/HederaCqlCurrent/mirror-runtime-verification.json`, SHA256
`CDFAE6A86E4CCB702EFDCBFE481C15ABE5D10B0F009688136076AA9EF8D8E6EA`.
Raw public mirror responses stay under that TestResults directory, not docs.
Zero consensus queries, submitted transactions, paid requests or account creation.

## Reproduced root cause fixed

Initial strict verification failed at byte5441: Windows auto-CRLF changed Solidity
metadata, while Git's canonical LF source matched deployed bytes exactly. The
specific .gitattributes rule now keeps OASISKeyValueStore.sol LF on every platform;
the actual checkout was normalized to identical canonical Git content. A build
check rejects CR bytes or a missing checkout rule. Contract logic/ABI is unchanged.
Full runtime bytes including metadata are still compared; no hash relaxation or
alternative source parser was introduced. Failed-CRLF diagnostic report retained as
mirror-runtime-crlf-rejection.json before successful canonical-source verification.

## Exact next step

Reconcile historical retained deployment/admission evidence into persistent state
without inventing missing outcomes; use public proof only as current-source evidence.
Inspect original testnet identity/funding/policy and the existing paid-query
preparation owner before any consensus read. Hedera native queries may consume
explicit testnet query credits; whole-query ceilings/preparation must cover all
selected keys and index reads. Do not spend mainnet HBAR or provision paid services.
The old prepared-read runner requires original pending-state checks and conservative
funding buffers; do not bypass them merely to obtain a CQL pass.

Then admit/reuse a dedicated three-record source, prove native3/2/missing and
restart, and Mongo-paired SDK/HTTP/browser5/4. Existing source SDK node_modules is
absent; inspect cached packages before restoring once. No runtime/download/service
was introduced here; cached EVM compiler and .NET resources were reused.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
