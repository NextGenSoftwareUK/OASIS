# SOLID private authentication — scoped native verification

## Approved retained-server resume and fresh native acceptance - 2026-10-20

User explicitly approved resuming the SAME retained loopback-only server, volume and identity. Inspected pinned image `58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a`, binding127.0.0.1:13000 and volume `oasis-solid-data:/data`, then `podman start oasis-solid` succeeded. No download, container replacement, key/account provisioning or data reset. No pending fixture journals existed before launch.

Existing reuse-only runner10469 rebuilt producer/consumer with zero errors (2/17 warnings) and completed6/6 native cases, zero skipped,1m02s, exit0. This includes the retained private DPoP/anonymous-denial/encrypted CRUD case and existing conditional acknowledgement/history/graph checks. All pending fixture journals absent afterward. Original server remains running as operator-owned; runner intentionally does not stop/reset it. Earlier first-token cold-start abort did NOT recur in this single suite; its root cause is not established and this is not exhaustive cold-start/restart durability certification.

Report `TestResults/SolidCurrent/solid-native-current.trx` SHA256 `A9F5D2879D67BC529F47EA978F7A73FCDDE11F74E393DC33B014641E86636FC2`; actual consumer Core `9371F0A34B8632422EF93F3DA054B2F6D2A24A41C5CCD7821F2ECDF5703A1847`, Shared `7C9FEA9BCF524E51CF40158526EAE9063CDDB2081CDF8297D53CAE050BDF4DE3`. These identify copied local binaries, not immutable published-source or deployed parity. Free C afterward68,238,987,264bytes; no space-recovery claim. Hosted credentials, full interface/authentication conformance, broader V2 and publication gates remain open.

Updated2026-10-19. This is NOT complete provider/OIDC certification. Scoped positive private DPoP encrypted CRUD is now verified against the retained native CSS server as recorded below. No hosted configuration or credentials changed.

## Native fixture extended for conditional acknowledgements — 2026-10-19, build gate pending

Build follow-up: after verifying Shared ref file present again and peer reported684Shared tests passed, consumer-only rebuild succeeded0errors17warnings. Existing shared identity guard admitted native consumer Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6` and Shared `44CD8A156274EFBCD11BB588D49CD5BA580994EA6D3AB74A4B28EA7E15C23070`. No producer rebuild or DLL copying. This supersedes build-pending status only; ZERO native cases executed, retained service remains stopped pending approval.

Existing six-case retained suite now asserts IsSaved/SavedCount and canonical provider keys for profile/detail/holon saves, three-node graph count, and private DPoP create/update. Existing anonymous encrypted CRUD fixture additionally replaces avatar/detail through normal GUID APIs and verifies updated canonical values; same journal/resources/key/cleanup, no new environment. These assertions are NOT yet native evidence. Initial consumer-only build failedCS0006 because concurrent Shared producer rebuild temporarily removed its obj/ref DLL; ZERO tests/services started. No stale DLL copied or previous binary run. Waiting for coordinated producer release, then rebuild consumer and resume native only after outstanding service approval. Existing234unit/657Shared evidence remains scoped prior checkpoint, not this native gate. No source feature publication/deployment claimed.

## Reuse-only runner must preserve original encryption authority — 2026-10-19

Removed Run-ReusableSolidPodTests.ps1's implicit replacement storage-key generation. Missing retained storage-key.dpapi now fails before builds/native fixture operations and instructs restoration of the ORIGINAL protected key; no fresh key may be silently substituted over existing ciphertext. Existing key/profile/identity retained, values never printed. Parser passes; exact AST-selected source admission guard was executed read-only with existing original key (accepted) and nonexistent probe path (refused), with zero file/env mutations. This is guard-level verification, not a full runner/native test. No before-fix data-loss incident inferred.

Read-only native inspect: SAME pinned image58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a, container EXITED, pending journals0, original protected storage key present. Earlier execution-policy denial remains respected; explicit approval requested to resume retained server for final native gate, no alternate launch bypass. No download/new environment/reset/account/key creation. Startup/factory/native acceptance and all-provider release gates remain incomplete.

## Startup binding candidate — local, final native gate pending (2026-10-19)

Source audit found no SOLIDOASIS setting or BootLoader registration despite the existing real provider/DPoP implementation. Added LOCAL `StorageProviderSettings.SOLIDOASIS`, `SolidOASISProviderSettings` in `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASISDNA.Solid.cs`, and `SolidProviderFactory.Create` plus the SOLIDOASIS register-switch case. The factory reuses the provider, canonical Pod-root validator and real DPoP handler; no replacement backend/protocol. Independent source-linked controlled tests and the existing private native fixture exercise the production factory. These source changes are not published yet; do not treat this configuration as deployed API behavior.

Actual candidate DNA shape (configuration, NOT a quest/request API payload):

```json
{"OASIS":{"StorageProviders":{"SOLIDOASIS":{
  "PodRoot":"https://pod.example/owned/",
  "AuthenticationMode":"DpopClientCredentials",
  "TokenEndpoint":"https://issuer.example/.oidc/token",
  "ClientId":"public-client-identifier",
  "StorageKeyEnvironmentVariable":"OASIS_SOLID_STORAGE_KEY",
  "ClientSecretEnvironmentVariable":"OASIS_SOLID_CLIENT_SECRET"
}}}}
```

The configured storage-key variable must hold base64 encoding of an externally persisted32-byte key, required in ALL modes. No generated-key/anonymous fallback. Factory clears its temporary decoded key after the existing codec clones it. Modes: DpopClientCredentials(default, explicit issuer/client/secret reference required), Bearer(explicit BearerTokenEnvironmentVariable, default OASIS_SOLID_BEARER_TOKEN), Anonymous(explicit selection, no issuer/client settings). Secret VALUES are read from process environment, not added to DNA. No Railway variables/provider lists/HyperDrive mode were changed. Invalid/missing/ambiguous settings fail before activation/I/O. Token/root security validation stays with existing owners; DPoP native signing-key allocation now occurs only after constructor configuration validation.

Security review reproduced an unpublished factory candidate gap: a different-root connection override could reuse configured credentials (constructor-only test, NO actual credential transmission). Before-fix1/1failed, solid-factory-override-before.trx SHA256 `1DD3731EE282B5C2D0A61577F64DF7A090918016A2FFDDA97EF1ADEAB1EDB396`. Factory now accepts only an override equal to the normalized configured PodRoot, before reading any secrets; switching Pod scope requires explicit configuration rather than credential rebinding. Equivalent trailing-slash normalization remains accepted.

Final controlled217/217pass248ms/zero skips, solid-unit-current.trx SHA256 `8263AA7FB0C9E2C07C5E7D2F3E3BA65348D8534218C4E7E10CFE8A7B464FB6FE` (12new startup cases). BootLoader builds0errors/2warnings; DNA net10/netstandard2.1 builds0errors/818 and819warnings; producer0errors/2warnings, units7warnings. Both test-project reference graphs refreshed only from EXISTING local NuGet package cache via explicit local source; no downloads/install/new environment.

Intermediate native6/6pass42s/zero skips through factory valid DPoP configuration, solid-native-current.trx SHA256 `DBB5891480F4CB7C7CA1AA801A99B03D7166E169F07A73B4B369D763C379791D`; producer/consumer0errors/2 and17warnings. Reused SAME private Pod/client/account/DPAPI fixtures/key, no reprovisioning. Process-only synthetic factory secret variables restored in finally. Subsequent override/error hardening is covered by final controlled tests but final native rerun is NOT complete: the restart/readiness command was rejected by execution policy BEFORE process creation. Approval requested; do not bypass it or relabel the earlier run as final-source native proof. Read-only state confirms same container exited137; pending journals0. The completed run's owned stop exceeded30s SIGTERM grace and Podman used SIGKILL, so this is NOT a clean shutdown/durability certificate. Retained volume/identity protected; graceful runtime shutdown and restart-resumption remain explicit gaps.

No source feature commit/push until this declared native gate completes. Concurrent Neo4j BootLoader registration changes are present in the same file: stage ONLY the SOLID hunk after verification, preserving the other contributor's changes. Full BootLoader process startup, bearer-native authorization, revocation/expiry/nonce interoperability, history/options/interfaces and full V2/hosted acceptance remain open.

## Authoritative retained-server finding

Same root-owned `oasis-solid` container, pinned CSS image and retained volume. Discovery at loopback13000 advertises client_credentials, ES256 DPoP and token endpoint `/.oidc/token`. Read-only inspection of installed `config/file-root.json` confirms `identity/handler/no-accounts.json`, public identity access and root initialized for anonymous access. Thus previous4native passes are genuine storage tests but cannot demonstrate private account authorization.

Reconfiguration approved by the user on 2026-10-19: same image/volume/server, brief restart, one reusable synthetic identity, no fresh installation/download/duplicate environment.

The retained container was reconfigured from `@css:config/file-root.json` to the installed account-enabled `@css:config/file.json`. Image ID remains `58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a`; volume `oasis-solid-data:/data`, loopback port13000, memory768MiB and CPU2 remain unchanged. Pre-removal writable-layer inspection showed only `/etc` changed; no application data was removed. Only the stopped container definition was removed (without volume removal) and recreated with `--pull=never`. Podman reported that SIGTERM exceeded20seconds and used SIGKILL; preserve the retained data and verify it before claiming a clean restart. Initial HTTP readiness was not yet established. Positive authentication remains unverified; no synthetic account has been provisioned yet. This is not an additional environment or a volume reset.

## Local implementation and evidence

`SolidDpopClientCredentialsHandler` uses real .NET HttpClient and ECDsa P-256/ES256, not fabricated signatures or a replacement storage backend. Explicit token endpoint and client credentials; one per-handler ephemeral proof key. The optional existing provider HttpMessageHandler constructor can accept this handler with an empty legacy bearer token. Standard transports disable redirects. Resource authentication is restricted to the exact configured Pod subtree, without URI credentials or fragments.

The handler obtains a DPoP token through the CSS client-credentials HTTP flow, validates token type/encoding/positive lifetime, caches it under a cancellation-aware semaphore, and refreshes before expiration. Each outbound resource request gets a new signed proof with method, normalized target, timestamp, random identifier and token hash. No private JWK member is sent. Conflicting caller authentication headers fail before I/O. Resource failures/401s never trigger automatic mutation replay.

Sources: [CSS7 client credentials](https://communitysolidserver.github.io/CommunitySolidServer/7.x/usage/client-credentials/), [RFC9449 DPoP](https://www.rfc-editor.org/rfc/rfc9449.html). This implements the described machine-client flow, not interactive authorization-code login or complete Solid-OIDC conformance.

Whole controlled205/205 tests pass479ms/zero skipped. Eleven new cases independently verify real signature/public JWK, key/token binding, hash/method/target/time, blocked concurrent token acquisition with unique proofs, bad/expired/missing/rejected token responses, out-of-scope rejection and one-shot unauthorized PUT, plus nonce handling below. This is controlled HTTP evidence, NOT a successful real issuer exchange or private Pod write.

RFC9449 issuer `use_dpop_nonce`400/401 with a valid nonce is answered at most once BEFORE resource dispatch, using a fresh proof. A repeated challenge fails visibly after two token requests; no Pod mutation has occurred. Nonces from resource responses are retained for the next explicit request; unauthorized PUT is never replayed automatically. Nonce fields must be singular, nonblank printable ASCII and bounded to1024characters. Header nonce problems fail rather than downgrade authentication. Three controlled cases verify issuer success/repeated-challenge refusal and subsequent explicit GET nonce use after a one-shot rejected PUT.

Current retained CSS suite5/5native passes5seconds/zero skipped. The new real-issuer negative case sends only deliberately nonexistent synthetic client credentials to the existing actual token endpoint, requires native400/401 and verifies zero Pod dispatch. Earlier four encrypted storage cases still pass with rebuilt consumer. This proves real issuer rejection and storage regression, NOT positive client authorization, nonce interoperability or private Pod writes. Same retained operator-owned server remains running; no account/configuration was changed.

Fixed `TestResults/SolidCurrent` evidence: unit TRX SHA256 `9E92E52B92DA47EA31CE1728A5D468D7DF8C6D228F54BDA32E89B019FE579827`; native TRX SHA256 `43F3E36CD825AD65129F9AE63FAAE7D64CD215FB5759DF4A340DD75D48090BFF`.

## Remaining gates

### Current authoritative outcome — 2026-10-19

Corrected the retained server's canonical base URL to `http://localhost:13000/` and its internal listening port to13000, so discovery/JWKS/WebID fetches use the same address inside and outside the container. Host publishing remains IPv4 loopback-only; same pinned image, volume and resource limits, no download or duplicate runtime. CSS warns that old absolute127.0.0.1 WebIDs are not canonical after the change. Detailed verification identified **the WebID claim** as the first rejected claim; the earlier issuer-only diagnosis was incomplete.

Reused the SAME synthetic account/password. Provisioned the corrected `oasis-auth-local` Pod and one corrected named client through discovered CSS account API controls; protected responses use fixed `private-local-pod.dpapi` and `private-local-client.dpapi`. The original empty `oasis-auth-fixture` Pod/old client are retained for explicit reconciliation, not silently deleted or treated as working. They contain no provider test records. Do not reprovision a second account. Both container-definition replacements required Podman's20second shutdown fallback to SIGKILL; subsequent native tests are storage regression evidence, not exhaustive filesystem-integrity proof.

Fresh native **6/6passed, zero failures/skips,40seconds**: real issuer rejects invalid credentials; four existing storage/graph cases still pass; the new private case obtains a real DPoP token, reads its owned ACL, CAS-configures owner-only inherited access with a pre-dispatch original-ACL journal, verifies anonymous root/record denial, performs encrypted provider create/read/update/delete, independently reads actual ciphertext, and verifies native404 after deletion. All owned fixture journals cleared. No verifier/security bypass, anonymous downgrade or mutation replay was used. Provider and independent transport use separately minted/key-bound DPoP sessions. This is successful CSS client-credentials interoperability, not interactive login, token revocation/expiry or full Solid-OIDC conformance.

Fresh controlled **205/205passed, zero failures/skips,343ms**, with rebuilt current producer/consumers. Native TRX SHA256 `797C5966B4883FC5E25A9255A4199B74B33F6E0091D96E230628DE7546502A0B`. Fixed reports remain `TestResults/SolidCurrent`. Free C: after this run:80,290,009,088bytes. No new installation/download/archive/ledger. Remaining gates below are NOT satisfied merely by this batch.

Controlled TRX SHA256 `82E77A9D4663B710F6D3C8AE9B65A29C1339998A81F901057D1A04C8658B7DDC`. Runner now rejects either private native-record or ACL pending journal before starting tests and requires the retained corrected client/Pod protected fixtures. Preparation does not print credential responses or create another account when saved identity authorization exists. The old empty synthetic Pod occupies only setup resources; cleanup must be scoped, not a volume prune.

Final runner/consumer rebuild repeat also passed6/6/zero skips40seconds, including the already owner-only ACL path and all cleared journals. Current native TRX SHA256 `35AB3299628F58B025DF66132BDE118FAFDA2B0EAF68B739DE2159F26F989C06`; earlier797C hash above identifies the first successful canonical-host run. Dependency-manifest/gitlink validator passes and shared Development was0ahead/0behind before scoped integration.

2026-10-19 positive-authentication work: added reuse-only `Scripts/TestHosts/Initialize-ReusableSolidIdentity.ps1`. It created exactly one synthetic account, a synthetic password login, Pod `oasis-auth-fixture`, and one named client credential. Tokens/password/client responses stay in CurrentUser DPAPI-protected fixed files under ignored `TestResults/SolidCurrent`; no secrets printed. Exclusive environment lease and create-before-dispatch pending markers prevent blind account/Pod/client recreation on lost responses. First Pod request was explicitly rejected400 because CSS requires a login method; only that known-rejected pending marker was removed before adding the login and completing provisioning. No unknown outcome was replayed.

New sixth native test is currently FAILED, not accepted: CSS client-credentials exchange succeeds but authenticated ACL access receives401. Installed CSS `DPoPWebIdExtractor` logs explain the cause: `The URI claim could not be verified as secure`; installed access-token-verifier requires HTTPS or specifically localhost-with-port HTTP, rejecting this retained server's `http://127.0.0.1:13000/` issuer. This is not evidence of successful private CRUD. Do not disable TLS/issuer checks, patch the verifier, or silently fall back to anonymous access. Correct canonical retained-server addressing/network configuration and reconcile the owned synthetic identity before rerunning. Public Pod-root access is intentional CSS default; the test will CAS-update only the synthetic Pod ACL to owner-only inheritance, preserving original ACL in a pending journal before mutation. That ACL mutation has NOT occurred because authenticated ACL read failed first; no private record mutation was dispatched. Existing five tests still pass; current combined suite5passed/1failed/zero skips. No provider certification or push of this unverified auth batch.

2026-10-19 post-reconfiguration verification: account discovery now exposes `controls.account.create`; the retained `/native-fixture/` container is readable. Rebuilt producer and integration consumer and reran the unchanged five-case native suite: **5/5 passed, zero failures/skips,3seconds**. This checks storage regression and negative issuer behavior after the forced shutdown, not positive private authentication or exhaustive data integrity. The synthetic account/client credential has not yet been created. Retained service is running; no runtime downloads, new volume or duplicate server. Free C: space measured after this run is recorded by the task tool output.

- Reconcile/revoke the retained superseded127.0.0.1 synthetic client and empty Pod without deleting unrelated volume data.
- Real revocation/expiry and restart-resumption tests; positive issuer exchange and private encrypted CRUD now have scoped native proof.
- Real issuer/resource nonce interoperability and token lifecycle/error/transport/disposal hardening; controlled bounded challenge processing exists but is not complete authentication certification.
- BootLoader/OASISDNA binding and explicit persisted credential handling; no secrets in source/chat/reports.
- Complete provider options/interfaces/history/paging interoperability and V2/hosted acceptance remain open separately.

No production spending or measured hosting-fee claim. Token refresh adds issuer HTTP requests; record operations retain existing Pod request/egress costs. No installations/downloads/new server were performed for this local implementation.
