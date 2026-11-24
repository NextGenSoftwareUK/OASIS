# Snowflake WEB4 dependency gate

The standalone Snowflake CQL reader builds and passed four offline checks.
The managed teaching warehouse and live WEB4 acceptance are still open.

A leased WEB4 trial resolved AWSSDK.Core 4.0.100.8, S3 4.0.101.4,
and DynamoDBv2, SecurityToken, SSO, and SSOOIDC 4.0.100.1. Restore
succeeded with those pins. Compile failed with 16 errors because existing
provider logic expects non-nullable SDK members. The trial pins were
reverted. Snowflake registration remains outside WEB4.

- DynamoDB capacity receipts must distinguish missing usage from zero in
  DynamoCapacityUsage. DynamoItemSize must handle nullable BOOL and NULL
  without undercounting explicit false.
- DynamoDB point reads must recognize an HTTP 200 response with no Item
  as a documented miss. Page reads still need to distinguish a valid empty
  page from missing or contradictory read evidence.
- Shared S3 can use the public nullable completion flag and key count
  instead of v3 private SDK accessors. Cover empty pages and preserve
  rejection of missing completion flags and conflicting continuation
  tokens. Apply the same completion contract to MinIO.
- Verify existing DynamoDB acknowledgement and shared S3 protocol tests
  against the changed SDK contract before reintroducing the Snowflake
  reference. Keep the existing live acceptance evidence for the v3 cohort.

[AWS SDK for .NET v4 migration guide](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-v4.html)

[DynamoDB GetItem response contract](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_GetItem.html)

## Local v4 migration checkpoint

The trial pins were reapplied after the nullable capacity, item-size,
point-read and pagination changes. Shared S3 now reads the public v4
presence flags. The AWS public metadata reader requires a native
last-modified value before publishing it. Nine focused S3 protocol
cases and 93 DynamoDB acknowledgement, lifecycle, capacity and CQL
cases passed on v4. WEB4 build with the Snowflake connector reference
is the next check. The source registration and managed qualification
remain open.

WEB4 restore resolved Snowflake.Data 6.2.0, AWSSDK.Core 4.0.100.8 and
System.IdentityModel.Tokens.Jwt 8.21.0 together. The first full build
after restore failed on an unrelated subscription-admin method that
was absent when compilation began. The implementation is now in the
shared checkout. A new leased build includes opt-in Snowflake reader
registration and the exact-key admission entries. The focused protected
configuration test passed. Native qualification requires a dedicated
SELECT-only Snowflake role, teaching warehouse, and reviewed catalogue.

The leased WEB4 build with reader registration passed with zero errors
and 2,159 warnings in 53 minutes. The focused WEB4 admission test
session was started next. The successful build closes the local
compile gate only.

The focused WEB4 run passed both offline admission cases. The managed
Snowflake and Mongo HTTP fixture was skipped because the reviewed
teaching source is not configured. No native request was submitted.

MinIO and Tigris now use the shared S3 listing implementation.
Both provider builds passed with SDK v4. Forty shared transport tests
and the focused omitted-object-list regression passed. Native MinIO
and Tigris checks on the upgraded packages remain open.

The final local consumer batch passed all 22 focused WEB4 tests after
the MinIO and Tigris changes. It covered HyperDrive JWT validation,
AWS S3 CQL examples, and DynamoDB hosted configuration. An initial
test compile was blocked by assertions in the concurrent subscription
plan-grant tests. The shared checkout had corrected them before the
successful rerun. No managed source or deployment was exercised.

BootLoader, a separate consumer of the upgraded AWS providers,
restored the same AWS SDK v4 family and built with zero errors and
three warnings. DynamoDB scan and indexed pagination controls passed
on the updated acknowledgement path. No BootLoader runtime or managed
provider checks were performed in this batch.

### Native verification still due

- MinIO passed 13 focused CQL cases and Tigris passed 15 focused CQL
  and S3 protocol cases on the upgraded SDK. Native requalification of
  those services is still due.
- AWS explicit and default-chain session credentials passed real SDK
  signing through the shared offline HTTP handler. Eleven AWS S3
  metadata controls passed, including rejection of a missing native
  timestamp. The native public S3 test skips without the reviewed
  catalogue.
- DynamoDB Local and the installed Java runtime are available on this
  laptop. The integration fixture compiles and skips without its
  owned service. Automatic command review rejected the attempted
  service start, test and cleanup command, so no native pass is claimed.

Earlier hosted cohorts remain accepted for their recorded releases.
