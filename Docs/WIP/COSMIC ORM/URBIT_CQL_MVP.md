# Urbit current-protocol CQL preparation

8 November 2026. Goal incomplete; priority-chain rollout remains first.

Current gate: reader/private virtual CQL/shared V2 manager pass 65 controlled
cases (42518331256). Actual Kelvin408/current contacts, owned native book,
authenticated HTTP SDK, private CQL and shared V2 manager now pass full3/subset2
before and after same-pier restart in
[42519994312](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42519994312)
at frozen d628fcc62. Foreign actors get0; anonymous HTTP403, unchanged projected
source/private-CQL hashes and exact owned cleanup are verified. Seven pure
namespace boundary controls pass locally; actual namespace transport passes
42519747804. WEB4 API admission, federation, live deployment and browser remain
unproved; no provider-count increase or accepted live-cohort replay.

Private WEB4 registration/configuration/compiler controls pass30/30 atd4edadf7f.
Shared admitted/MVC/federation component suite passes7/7 at7dd4c5187
(run42526697897); Mongo responses remain controlled in those federation cases.
Actual native WEB4 MVC now passes before/after same-pier restart at6981acb6b
(run42525613719): real source/SDK/private policy/adapter/manager/controller,
owner3/2, all contact/overlay fields, receipts, anonymous401, forged-field400,
foreign403 and inactive503. API projection hashes match and exact cleanup passes.
Native identity validation/outbox are controlled; production JWT/durable billing,
public HTTPS profile hosting/registration and live/browser remain unproved.

Next: actual Mongo-backed native federation with owned copies and source
preservation, then durable hosting/demo authorization, exact image qualification
and live/browser acceptance. No public card/count increase. Query-only source
and server-owned owner/connection/CID policy remain mandatory.

The dated sections below preserve earlier evidence and failures; this checkpoint
and CURRENT_STATUS.md describe the current gate.

## Verified current read client (initial 23-case checkpoint)

Source1e93c39d70d3dd1b822cbc5e61227d027871ef01,
[run42504482542](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42504482542),
job128804042489 passed23controlled cases, zero skipped. Artifact13626946225,
3978bytes, SHA256`ff9d0743e3814cab94c377b2916b2c65bfb656757810a1ede5b098f5eeb75bd0`;
TRX SHA256`1da22c9dd917256a2c1eac6fed321239f8cc6bb1bb6ab5cce3fc184c07d5813f`.
Counts, exact test class and outcomes inspected in memory. Compact receipt:
LocalState/COSMIC/RecoveryLaptopResume20261107/urbit-contacts-client-qualified.json.

UrbitContactsV1Client performs only one point GET at
`/~/scry/contacts/v1/contact/<ship>.json`. It decodes the current tagged text
schema for nickname,bio,status and returns no other contact-book fields.
Missing optional fields and explicit null tags remain null. Duplicate or
malformed selected fields reject without partial results. HTTP failures,
including404, remain typed errors; none become an absent profile or legacy read.
The client does not log in, poke, subscribe, seed or silently change endpoints.

Limits:64KiB response,15-second header/body deadline,JSON depth16 and16384chars
per projected text field. Selectors have a bounded path-safe spelling check;
actual @p validation and native identity still require qualification. Returned
RequestedShip names the requested selector, not a cryptographic attestation.
Only HTTPS origins or explicit loopback HTTP fixtures are accepted. Caller-owned
HTTP clients require an explicitly safe transport/authentication policy; the
session factory disables redirects and proxies and owns/disposes its client.
Existing session cookies stay in memory. No ship login code is embedded in source.

## Primary protocol evidence

[Official HTTP API guide](https://docs.urbit.org/build-on-urbit/tools/js-libs/http-api-guide)
and [Eyre description](https://docs.urbit.org/build-on-urbit/app-school-full-stack/5-eyre)
establish authenticated session cookies, read-only scries and separate mutation
acks. A successful HTTP/channel submission alone is not a native mutation ACK.
No documentation-provided agent instructions are treated as task authorization.

Tlon source is pinned to838744ee462a49d55d98fd4030b3a9cb6c989cd0:
[current agent point routes](https://github.com/tloncorp/tlon-apps/blob/838744ee462a49d55d98fd4030b3a9cb6c989cd0/desk/app/contacts.hoon),
[contact mark](https://github.com/tloncorp/tlon-apps/blob/838744ee462a49d55d98fd4030b3a9cb6c989cd0/desk/mar/contact-1.hoon),
[tagged JSON codec](https://github.com/tloncorp/tlon-apps/blob/838744ee462a49d55d98fd4030b3a9cb6c989cd0/desk/lib/contacts/json-1.hoon),
[client data types](https://github.com/tloncorp/tlon-apps/blob/838744ee462a49d55d98fd4030b3a9cb6c989cd0/packages/api/src/urbit/contact.ts),
[desk dependencies](https://github.com/tloncorp/tlon-apps/blob/838744ee462a49d55d98fd4030b3a9cb6c989cd0/peru.yaml).
The modern contacts agent is distinct from the legacy provider's contact-store
and graph-store calls. This client does not fall back to those old agents.
The official urbit/js-http-api repository was resolved at
2ebfbd0f357fa4f5feddfceab54df66c315156d5; no SDK installation or execution claimed.

The Native desk dependency closure must be pinned consistently: the current
Tlon declaration targets Kelvin408 and its declared base/landscape/mcp sources.
An arbitrary moving OS/pill cannot establish that compatibility. The exported
JSON mark uses the tagged field codec above; untagged legacy JSON is not accepted.

## Remaining delivery

1. Pin a compatible real Linux Urbit runtime/pill and current contacts desk in
   the existing cloud fixture pattern. Bound footprint and verify owned cleanup;
   no laptop installation or unbounded node/state snapshot. A fake development
   ship is a real native test environment, not a public-network acceptance claim.
2. Authenticate an explicitly owned synthetic source, verify actual point reads,
   session/error behavior, source preservation and restart with retained intents
   and ACKs. Never use documentation/mock profiles as native records.
3. Prove a three-record full/two-record subset catalogue, stable mapping and
   tenant isolation before implementing/admitting the CQL source. Current point
   profiles and local book pages have different native routes/JSON shapes;
   choose an explicit reviewed record contract, never a response-driven fallback.
4. Then shared manager/HTTP/Mongo, ordinary private copies, repeat/isolation/
   replicas, explicit priced WEB4 admission and live/browser verification.

Legacy storage/profile writes, general graph migration, full interfaces and
EF/HyperDrive parity remain separate. No live service, reader key, native
runtime, source database or private contact book was accessed in this milestone.

## Source-trigger correction verified

Workflow source path now names UrbitContactsV1Client.cs explicitly. Source
c61a5db0b08d1c3533717aa37151874180958e67/run42505144648 passed the same23
controls/no skips, and no C# reader source changed from the first qualification.
Artifact13627470788/3995bytes SHA256
08da329d31287992ecd584012cad092234e6a01af0f7d2f6d5eebb75b223be69;
TRX SHA2561c6039f939f23b2ad70df1ae45bfa500a2d56e5d8e6a962fe1ce6c7e9755e653.
Artifact inspected in memory and compact workflow receipt retained. This is
23distinct cases, not46independent capabilities or native execution.

The10hash-verified primary-source cache files (69693bytes) were removed after
contract extraction; exact commit/path/size/SHA manifest and lifecycle/source
references remain. No credentials/native data, runtime or original cohort
was removed. No model/image/SDK installed or local build performed.


## Offline runtime preparation — 8 November 2026

Pinned Vere v4.6 archive SHA256 f332e96d66cb767f847fbf15e04b4f953fd078e610b449fbbc26629ea312a3ae (9729797 bytes) and the separately documented source-repository development solid.pill at urbit/urbit 408k-2, commit 5a187fededc4582a34fcd6055c67bb63e0917b94, SHA256 b6e385508c5432fd84b1daf7f110b82c08973e43ab0da283e4580e23c3a7fb33 (9282268 bytes). Previously denied bootstrap-service URLs were not retried or mirrored.

Cloud run42506910117 failed before native execution because archive selection assumed a filename. Preserved artifact13627771744 (796 bytes), SHA256 f1d7007c5854b6939d3825cd682707c690136602d09c7d22732ca2b225db09a6, in-memory inspected with compact local failure receipt. Source57e5c457757f8d888c045045c6e3ded88df46b82/run42507284986 advanced past unique ELF selection and native version validation, but Git LFS fetch timed out after180 seconds. No offline boot, contacts, CQL or live pass is claimed. Its subprocess timeout left helper processes alive during checkout cleanup; corrected to terminate/reap the entire owned Linux process group before cleanup, with bounded failure diagnostics. Syntax checked locally; shutdown correction not yet cloud-qualified. Native workflow is now manual-only pending fetch diagnosis, preventing unrequested repeated source downloads. Do not replay the same timed-out fetch without identifying its cause.

Next: inspect the second run artifact/cache outcome, diagnose the pinned public Git LFS fetch, then qualify actual offline boot and cleanup. Current contacts desk dependency closure and real contacts reads remain separate unqualified gates. No Development deployment or laptop runtime installation.

The second failed artifact13627734175 contains only intent.json and runtime-version.txt; cleanup failure prevented qualification.json, so no cleanup claim is valid. Git LFS upstream command_fetch.go confirms ref-level fetch scans the tree before transfer. Replaced that oversized discovery operation with documented git-lfs-smudge of the already commit/OID/size-verified single pointer (https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-smudge.adoc). This is the same repository LFS object and server, with no endpoint fallback, retry or alternate boot image. Exact downloaded stdout and cached LFS object both require pinned size/SHA. Process-group shutdown remains mandatory. Timeout cause beyond the unnecessary tree scan is not yet proven; cloud execution is the next gate.


## Offline native boot accepted — 8 November 2026

Source61c28aa5be12ae1c4f7a9f953a11a37eab2a7e77, [run42507813675](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42507813675), job128814161160 completed successfully. Artifact13628570452,1998bytes, SHA256 bcd8f79d968e31e4e35655d32731585681c684d86bc53dc8384c4b3ccd3df129 inspected in memory. Exact source/flags, exit0, no OOM, network none, owned container/volume removal and temporary source checkout removal verified. Native log explicitly records bootstrap complete and pier(23) live, followed by successful exit. Archive member is vere-v4.6-linux-x86_64,24044936bytes, executable SHA256 47ad302e8934271dfc00dcdab1d553c6d6460b105e3099ff47e0e4e4231bb26d. Both boot-image output and LFS object passed exact pinned size/SHA. Cloud cache43057001bytes; no laptop binary/pill installation. Compact artifact/member hashes retained in urbit-offline-runtime-qualified.json.

The single-pointer operation completed; ref-wide discovery timeout was avoided by removing unnecessary discovery, not extending timeouts or changing servers. Earlier failed evidence remains preserved. The successful path verifies ordinary cleanup; forced timeout/process-group behavior is not independently fault-injected proof.

Remaining step1 is now the current contacts desk dependency closure and compatible install, not another runtime boot replay. The boot contains base agents, not a qualified modern contacts desk. Real synthetic contact reads/restart, reviewed three/two-record mapping, manager/HTTP/Mongo and live/browser gates remain open. No provider coverage/live count increment and no Development deployment.


## Current contacts installation inputs pinned

Scripts/TestHosts/urbit-contacts-inputs.json records the reviewed upstream assembly contract: dependencies first, application source last; Kelvin408; desk groups; contacts agent. Exact upstream peru.yaml SHA25627572c42c542d55a05f46269a1b39dfe2b021203f4c208d7fd448957b24f7091 rechecked before extracting its declared picks. Base408k-rc2 resolves192187b2975c6a06840c9630fae9394aac09a8e8 (20files), landscape5193635 resolves5193635c45a346b3030280e76ee32d6fa290bc98 (4files), MCP03630c712a0a2f3f23a39e710e1b92b8b8da077d (2files). All26regular-file paths and Git blob identities verified against exact, nontruncated source trees; total86984bytes, no dependency target collisions. App source pinned838744ee462a49d55d98fd4030b3a9cb6c989cd0 contains669desk files/4555031bytes. One nonregular documentation symlink desk/CLAUDE.md (9bytes) explicitly recorded; no extraction/implicit traversal allowed before reviewing its target.

This pins installation inputs, not a completed assembly/install or native contact read. Upstream full bill includes23agents and its CI normally starts from a groups-bearing pill; our accepted solid.pill contains base only. Must use the documented from-scratch desk installation, verify actual dependency compilation and contacts initialization, and preserve real errors. Do not pretend a successful base boot installed contacts. Next engineering task: bounded cloud-only source assembly, exact symlink handling and native install using the reusable pinned runtime assets; no frontend/package/runtime installation on laptop.


## Current contacts source assembly accepted

Sourcee1053eef3f6eb465f79f767b20bd0c4aa0af5d59, [run42508500262](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42508500262), job128816251661 passed cloud source assembly. Artifact13628616882/33200bytes, SHA2560b18a49bce3b064b178c765f4a5520a35cc78d7ff0fde87c426fd82137183036 inspected in memory. Exactly695assembled files/4657933bytes; manifest SHA25654eff9a62fc2337d472767cd61e5ccd0d1743bba243b0da614d016916a86d464. Verified26dependency blob identities, all669upstream desk entries against their Git objects/count/bytes, exact declaration digest, Kelvin408, contacts bill/agent and application-over-dependencies order. The only symlink was verified as the exact in-desk AGENTS.md link and materialized to match upstream rsync-L semantics; no repository instructions executed. Source commit provenance stamped. Four temporary source checkouts removed; assembled desk and file manifest retained in cloud-only cache keyed by helper+inputs hashes. Compact receipt urbit-desk-assembly-qualified.json retained locally, no source ZIP extracted.

Initial manual dispatch returned404 because the new workflow was not registered; a task-source push registered it and launched the single accepted run. Automatic trigger now removed to preserve accepted assembly; manual-only. No frontend/npm/peru runtime installation. Assembly is true, installed/nativeContactsQualified/cqlQualified/liveDeployment all false. Next: restore exact verified desk/runtime caches, qualify from-scratch native groups installation (including required landscape dependency), actual contacts reads and restart. Do not relabel source assembly as native compilation or CQL acceptance.


## Native control/install qualification in progress

Added bounded conn.c/newt client using pinned Vere native jam/cue, Unix socket only,1MiB frames, one end-to-end deadline and exact thread acknowledgements. Seven framing controls pass locally and in cloud42509161255 (no native SDK/runtime run on laptop). Source595f0e413/run42509048636 stopped before starting any ship: the immutable original runtime cache had been saved before the pill was available. Added explicit assets-only preparation (returns before Docker), run42509161255/source4fd27f761 verified both native/pill hashes and saved complete43057001byte cloud cache under urbit-linux-complete hash key; accepted boot-only proof not replayed. Then native attempt stopped on bind directory permissions before any thread acknowledgement. Artifact13627844264/2248bytes SHA2561732ec9e44a0f5d8c3e92779f272f54e67f44060b49901724daad9746f21dd1c inspected in memory; no OOM/network none; owned container and pier removed. Compact receipt urbit-native-install-first-failure.json retained. Native control/source commit/contacts flags all false.

Sourceaee605aef matches container UID:GID to the host-owned disposable bind directory, retaining cap-drop ALL/read-only root/network none. It restores the already saved complete asset cache directly (no repeated source/pill preparation). Follow run42509342247; no duplicate dispatch. The qualifier uses full assembled upstream groups bill/source, a synthetic owned desk created from base via documented kiln merge-init, native commit/install acknowledgements plus explicit source/agent readiness checks. No three-record contacts, HTTP/CQL/live pass is claimed. Native install may reveal additional from-scratch application prerequisites; retain real errors rather than trim agents or substitute the legacy contacts store.


## Native control accepted; OS compatibility gate identified

Runaee605aef/42509342247 artifact13628782909/1585bytes SHA256979de058d3ae21077b4edddaccb132e5db75a73adb98912453c717f49c86514d inspected: native returned successful [1 %avow 0 %noun 27503], but checker expected textual %ok.27503 is the exact little-endian noun atom for ok, so source0789be55d changes the test thread to return explicit numeric0 with one exact expected representation, not dual-format parsing.

Run42509564721/job128819455148/source0789be55d93cfce812e6c0afa64416135a24cf23 passed actual native control, create-groups, mount-groups and commit acknowledgement operations. Artifact13628444378/2600bytes SHA256f55785266cb031c365e2bde89fee422ea284851025869721d46ce533d1924bd2 inspected in memory. source/contacts acceptance remained false: native log reports wait-for-kelvin need409/have408; base-only repository solid.pill boots Kelvin409, despite kernel release source itself being408. Source verification correctly stopped acceptance. Owned container/pier removal, exit0/no OOM/network none verified. Compact receipts retained; no CQL/live count increase.

The compatibility invariant requires native base408 before installing current contacts408. Do not edit the application marker or substitute legacy contacts. Pinned kernel source5a187fededc4582a34fcd6055c67bb63e0917b94 has pkg/arvo/sys.kelvin [%zuse408]. Metadata inventory:539arvo entries/3753411rawbytes/151symlinks; complete pkg tree1431entries/9101886bytes. New cloud source assembler checks all package Git blobs before materializing only arvo's exact in-package resolved targets, records every output digest, and removes the temporary checkout. Source505852ba9/run42510963618 is in progress; no native OS update yet. Next: inspect assembly output, restore that exact source cache, perform native mounted-base update, verify running zuse408 and only then resume current contacts installation.


## Kelvin408 source assembly accepted

Initial run42510963618 correctly rejected upstream directory aliases under its file-only contract; artifact13629631396/638bytes SHA2566be323c180f5d766ddcd79a2be535c49306ac0744a486a897eedde46ddd8a202 inspected, temporary checkout removal verified, no native update. Exactly four aliases were identified in the already-pinned Git tree: lib/verb, lib/wasm, mar/verb, sur/wasm. Updated bounded materializer follows those directories only within verified pkg source, checks every resulting regular-file Git identity, rejects destination collisions and caps recursion/files/bytes.

Sourceefcffd51b74753423082b1374e75cea10b6353ae/[run42511142535](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42511142535), job128824267388 passed. Artifact13629950581/41219bytes SHA25653eb6d143fb24d2f16633c9eacff6a91106a4148b8f23aef39b1b3bbbb4a0a40 inspected in memory.546materialized files/4307737bytes,151source symlinks, exact four directory aliases; output manifest SHA25609f7f0e3d6798499b5fb87f3037bb8b6f4ee0dd52badef5ea26f8392b306f7fe; temporary checkout removed. Reusable kernel408 cache retained only in cloud. Workflow frozen manual-only after accepted assembly. Native kernelUpdated, contactsQualified, CQL and live flags remain false. Next: restore exact output manifest/cache and update the owned native base before creating/installing groups; verify actual running zuse408, not merely the source marker.


## Native base update orchestration / actual-version check

Sourced910fea3e/run42511356066 restored the three accepted exact caches, verified every output file digest, mounted/replaced only its owned synthetic base and requested native commit before groups creation. A preflight source read of pinned sys/zuse.hoon then proved ++zuse is numeric408, not [%zuse408]. Its tuple readiness predicate was deterministically false, so the confirmed running owned job was explicitly cancelled to avoid an unnecessary600-second wait; it was not restarted because of an observation timeout. Cancellation is terminal. Artifact13630100323/343bytes exists, but native cleanup/update acceptance is not yet established from that cancelled run; do not claim it. Existing before-dispatch intent identifies its exact owned resources. Hosted runner was ephemeral; no live Development service changed.

Source0029c9101 now reads the actual native zuse atom directly. Only successful exact408/409 replies are readiness observations; all native errors reject, with no format guessing or replayed mutations.408 is required before groups creation. Follow run42511441973, currently in progress. Native source/contacts/CQL/live flags remain unqualified until retained output proves them. No new laptop installation or Development push.


## Explicit book-page reader — 43controlled cases accepted

Sourcec0746d52022d0ec152918000ec3b8158cf1e5606/[run42511819014](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42511819014), job128826320748 passed43distinct controls/no skips: existing23profile cases plus20page cases. Artifact13629438625/7529bytes SHA2564fe8d07522bb24d4717ca5bb061186f01a19258ee37b6439ef6346b78ee57452; TRX SHA256e0256c214784bbe611c7bb218b31446f4b1ed5968cfb391d7027ad37eee2bac7. Exact class, counters and every result inspected in memory; compact receipt retained.

ReadBookPageAsync selects only /~/scry/contacts/v1/book/id/<CID>.json. Current upstream contacts/sur and tagged codec define a page as exactly [contact,overlay]. Reader preserves those as separate reviewed nickname/bio/status objects; it neither merges fields nor treats a page as a ship profile. CID spelling/path safety is bounded; native CID type/existence remains authoritative. Shared transport has one64KiB/15second path, typed HTTP errors, no404-absence conversion, no profile/legacy fallback, cancellation and caller ownership intact. Proposed native cohort uses three explicitly owned local book pages on one ship rather than three fake peers. Actual seed/read/restart and CQL mapping still unqualified; no count increase or live enrollment.

Native OS update run42511441973/source0029c9101 passed control/mount/commit acknowledgements, compiled new Hoon/Arvo and migrated vanes, but its queued version observation hit an incorrectly narrower120-second timeout during base rebuild. It then stopped under cleanup (exit137,OOMfalse), owned container/pier removal verified. Artifact13629068646/2455bytes SHA256d82dca1f88fe0963ea06b41d5549634b8484cf67972c99e8e3790faddb54219c inspected; actual running408 was not proved. Source614d59864 applies the remaining existing600-second upgrade deadline to the queued control observation, with no replayed update mutation and no expanded overall budget. Successful lifecycle additionally requires exit0/no OOM/network none after cleanup. Follow run42511918338, still active at this checkpoint; no native acceptance yet.


## Planned private native book cohort (not seeded)

Tracked UnitTests/Fixtures/urbit-contacts-book-source.json fixes three synthetic local page IDs0v1/0v2/0v3 and the two-ID subset0v1/0v3, with one durable source owner and pinned current application commit. All seed/read/restart/CQL/live flags explicitly false. This is desired input, not a before-dispatch journal, native ACK or source completion proof. Before native mutation, retain a separate run-specific ownership intent and capture each actual native acknowledgement; never seed again on an observation failure. Existing native runtime/control proofs do not mean this cohort exists.

The page contract keeps peer contact and user overlay separate; native book overlays are private. Do not enroll this source via BoundedPublicKeyValueQuery or classify it as public native data. CQL mapping must preserve ownership/privacy and prove another actor receives no private records; the synthetic three/two fixture can be taught only under explicit scoped visibility rules. Projection of nickname/bio/status does not grant publication rights. Query/read source must not acquire legacy contact-store writes or alter the current source during CQL evaluation.


## Native OS/current contacts initialization accepted

Source614d5986406f37639f419df72a956a61bbfcdce5, [run42511918338](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42511918338), job128826625694 passed. Artifact13629794881/3888bytes SHA256034721107992db400dca61d60e8eb12f328c83f4df5a713fa6e95edc159819ff inspected in memory. Exact numeric runtime response [1 %avow 0 %noun408] verifies actual running Kelvin408, not only a source marker. Native control, mounted base update, groups creation/mount/commit, source existence verification, full upstream groups-bill install and contacts-agent readiness all passed. Native logs explicitly install contacts. Exit0/no OOM/network none; exact owned container and pier removed. Compact receipt urbit-native-current-install-qualified.json includes all output member hashes. Workflow now manual-only to avoid replaying this accepted standalone gate.

No cohort was seeded and nativeContactsQualified/cqlQualified/liveDeployment remain false. The full upstream bill was preserved; native logs also report missing/not-running settings and a notes desk message because Landscape has not been installed. Do not claim full Tlon application health. Native next gate is actual owned contacts page seed/read/restart, including any required Landscape dependency if the real operation exposes it. The synthetic source fixture is still desired input only. Preserve privacy and explicit ownership; no public-native enrollment or CQL mapping acceptance from initialization alone. Accepted installation must not be rerun as an independent progress check; subsequent native runs must add actual required seed/read/lifecycle evidence.


## Native book cohort/restart qualification started

Source6e1a22a1c/run42513709558 adds actual required work to the accepted installation path: verify native book empty, write exactly the three fixed private synthetic local pages, retain per-page before-dispatch intents and separate native ACKs, verify exact contact-empty/overlay-field equality for full3/subset2, cleanly stop and restart the same owned pier, then recheck full3/subset2 without resaving. This is one owned source; restart recreates only its labeled container while preserving its exact data directory, using existing-pier runtime arguments. Runtime/OS/application source caches reused. Original accepted standalone install remains manual-only and is not dispatched again.

The native callback uses current contact-action-1 and current book/id routes, with fixed path-safe CIDs and restricted synthetic text; no legacy store, login code or real private book. nativeBookAgentQualified/nativeBookRestartVerified are separate from nativeContactsQualified (the .NET HTTP client), CQL and live flags. All remain unverified until the new artifact is inspected. Before-dispatch fixture intent is separate from desired input; a failed observation cannot authorize another seed. Final native success requires exact owned cleanup and exit0/no OOM/network none. Next after this gate: actual authenticated HTTP reads with the .NET client and its typed errors, then private-owner CQL mapping/isolation; no public-reader enrollment for private overlays.


## Real HTTP client host prepared

Scripts/TestHosts/UrbitContactsHttpHost references the actual provider reader. It accepts only an owned127.0.0.1HTTP origin and the hash-pinned synthetic fixture, takes the fake-ship login code through piped in-memory JSON, requires explicit anonymous401/403 denial, performs actual Eyre login/session handling, then uses UrbitContactsV1Client.FromSession for full3/subset2 point reads. Both projected contact and overlay values must exactly match the fixture; stdout contains only phase/counts/denial status and projected-source hash, never input/login code/cookies. Before/after phases can compare that hash across the same native pier restart. No CQL/private-data publication claim.

Initial host source40b9273a2/run42514062260 compiled successfully with manifest-pinned Core/libraries under the provider build lease. Strict auth-denial assertion source6c0e2cc74 is under build42514186079; this is compilation only, no native HTTP request made. Native book lifecycle42513709558 remains active and must finish before claiming seed/read/restart. The HTTP host is not yet connected to a loopback-published, isolated native fixture; that exact transport/auth integration is the next gate after the book result. No test-double response or node-side read can count as real .NET HTTP execution.


## Strict real HTTP host compilation accepted; transport wired, not run

Strict source6c0e2cc74128261fcfccec531e273c01096ce5ac/run42514186079/job128833485661 completed success. Artifact13630577832/330bytes SHA256588849a114dad9dd4b466b6067b125158c39a164bca039e733a9bc0b11c80796 inspected in memory; compiled host assembly SHA25664f1b027a7f83e54489d1d65799cfcb0f475b9644436ddc894ba3806c80db288. buildQualified=true, nativeHttpClientQualified/CQL/live=false. Requires actual401/403 for anonymous denial; an arbitrary failed native request cannot establish privacy.

Source9393d5f9d wires manual scope native-http in the registered HTTP-host workflow. It builds under the existing bounded provider lease, restores the exact three immutable caches at their original cache paths (explicit validated workspace cache root handles the nested build checkout), and creates a labeled internal-only Docker network with one127.0.0.1published HTTP port. Runtime/contacts-source hashes remain mandatory; no cache-miss downloads or alternate native endpoints. Hook derives the owned fake ship's +code through bounded numeric character replies matching the pinned native generator, keeps it only in memory, and pipes it directly to the real .NET host. Both full3/subset2 phases must pass before/after the same owned pier restart with identical projected-source hashes. Exact container/network/pier cleanup required. Ordinary offline native mode stays explicit network-none; this SDK phase explicitly uses an isolated internal network, not a hidden fallback.

This transport wiring is syntax-checked and pushed, not executed. Native book42513709558 remains active; inspect its terminal artifact first, then dispatch scope native-http only after resolving any actual native source failure. The book workflow is now manual-only to avoid starting another native source job on these preparatory edits. No native HTTP, CQL or live acceptance/count increase from compilation or wiring.


## Fixture serialization invariant fixed before combined native qualification

Book run42513709558/job128832026139/source6e1a22a1c terminated failure before any book operation or seed intent. Artifact13630314582/3884bytes SHA2566d7e82a6bee879fb03af12ec0507e10fd29a5b76f25aa7473ba9dfdc0e9a4c1a inspected in memory. Kelvin408/source/contacts initialization and clean exit0/no OOM/network-none/container+owned-pier removal verified; no page seed/read/restart or HTTP acceptance. The sole failure was the fixture SHA: laptop CRLF1117bytes/hash53c795... versus Git LF1072bytes/hash75ac02.... Byte comparison proved line endings were the only difference; owner/CIDs/values unchanged. No native cohort was written, so there is no source seed to replay or new owner to allocate.

Source019e8931e pins fixture text eol=lf via its scoped .gitattributes and exact tracked SHA25675ac02bb8c0ad875ad227248cfe810441aa9f6550de39031e7381062b17293cb in both Python and C# gates. One fixture loader validates/returns the input before any native startup; callback consumes that value rather than discovering a mismatch after installation. Local pure JSON preflight and Python syntax checks pass; no native data/runtime executed locally. Historical failed digest receipts preserved, not rewritten as successes.

Combined real native book/HTTP scope dispatched once at source019e8931e after prior job was terminal and artifact reviewed. Follow [run42514925237](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42514925237). It must first pass native empty-book/seed/full3/subset2, then real .NET anonymous401/403/session reads and same-pier restart/source hash preservation, with internal-network/container/pier cleanup. Native HTTP remains unqualified until artifact inspection; CQL/live flags stay false. Existing original live cohorts and priority-chain gates unchanged.


## 8 November: private CQL controls and identity ambiguity

Cloud run [42516923950](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42516923950) at d626074bd completed successfully after the authority test captured its catalogue through the supported expression boundary. Artifact inspection remains required before recording detailed acceptance. No live WEB4 or native HTTP acceptance follows from controlled tests.

Pinned contacts source returns the default empty page `[{},{}]` for a missing CID as well as an existing blank page. The reader now preserves that ambiguity through HasNativeFields; the private CQL adapter rejects ambiguous activation and projection with CQL_NATIVE_IDENTITY_AMBIGUOUS. It does not infer absence, create phantom holons, scan the whole book, or mutate native records. Two regression cases raise the expected combined reader/private-CQL count to 64. Blank-page identity support remains incomplete pending an independently verified native existence contract.

Combined native HTTP run42514925237 failed before seeding: Gall scry paths omitted their required output mark. The harness now appends `/noun` to native book and page paths, matching Gall's documented contract. HTTP `.json` paths remain unchanged. That failed run removed its owned container, internal network and pier; it did not establish book, HTTP or CQL acceptance. Next native run also exercises the actual private CQL adapter through authenticated HTTP before and after the same owned pier restart. No new Development push or original cohort replay.


64/64 controlled reader/private-CQL cases accepted at1937289c45cbee8536801ddefe6c139ff440fe93, run42517629662/job128844017030. Artifact13631903212 SHA25668ff24df5756eb72b67b0b1adf10954488fcd02da7b38b1ffccf213ad983af09 inspected in memory: all64 executed/passed, none skipped. Prior62-case result artifact13631293062 SHA25613bfa30a76970a2148dd127f7450b1a0f9639651fcc630dfd40ec8cab01131f0 independently inspected. No local archive extraction.

One combined native HTTP/private-CQL lifecycle dispatched at the same source after verifying prior native operations terminal: [run42517795274](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42517795274). Reuses exact cloud caches, owns one synthetic pier and one internal network, and must verify native full3/subset2, anonymous denial, authenticated actual SDK and provider CQL, foreign actor zero, restart-preserved projections, and owned cleanup. Pending result; no native/private-CQL/live acceptance yet.


## Shared V2 manager controlled gate accepted

Source2558e56bb, run42518331256/job128846134473 passed65/65 reader/private-CQL cases with zero skips. Artifact13632455738 SHA256c506482a1d1f22de1558755be9c679c2571c2f15a51a134645c054cb03d9fba2 verified and TRX inspected wholly in memory. The additional case invokes the real HolonManager.ExecuteQueryAsync V2 path over the actual Urbit adapter and controlled HTTP transport: full3/subset2, provider provenance, private owner, root-only rows and source CID metadata survive typed mapping; foreign actor returns zero with no additional HTTP request. Failover, replication and load balancing disabled explicitly. This proves controlled shared-manager execution only, not native manager, HTTP API, federation, HyperDrive replication or live deployment. Real native HTTP/restart run42517795274 continues at frozen1937289c4; do not restart on an observation timeout or dispatch a duplicate.


Native shared-manager extension prepared after the accepted controlled gate. The real HTTP host now runs the shared V2 HolonManager on the same native session/catalogue for3/2 and foreign0, compares all typed metadata against directly read private rows, and requires provider provenance/owner/root-only projection. The lifecycle hook requires this before and after the same restart. No second fixture/database, failover or replication enabled. Python syntax checks pass; this extension is not compiled/executed yet. Frozen running42517795274 still tests the earlier direct-provider scope, so its artifact cannot certify the later manager extension. Follow original run to terminal and inspect its evidence before dispatching further native work.


## Native book reads accepted before transport boundary failure

Run42517795274/job128844537034 at1937289c4 terminated failure. Artifact13632891336/9167bytes SHA256f34265092cf7cbaebde73ec87a8cbc55452caece8ce36dc139e98bfb5d98a482 inspected in memory. Actual empty-book check, three seed intents/ACKs, full3/subset2 exact native reads all pass. The `/noun` correction is proved. Before HTTP host invocation, http_origin rejected Docker's effective port-binding cardinality. No HTTP request, restart or provider/native-manager CQL acceptance follows. Login-code derivation reached payload construction and was never emitted/stored. Clean exit0/no OOM, owned internal-network/container/pier removal all verified; the three synthetic records were removed with that owned pier. No surviving source to adopt/reseed and no original live cohort was touched.

Root-cause investigation now uses transport-only scope in the registered host workflow: same pinned Node image, internal-only Docker network, requested loopback ephemeral publication and compact requested/effective binding metadata, with owned cleanup; no native boot, auth or source data. Do not guess another origin, weaken loopback validation or switch to public/host networking. Manager host source89ac6195b compile-only run42519121747 completed success; artifact inspection pending. No native manager claim from compilation.


## Docker publication root cause reproduced; namespace transport under qualification

Transport-only run42519373580/job128849289362 at9030a4d52 reproduced the infrastructure contract: Docker29.4.2, internal network, HostConfig.PortBindings requested127.0.0.1/ephemeral80, but NetworkSettings.Ports[80/tcp]=null. Artifact13632363955 SHA256e6b58cae996664b798eedeb0a87bd7346b7ed84113e1ed14bc37a577a5ea846e inspected in memory. Diagnostic docker-port command failed because no effective publication existed; owned container/network removal verified. This is not an HTTP authentication/Urbit/CQL failure. No native ship was started.

Sourceab9351fb8 replaces unsupported publication with one explicit namespace-loopback transport. The client joins only the owned running native container's network namespace after validating label, exact sole network and absence of published ports; it retains the host filesystem for frozen .NET outputs and drops to the runner UID/GID before execution. It uses fixed127.0.0.1:80 inside that namespace. No alternate endpoint/public network or retry/fallback path. Restart reacquires the new owned process identity. The transport-only cloud check42519747804 must prove actual HTTP success as runner plus cleanup before another native lifecycle. Native/CQL/live gates remain unaccepted.

Manager extension compile source89ac6195b passed42519121747. Artifact13632506823 SHA256e6d9602389f172f95e58b47aaa29bc13487f8e288c843dd46516e0ffb75a8f3e inspected; frozen assembly SHA2568bd1e8fc261cf9491b9ebdbd45d046f60d7f5b418f2c190545582d5937a2767f. Compilation does not establish native-manager/API behavior.


Namespace transport accepted atab9351fb8/run42519747804/job128850425697. Artifact13632987214 SHA256f97365a9415ce891902c746b47b00e8aefbae5ac0bdda400c17e370fc484190f inspected in memory: actual loopback HTTP success as runner, no requested/effective published ports, owned container/network cleanup verified. Host compiled with assembly SHA256b24a94f769e2b9f17217dbb23a1c6d025689b4ab4c05ea49ff9384f6998066c1. One native lifecycle atd628fcc62 now follows [42519994312](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42519994312), after verifying no other native/transport operation active. Requires actual native seed/read, anonymous denial/session SDK, direct CQL and shared V2 manager3/2/foreign0, restart-preserved projections and exact cleanup. Pending; no native HTTP/manager/live claim.


## Native SDK/private CQL/shared-manager restart accepted

Run42519994312/job128851185181 atd628fcc62ad401b9dfcdda6448fc4ef99d256f86 completed success. Artifact13632864832/12122bytes SHA25679328082767f9afe196a6f2eb8af8e52604d6313260a707536b758f5a0667544 inspected entirely in memory, including three exact owner-bound seed intents/ACKs. Frozen real host assembly SHA256f770878952d1e8263553b12d730ae65d2794be8b5ff7afff1b9a1b35bac527f3.

Actual Kelvin408/current contacts installation, native book full3/subset2 and same owned pier restart passed. Both phases: real anonymous HTTP403; login cookie/session; actual provider SDK3/2; private CQL3/2/foreign0; shared V2 HolonManager3/2/foreign0; owner/provenance/root-only metadata projections checked. Native projected-source hash567ee3dc83ad01ab384428739ad34a801081af19be09ca1332ffbd78d2129062 and private-CQL projection hasheaa2e14e2b7d66198da671322b5c6837bb6533ee7c6a52fd6f81fe4db0d07956 match across restart. No source copies/imports/failover/replica writes were performed by the readers. Only the explicitly owned synthetic fixture was seeded.

Restart shutdown0, final exit0/no OOM; exact owned internal network/container/pier removal verified. Three synthetic pages removed with owned pier, caches retained cloud-only. No local build/runtime/artifact extraction. Receipt urbit-native-namespace-manager-qualified.json contains compact safe evidence only. Native SDK/provider/shared-manager scope accepted; generic cqlQualified/liveDeployment remain false because actual WEB4 admission/API/federation/live/browser have not run. Empty existing native pages remain ambiguous and fail explicitly. Do not replay accepted native boot/book/SDK checks alone; next work is shared WEB4 admission/policy/HTTP scope. Seven new pure namespace boundary tests pass locally and are wired into subsequent cloud host runs without triggering another native fixture.


## Private WEB4 registration candidate prepared

The existing Startup/CqlReadSourceProfile/CqlV1Compiler path now has an opt-in Urbit query-only registration plus provider project reference. Six server-owned CQL_URBIT_QUERY_ settings are required together: ORIGIN (credential-free public HTTPS), SHIP, SESSION_TOKEN (existing native session; no login/poke), OWNER, CONNECTION_ID and PAGE_IDS (JSON1–32unique native CIDs). All absent means no registration; partial/malformed config fails before registration. Secrets are never included in errors or examples. No actual configuration or live deployment changed.

A guarded DNS/HTTPS transport disables redirects, proxies and cookie mutation. Its single exact-read handler permits only GETs of the configured point paths on the configured authority, attaches the configured session only after those checks and rejects caller-supplied credentials, query strings, full-book reads, login and writes. The source remains inactive until the existing read-profile lifecycle explicitly probes it. Its keyed ICqlReadAccessPolicy grants only the configured owner/Urbit authority the stable logical catalogue. Other authenticated actors receive access rejection before native dispatch. This private integration is not a public sandbox teaching-data grant; live demo authorization/hosting remain separate gates.

CqlUrbitHostedConfigurationTests defines27controlled cases covering registration, invalid configuration, owner/authority rejection, exact-path credential containment and read-profile versus composition admission. New cloud-only qualify-urbit-web4 workflow uses exact pinned submodules/libraries and the bounded provider build lease; no native runtime or data source is requested. Compilation/test acceptance pending. Existing65reader/CQL controls, native SDK/manager/restart and original live cohorts are not replayed.


Initial WEB4 registration source55795aa69 passed27/27 controlled cases in42521643339/job128856216698. Artifact13633344725 SHA256f189d28a05eb5721e6926b2670ffa52fbe351765352da905d94218ef298e5df5 inspected in memory, zero skips. Full API/provider graph compiled cloud-only. Shared compiler/Startup edits triggered eleven older boundary workflows; all eleven were explicitly cancelled and verified terminal, while the scoped Urbit run finished. These were controlled jobs, not native cohort replays. Cancellation receipt retains exact run identities.

Next scoped batch adds an explicit duplicate-provider-policy refusal plus two actual API-compiler-to-adapter-planner cases: unordered point reads admitted; name ordering rejected by the source capabilities. Expected30cases. This task-branch push suppresses redundant automatic push workflows using GitHub's documented commit annotation; exactly one manual qualify-urbit-web4 run at that source is mandatory. No deployment/merge or blanket green-check claim from suppression. Exact dependency validation and leased full API build still run in that workflow; Development integration remains held until scoped evidence and required release checks exist.


Single30-case qualification atd4edadf7f is [42522330369](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42522330369), manual event confirmed; no redundant automatic workflows at that source. Follow this exact handle to terminal and inspect hashed TRX. No native ship/network/auth requests or deployments requested.


## Admitted endpoint component gate prepared

CqlUrbitAdmittedTests extends the existing shared WEB4 endpoint/admission test harness. It references the actual Urbit provider and links the exact shipping private registration/read-profile source (no parallel query engine). Three cases cover owner full3/subset2 through CqlV1Endpoint, real SDK/adapter, actual V2 manager, leaf metadata/ownership projection and one provider-query usage receipt; foreign actor403 and unsupported name-order400 must occur before another native read or manager creation. Only native HTTP replies and usage outbox storage are controlled. This is a component gate, not native WEB4 HTTP, federation, live or browser proof.

The existing Core.UnitTests project is reused with one provider reference and two production-source links. Cloud-only qualify-urbit-web4-admission builds exact manifest-pinned Core/libraries under the same bounded provider lease; no native ship, database, credential or local SDK installation. Trigger overlap inspected before this batch: no older task-branch workflows match these new test/workflow paths. Current30-case full-API registration42522330369 remains running at its frozen source and must not be restarted on an observation timeout.


## Admitted component failure identified at Core tariff enrollment

Component run42522703723/job128859479687 at2218710dd compiled but all3cases failed before endpoint execution: StorageQueryTariff.Capture rejected Urbit as an unsupported query provider. Artifact13634116617 SHA256e8c8caa2c98bb1d9a15254200d68595b517b000a8debb20a180eee1d80f8e693 inspected in memory; original TRX failure retained. No native request/data/API acceptance follows from that run.

Core task branch was fetched/checked equal to its remote, then only Services/Subscriptions/PreparedStorageExecution.cs was changed: add Urbit to the explicit admitted unsigned query tariff set. Coree62ec44cfc7d80640195ae4a6e538908015ea61e pushed on the existing work/cosmic-cql-decentraland branch; unowned Enums/ProviderType.cs preserved. No tariff amount, actor/permission, measurement or canonical-write rule was relaxed. Parent gitlink and OASIS_API_CORE_COMMIT advance together via sync --source index, preserving unrelated dirty STAR/WEB6 heads. This is a task candidate, not a Development deployment or moved immutable image tag.

Next exact-source component qualification must rerun all3admitted cases with this pinned Core. Redundant push workflows are suppressed only for this task commit; manual qualify-urbit-web4-admission remains mandatory and performs the dependency check and leased build. Full-API30-case run42522330369 still refers to its original frozen source and is followed independently.


## WEB4 configuration and admitted endpoint component gates accepted

Registration/compiler run42522330369/job128858331868 atd4edadf7f passed30/30, zero skips. Artifact13633868589 SHA2562bac5248c9090130b374cb31be0e6ab8debdc31117a555e1826036e4ee07c4f6 inspected in memory, exact class and all outcomes verified. It includes duplicate-policy refusal, permitted unordered compiler/planner admission and rejection of unsupported name sorting. Its frozen dependency predates the one-entry Core tariff correction; it does not certify a later deployment image.

Corrected admitted component run42523100057/job128860696806 at ec1f2baf144134cfbd7c1e745f65ac0819e1f2b5 with Coree62ec44 passed3/3, zero skips. Artifact13634475851 SHA256e457a7cd9ffdef9f2fdf29ff1115205e91f1bbcc57c7925477420521619e1d1f inspected in memory. Same shipping CqlV1Endpoint + private keyed policy + API compiler + actual SDK/adapter + V2 HolonManager and leaf projection: owner full3/subset2 private/provenance/metadata and one measured query receipt per execution; foreign403 and unsupported sort400 before further native reads or manager creation, zero execution receipts. Controlled native responses and usage outbox only; no native WEB4 HTTP/federation/live/browser claim. Original0/3failure preserved, invariant fixed in Core rather than bypassing admission. Manifest/seven-Dockerfile/gitlink validation at ec1f2baf1 passes. Both existing task branches pushed; no Development or production rollout.


## Real MVC HTTP wire gate prepared (controlled native responses)

The admitted suite now adds one real loopback Kestrel/MVC test on the shipping CqlV1Controller route /api/data/query-cql. It reuses the actual private policy/SDK/adapter/V2 manager and checks anonymous401, unknown actorId field400 before native reads, owner full3/subset2 private leaf metadata, one query receipt per success, foreign403 before native reads and inactive503 without rows or fallback. Explicit channel completion synchronizes receipt observations; no request retry or timing-dependent polling. Expected suite4cases.

Native responses, validated identity and usage outbox remain controlled fixtures; this is actual socket/MVC binding plus production query machinery, not production JWT/durable billing, native-source WEB4 HTTP, federation or live/browser proof. Cloud-only runner starts/stops the loopback app in try/finally; no native ship/database or local runtime is requested. Accepted native SDK/manager/restart evidence remains untouched. Only this test/workflow path triggers qualification, avoiding redundant older-provider builds.


## Actual MVC wire gate accepted with controlled native source

Run42523748419 atc2a8b239182c3a892147f8d5152349ee0d47c401/Coree62ec44 passed4/4 admitted cases, none skipped. Artifact13634666563 SHA256f93086e24486f777fc275f16118e1d99ad6f0a03df88f10f60616b4bef5df8ee inspected in memory; all outcomes and exact MVC method verified. The real ephemeral loopback server routes through shipping CqlV1Controller/CqlV1ResourceFilter/DTO binding/CqlV1Endpoint/private keyed policy/API compiler/actual SDK/adapter/V2 manager and leaf projection. Anonymous401 and forged actorId400 occur before source reads; owner3/2 returns exact private IDs/metadata with one measured query receipt each; separately validated foreign identity403 and inactive503 have no further native reads, manager creation or query receipts. No replacement rows on inactive source. Server always stopped/disposed; a completion channel synchronizes post-request receipts without retry.

Native HTTP replies, identity validation and usage outbox remain explicit controlled fixtures. This result does not certify production JWT, durable billing, native-source WEB4 HTTP, public-source registration transport/hosting, federation, live or browser. Next native integration should reuse this MVC walkthrough with the owned synthetic source owner/catalogue and in-memory native session, preserve before/after restart API projection hashes and inspect exact cleanup. No public demo card/count increase yet.


## Native WEB4 MVC/restart scope prepared, not executed

The accepted MVC walkthrough is shared by controlled and native entry points, with an explicit source owner and exact expected contact/overlay projection. It now returns a normalized private API projection hash. The native fact requires the owned cloud fixture, exact LF fixture hash and three CIDs; it logs in using in-memory input, counts only actual selected native GETs, and runs the shipping MVC route against the real SDK/adapter/native book. It validates all six contact/overlay fields, owner/IDs/private flags, full3/subset2, query receipts, anonymous401, forged-field400, foreign403 and inactive503 without extra native reads. Input/session values are never assertions, arguments, reports or stored settings. Native test is opt-in for ordinary suites; the selected native workflow requires one executed/passed case, so a skip cannot qualify it.

New native-web4 scope in the registered HTTP-host workflow serializes Release builds of the existing host and reused Core.UnitTests project under one bounded lease before native startup. It restores the same three immutable caches and uses the same owned book/pier/internal namespace. Before/after restart, the hook runs the native fact on frozen outputs with no build/restore, validates its TRX1/1 and compact proof, and requires identical API projection hashes as well as existing source/private-CQL hashes. Native identity and usage outbox are still controlled; production JWT, durable billing, public HTTPS registration/hosting, federation/live/browser remain unproved. No second native fixture is active or dispatched for this preparation.

Namespace command now gives the child the existing runner home/CLI home and has one boolean option to preserve only COSMIC_URBIT_NATIVE_WEB4_INPUT through sudo. No broad environment preservation. The child removes that variable immediately after capture; Python retains no persistent environment/configuration and refuses to retain diagnostics containing the login code. Eight pure namespace controls pass locally. Refactored four controlled admitted/MVC cases must pass cloud compilation before native-web4 dispatch.


Refactored shared MVC preflight at6981acb6b passed4/4 in42525205507/job128867191602. Artifact13634709046 SHA256e001c115c4c70f05ad4bc3ff6dd84f3c7d87df0306eda5a6b364358d2f1efc0a inspected in memory, all outcomes/classes verified. Native fact compiled but was not selected; no native acceptance from preflight. Eight namespace controls pass locally. After proving no other native lifecycle active, one native-web4 scope dispatched at exactly6981acb6b: [42525613719](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42525613719). Follow this original handle; do not restart on observation timeout. Actual MVC-to-native, before/after API hash preservation and cleanup pending. No Development deployment or public profile count change.


## Private federation component gate prepared while native MVC runs

Three new admitted cases exercise the shipping federation endpoint with the actual Urbit SDK/adapter and a controlled canonical Mongo source. Full3+2/subset2+2 must preserve5/4physical copies, provider provenance, private owner fields, independent Mongo owner/public versus Urbit keyed-catalogue permissions and two measured query receipts. Other-owner private denial403 or inactive source503 must reject the entire request before either source query or manager creation; canonical copies cannot substitute for the required private source. Mongo responses and native Urbit replies remain controlled in this component suite, so no native Mongo/federation acceptance or unrelated-copy filtering claim follows.

Expected admitted suite7cases (existing4plus3federation). No native job/environment is started by this change. Already-running native MVC42525613719 remains at frozen6981acb6b and is followed independently; these new tests cannot change that job's source or certify its results. Exact native federation with owned Mongo remains a later distinct gate.


## Actual native WEB4 MVC/restart gate accepted

Native run42525613719/job128868449497 at6981acb6b06c42ed0bede16a6872edc93fe99b27/Coree62ec44 completed success. Artifact13635911089 SHA2561c2806b86aa9947353b67509fbc15e87287f50ec33285f37dcde5541fbef6312 inspected entirely in memory. Both phase TRXs contain the exact UrbitNativeWeb4MvcReadsOwnedBook case, each1executed/1passed/0skips. All before/after JSON assertions independently validated.

Real shipping MVC route -> explicit private policy/API compiler -> actual V2 manager/adapter/SDK -> native current contacts book passes owner full3/subset2, exact six contact/overlay fields and identities/private ownership, six native point reads per phase (one probe+3+2), two successful query receipts and zero rejected-query receipts. API anonymous401, forged-field400, foreign403 and inactive503 occur without extra native reads. Private API projection SHA256df6d5ce459ad17015f4f32ef5df8331531f40597d231dc2fa9a67ad09ba6a80f matches across same-pier restart. Existing raw source/private-CQL hashes are preserved too. Native validated identity and usage outbox remain controlled fixtures; production JWT/durable billing and public HTTPS registration/hosting are not certified by this run.

Three owned pages seeded once, before/after native book reads pass, restart/final exit0, no OOM; owned container/internal-network/pier removal verified. No native credentials persisted or logged. No accepted original live cohort replayed, no remaining native process/data to adopt. Cloud caches retained; local safe receipt urbit-native-web4-mvc-qualified.json only. Generic CQL/live flags remainfalse because native federation/hosting/live/browser still require their gates.

Controlled admitted/federation run42526697897/job128871791735 at7dd4c5187 passed7/7 with zero skips. Artifact13634769248/2218bytes SHA256b752c5b2d43616f1f889755ea203a8aeb2922c0f405b3a1534333ca442705e22 inspected in memory; exactly3federation cases plus4previous admitted/MVC cases, all outcomes verified. Federation preserves5/4physical rows and per-provider receipts/provenance, independent policies and whole-query denial/unavailability before reads; canonical Mongo responses are controlled, so no native Mongo/federation claim. All changes/evidence pushed on existing task branches, no Development deployment or profile-count increment.


## Actual Mongo-backed federation prepared; not executed

The existing Core.UnitTests host now references the actual MongoDBOASIS provider/driver. Its shared MVC fixture accepts an explicit admitted provider set, preserving the one shipping controller/endpoint path. The native federation fact validates the owned replica metadata before connecting; it checks database absence and writes an exclusive ownership/ID intent before initial activation/three canonical saves. Only the beforeRestart phase seeds two matching native IDs plus one unrelated private copy. afterRestart requires the existing marker/IDs and never reseeds. Both phases require real HTTP5/4physical rows, per-source provenance/owner/receipts, unrelated-copy exclusion and unchanged raw canonical BSON. Mongo's owned profiler verifies foreign403/inactive503 caused neither a native point read nor a canonical query; no partial/fallback results. The after phase drops only its verified database and confirms absence.

Cloud-only owned_urbit_mongo uses the existing native internal network, exact owner labels, no published ports, two unique labelled volumes, one replica member advertising its inspected private IP, and explicit no-driver-retry URI flags. It reuses Mongo8.0.15 but pins the official registry index digest f4d54619262ae3bc6a0a8efbebcef970b87b8ad70697479a75ce308a6f400158 (Linuxamd64manifest8332f93f6cb881db35f58caaea2e986e6044f97a3cf2629515875361b3ae359c). Only registry metadata was fetched on the laptop, no image/runtime. Graceful owned container/volume cleanup precedes the parent network cleanup. No source credentials are placed in metadata files.

Registered host workflow adds mongo-transport (short replica/topology/cleanup check with no native ship) and native-federation (existing cached native source + actual Mongo + same-pier restart). Release producers remain serialized under one lease; consumers use frozen outputs/no-build/no-restore. Workflow scope passes through a step-only environment value, not interpolated shell code. Native input reuses the single named child-only secret field; strict shapes distinguish MVC and federation explicitly. Seven controlled cases must compile/pass and the short Mongo resource check must pass before any new full native dispatch. No current live profile/card/count or deployment change, and no production JWT/durable billing claim.
