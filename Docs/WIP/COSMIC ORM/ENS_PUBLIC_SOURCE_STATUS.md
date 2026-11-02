# ENS public CQL source — next interface review

2 November 2026. Red/unqualified. No native probe, source owner, CQL source,
hosted admission/card or browser proof. Counts unchanged57integrated/53hosted.

Existing provider: Providers/Network/NextGenSoftware.OASIS.API.Providers.ENSOASIS/ENSOASIS.cs.
It reads a hardcoded ENS Subgraph URL, https://api.thegraph.com/subgraphs/name/ensdomains/ens,
and projects names/owners into avatar/holon metadata. Current public availability,
normalization and response contracts have not been verified in this milestone.
Legacy code remains unchanged; document its gaps rather than repairing it.

Next verify official current ENS normalization/namehash/registry/resolver read
contracts and public-mainnet RPC eligibility, using existing cached runtime/RPC
helpers. Propose an explicit bounded three-name read catalogue and filtered pair,
with precise resolver/address/snapshot provenance and unresolved records modeled
explicitly. Do not infer wallet ownership, full ENS support or independent
consensus from a returned resolution. Define supported name forms and offchain
resolution behavior before implementation; no silent gateway/endpoint fallback.

Before any native call, retain a single exclusive intent with exact endpoint,
network/selector bounds and request budgets. Use the production guarded transport,
no paid key/account/install/new node, no transactions or source writes. Only after
native qualification implement the query-only profile, controlled tests, spaced
C# walkthrough, HTTP/Mongo/privacy/replicas and original live acceptance/card.

## Verified protocol decision — 2 November 2026

Official sources reviewed (not native execution evidence):

- [ENSv2 readiness](https://docs.ens.domains/web/ensv2-readiness/):
  the upgradable Universal Resolver is the resolution entry point. The documented
  integration sentinel `ur.integration-tests.eth` must return
  `0x2222222222222222222222222222222222222222`; the old-route result
  `0x1111111111111111111111111111111111111111` must fail qualification.
- [Universal Resolver](https://docs.ens.domains/resolvers/universal/):
  mainnet proxy `0xeeeeeeee14d718c2b47d9923deab1335e144eeee`,
  `resolve(bytes name, bytes data)` returns `(bytes result, address resolver)`.
  DNS-encode the normalized name; inner record call is `addr(bytes32 node)`.
  Never substitute direct registry/per-name resolver lookup when this fails.
- [ENSIP-15](https://docs.ens.domains/ensip/15/): normalization includes Unicode
  and emoji rules; lowercasing or UTS-46 alone is not general ENS normalization.

Qualification catalogue: exactly `nick.eth`, `vitalik.eth`,
`ur.integration-tests.eth`; filtered pair: `nick.eth`, `vitalik.eth`.
These are explicit pre-normalized ASCII fixtures, not an arbitrary-name API or
suffix-based ENS detector. The initial profile must reject names outside its
configured catalogue and must not advertise full Unicode normalization.

Reuse the existing anonymous Ethereum RPC endpoint
`https://ethereum-rpc.publicnode.com/`, SDK10.0.401 and cached Nethereum7.0.0
ABI/Keccak dependencies; do not modify the completed Ethereum source profile.
Before native execution, exclusively persist the exact qualification intent.
Bound the sequence to chain identity, finalized block observation, proxy-code
check and three resolution calls at that same canonical block hash. Record the
actual selector, block hash, resolver, raw-response hashes and resolved address.
Reject malformed ABI offsets/lengths/padding, wrong chain, missing proxy code,
wrong sentinel, zero/empty records and failed RPC calls; never turn failures into
successful unresolved rows. All-or-error three-record qualification.

CCIP Read is a separate protocol requirement, not a transport fallback. Until
its guarded gateway/callback verification is implemented and tested, surface
`OffchainLookup` explicitly as unsupported for this bounded direct-resolution
profile. Do not follow arbitrary gateway URLs or replace the Universal Resolver.
General CCIP/wildcard/Unicode/multicoin/text/reverse/ownership/history support
remains red and must be disclosed in the eventual demo guide.

Existing Ethereum test host already references the Ethereum provider and its
cached ABI/Keccak dependency graph. Use it for codec/protocol qualification
checks; do not create another environment. No native call or installation was
made during this review. Next: implement bounded DNS/namehash/ABI codec and
controlled rejection tests, then record the native intent and execute once.
