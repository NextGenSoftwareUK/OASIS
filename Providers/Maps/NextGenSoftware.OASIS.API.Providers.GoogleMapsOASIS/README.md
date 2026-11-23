# GoogleMapsOASIS

Google Maps Platform map provider for the OASIS (`IOASISMapProvider`), built on `NextGenSoftware.OASIS.Providers.Shared.Maps`.

- **Directions:** Routes API `POST https://routes.googleapis.com/directions/v2:computeRoutes` (WALK, DRIVE, BICYCLE), with the encoded polyline decoded into route geometry.
- **Place search:** Places API (New) Text Search `POST https://places.googleapis.com/v1/places:searchText`, biased to a circle of up to 50 km.
- **Address geocoding:** Geocoding API `GET https://maps.googleapis.com/maps/api/geocode/json` through `GeocodeAddressAsync`.
- **Rendering:** the client draws the map (Google Maps JavaScript API or a native SDK) by binding an `IMapCommandSink` in `Initialize`, or by draining `DrainCommands()`.

Failures return `false` or an `OASISResult` error with Google's own message in `LastError`. Nothing is faked or retried.

## Usage

```csharp
var maps = new GoogleMapsOASIS(apiKey);   // key with Routes, Places (New) and Geocoding enabled
maps.Initialize(renderer);                 // or null to queue commands
maps.SetOrigin(new Geolocation(51.5074, -0.1278));
var route = await maps.CreateAndDrawRouteAsync(new[] { from, to }, RoutingType.Walking);
```

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).
