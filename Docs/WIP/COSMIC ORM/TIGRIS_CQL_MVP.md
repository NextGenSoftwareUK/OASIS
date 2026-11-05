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

Prepare a fail-closed native fixture and launcher against an explicitly admitted,
owned Tigris teaching bucket, using read-restricted credentials from environment
variables. Independently verify original SDK object hashes/ETags, owner and
identity, three live records, one deleted and one absent; prove CQL 3/2/0 and
paging, then unchanged originals. No MinIO substitution, bucket creation, source
rewrite or new paid account. Native credentials/source were not present in the
inspected Tigris environment variable names; no credential values were printed.
Add the concrete owner action only after this fixture is reviewable.

After native qualification: manager/Mongo/HTTP and browser 5/4, exact temporary
fixture cleanup, and only then an accepted local catalogue card. Hosted sandbox
quota, LocalFile deployment access and existing owner decisions remain unchanged.
