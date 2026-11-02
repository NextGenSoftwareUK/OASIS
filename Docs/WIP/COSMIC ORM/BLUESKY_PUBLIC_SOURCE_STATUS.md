# BlueSky public profile source qualification

Updated1November2026. Native AppView and local CQL/actual C# CLI verified; API admission/local HTTP federation verified; hosted rollout remains pending. Current50integrated types include BlueSky native-only;45hosted proofs do not.

## Contract and evidence

- Existing shipping BlueSkyOASIS provider uses credentialed PDS sessions for canonical storage; no legacy implementation changed. The new scope is a separate anonymous public profile projection.
- Official [API hosts/auth](https://bsky.network/docs/api-directory/) and [HTTP reference](https://endpoints.bsky.app/) identify `https://public.api.bsky.app/` for unauthenticated public reads. `app.bsky.actor.getProfiles` accepts bounded actor identifiers and returns detailed profiles.
- Three guarded HTTP200 GETs passed: explicit handle discovery3, observed DID selection3, then DID subset2. Handles `bsky.app`, `atproto.com`, `jay.bsky.team`; returned sets complete/unique and identities validated. No search/firehose, source write, PDS session/account/funding/node/install.
- Original DIDs: `did:plc:z72i7hdynmk6r22z27h6tvur`, `did:plc:ewvi7nxzyoun6zhxrhs64oiz`, `did:plc:oky5czdrnfjpqslsw2a5iclo`. Future native queries should use these stable captured DIDs rather than mutable handles.
- Cached public AppView observations only. No signed repository proof, bi-directional DID-document verification, authoritative freshness or common snapshot is claimed. Display names/descriptions can change; do not fetch avatar/banner URLs implicitly.
- Offline parser/transport check passes: bounded encoded GET, complete returned array without task-output leaks, duplicate identity rejection and HTTP denial without retry. Zero native requests in this check.

## Local CQL implementation milestone

- Separate read-only BlueSkyPublicProfileMetadataSource in the existing Social project, using shared CQL/planner/native mapper. Captured canonical did:plc equality/membership, maximum8configured DIDs/one native batch per query, no discovery/images/PDS sessions/writes.
- 37controlled tests pass/0failures/0skips: stable output despite reordered AppView array, point/subset/window, missing/duplicate/unexpected identities, exact count above2^53, Unicode, missing optional fields without invented counts/dates, invalid schema/count/time/text bounds, cancellation/budget and no partial publication; authenticated configuration and native saves rejected.
- Producer and actual console builds pass. Actual C# CLI/manager/LINQ3full/2filtered profiles pass against retained owner/DIDs. Original console intent/outcome retained; no source transaction/session or Mongo save.
- Optional displayName maps Name when nonblank, otherwise observed handle; original optional metadata remains distinct. Optional biography/count/date fields stay missing when absent. Counts remain exact nonnegative64-bit strings, RFC3339 timestamps retain original text. Production guarded sockets transport disables cookies.
- [Setup/C# guide](../../Devs/BLUESKY_PUBLIC_PROFILE_CQL_GUIDE.md), [commented walkthrough](../../../Examples/CosmicCqlDemo/BlueSkyPublicWalkthrough.cs). Existing credentialed PDS storage remains unchanged.

## Exact next step

Core permission1/API admission2/local HTTP1/publisher9/prepared-planner1/client preparation1 pass. Native3/2, Mongo5/4, foreign-private0/public3,2SDK version1 saves/9query receipts verified. Exact marked cosmic_cql_bluesky_http_demo database dropped, HTTP/Mongo stopped/27029 absent. Original fixture journal retained, no reseed/replay.

Lazy CQL_BLUESKY_PUBLIC_SOURCE_JSON registration, independent unsigned read permission/free tariff and4presets implemented; no PDS auth/write grant/default routing change. Protected baseline/candidate under LocalState/COSMIC/HostedSourceRolloutCurrent/bluesky-public-*: exactly3values differ, DNA unchanged, no settings applied yet. Source-bound native/local ACKs durable. Core task499e4e0b06104a3845abee243946fd61a27f069f pushed/ref-verified; parent gitlink/manifest paired.

Next: publish verified code to Development, observe exact code deployment, apply reviewed settings once and issue one journalled settings deployment. Only after SUCCESS run original hosted acceptance/card. No BlueSky hosted/browser claim yet. Preserve original owner/DIDs and completed native/local cohorts. Frontend preparation test taska5343b2 pushed, no card/publication yet.

## Durable state and raw output

Original intent/stable source owner/catalogue: `LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent`. Never regenerate the owner or replay the original qualification. Raw HTTP and offline fixtures: worktree `TestResults/BlueSkyPublicSourceCurrent`, not Docs. Scripts: [qualification](../../../Scripts/Sandbox/Probe-BlueSkyPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-BlueSkyPublicProbe.ps1).

- `LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent/source.json`: 265bytes; SHA256 `697a63bcb3a589ce6d8f841a2181fea6a1f5702886c161ab459e01ffe80a978c`.
- `TestResults/BlueSkyPublicSourceCurrent/qualification.json`: 1553bytes; SHA256 `b02ba4159618f4f9be95642d080f6dbbe2b641dd9414aa59d3f9f27e7c96bbd4`.
- `TestResults/BlueSkyPublicSourceCurrent/1-getProfiles.json`: 4654bytes; SHA256 `3521b839c7b9df3938baa77987e8ec058fdd44221abb6a351ddfff363d8616e0`.
- `TestResults/BlueSkyPublicSourceCurrent/2-getProfiles.json`: 4654bytes; SHA256 `3521b839c7b9df3938baa77987e8ec058fdd44221abb6a351ddfff363d8616e0`.
- `TestResults/BlueSkyPublicSourceCurrent/3-getProfiles.json`: 3521bytes; SHA256 `786492f1770f7116435eeb8d2037f77356aa08b608d8d1b79cc7661c54581b3f`.

- `TestResults/BlueSkyPublicSourceCurrent/controls.trx`: 56955bytes; SHA256 `e949d1e79d292bd20297ed942674f879910be16434e13af513a01c50540dc5d5`.

- `TestResults/BlueSkyPublicSourceCurrent/provider-build.log`: 39311bytes; SHA256 `2068b6e3bf02ceb0206fb7523a0943277b19f6014503e3202191e7b8d89a9332`.

- `TestResults/BlueSkyPublicSourceCurrent/demo-build.log`: 10857bytes; SHA256 `ff2f096587d502d77e97d406a2a42b34eea0c778875f734761ced70bf89072f1`.

- `TestResults/BlueSkyPublicSourceCurrent/cql-console.log`: 1134bytes; SHA256 `23978e479eae39642234452fce94fc2d9ed5d764ef16164abfc84cee19726f17`.

- `TestResults/BlueSkyPublicSourceCurrent/core-admission.trx`: 3306bytes; SHA256 `18fcb839776521faec737a65b5f7dad285d5d2dc41ccc0a8c0ce7f12e5c35b2b`.

- `TestResults/BlueSkyPublicSourceCurrent/api-admission.trx`: 4968bytes; SHA256 `d415a645fd2087f24c739bed9de44fd53e598a3918440bc77e0687e45ef5c2d2`.

- `TestResults/BlueSkyPublicSourceCurrent/mongo-http-native.trx`: 3365bytes; SHA256 `b7bd9051097a8becdcbed1e9e0ab5e7546a753c25046ebc9b1a532ea4b0d538a`.

- `TestResults/BlueSkyPublicSourceCurrent/prepared-planner.trx`: 3335bytes; SHA256 `a368c6677736c43e00dd645d4ffe4f8bfb6a1c3a65be0130ddce1c94d1e121cb`.

- `TestResults/BlueSkyPublicSourceCurrent/bluesky-mongo-native-summary.json`: 227bytes; SHA256 `5e7288ba0945652d254d073ada94e1962e75168cf5e8ce205b0be79e3a375365`.

- `TestResults/BlueSkyPublicSourceCurrent/prepared-public-review.json`: 3828bytes; SHA256 `54954703dd506ce8a6cb1c1167e1ca7f5a2270c381233e6de4f86b2775fb80b4`.
