# Fantom V2 CQL MVP — controls verified, native pending

Updated 29 October 2026. Fantom remains red in the all-provider demo inventory.

## Reproduced root causes and fixes

The shipping Web3Core Fantom class inherited a CQL adapter but its authority was
excluded by the compiler and its constructor omitted the encryption chain identity.
Initial controls reproduced3failed/5passed: missing default250, trusted catalogue
rejection, and access failure before expected inactive-source handling.

- Admit explicit FantomOASIS authority to the existing bounded GUID compiler;
  trusted catalogue/frozen selection/unsupported-shape rejection, at most100IDs.
- Keep the original three-string constructor and delegate to one initializer with
  default250. Explicit positive BigInteger chain supports configured networks;
  zero/negative reject before native access. Local tests explicitly select31338.
- Legacy files were not changed. Central Fantom BootLoader case remains commented;
  direct construction must remain distinct from central registration proof.
- No alternate provider/store path, network probing or automatic retry added.

## Verified controls

[Fantom tests](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/FantomCqlTests.cs):
**11passed**. Changed-compiler EVM regression controls72 pass: **83total**, zero
failed/skipped. Web3Core/Fantom/test producers build with bounded workers and
cached-only dependency restores. No native process or source fixture yet.

Evidence under `TestResults/FantomCqlCurrent`:
- `fantom-repro.trx` SHA256 `26d3f2c12179763be5b22e1a3d9cd25810968d63bee3568b4130c1c113cd2490` (original failures retained).
- `fantom-family-controls.trx` SHA256 `7ac95c04ba2bc2d29186d41e59398dbf003c80ec1402cd68332af81e55768ba9`.

## Exact next step and remaining gates

Reuse retained Ganache7.9.2/solc0.8.26/ethers6.13.1, original shared chain31338 and
DPAPI storage key. Journal one Fantom-specific original contract/actor/threeIDs,
deployment receipt/write acknowledgements/native encrypted-response hashes.
Verify native membership3to2, unchanged ciphertext hashes/nonce and restart reuse
without source writes, then Mongo-paired C#/HTTP/browser5/4 and hub preset.
Keep demo inventory red until the paired/browser proof passes.

Public Fantom network availability/consensus/finality/bridging/gas or migration to
other networks is not certified by this local scope. Central DNA/factory routing,
full graph/history/conditional concurrency/placement/EF parity remain open.
