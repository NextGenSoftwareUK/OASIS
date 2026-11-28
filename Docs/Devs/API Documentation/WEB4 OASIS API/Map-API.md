# Map and place APIs

This reference follows the shipping MapController, MapRoutesController and PlacesController. The API exposes recorded locations, routes and visits for OASIS apps. Our World is one client. Source presence does not certify every renderer/provider or hosted configuration.

## Authentication and results

Use the WEB4 base URL and an authenticated avatar bearer. Read `OASISResult.isError`, `message` and `result`; an HTTP200 alone does not acknowledge a successful operation. Actor ownership comes from the authenticated session. Source-app labels do not grant access, establish app identity or confer route rewards.

## Recorded routes

`POST /api/map/routes` saves an immutable route header and separate geometry; `GET /api/map/routes` lists the current avatar's route summaries with `offset`/`limit` (1–100). `GET /api/map/routes/{id}` opens geometry only when requested and returns the authority/revisions needed for updates. A public route is readable by other authenticated avatars; private routes remain owner-only.

Creation takes the existing holon write intent (`operationId`, `expectedRevisions`, provider options) and `route`: `id`, `name`, `sourceApp`, `sourceRouteId`, `isPublic`, `segments`. Each segment contains at least two points with WGS84 `latitude`/`longitude` and optional UTC `utc`. Limits are100,000points,50,000segments,8MB serialized geometry and16MB request. Supply create-only null revisions for the route ID and deterministic geometry ID. The geometry ID is the .NET GUID formed from the first16SHA-256bytes of UTF-8 `oasis:map-route-geometry:{lowercase-route-guid}`. Do not use plain hexadecimal digest ordering as a GUID conversion.

`PUT /api/map/routes/{id}/access` conditionally changes `name` and/or `isPublic` using the exact returned revisions for both records. It does not replace geometry. Use one stable idempotent operation identity per intended write. GPX provenance is reported metadata; importing a file does not prove its origin, ownership or walked distance.

## Real-world place visits

Rollout boundary (28 November2026): optional sourceApp preservation is implemented on work/gpt-oportal-map-integration, but its new cross-app native visit acceptance is blocked by public-catalogue connectivity and it has not been promoted. Treat it as a source contract, not a deployed guarantee. Current accepted legacy requests retain Our World attribution.

`GET /api/places/nearby` takes `category`, `latitude` and `longitude`. Categories are All, Park, Forest, Beach, Woodland, Farm, Mountain, River, Lake, Stream, Yoga, Meditation, Mindfulness, Ecstatic Dance, Vegan Food and Health Shops. The configured catalogue resolves real place identity, name, category, coordinates and boundaries. Configure its real data source; public Overpass is not an unlimited production backend.

`POST /api/places/observe` takes `id` (stable observation GUID), `placeId` from the catalogue, finite coordinates, fresh UTC `observedUtc` (within two minutes), and optional `sourceApp`. The source label is1–256characters without surrounding whitespace or control characters. Existing requests without the field retain Our World attribution. New clients should send their own explicit label; Unity sends Our World. The label is caller-reported and is not an authenticated application identifier.

The server resolves the place and uses its arrival/departure boundaries:50metres or nearer enters,100metres or farther leaves, and the gap does not change presence. The durable ledger commits presence, visit and karma together. The first visit to a physical place earns100and a return earns50; changing app labels does not reset first-visit eligibility or create another active visit. Reusing an observation ID for another place or app label is refused. Departures and already-active observations may return no visit record with an explanatory message.

`GET /api/places/presence` returns active visits. `GET /api/places/visits?offset=0&limit=50` returns `items`, `total` and optional `nextOffset`; absent/null continuation ends the page. Visit records include `id`, `placeId`, `name`, `category`, coordinates, `visitedUtc`, `karma`, `firstVisit` and `oApp`. The current avatar sees their visits across apps. Avatar karma audit/transaction descriptions retain the reported app label.

The WEB4 JavaScript client already forwards the observation object as JSON:

```javascript
const observed = await client.places.observe({
  id: crypto.randomUUID(),
  placeId: place.id,
  latitude: actualLatitude,
  longitude: actualLongitude,
  observedUtc: new Date().toISOString(),
  sourceApp: appName
});
if (observed.isError) throw new Error(observed.message);
```

Choose `place` from the real catalogue and use the client's actual coordinates. A submitted GPS observation is not device-attestation evidence.

## OPORTAL and Our World

OPORTAL uses MapLibre for the web map and lazy route geometry. Real hotspot/linked-quest creation, visibility filtering and popup layer status/close passed native46142470160; that does not accept a WebGL canvas, GeoNFT mint, Unity build or hosted deployment. The proposed displayed-map control repair (portal branch work/gpt-oportal-map-controls) still requires its actual-renderer native gate. Its Map popup reads the same durable places ledger that Our World writes, including karma and app labels. Its geo layers read owned WEB4 GeoNFTs and WEB5 nearby visible hotspots plus linked quests/objectives; those domain records remain authoritative, rather than being duplicated into another map ledger. The layer UI shows only loaded records, not an exhaustive worldwide catalogue. Our World uses its Unity/GOMap display with its own edge/runtime integration; a web-map render is not proof of a Unity build/device pass.

## Stored map locations

`GET /api/map/nearby` requires latitude/longitude and accepts `radiusKm` (default10). `GET /api/map/search-locations` accepts query, optional type and proximity filters; `POST /api/map/search` binds a JSON body with optional `query`, `type`, `latitude`, `longitude` and `radiusKm`, matching the generated SDK. SDK REST, actual gRPC SDK/source proto and GraphQL searches passed native46151924276; gRPC/GraphQL retain their existing Wizard admission and use the validated request avatar. MCP exposure and hosted/current merged-graph rollout remain separate. `POST /api/map/visit/{locationId}` records a stored-map-location visit with an optional JSON string purpose. `GET /api/map/visit-history` pages that location history using limit/offset, and `GET /api/map/stats` returns that avatar's stored-map statistics. This location-history contract and the real-world place/karma ledger are distinct features.

## Exact REST route catalogue

The legacy renderer/control operations below require a configured compatible map provider/runtime. Provider/runtime binding and complex-body request acceptance on these legacy control actions remain open; route presence is not a successful renderer operation. Their historical spellings, including `Pam` and `Beween`, are the actual routes; do not silently substitute invented paths. There are no `/api/map/{mapId}/performance` or `/api/map/{mapId}/health` controller routes in this catalogue.

| Method | Path |
|---|---|
| POST | `/api/map/CreateAndDrawRouteOnMapBetweenHolons/{holonDNA}` |
| POST | `/api/map/CreateAndDrawRouteOnMapBeweenPoints/{points}` |
| POST | `/api/map/Draw2DSpriteOnHUD/{sprite}/{x}/{y}` |
| POST | `/api/map/Draw2DSpriteOnMap/{sprite}/{x}/{y}` |
| POST | `/api/map/Draw3DObjectOnMap/{obj}/{x}/{y}` |
| POST | `/api/map/HighlightBuildingOnMap/{building}` |
| POST | `/api/map/PamMapDown/{value}` |
| POST | `/api/map/PamMapLeft/{value}` |
| POST | `/api/map/PamMapRight/{value}` |
| POST | `/api/map/PamMapUp/{value}` |
| POST | `/api/map/SelectBuildingOnMap/{building}` |
| POST | `/api/map/SelectHolonOnMap/{holon}` |
| POST | `/api/map/SelectQuestOnMap/{quest}` |
| POST | `/api/map/ZoomMapIn/{value}` |
| POST | `/api/map/ZoomMapOut/{value}` |
| POST | `/api/map/ZoomToHolonOnMap/{holon}` |
| POST | `/api/map/ZoomToQuestOnMap/{quest}` |
| GET | `/api/map/nearby` |
| POST | `/api/map/search` |
| GET | `/api/map/search-locations` |
| GET | `/api/map/stats` |
| GET | `/api/map/visit-history` |
| POST | `/api/map/visit/{locationId}` |
| POST | `/api/map/routes` |
| GET | `/api/map/routes` |
| GET | `/api/map/routes/{id}` |
| PUT | `/api/map/routes/{id}/access` |
| GET | `/api/places/nearby` |
| POST | `/api/places/observe` |
| GET | `/api/places/presence` |
| GET | `/api/places/visits` |

## References

[Full generated WEB4 Postman collection](WEB4-Full-API.postman_collection.json) · [Overpass resource guidance](https://dev.overpass-api.de/overpass-doc/en/preface/commons.html). gRPC/GraphQL/MCP coverage of existing operations remains tracked in `Scripts/api_surface_backlog.json`; this optional JSON field adds no new route or tool and does not claim unimplemented surface parity.
