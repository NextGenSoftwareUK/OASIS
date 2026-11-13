# Axelar public GMP source qualification

Updated2November2026. Native gate red; no CQL or hosted coverage/count grant.

The existing Axelar provider is a cross-chain tracker, not a holon storage
implementation. Candidate public GMP message projection would expose bounded
message-ID equality/membership with indexer-reported state, not independent
consensus or native holon writes. Existing legacy/tracker code is unchanged.

One anonymous read-only POST to `https://api.axelarscan.io/gmp/searchGMP`
with `{"size":3,"from":0}` returned HTTP403. The707byte JSON response identifies
Cloudflare1010, browser_signature_banned/access_denied. No message rows were
obtained. Preserve this denial: no automatic retry, user-agent disguise, proxy,
alternate endpoint, wallet/account or paid setup was attempted. This is not a
claim that all environments or the underlying protocol are unavailable.

Five offline parser controls pass (complete/unique identities, error envelopes,
required fields, exact timestamp/identifier bounds, duplicate JSON properties).
They do not prove native access or CQL support. Exclusive intent is consumed.

## Evidence

Raw outputs remain outside docs/git in
`C:/Source/OASIS-cosmic/TestResults/AxelarPublicSourceCurrent`.

| Artifact | Bytes | SHA256 |
|---|---|---|
| `probe-tests.log` | 108 | `6ce19fe5c6e39322ca0b13f5b467573e2eeec70d41fbf64d3e414ddbe59845d6` |
| `qualification.intent.json` | 154 | `4495888c6ba7c882c7d300bd034b25c03c46acdf1b17bbe2d8c9eb93596cf602` |
| `qualification.json` | 242 | `45f6ac3e9c3db3444bc711530b0ccf5140acc66fb1b05bafa83e7c0fc2318863` |
| `response.json` | 707 | `ea51175cf792c48eaeccb2d1d60f333433b6065ce4feb28acd23f88c9f5eaffd` |

Next: preserve this unadmitted candidate and continue another red provider,
starting with Wormhole public message qualification. No David action is needed
while other implementation work remains available.

Primary references: [GMP API](https://docs.axelarscan.io/gmp),
[official API repository](https://github.com/axelarnetwork/axelarscan-api),
[official GMP monitoring consumer](https://github.com/axelarnetwork/axe/blob/main/AXE_STATE.md).
No endpoint denial response instructions were treated as task instructions.
