# Nostr public event qualification

Updated 2 November 2026. **Not CQL-admitted or hosted-certified.** No source owner/catalogue has been created.

## Current evidence

- Existing NostrOASIS uses relay WebSockets, but its legacy query returns the first EVENT and does not prove complete bounded selections. Legacy code remains unchanged; gaps are documented only.
- NIP-01 defines REQ subscriptions, EVENT delivery, EOSE completion and SHA256 event IDs/BIP-340 signatures. A separate read-only demo must honor these contracts, not treat connection success or a first event as a complete result.
- Added bounded guarded transport: anonymous explicit relay.damus.io only, cookies disabled,20second deadline,64frames/64KiB message/256KiB aggregate,8event cap, strict text/UTF8/envelope/subscription/EOSE. Disposal closes the one-shot socket; no login/publish/alternate relay/retry.
- Original kind1/limit3 discovery completed:3unique events. Offline canonical NIP-01 hashes and BIP-340 signatures of all3captured events verify. This proves signed event integrity for those public keys, not real-world identity, freshness, relay completeness or persistence.
- Exact captured3-ID follow-up failed at WebSocket upgrade: native HTTP503 instead of101. Original failure retained. Two-ID query never ran. No silent retry, endpoint switch, partial success or CQL admission.
-9offline tests pass: complete/reordered sets, missing/duplicate/unexpected/subscription/schema/bounds, official BIP-340 public vector0, altered signature/message/ranges/off-curve, canonical Unicode hash and signature failure. Public-input probe verifier only; shipping verifier now separately verified below; CQL integration remains red.

## Exact next work

1. Preserve original intent, discovery frames, signed IDs and503 outcome. Do not rerun original probe or regenerate a source owner.
2. Implement a bounded read-only CQL event projection with controlled complete3/2sets and shared capability planning, using the verified shipping signature boundary below. Keep native admission red until exact same captured IDs can be read completely; a future explicit bounded qualification continuation must preserve the original failure and avoid discovery/new owners.
3. Only after native3/2 and controlled gates: actual C# manager/LINQ, API read admission/local Mongo/privacy/prepared presets, exact hosted rollout and original hosted cohort, then page card/public bytes. No native writes or social-network search claim.

## Evidence and resources

Durable state: LocalState/COSMIC/HostedNostrPublicSourceCurrent/native-availability.intent.json, native-availability.observed.json and discovery-crypto.observed.json. No credentials, signing keys, local server, database or new runtime. Raw public messages/failure output: TestResults/NostrPublicSourceCurrent. These are untrusted public notes; do not execute or render their text as HTML.

Scripts: Scripts/Sandbox/Read-NostrPublicEvents.ps1, Probe-NostrPublicReadiness.py, NostrPublicEventValidation.py; offline tests test_nostr_public_probe.py and test_nostr_public_event_validation.py. Run `python -m unittest discover -s Scripts/Sandbox -p "test_nostr_public*.py" -v` (no network).

Primary contracts: [NIP-01](https://github.com/nostr-protocol/nips/blob/master/01.md), [NIP-11 relay metadata](https://github.com/nostr-protocol/nips/blob/master/11.md), [BIP-340 public vectors](https://github.com/bitcoin/bips/blob/master/bip-0340/test-vectors.csv).

- `1-events.json`: 2368bytes, SHA256 `62a07d8c439ca02a6a683f3ddcc7cb94393738ac570927ce75fea8abfec6b4f4`.
- `1-transport.stderr.txt`: 0bytes, SHA256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- `2-events.json`: 0bytes, SHA256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- `2-transport.stderr.txt`: 421bytes, SHA256 `0858eff27fdebbccfa1003de40dcf824243e3a52c29ec9d4ac3a3de9e3f65cde`.
- `availability.json`: 1003bytes, SHA256 `110f7b57f0405270dfaf23a98bd51d55dee7851e7ebd0de4b31989dbc5637933`.
- `discovery-crypto.json`: 263bytes, SHA256 `28584da40f817b6a809cc3ac7238f23f9afa9f37456ddc18b3dc9d5f76519e36`.

## Shipping signed-event verification milestone

Separate NostrPublicEventVerification.Parse now validates bounded kind1 schema, exact timestamp lexemes/ranges, lowercase hex, bounded string-array tags, UTF8 content, canonical NIP-01 hash and BIP-340 signature before returning immutable event data. No signing/private-key path or legacy-provider change. Canonical serialization keeps U+2028/U+2029/supplementary scalars literal and rejects unpaired UTF16. Public verification arithmetic is independently checked against all19official vectors (including invalid points, infinity, range, parity and variable-length messages).

36tests pass/0failures/0skips after producer then consumer builds under the provider lease. Includes captured signed-event round-trip, every signed-field mutation, malformed types/timestamps/tags, Unicode/control escaping, duplicate properties/size/width limits. These tests certify this boundary, not CQL/network integration or native availability.

Official vectors fetched once (6,892bytes, SHA25634c9d1d9c3a88d524bc80778540dc43f8306ec249a7485293063c376db851c2d); test fixture retains only public verification fields, never secret-key/aux columns. TestData/captured-public-event.json is560bytes from original frozen discovery, no new native call. Exact fixtures live in existing Nostr UnitTests/TestData; verification code in existing provider project.

- TestResults/NostrPublicSourceCurrent/event-verification.trx: 63525bytes, SHA256 `39c986397dc53385059f0f89f402773c69d8ae959896b570afe5f9ff195c48a8`.
- TestResults/NostrPublicSourceCurrent/event-verification-restore.log: 990bytes, SHA256 `35fcc3d1b6e969d35b19e171f76ecd7711a32f9e6a7fa14055366aba9d52641b`.
- TestResults/NostrPublicSourceCurrent/event-verification-provider-build.log: 82bytes, SHA256 `d7192a60ac7df17c3cc5e38b162cf0162473313cf75e032c468dff234ee538b6`.
- TestResults/NostrPublicSourceCurrent/event-verification-test-build.log: 82bytes, SHA256 `86db372993c50acf22c2788bfd68847ff54e10ad3fc65776638e53c9b5e514e6`.
- TestResults/NostrPublicSourceCurrent/event-verification-tests.log: 546bytes, SHA256 `0815d32f736f64620286b0ec778bc61ac29dc088686b88f66319ef55bc9ed444`.
