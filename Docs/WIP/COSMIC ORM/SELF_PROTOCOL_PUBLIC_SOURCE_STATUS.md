# Self Protocol public CQL source qualification

7 November 2026. Preliminary source/access review only. No CQL or hosted count
increase, provider modification, runtime installation or native business write.
SubQuery's queued Development rollout remains the immediate priority.

## Existing implementation and primary contract

The existing SelfProtocolOASIS provider already contains real Nethereum proof
verification, typed hub/registry calls, context/scope hashing and disclosure
validation. Preserve that work; a new public CQL representation must not replace
it or claim that registry metadata proves an individual's identity.

The maintained [Self MCP source](https://github.com/selfxyz/self-mcp/tree/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1)
was pinned at b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1 without cloning or installing
the server. Its [configuration](https://github.com/selfxyz/self-mcp/blob/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1/src/config.ts)
selects https://forno.celo.org for mainnet. Its
[contract tools](https://github.com/selfxyz/self-mcp/blob/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1/src/tools/contracts.ts)
name chain42220, the same hub0xe57F4773bd9c9d8b6Cd70431117d353298B9f5BF as the
existing provider, registry(bytes32), discloseVerifier(bytes32) and the mandatory
getIdentityCommitmentMerkleRoot() view. Published mainnet registry references:

| Registry | Attestation ID described by upstream | Published address |
|---|---:|---|
| Passport |1|0xd603Fa8C8f4694E8DD1DcE1f27C0C3fc91e32Ac4|
| Aadhaar |3|0x70D543432782D460C96753b52c2aC2797f26924B|
| KYC |4|0x9cABdeBC3aF136efD69EB881e02118AC612c63b9|

These are source references, not observed on-chain bindings. Resolve each selected
attestation through the hub and verify the actual native contract before admission.
Do not infer support for document types3/4 in the existing proof verifier merely
from these public metadata references. Existing proof schemas remain unchanged.
Upstream catches unsupported OFAC getter failures and returns null; do not copy
that pattern into mandatory CQL reads. Declare each representation's actual
getters and fail a selected mandatory read without publishing partial rows.

## First anonymous protocol observation

One journalled singleton JSON-RPC POST, eth_chainId/id1, to the documented mainnet
endpoint returned HTTP200/application-json,43bytes, SHA256
`5e4b9353ad4649715e16eb95f059be7cc3fe2253d7e4af996da4a3021a112b09`.
Its validated result is0xa4ec (42220). This confirms reported chain ID/access,
not genesis, finalized state, contract deployment, identity proof or CQL execution.
Redirects and proxy use were disabled; no retry, alternate endpoint, wallet,
account, private document, signature, funding or transaction was involved.

Raw/status were persisted before parsing. Original intent,43byte response,
observation and validated receipt are under
`LocalState/COSMIC/RecoveryLaptopResume20261107/self-protocol-chain-readiness.*`.
The source tree response30589bytes was inspected in memory and hashed:
`5a5cedbd3eb7d1f375db596785d308beb9e01390e0c593fcbaa69a9eaf932282`;
only its compact path/hash receipt is retained. No source archive or dependency
environment was created.

## Remaining gates

Independently bind mainnet genesis/network identity, resolve one RPC-reported
finalized canonical block, then qualify actual hub/registry code and three
mandatory registry-root views at that same block. Preserve complete response
identity, exact uint256 values and registry provenance. No source writes.

If that protocol contract qualifies, implement bounded mapped-ID CQL over the
three public registry observations using the existing SDK/shared query pipeline.
Require controlled failures, actual native3/2, manager/WEB4/Mongo5/4 and privacy,
then exact release/image, hosted snapshot reuse/replicas and browser checks.
This is a prospective public registry scope; general identity verification,
storage and full provider/ORM parity remain their separately tracked roadmap work.

[Provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Active rollout](CURRENT_STATUS.md).
