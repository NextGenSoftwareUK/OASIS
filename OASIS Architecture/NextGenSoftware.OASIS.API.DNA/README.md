# NextGen Software WEB4 OASIS API DNA.

This module is responsible for loading and managing the OASISDNA.json config file. This is where all of the settings for the ONODE are kept such as settings for each OASIS Provider configured for that ONODE, which ones to use for auto-replication, auto-loadbalance, auto-failover, email, authentication, security, error handling, encryption, logging settings etc.

NOTE: This module is not intended to be used standalone, it is simply a dependency for the [OASIS.API.Native.Integrated.EndPoint](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint) module/package and is the recommended way to use the OASIS API along with the REST API below.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version):
https://api.oasisweb4.one

Full documentation and source code can be found here:
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!

## Settings added in November 2026 (all in `OASIS_DNA.json`; keep every copy and the Railway `OASIS_DNA_JSON` in sync)

| Section | Key | Default | Meaning |
|---|---|---|---|
| `OASIS.AccessLog` | Enabled | false | Record who READ a holon (ids only). Off = no overhead, no writes. An app can override per app account. |
| | Apps, HolonTypes, Operations | [] | Limit what is recorded; empty = all. |
| | CaptureIp | false | Also store the caller's IP. |
| | RetentionDays | 365 | Auto-delete (MongoDB TTL); 0 = forever. |
| | QueueCapacity, FlushIntervalSeconds, BatchSize | 10000, 5, 500 | In-memory queue and background write. |
| | MaxHolonIdsPerEvent | 200 | A bulk read stores one event with at most this many ids. |
| `OASIS.Security.Mfa` | Enabled | false | Optional authenticator-app second step; only avatars that enrol are asked. |
| | Issuer, Apps, AvatarTypesThatCannotDisable | OASIS, [], [] | Name in the app, limit to apps, types that need an admin reset. |
| | CodeDigits, TimeStepSeconds, AllowedDriftSteps | 6, 30, 1 | TOTP parameters. |
| | ChallengeTtlSeconds, MaxChallengeAttempts | 300, 5 | Sign-in challenge lifetime and wrong-code limit. |
| | MaxFailedCodes, CooldownSeconds, MaxCooldownSeconds | 5, 60, 3600 | Per-avatar cool-down. |
| | RecoveryCodeCount, EventRetentionDays | 10, 730 | Recovery codes and audit retention. |
| `OASIS.UsageVisibility` | OwnerViewEnabled, WarningsEnabled, EmailEnabled, AppPayAsYouGoEnabled | true | Usage views and warnings (email uses `OASIS.Email`). |
| | DefaultWarningPercents | [80, 100] | Warning thresholds. |

The access log and MFA store their data in the MongoDBOASIS database named in `StorageProviders.MongoDBOASIS`. People and app accounts can override the usage visibility defaults (and apps the access log) in the portal; their choice wins.

Copies to keep in sync: `Default/OASIS_DNA.json`, the test-harness copies in the OASIS2 repo, the STAR ODK `STARDNA/DNA/Default/OASIS_DNA.json` and the NuGet OAPP template, and Railway `OASIS_DNA_JSON`. `OASIS_DNA_SYSTEM.json` is encrypted binary and is not edited.
