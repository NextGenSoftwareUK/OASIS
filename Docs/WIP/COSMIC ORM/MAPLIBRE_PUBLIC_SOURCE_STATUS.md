# MapLibre public style source qualification

8 November 2026. Source readiness only; no CQL acceptance or live admission.

The existing MapLibreOASIS map facade still contains literal success returns
without a rendering client. Those methods are not qualified by this review.
Full rendering and map-provider implementation remain later roadmap work.

For the breadth-first CQL demo, MapLibre publishes real style documents with
no account requirement in its [official demo repository](https://github.com/maplibre/demotiles).
The README at commit
[601ae60796ceceda2cbd2ed3d2ea92d17a84be4b](https://github.com/maplibre/demotiles/tree/601ae60796ceceda2cbd2ed3d2ea92d17a84be4b)
explicitly lists all three candidate URLs:

| Native style key | Official document | Readiness |
|---|---|---|
| world | https://demotiles.maplibre.org/style.json | HTTP200; 77224 bytes; name MapLibre; 2 sources, 8 layers |
| bright | https://demotiles.maplibre.org/styles/osm-bright-gl-style/style.json | HTTP200; 119336 bytes; name OSM Bright; 1 source, 117 layers |
| terrain | https://demotiles.maplibre.org/styles/osm-bright-gl-terrain/style.json | HTTP200; 75856 bytes; name OSM Bright Terrain; 3 sources, 118 layers |

Each response is application/json and style version8, matching the
[MapLibre style specification](https://maplibre.org/maplibre-style-spec/root/).
Recursive duplicate JSON properties are rejected; each source collection is
nonempty and layer IDs are unique. This is not complete renderer or style
specification validation. Source URLs, glyphs, tiles and other referenced
resources were not followed or downloaded. No browser or GPU was started.

SHA256, in table order:
dd86fd257f9895da23b7da5dac62829a0b25ab458f71b06a3e06857b076caf06;
4733a24f6ae359b7a104cf1a5cde845eb5f16dd590c65059d8de89a40242cb36;
277139cde0c525ddaa274fe3751bb46eafedb2986d544fa7116ff7aa3ac81742.

All raw bodies/status/hashes were retained before parsing, with one bounded
GET per document, TLS validation, no redirects/proxies/cookies/retries/writes.
The three JSON bodies total272416 bytes. Primary README provenance and
compact validation receipts are in the existing recovery state under
maplibre-primary-catalogue-reviewed.json and maplibre-*-style-readiness.*.
No package, runtime, source checkout, image or database was installed.

Next: implement an explicit three-key read-only style-document CQL source,
preserving native document identity and provenance without requesting child
resources. Qualify malformed JSON/schema, unknown/mixed IDs, payload bounds,
privacy, native3/2, manager/HTTP/Mongo and later hosted reuse/replicas/browser.
No map renderer, tile-serving performance, style editing or full SDK parity
claim follows from a public document query. Coverage counts are unchanged.
