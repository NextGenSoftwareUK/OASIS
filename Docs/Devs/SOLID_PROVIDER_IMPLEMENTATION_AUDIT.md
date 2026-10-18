# SOLID provider implementation audit

2026-10-18. Incomplete; no full-provider release certification.

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
