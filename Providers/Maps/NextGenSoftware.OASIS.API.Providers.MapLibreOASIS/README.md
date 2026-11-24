# MapLibreOASIS

MapLibre map provider for the OASIS (`IOASISMapProvider`), built on `NextGenSoftware.OASIS.Providers.Shared.Maps`.

MapLibre is an open-source renderer with no routing or search service of its own, so the provider pairs it with OpenStreetMap services:

- **Directions:** OSRM (`/route/v1/{foot|driving|cycling}`), with GeoJSON geometry.
- **Place search:** Nominatim (`/search?format=jsonv2`), biased to a box around the search point.
- **Rendering:** `Scene` (`MapLibreSceneBuilder`) produces style-spec v8 GeoJSON sources (routes, pins, placements), matching `line` and `circle` layers, and `jumpTo` camera options. A `maplibre-gl` client passes them to `map.addSource`, `map.addLayer` and `map.jumpTo`. Live renderers can also bind an `IMapCommandSink` in `Initialize`.

The keyless public OSRM demo and Nominatim instances are rate limited to one request per second and are not for production volume. Pass self-hosted base URLs to `new MapLibreOASIS(osrmBaseUrl, nominatimBaseUrl)` for production.

This project also contains `MapLibreStyleQuerySource`, the read-only COSMIC CQL source over MapLibre's public demo style documents.

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).
