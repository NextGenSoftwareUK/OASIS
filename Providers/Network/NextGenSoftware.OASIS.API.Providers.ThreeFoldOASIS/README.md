# NextGen Software WEB4 OASIS API ThreeFold OASIS Provider

WEB4 OASIS API ThreeFold OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
## QSS storage configuration

Current source reuses the shared encrypted S3 transport and record policy through MinIOOASIS. Existing endpoint/access-key constructors use `OASIS_THREEFOLD_STORAGE_KEY`: canonical Base64 for a separately persisted 32-byte encryption key, independent of signing credentials. Retain the key across restarts; no replacement key is generated. An explicit `IAmazonS3`/key-resolver constructor is also available; the caller owns the client by default, and SDK retries must be disabled (`MaxErrorRetry=0`). Dispose the provider when finished.

Provision the bucket before activation and match `useSSL` to the endpoint scheme. Legacy plaintext OASIS object prefixes cause an explicit read-only refusal before new-layout operations; no automatic migration/reset occurs. Default holon deletion remains soft. Encrypted identity indexes preserve avatar lookup by GUID, username and email.

Controlled conformance through the shared adapter does not certify a particular QSS service. Live QSS HEAD-bucket, conditional request, pagination, IAM and durability support must be verified against the configured endpoint; MinIO-compatible evidence is not QSS-native acceptance.
