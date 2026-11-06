# Netlify Blobs CQL MVP

The read-only candidate uses the site's existing canonical `holon/<UUID>`
keys in one approved store. It obtains a signed URL through Netlify's
strong-consistency API, then fetches the object without forwarding the
bearer token. The credential goes only to `api.netlify.com`. The object
origin must match the reviewed HTTPS endpoint. The guarded transport
checks public DNS and disables proxying, redirects and cookies.
[Netlify Blobs consistency](https://docs.netlify.com/build/data-and-storage/netlify-blobs/#consistency) ·
[Official client request flow](https://github.com/netlify/primitives/blob/a8996a12d91cbf816ef8fb70cb589156dd57417f/packages/blobs/src/client.ts).

The profile activates against one existing public holon and checks all
selected records before applying pagination. Each signed response and
object has a byte budget, and writes, deletes and listing are disabled in
the query profile. The legacy storage constructor keeps its existing behavior.

Seven bounded-read tests and two legacy protocol tests passed without
skips at Core `9e38eba55954ad9a3e4d6b1cc85c428d76a3ff2a`.
The TRX SHA-256 is
`83e85c39485440d8de01ad2ebefc52bb64fb9b78cd1119aaf26593ae73c6f725`.
Native Netlify qualification requires an approved teaching store,
reviewed signed object origin and appropriately scoped access. The
official off-platform flow documents a personal access token, so the
available access scope needs explicit verification before hosted use.

WEB4 reads `CQL_NETLIFY_BLOBS_QUERY_SITE_ID`, `STORE`, `OBJECT_ORIGIN`,
`READER_TOKEN` and `PROBE_ID` with that prefix. The protected-settings
review binds the site, store, object origin and first public UUID. All 20
review checks passed, including invalid and substituted object origins.
All nine WEB4 admission tests passed without skips at Core
`77d943944552f188e0aaccf128ba2c8cc2f4ef9d`. The admission TRX SHA-256 is
`a776a5ded2542ba84831c454de11901f936afbcc981fb55d8381903cc616e9b6`.

The native verifier draft is
`LocalState/COSMIC/HostedNetlifyBlobsCqlQualification/verify-native.ps1`.
It checks the four existing keys and the absent key using exact Netlify
HEAD requests, confirms the tombstone, and runs CQL 3/2/0. It requires the
same ETags before and after the reads. The missing-token preflight passed.
The native probe compiled at the exact WEB4 Core pin. Live execution
requires an approved teaching store and reviewed reader credential.

Review the credential's actual project and role permissions separately.
The native verifier establishes exact reads and unchanged ETags.
[Netlify token settings](https://docs.netlify.com/manage/accounts-and-billing/user-settings/).
