# Tigris CQL MVP continuation

5 November 2026. Controlled implementation verified; native qualification pending.
Accepted counts remain 103 local, 60 hosted HTTP and 28 hosted browser.

## Implemented scope

Tigris now exposes the shared bounded identity CQL compiler and explicit read-only
query activation through an existing live probe. It uses the provider's existing
AWS SDK and canonical `oasis/holon/{id:N}` keys, not the MinIO key layout.
Activation performs one bounded GET; no bucket HEAD, LIST, provisioning or saves.
Queries enforce authoritative identity selection, deletion exclusion, pagination,
root projection before deserialization, strict native failure propagation and no
partial publication. Limits: 100 admitted identities, 1 MiB per record, 4 MiB per
query, 15 seconds and zero SDK retries for the query client. Historical record
selection remains explicitly unsupported.

The shared production client factory previously forced path-style bucket URLs.
A real AWS SDK HTTP-pipeline control reproduced the incorrect host
`t3.storage.dev` instead of `teaching.t3.storage.dev`. The factory now selects
virtual-hosted URLs and preserves the explicitly configured endpoint and `auto`
signing region. No endpoint probing or second request path was added. Vendor
guidance: [Tigris's published migration article](https://dev.to/tigrisdata/moving-to-virtual-hosted-urls-h1g),
which documents virtual-hosted URLs for new buckets and the .NET setting.

## Evidence

Existing EdgeStorage protocol test project and cached AWS SDK reused under the
provider build lease. Filter only `TigrisCqlTests|TigrisS3ProtocolTests`.
14 passed, 0 failed, 0 skipped. Includes 12 CQL cases and two existing Tigris
protocol tests. These are controlled HTTP/SDK tests, not native cloud evidence.

- Initial build error (wrong serializer qualification) retained separately;
  corrected to the provider's shared canonical serializer.
- Address regression: `LocalState/COSMIC/RecoveryLaptopTigrisCql/address-red-v2/controls.trx`;
  one expected failing host assertion before factory correction.
- First corrected run: 13/14; test incorrectly read the typed Authorization
  property. Corrected assertion reads the actual SDK header collection.
- Final: `LocalState/COSMIC/RecoveryLaptopTigrisCql/controls-green-v2/controls.trx`;
  SHA256 `42fc1b51fc8ea7d801400fb09c235eaa0935a2d43506cc6f1b9cc0c53c3f0322`.
- Build logs: `TestResults/ProviderBuildCurrent/cosmic-tigris-*.log`.

Build used actual shared Core `54159614ebdb879c8636eed5fae60be97dbbfff3`;
this does not qualify the task's pinned parent API image or deploy Tigris.

## Next scope

Prepared an opt-in native fixture and launcher against an explicitly admitted,
owned Tigris teaching bucket, using read-restricted credentials from environment
variables. Independently verify original SDK object hashes/ETags, owner and
identity, three live records, one deleted and one absent; prove CQL 3/2/0 and
paging, then unchanged originals. No MinIO substitution, bucket creation, source
rewrite or new paid account. Native credentials/source were not present in the
inspected Tigris environment variable names; no credential values were printed.
The fixture lives beside the controlled tests but is compiled only with
`EnableTigrisCqlNativeTests=true`. A missing source is an error, never a skipped
native pass. The launcher checks exactly one pass and zero skips and retains
intent/ack/TRX/source-hash evidence in a unique phase of the existing Tigris cache.
Build-only qualification completed with 0 errors (1135 existing/dependency and
nullable warnings), phase `build-6c8cb5473d39452db1b367cd7288e84d`. Missing native
descriptor is verified to fail before a phase or lease is created. The initial
launcher expression error was corrected before this successful build. No native
request ran. [Owner access entry](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#tigris-owned-teaching-source-access---2026-11-05)
is pending; continue other red scopes without substituting a service.

### Native admission and invocation

`Scripts/TestHosts/Run-CosmicTigrisNative.ps1 -BuildOnly` compiles without cloud
access. Omit `-BuildOnly` only after the owned teaching source is admitted.
Set `COSMIC_TIGRIS_CQL_SOURCE` to a private JSON descriptor, and privately supply
`COSMIC_TIGRIS_CQL_ACCESS_KEY` and `COSMIC_TIGRIS_CQL_SECRET_KEY` in the process
environment. Do not put credentials in the descriptor, chat or committed files.

The descriptor has exactly these case-sensitive fields: `Endpoint`, `Bucket`,
`Actor`, `LiveIds` (three UUIDs), `DeletedId`, `MissingId`, and `Originals` (four
objects with `Id`, `Sha256`, `ETag`). SHA/ETags must be captured from the original
owned source's provisioning receipts, not generated from a failing query run.
All five IDs must be distinct and nonempty. Endpoint is exactly
`https://t3.storage.dev` or `https://fly.storage.tigris.dev`; bucket is a non-dotted
DNS-safe name. Descriptor is at most 16 KiB, rejects unknown/duplicate properties,
and must remain byte-identical during the run. The fixture compares four native
canonical object identities, owner, deletion, exact bytes and ETags with those
receipts before and after CQL 3/2/0/paging. The missing object must return exactly
`NoSuchKey`/404. No HEAD, listing, write, delete or provisioning call is issued.

If no source exists, identify an owned Tigris account/bucket and private
provisioning access so engineering can prepare the teaching records and receipts;
David need not write records manually. Do not create a paid account implicitly.

After native qualification: manager/Mongo/HTTP and browser 5/4, exact temporary
fixture cleanup, and only then an accepted local catalogue card. Hosted sandbox
quota, LocalFile deployment access and existing owner decisions remain unchanged.
