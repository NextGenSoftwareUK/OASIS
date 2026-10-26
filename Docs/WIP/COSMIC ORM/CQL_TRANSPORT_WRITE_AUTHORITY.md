# Transport write authority implementation gates

Source audit: 26 October 2026. These gates remain open; the verified REST routing
checkpoint does not enroll GraphQL or gRPC writes.

## Current authoritative findings

| Surface | Current source | Required correction |
|---|---|---|
| gRPC `DataGrpcService.SaveHolon` | Activates a requested provider, acquires the default manager, calls actor-less ordinary save | Capture authenticated actor before awaits; validate stable conditional intent before activation; use request-owned manager and shared owned-save boundary |
| gRPC `SaveHolonGrpcRequest` | No operation identity or revision map | Add new protobuf field numbers for operation ID and expected provider revisions; never renumber existing fields |
| gRPC `HolonMessage` | Flat root fields only, despite request child flags | Define recursive children and stored child-role observation contract before claiming graph parity; validate IDs/types rather than silently converting malformed inputs into creates |
| gRPC `HolonResponse` | Error boolean/message only | Preserve machine-readable error code, operation result and committed delta; define conflict/unknown-outcome mapping |
| GraphQL `Mutation.SaveHolon` | Creates a new root from three strings; uses ordinary actor-less save | Bind request actor explicitly, accept stable operation intent and conditional create semantics; this is not currently a submitted graph-update method |
| gRPC/GraphQL sharing | Both delegate grant mutation to `HolonAccess` | Preserve pinned authority, authenticated actor snapshot and full provider failure evidence; manager acquisition must not activate before admitted execution |

Sources are `GrpcServices/DataGrpcService.cs`, `Protos/data.proto`,
`GraphQL/Mutation.cs` and `Helpers/HolonAccess.cs` under the shipping WEB4 project.
The existing gRPC mapping accepts an ID but discards child graphs because the
wire message has no children. A graph-capable contract needs actual round-trip
tests; adding a save-depth flag alone cannot establish graph support.

## Shared invariants and acceptance

1. Reject missing/empty authenticated actor before manager acquisition, provider
   activation or I/O. Never trust a body actor, singleton logged-in avatar or
   client-supplied Wizard flag. Capture host-authenticated role before awaits.
2. Use `HolonWriteEndpoint` and Core owned-save planning for selected-node
   ownership/revision validation. Stable operation IDs are caller intent; do not
   generate a new ID when replaying an uncertain result.
3. Preserve explicit write authority and request routing snapshot through
   observation, ownership checks and conditional write. A conflict never enables
   retry against a replica or unconditional save.
4. Enroll transport paths in existing reservation/admission/outbox/pricing owners.
   Verify real physical attempts and settled receipts. A direct service unit test
   cannot establish deployment admission, JWT middleware or HTTP/2 behavior.
5. Preserve original `OASISResult` failure metadata, counts, diagnostic details and
   exceptions through generic projections. Do not collapse multi-error provider
   evidence into a newly constructed single-error result.
6. Run actual signed GraphQL and HTTP/2 gRPC requests against shipping transport
   registration for MongoDB, SQLite and Neo4j. Include foreign selected children,
   stale revisions, explicit provider/default races, invalid input, no-op, replay
   and unknown outcomes. Verify no I/O on pre-admission rejection.

## Immediate regression gate

`CosmicAuthorityFailureEvidenceTests.cs` exercises a provider failure containing
three errors, detailed diagnostic, exception and metadata through direct pinned
authority observation and the shared sharing helper. Both original controls
genuinely failed (`ErrorCount`: expected three, actual one). The correction uses
the existing generic result-copy owner to retain the complete provider evidence,
then rejects warnings/missing/mismatched observations without publishing a record.
It preserves existing error codes and the sharing boundary's invalid-read code.

Both original controls now pass. Two further controls verify conditional grant
and revoke failure evidence, one physical write attempt and no successful
acknowledgement. Full Core passes 3,372 tests without skips. Compressed original
red and full Core reports are in `Verification/authority-evidence-*.trx.gz`.
Final WEB4/portable gates are still being checked; this does not complete the
GraphQL/gRPC transport migrations above. Legacy behavior remains unchanged.

Legacy paths are documented only. This work targets HyperDrive V2.

Final evidence checkpoint: gate40497terminal0; portable Core and shipping WEB4 builds pass, WEB4231/231PASS0skip. Core289fa01 published. No new native cohort was run for this failure-projection change; the preceding routing native cohort is separately retained and is not relabelled as this revision's evidence. GraphQL/gRPC migration and native transport enrollment remain open.
