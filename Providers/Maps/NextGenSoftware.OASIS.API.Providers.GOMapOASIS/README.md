# GOMap OASIS Provider

Places OASIS avatars, holons, quests, OAPPs, GeoNFTs and geo hot spots on real-world maps for Our World and One World, using the GO Map Unity package as the renderer.

## How it works

GO Map is a Unity Asset Store package, so its types only exist inside a Unity player. `GOMapOASIS` derives from the shared `MapProviderBase` (`Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.Maps`), which owns the map session: camera, pins, placements, routes and world-space projection.

- **Headless** (`Initialize(null)`): every change is queued as a `MapCommand`; a Unity client calls `DrainCommands()` each frame and applies them.
- **Bound to GO Map** (`new GOMapOASIS(goMap)` or `BindGOMap(goMap)`): every change is applied to the live component through `GOMapCommandSink` and `GOMapUnityBridge` (reflection, so no UnityEngine reference). If the component lacks the method or the call throws, the operation returns false, `LastError` is set and nothing is recorded. The bridge's own `LastError` names the missing method. On bind, the component's `locationManager.currentLocation` becomes the world origin.

Directions come from OSRM and place search from Nominatim, both keyless and configurable to self-hosted instances:

```csharp
var provider = new GOMapOASIS("https://osrm.example.org", "https://nominatim.example.org");
provider.Initialize(null);
provider.SetOrigin(new Geolocation(51.5138, -0.0984));
provider.PlaceQuestOnMap(quest, 51.5081, -0.0759);
var route = await provider.CreateAndDrawRouteAsync(new[] { from, to }, RoutingType.Driving);
```

The public OSRM demo only serves driving routes.

## GO Map component contract

When bound, commands call these methods on the component:

| Command | GO Map method |
|---|---|
| Camera (pan, zoom, centre) | `centerMap(GOCoordinate)`, `setZoom(double)`; `UpdateValue(double)` when the bearing changes |
| Drop / remove pin | `dropPin(GOCoordinate, object)`, `removePin(object)` |
| Place entity | `placeHolon`, `placeBuilding`, `placeQuest`, `placeGeoNFT`, `placeGeoHotSpot`, `placeOAPP` `(object, GOCoordinate)` |
| Remove placement | `removePlacement(object)` |
| Select / highlight | `selectObject(object)`, `highlightBuilding(object)` |
| Draw | `placeObject`, `placeSprite`, `drawOnHUD` `(object, float, float)`; `drawRoute(GOCoordinate[])` |

Coordinates are `GoShared.GOCoordinate` instances, located in the loaded assemblies by name.

## CQL route source

`GOMapRouteQuerySource` is a read-only CQL source of three fixed public Berlin driving routes, served through `Directions/OSRMDirectionsProvider`. That client keeps the raw OSRM JSON and its SHA-256 for evidence, enforces a 256 KiB response bound and spaces requests one second apart process-wide.

## Tests

- `TestProjects/...GOMapOASIS.UnitTests`: offline (canned OSRM responses and a fake GO Map component). Runs in the `map-platforms-unit-tests` workflow. The two native route tests are inconclusive unless their cloud-qualification variables are set.
- `TestProjects/...GOMapOASIS.IntegrationTests`: live OSRM and Nominatim, opt-in with `GOMAP_RUN_INTEGRATION_TESTS=1` (override `GOMAP_OSRM_URL` / `GOMAP_NOMINATIM_URL`).
- `TestProjects/...GOMapOASIS.TestHarness`: console walkthrough; `--live` adds routing and search.

## License

MIT
