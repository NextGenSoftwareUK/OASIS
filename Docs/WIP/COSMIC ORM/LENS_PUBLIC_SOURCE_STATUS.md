# Lens public-account demo qualification

Updated 2 November 2026. Native qualification failed; not implemented or
admitted to CQL. Provider/hosted/browser totals unchanged.

## Verified interface distinction

The [official SDK account actions](https://github.com/lens-protocol/lens-sdk/blob/main/packages/client/src/actions/account.ts)
define fetchAccount, fetchAccounts and fetchAccountsBulk on AnyClient. A session
adds account-specific operations; public read support in source still needs
native verification. The [SDK README](https://github.com/lens-protocol/lens-sdk)
identifies its work as Lens Protocol v3.

Existing LensOASIS comments call api.lens.xyz/graphql a v2 profile API, whereas
the current SDK uses account requests. LensV2OASIS separately targets
api-v2.lens.dev. Do not submit legacy profile operations to a current endpoint,
change legacy code or count v3 accounts as proof of LensV2 support.

Official website API/account documentation could not be fetched by the web
tool. A GitHub directory request returned429; no repeat, proxy or bypass was
attempted. This is documentation-tool evidence, not a native API failure.
Search also returned scholarly/patent Lens.org APIs; these are unrelated and
excluded from the provider design.

## Pinned schema and first native qualification

SDK revision `342abd5efb3b12d306c89101a00e60f722df7f33` was obtained
with git ls-remote. Retained official schema201295bytes/SHA256
`c77e3843f7f211de49a6789a9bca05474f4369f5f1d0c5fb877a47e7b2786633`
and environment1266bytes/SHA256
`0dd19dfb4734d4be8d55e018f758a9d092bfaa445ac4efe4a7c589947bbccc1d`
under `LocalState/COSMIC/LensPublicQualificationCurrent/sdk/`; no clone/install.
[Mainnet environment](https://github.com/lens-protocol/lens-sdk/blob/342abd5efb3b12d306c89101a00e60f722df7f33/packages/env/src/index.ts)
selects `https://api.lens.xyz/graphql`;
[pinned schema](https://github.com/lens-protocol/lens-sdk/blob/342abd5efb3b12d306c89101a00e60f722df7f33/packages/graphql/schema.graphql)
defines accounts(pageSize:TEN), accountsBulk(addresses), address/owner/username
and nullable metadata name/bio. No metadata URLs were selected or traversed.

`Scripts/Sandbox/Probe-LensPublicReadiness.py` plans at most3anonymous read
POSTs: firstpage10, deterministic3keys within that sample, then exact3/2bulk
sets. It rejects duplicate JSON/identities, missing selected fields, malformed
addresses/text, GraphQL errors/partial data, unexpected key sets, redirects,
oversized responses and elapsed-budget violations. Socket timeout15s; elapsed
checks are not a hard cancellation guarantee during DNS/blocking reads.
This qualifier is not a production CQL adapter.

7offline tests in `Scripts/Sandbox/test_lens_public_probe.py` passed. First
native discovery returned HTTP403 after0.5s,702bytes: Cloudflare1010,
`browser_signature_banned`, `retryable:false`, explicit instruction not to
retry. Neither bulk read ran. No accounts selected or native data accepted.
No user-agent rotation, alternate endpoint, browser automation or other bypass.

Durable original intent/ACK/denial are retained at
`LocalState/COSMIC/LensPublicQualificationCurrent/`; raw response/report also
at `TestResults/LensPublicSourceCurrent/`. Exclusive intent is consumed and
must never be removed/recreated to replay the attempt.

| Durable artifact | Bytes | SHA256 |
|---|---:|---|
| `qualification.intent.json` | 344 | `f87578d642e40f7ddcce1728e79b169152f7e6c4c7571ec120d805401fcc877c` |
| `qualification.json` | 344 | `bc87cc1112bee0131968757f066f95cefcced580c3a4968d1140aba81bfaebfb` |
| `discovery.json` | 702 | `fe6dae2f3c075f2dda00ac33ae6c40adabcaa1790d62404be9439a3d0e064630` |

## Next scope

Lens remains red: anonymous access denied from this environment. No CQL source,
source owner, fixture, tariff, hosted setting or sandbox card was created.
Legacy LensOASIS/LensV2OASIS unchanged. A future continuation requires explicit
legitimate access and preserved lineage, not denial bypass. No repeated owner
setup question while other work is available. Next review Bittensor's official
public read interface and existing Substrate adapter reuse. Counts unchanged.
