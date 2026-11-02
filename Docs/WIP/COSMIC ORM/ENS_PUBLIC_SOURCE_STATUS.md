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
