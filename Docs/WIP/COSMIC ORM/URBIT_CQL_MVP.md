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
