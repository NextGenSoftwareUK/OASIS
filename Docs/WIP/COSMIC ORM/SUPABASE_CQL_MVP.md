# Supabase CQL MVP — controls verified, native pending

Updated 29 October 2026. Supabase remains red in the provider inventory.

## Implemented and verified controls

Provider uses PostgREST directly with API-key/Bearer headers and the existing
plaintext canonical `data_json` column beside UUID primary key `id`, parent ID,
holon type and deletion projection. No new SDK or alternate store was introduced.

- Explicit Supabase-only CQL capabilities reuse the bounded identity compiler:
  frozen GUID equality/membership/boolean composition/paging, at most100 trusted IDs.
- Native `id=in.(...)`, deletion exclusion and `order=id.asc&limit=100`; unsupported
  business predicates are rejected. No whole-store managed predicate filtering.
- One canonical reader serves holon point/parent/all-record/CQL reads. It enforces
  HTTP success, a complete exact `Content-Range`, row identity/payload agreement,
  no duplicates, live records, strict UTF8 and a 4MiB streamed-body bound.
- CQL verifies authorization/selection and native ordering, supports cancellation,
  and rejects incomplete results or budget overflow without returning partial rows.
- Missing/rejected activation table fails; activated state reflects lifecycle.
- Save freezes the full derived payload and proposed ID/key before dispatch,
  requests a returned representation, and verifies identity/payload/projections.
  Caller ID/key and saved count publish only after acknowledgement. An uncertain
  dispatched result carries the native reconciliation contract; no automatic replay.
- Decimal precision remains in the canonical JSON. The manager owns save baselines.

[Controls](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.UnitTests/SupabaseCqlTests.cs):
**32 unit tests passed**, zero failed/skipped, including existing provider tests.
Provider/unit builds pass with bounded workers and cached-only restores.
Raw evidence: `C:/Source/OASIS-cosmic/TestResults/SupabaseCqlCurrent/supabase-controls.trx`.
SHA256: `62f78b01219c775e577ebebcd297ba972e628710a54a285f0a9d46d11ac9a878`.

Native range/count behavior follows [PostgREST pagination](https://docs.postgrest.org/en/stable/references/api/pagination_count.html);
returned-save representation follows [PostgREST preferences](https://docs.postgrest.org/en/stable/references/api/preferences.html).
These HTTP controls do not establish native Supabase compatibility or durability.

## Exact next step and remaining gaps

No Supabase/PostgREST executable was found on Ubuntu PATH or under
`/var/tmp/oasis-runtimes`; no hosted account/service/database/identity was created.
Inspect existing PostgreSQL/container/runtime caches before admitting one reusable
free native REST/PostgreSQL environment. Document its distinction from hosted Supabase.
Create one persistent schema/identity with pre-dispatch write journals, verify
several original records across restart, then add one Mongo-paired C#/HTTP/browser
preset using the shared teaching host. PostgreSQL-provider proof does not prove
this REST adapter. Leave the demo inventory red until those checks pass.

Atomic cross-client conditional writes, general child/depth/history semantics,
avatar/detail ownership, bulk atomicity, full query parity and central registration
are not certified here. Supabase currently uses a privileged configured REST key;
production row-policy/group/auth configuration needs independent acceptance.
