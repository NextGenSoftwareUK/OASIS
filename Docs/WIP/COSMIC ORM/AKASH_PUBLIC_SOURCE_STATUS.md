# Akash public source — next interface review

2 November 2026. Red/unqualified. No native probe, source owner, CQL adapter,
query admission, hosted card or count increase. Eclipse remains the active
release; this review uses its verified deployment wait without altering it.

Existing adapter: Providers/Network/NextGenSoftware.OASIS.API.Providers.AkashOASIS/AkashOASIS.cs.
It targets api.akash.network Cosmos REST, maps balances to avatars and deployments
to holons. Its node-info activation does not establish complete identity/query
semantics. Legacy implementation remains unchanged; gaps are documentation-only.

## Official interface distinction

[Integration overview](https://akash.network/docs/api-documentation/getting-started/)
separates anonymous indexed network data from authenticated managed deployment
APIs and blockchain-node RPC. A public catalogue demo must not imply it creates
compute deployments or verifies chain state. No key/card/wallet is needed for the
documented network-data scope; native anonymous eligibility still needs testing.

[Provider API](https://akash.network/docs/api-documentation/rest-api/providers-api/)
documents https://console-api.akash.network with GET /v1/providers and
GET /v1/providers/{address}. Records expose registered owner, display name,
service URL, observation flags and resource measurements. Fields can be nullable
or change between observations; indexer health flags are not an uptime guarantee.
The list endpoint documents scope=all/trial, not arbitrary server-side pagination.

## Proposed bounded read-only MVP

Before any native request, record one exclusive endpoint/method/byte/time budget
intent using the production guarded .NET transport. Reuse existing runtimes and
Cosmos/bech32 helpers; no installation, database, wallet or cloud allocation.

Discover at most one bounded provider list, freeze three unique canonical akash
owner addresses from that observation, then qualify three individual reads.
Stop at the declared byte/depth/time limit; do not loop pages or substitute
another endpoint after failure. No final catalogue/owner allocated until all
three records are proved; point selectors must match the requested owner exactly.

Prefer a strict public metadata allowlist: owner, safe display label, service URL
as text, explicit observation flags and bounded exact integer capacity values.
Exclude contact email, exact IP/geolocation, arbitrary attributes and unneeded
nested payloads. Never connect to a provider-supplied hostUri or execute its
workload endpoint; the URL is descriptive data, not an instruction.

Potential CQL scope is configured AkashProviderAddress equality/IN and paging,
then actual spaced C#3/2, HTTP/Mongo5/4/privacy, original hosted/save/replica
acceptance and website card. Label it indexer-reported provider metadata, not a
shared finalized snapshot, consensus proof, owned cloud database or provisioned
compute service. Unknown values remain explicit, not zero or inferred availability.

Next after Eclipse's existing release completes: define exact discovery/probe
budgets and test identity/response codecs, then freeze the native intent. No
Akash native request or environment has been created during this review.

## Pure identity/response codec verified during Eclipse wait

Production AkashPublicProviderCodec validates canonical lowercase44character
akash Bech32 addresses (20bytes/checksum1), exact requested owner, unambiguous
root fields, bounded credential-free HTTPS service URI as descriptive text,
nullable display name and explicit online boolean. It copies only four declared
fields; no contact email, geolocation, arbitrary nested data or capacity/audit
claims. No request is made to a provider-supplied URI. Capacity measurement
semantics remain unqualified rather than guessed from numeric JSON.

13controlled tests pass0failed/0skipped using the actual Akash project in the
reused Ethereum test host. Official example address passes checksum; corrupted
checksum/prefix/case/width, mismatched owner, duplicates/errors, credentials,
unsafe URI scheme, bad labels and boolean types reject.

Evidence TestResults/AkashPublicSourceCurrent/codec-controls.trx, SHA256
65b1bc53672e507045f1f768d5e519b8cd0a99de9e7d6c4cc8460a80f5f0d9cc.
No Akash native call/source owner/config/admission/count increase. Existing
Eclipse source/candidate/release unchanged. Next after the Eclipse release: exact
bounded discovery/probe intent and guarded native reader qualification.

## Bounded HTTP reader controls passed

Production AkashPublicProviderReader separates initial discovery from configured
point reads. Discovery is one GET/2MiB/max1024unique canonical owners, selecting
three deterministically. Queries never rediscover;1–8explicit owners,256KiB per
point response,30second method deadline and16JSONdepth. Point selections either
complete or throw, no partial rows or retries. Conservative process-local1req/s
admission, not an upstream SLA or distributed rate cap.

Eight controlled tests pass0failed/0skipped: discovery3/point3, request spacing,
descriptive hostUri never contacted, duplicate/incomplete/invalid discovery,
final-point HTTP failure, identity mismatch, oversized body and no-dispatch invalid
or cancelled selection. Test fixture public addresses are synthetic, not wallets
or funded accounts. Existing codec13controls remain the separate prior proof.

Evidence TestResults/AkashPublicSourceCurrent/reader-controls.trx, SHA256
00dbd926283cded8eb6e8d8c2e0803d2d584bc81ec444bcd584974cd1a06d19e.
No native Akash request, discovery intent, source owner, CQL or hosted admission.
Next explicit native qualification intent remains pending; Eclipse release
failure diagnostics are current priority.

## Original native qualification failed at bounded discovery

Original exclusive qualification intent persisted before dispatch at
LocalState/COSMIC/AkashPublicQualificationCurrent/qualification.intent.json.
One discovery request reached a successful HTTP status, but its body exceeded
the declared2MiB limit; reader stopped before parsing a complete catalogue or
dispatching provider point reads. Native test1failed/0passed. No source owner,
CQL admission, wallet, account, node, data write or provider-hostUri request.

Failure/log/TRX retained in TestResults/AkashPublicSourceCurrent/native-*.
Native failure JSON SHA256 42bd3545b986815bb203b671233977a61eaa536c6773b141e998ee84119886b6.
No original intent/result replacement or automatic replay. The full-list discovery
strategy needs a bounded selection design; do not raise limits blindly or label
the public catalogue qualified. Existing codec13/reader8controlled proofs remain
scoped controls, not proof of successful native integration.

Next review documented bounded selection/discovery options and implement the
correct read strategy with tests before any explicit evidence-bound continuation.
Eclipse image-build failure still lacks actionable diagnostic evidence; no
Eclipse settings/activation/cohort/card has been published. Counts59/54 unchanged.
