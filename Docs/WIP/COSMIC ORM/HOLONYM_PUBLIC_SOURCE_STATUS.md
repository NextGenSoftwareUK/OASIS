# Holonym / Human ID public CQL source review

7 November 2026. Source review only. No native RPC/index request, new runtime,
account, proof, wallet, business write, CQL count or hosted claim.
Self Protocol image42447469381 remains the active rollout priority.

The existing HolonymOASIS uses real Nethereum calls to the Optimism Hub V3
sbtOwners mapping. It distinguishes absent/expired/revoked state and preserves
transport failures as errors. Keep that implementation and existing tests.
Current constants match the [maintainer's legacy deployment documentation](https://docs.passport.human.tech/building-with-passport/individual-verifications/major-concepts/sbts).
That documentation marks EVM SBTs as legacy and directs new EVM integrations to
attestations; Stellar's SBT integration is a separate current representation.
A new current-verification demo therefore needs the attestation contract,
not a renamed legacy proof. A deliberate historical SBT profile could remain
separately named, without automatic fallback between those representations.

The [published attestation contract](https://docs.passport.human.tech/building-with-passport/individual-verifications/major-concepts/attestation-protocols)
names production index https://mainnet-rpc.sign.global/api/index/attestations,
attester0xB1f50c6C34C72346b1229e5C80587D0D659556Fd and Optimism production schema
onchain_evm_10_0x1. Its raw payload schema is string circuitId,uint256[] publicValues.
Qualification must bind network/schema/attester and interpret these unsigned
values exactly, including action, expiry and issuer. Public attestations alone
are not a fresh independent identity proof. The page also labels a Base schema
onchain_evm_10_0x8 despite listing Base's chain8453: resolve that discrepancy
against the actual protocol before any Base admission; do not guess an encoding.

Native qualification remains agent-owned: pin the maintained Sign read ABI/SDK,
prove its bounded page and exact-ID contract, retain original raw/status before
parsing, select three explicit authentic records and verify all source bindings.
No unbounded scan or invented missing records to satisfy a three-row demo.
Require malformed/cross-schema/partial-error rejection, exact-ID CQL3/2,
WEB4/Mongo5/4/privacy, immutable deployment and hosted reuse/replica/browser gates.
The unrelated untracked SignProtocol provider directory belongs to other work;
do not stage or modify it for this review.

For the legacy implementation, primary Solidity source is pinned at
[holonym-foundation/id-hub-contracts cb757697740d9b56d5aef4f7169ea41ee47c1c83](https://github.com/holonym-foundation/id-hub-contracts/blob/cb757697740d9b56d5aef4f7169ea41ee47c1c83/contracts/Hub.sol).
It exposes the mapping and a separate public-values-bearing SBT representation.
The mapping getter does not return the dynamic array; preserve that distinction.
Only tree paths/hash retained locally:81528bytes/SHA256
e3aabad70436a60ec3a2a062816c996b3dbf08078ea25178e17bfd80328971d1.
No source archive/clone, package or native environment installed. The docs edit
link could not be fetched; no access restriction was retried or bypassed.

[Inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Current rollout](CURRENT_STATUS.md).
