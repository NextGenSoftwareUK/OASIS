# Farcaster public source qualification

Updated 2 November 2026. Not qualified, implemented or admitted to CQL here.
No change to integrated, hosted or browser provider totals.

## Observed native availability

The bounded candidate was Pinata's anonymous Hub at `https://hub.pinata.cloud`,
using explicit FIDs1/3/99 and `user_data_type=2` (display-name messages).
The first FID request failed before an HTTP response (`URLError`); remaining
FID requests did not run. A separate DNS-only observation returned
`DNS_ERROR_RCODE_NAME_ERROR`. This establishes failure from this environment
at the recorded time, not a universal claim about every Farcaster node.

Four offline qualifier tests pass: exact native identity/mainnet/type, uint32
timestamp preservation, malformed or wrong-kind data and duplicate JSON fields.
These parser tests are not native or CQL support evidence.

No account, signer, source owner, node environment, catalogue, Mongo fixture,
permission, tariff, hosted setting or sandbox card was created. No writes,
HTTP retries, alternate endpoints or paid Neynar calls occurred. Preserve the
original attempt instead of replacing it with a successful later observation.

## Evidence outside docs/git

`C:/Source/OASIS-cosmic/TestResults/FarcasterPublicSourceCurrent/` contains3files,
469bytes. Retain while native availability is unresolved; no credentials exist
in this evidence.

| Artifact | SHA256 |
|---|---|
| `qualification.intent.json` | `bd4be0faecd0683eb18880a092dec1514e05fa6662b55d8cf71eded45d22e589` |
| `qualification.json` | `9d4b02cae400a8a6d2a7187d3814c6fcf38ba69a2db255e9ccd3670625286154` |
| `dns.observed.json` | `0a5f91c22bf88ce5e0c221ff6e81ba663a8e934ddf4fbe180a46de51d26577d3` |

Offline command:

```powershell
python -m unittest discover -s Scripts/Sandbox -p test_farcaster_public_probe.py
```

The native probe's exclusive intent is consumed. Do not remove its guard or
rerun it as though this failure never happened.

## Next scope and legacy gaps

Revisit only with an explicitly selected available free/public Hub and a fresh
qualification that links this original failure. Native protocol/API access must
be proved before adding capability declarations, host admission or demo claims.
Any future display-message profile must state whether signatures are actually
verified and must not imply freshness, a current full account profile, signer
authorization or independent Snapchain consensus from a node-reported value.

Existing `FarcasterOASIS` uses Neynar's API-key interface. It is not a qualified
anonymous Snapchain display-message CQL adapter. Legacy issues are documented;
the legacy implementation is unchanged. No action is required from David while
other provider/demo work can proceed.

Official sources: [Snapchain UserData API](https://snapchain.farcaster.xyz/reference/httpapi/userdata),
[Farcaster account-profile guide](https://docs.farcaster.xyz/developers/guides/querying/fetch-profile),
[Pinata's public-Hub announcement](https://pinata.cloud/blog/why-we-are-all-in-on-farcaster/).
The older announcement establishes the operator's published service intent;
the captured native/DNS observations determine availability for this attempt.
