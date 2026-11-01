# TRON public header qualification

Updated 1 November 2026. Qualification only; no CQL/API/hosted grant or count increase.

While the exact Sui deployment is queued, three anonymous header-only reads from `https://api.trongrid.io/walletsolidity/getblock` at heights0/1/2 returned HTTP200. Bodies were522bytes each, no transaction bodies. The observed1→0 and2→1 parent links agree. No wallet/API key/account/runtime install or transaction was used.

Official contract: [SolidityNode GetBlock](https://developers.tron.network/reference/getblock-2) supports explicit height/ID and header-only selection. [SolidityNode GetBlockByNum](https://developers.tron.network/reference/getblockbynum) describes successful block results as solidified in that node view. This observation is not independent consensus or signature verification.

Mainnet genesis observed: `00000000000000001ebf88508a03865c71d452e25f4d51194196a1d22b6653dc`. Verify network identity explicitly in any future adapter. Genesis raw_data omits number/timestamp and has a special witness payload; do not treat it as a normal account address or silently invent general defaults. A future demo can select ordinary early headers1/2/3 while checking genesis separately.

Raw public observation: `TestResults/TronPublicSourceCandidateCurrent/readiness.json`, SHA256 `894646b9935fa21f92ff8bdb552956aa8112a29ff56c922e44e7fe5a2f4ed3a8`, 1985bytes. Source identity/catalogue not created yet.

Next after Sui hosted gate: qualify third ordinary header and strict native schema/precision/identity rules; implement explicit bounded read-only header profile, tests and commented C# sample; then admission/planner/localHTTP before hosting. Existing TRON signed wallet provider remains separate. No generic scans/metadata queries/signed writes are certified by this probe.
