# Holonym / Human ID public CQL source review

7 November 2026. Source and native index readiness review only. No new runtime, account,
proof, wallet, business write, CQL count or hosted claim.
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

## Current indexed records and exact-ID contract

One bounded request using the [Sign REST contract](https://docs.sign.global/developers/api/rest-attestations/)
(size3/page1, fixed Optimism schema/attester/mode) returned HTTP200/application-json,
5985bytes, SHA256f1fe2a4cd689bbb4fcbd25a3e2d41f4bdf77dc4263090289ca46069e594c5d12.
Exactly3distinct native IDs: onchain_evm_10_0x3248d, onchain_evm_10_0x3248c,
onchain_evm_10_0x3248a. Envelope, chain10/modeonchain, published attester,
schemaid/data types and canonical bounded string/uint256[] ABI layout checked.
The advertised total147138 is index metadata, not a scanned or imported count.

The observed payloads contain5unsigned values, while the Human ID field table
lists6indices. The third circuit's literal string omits the documentation's
leading zero nibble. Schema type is string; preserve that native literal.
The first local observer's fixed66byte assumption failed at the third row;
reconciliation used the original retained bytes and actual ABI string contract,
without another request. No assigned field meanings, normalization workaround,
proof validity, active identity or native consensus claim.

One documented exact-ID GET for onchain_evm_10_0x3248d returned200/2519bytes,
SHA2562abd129de8e9fd3bf2447f67ac409da071b5c2880687e4391d813323906dd236.
All19core attestation fields match the original listed record, including raw
payload and recipients; direct fullSchemaId/rawschemaId/nested schema/data
bindings agree. Initial whole-envelope equality stopped on observed exact-route
extensions and index-maintenance timestamps, not changed credential contents.
Reconciled from the same response. Preserve sync/extra fields as maintenance
metadata; they must not silently change a prepared native record's content hash.

Next source can use this single exact-ID read contract with the fixed3IDs;
listing was discovery only, never an error fallback. Preserve raw circuit,
all exact uint256 values, schema/attester/record identity and reported state.
Broader semantic identity verification needs authoritative emitter/circuit
layout resolution. Do not substitute a legacy SBT call on index/read failure.
Require real SDK ABI round-trip validation and negative controls before CQL.

All raw/status captured before parsing under existing recovery state:
holonym-current-attestation-readiness.* and holonym-exact-attestation-readiness.*.
Redirects/proxies/retries disabled; reads only, no SDK installation. These are
source readiness observations, not CQL3/2, independent on-chain confirmation,
manager/HTTP/federation, hosted replication/reuse or browser acceptance.

## Exact-ID CQL implementation awaiting cloud qualification

HolonymAttestationQuerySource adds a current Sign-index read representation;
legacy Nethereum SBT methods remain unchanged. It preflights the complete ID
catalogue, reads only the fixed native exact-ID route, binds record/schema/
network/attester and payload types, then uses Nethereum7.0 ABI decode plus exact
re-encoding. Allocation-safe dynamic string/array framing checked first; UTF-8
literal and all unsigned values retained without guessed identity meanings.
Observed null revokeTimestamp is preserved, not changed to zero; a reported
revocation requires its timestamp. Moving index maintenance fields excluded.
No listing fallback, writes, scans, callbacks or private document operations.

Cloud workflow qualify-holonym-attestation.yml requires25controlled cases
(23new exact-ID/protocol/ABI/privacy-boundary checks plus2legacy controls),
then1real current-index CQL3/2 case with no skips. Full uint256 values, short
biometric string, allocation bombs, malformed offsets/padding/trailing bytes,
source mismatches, partial errors and stable snapshot metadata covered.
Nethereum ABI7.0.0 API contract read from the already cached NuGet README and
pinned package repository revisiona7aadf5cc89478dd91ef406a8780647acd484925;
no package installed. This implementation is not yet compiled or accepted.
No WEB4/Core read tariff, hosted admission, deployment or count increase.
