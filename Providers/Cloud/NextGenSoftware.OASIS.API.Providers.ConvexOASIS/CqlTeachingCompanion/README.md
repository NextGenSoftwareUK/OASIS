# Convex CQL teaching companion

This standalone Convex project serves an indexed, read-only HTTP action for
synthetic teaching holons. Keep its deployment and data separate from
application data. It includes an internal seeding mutation for the local
qualification runner.

`GET /oasis/cql/holon?id=<canonical-lowercase-guid>` requires a bearer token
from the deployment's protected `OASIS_CQL_READER_TOKEN` setting. The action
returns a matching explicitly public holon as JSON. It returns HTTP 403 for
private holons, HTTP 200 with JSON `null` for missing or deleted holons,
HTTP 401 for invalid or missing credentials, and HTTP 400 for invalid keys or
additional parameters.

To reproduce the Windows probe, copy the two npm manifests to a disposable
workspace and install the pinned dependencies there using Node 22 or later.
Run `python -B verify-local.py --workspace <workspace> --backend <verified-exe>
--node <node-exe>` with Python 3.11 or later. The runner checks the backend
binary digest, copies the companion sources, starts a loopback instance,
installs the companion, seeds three records, and checks the HTTP action.
The backend and CLI output and the local fixture database stay under
`<workspace>/native-fixture`. The fixture database holds a disposable reader
token. Remove it after the process exits.

This proof covers the teaching read action. The WEB4 CQL adapter and the
provider's general holon and avatar companion functions are separate work.
