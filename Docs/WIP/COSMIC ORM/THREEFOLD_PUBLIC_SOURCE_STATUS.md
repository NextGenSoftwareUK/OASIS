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

## Original native qualification passed - 2 November 2026

4offlinecontrols/PowerShellsyntax pass. Original.NETHttpClient(defaultheaders)6GETs allHTTP200: bounded3nodediscovery, exact3pointreads, exact2subset. StableNodeID/FarmID/GridVersion/country/city projections matchdiscovery/subset. NodeIDs 1,2,3. No IP/contact/resource-pricepayload projected, no compute/storagecontract/writes/install/account. Operator mainnetproxy provenance only; independentchainstate proof remainsfalse, no uptime/health/reliability claim.

Originalintent/discoveredkeys/report/compressed6rawresponses+SHAmanifest retained ThreeFoldPublicQualificationCurrent; raw TestResults/ThreeFoldPublicSourceCurrent. Acceptedqualification frozen/no replay. NativeCQL/ORM/manager/CLI/HTTP/Mongo/hosted remain unverified; no providercount increase. Next separate read-only GridProxy node metadata source, configured UInt32nodeIDeq/IN using boundednativepointcomposition, preciseidentity/complete-selection/error/cancellation/read-onlytests.

response-1.json:4769bytes SHA256 `5f5fae1ff5e7f6d5cf61abda290152e6aa99ed3457700f8c476d16a911b57361`.

response-2.json:2343bytes SHA256 `20d36868bdecdf7a660d5ff6a269ff678fa4eab11780cd053cfb3398f76b410c`.

response-3.json:1231bytes SHA256 `1405466a9b972e966ec6b72e15fc55cef34824975bceaf5f3b61904728415573`.

response-4.json:1232bytes SHA256 `1383da01164b64965420ae659e9f6da4defe6ba6827d96842bbbdeae463df4a3`.

response-5.json:2343bytes SHA256 `20d36868bdecdf7a660d5ff6a269ff678fa4eab11780cd053cfb3398f76b410c`.

response-6.json:1231bytes SHA256 `1405466a9b972e966ec6b72e15fc55cef34824975bceaf5f3b61904728415573`.
