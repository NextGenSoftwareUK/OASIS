# Cloudinary public image CQL

The source reads exact JPEG metadata from Cloudinary's public demo cloud.
Each reviewed asset maps to one stable logical Holon ID through the native
record mapper. CQL uses the public catalogue to resolve the selected IDs
and makes a bounded GET for each selected asset. The request uses
`fl_getinfo`; the response supplies dimensions and byte counts as JSON.
The scoped source leaves the owned Cloudinary storage path independent.
[Cloudinary metadata API](https://cloudinary.com/documentation/transformation_reference#fl_getinfo) ·
[Published demo assets](https://cloudinary.com/blog/beyond_drupal_media_make_images_and_video_fly_with_cloudinary)

An anonymous read on 6 November returned HTTP 200 and JSON for
`sample.jpg`, `lady.jpg` and `woman.jpg`. The payload sizes were 116,
119 and 119 bytes. The direct probe made no account, image body or native
asset changes. The leased suite passed seven controlled checks and one
real Cloudinary CQL test. The latter verified native 3/2 selections at
Core `67c54b4199187a44c369002b13b188808525cf98`. The TRX SHA-256 is
`236d1298a6170e65658141d387963db643dc9bc928bd355728ac3f9a1b5fbaff`.
The final provider source repeated all eight checks successfully at the
WEB4 candidate Core `e400afd53cef4997e8bfcabc4b34b3f84a854626`, with TRX
SHA-256 `e5ae49306ae76042dd0b2a5e61256bc61ba5a01430ac833d8a179dd0d3e9f5b3`.

The combined Cloudinary and pgvector WEB4 admission passed 12 checks and
ten Milvus regressions at Core `e400afd53cef4997e8bfcabc4b34b3f84a854626`.
The admission TRX SHA-256 is
`a4f42b2da21a43a0a310e922b2e98c15f6bbe11ed4cc64b0597f7aa1fcfc94ca`.
The reviewed `CQL_CLOUDINARY_QUERY_SOURCE_JSON` binds the owner, exact
assets and existing public probe. Live federation and browser acceptance
remain open. LocalFile remains first for the live rollout.

The reviewed local WEB4 HTTP batch verified native 3/2, paired MongoDB
5/4, and visitor isolation. The owned two-record Mongo fixture was
removed after checking the exact rows. The test used controlled local
identity and metering with real Cloudinary reads and MongoDB writes.
[Local acceptance scope](PROVIDER_CQL_AND_SANDBOX_STATUS.md#reviewed-public-source-http-on-6-november-2026)

## Joint hosted candidate

The scoped candidate dcae58d976d2af04800d1670fc1e4fe3979b5608 and Core
f695b1cd8880b3009b7b14190ab41867fb38ee65 are published on
work/cosmic-registry-object-release-candidate. This batch adds the public
Oracle object HEAD profile, Cloudinary image metadata and Proof of
Humanity registry reads. The shared registration includes only those
reviewed profiles. The existing native and local WEB4 proofs remain
the source acceptance. [Image CI 42336260220](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42336260220)
must pass before promotion and hosted acceptance.

The eight-value protected candidate and twelve presets use prefix
registry-object-public under LocalState/COSMIC/HostedSourceRolloutCurrent.
No hosted settings were applied. Oracle database hosting remains deferred.
