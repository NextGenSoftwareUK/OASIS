# Map search JSON/identity contract repair

Source base Development60b0ca66; no Core/ONODE.Core/STAR or DNA change. REST POSTapi/map/search previously read simple filters from the query string while the generated WEB4 map.search wrapper sends JSON. Replace the action arguments with one explicit JSON body DTO, preserving the existing manager/filter rules and GETsearch-locations.

Both gRPCSearchMapLocations and GraphQLsearchMapLocations previously constructed MapManager(Guid.Empty), so the current manager rejected their searches even after host authorization. Bind the manager to the existing validated HttpContext.Items Avatar. The existing Wizard admission/interceptors remain unchanged; no identity comes from caller input or token reparsing and no extra database lookup is added. Shared transport enum parsing rejects invalid location types instead of silently ignoring them. GraphQL reports real binding/manager errors instead of null. Protobuf fields and JSON-string result format stay unchanged.

Postman body example and generator synchronized; canonicalMap-API reference describes JSON POST filters, source-only rollout and existing remote renderer gaps. Generated WEB4 method/type/bundle source is unchanged because it already forwards this JSON body. Existing MCP exposure backlog still lists POST/api/map/search; no missing tool is claimed delivered. SDK and site guide follow-through remains part of this item before acceptance.

Planned representative native gate: one real canonical MapLocation fixture, positive/no-match filter reads through the shipping WEB4 SDK/REST, actual gRPC client/proto and GraphQL on isolated Mongo. No provider core-suite reruns, hosted/shared Atlas, CQL scope, paid operation or new owner action. Source presence/build alone is not native acceptance.
