# NextGen Software WEB4 OASIS API Ceramic OASIS Provider

WEB4 OASIS API Ceramic OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integrate into the OASIS.

Ceramic Network is a decentralised data protocol for mutable, verifiable event streams. Current Ceramic nodes
(`ceramic-one`, rust-ceramic) accept only signed events (DAG-JOSE signed by a DID, sent as CAR files) and serve aggregated
stream state; the old js-ceramic `/api/v0` HTTP API and the public Clay node are gone.

## How storage works

- `StorageService/storage-service.mjs` (Ceramic SDK 0.12.0, pinned by `package-lock.json`) owns one `did:key` and one
  Ceramic model instance document on a `ceramic-one` node, and exposes the shared repository storage-service contract on
  loopback with a bearer token: `GET /health`, `GET|PUT|DELETE /kv?key=`, `GET /keys?prefix=`.
- Each put or delete is one signed data event (a JSON patch on `{records: {...}}`) on that stream. Ceramic data is public,
  so the provider compresses and encrypts every value with its 32-byte key before it leaves the process.
- `<data dir>/state.json` holds the DID seed, the model and document stream ids and the CID of the last event written. At
  start-up the service loads the records from the node's stream state and refuses to start unless that state has reached
  the recorded CID. Keep the data directory private and backed up: it holds the signing seed.
- ceramic-one caps an event block at 256000 bytes, so records over 240000 bytes are rejected explicitly.

## Running

```bash
docker run -d -p 127.0.0.1:5101:5101 docker.io/dclimate/ceramic-one@sha256:0fcad81010643b32a62ea7e1891842cbfaa880e92b54ff56eec57a5dfd095ef4 \
  --network in-memory --bind-address 0.0.0.0:5101 --store-dir /data --p2p-key-dir /data
cd StorageService && npm ci
OASIS_CERAMIC_STORAGE_TOKEN=<32+ chars> node storage-service.mjs <data dir> 18931 http://127.0.0.1:5101
```

Then `new CeramicOASIS("http://127.0.0.1:18931", token, key32)`. The native storage conformance fixture is
`TestProjects/NextGenSoftware.OASIS.API.Providers.CeramicOASIS.IntegrationTests/start-ceramic-fixture.sh`.

Full documentation and source code: https://github.com/NextGenSoftwareUK/OASIS
