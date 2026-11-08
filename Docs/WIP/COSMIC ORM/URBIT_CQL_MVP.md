# Urbit current-protocol CQL preparation

8 November2026. Current contacts reader implemented; CQL, native ship execution,
manager/federation, live WEB4 and browser remain unqualified. No coverage-count
increment or earlier accepted cohort replay. Priority-chain rollout remains first.

## Verified current read client

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
