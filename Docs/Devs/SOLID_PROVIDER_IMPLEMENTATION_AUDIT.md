# SOLID provider implementation audit

2026-10-18. Incomplete; no full-provider release certification.

## Import/export integration and retired transitional code

Import now uses existing encrypted graph save, preserving bool status/flags/native partial-write causes. Export uses encrypted flat holon query and existing CreatedByAvatarId scope; username/email via private identity lookup. Counts set consistently; no guessed/import or/export route, no plaintext export request body. Scope follows shared provider's holon contract, NOT a complete account/avatar-detail backup.

11newcontrolled cases genuinely failed before; final118unitpass255ms after removing9obsolete reflection tests for retired unused plaintext/RDF projection parser. All actual encrypted codec/public-operation tests remain. Intermediate127 included those dead-helper cases and is superseded. Removed SolidRdfRecord, its private projection callers/writers and unused search parser recoverable in Git; encrypted dotNetRDF runtime still used. Initial historical parser stage below is now superseded, not an active alternative plaintext record format.

Native server/auth proof still blocked; no new launch/bypass/download. Deletion/history/pagination/conditional writes/recovery/continuation/manager options/hosted and full all-provider gates remain unfinished.

## General search integration

Search sync+async now reuses StoredRecordSearch validation/matching and canonical Core SearchParams snapshot, not POST/oasis/search. Targeted encrypted avatar/holon reads, existing masks/group/metadata/owner/parent semantics, deleted exclusion/counts and shared graph projection. Snapshot before await prevents caller mutation changing in-flight query. Invalid input classified before I/O; downstream error envelope preserved.10newcontrolled cases/full116unitpass160ms; text/numeric/UTC-date masks and credential matching exclusion tested. No actual pod/network/hosted proof.

This supports tested Core request DTOs; arbitrary external nested interface implementations may be rejected by restricted serializer, not silently approximated. O(n)scan with existing bounds/no pagination/history. Internal search returns private storage DTOs; credential exclusion in matcher does NOT redact response payloads or replace API authorization. Full field/mask/options/continuation/manager integration, deletion/import/export/history/pagination/native OIDC/DPoP/server/ETag/durable recovery/hosted gates remain unfinished. Current runtime startup policy gate unchanged; no alternate launch.

## Single-root graph read integration

LoadHolon GUID/string sync+async now honors provider-linked children, recursion and max depth through existing shared graph projector after encrypted point read + collection read. Disabled/provider-false requests remain point-only. Key parsing centralized; negative depth fails before I/O; native point404 retains canonical RecordNotFound. Failed child read/cycle returns error without partial root success.14newcontrolled cases/full106unitpass136ms include graph-save→single-load round trip in both call modes and independent corrupt/cycle controls. Exact hashes/true-before count in release gates.

Point read and collection reads are not an atomic snapshot. Explicit history still unsupported; root-requested version separated from current independently-versioned descendants by design, not historical verification. General continueOnError and manager/provider child flags, pagination/version/deletes/import/export/general search, native pod/auth/ETag/durable recovery/hosted remain unfinished. Runtime startup policy gate still unchanged; no native pass claimed.

## Selected graph write integration

Single and batch holon saves now reuse shared HolonGraphWritePlan. Whole selected graph preflight/snapshot before I/O; depth/recursion/initial-depth and parent identities staged; each encrypted record PUT/readback uses existing native path; only verified node IDs/parent/key acknowledged onto caller sources. Selected writes preserve canonical Holon contract fields, not arbitrary extra subclass-only properties. IsSaved only after whole selected graph succeeds. Later failure retains acknowledged resource URIs in KeyValuePartialMutationException; no fake rollback/atomicity. Unknown first HTTP write now implements existing reconciliation marker, with resource URI separate from empty chain transaction ID.

14newcontrolled cases/full92unitpass113ms,12genuine before failures; shared408unitpass1s. No actual pod, concurrent native writes, durable recovery or V2 routing proof. Graph stops at first failure instead of silently skipping and claiming success, even when continueOnError=true. Full continuation option/manager orchestration/saveChildrenOnProvider semantics remain gates. Single-root LoadHolon child projection, versions/pagination/delete/import/export/general search, native authentication/server and ETag/uncertain journal/hosted remain unfinished. This does not complete relevant provider interfaces or shrink all-provider scope.

## Metadata and parent-query integration

Metadata single/pair All/Any and parent GUID/string queries now select decrypted native collection records locally, reusing shared StoredRecordSearch and HolonGraphProjection rather than guessed Solid search endpoints. All-holon reads share the projector. Captures metadata dictionary before awaiting; rejects invalid mode/blank key/type/depth before HTTP. Supports provider-linked child projection and initial-depth limit through existing shared semantics. Root metadata filters do not suppress nonmatching child records.11newcontrolled cases/full78unitpass229ms; exact true red/green hashes in release gates.

Fixed reversed continueOnError/loadChildrenFromProvider names/defaults on sync string-parent override. Core interface calls with distinct flags now project correctly. CLR signature unchanged; concrete-type callers compiled using old optional constants/order should rebuild. No native-server/live-pod proof from these tests. O(n)read/decrypt scan, not native index; limits and current-only history inherited. General partial-result/continueOnError behavior, graph writes/single-root child expansion, paging, delete/import/export/general search and hosted/auth/recovery remain unfinished. Runtime still absent after execution-policy denial; no alternate launch attempted.

## Native verification prerequisite: operator action

One official CSS7.1.8 image now retained in existing root WSL Podman: amd64 manifest digest125727897fde6e2225239a0748bf47d8fa559cac065d5694dc10f6428799e9f2, local image58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a,336257823bytes unpacked. Downloaded once after existing-cache inventory; do not pull/clone/reinstall per run.

Execution policy blocked container creation/start, so no oasis-solid service exists and NO real-pod tests have executed. Operator must permit/start the single retained service using that image, container name oasis-solid, existing root WSL Podman, loopback port13000 mapped to3000, retained volume oasis-solid-data→/data, memory768MiB/2CPU bounds. CSS arguments: `-c @css:config/file-root.json -f /data -b http://127.0.0.1:13000/ -p 3000 -l warn`. No alternate assistant launch path attempted. File-root mode permits anonymous access: ONLY loopback synthetic fixtures, not production/private-pod auth proof.

After operator setup, `Scripts/TestHosts/Run-ReusableSolidPodTests.ps1` verifies exact running image and existing test assets, builds current cached producer/consumer, provisions/reuses one DPAPI test key and runs exactly2 RetainedSolidPodTests cases with zero skipped requirement/fresh-report guard. Runner never starts/stops/removes/downloads runtime; operator owns service. Native fixture creates/reuses its/native-fixture/containers, journals intended resource IDs before mutation, checks absence before record writes, verifies actual encrypted HTTP records/collections/lookups, and raw-HTTP deletes only its confirmed generated records then verifies404. Raw cleanup does NOT certify provider delete methods. Failure leaves original journal+protected key intact; no automatic replay/reset/cleanup of uncertain operations. This code compiles; live behavior and cleanup/recovery remain unproven until service available.

Existing integration project restored once using retained caches; native tests compiled but NOT run. Legacy skipped/public-pod tests untouched/not counted. No native success report exists from this stage. Current full-provider/native-auth/history/pagination/graph/query/import/export/recovery/hosted gates remain open.

## Private identity and GUID lookup integration

Avatar/detail username+email sync/async lookups now reuse native encrypted LDP collections and match locally (trim+OrdinalIgnoreCase), with no private query value in HTTP URI. Missing identity maps to canonical RecordNotFound; duplicate identities error, no arbitrary first match. Blank values reject before I/O and upstream errors preserve cause/details. Avatar/holon provider-key reads parse nonempty GUID and access canonical encrypted resource directly instead of/by-key URLs.13newcontrolled tests/full67unitpass247ms;9true initial regressions. Exact hashes in release gates.

This is an O(n)client scan, not remote indexed search; collection safety limit/nonpaged/history limitations inherited. Native authentication/data authorization, scalable HMAC/blinded indexes, privacy review and actual pod verification remain needed. No plaintext lookup index is added. Hosting read cost/latency scales with records, and absence of chain gas does not mean free hosted I/O. Existing delete-by-email/username still has old invalid-route assumptions and needs integration; metadata/parent/graph/import/export/version/ETag/recovery also unfinished. Do not infer provider completion from lookup tests.

## Nonpaged LDP collection integration

All-avatar/detail/holon collection reads now follow native LDP contains URIs and decrypt each canonical member. Uses requested container as RDF base, checks container type, prevalidates every URI against exact owned category/GUID namespace, deduplicates, bounds10000records and returns no partial success after any member failure. Holon type filter is local after decryption. Twelve new controlled tests/full54unitpass99ms; six sync/async×3positive cases, unsafe member guards, pagination rejection, empty and corrupt member behavior. No actual Solid pod/server/authentication proof. Exact before/final evidence in release gates.

Paging and explicit history are rejected, not silently truncated. Need verified native pagination before supporting large/paged containers; current bound is explicit error, not a maximum success size. Lookups, metadata/parent queries, graph/children/continueOnError options, import/export, versions, conditional writes/ack/recovery, key/DNA lifecycle and real-pod auth remain unfinished. Earlier collection-format incompatibility is fixed ONLY for these all-record methods, not all collection/query routes. [Native LDP containment specification](https://www.w3.org/TR/ldp/).

## Single-record HTTP integration

SaveAvatar/SaveAvatarDetail/SaveHolon and GUID Load counterparts now use encrypted canonical RDF, with externally supplied key. Both sync+async facades verified through controlled HTTP transport. Full-body detached snapshot before await, PUT plus GET/decrypt/canonical equivalence, publish newly assigned caller ID only after success. Rejected4xx errors are known rejections; transport/408/5xx or readback failure retains original URI in typed reconciliation exception. No blind retry, rollback or durable recovery claim. Current-only reads reject nonzero version rather than ignoring it. HTTP resources disposed and Turtle content type checked.

Production construction: `new SOLIDOASIS(podRoot, authTokenFromSession, storageKeyFromSecretStore)`. Key is exactly32bytes, caller durably persists/rotates it; no automatic generated key or OASISDNA binding added. Existing two-argument construction still supports lifecycle/public helper behavior, but private single-record reads/writes fail without key. No plaintext fallback. Resource URI/GUID/category remain visible and need ACL/privacy review.

TRANSITIONAL: old lookup/collection/search/import/export and graph paths do NOT yet read/write the new encrypted format or implement native pod indexes. They must be integrated before release. Holon navigation/children/depth, versions, conditional writes, pod auth, ETag, durable original-attempt recovery, all fields/options/interfaces/live pod and hosted release gates remain open.12new controlled HTTP cases/full42unitpass, not real pod or full native API proof. Core LevelManager race exposed by parallel tests fixed at shared source rather than disabling test parallelism; exact evidence in release gates.

## Canonical private codec: initial stage (single-record wiring now described above)

SolidPrivateRecordCodec now composes Core StoredRecordJson, shared EncryptedRecordCodec/AES256GCM and dotNetRDF graph reader/writer. Existing canonical contract owns fields, safe stored type binder, JsonElement handling and navigation-by-ID rules; no reinvented serializer or crypto. Full-field bytes are encrypted rather than individually projected into RDF. Exact two-triple marker+payload envelope rejects plaintext downgrade. Associated data binds canonical pod namespace/resource URI, record category and GUID; decrypted ID must agree. Caller must supply and durably protect a 32-byte storage key; no ephemeral autogenerated key, credential log or key persistence added here.

Six new component cases/full30unitpass116ms (hash in current release gates): avatar/detail/holon representative fields, private canaries absent in RDF, fresh randomized ciphertext, wrong key/ID/type/pod, tamper, copied key, disposal and plaintext rejection. Real cryptography and RDF library, NO HTTP/pod traffic. This is an integration prerequisite, NOT wired Save/Load behavior. Old writer field loss remains until integrated with acknowledged HTTP writes and verified reads. No native pod, complete-field/interface or hosted/API completion claim.

Resource URI still exposes category/GUID; codec encryption does not make that safe for public publication or implement blinded search/indexing. Pod ACL, key lifecycle/provisioning, lookup/index design, native auth, immutable namespace moves and recovery need explicit integration. Cross-pod moves require deliberate re-encryption; no fallback to another key/namespace/plaintext. Canonical record navigation excluded as in other providers, so children must use existing graph/index machinery rather than assuming embedded deep graph storage.

## Implemented parser correction

Uses real pinned dotNetRdf.Core3.5.2 TurtleParser for URI-aware predicate and subject handling, not string searches. AngleSharp1.5.0 explicitly overrides vulnerable1.4.0 transitive selection. Parses only provided text through StringReader; no remote resource loader or RDF dereferencing. Groups identified subjects, decodes literal escapes, validates nonempty GUID identity, rejects duplicate identities across subjects and conflicting/nonliteral mapped scalar values. Missing/multiple records fail single-record projection. Multiple embedded subjects remain distinct in collection projection. Unknown predicates are not mapped, not certified as preserved storage.

Seven red-before cases, nine parser cases final, full24unit tests pass49ms. Exact hashes in current release gates. Tests reflect private projection helpers; do not mistake them for authenticated HTTP/pod tests. Existing explicit-null URL fallback removed in favor of error; omitted constructor default unchanged. Unit project global Xunit import repaired and obsolete description assertions updated. No new pod or credentials.

## Remaining implementation gates

- Current Avatar writer persists only identity, username/email and names; Holon only identity/name/description; AvatarDetail only identity. This is lossy and cannot be certified as complete relevant storage. Need canonical full-field/private encrypted record codec using existing OASIS/shared serialization rather than another partial model. Existing query/encryption exemption semantics must remain compatible without publishing secrets.
- List parsers handle embedded identified subjects, NOT LDP membership dereferencing. Real Solid containers generally describe membership; implement bounded authenticated contains traversal and pagination, preserving document base URI and server resource identity. Relative IRIs/document base handling requires explicit request-resource context and live fixtures; not certified here.
- `/by-email`, `/by-username`, `/by-key`, `/search` and parent-filter routes are application-specific assumptions, not a standard Solid indexing API. Implement explicit native pod resources/index design and atomicity/recovery expectations, not guessed server endpoints.
- Current writes use handwritten Turtle and return success after PUT without full record readback/equivalence or concurrency evidence. Need native acknowledgement, ETag/preconditions, uncertainty classification and original-attempt recovery; no automatic unsafe replay.
- Versions, children/depth, soft-delete propagation, complete metadata, auth lifecycle and supported media-type negotiation must be verified. Version filtering should apply to selected roots, retaining independent child versions per approved policy.
- Bearer header alone is not proof of Solid-OIDC/DPoP support; reuse a reviewed client/auth flow and existing suitable server environment before considering any installation. No live/personal pod writes merely to test guessed paths.
- RDF projection still has intentional mapping limits and some outer search helpers swallow errors. Empty containers are not proof of complete collection traversal. Existing package full-interface claims are not functional evidence.

## Primary references

- [dotNetRDF reading RDF](https://dotnetrdf.org/docs/stable/user_guide/reading_rdf.html): standard parser rather than bespoke lexical matching.
- [dotNetRdf.Core package](https://www.nuget.org/packages/dotNetRdf.Core/3.5.2): pinned Core dependency, not the all-features package.
- [AngleSharp advisory](https://github.com/advisories/GHSA-pgww-w46g-26qg): versions before1.5.0 affected; explicit patched pin used.

Full relevant interfaces and real-pod operations must be implemented/proven before deeper V2 multi-provider work; this audit does not narrow the overall goal.
