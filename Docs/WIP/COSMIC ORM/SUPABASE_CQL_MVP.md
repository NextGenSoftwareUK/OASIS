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
- Soft delete uses the same acknowledged save, updating both canonical DeletedDate
  and native deletion projection. Rejected writes retain reconciliation errors.
- Hosted REST path defaults to `/rest/v1`; directly hosted PostgREST explicitly
  selects `restApiPath: ""`. There is no URL probing or alternate-route retry.
- Decimal precision remains in the canonical JSON. The manager owns save baselines.

[Controls](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.UnitTests/SupabaseCqlTests.cs):
**36 unit tests passed**, zero failed/skipped, including existing provider tests.
Provider/unit builds pass with bounded workers and cached-only restores.
Raw evidence: `C:/Source/OASIS-cosmic/TestResults/SupabaseCqlCurrent/supabase-controls.trx`.
SHA256: `4660c6e7b75d4453b05a562cd05c8a6efb256c216258f7bc346d7400f2471df9`.

Native range/count behavior follows [PostgREST pagination](https://docs.postgrest.org/en/stable/references/api/pagination_count.html);
returned-save representation follows [PostgREST preferences](https://docs.postgrest.org/en/stable/references/api/preferences.html).
These HTTP controls do not establish native Supabase compatibility or durability.

## Exact next step and remaining gaps

Initial Ubuntu PATH/cache inspection found no PostgREST. Official static PostgREST
**16.5** is now admitted at `/var/tmp/oasis-runtimes/postgrest-16.5` (20,507,028B).
Archive SHA256 `5099e7ada38f9f6846cd87c14c5f158b7c165f1e9959665ad412e61f4dceba95`;
binary SHA256 `cc218dc89e882ae2b828b3e8d08a1cffd29f4fa126a0869b038ac9e3a12c357d`.
Version command passed; verified4,520,424B download archive removed. No OS package,
SDK or container image was downloaded. Preparation is reusable/hash-checked:
`wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/prepare-cosmic-postgrest-linux.py`.

Reuse retained stopped `oasis-postgres` container/image/volume and loopback15432;
identities are recorded in the resource ledger. No new database, role, fixture,
JWT secret, service process or hosted account has yet been created.
Next create one owned schema/database/least-privilege role with durable setup
intent, run PostgREST on loopback18091 with explicit REST root and JWT validation.
The local REST data plane is not a hosted Supabase/Kong/Auth-stack certification.
Create one persistent schema/identity with pre-dispatch write journals, verify
several original records across restart, then add one Mongo-paired C#/HTTP/browser
preset using the shared teaching host. PostgreSQL-provider proof does not prove
this REST adapter. Leave the demo inventory red until those checks pass.

Atomic cross-client conditional writes, general child/depth/history semantics,
avatar/detail ownership, bulk atomicity, full query parity and central registration
are not certified here. Supabase currently uses a privileged configured REST key;
production row-policy/group/auth configuration needs independent acceptance.
