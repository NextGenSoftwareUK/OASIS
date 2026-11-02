# ThreeFold Grid Proxy public node qualification

Updated2November2026. Red: no native/CQL/hosted/browser acceptance yet.

[Official proxy guide](https://manual.grid.tf/labs/documentation/developers/proxy_readme/proxy/)
publishes gridproxy.grid.tf for mainnet, separate from dev/QA/test networks.
It exposes public indexed TFChain node information, including filtered/paged
collections and /nodes/{node_id}. This is distinct from creating contracts,
renting compute, signing transactions or storing COSMIC holons on the grid.

## Pinned contract

threefoldtech/zos_sdk_go commitf8d1d856db5b13154d7262daa548aa81b38bbfe8:
Swagger JSON and two Go data schemas retained with SHAmanifest under
LocalState/COSMIC/ThreeFoldPublicQualificationCurrent/sdk.

The /nodes collection has page/size/sort_by/sort_order/randomize and geographic,
resource and farm filters. It does not advertise arbitrary node-ID membership.
The point route accepts node_id. NodeDTO uses nodeId/farmId/country/city and
nested capacity values; whitelist projection fields, preserve integer units,
exclude IPs/twin contacts/arbitrary metadata. Do not invent an IN route.

## Exact next gate

Use the same .NET HTTP transport as the intended adapter, with default headers,
no authorization, redirects, retries or alternative hosts. First bounded paged
GET /nodes?page=1&size=3&sort_by=node_id&sort_order=asc&randomize=false; freeze its
three existing distinct IDs under an original intent. Read exactly those three
point records, then a two-ID subset through bounded native point composition.
Require exact selected identities and same stable public projection. No source
scans during later CQL execution. Pin operator-reported mainnet endpoint, without
claiming independent TFChain state proof or node uptime/availability guarantees.

No native endpoint request, environment, node, account, contract, deployment,
funding or installation has been created. Controlled tests precede the native
capture. ClickHouse's existing code deployment remains the immediate priority.
