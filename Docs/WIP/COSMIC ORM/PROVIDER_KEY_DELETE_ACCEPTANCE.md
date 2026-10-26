# Provider-key deletion acceptance contract

Status: implementation in progress, 2026-10-26. Core SDK gate is verified at `5e483cb`; HTTP/native acceptance below is still pending. This supplements the existing roadmap rather than replacing its remaining scope.

## HTTP intent

Preserve the existing DELETE `/api/data/delete-holon-by-providerkey/{providerKey}` route and its legacy engine behavior. V2 must require canonical nonempty GUID headers `X-COSMIC-Holon-Id` and `X-COSMIC-Operation-Id`, plus canonical `X-COSMIC-Expected-Revision`. A revision equal to Guid.Empty is an actual token, not a create-only condition. Native keys must be nonblank and at most 4096 characters. Keep the existing two-argument action callable; any new provider selector must preserve that signature through delegation or a distinct HTTP entry point.

Validate intent before configuration, manager acquisition or provider I/O. Capture the authenticated actor and Wizard role before awaiting. Resolve the request provider once and scope configuration as on logical-ID deletion. Never use a caller-supplied role or owner. Ordinary deletion calls `DeleteOwnedHolonByProviderKeyAsync`; the authenticated Wizard policy still requires a pinned key observation, exact logical ID agreement and conditional delete with the original revision. Preserve provider failure codes/counts/metadata and committed deltas. Return the shared error-to-HTTP status mapping. No refresh of a stale revision, fallback authority or unconditional retry.

## Ordinary provider tariffs

The HTTP factory already rejects unpriced ordinary operations independently of route classification (`60adf1c`). Add explicit `LoadHolonByProviderKey` and `DeleteHolon` operation catalogue entries alongside existing operations. Bound the tariff matrix to three providers times five operations. Never alias delete to save or implicitly charge zero.

| Selected exact deployment route | Required ordinary operation tariffs |
| --- | --- |
| POST canonical save routes and existing save aliases | LoadHolon, SaveHolon, SaveHolons |
| DELETE logical-ID routes | LoadHolon, DeleteHolon |
| DELETE provider-key route | LoadHolonByProviderKey, DeleteHolon |
| POST gRPC SaveHolon | LoadHolon, SaveHolon, SaveHolons |
| POST gRPC DeleteHolon | LoadHolon, DeleteHolon |
| POST GraphQL mutation endpoint | LoadHolon, SaveHolon, SaveHolons, DeleteHolon |

Classification validates already enrolled exact routes; it never enrolls a prefix. Startup validates relevant operation rows against the selected route budget/catalogue. Runtime admission remains fail-closed for other selected routes. Existing save-only deployment profiles must not require unrelated key-read/delete tariffs.

## Required verification

- Missing/malformed/empty logical ID, missing operation/revision, invalid key and unenrolled alias: no manager/provider access.
- Key resolving to another ID: reject even when the actor owns both records; cover Wizard too.
- Stored foreign owner: forbidden; payload actor/role cannot override authentication.
- Stale original revision, changed default, unavailable primary with available replica: no refreshed condition or authority substitution.
- Soft and hard acknowledgement, failed/unknown outcome evidence, native committed delta and correct error/status publication.
- Complete 15-row tariff matrix accepted; duplicate/unknown/excess rows rejected; missing delete/key price rejected; existing save profile remains valid.
- Shipping MVC route coverage followed by signed real-provider MongoDB/SQLite/Neo4j matrix. Prejournal exact fixture IDs, freeze source/binary identities, verify attempt/receipt/settlement counts and teardown. A failed or unknown write requires exact reviewed reconciliation, never blind replay.

## Shared build handoff

Core/Shared/WEB4 sources and outputs were released to the provider session after gate 98572 (3412 Core cases, both target builds). Do not mutate or rebuild those shared resources until that consumer slot is explicitly returned. Prepare the HTTP contract and acceptance matrix here while it runs. This handoff is scheduling, not a product acceptance blocker.
