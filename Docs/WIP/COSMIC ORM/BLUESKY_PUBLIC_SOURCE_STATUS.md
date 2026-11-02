# BlueSky public profile source qualification

Updated1November2026. Native AppView protocol qualified only; CQL/API/hosted integration remains unimplemented. Existing49integrated/44hosted counts do not include BlueSky.

## Contract and evidence

- Existing shipping BlueSkyOASIS provider uses credentialed PDS sessions for canonical storage; no legacy implementation changed. The new scope is a separate anonymous public profile projection.
- Official [API hosts/auth](https://bsky.network/docs/api-directory/) and [HTTP reference](https://endpoints.bsky.app/) identify `https://public.api.bsky.app/` for unauthenticated public reads. `app.bsky.actor.getProfiles` accepts bounded actor identifiers and returns detailed profiles.
- Three guarded HTTP200 GETs passed: explicit handle discovery3, observed DID selection3, then DID subset2. Handles `bsky.app`, `atproto.com`, `jay.bsky.team`; returned sets complete/unique and identities validated. No search/firehose, source write, PDS session/account/funding/node/install.
- Original DIDs: `did:plc:z72i7hdynmk6r22z27h6tvur`, `did:plc:ewvi7nxzyoun6zhxrhs64oiz`, `did:plc:oky5czdrnfjpqslsw2a5iclo`. Future native queries should use these stable captured DIDs rather than mutable handles.
- Cached public AppView observations only. No signed repository proof, bi-directional DID-document verification, authoritative freshness or common snapshot is claimed. Display names/descriptions can change; do not fetch avatar/banner URLs implicitly.
- Offline parser/transport check passes: bounded encoded GET, complete returned array without task-output leaks, duplicate identity rejection and HTTP denial without retry. Zero native requests in this check.

## Exact next step after ChainLink rollout

Implement a separate read-only BlueSkyPublicProfileMetadataSource in the existing Social provider project, using shared CQL/planner/native mapper and bounded equality/membership on BlueSkyActorDid. Require explicit known DIDs/guarded anonymous HTTPS AppView configuration. Validate exact selected DID set/duplicates/schema/size/cancellation before publishing any rows; no scans or partial results. Preserve profile names/descriptions and exact optional count/time metadata without inventing missing fields. Keep signed PDS storage separate.

Then controlled tests/native manager+C#3/2, independent API read admission/tariff, local Mongo/privacy/prepared presets, exact hosted rollout/original acceptance and card. Do not count native qualification as CQL/hosted support. ChainLink's in-flight settings deployment remains the current priority.

## Durable state and raw output

Original intent/stable source owner/catalogue: `LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent`. Never regenerate the owner or replay the original qualification. Raw HTTP and offline fixtures: worktree `TestResults/BlueSkyPublicSourceCurrent`, not Docs. Scripts: [qualification](../../../Scripts/Sandbox/Probe-BlueSkyPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-BlueSkyPublicProbe.ps1).

- `LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent/source.json`: 265bytes; SHA256 `697a63bcb3a589ce6d8f841a2181fea6a1f5702886c161ab459e01ffe80a978c`.
- `TestResults/BlueSkyPublicSourceCurrent/qualification.json`: 1553bytes; SHA256 `b02ba4159618f4f9be95642d080f6dbbe2b641dd9414aa59d3f9f27e7c96bbd4`.
- `TestResults/BlueSkyPublicSourceCurrent/1-getProfiles.json`: 4654bytes; SHA256 `3521b839c7b9df3938baa77987e8ec058fdd44221abb6a351ddfff363d8616e0`.
- `TestResults/BlueSkyPublicSourceCurrent/2-getProfiles.json`: 4654bytes; SHA256 `3521b839c7b9df3938baa77987e8ec058fdd44221abb6a351ddfff363d8616e0`.
- `TestResults/BlueSkyPublicSourceCurrent/3-getProfiles.json`: 3521bytes; SHA256 `786492f1770f7116435eeb8d2037f77356aa08b608d8d1b79cc7661c54581b3f`.
