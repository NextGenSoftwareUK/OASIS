# SOLID collection pagination

Implemented 2026-10-18 in the existing private-record collection reader. Avatar, AvatarDetail and Holon list/selector/graph consumers share this path; no second query backend or guessed search endpoint was introduced.

## Protocol and security

Uses the HTTP forward-traversal model described in the [W3C LDP Paging Working Group Note](https://www.w3.org/TR/ldp-paging/), including collection `Prefer` hints, explicit303 first-page handling, `Link` next/canonical/type relations and canonical sequence `etag`. This Note is not a W3C Recommendation; this implementation is not a claim of complete LDP Paging conformance.

Requests remain GETs.303 is explicitly interpreted as a page transition, not a replacement resource; other redirects remain disabled/rejected. Every page URL must stay under the configured Pod root with no URI credentials or fragment. Cross-Pod sequences are deliberately refused to avoid disclosing authentication to a linked server. Applications requiring cross-Pod paging need a reviewed per-origin authorization design, not automatic credential forwarding.

One strict parser handles combined/multiple HTTP Link fields, quoted parameters, escaped characters and commas/semicolons inside quoted values or URI references. Duplicate parameters, alternate anchor contexts, ambiguous next/canonical links and malformed syntax fail visibly. No regex guesses that a partial page is a complete collection.

Every paged representation must identify the exact original canonical collection and a nonblank sequence ETag. A changed marker aborts without a retry/restart or partial result. Distinct page URLs are visited once;1000requests/redirect hops and100000merged triples are safety ceilings. Existing10000record and exact canonical member-namespace validation remain. All pages merge before any encrypted member reads; members deduplicate before reading/decrypting. No partial result is published after an unsafe/missing/corrupt page or record.

The marker checks concern collection page consistency, not a transactionally stable snapshot of separately fetched member payloads. No such snapshot guarantee is claimed. Reads cost one request per page plus one per distinct encrypted member, and local decryption; provider hosting may charge for requests/egress. No measured production fee or indexed-query performance claim.

## Evidence and remaining gates

Controlled194/194 tests pass303ms, zero skipped.16new paging cases cover sync/async all3record kinds, duplicate member merging, quoted Link delimiters,303, changed/missing/wrong canonical markers, unsafe origins/namespaces, cycles, malformed/ambiguous links and later-page410. Existing unverified-paging test now rejects missing canonical identity rather than all pagination.

Current retained Community Solid Server4/4native tests pass5seconds, zero skipped, proving existing encrypted CRUD/graph/query/import/export paths continue working. That server's run does **not** prove native paginated responses or every protocol variation. Actual paginated-server interoperability, OIDC/DPoP, history, full interfaces/options and complete hosted/V2 acceptance remain OPEN.

Fixed reports under `TestResults/SolidCurrent`: unit SHA256 `8E50E223D27C6BE9DC96C3ACA52156C2D0AD1179D114328D3F86FF8F7E2BC3C4`; native SHA256 `819D26C2D4A4F2A0883F8650C00099A97C3362155A22C890F86944B9BB3DA120`. Same retained runtime/Pod and package cache; no installations/downloads/new environment. Operator-owned Pod remains running, as required by its reuse-only runner.
