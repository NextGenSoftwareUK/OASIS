# NextGenSoftware.OASIS.Providers.Shared.Maps

Shared base for the OASIS map providers (Google Maps, HERE Maps, MapLibre).

- `MapProviderBase` implements `IOASISMapProvider` (the Contracts interface `MapManager` uses) over a real routing service and a real place-search service. It owns the map session (camera, pins, placements, routes) and sends every visual change as a `MapCommand` to a live `IMapCommandSink` renderer, or queues it for `DrainCommands` when headless.
- Failed operations return `false` (or an `OASISResult` error) with the cause in `LastError`. Routes are never replaced with straight lines.
- `OsrmRoutingService` and `NominatimPlaceSearchService` are keyless OpenStreetMap services. Their public instances are limited to one request per second; use self-hosted instances for production volume.
- `GooglePolylineCodec` and `FlexiblePolylineCodec` decode Google and HERE route geometry.
- `MapHttpClient` sends each vendor request once, with a deadline, no redirects and a response size bound, and returns the vendor's own error text on failure.
