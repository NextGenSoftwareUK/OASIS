# Our World routing audit

Reviewed the local Our World Unity checkout at commit `ed82cd0a3504304dc725dc03292360b123806617` and the enabled world scene. This is a source and scene configuration audit. A running Unity session, a packaged build, and authenticated routing requests were outside this audit. The Our World checkout already had unrelated working changes and was read only.

## Current routing and rendering

| Path | What the source and scene show |
| --- | --- |
| World scene and base map | Unity Build Settings enable `Assets/_Game/Scenes/UnityWorldSpace.unity`. `GameController` defaults to preference 2 and enables GO Map as the maintained renderer. It keeps the Mapbox root active for shared camera and location infrastructure and supplies `MapboxAPIProvider` in the default, Mapbox and WRLD3D preference branches. |
| Tracked quest to a GeoNFT or hotspot | `TrackedQuestNavigation` resolves the selected objective's portal and calls `NFTFetchController.BeginTrackedQuestNavigation`. The fetch controller reads the player's GO Map position, tracks direct proximity separately and asks `GODirections.RequestDirectionsWithCoordinates` for a walking route. It reevaluates at five-second intervals and requests another route after at least 15 metres of movement. |
| Provider selected by the enabled scene | The scene's `GOToAddressDemo` references `GODirections` instance `2038093846`. That instance overrides `directionsApi` to `1`, which maps to Mapbox. The plugin calls the Mapbox Directions v5 endpoint and draws the returned route through GO Map. The plugin supports Google's legacy Directions endpoint as an alternative. The enabled scene's override selects Mapbox even though the component's default is Google. |
| Unity directions controller | `PlaceDisplayElement` initializes `DirectionsController` with `MapboxDirectionsAPIProvider`. Its two direct route-start lines are commented out. The editor action explicitly starts a route. When invoked, this path goes through `MapboxDirectionsAdapter` and the Mapbox Unity SDK, then passes the waypoints to `DefaultNavigationBehaviour` for display. This is a separate Mapbox client path from the GO Map plugin. |
| Places and Edge | `NearbyPlacesController` uses the map provider for nearby-place lookup. `OurWorldEdgeRuntime` composes the Edge Unity host and delegates gameplay sync, including quest progress and active quest selection, to its client. The route call chain above does not go through that host. |

The enabled scene therefore combines GO Map rendering with Mapbox route computation. Both inspected route clients use Mapbox under that scene's configuration. The repository also contains a selectable legacy Google backend, and OASIS's separate `GOMapOASIS` provider uses OSRM. Neither establishes a Google or OSRM route call from the enabled Our World scene.

## Evidence in the Our World checkout

- [Enabled Unity scene list](<C:/Source/Our World/ProjectSettings/EditorBuildSettings.asset:18>) and [scene routing provider override](<C:/Source/Our World/Assets/_Game/Scenes/UnityWorldSpace.unity:67608>).
- [Map preference and data provider wiring](<C:/Source/Our World/Assets/_Game/Scripts/Controllers/GameController.cs:39>).
- [Tracked quest route request](<C:/Source/Our World/Assets/NFTFetchController.cs:181>) and [GODirections backend dispatch](<C:/Source/Our World/Assets/GODirections - Directions plugin for GoMap/Core/GODirections.cs:109>).
- [Park view wiring](<C:/Source/Our World/Assets/_Game/Scripts/Views/ParksList/PlaceDisplayElement.cs:33>), [Unity directions controller](<C:/Source/Our World/Assets/_Game/Scripts/Controllers/DirectionsController.cs:67>) and [Mapbox Unity SDK adapter](<C:/Source/Our World/Assets/_Game/Scripts/Adapters/MapboxDirectionsAdapter.cs:28>).
- [Edge composition](<C:/Source/Our World/Assets/_Game/Scripts/OASISIntegration/OurWorldEdgeRuntime.cs:14>).

## Shared OASIS routing

`IOASISRoutingProvider` is a general OASIS API contract. It accepts WGS84 origin and destination, the existing travel mode enum and cancellation. It returns distance in metres and ordered WGS84 geometry. Google Maps, GOMap, Mapbox and HERE implement the contract through their respective route backends. Applications can use the result with their own map renderer. Our World's current Unity paths were not migrated as part of this audit.

Google Maps reads the restricted route credential from OASIS_GOOGLE_ROUTES_API_KEY. Mapbox reads OASIS_MAPBOX_ACCESS_TOKEN, including for callers of its existing Directions service. HERE reads OASIS_HERE_ROUTING_API_KEY. GOMap's default public OSRM demo is a driving endpoint. Configure an OSRM deployment built for the requested travel profile for walking and cycling. MapLibre is a renderer and needs a separately configured routing backend.
