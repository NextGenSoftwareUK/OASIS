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

WEB4 enrollment and live federation remain open. LocalFile remains
first for the live rollout.
