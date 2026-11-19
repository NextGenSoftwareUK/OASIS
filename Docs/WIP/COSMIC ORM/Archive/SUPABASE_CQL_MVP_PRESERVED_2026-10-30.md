> Superseded by ../SUPABASE_CQL_MVP.md for current integration; retained historical local REST proof and hosted limitations.

# Supabase CQL MVP — native REST and Mongo-paired demo verified

Updated 29 October 2026. The green inventory row certifies only the local REST data-plane scope below.

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

## Native REST data-plane proof

Official static PostgREST16.5 at `/var/tmp/oasis-runtimes/postgrest-16.5`
(20,507,028B), binary SHA256
`cc218dc89e882ae2b828b3e8d08a1cffd29f4fa126a0869b038ac9e3a12c357d`.
Archive SHA256 `5099e7ada38f9f6846cd87c14c5f158b7c165f1e9959665ad412e61f4dceba95`;
verified4,520,424B download removed. Runtime/admission retained; no new SDK/image.
Reuses exact stopped `oasis-postgres` container/image/volume, loopback15432.

New retained database `cosmic_supabase_demo` (7,845,555B), only the canonical
`public.oasis_holons` fixture table. Separate login `cosmic_supabase_login` is
NOINHERIT; JWT role `cosmic_supabase_api` is NOLOGIN, with table SELECT/INSERT/UPDATE.
JWT signing secret/token and database password live mode0600 under root0700
`/var/tmp/oasis-supabase-cql` (995B metadata); never include secrets in reports.
No anonymous role; missing and altered-signature JWTs reject401.
REST endpoint is loopback18091 with explicit `restApiPath: ""`.
This is the Supabase provider's native PostgREST data plane, **not** certification
of hosted Supabase, Kong/API-key gateway, Auth, storage or realtime services.

[Native test](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests/SupabaseNativeCqlTests.cs)
passed1+1 across actual REST and PostgreSQL restart, then passed1+1 again without
re-seeding. Four acknowledged saves/one acknowledged delete yield3live/1deleted;
query with missing/deleted IDs yields3live, filtered2live, paged1; unsupported name
filter rejected. Direct SQL canonical row/payload hash unchanged before/after:
`762e72718aff2d8793189610c759d6fcb68447d43ce44382874841abb9e2f7e2`.
Every initial mutation has a durable pre-dispatch intent; unresolved outcomes
refuse replay. Source/IDs/acknowledgements retained outside disposable TestResults.
Native services stopped after successful tests; no18091/15432listener remains.

Evidence under `TestResults/SupabaseCqlCurrent`:
- `native.trx` SHA256 `8dab5be97cc14b5ff0f143967aee6bfb137668020e68608544a0b591c22eb0b0`.
- `restart.trx` SHA256 `ee81a94a3c61e08c5f92223b92c3c71d6b6096a238d2d09ab6d213979f626714`.

After building the integration project with the required lease/worker flags:
```powershell
wsl -d Ubuntu -u root -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-supabase-linux.py
```
Runner reuses the original database/role/token/source and refuses occupied ports,
active container adoption, incomplete setup or unresolved writes; no reset path.

## Mongo-paired demo

[Executable C#/HTTP example/test](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests/SupabaseMongoCqlNativeTests.cs)
uses the shared commented membership walkthrough and teaching host. HolonManager
executes native REST and Mongo reads. All: Supabase3 + Mongo2 = **5physical rows**;
subset2+2 = **4**, preserving provenance and excluding an unrelated Mongo record.
Forged actor/unknown preset reject400; inactive secondary rejects503 without rows.
Direct run and actual hub/browser run each pass native1/restart1/Mongo bootstrap1/
paired1. Browser tab36 verifies5/4/provider copies and controlled shutdown.
Hub controls:8single/16plans,32paired/64plans,50paired rejections,3archive
rejections; listeners unchanged. Original SQL payload hash remains unchanged.
Exact disposable `cosmic_cql_mongo_supabase_demo` database3records verified/dropped.
Owned PostgreSQL15432/REST18091/Mongo27039/browser5123 stopped; source retained.

Latest `TestResults/SupabaseCqlCurrent/mongo-federation.trx` SHA256:
`4900d605ec67fe8e21e08c6aff5e568eb4a01a470a9d030b76439478bf14a824`.
Visual proof `TestResults/SupabaseCqlCurrent/browser.jpg` is disposable.
First paired run stopped before fixture creation because effective-only UID
switching failed process ownership verification. Real/effective UID now match
Mongo data owner1000; saved-root access is used only for private Supabase tests.
Shared Mongo helper verification was preserved; no root-owned data files created.

```powershell
# Interactive, then stop using the page button.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Supabase -Federated
# Bounded native/restart/C#/HTTP verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Supabase -Federated -VerifyOnly
```

## Remaining gaps
Atomic cross-client conditional writes, general child/depth/history semantics,
avatar/detail ownership, bulk atomicity, full query parity and central registration
are not certified. Hosted row-policy/group/auth/gateway configuration needs
independent acceptance. No universal uptime or full HyperDrive certification.
