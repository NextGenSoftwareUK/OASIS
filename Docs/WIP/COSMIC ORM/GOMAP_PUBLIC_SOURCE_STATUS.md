# GOMap public query source review — 8 November 2026

Status: existing real HTTP routing client reviewed; no CQL source admitted, no native route request made, no coverage increment.

The current GOMap project targets net10.0. Its existing OSRMDirectionsProvider uses an injected HttpClient or one shared client, identifies itself with an OASIS user agent, and calls the route HTTP API with full GeoJSON geometry. This is a genuine integration. The README netstandard2.1 description does not describe the current project target. Unity rendering, placement, routing and persistence require separate evidence.

Primary policy reviewed directly on 8 November:

- [OSRM official demo-server policy](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server): reasonable non-commercial use only, maximum one request per second, no availability or freshness guarantee.
- [FOSSGIS operator policy summary](https://routing.openstreetmap.de/about.html): required attribution and fix-the-map link, valid user agent/referrer, maximum one request per second, no scraping/heavy usage; requests are logged. Profile configuration can differ from stock OSRM.

The current client does not serialize requests or enforce a shared rate limit, reads the entire response without a source byte limit, has no cancellation parameter, accepts malformed coordinate entries by skipping them, and returns an empty list on failures with LastError as side-channel evidence. Do not expose this unchanged as a bounded CQL catalogue or infer that an empty list is a successful native read. Existing routing behavior has not been edited by this review.

Next engineering gates: choose a supported hosting/use contract for the public sandbox; preserve the real routing API while defining strict bounded response/schema/error/cancellation behavior at the responsible layer; establish fixed public teaching coordinates and stable identity without exposing user locations; retain full route data and attribution; qualify controlled and native paths in the reusable cloud environment, then WEB4 source registration, owner scoping, presets, federation, privacy and HyperDrive replicas. No unreviewed alternate endpoint, local OSRM dataset/container/install, account request or live enrollment was introduced.
