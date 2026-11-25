# NextGen Software WEB4 OASIS API MapBox OASIS Provider

WEB4 OASIS API MapBox OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

This provider allows the Our World (part of the OASIS Geolocation AR UI) to switch to the MapBox mapping plugin/asset within Unity.

## Map provider

`MapBoxOASIS` implements `IOASISMapProvider` on `NextGenSoftware.OASIS.Providers.Shared.Maps`:

- **Directions:** Mapbox Directions API v5 (`mapbox/walking`, `mapbox/driving`, `mapbox/cycling`) with GeoJSON geometry. It also implements `IOASISRoutingProvider`.
- **Place search:** Mapbox Geocoding API v6 forward search (`/search/geocode/v6/forward`), biased with `proximity`, at most 10 results.
- **Rendering:** the client draws the map (Mapbox GL JS, or the Mapbox Maps SDK for Unity or mobile) by binding an `IMapCommandSink` in `Initialize`, or by draining `DrainCommands()`.
- **Access token:** pass it to the constructor, or set `OASIS_MAPBOX_ACCESS_TOKEN` for the parameterless constructor. Failures return `false` or an `OASISResult` error with Mapbox's own message; nothing is faked or retried.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!