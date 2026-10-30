# BNB Chain V2 CQL MVP — controls verified, native pending

Updated 29 October 2026. BNB Chain remains red in the all-provider demo inventory.

## Reproduced root causes and fixes

The shipping Web3Core BNB class inherited CQL, but its authority was excluded by
the compiler and its constructor omitted the chain identity needed by encryption.
Initial controls reproduced3failed/5passed: missing default56, rejected trusted
catalogue and an access error before expected inactive-source handling.

- Admit explicit BNBChainOASIS authority to the existing bounded GUID compiler;
  trusted catalogue/frozen selection/unsupported-shape rejection, at most100IDs.
- Preserve original three-string constructor and delegate to one initializer with
  default56. A positive BigInteger chain overload supports explicit networks,
  including31338 for retained local tests. Zero/negative chains reject before access.
- Legacy BNB code remains unchanged. Central BootLoader BNB registration is
  commented out, so later direct-construction demo proof must be labelled clearly.
- No alternate query/storage path, factory redirection or network probing added.

## Verification

[BNB controls](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/BNBChainCqlTests.cs):
**11passed**. Other changed-compiler EVM regression controls61 pass: **72total**,
zero failed/skipped. Web3Core/BNB/test producer builds pass; cached-only restores
and bounded workers. No native service/contract/actor/key created for this step.

Evidence under `TestResults/BNBChainCqlCurrent`:
- `bnb-repro.trx` SHA256 `28c45d15d921963295286275512d11111679432201d0841d1174e54b89da3cb4` (original failures retained).
- `bnb-family-controls.trx` SHA256 `95f9675d86f614aaeb56a217283da097d4f640e194811c2e2d99b424a104df90`.

## Exact next step and remaining gates

Reuse retained Ganache7.9.2/solc0.8.26/ethers6.13.1, original shared EVM31338 chain
files and DPAPI key. Admit one BNB-specific original contract/actor/threeIDs,
journal pre-dispatch intents/deployment receipt/write acknowledgements and native
ciphertext-response hashes. Explicit local31338 identity must reach encryption.
Verify native membership3to2 and unchanged hashes/nonce, then restart without source
writes and Mongo-paired C#/HTTP/browser5/4/hub. Do not increment demo count early.

Direct construction is distinct from central registration. Public BNB Chain
consensus/finality/bridging/gas and production DNA/factory routing are unverified.
Full graph/history/conditional concurrency/placement/EF parity remain open.
