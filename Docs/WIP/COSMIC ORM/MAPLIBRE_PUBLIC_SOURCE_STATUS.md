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

## Bounded read implementation prepared

MapLibreStyleQuerySource uses the existing bounded ID compiler and public
source mapper, with explicit world/bright/terrain keys. It issues one GET for
each selected reviewed URL, rejects redirects/partial responses/non-JSON,
bounds decoded bodies to262144bytes and JSON depth48, and rejects duplicate
properties recursively. Version8, bounded nonempty names/sources/layers,
supported source/layer kinds, distinct layer IDs and local source references
are required. It preserves the entire original UTF-8 style JSON, SHA256,
native URL/key/version and counts as portable scalar metadata. Referenced
resources remain opaque data and are never requested or executed.

Unknown/mixed ID selections preflight before transport; source writes and
scans return explicit unsupported errors. Creator/public/active/childless
projection and owner-dependent logical identity use existing shared contracts.
The original map facade is unchanged and its unimplemented rendering remains
open. No shared helper, Core enum or admission tariff changed for this source.

New provider test project contains27controlled cases and1opt-in real native
CQL3/2case. These cover HTTP/schema/JSON/UTF-8/payload boundaries, no child
resource requests, no partial rows, inactive/cancelled reads and catalogue
validation. New cloud workflow enforces exact counters/no skips, one build
lease and bounded .NET workers; no local compilation/runtime/environment.
Project XML and expected case counts checked; cloud qualification pending.
Coverage counts unchanged; no WEB4 registration or hosted source prepared.

Tasksourcea1ebbf5f75a10ce426103d7bce77fa9dc06b9641 pushed on original task branch; cloud42454653284 observedin_progress. One automatic run, no manual dispatch or local compiler.

Cloud42454653284/job128650985638 succeeds:27controlled+1actual native CQL3/2,
zero skips. Artifact13607083066/5626bytes SHA256
bff12b69e30c9710e237577a52ea5d1813cc5cbb7cd8084da3824cbfab25ec8a and both
TRX hashes/counters verified wholly in memory. Real complete style documents
round-trip with their SHA256 and source/layer counts; no child requests.
WEB4 explicit owner registration/read compiler/project binding,8boundary
cases and real shared HTTP/Mongo fixture prepared. Core260d7c7dcc5512cccceb049480bd323d274b7be7
adds only the read-tariff entry on the original Core task branch; parent
manifest/gitlink advance together. No hosted source/admission/count change.
