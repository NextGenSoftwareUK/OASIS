# API surface sync status (2026-11-27)

Check: `Scripts/check_api_surface_sync.py` (workflow `.github/workflows/api-surface-sync.yml`). Cells are covered/total REST routes.

## Before (Development, before this work)

| API | REST routes | SDK | MCP | Postman | gRPC rpcs | GraphQL methods |
| --- | --- | --- | --- | --- | --- | --- |
| WEB4 | 852 | 851/852 | 95/852 | 852/852 | 230 | 196 |
| WEB5 | 639 | 639/639 | 0/639 | 639/639 | 168 | 160 |
| WEB6 | 121 | 121/121 | 0/121 | 0/121 | 51 | 51 |
| WEB7 | 7 | 7/7 | 0/7 | 0/7 | 7 | 8 |
| WEB8 | 8 | 8/8 | 0/8 | 0/8 | 8 | 9 |
| WEB9 | 1 | 1/1 | 0/1 | 0/1 | 1 | 3 |
| WEB10 | 1 | 1/1 | 0/1 | 0/1 | 1 | 4 |

## After

| API | REST routes | SDK | MCP | Postman | gRPC rpcs | GraphQL methods |
| --- | --- | --- | --- | --- | --- | --- |
| WEB4 | 852 | 852/852 | 98/851 | 852/852 | 230 | 196 |
| WEB5 | 639 | 639/639 | 0/639 | 639/639 | 168 | 160 |
| WEB6 | 121 | 121/121 | 0/119 | 121/121 | 51 | 51 |
| WEB7 | 7 | 7/7 | 0/7 | 7/7 | 7 | 8 |
| WEB8 | 8 | 8/8 | 0/8 | 8/8 | 8 | 9 |
| WEB9 | 1 | 1/1 | 0/1 | 1/1 | 1 | 3 |
| WEB10 | 1 | 1/1 | 0/1 | 1/1 | 1 | 4 |

## Done
- MCP: `web4_cql_sql`, `web4_cql_ask`, `web4_admin_cql_sidecars` (OASIS-WEB6 Development `06649bf`; catalogue now 511 tools).
- SDK: WEB4 `recoverConditionalOperation` called `/api/data/~`; fixed (SDK main `4e49abe`). All other SDK routes were already covered.
- Postman: WEB4 regenerated (same route bug fixed in the generator); new WEB6-WEB10 collections (`Scripts/generate_web6_10_postman.py`).
- Docs site: the CQL routes were already in `reference/cql.html`; no change needed.

## Left (known gaps, listed route by route in `Scripts/api_surface_backlog.json`)
- MCP: about 1,525 routes have no *provably* matching tool. Many have an equivalent in-process tool with a different
  name; map those in `api_surface_allowlist.json` (`mcpToolMap`), then add the missing tools. The backlog may only shrink.
- gRPC and GraphQL are counted, not mapped route by route. WEB4 exposes no CQL over either, so the new CQL routes need none.

## Decisions
- Known gaps are baselined so the check fails only on new drift; they are not marked intentional.
- CI uses no secrets, so it cannot read the private WEB6 and STAR ODK submodules: WEB5/WEB6 routes are checked only when
  the script runs locally with them checked out. MCP is checked in CI from the committed `api_surface_mcp_index.json`.
- Running the same check inside the STAR-ODK and OASIS-WEB6 repositories is the follow-up that closes that hole.
