# V2 REST conditional graph writes

Status: controller/service migration implemented; full shipping HTTP and hosted
admission certification remains open. This is not a production-completeness claim.

The existing `POST /api/data/save-holon` and `POST /api/data/save-holons` actions
use the generic Core owned graph APIs in HyperDrive V2. Existing single-save
aliases delegate to the same action. Legacy behavior is unchanged and its gaps
remain documented in the [owned graph contract](CQL_OWNED_GRAPH_WRITE_CONTRACT.md).

## Request intent

```json
{
  "operationId": "11111111-1111-4111-8111-111111111111",
  "providerType": "MongoDBOASIS",
  "saveChildren": true,
  "recursive": true,
  "maxChildDepth": 4,
  "expectedRevisions": {
    "22222222-2222-4222-8222-222222222222": "33333333-3333-4333-8333-333333333333"
  },
  "holon": {
    "id": "22222222-2222-4222-8222-222222222222",
    "name": "Edited by its creator"
  }
}
```

Use `holons: [...]` for the bulk action. Supply all required selected-node
conditions, including children. Revision values are opaque provider revisions,
not logical `VersionId` values. A null condition means create-only. The empty
GUID is an actual legacy revision, not an absent record. Creator writes may
instead preserve provider revision metadata from their loaded holons; fresh
ownership reads never replace a stale caller revision.

An operation ID is required and must remain stable when reconciling an unknown
outcome. It can appear in the body or one `X-COSMIC-Operation-Id` header. The
header accepts a nonempty D-format GUID; coalesced duplicate values and
contradictions with body intent are rejected. This supports the existing
single-save aliases whose bodies contain a holon rather than the request DTO.
It is distinct from the host usage reservation's `Idempotency-Key` contract;
neither key replaces the other when admission is enabled.

Actor identity and Wizard status come from the server-authenticated avatar,
captured before configuration or an await. Payload creator fields and role
labels grant no access. Ordinary callers authorize every selected existing node
at the exact write authority before mutation. A later foreign root/child or an
inconclusive lookup prevents every batch mutation. One conditional transaction
commits the selected batch; `ContinueOnError` cannot request partial commits.
Selection still uses `SaveChildren`, `Recursive` and `MaxChildDepth`.

The existing trusted server Wizard policy is retained, using conditional Core
saves rather than an unconditional bypass. Wizards must provide valid caller
revision intent for existing selected records. This is not a new tenant-role
authorization model or permission granted by a request-body field.

## Responses and settings

The V2 controller sets the HTTP status and response envelope consistently:

| Result | HTTP status |
| --- | --- |
| Acknowledged save or valid no-write result | 200 |
| Missing authenticated actor | 401 |
| Foreign selected record | 403 |
| Stale provider revision | 409 |
| Invalid operation intent or missing required revision | 400 |
| Inconclusive authority/admission/provider failure | 503 |

The `OASISResult` error code remains in the envelope. A 503 with an unknown
mutation outcome is not permission to replay using a new operation ID. V2 bulk
settings are reset in a finally block on ownership denial, lookup exceptions
and failed writes, subject to the existing explicit `SetGlobally` contract.

## Verification and remaining gates

The linked tests compile the actual shipping service and DTOs. Eleven new
cases cover stable intent, exact header handling, forged child/bulk ownership,
conditional Wizard behavior and null-versus-empty revision round trips.
Full Core3,302/3,302 and shipping WEB4 build pass. Native service verification
against MongoDB, SQLite and Neo4j passes all three providers: foreign bulk roots
are denied, header-based writes succeed and stale writes conflict. The existing
138SDK/138signedCQL HTTP examples also pass,345settled operations. Source and
six producer/consumer binary identities match; owned fixtures are cleaned.
Evidence: `TestResults/CosmicCqlDemo/hosted-write-evidence.json`.
This verification is separate from actual
DataController HTTP verification and from the signed CQL query walkthrough.

Before declaring hosted writes complete:

- Exercise the real shipping DataController over HTTP, including actual status,
  response error-code preservation, caller snapshots and settings restoration.
- Finish configured admission coverage for canonical and alias routes. The
  existing exact-path registry does not automatically enroll parameterized
  aliases. An inherited policy is actor-checked; an absent policy is currently
  allowed in the explicitly unconfigured mode. The host must distinguish an
  intended unconfigured deployment from a configured route that missed admission.
- Keep configuration/provider activation inside the appropriate admitted owner.
  `ConfigureOASISEngine` can currently activate an explicit provider before the
  save boundary validates intent; service-level zero-I/O tests do not cover it.
- Admit and price actual ownership reads and conditional writes using explicit
  server policy. Query-attempt tariffs do not establish write pricing; no
  invented zero, query receipt or native invoice is added by this boundary.
- Finish gRPC save migration and authenticated GraphQL actor binding before
  enrolling authoritative recipient/tenant CQL access.
