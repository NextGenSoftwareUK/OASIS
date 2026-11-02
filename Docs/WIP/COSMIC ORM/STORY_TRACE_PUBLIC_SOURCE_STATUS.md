# Story / DATA public contract qualification

Updated 2 November 2026. No CQL adapter/admission or sandbox proof added.

## Current contract and legacy gaps

The existing `StoryProtocolOASIS` comment lists chain1516 and uses
`api.storyprotocol.xyz/api/v3` with an API key. Current official network docs
list1514. They describe a planned Sui migration and eventual retirement of the
independent chain. These observations document legacy drift; legacy code is
unchanged.

The current Trace guide offers anonymous audit reads on `api.dataapis.io`.
Provider writes need onboarding/keys. The candidate here was its recent-record
read, with an explicit3-row limit. No new adapter was built against the older
chain/API, and no claim of Sui/Trace equivalence was made.

## Observed failure

One anonymous GET to
`https://api.dataapis.io/api/v1/data-audit/recent?limit=3` returned HTTP403.
The705-byte JSON error identifies Cloudflare1010,
`browser_signature_banned`, category `access_denied`, and `retryable:false`.
No native records or IDs were obtained. This is an access denial from this
environment at the observed time, not evidence that the documented schema or
every public client is broken.

No retry, user-agent impersonation, proxy, browser automation, alternate staging
endpoint, account/API key or write was attempted. Source owners, demo avatars,
Mongo data, tariffs, host settings and CQL capability claims were not created.
No action is required from David while other providers can progress.

## Evidence

Raw evidence stays in
`C:/Source/OASIS-cosmic/TestResults/StoryTracePublicSourceCurrent/`:

| Artifact | Bytes | SHA256 |
|---|---|---|
| `discovery.intent.json` | 157 | `7c8453f3bf31f6b1bc32429cc4abedba595c7a25a9a10a420c34089009863016` |
| `discovery-response.json` | 705 | `c829bb059ae634885e6035c6f61e0605933e916b9f5eff2a159364c5d69d8d9d` |
| `discovery.observed.json` | 230 | `81b67b9603d6fc6202101b790ef2652ddcfa1e2f2d31603698bedae4cc97829b` |

Total1092bytes; no credentials. Exclusive discovery intent is consumed. Do not
delete its guard, overwrite outcomes or rerun it as though this denial did not
happen. Revisit this source only after a documented supported access change;
native qualification remains required before implementation or admission.

## Next available breadth work

Inspect `BlockStackOASIS` and the modern Stacks Node RPC account/tip contract.
Qualify a fixed public mainnet endpoint without signing, keys, new environments
or writes before implementing a bounded read profile. Preserve this independent
Story failure; it is not a reason to block the whole ORM/CQL programme.

Primary references: [DATA introduction](https://docs.datafdn.org/introduction),
[Sui transition](https://docs.datafdn.org/data-on-sui),
[current mainnet](https://docs.datafdn.org/network/connect/mainnet),
[Trace integration/read API](https://docs.datafdn.org/trace/integration).
