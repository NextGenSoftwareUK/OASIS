# HEREMapsOASIS

HERE Location Services map provider for the OASIS (`IOASISMapProvider`), built on `NextGenSoftware.OASIS.Providers.Shared.Maps`.

- **Directions:** HERE Routing API v8 `GET https://router.hereapi.com/v8/routes` (pedestrian, car, bicycle) with `return=polyline,summary`. Each section's flexible polyline is decoded and the sections are joined.
- **Place search:** HERE Geocoding & Search v1. `/discover` is used when the search has a bias point and `/geocode` otherwise.
- **Rendering:** the client draws the map (HERE Maps API for JavaScript or the HERE SDK) by binding an `IMapCommandSink` in `Initialize`, or by draining `DrainCommands()`.

Failures return `false` or an `OASISResult` error with HERE's own title and cause in `LastError`. Nothing is faked or retried.

## Usage

```csharp
var maps = new HEREMapsOASIS(apiKey);
maps.Initialize(null);
var places = await maps.PlaceSearch.SearchAsync(new MapPlaceSearchRequest("coffee", near: here));
```

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).
