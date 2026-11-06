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
