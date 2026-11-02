# Nostr public event qualification

Updated 2 November 2026. **API/local qualification verified; not hosted-certified.** Stable source owner/catalogue now retained after explicit actual manager/LINQ qualification; no API/hosted enrollment.

## Original discovery evidence (preserved)

- Existing NostrOASIS uses relay WebSockets, but its legacy query returns the first EVENT and does not prove complete bounded selections. Legacy code remains unchanged; gaps are documented only.
- NIP-01 defines REQ subscriptions, EVENT delivery, EOSE completion and SHA256 event IDs/BIP-340 signatures. A separate read-only demo must honor these contracts, not treat connection success or a first event as a complete result.
- Added bounded guarded transport: anonymous explicit relay.damus.io only, cookies disabled,20second deadline,64frames/64KiB message/256KiB aggregate,8event cap, strict text/UTF8/envelope/subscription/EOSE. Disposal closes the one-shot socket; no login/publish/alternate relay/retry.
- Original kind1/limit3 discovery completed:3unique events. Offline canonical NIP-01 hashes and BIP-340 signatures of all3captured events verify. This proves signed event integrity for those public keys, not real-world identity, freshness, relay completeness or persistence.
- Exact captured3-ID follow-up failed at WebSocket upgrade: native HTTP503 instead of101. Original failure retained. Two-ID query never ran. No silent retry, endpoint switch, partial success or CQL admission.
-9offline tests pass: complete/reordered sets, missing/duplicate/unexpected/subscription/schema/bounds, official BIP-340 public vector0, altered signature/message/ranges/off-curve, canonical Unicode hash and signature failure. Public-input probe verifier only; shipping verifier/CQL/local API now separately verified below; no hosted claim.

## Exact next work (current)

1. Preserve original intent, discovery frames, signed IDs and503 outcome. Do not rerun original probe or regenerate a source owner.
2. Nostr code79b7be91/settings21ee8d23 SUCCESS at6ee3ebfcd/Core84a2151. Original owner native3/2 and Mongo5/4 pass; two normal private version1 saves ACKed, pending0. Independent read-only audit confirms matching Mongo/SQLite/Neo4j2records each with exact owner/version/metadata;0additional saves. Native read intermittently fails503/CQL_QUERY_FAILED during repeat preparation; foreign privacy/reuse/full hosted acceptance remain red. No replacement owner, source switch or resave; card task-only2ffca06/mainac0ca0c unchanged.
3. Add sanitized operator diagnostics for native query errors (exception type/transport stage/status only, never credential-bearing messages/URLs). Current HTTP endpoint discards result.Exception and deployed logs contain no native cause. Diagnose/fix the actual intermittent fault, then explicit read-only continuation of original cohort; never repeat preparation/saves or mint another owner. Original native-read continuation already consumed.

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

## Read-only CQL and actual native console milestone

32new controlled tests pass/0failures/0skips for complete/reordered3/2sets, point/window, unknown keys without I/O, cancellation/inactive/budget errors, missing/duplicate/unexpected/tampered events (including empty output page), unsupported queries, no saves/deletes, exact configuration and fragmented socket/EOSE/frame/message/aggregate/event/UTF8 bounds. Uses the separately verified signed-event boundary (36tests/all19official vectors); no legacy code changed.

One explicitly journalled actual console qualification reused the original captured IDs on the same relay; no discovery, alternate endpoint or automatic retry. Activation and actual manager/LINQ3full/2filtered passed; earlier503 outcome preserved. Stable candidate/owner/source and native-qualified ACK retained under LocalState/COSMIC/HostedNostrPublicSourceCurrent. Never rerun Qualify-CosmicNostrPublicConsole.ps1 or regenerate owner/catalogue. No source publication, account, keys, Mongo saves, API/hosted/browser claim.

New source NostrPublicEventMetadataSource and guarded NostrPublicRelayRead use bounded event-ID equality/membership, complete signed selection before paging and explicit read-only errors. Metadata preserves event ID/public key/time/kind/content/tags/signature/policy; native signatures prove key-bound integrity, not identity/freshness/relay completeness. [Setup and commented C# guide](../../Devs/NOSTR_PUBLIC_EVENT_CQL_GUIDE.md). Next: Core/API independent read admission/local HTTP+Mongo/privacy/prepared gates, then hosted acceptance/card.

- TestResults/NostrPublicSourceCurrent/cql-controls.trx: 49358bytes, SHA256 `bf624558f4053bf0a9d92661279136e871b83389e8836ddad26e32e790bcb2d2`.
- TestResults/NostrPublicSourceCurrent/cql-controls.log: 540bytes, SHA256 `1102d4b73769ea5a5147a838263045fce2af4929e6242ba63f16e925c62a878f`.
- TestResults/NostrPublicSourceCurrent/cql-provider-build.log: 82bytes, SHA256 `4d8824e4844fe5dc915e0629abdd0729cd92038104c04644c8b43ebe3bb0dac1`.
- TestResults/NostrPublicSourceCurrent/cql-tests-build.log: 82bytes, SHA256 `3fe950a3cea737d0628f0d4c3f06539084ebae680c583442e5691e273fc57f6a`.
- TestResults/NostrPublicSourceCurrent/console-build.log: 83bytes, SHA256 `5670f4f8791fef9688ee8413f2d2c83b347c7a4435877f39763757aef9f5c408`.
- TestResults/NostrPublicSourceCurrent/console-qualification.log: 1712bytes, SHA256 `ab79b5088c335f14764db1bae6b35a987a25810806b99de684c1b4f2a7f5e3c6`.
- Retained source.json: 314bytes, SHA256 `f24add2d4caab44b961903e3ecac140002b2393c4b010b48d192f950c83683fd`.

## API/local HTTP and prepared rollout milestone

Nostr Core1/API2/localHTTP continuation1/publisher9/prepared-planner1/client preparation1 pass. Native3/2, Mongo5/4, foreign-private0/public3; original two version1 saves reused with0additional saves. Exact marked DB removed after consumers stopped, owned Mongo stopped/27029 absent. Original failed test (wrong BlueSky policy/handle assertion) retained; correction compares actual Nostr policy and signed metadata, original identity/owner/operation ACKs preserved.

Lazy CQL_NOSTR_PUBLIC_SOURCE_JSON registration, explicit query-only compiler/tariff admission, four public/Mongo presets and encryption query-key guard are implemented. Coretask84a2151 pushed/ref-verified; parent gitlink/manifest matched. Protected baseline/candidate under LocalState/COSMIC/HostedSourceRolloutCurrent/nostr-public-*. Exactly3values differ, canonical DNA/unrelated values unchanged; no settings applied. Source-bound web4-local-qualified ACK durable; client test-only branch2a92a34 pushed (main unchanged). Actual prepared export plans against shipping native/Mongo capabilities without network/activation. Guide HTTP JSON matches candidate exactly. Next publish verified code, then guarded settings/live acceptance/card.

Original HTTP fixture: LocalState/COSMIC/NostrMongoHttpCurrent, original failed TRX retained plus explicit read-continuation.intent.json binding source/fixture/failure hashes. No original saves/operations replayed; no replacement actor/source owner. Local SDK fixture clones native metadata; generic browser preparation still copies only identity/name/description/provenance. Production auth/billing/replication/browser gates remain separate.

- TestResults/NostrPublicSourceCurrent/core-admission.trx: 3294bytes, SHA256 `9c0b13fd23e8af242c6943ad3a86723b67cc975bdebf9dd49aa66f1267782739`.
- TestResults/NostrPublicSourceCurrent/api-admission.trx: 4962bytes, SHA256 `8f58bb356b38366701ef19ff5bf5e9d98655d2ab07ea99054ef1d1baa5307a79`.
- TestResults/NostrPublicSourceCurrent/mongo-http-native.trx: 7156bytes, SHA256 `9408bc54da2018a14c42f097db3027c6192570f691a769ba8ba721a85aadac5a`.
- TestResults/NostrPublicSourceCurrent/mongo-http-continuation.trx: 3353bytes, SHA256 `5bff7a92244beb16b0fd81d846ef748d83257a4038fd946d2f475d5421d1455a`.
- TestResults/NostrPublicSourceCurrent/nostr-mongo-native-summary.json: 271bytes, SHA256 `bb98770d2b363e05b6542cdba373de47235e7ff01252368c93b61ad5cb3198e1`.
- TestResults/NostrPublicSourceCurrent/prepared-planner.trx: 3329bytes, SHA256 `e4428388ca1cf4b4240bd42cc7863d553a232d89c0205007927fb161b529fdb1`.
- TestResults/NostrPublicSourceCurrent/prepared-public-review.json: 4186bytes, SHA256 `1e03cfe108dc141ac71b7421308598759f8afc31ac664d61a4ef8181b6c90298`.
- TestResults/NostrPublicSourceCurrent/frontend-preparation-test.log: 217bytes, SHA256 `9bc7cf1e9cdc801c88e58b16e4deea882eb681298f768370b1caaae444c65ca8`.

## Exact Development publication

Development parent `6ee3ebfcd95f594844f518b95451fb9807f342a5` / Core `84a215184f6c2d8b988d5d45bf133a7a0672a26e` ordinary fast-forward published/ref-verified; dependency manifest validates. Exact code job `79b7be91-ac39-4814-be4f-faa1b75613de` SUCCESS. No Nostr hosted settings applied/cohort started. Publication ACK LocalState/COSMIC/HostedNostrReleaseCurrent/code-publication.acknowledged.json; raw public observation TestResults/NostrPublicSourceCurrent/code-deployment.json. No production/master promotion, new hosting spend or active Nostr setting.

Pending card/pinned setup/C#/HTTP links prepared and pushed on frontend task2ffca0642b1a23f7170f41d7bc9e5d1a27e5f0c6 only. Mainac0ca0c remains unchanged. Static card/links parsed offline; no hosted/browser claim or public publication.

## Settings applied once; exact rollout

Code job79b7be91-ac39-4814-be4f-faa1b75613de SUCCESS at6ee3ebfcd95f594844f518b95451fb9807f342a5/Core84a2151. Three Nostr settings applied once and per-key ACKed; whole candidate equality verified. One journalled settings deployment21ee8d23-edce-4723-82ac-1839cd17c975 BUILDING. No hosted cohort started. Temporarily checked out exact code for guarded apply, restored task branch. Per-key intents/ACKs and exclusive nostr-public-redeploy.intent.json/protected .outcome.dpapi retained under LocalState/COSMIC/HostedSourceRolloutCurrent; public settings ACK under HostedNostrReleaseCurrent. No hosted/browser claim. After settings SUCCESS, journal at most2temporary visitors/2normal private saves before original hosted acceptance.

## Hosted partial proof and unresolved repeat read

Nostr code79b7be91/settings21ee8d23 SUCCESS at6ee3ebfcd/Core84a2151. Original owner native3/2 and Mongo5/4 pass; two normal private version1 saves ACKed, pending0. Independent read-only audit confirms matching Mongo/SQLite/Neo4j2records each with exact owner/version/metadata;0additional saves. Native read intermittently fails503/CQL_QUERY_FAILED during repeat preparation; foreign privacy/reuse/full hosted acceptance remain red. No replacement owner, source switch or resave; card task-only2ffca06/mainac0ca0c unchanged.

First native-only API503response was not retained by the harness; this missing evidence is explicitly recorded in native-failure.observed.json. No claim that its exact native cause is established. Deployed Node24.19.0 read-only SSH probe returned3EVENT+EOSE on same relay/original IDs. One explicit same-owner native-only continuation then passed native3/2, prepared2normal private copies and passed federation5/4. The repeat preparation's next native read failed503/CQL_QUERY_FAILED (operation32b7b40a-6014-014f-979f-4fb9065c5e6e); no pending save intent and no uncertain private write. Full hosted acceptance remains incomplete.

Independent read-only Mongo/SQLite/Neo4j audit confirms2matching version1 private records each, original owner and all compared metadata, without additional saves. Durable partial-replicas.observed.json records incomplete status. Preserve progress/save ACKs/native proof; no automatic rerun/continuation. Next diagnose actual .NET failure with sanitized operator evidence; no relay fallback/timeout increase.

Harness root fix: query-only raw status/body/operation captured before interpretation; no auth body/outside-TestResults/overwrite,4offline tests pass including HTTP503capture before raise/no retry. Original failure stays marked missing, not fabricated. --resume-unstarted-native requires same owner/proven no-save failure and exclusive intent; now consumed and ineligible after saves.

- TestResults/HostedCqlIntegrationCurrent/NostrOASIS-native-http.json: 16874bytes, SHA256 `34f19c88ae6a2459e6a25a4e63f80040337d6e0b43aa53526a53103b6f67ae4e`.
- TestResults/HostedCqlIntegrationCurrent/NostrOASIS-client-http-flow.json.progress.json: 35505bytes, SHA256 `fc5a48c2be7d1f852be75bc39c9d414061225475cb599ac7a8d02452fe188ae9`.
- TestResults/HostedCqlIntegrationCurrent/NostrOASIS-partial-replica-summary.json: 534bytes, SHA256 `61111c1594071eceffc8a69749053a08bf5d7283e4fdac66bcbaa0e3ca4142c5`.
- TestResults/NostrPublicSourceCurrent/hosted-egress.json: 164bytes, SHA256 `e349b9df3129b41bf60819313362c046a2d3529c19ddef2bb310c6bef2a83fa5`.
- TestResults/NostrPublicSourceCurrent/hosted-failure-log-summary.json: 367bytes, SHA256 `a80dfdf612b2729de3298a562336d9250a378b93db9b30f9624f0d769dfa0f75`.

## Safe failure diagnostics implemented; live cause still pending

Safe native failure diagnostics implemented:2fake handshake tests +4API cause/privacy tests pass/0skips; producer/consumer builds pass. Failed handshake retains typed HTTP status/original cause, activation wrapper preserves cause, leaf query/activation operators log only known category/stage/status/WebSocket enum (event4831), no raw exception/messages/URLs/tokens/headers. No retry, deadline, source-selection or public error-body change. Actual read failure remains unexplained; this is instrumentation, not a reliability fix. Original3/2,5/4 and2replicas/store proofs/two version1 saves remain frozen. Next exact code rollout, then bounded same-owner native diagnostic to obtain actual cause; do not rerun completed setup or writes.

- TestResults/NostrPublicSourceCurrent/handshake-diagnostics.trx: 4235bytes, SHA256 `b6eac67b5bb124a00a33b235986583bc83f67f0d18e9d64db0ad3c19591e18b2`.
- TestResults/NostrPublicSourceCurrent/api-failure-diagnostics.trx: 8311bytes, SHA256 `8da4c67fdfff6092d6b28d62b146bee1cec719cc2469e4719b5da0c90a62b688`.

## Exact diagnostic release rollout

Diagnostic release210068635b39201f7ec4fff64770c367e5caa76d published/ref-verified on Development, Core84a2151 unchanged, manifest validates. Exact jobcbdad505-a272-494b-8b55-60d2ed62870e BUILDING. No settings redeploy/new saves. Public ACK: LocalState/COSMIC/HostedNostrReleaseCurrent/diagnostics-code-publication.acknowledged.json. Observation: TestResults/NostrPublicSourceCurrent/diagnostics-deployment.json. Wait for exact success before bounded diagnostic; original owner/progress/copies remain frozen. No reliability fix or full hosted acceptance claimed.

Bounded runner ready: Scripts/Sandbox/Diagnose-HostedNostrReads.py requires exact successful diagnostic release, unchanged qualified source/owner, original3/2+5/4 progress/two save ACKs/no pending saves. One exclusive intent, maximum8native read-only calls, stop at first failure, raw outcomes captured before interpretation. Only whitelisted operator fields leave raw log memory.3pure controls pass for selection/bounds/redaction; no live diagnostic dispatched while code BUILDING. Never run preparation/resaves or replace actor.
