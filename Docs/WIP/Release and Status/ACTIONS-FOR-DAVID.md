# Actions for David

The single list of everything that needs you: your accounts, phones, admin rights, approvals or a decision. Everything else is done or in progress. Tick items off and tell Claude when one is done; it picks up the next step. Never paste passwords, API keys, tokens or connection strings into chat.

All agents must record owner-dependent requests here before asking, update answers, and preserve deferred decisions without repeatedly asking. Agent implementation/tool failures remain agent work.

Last updated 31 October 2026. This file replaces `OWNER_SETUP_CHECKLIST.md` (merged into Part B below) and the PIPER work log's action list (Part A).

## Open actions at a glance

| # | Action | Part |
|---|---|---|
| 1 | **Publish the fixed OASIS npm SDKs** (fixes PIPER "Save failed: Stable operation intent…") | A1 |
| 2 | Set up npm trusted publishing for the 7 SDK packages (one-time) | A2 |
| 3 | PIPER: set `SESSION_PLAN_GITHUB_TOKEN` on piper-server, check the screens, tell Hailey it's ready for Gate 2 | A3 |
| 4 | Release gate: Railway tokens, staging Mongo user, staging connection strings | B |
| 5 | OurWorld Unity Cloud access | B |
| 6 | Provider acceptance access: ActivityPub, EVM decision, AWS S3, Solana devnet funding, Oracle Cloud, CoinGecko key | C |
| 8 | **Add two DNS records so the new HoloNET docs and learn sites go live** (NameSilo) | E1 |
| 9 | Rotate the Arbitrum testnet key and the block-explorer API key that were committed to git (removed from files, still in history) | E2 |
| 7 | Older items: phone tests, WSL disk, Vercel branch check, oasisweb4 site decision, iOS runtime script, Git LFS budget, upstream Holochain PRs | D |
| 10 | COSMIC/CQL: decide follow-up Default-routing fix publication; hosted Azure/Oracle remain deferred | F |

---

## Part A — npm SDKs and PIPER (added 28 October 2026)

### A1. Publish the fixed OASIS npm SDK packages

**Why:** OASIS HyperDrive V2 rejects writes without a stable operation id, which broke every PIPER save. The fixes are committed and pushed but not on npm, because publishing needs your 2FA.

| Package | Version | Change |
|---|---|---|
| `@oasisomniverse/web4-api` | 2.0.16 | 2.0.15: `X-COSMIC-Operation-Id` on every write + `Idempotency-Key` on every request (same id on writes). 2.0.16 (28 Oct): keeps `metaData`/`providerMetaData` keys as sent, so a loaded holon saves back with its revision (PIPER edits) |
| `@oasisomniverse/web5-api` | 1.0.6 | `Idempotency-Key` on every request (committed 21 Oct, never published) |
| `@oasisomniverse/web6-api` | 2.1.2 | same as web5 |
| `@oasisomniverse/web7-api` … `web10-api` | 1.0.5 each | same as web5 |

Run in a terminal and approve each 2FA prompt:
```bash
cd C:/Source/OASIS-API-Javascipt-Package-WEB4 && npm run publish-all
```
Or with an authenticator code (codes last ~30 s, so a later package may still prompt): `npm run publish-all -- --otp 123456`. The script tests each package, skips anything already published, and prints a summary.

- [ ] All seven published.
- [ ] Told Claude, who then refreshes the lock files and pushes piper-server and piper-ui (both already on `^2.0.15`, committed, not pushed).

### A2. One-time: npm trusted publishing (so releases publish on push)

Each SDK repo has a GitHub workflow `publish.yml` that publishes on push whenever `package.json` has a version not yet on npm. It needs npm to trust it (no token, no 2FA):

- [ ] For each of the 7 packages on npmjs.com: package → **Settings → Trusted publishing → GitHub Actions** → owner `NextGenSoftwareUK`, the matching repo (`OASIS-API-Javascipt-Package-WEB4`, `OASIS-API-Javascript-Package-WEB5` … `WEB10`), workflow `publish.yml`.

Until then the "publish" workflow fails on each push (harmless).

### A3. PIPER (M4)

Full context: `C:/Source/PIPERDOCS/milestone4-session-plan-engine/piper-m4-work-log.md` (Open items).

- [ ] Set `SESSION_PLAN_GITHUB_TOKEN` on the piper-server deployment (fine-grained, Elias-Therapy/session-plan only, Contents + Checks read-only; steps in session-plan `PIPER_INTEGRATION.md` §D). Packs, goal form and plans depend on it.
- [ ] Press **Reload packs** once CI is green for session-plan `0c1d50f` (Hailey's min/independent cue rulings).
- [ ] Check the screens: new student, goal form, SA plan panel, session reports, hand-scored session panel, Start session.
- [ ] Then tell Hailey it's ready for Gate 2.
- [ ] Optional: copy Hailey's Drive recordings into piper-ui `public/voice/lines/` as .ogg and list them in `voiceLinesIndex.json`.

---

## Part B — Release gate and OurWorld Unity Cloud (was OWNER_SETUP_CHECKLIST.md, 15 October 2026)

Updated 15 October 2026 (America/Cancun).

These are the remaining owner-account actions for the staging/production gate and OurWorld Unity Cloud. Never paste passwords, API keys, connection strings or tokens into chat. No new Mongo cluster, paid plan or environment is needed for the gate.

### 1. Create two Railway project tokens

1. Open [Railway OASIS](https://railway.com/project/712f50f6-3b7b-46d4-b229-44b49922f16b).
2. Click **Project Settings → Tokens → Create Token**.
3. Name the first token `github-staging-gate`; select environment **staging**. Create it and retain the value securely.
4. Create a second token named `github-production-gate`; select environment **production**.
5. Use environment-scoped **project tokens**, not personal account tokens. If token creation is denied, use the project/workspace owner account; do not broaden an unrelated credential.

#### Save the tokens in GitHub

Open [OASIS Settings → Environments](https://github.com/NextGenSoftwareUK/OASIS/settings/environments). Select each environment below, then **Environment secrets → Add secret**. Enter the exact name and corresponding token; click **Add secret**.

| GitHub environment | Secret name | Value |
|---|---|---|
| `oasis-staging-gated` | `RAILWAY_TOKEN` | Staging-scoped project token |
| `oasis-production-gated` | `RAILWAY_TOKEN` | Production-scoped project token |
| `oasis-production-gated` | `RAILWAY_STAGING_TOKEN` | The same staging-scoped project token |

- [ ] All three environment secrets saved.

Do not put these at repository scope. Do not change the existing master-only restrictions, required reviewer `dellams`, or disabled administrator bypass.

### 2. Create the staging-only MongoDB user

1. Open [MongoDB Atlas](https://cloud.mongodb.com/). Select the project containing the existing OASIS cluster.
2. Under **Security**, open **Database & Network Access** (older layouts: **Database Access**).
3. Select **Database Users → Add New Database User**.
4. Choose **Password** authentication.
5. Set username to `oasis_staging_gate`. Generate a strong password and save it securely.
6. Under **Database User Privileges → Specific Privileges**, add these two entries:

| Privilege | Database | Collection scope |
|---|---|---|
| `readWrite` | `oasis_staging` | All collections in this database |
| `readWrite` | `oasis_subscription_accounting_staging` | All collections in this database |

7. Remove any default administrator or **Read and write to any database** role. Do not select “any database.”
8. Enable **Restrict Access to Specific Clusters** and select only the existing OASIS cluster.
9. Click **Add User**. Do not make it a short-lived temporary user if the deployed staging services will continue using it.

- [ ] User created with only the two database permissions above.

Both databases already exist and were created empty. Do not recreate them or copy production data. Do not change Atlas network rules or open access to all IPs as part of this checklist. If the form is unavailable, sign in as the Atlas project owner or database-access administrator.

### 3. Install that user's connection string in Railway staging

1. Return to Railway OASIS and select **staging** in the environment selector. Double-check this before editing.
2. For **each of the seven WEB4–WEB10 API services**, open **Variables → OASIS_DNA_JSON → Edit**.
3. Find the existing JSON property:

   ```text
   OASIS.StorageProviders.MongoDBOASIS.ConnectionString
   ```

4. Replace only the username/password in that existing URI with the new user's credentials. Preserve its cluster hostname and connection options, including the authentication database. URI-encode special characters in the password; the JSON must remain valid.
5. Keep `OASIS.StorageProviders.MongoDBOASIS.DBName` equal to `oasis_staging`.
6. In **WEB4** also update `SUBSCRIPTION_MONGODB_CONNECTION_STRING` if that variable exists, using the same staging-only identity. Keep `SUBSCRIPTION_MONGODB_DATABASE` equal to `oasis_subscription_accounting_staging`.
7. Save/stage changes **without deploying**. Do not click Deploy, Redeploy or Apply-and-deploy. If Railway offers no save-without-deploy path, stop and tell me rather than triggering a deployment.

- [ ] Seven staging application connection strings updated.
- [ ] WEB4 explicit accounting connection string updated if present.
- [ ] No production/dev configuration edited and no deployment triggered.

Do not create a top-level `MONGODB_URI` instead: this gate checks the existing DNA property and WEB4 accounting override. The database names and test Stripe configuration are already set; leave them unchanged.

### 4. Tell me when the gate setup is ready

Reply with this nonsecret confirmation:

> Railway environment secrets installed. Mongo user `oasis_staging_gate` has readWrite only on the two staging databases. Staging connection strings updated without deploying.

If you used a different username, tell me that username only. I will record the reviewed username in gate policy, verify the configuration without exposing secrets, obtain exact-source CI/promotion evidence, and run staging acceptance.

#### Your final production approval

After staging passes, I will send the exact GitHub run link. Sign in as **dellams**, open **Review deployments**, select `oasis-production-gated`, inspect the candidate/staging evidence and click **Approve and deploy** only if satisfied. I will not approve on your behalf. Approval is not needed yet; there is no accepted staging candidate waiting today.

The gate promotes the same source SHA and pinned dependencies, not a byte-identical prebuilt container image. Live end-to-end proof remains incomplete until staging, your approval and production acceptance all pass.

### 5. Separate remaining task: OurWorld Unity Cloud

Follow [the detailed OurWorld access guide](<../Our World/OURWORLD_UNITY_CLOUD_ACCESS.md>). Owner actions:

- [ ] Sign in to [Unity Cloud](https://cloud.unity.com/), select/create the intended OurWorld project in your organization, and approve the permitted Build Automation scope/budget.
- [ ] Create a scoped Build Automation service account/key for that project.
- [ ] In [OASIS Actions secrets](https://github.com/NextGenSoftwareUK/OASIS/settings/secrets/actions), save `UNITY_CLOUD_KEY_ID` and `UNITY_CLOUD_SECRET_KEY` as repository secrets.
- [ ] In [OASIS Actions variables](https://github.com/NextGenSoftwareUK/OASIS/settings/variables/actions), save `UNITY_CLOUD_ORG_ID` and `UNITY_CLOUD_PROJECT_ID`.
- [ ] Authorize Unity's read access to `NextGenSoftwareUK/Our-World` and required private dependencies/LFS assets only.
- [ ] Tell me the entries are saved, which build targets you approve, and the build/spend limit. Do not send key values.

I can then verify access and configure the Cloud project/integration within that approved scope. Saving credentials alone does not configure Cloud or prove a build. No legacy ULF/GameCI activation or unapproved billing will be used.

### What you do not need to do

Do not republish existing packages/releases, rerun old promotions, enable Railway push autodeploys, enable the staging Stripe webhook manually, or delete caches/environments. The HoloNET hosted-conductor verification repair is my remaining engineering work, not an owner setup task. Physical-device/two-device acceptance is separate and not proved by CI or publication.

Technical details: [staging-gate runbook](RAILWAY_STAGING_GATE.md) · [release status](OASIS_RELEASE_STATUS.md).

---

## Part C — Provider acceptance access (23–25 October 2026)

### Approved retained SOLID init-wrapper change — 24 October 2026

- [x] David approved reconfiguring ONLY the existing `oasis-solid` container definition with Podman's built-in `--init` (reply: “sure”), preserving pinned image58bee7c3..., existing oasis-solid-data volume, protected identity and loopback127.0.0.1:13000. No further approval action is pending from David for this change. No download/new environment/data reset is authorized or needed; inherited `/usr/bin/catatonit` already exists.
- [x] Agent applied and verified the approved same-definition init change: PID1 podman-init/PID2 node; frozen authenticated read PASS; shutdown1008ms, exited143/OOMfalse, no SIGKILL. Volume/identity hashes match. This closes the observed read-only shutdown issue, not write-drain/crash durability or full provider/V2 acceptance. Details: [SOLID authentication](../HyperDrive-Providers/SOLID_AUTHENTICATION.md), resource ledger in the same task folder.

### Pending ActivityPub paged account-search acceptance — 24 October 2026

- [ ] Configure an existing authorized Mastodon test user token with `read:search` outside chat as `OASIS_ACTIVITYPUB_SEARCH_TOKEN`; provide non-secret instance origin through `OASIS_ACTIVITYPUB_SEARCH_INSTANCE` and a narrow known public-handle query through `OASIS_ACTIVITYPUB_SEARCH_QUERY`. Never put tokens/private emails in chat or the query. Reuse the existing suitable test identity; no account creation requested.
- [ ] Opt in with `OASIS_ACTIVITYPUB_SEARCH_LIVE=1`,then run only `AuthorizedMastodonAccountSearchTests` in the existing ActivityPub integration project under the shared build lease. This performs GETs only,with `resolve=false`; no status deletion/posting/remote-account resolution. Token scope and server search backend are enforced by the native API. The result must retain stable native/account projection identities and fully exhaust bounded pages; a public anonymous first page is NOT full search acceptance. Mastodon requires a user token for offset pagination. Source/local231controlled proof is documented in the ActivityPub audit,not counted as live authenticated certification.

### Pending ActivityPub authenticated status-delete acceptance — 24 October 2026

- [ ] Configure a protected user token for an existing authorized Mastodon test instance outside chat, with `write:statuses`. Supply only the non-secret instance origin and native ID of a specifically approved owned disposable status. Do not send the token in chat. No personal posts will be deleted or accounts created merely to obtain a green test.
- [ ] Explicitly approve deletion of that exact disposable status before live acceptance. If the instance is shared, coordinate ownership and rate limits first. No live POST/signup/federation write is currently authorized by these controlled tests.

Implementation/controlled200case gate is recorded in [ActivityPub audit](../HyperDrive-Providers/ACTIVITYPUB_PROVIDER_IMPLEMENTATION_AUDIT.md). Public8GET evidence is not authenticated mutation/ownership proof. Lost or invalid mutation acknowledgements must preserve original status identity for reconciliation, never trigger automatic DELETE replay. Reuse any existing suitable test identity/environment; no duplicate server setup requested.

### Pending local EVM authority/recovery decision — revalidated 23 October 2026

- [ ] Choose how to proceed with the preserved uncertain Ethereum/Web3 test outcomes: supply the original local chain state/RPC authority if available, or explicitly authorize preserving the historical outcomes as unverified while reusing the cached local runtime for an isolated new test-contract cohort. No public ETH/gas spending is proposed. Do not delete the old fixture or describe new-chain success as reconciliation of old transactions.

Current original `C:/Source/OASIS/Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/bin/Debug/net10.0/TestResults/Web3CoreRouting/routing.db` is827392bytes/SHA256`1A826BEFECEBFF8A6EC2628447FED96BE0575ABBC17A83E27C1817227CA69E96`, unchanged from documented44-operation original. Its old ephemeral chain authority was lost; no matching authority is established by starting another chain. The existing native runner still rejects before setup, and no node restart/reset/replay or original cleanup occurred. Detail/evidence: `Docs/WIP/HyperDrive-Providers/EVM_RECORD_HISTORY_GAPS.md`. Controlled selector/codec tests can continue but are not live-chain acceptance. This decision is separate from hosted Oracle/AWS credentials below.

### Pending AWS S3 live acceptance access — added 23 October 2026

25 October update: the SAME runner now also supports `-RecordLifecycle -StorageEncryptionKeyEnvironmentVariable OASIS_AWS_S3_STORAGE_ENCRYPTION_KEY` with the existing region/bucket/profile and explicit cost-approval arguments. Reserve an initially EMPTY exclusive `oasis-provider-acceptance/` prefix; do not use customer data or concurrent writers. This invokes the actual encrypted public provider through the shared OCI/S3 acceptance body, not only object transport. Nine public mutations plus queries/index/history observations can issue many more SDK requests; this is not a fee cap. Current-object cleanup does not purge retained bucket versions/delete markers or certify zero storage cost. Keep secrets outside chat/Git. Controlled signed-SDK tests pass; hosted execution remains pending an approved test account/bucket/profile/cost scope.

The guarded native transport entry point is prepared (not run against AWS): `Scripts/TestHosts/Run-AwsS3LiveAcceptance.ps1 -Region <region> -BucketName <isolated-test-bucket> -ProfileName <protected-profile> -ApproveCloudRequests`. Omit ProfileName only to use the explicitly configured SDK default credential chain. It reuses the existing .NET10 PowerShell/build outputs, checks source identity and requires deliberate cost approval; it does not install/build/provision. The fixed object is `oasis-acceptance/transport-probe`. Existing pending/success/interrupted-snapshot evidence blocks another run. Do not delete that evidence to force a retry. A successful transport probe is not full provider/IAM/billing/HyperDrive certification. S3 bucket versioning can retain billable versions even after the probe's current-object deletion; choose an appropriate isolated test bucket and review retention costs.

- [ ] Configure a least-privilege test identity outside chat using the AWS SDK default credential chain (for example a protected AWS profile or workload role), or protected explicit/session credentials. Tell us only the non-secret region, existing disposable test bucket and allowed test prefix. Do not send access secrets or private storage keys in chat.
- [ ] Confirm scoped permission for bucket HEAD/LIST and object GET/PUT/DELETE, including native conditional operations. If the bucket uses SSE-KMS, confirm the required KMS permissions and costs. Confirm request/storage/transfer cost limits before live tests; do not use customer records. Bucket creation is not part of activation or authorized implicitly.
- [ ] Persist a separate 32-byte storage-encryption key as canonical base64 in the protected environment variable `OASIS_AWS_S3_STORAGE_ENCRYPTION_KEY` (or configure another variable name). Preserve it for test reopens; the provider never generates a replacement. Signing credentials and storage encryption are separate.

Read-only inspection found no AWS-named process environment variables or default `C:/Users/david/.aws/config` / `credentials` files. This is not a claim that no AWS account/profile exists elsewhere. Controlled SDK tests do not establish live cloud access, fees, native durability or HyperDrive V2 acceptance. AWS clients require BOTH DynamoDB and S3; existing DynamoDB test results do not certify S3. Details: [AWS implementation audit](../HyperDrive-Providers/AWS_DIRECT_CONFIGURATION_AUDIT_2026-10-20.md).

### Pending Solana devnet NFT funding — 23 October 2026

- [ ] Fund the retained synthetic public address `Fi4AJCtDCehEVc7CiQVcHC9t57BmKk3B6gtMjWqA4Ffr` with **free Solana devnet test SOL only**, using the official [Solana faucet](https://faucet.solana.com/). Do not send mainnet/real-money SOL or share private keys. Latest read-only check24October16:25:46UTC verified official devnet genesis and finalized balance0lamports (slot514667749); no native NFT test can proceed unfunded. Existing wallet/SDK must be reused; no faucet retry/signing/broadcast was attempted in this check.

The earlier approved RPC faucet request returned an internal error and no signature. Its original pending journal remains preserved/unresolved; do not erase it or infer nonexecution from the current balance. A manual top-up is not closure of that historical outcome. Notify this session after funding so read-only authority/balance can be checked before any approved devnet mint/transfer/burn. Native test costs/rent must be recorded; free test-SOL authorization is not production-sponsored spending approval. Details: [Solana checkpoint](../HyperDrive-Providers/SOLANA_SUBMISSION_CONCURRENCY.md).

### Pending Oracle Cloud test access — added 23 October 2026

- [ ] **Cloud account/tenancy name or sign-in:**25October read-only browser check reached the blank “Cloud Account Name” prompt at Oracle Cloud Sign In, not the tenancy console. Provide the test account/tenancy name (not a password), or sign in yourself through the visible Oracle tab and tell this session when ready. No account, API key, permission or paid resource was created. Existing default OCI profile/process configuration is still absent from the checked locations; nondefault profiles can be reused if you supply their non-secret paths/names. Never send passwords, private keys or wallet secrets in chat. This is in addition to the isolated database/bucket and cost-scope details below, not hosted acceptance itself.

- [ ] **Existing Oracle schema review before deployment:** identity USERNAME/EMAIL columns now require explicit character length semantics (new bootstrap uses `VARCHAR2(256 CHAR)`). Existing BYTE schemas are rejected for review, not automatically altered. Have the DBA inspect existing values, capacity and indexes before any migration; do not blindly narrow wider columns. Only verified-empty local test identity tables use the guarded test upgrade. Details: [Oracle client readiness](../HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md).

The retained local Oracle test schema is already upgraded and verified; David need not repeat that operation. The unchecked review above applies only to existing hosted/client schemas before deployment.

Oracle is the first provider priority (both database and object storage), followed by AWS. Local Oracle Free and signed SDK loopback tests do not replace live cloud acceptance. David plans to check access tomorrow; no new paid resources are authorized by this checklist.

- [ ] **Autonomous Database:** provide an isolated test database's connection/service name and test username; configure its password securely outside chat. If wallet-based access is required, place the wallet locally and share only its path and connection alias. Keep customer data out of the test database.
- [ ] **Database read-only preflight target:** provide the approved `DB_UNIQUE_NAME`, effective test schema/session username and actual service name (non-secret target identity). Configure the full test connection through a protected environment variable; share its NAME only, never the value. The separate `Run-OracleDatabaseLivePreflight.ps1` requires these exact identities and explicit `-ApproveCloudReadRequests`, observes TCPS on its inspection connection and validates existing canonical schema with provisioning denied. It does not execute the local fixture's triggers/DDL/CRUD or provision any cloud resource. Connection/read requests and database compute can still incur charges. Wallet/certificate/server-name validation policy requires separate review; a TCPS observation is not full TLS/security certification.
- [ ] **Database bounded provider CRUD approval:** after reviewing the isolated existing schema and cost limits, the SAME command's `-RecordLifecycle -ApproveCloudWriteRequests` mode exercises nine public mutation calls and exact owned current-row cleanup, with original intent/hash preservation and no DDL/retry/error cleanup. It requires the same approved target and protected connection VARIABLE NAME. Nine is NOT a request/fee cap: verifier record reads alone currently total76, excluding provider/history/index/catalog SQL. Current-row absence does not erase undo/redo/logs/backups or certify zero storage cost, hosted durability, wallet/certificate policy, every provider connection or full V2. Review those separately; do not run the broad local trigger/fault suite against a cloud/client schema. [Command and scope](../HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md#guarded-sql-provider-acceptance-entry-point).
- [ ] **OCI Object Storage:** identify an isolated disposable test bucket, its region, namespace and bucket name. Configure a local OCI API-key profile with the required test-bucket permissions; share only its profile name and configuration-file path. Keep the private key in its protected file, never in chat or Git.
- [ ] **OCI encrypted record acceptance:** configure a separate persisted32byte storage-encryption key as canonical base64 outside chat, for example in `OASIS_ORACLE_OBJECT_STORAGE_ENCRYPTION_KEY`. Provide only the VARIABLE NAME, never its value. Reserve the initially EMPTY, exclusive `oasis-provider-acceptance/` prefix in the isolated bucket for this bounded record gate; no customer records or concurrent writers. The existing transport probe uses a different prefix and does not certify records.
- [ ] Confirm any cloud cost limits before paid provisioning or testing. Requests, database compute and retained storage may incur charges; no exact cost is established yet.

A guarded native OCI transport runner is now prepared: [command, approval guard and uncertain-outcome handling](../HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md#guarded-oci-live-transport-entry-point-prepared--not-cloud-certified). Its current probe includes native object listing after creation and deletion: twelve nominal SDK requests with single-page listings; pagination may add requests, so this is NOT a billing/request cap. Confirm bucket inspection and scoped object LIST/read/create/replace/delete permissions and request/egress/storage cost limits. It uses your existing profile and isolated test bucket; do not run it against customer data. Its native probe does not replace full provider/encryption/IAM/HyperDrive acceptance. No live cloud request has been made by preparing its local controls.

Default local OCI configuration was absent at the latest check; nondefault profiles may already exist and should be reused. Do not create duplicate environments. Detailed implementation/test status: [Oracle client readiness](../HyperDrive-Providers/ORACLE_CLIENT_PRIORITY.md).

The optional `-RecordLifecycle -StorageEncryptionKeyEnvironmentVariable <variable-name>` scope on the SAME guarded `Run-OracleObjectLiveAcceptance.ps1` runs the actual public provider, not just object transport. It requires all existing profile/region/namespace/bucket and `-ApproveCloudRequests` arguments. Nine acknowledged public mutations plus queries, private-index enumeration, native readbacks and cleanup can generate MANY more SDK requests than the transport probe; nine is NOT a request/fee cap. Confirm request/egress/storage limits before executing. Pending/report/atomic-next evidence fences another run; unknown writes are preserved without retry/error cleanup. Local signed-SDK validation is not hosted success.

**Retention/cost warning:** [OCI bucket versioning](https://docs.oracle.com/en-us/iaas/Content/Object/Tasks/usingversioning.htm) can retain previous versions and delete markers after current-object deletion, with continuing storage charges. Empty current listing is not physical erasure or zero retained storage. Select/review the isolated test bucket's versioning/retention policy and cleanup authority before testing; this runner does NOT alter bucket policy or purge native object versions. Its report explicitly marks bucket-version/fee/HyperDrive/full-provider verification false. Never delete original uncertainty evidence or user versions to force a green gate.

### Pending provider credential action — CoinGecko

- [ ] Revoke/rotate the previously embedded CoinGecko credential if active. Configure its replacement as `COINGECKO_DEMO_API_KEY` in the retained test environment, not in chat or source control. Live quote verification remains pending.

---

### Pending Cloudflare Workers KV native acceptance — added28October2026

The real REST/shared encrypted adapter passes52controlled core checks plus25protocol/metadata controls. Hosted acceptance still needs a dedicated owned namespace and scoped KV read/write token. No Cloudflare account/namespace or installation was created; process configuration and three standard Wrangler profile locations were empty.

Configure account/namespace/token and the separately persisted32-byte storage key outside chat using the `OASIS_CLOUDFLARE_CONFORMANCE_*` variables listed in [the scoped baseline](../HyperDrive-Providers/CLOUDFLARE_KV_STORAGE_BASELINE.md). The opt-in native suite preserves actual failures. Workers KV's native eventual consistency, same-key write limit and lack of CAS/transactions remain contract gates even with credentials; do not assume a controlled pass certifies immediate visibility or current credential authority. A stronger authority/Durable Objects design is separate from this KV adapter.

## Part D — Older owner actions (14–15 October 2026)

Things only you can do: they need your phone, your accounts, admin rights or a decision.
Everything else is done or in progress. Last updated 2026-10-14.

Tick them off as you go; tell Claude when one is done and it will pick up the next step.

---

### 1. Publish HoloNET 4.1.0 to NuGet

> **Superseded, nothing to do (31 October 2026).** HoloNET is now published from the OASIS repo's own secrets through
> `publish-holonet.yml` (NuGet 4.2.0 to 4.5.1 are live, each with a GitHub release and its packages attached), so the
> two secrets on the `holochain-client-csharp` fork below are not needed. Kept for the record.

**Why:** 4.1.0 (Holochain 0.7 support) is ready but can't publish until the fork has two secrets.
Claude can't handle API keys or tokens, so you add them.

1. Create a NuGet API key at <https://www.nuget.org/account/apikeys> with **Push** for `NextGenSoftware.Holochain.HoloNET.*`, then:
   ```bash
   gh secret set NUGET_API_KEY -R NextGenSoftwareUK/holochain-client-csharp
   ```
2. Create a fine-grained GitHub token at <https://github.com/settings/personal-access-tokens/new>:
   resource owner **NextGenSoftwareUK**, repositories **HoloNET-ORM, HDK, HyperNET, API**, permission **Contents: Read-only**. Then:
   ```bash
   gh secret set PRIVATE_SUBMODULE_PAT -R NextGenSoftwareUK/holochain-client-csharp
   ```
3. Tell Claude. It runs the publish, checks NuGet and creates the v4.1.0 GitHub release.
   (Details: `holochain-client-csharp/RELEASE-4.1.0.md`.)

### 2. Test on your phones

**Why:** everything mobile has passed on emulators/simulators only.

**Android:** plug in the phone with **USB debugging** on (Settings → Developer options) and accept the "Allow USB debugging" prompt.
Claude installs the 0.7 shared runtime and runs the bridge test; you'll only need to tap "approve" on one notification.

**iPhone:** needs a Mac (or a device farm). The simulator tests pass in CI.

### 3. Compact the WSL disk (administrator required; updated 2 November)

**2 November evidence:** cleanup recovered about16.1GB of Windows artifacts; latest free60.9GB. WSL trim/cache cleanup completed and Ubuntu stopped. The98.6GBVHD still holds92.3GBof used filesystem data, so compaction alone offers roughly6GB, not80GB. This agent is not elevated; administrator compaction remains pending. [Measured recovery/retention plan](../Cleanup/DISK_RECOVERY_CURRENT.md).

**Decision resolved 2 November 2026: KEEP BOTH.** David explicitly retained `/home/dellams/miniforge3` (34.5GB) and `/home/dellams/art-ml-models` (2.8GB). Exclude both from cleanup; do not ask again unless David changes this decision. Existing provider containers/volumes/chain ledgers also remain preserved; no blanket prune or dataset reset authorized by their stopped state.


In **Ubuntu**:
```bash
sudo fstrim -av
```
Then in an **admin PowerShell**:
```bash
wsl --shutdown
```
```bash
diskpart
```
At the `DISKPART>` prompt:
```
select vdisk file="C:\Users\david\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu_79rhkp1fndgsc\LocalState\ext4.vhdx"
attach vdisk readonly
compact vdisk
detach vdisk
exit
```
Don't use `wsl --manage ... --set-sparse --allow-unsafe` (WSL warns it can corrupt the disk).

Personal Miniforge and ML models are retained by David's 2 November decision.
Other personal ML resources are not authorized for removal.

### 4. Check Vercel before deleting old branches

**Why:** Max's guides say **www.oasisweb4.com** deploys to Vercel from the OASIS **`max-build2`** branch.

1. In Vercel, open the oasisweb4.com project → Settings → Git. Note the repo and production branch.
2. If it's `NextGenSoftwareUK/OASIS` / `max-build2`: point it at the `oasisweb4.com` repo first (or tell Claude to).
3. Then tell Claude to delete the OASIS **`main`** and **`max-build2`** branches. Everything on `main` is backed up in the private
   `OASIS-main-archive` repo and its projects are split into their own private repos.

### 5. Decide: Max's newer oasisweb4 site

`main` had a newer version of the website (login, mobile menu fixes). It's in `OASIS-main-archive` and on the
`from-oasis-main` branch of `oasisweb4site`. Adopt it, or keep the current site?

### 6. Run one PowerShell script (permission was blocked for Claude)

Downloads the pinned iOS conductor release and checks its SHA-256s:
```bash
pwsh -File Scripts/fetch_holochain_ios_runtime.ps1
```
(from `C:\Source\OASIS`). Tell Claude if it fails.

### 7. Optional: Git LFS budget

The org's Git LFS budget is used up, so `OASIS-main-archive` holds LFS pointers rather than copies (the files stay in the OASIS repo's
LFS storage). Raise the budget in the org's billing settings if you want the archive self-contained.

### 8. Watch the upstream Holochain PRs

Offered to `holochain/android-service-runtime`; reply to any review comments, or tell Claude to:

- [#149](https://github.com/holochain/android-service-runtime/pull/149): client parcelers for 0.7
- [#150](https://github.com/holochain/android-service-runtime/pull/150): Tauri service plugin network config for 0.7
- [#151](https://github.com/holochain/android-service-runtime/pull/151): iOS support

### 9. Decisions from the HoloNET testing: done

1. **Synchronous methods now wait for and return the result** (you chose this, 2026-10-14). 66 methods; events still fire.
   Behaviour change for anyone relying on the old immediate return; noted in the CHANGELOG for 4.1.0.
2. **Holochain capability-grants bug**: already reported upstream
   ([holochain/holochain#5950](https://github.com/holochain/holochain/issues/5950)) and fixed in **Holochain 0.7.1**
   (released 2026-10-14). HoloNET's workaround is correct on 0.7.0 and 0.7.1.

---

### 10. Holochain 0.7.1: hApps rebuilt (informational, done)

HoloNET supports **both 0.7.0 and 0.7.1** (96/96 live tests pass on each). The OASIS hApp's **main now builds for
0.7.1** (hdk 0.7.1 / hdi 0.8.1). A 0.7.0 conductor needs the tag `holochain-0.7.0`, because the two builds aren't
interchangeable ("callback has invalid parameters").

### Status: HoloNET test coverage (measured 2026-10-15)

- **1,647 tests; 1,637 pass on both Holochain 0.7.0 and 0.7.1**, plus 10 conductor auto-start tests (run on request).
- **HoloNET.Client line coverage 95.1%, branch 85.4%** (was 54.9% / 49.6%).
- The push found and fixed **about 40 real bugs**, including calls that could hang forever, decoders that never matched
  the 0.7 wire format, and "shut down all conductors" killing every `rustc` process on the PC. The full list is in
  `holochain-client-csharp/CHANGELOG.md`. Two shared libraries got fixes too (NextGenSoftware.Logging, .WebSocket).
- **Not 100%, on purpose:** the remaining ~285 lines are defensive `catch` blocks and fallbacks that can't run any more
  after the fixes, the "show the conductor window" option (would pop up windows during tests), and generated code.
  Covering them means deleting the safety nets, which would make HoloNET less robust.
- HoloNET now bundles **Holochain 0.7.1 and 0.7.0** (Embedded package), selectable with `EmbeddedHolochainVersion`.
- Windows Firewall: tests run `holochain.exe` only from `%APPDATA%\NextGenSoftware\HoloNET\0.7.0\` and `\0.7.1\`.
  Allow each once and it won't ask again.
- Still no Unity test project; the TestHarness is a manual demo app.

---

## Part E — HoloNET sites and keys (31 October 2026)

### E1. Add three DNS records for the new HoloNET sites

**Context:** HoloNET, docs.holonet.oasisomniverse.one, learn.holonet.oasisomniverse.one and sandbox.holonet.oasisomniverse.one.
**Why:** all three sites are built, deployed on Vercel and their domains are verified, but `oasisomniverse.one` DNS is hosted
at NameSilo (nameservers `ns1-3.dnsowl.com`), not Vercel, so the names don't resolve until you add the records. HTTPS
certificates are issued automatically once they do.
**Action:** at NameSilo, in the DNS records for `oasisomniverse.one`, add:

| Type | Host | Value |
|---|---|---|
| CNAME | `docs.holonet` | `805b005e3c43a61c.vercel-dns-016.com` |
| CNAME | `learn.holonet` | `d7fd0ebb133fee9e.vercel-dns-016.com` |
| CNAME | `sandbox.holonet` | `4ddb2653e3e287ed.vercel-dns-016.com` |

(The same way `holonet.oasisomniverse.one` is set up.) Then tell Claude; it checks the sites load.
**Status:** open, 31 October 2026 (sandbox record added 1 November 2026). **Details:** Vercel projects `holonet-docs-site`, `holonet-learn-site` and
`holonet-sandbox-site`; repos NextGenSoftwareUK/HoloNETDocsWebsite, HoloNETLearnWebsite and HoloNETSandboxWebsite; docs and learn are generated by
`holochain-client-csharp/scripts/build-sites.py`.

### E2. Rotate the Arbitrum testnet key and the block-explorer API key

**Context:** OASIS repo, `Providers/Blockchain/*`.
**Why:** a testnet chain private key (Arbitrum Sepolia, also reused in the Polygon, Rootstock and Web3Core configs) and a
block-explorer API key were committed. I removed them from every file on 16 October 2026 (the Arbitrum test harness now reads
`OASIS_TEST_CHAIN_PRIVATE_KEY`), but both are still in git history, so treat them as exposed.
**Action:** stop using that key (it's a testnet key, so move any test funds out first), generate a new one for local use, and
revoke/regenerate the block-explorer API key at the provider. Optionally purge history. Never paste the new values into chat.
**Status:** open, 31 October 2026.

### E3. HoloNET sandbox runs on the dev WEB4 API (decided 1 November 2026) - needs HoloOASIS working in dev
**Context:** sandbox.holonet.oasisomniverse.one should work like the web4 and web6 sandboxes: a Vercel page in front of https://dev.api.web4.oasisomniverse.one (throwaway avatar per visitor, `OASIS.Sandbox.Enabled`). The separate HoloNET sandbox service is no longer needed in production.
**Why:** the WEB4 API image (`Docker/Dockerfile.web4`) has no Holochain conductor, so the HoloOASIS provider (`ws://localhost:8888`) has nothing to connect to in dev or live. See E5.
**Action:** none for this entry beyond E5. **Status:** decided 1 November 2026; the DNS record for the sandbox host is still in E1.

### E4. Create a new NuGet API key for HoloNET 4.6.0 (the old one has expired)

**Context:** HoloNET 4.6.0, GitHub Actions workflow `publish-holonet.yml` in NextGenSoftwareUK/OASIS (secret `NUGET_API_KEY`).
**Why:** 4.6.0 is built, tested and ready (29 packages: 7 libraries x 4 conductor variants + the `dotnet new` template pack; the dry run on GitHub passed
the manifest check, packing, verification and the install smoke test). The real publish on 1 November 2026 failed at the NuGet push: nuget.org answered
403 "The specified API key has expired", for every package, so nothing was published. The currently published Client (4.5.0) cannot be installed because it
depends on a package that does not exist; 4.6.0 fixes that.
**Action:** at https://www.nuget.org/account/apikeys create a new API key (Push new packages and package versions; glob pattern `NextGenSoftware.*`), then
put it in the `NUGET_API_KEY` secret of NextGenSoftwareUK/OASIS (Settings > Secrets and variables > Actions). Never paste the key into chat. Then tell Claude;
it re-runs the publish (about 25 minutes), attaches the packages to the v4.6.0 GitHub release and checks every package is on nuget.org.
**Status:** open, 1 November 2026. **Details:** failed run https://github.com/NextGenSoftwareUK/OASIS/actions/runs/41338513590 (the step "Push to NuGet.org").

---

## Part F — COSMIC ORM / CQL demo owner decisions (31 October 2026)

Current owner decision: **1 November 2026: David explicitly authorized as many Development pushes as needed to test and fix the live demo. Do not ask again for daily-cadence exceptions for this work.** Production/master promotion and new hosting spend remain outside this authorization.
See [COSMIC current status](../COSMIC%20ORM/CURRENT_STATUS.md).

| Item | Status / David's decision | When action is needed |
|---|---|---|
| Existing development Wizard/admin session for CQL source recovery (3 November2026) | **David offered an existing Wizard account;awaiting local sign-in**. Code deployed;27controlled cases/live401+403 pass. Secure local helper and4offline checks ready. 11 November UTC: Development fd40b1ad12 still reports Hedera and Nostr unavailable. Hedera timed out waiting for response headers (15,017 ms) and Nostr returned HTTP 503 on WebSocket connect. Operator sign-in remains pending on this laptop. | In your own terminal run `python Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py` from `C:/Source/OASIS2` (current laptop), enter your existing development Wizard username/password privately,then tell the agent sign-in is complete. No password/token in chat,account creation or role promotion. Needed for admin-only Nostr recovery;agent owns tests/recovery/journalling. [Secure steps](../COSMIC%20ORM/CQL_SOURCE_RECOVERY.md#secure-local-sign-in). |
| Consolidated Development batch timing (31 October) | **Approved; published** — David replied “Publish one consolidated batch now”. Development now parent57174def3/Core6bbd015, both remote-verified; newer public-provider expansion remains on its separate task branch | One additional consolidated Development publication authorized. No master/production promotion or hosting spend. [Scope/evidence](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#consolidated-release-preflight--31-october-2026). |
| Default-routing follow-up publication (31 October) | **Approved and deployed, 1 November 2026** — David replied “Publish correction now”; Development051ac6af7 is live, Railway d8876691 SUCCESS; Default preservation and three-source failover verified for the live-found V2 controller acquisition bug.13guard+61controllerHTTPtests pass0skips; original routing fixture/3replicas preserved | First approved extra batch57174def3 deployedSUCCESS, revealing Default is bound toMongo before balancing. Dailycadence exception needed for another deployment. No provider expansion/Corepin/hosting spend. [Root cause/tests](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#live-default-routing-root-cause-and-scoped-fix--31-october-2026). |
| Expansion SDK packaging correction (1 November) | **Approved and published, 1 November 2026** - David replied Publish packaging correction now. Development89681ce40 is pushed and remote-verified; observe its exact deployment before enrollment. Expansion build failed before enrollment; the prior routing release remains live | Seven Polkadot imported modules were omitted from the publish manifest. Actual MSBuild publish metadata, five bridge closure checks and two regression tests pass. No settings, new hosting or production/master promotion. [Details](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#polkadot-publish-root-cause---1-november-2026). |
| Hosted PostgreSQL CQL demo (1 November 2026) | **Deferred by David, 1 November 2026** - David replied "Leave hosted PostgreSQL pending". Local native/paired HTTP proof is preserved; no new paid resource or account created. | No owner setup is needed now. Preserve this deferral and continue other providers; do not re-ask for access or provision hosting. Revisit only when David resumes hosted PostgreSQL. [Local proof and placement gap](../COSMIC%20ORM/POSTGRESQL_CQL_MVP.md#next-placement-work). |
| Hosted Azure Cosmos | **Deferred** — free only; no existing free subscription; David said leave hosted Azure pending | Only if David chooses to resume. No paid upgrade, card/signup request or new account creation now. |
| Hosted Oracle Database / OCI | **Deferred** — no existing Oracle account; David said leave hosting pending | Only if David resumes. Then record the actual account/connection/wallet setup needed here; request paths or variable names only, never secret values. |
| First AWS demo | **Approved** — use a public S3 dataset | Decision implemented: anonymous native metadata reads and Mongo pairing verified through hosted HTTP. No AWS account or bucket setup needed for this demo. |
| Temporary demo avatar daily calls | **Approved: 200** | Implemented/live-verified. Normal Free accounts stay at 20; token/spending limits unchanged. No further approval requested. |
| Browser access for demo verification | **Already supplied** — David opened the demo in Edge; the demo is also open in the in-app browser | The agent must resolve its control/verification tooling. Do not repeatedly ask David to open it. Record any genuinely new manual step here before asking. |

These decisions do not close unrelated full-provider, production release, device
or credential actions elsewhere in this file. No blanket cloud spending, on-chain
funding, paid account setup or production promotion is authorized here.

All future David-dependent actions from this COSMIC/CQL work belong in this list,
with date, reason, exact next step and supporting links. Do not create another
David checklist or turn code/test/automation fixes into owner setup tasks.

---

## OurWorld OLAND — Vercel team access

**Status:** no owner action currently required,1 November2026. Vercel private reads still403, but WEB4 already has a configured Stripe test key and Solana devnet wallet. Agent will reuse those existing server settings. Earlier Vercel sign-in requests can be ignored for OLAND setup.

**Action:** re-authenticate the Vercel connector to the OASIS team when a fresh consent link is provided. Earlier single-use links expire after10 minutes; do not reuse expired links or paste secrets into chat.

**Why:** private founders development Stripe settings returned403 requiring team authorization. David confirmed reuse of the founders treasury; its public wallet and development Solana/Stripe test modes are already verified. Agent-owned API/provider implementation, tests and deployment verification remain agent work.

**Details:** [OLAND payment setup and implementation status](<../Our World/oland-purchase/implementation-status.md>); [Unity task status](<C:/Source/Our-World/Docs/WIP/oland-purchase/implementation-status.md>). No private values are recorded here. Do not mark access complete until a private environment read succeeds.



OLAND clarification,1 November2026: David reported Vercel approval completed; private connector reads still returned403, but access is no longer required for OLAND. OLAND land pricing already exists in the API. Wallet conversion uses explicit public market quotes; no CoinGecko key setup action is required for OLAND development checkout. Preserve any separate credential actions for provider billing.

### E5. HoloOASIS in the WEB4 API: use HoloNET's auto-downloaded conductor (dev first, then live)
**Context:** WEB4 API on Railway (`Docker/Dockerfile.web4`, `Docker/entrypoint.onode.sh`, `Docker/oasis-dependency-versions.env`); HoloNET `main` (commit 4daa7437 or later).
**Why:** HoloNET now downloads and starts the official Holochain conductor itself on Linux (`HoloNETDNA.AutoDownloadConductor`, proven on Ubuntu, macOS and Windows runners), so the image needs no Holochain binary. The WEB4 image builds HoloNET from a pinned commit, which predates this.
**Action:** (1) approve moving the HoloNET pin in `oasis-dependency-versions.env` to that commit (GPT owns the global release, so this goes through GPT); (2) in the Railway dev service make sure the HoloOASIS section of `OASIS_DNA_JSON` uses the local node with auto-start on; optionally mount a persistent volume for the conductor cache; (3) after dev is verified, approve live. Never paste secrets into chat.
**Status:** open, 2 November 2026. **Details:** `docs/conductor.md` in the HoloNET repo.

### E6. Decide the offline (Embedded) package matrix for Linux and macOS
**Context:** HoloNET packages (`packages.json`). Embedded packages carry the conductor inside them for offline use; today they are Windows-only (Holochain 0.7.1 and 0.7.0).
**Why:** you asked for Embedded packages for every OS and conductor version. Each package holds one OS/CPU and one version, so the count depends on the matrix. Compressed binaries keep each Client package at roughly 25 to 30 MB; the ORM, API and HyperNET variants are thin and depend on the matching Client one.
**Action:** choose the CPUs: Linux x64 only, or x64 and arm64; macOS Apple Silicon only, or Apple Silicon and Intel (each extra CPU doubles that OS's packages: 16 new packages for one CPU per OS and both versions, 32 for both CPUs); and whether older Holochain versions (0.6.x, 0.5.x) get Embedded packages too.
**Status:** open, 2 November 2026.


### Our World place metadata source preference — 2 November 2026
Status: awaiting optional preference. Recommended: free-only sources first (OSM, OpenActive, venue-published calendars/structured data; optional Geoapify free quota), with provider labels and missing fields shown honestly. Alternative: allow a separately capped paid review/rating source after evaluating its map-use terms. No account/key/payment action requested yet; public Overpass is currently at capacity, an agent-owned integration/availability finding. Details: [Worldscape implementation status](<../Our World/oland-purchase/places-api-build.log>); source terms: https://www.geoapify.com/pricing/ and https://developers.google.com/maps/documentation/places/web-service/policies . This preference affects enrichment scope, not the authorized Places Visited/history implementation.

2 November2026 update: David chose **Free sources only**. Preference resolved; paid sources remain disabled. No account/key/payment request has been made. Do not re-ask this budget preference.


### Our World — existing Geoapify free-project key location
Status: missing account/configuration information,2 November2026. David approved free sources only. Identify an existing Geoapify free project/secure environment containing its key, or configure GEOAPIFY_API_KEY as a secret on existing Railway development WEB4. No payment card or paid upgrade requested. Why: no Geoapify/Places/Overpass variables were found on that service; the public Overpass source is currently at capacity. Agent continues history/UI/API implementation and owns provider wiring, quota caps and testing. [Secure setup instructions](<../Our World/worldscape-places/free-place-source-setup.md>).

2 November2026 update: David confirmed he has no Geoapify account/key. Do not re-ask where an existing key is stored. A new free account/project is the remaining optional owner setup; adapter and shared free-credit cap are agent work. Keep paid plans disabled. Continue independent Places Visited/history and open-data work.


2 November2026 update: David configured GEOAPIFY_API_KEY on Railway development WEB4. Verified key presence without logging its value. Owner account/key setup complete; API deployment and hosted source/quota checks remain agent-owned. Do not ask David to configure the key again.


## COSMIC CQL LocalFile qualification files from the newer laptop

- Resolved 2026-11-04. David authorized recreation. A separate public teaching
  cohort was created and qualified on this laptop. SDK export and WEB4 bootstrap
  parser each passed one test. The publisher accepted the replacement bundle
  and both source hashes. Recovery of the original files is no longer needed
  for the demo. The earlier cohort was not overwritten.
- [Replacement qualification and rollout](../COSMIC%20ORM/LOCALFILE_CQL_MVP.md).

## Railway access for Development WEB4 rollout from this laptop

- Resolved for the old laptop on 2026-11-07. David completed private sign-in.
  The existing CLI reads the exact Development deployment and settings.
  The Graph Studio code deployment is queued as
  `ab79d580-2412-4a70-aadc-4c0a812ec44d`. No further sign-in action is pending.

- Resolved for the recovered laptop on 2026-11-06. Its existing Railway
  session reads Development settings and deployment metadata. Its registered
  SSH identity verified a writable `/data` mount on the prior deployment.
  No sign-in action is needed for this rollout. The older laptop steps below
  are historical. [LocalFile rollout](../COSMIC%20ORM/LOCALFILE_CQL_MVP.md).

- Update 2026-11-06: the old laptop's read-only Railway check returned
  Unauthorized. At that check Development was at
  `b718f287933b7e79e862edbcf7d3bc13c37ba559`. See the LocalFile runbook
  for the separate read-only observation using the recovered session.

- Status 2026-11-04: the installed Railway CLI returned Unauthorized on the
  read-only account check. The LocalFile publisher needs authenticated access
  to read the current Development WEB4 settings and verify the exact release.
- Update 2026-11-05 UTC: the isolated candidate image build succeeded.
  Railway still returns Unauthorized on this laptop. After sign-in,
  engineering will confirm the Development WEB4 /data attachment and
  verify the exact image and settings before rollout. The registry rejected
  anonymous pulls. Engineering will check the existing Development WEB4
  registry authorization after sign-in and update this entry if package access
  requires an owner action.
  [Candidate image build](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/41948106729).
- The replacement LocalFile bundle is qualified. Sign in privately with
  Railway CLI 5.62.1 on this laptop using the existing account with Development
  WEB4 project access. From C:/Source/OASIS2 run:

  ```powershell
  & './LocalState/COSMIC/RecoveryLaptopRailway5621/node_modules/@railway/cli/bin/railway.exe' login
  ```

  Tell the agent when sign-in completes. Keep credentials out of chat and Git.
- [LocalFile qualification and rollout](../COSMIC%20ORM/LOCALFILE_CQL_MVP.md).

## CouchDB native query-role CI

- Status 2026-11-05 UTC: a scoped two-file CI branch is ready at
  `codex/couchdb-cql-reader-ci`. The GitHub integration returned HTTP 403
  when opening a draft PR. If using an account with PR access, open a draft
  to `Development` from the comparison below. The draft runs the existing
  provider matrix against a disposable CouchDB container. Leave it unmerged
  while engineering reviews the native result.
  [Open the branch comparison](https://github.com/NextGenSoftwareUK/OASIS/compare/Development...codex/couchdb-cql-reader-ci).

### Cloudflare D1 Worker for Development WEB4

Status 4 November 2026. Pending existing Cloudflare account access, after the
LocalFile rollout. Local D1/Mongo WEB4 HTTP verification passed with the
isolated symbol fixture. The hosted demo needs a dedicated development D1
database and a Worker with its DB binding on an existing account. David can
grant scoped access through the existing secure account workflow, or provide
the public HTTPS /symbols URL after deploying the reviewed Worker and schema.
Keep this free and separate from the Workers KV namespace action. Do not share
API tokens in chat. Engineering owns HTTPS qualification, Railway enrollment,
and hosted federation and replication verification.

[Worker module](../../../Scripts/Sandbox/D1SymbolWorker.mjs) ·
[Fixture schema](../../../Scripts/Sandbox/D1SymbolWorker.sql) ·
[HTTPS qualification](../../../Scripts/Sandbox/Probe-D1WorkerHostedReadiness.py).


## RavenDB local demo runtime license review — 2026-11-05

- Status: pending review before installation/use; bootstrap preparation remains agent-owned.
- Context: RavenDB bounded CQL controls pass and native read fixture compiles.
  The pinned official RavenDB.Embedded 7.0.1 package declares
  `requireLicenseAcceptance=true`; agent has not accepted a license or started it.
- Concrete owner step: review [the supplied license](../../../LocalState/COSMIC/RecoveryLaptopRavenDB701/LICENSE.txt)
  and decide whether this RavenDB runtime may be used for the owned local demo.
  No cloud account, subscription, purchase or credentials are requested.
- Reason: the package explicitly requires license acceptance before installation/use.
  [Scope and evidence](../COSMIC%20ORM/RAVENDB_CQL_MVP.md).


## Recovery laptop: enable WSL2 host prerequisites — 2026-11-05

- Status (8 November2026): device access/reboot remains deferred and no system changes were performed. Typesense native/restart and manager/HTTP/Mongo qualification now pass on the existing owned cloud runner; enabling WSL is not a prerequisite for those accepted scopes. No new cloud account was created; local Linux use remains a separate optional device decision.
- Context: Typesense's controlled CQL tests pass and native fixture compiles.
  This recovery laptop has no registered WSL distributions or standard Docker
  executable. System32/wsl.exe --status requires Virtual Machine Platform and
  firmware virtualization; Windows currently reports firmware virtualization false.
- Concrete owner steps: when ready for a reboot, check/enable virtualization in
  this laptop's BIOS/UEFI if supported. Enable Virtual Machine Platform using
  an Administrator terminal per [Microsoft's WSL2 prerequisites](https://learn.microsoft.com/en-us/windows/wsl/install-manual#step-3---enable-virtual-machine-feature),
  then reboot when your work is saved. Tell the agent when the host is ready;
  engineering will recheck and provision only one reusable Linux environment.
- Reason: BIOS/device access and a reboot window belong to David. Do not
  silently replace this with a cloud account or repeated provider environments.
  This is separate from the other laptop's older WSL disk-compaction item.
- Evidence/scope: [Typesense native gate](../COSMIC%20ORM/TYPESENSE_CQL_MVP.md).


## Algolia owned teaching source access - 2026-11-05

- Status: pending owned-source/account access; no native request or new account created.
- Context: bounded canonical-Holons CQL implementation passes31controlled tests; the read-only native qualification fixture compiles. This is not native or hosted Algolia acceptance. No Algolia process-environment credentials/source descriptor have been supplied on this laptop.
- Concrete owner step: make an owned Algolia teaching application's existing oasis_holons fixture available through a private source descriptor and an index-restricted key with search ACL. Descriptor fields are AppId, Actor, three LiveIds, one soft-deleted DeletedId, and one absent MissingId. Set COSMIC_ALGOLIA_CQL_API_KEY privately in the process environment; keep credentials outside chat and the descriptor. If no teaching fixture exists, identify the owned application and secure provisioning access so engineering can prepare it; David is not asked to write records manually.
- Reason: account selection and credentials belong to David. Engineering owns source preparation and native verification; no permission is inferred to create or purchase another account. The prepared read-only launcher makes no native writes and preserves first failures without automatic retries.
- Reviewable result and steps: [Algolia CQL native gate](../COSMIC%20ORM/ALGOLIA_CQL_MVP.md), [qualification launcher](../../../Scripts/TestHosts/Run-CosmicAlgoliaCqlNative.ps1).

## Tigris owned teaching source access - 2026-11-05

- Status: pending owner access; no native Tigris acceptance or count increment.
- Context: 14 scoped CQL/SDK/protocol controls pass; virtual-hosted addressing fixed. A read-only native fixture and opt-in launcher are prepared. No Tigris source descriptor or credentials were present in this laptop process environment.
- Concrete owner step: identify an owned Tigris teaching bucket/account and make read-restricted access available privately. An existing fixture needs three live canonical Holons, one soft-deleted and one absent identity, with original SHA256/ETag receipts. If no fixture exists, identify the owned account and secure provisioning access so engineering can prepare it; David need not create records manually. Do not open a new paid account for this step.
- Private configuration: COSMIC_TIGRIS_CQL_SOURCE is a bounded descriptor path; COSMIC_TIGRIS_CQL_ACCESS_KEY and COSMIC_TIGRIS_CQL_SECRET_KEY stay in the process environment, never chat or git. Exact descriptor fields and supported native endpoints are in the scope below.
- Reason: independent native SDK reads, CQL 3/2/0/paging and unchanged-original evidence require the owned service; MinIO or controlled SDK handlers cannot qualify Tigris.
- Reviewable result: [Tigris CQL scope and native admission](../COSMIC%20ORM/TIGRIS_CQL_MVP.md), [read-only qualification launcher](../../../Scripts/TestHosts/Run-CosmicTigrisNative.ps1).
- Agent work continues on other red scopes while access is pending. Existing LocalFile, Nostr, quota, license and device decisions remain separate preserved entries.

## MinIO hosted teaching source access - 2026-11-05

- Status: pending owned source/access; no hosted MinIO acceptance or count increment.
- Context: local native/restart/manager/Mongo/HTTP/browser proof is already accepted. WEB4 opt-in HTTPS configuration and four-preset admission now pass 17 checks, provider lifecycle/CQL 13 checks, and the hosted native read-only qualifier builds. Exact candidate Core ece17341 is published; Development remains unchanged.
- Concrete owner step: identify an existing owned reachable TLS MinIO teaching service/bucket and make its read-restricted credentials available privately. Use synthetic teaching Holons, not user data. If no fixture exists, identify the owned service and secure provisioning access so engineering can create the records/receipts; David need not create records manually. A new host/account, cost or infrastructure choice requires David's explicit decision; none has been created.
- Source qualification: descriptor fields Endpoint, Bucket, Region, Actor, three LiveIds, one DeletedId, one MissingId and four Originals (Id/Sha256/ETag) from original provisioning receipts. Private process variables COSMIC_MINIO_HOSTED_SOURCE, COSMIC_MINIO_HOSTED_ACCESS_KEY and COSMIC_MINIO_HOSTED_SECRET_KEY; no credentials in chat, descriptor or git. Native qualifier issues bounded GETs only and requires strict missing-key behavior; authorization failures remain failures.
- Reason: Development must reach a real qualified source using the restricted query key. This laptop's loopback source/root credentials cannot be used as a hosted source. Source proof, reviewed settings, exact image and deployed browser/privacy/replica acceptance are separate gates.
- Reviewable result: [MinIO hosted preparation and exact evidence](../COSMIC%20ORM/MINIO_CQL_MVP.md#hosted-registration-preparation-5-november-2026), [read-only qualification launcher](../../../Scripts/TestHosts/Run-CosmicMinioHostedNative.ps1).
- Agent work continues on other red scopes; existing LocalFile image/mount, Nostr operator, quota and deferred hosting decisions are preserved.

## Meilisearch hosted teaching source access - 2026-11-05

- Status: pending owned source access. The 17 focused WEB4 controls pass and the native HTTPS qualifier compiles.
- Owner step: identify an existing reachable HTTPS Meilisearch service for synthetic teaching records and provide restricted access privately. Limit the query key to indexes.get and documents.get on oasis_holons. If the owned service has no teaching index yet, provide secure provisioning access so engineering can prepare it separately.
- The qualifier requires three public live root records, one deleted record and one absent ID under the declared actor. Give engineering the existing origin and make the restricted API key available through the process environment. Keep keys out of chat, the descriptor and git.
- Reason: native HTTPS source proof must precede any Development settings review or hosted acceptance. The accepted Windows-local demo remains available. No new paid host or account was requested.
- [Settings, descriptor and qualification scope](../COSMIC%20ORM/MEILISEARCH_CQL_MVP.md#web4-hosted-enrollment-preparation).

## Solr hosted teaching source access - 2026-11-05

- Status: pending owned source access. The 18 focused WEB4 checks passed and the native HTTPS qualifier compiled.
- Owner step: identify an existing reachable HTTPS Solr service for synthetic teaching records and make restricted credentials available privately. Restrict the account to the unique-key schema read and RealTime Get on oasis_holons. If the owned service has no teaching collection, provide secure provisioning access so engineering can prepare it separately.
- The qualifier requires three public live root records, one deleted record and one absent ID under the declared actor. Keep the query username and password in the process environment. Do not place credentials in chat, the descriptor or git.
- Reason: native HTTPS proof precedes any Development settings review or hosted acceptance. The accepted Windows-local demo remains available. No new paid host or account was requested.
- [Settings, descriptor and qualification scope](../COSMIC%20ORM/SOLR_CQL_MVP.md#web4-hosted-enrollment-preparation).

## Elasticsearch and OpenSearch hosted teaching sources - 2026-11-05

- Status: pending owned source access. Ten shared WEB4 checks passed and both native HTTPS qualifiers compiled.
- Owner step: identify existing reachable HTTPS teaching services and provide restricted access privately. Elasticsearch requires an encoded read key for oasis-holons. OpenSearch requires a read account for oasis_holons. Each source needs a live probe and three public roots, one deleted record and one absent ID under its declared actor. If an owned service lacks the teaching index, provide secure provisioning access for engineering to prepare it separately.
- Reason: each vendor's native HTTPS proof precedes Development settings review and hosted acceptance. Keep credentials in the process environment, outside chat, descriptors and git. Existing local demos remain available. No new paid resources were requested.
- [Elasticsearch qualifier](../COSMIC%20ORM/ELASTICSEARCH_CQL_MVP.md#web4-hosted-enrollment-preparation) and [OpenSearch qualifier](../COSMIC%20ORM/OPENSEARCH_CQL_MVP.md#web4-hosted-enrollment-preparation).

## ArcadeDB hosted teaching source access - 2026-11-05

- Status: pending owned HTTPS source access. Seven WEB4 checks passed and the native HTTPS qualifier compiled.
- Owner step: identify an existing reachable ArcadeDB HTTPS service for synthetic teaching records and make credentials available privately. Use a restricted reader for the oasis database. If the teaching source does not exist, provide secure provisioning access so engineering can prepare it separately.
- The source needs three public live roots, one deleted record and one absent ID under the declared actor. Keep credentials in the process environment, outside chat, descriptors and git. No new host or paid resources were requested.
- Reason: native HTTPS proof precedes settings review and hosted acceptance. [Settings and fixture](../COSMIC%20ORM/ARCADEDB_CQL_MVP.md#web4-hosted-enrollment-preparation).

## SurrealDB hosted teaching source access - 2026-11-05

- Status: pending owned HTTPS source access. Nine WEB4 checks passed and the native HTTPS qualifier compiled.
- Owner step: identify an existing reachable SurrealDB HTTPS service with an explicit namespace and database for synthetic teaching records. Make a database VIEWER account available privately. If the teaching catalogue does not exist, provide secure provisioning access so engineering can prepare it separately.
- The source needs three public roots, one deleted record and one absent ID under its declared actor. Use a live record as the probe. Keep credentials in the process environment, outside chat, descriptors and git. No new host or paid resources were requested.
- Reason: native HTTPS proof precedes settings review and hosted acceptance. [Settings and fixture](../COSMIC%20ORM/SURREALDB_CQL_MVP.md#web4-hosted-enrollment-preparation).

## Qdrant hosted teaching source access - 2026-11-05

- Status: pending owned HTTPS source access. Four activation checks and seven WEB4 checks passed. The native HTTPS fixture compiled.
- Owner step: identify an existing reachable HTTPS Qdrant service for synthetic teaching records and make a key restricted to reads on oasis_holons available privately. If the teaching collection does not exist, provide secure provisioning access so engineering can prepare it separately.
- The source needs three public live roots, one deleted point and one absent ID under its declared actor. Use a live record as the probe. Keep the key in the process environment, outside chat, descriptors and git. No new paid resource was requested.
- Reason: native HTTPS proof precedes settings review and hosted acceptance. [Settings and fixture](../COSMIC%20ORM/QDRANT_CQL_MVP.md#web4-hosted-enrollment-preparation).

## Redis and Valkey hosted teaching sources - 2026-11-05

- Status: pending owned TLS sources. Five controlled encrypted-probe and twelve WEB4 admission checks passed. The two native TLS qualifiers compiled and require reviewed teaching sources.
- Owner step: identify existing dedicated standalone Redis and Valkey teaching instances reachable from Development WEB4, with public IP endpoints and matching TLS certificate hostnames. Make the restricted reader credentials and existing teaching encryption key available through protected settings. If an owned instance lacks a teaching catalogue, make separate secure provisioning access available for engineering.
- Each source uses database zero and needs three public live roots, one deleted record and one absent ID under the declared actor. Choose a live root as the probe. Restrict the reader to the required native SDK handshake and encrypted teaching-record reads. No new host or paid resource is requested.
- Reason: native TLS proof for each product precedes WEB4 settings review and hosted acceptance. [Redis settings and fixture](../COSMIC%20ORM/REDIS_CQL_MVP.md#development-web4-candidate) and [Valkey settings and fixture](../COSMIC%20ORM/VALKEY_CQL_MVP.md#development-web4-candidate).

## DynamoDB hosted teaching source access - 2026-11-05

- Status: pending owned AWS source and approval for the bounded native reads. Six regional activation and seven WEB4 admission checks passed. The native AWS fixture compiled and awaits the reviewed source.
- Owner step: identify the region of an existing isolated DynamoDB teaching table named OasisHolons, confirm the approved scope for billable reads and arrange an IAM reader restricted to GetItem and BatchGetItem on that table. Make credentials available through protected settings. If the teaching records are absent, make separate reviewed provisioning access available for engineering.
- The catalogue needs three public live roots, one deleted record and one absent ID under the declared actor. Use one live ID as the exact activation probe. Keep access keys and session tokens outside chat, descriptors and git.
- Reason: the real regional SDK proof precedes Development settings review and hosted acceptance. The retained DynamoDB Local evidence is separate. No new table, account or paid resource was requested. [Settings and fixture](../COSMIC%20ORM/AWS_CQL_MVP.md#development-web4-dynamodb-candidate).

## Typesense hosted teaching source access - 2026-11-06 UTC

- Status: 17 WEB4 admission checks passed. The guarded native HTTPS fixture compiled and awaits an existing reviewed source.
- Owner step: if there is already an owned Typesense teaching service, identify its HTTPS origin and make a restricted key available privately for collection metadata and exact document reads on oasis_holons. Use a dedicated teaching collection with three public roots, one deleted record and one absent ID under the declared actor.
- Reason: real Typesense qualification precedes WEB4 settings review. The existing WSL device action remains separate. No cloud account or new paid service is requested. [Typesense settings and fixture](../COSMIC%20ORM/TYPESENSE_CQL_MVP.md#development-web4-candidate).

## PocketBase hosted teaching source access - 2026-11-06 UTC

- Status: 26 provider and eight WEB4 admission checks passed. The native HTTPS qualifier compiled.
- Owner step: identify an existing dedicated PocketBase HTTPS teaching service and provide a restricted record reader token privately. Scope its collection rules to the teaching objects in oasis_holons. Keep administrator credentials and the original local demo separate.
- The catalogue needs three public live roots, one deleted object and one absent ID under the declared actor. Use a live root as the probe. No new paid service or account is requested.
- Reason: native HTTPS qualification precedes Development settings and browser acceptance. [Settings and qualification](../COSMIC%20ORM/POCKETBASE_CQL_MVP.md#development-web4-candidate).

## Supabase hosted teaching source access - 2026-11-06 UTC

- Status: 36 provider and eight WEB4 admission checks passed. The native HTTPS qualifier compiled.
- Owner step: identify an existing dedicated Supabase teaching project. Provide its HTTPS origin and public gateway key, and arrange a separate restricted authenticated reader JWT in protected settings. Review the token's expiry and row policies for the dedicated teaching principal.
- The catalogue requires three public live roots, one deleted row and one absent ID under the declared actor. Use a live record as the probe. No new paid service or account is requested.
- Reason: native gateway and row-policy qualification precede Development settings and browser acceptance. [Settings and qualification](../COSMIC%20ORM/SUPABASE_CQL_MVP.md#development-web4-candidate).

## Firebase hosted teaching source access - 2026-11-06 UTC

- Status: 16 provider and eight WEB4 admission checks passed. The native HTTPS qualifier compiled.
- Owner step: identify an existing dedicated Realtime Database teaching source and its Firebase project ID. Arrange a restricted Firebase Authentication ID token for the teaching reader through protected settings. Review token expiry, database Security Rules and deployed request instrumentation before use.
- The catalogue needs three public live roots, one deleted record and one absent ID under the declared actor. Choose a live root as the probe. No new paid service or account is requested.
- Reason: native HTTPS and rules qualification precede Development settings and browser acceptance. [Firebase configuration and fixture](../COSMIC%20ORM/FIREBASE_CQL_MVP.md#development-web4-preparation).

## Fastly KV hosted teaching store access - 2026-11-06

- Status: seven Fastly provider and seven WEB4 admission checks passed. The native verifier compiled. No credentialed request has run.
- Owner step: identify an existing dedicated Fastly teaching account and store. Review whether a read-only token can access the KV item route within the approved account boundary. Fastly notes that service restrictions do not constrain non-service endpoints, so service restriction alone is insufficient. If the boundary is approved, arrange the token through the protected `CQL_FASTLY_QUERY_READER_TOKEN` setting.
- The reviewed catalogue requires three public live roots, one deleted record and one absent ID under the declared actor. Use a live root as the probe. No new paid resource is requested.
- Reason: native 3/2/0 and unchanged item generation checks precede Development enrollment. [Fastly evidence](../COSMIC%20ORM/FASTLY_KV_CQL_MVP.md) · [Token scopes](https://www.fastly.com/documentation/reference/api/auth-tokens/user/).

## Deno KV managed teaching database access - 2026-11-06

- Status: 11 bounded CQL profile checks passed and the native verifier compiled. The current managed connector returned HTTP 401 to the unauthenticated probe. No credentialed request has run.
- Owner step: identify an existing dedicated managed Deno KV teaching database in Deno Deploy and its database UUID. Confirm the allowed scope of the personal or organization credential before placing an approved token in `CQL_DENO_KV_QUERY_READER_TOKEN` through protected settings. Deno's guide documents credential types, so the actual privileges still need review.
- The catalogue needs three public live roots, one deleted record and one absent ID under the declared actor. Engineering owns confirming the strong data endpoint, exact probe, native counts and protected WEB4 admission. No new paid resource is requested.
- Reason: credentialed native proof is required before Development enrollment. [Deno KV evidence](../COSMIC%20ORM/DENO_KV_CQL_MVP.md) · [Managed connection guide](https://docs.deno.com/deploy/reference/deno_kv/#connect-from-outside-deno-deploy).

## Cloudflare Workers KV teaching namespace access - 2026-11-06

- Status: two bounded provider tests passed and the native verifier compiled. No credentialed request has run.
- Owner step: identify an existing dedicated Workers KV teaching namespace containing only public demo records. Confirm the account scope of a token with Workers KV Storage Read and provide the approved token through `CQL_CLOUDFLARE_KV_QUERY_READER_TOKEN` in protected settings. No Worker, paid resource or new account is requested.
- Engineering owns confirming the exact account and namespace IDs, three stable public records, one deleted record, one absent key and native 3/2/0 results before WEB4 enrollment.
- Reason: verify native access and missing-key behavior in the reviewed account. [Cloudflare Workers KV evidence](../COSMIC%20ORM/CLOUDFLARE_KV_CQL_MVP.md) · [Read permission](https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/).

## Migrated Vercel KV teaching store access - 2026-11-06

- Status: two CQL profile checks and two storage regressions passed. The native verifier compiled. No credentialed request has run.
- Owner step: if an existing dedicated Vercel KV teaching store was migrated to Upstash, identify its approved HTTPS endpoint and arrange its read-only token through `CQL_VERCEL_KV_QUERY_READER_TOKEN` in protected settings. No new store or paid resource is requested.
- Engineering owns checking the original `oasis/holon/{id:N}` layout, three public roots, one deleted record, one absent key and native 3/2/0 before Development enrollment.
- Reason: this provider identity uses a separate layout from the accepted Upstash cohort. [Migrated Vercel KV evidence](../COSMIC%20ORM/VERCEL_KV_CQL_MVP.md) · [Upstash read-only token](https://upstash.com/docs/redis/features/restapi).

## Appwrite DocumentsDB teaching project access - 2026-11-06

- Status: five CQL profile checks, 14 storage regressions and nine WEB4 admission checks passed. The native HTTPS verifier compiled. Credentialed verification is open.
- Owner step: identify an existing dedicated Appwrite teaching project and its HTTPS endpoint, project ID and database ID. Review the current `documentsdb.documents.read` scope and arrange a reader key through protected `CQL_APPWRITE_QUERY_READER_TOKEN` settings. No new project or paid resource is requested.
- Engineering owns verifying the unique `oasis_id` index, three public roots, one tombstone, one absent key and the native 3/2/0 and unchanged payload checks before Development enrollment.
- Reason: the key is scoped to document reads in its project, so the reviewed teaching project and exact catalogue must be confirmed. [Appwrite evidence](../COSMIC%20ORM/APPWRITE_CQL_MVP.md) · [API key scopes](https://appwrite.io/docs/partners/project/api-keys#scopes).

## Convex teaching deployment access - 2026-11-06

- Status: the isolated Windows backend passed the companion HTTP probe. All 20 adapter and Function API tests passed. Hosted proof is open and follows LocalFile.
- Owner step: identify an existing dedicated Convex teaching project and its approved HTTPS site. Arrange project access to deploy the tracked teaching companion and set the dedicated reader token in the Convex deployment and protected WEB4 settings. Keep the token and deployment credentials private. No new paid resource is requested.
- Engineering owns seeding three public roots, a tombstone and a private control, reserving one absent key, verifying the native CQL 3/2/0 and unchanged public responses, and reviewing the exact settings before WEB4 enrollment.
- Reason: the existing Convex deployment must be approved for the standalone teaching schema and synthetic data. [Convex evidence](../COSMIC%20ORM/CONVEX_CQL_MVP.md).

## Turso libSQL teaching database access - 2026-11-06

- Status: ten controlled checks and one real Linux libSQL CQL check passed. Managed HTTPS acceptance remains open and follows LocalFile.
- Owner step: identify an existing dedicated Turso libSQL teaching database and approved HTTPS origin. Arrange a database-scoped read-only token through protected `CQL_TURSO_QUERY_READER_TOKEN` settings. No new paid resource is requested.
- Engineering owns verifying the reviewed actor, three public roots, tombstone and absent ID with the managed endpoint, then confirming WEB4 results before enrollment.
- Reason: the local libSQL result does not identify or authorize a managed database. [Turso evidence](../COSMIC%20ORM/TURSO_CQL_MVP.md) · [Turso database token scopes](https://docs.turso.tech/api-reference/databases/create-token).

## Milvus hosted teaching collection access - 2026-11-06

- Status: native Milvus 2.6.26 returned 3/2/0 and denied the private record. Six guarded provider and ten WEB4 admission checks passed. The hosted verifier compiled. No credentialed hosted request has run.
- Owner step: identify an existing dedicated Milvus HTTPS teaching deployment. Arrange a dedicated reader with Query access limited to `oasis_holons` in the default teaching database and provide its token privately through `CQL_MILVUS_QUERY_READER_TOKEN`. Keep credentials out of chat and Git. No new paid deployment or admin token is requested.
- Reason: engineering will review the scoped grant, bind the reviewed synthetic fixtures and verify exact native reads and private denial before Development enrollment. LocalFile remains first. [Milvus evidence](../COSMIC%20ORM/MILVUS_CQL_MVP.md) · [Milvus grants](https://milvus.io/docs/grant_privileges.md).

## pgvector hosted teaching database access - 2026-11-06

- Status: native PostgreSQL 17.11 and pgvector 0.8.7 passed 3/2/0, private denial, and checks rejecting an overprivileged role or non-vector table. Opt-in WEB4 admission passed. The hosted verifier compiled. No credentialed hosted request has run.
- Owner step: identify an existing dedicated pgvector teaching database at a trusted TLS hostname on port 5432. Arrange a dedicated role with SELECT on `public.oasis_holons` ID and document columns and no write or schema-creation privileges. Provide its password privately through `CQL_PGVECTOR_QUERY_READER_PASSWORD`. No paid service or admin credentials are requested.
- Reason: engineering will bind the reviewed database and role to the catalogue and verify its TLS identity, vector schema and effective grants before Development enrollment. LocalFile remains first. [pgvector evidence](../COSMIC%20ORM/PGVECTOR_CQL_MVP.md) · [PostgreSQL privilege functions](https://www.postgresql.org/docs/17/functions-info.html).

## Web3.bio protected access for the ENS Offchain CQL teaching source

- Status 2026-11-06: open after LocalFile. The local WEB4 federation rehearsal
  recorded a real upstream HTTP 429 on the anonymous public index. Two
  acknowledged private Mongo records are retained for read-only continuation.
- Use an existing Web3.bio API key if the project has one. Otherwise request a
  free key through the provider's published support channels. Keep the value in
  the approved secret store and install it as the protected Development WEB4
  setting `CQL_ENS_OFFCHAIN_QUERY_API_KEY` when the ENS rollout is scheduled.
  Share only confirmation that access is ready. [Web3.bio authentication](https://api.web3.bio/#api-key-and-authentication).
- Engineering owns the authenticated native reads, retained fixture
  reconciliation, and hosted privacy and federation checks.
  [ENS qualification](../COSMIC%20ORM/ENS_OFFCHAIN_CQL_MVP.md).

## Safe Transaction Service protected key for the Development teaching profile

- Status 2026-11-06: open after LocalFile. The anonymous native test passed
  exact 3/2 CQL reads against Ethereum, Base and Arbitrum. The public source
  reports index synchronization and does not need a wallet or signing key.
- Use an existing approved Safe API key for Development, or create one
  through the Safe developer dashboard on the free Builder plan. Store it
  only as the protected `CQL_SAFE_INDEXING_QUERY_API_KEY` setting when the
  Safe Development rollout is scheduled. Confirm only that access is ready.
  [Authentication](https://docs.safe.global/core-api/how-to-use-api-keys) and
  [Safe Builder plan](https://safe.global/plans).
- Engineering owns the reviewed network catalogue, admission, usage bounds,
  authenticated verification and the hosted demo.
  [Safe indexing qualification](../COSMIC%20ORM/SAFE_INDEXING_CQL_MVP.md).

## Apache Druid protected datasource access for Development WEB4

- Status 2026-11-06: open after LocalFile. Druid 38.0.0 passed native
  3/2/0 against the disposable teaching datasource. Hosted qualification
  needs a separate approved Druid HTTPS endpoint and scoped reader.
- Identify an existing approved Druid deployment for this demo. On its
  Basic Security authorizer, grant a dedicated reader only
  `DATASOURCE READ` for `^oasis_cql_public_holons$`. Install the reader
  username and password in the protected Development settings
  `CQL_DRUID_QUERY_READER_USERNAME` and `CQL_DRUID_QUERY_READER_PASSWORD`
  when scheduled. Share only the approved HTTPS origin and readiness.
  [Druid authorization](https://druid.apache.org/docs/latest/operations/security-user-auth/).
- Engineering owns the synthetic dataset, reader denial checks, source
  enrollment and the end-to-end WEB4 test.
  [Druid qualification](../COSMIC%20ORM/DRUID_CQL_MVP.md).

## Apache Pinot protected broker access for Development WEB4

- Status 2026-11-06: open after LocalFile. The official Pinot 1.6.0
  runner passed public exact-key CQL 3/2/0. Hosted verification needs
  an existing approved HTTPS broker and a scoped reader.
- Identify the approved Pinot deployment. Assign a dedicated broker
  principal to only `oasis_cql_public_holons` and set its row-level
  predicate for that table to `is_public = 1`. Put the username and
  password in protected `CQL_PINOT_QUERY_READER_USERNAME` and
  `CQL_PINOT_QUERY_READER_PASSWORD` when the rollout is scheduled.
  Share only the approved HTTPS broker origin and readiness.
  [Pinot broker authentication](https://docs.pinot.apache.org/operate-pinot/security/authentication/basic-auth-access-control)
  and [row-level security](https://docs.pinot.apache.org/operate-pinot/security/access-control).
- Engineering owns the teaching fixture, ACL and private-row denial
  verification, admission and the hosted demo.
  [Pinot qualification](../COSMIC%20ORM/PINOT_CQL_MVP.md).

## Temporal Cloud read access for Development WEB4

- Status 2026-11-06: open after LocalFile. The official SDK passed the
  native exact-run test, and the Cloud namespace binding passed its
  control. Hosted acceptance needs a dedicated approved teaching namespace.
- Identify an existing approved Temporal Cloud namespace in
  `namespace.account` format. Use a service account with read-only
  access scoped to that namespace and store its API key in protected
  `CQL_TEMPORAL_QUERY_API_KEY` when the rollout is scheduled. Share only
  the approved namespace and readiness.
  [Temporal account access](https://docs.temporal.io/cloud/manage-access)
  and [API key connection](https://docs.temporal.io/cli/cloud).
- Engineering owns the synthetic run catalogue, scope and denial checks,
  admission and live federation. [Temporal evidence](../COSMIC%20ORM/TEMPORAL_CQL_MVP.md).

## Transfer the qualified PouchDB source files from the old laptop

- Open 2026-11-06. The recovered laptop lacks the three original PouchDB
  publisher inputs. Copy the bundle, qualification and bootstrap receipt
  using the exact relative paths in the [PouchDB handoff instructions](../COSMIC%20ORM/POUCHDB_HOSTED_RUNTIME_PREPARATION.md#recovered-laptop-input-check).
- This transfer enables the next Development demo after LocalFile. Keep
  credentials, protected settings and the database on the old laptop.
  Engineering will verify the original hashes before preparing settings.


## Gelato Gasless native capability access — deferred (8 November 2026)

Status: deferred; no action requested during the current four-provider demo batch.
The supported read-only client passes34controlled tests, but neither this laptop
nor WEB4 Development has a Gelato-named credential setting. Native supported-chain
records cannot be certified from controlled responses.

When this provider reaches the live-demo queue, David's account/access decision
is needed: use an authorized Gelato Cloud development key with capability-read
access and provision it privately as `GELATO_GASLESS_API_KEY` in the agreed dev
secret store; never paste it into chat or commit it. If no account/key is available,
preserve this deferred gate. No funding, sponsorship, signing or transaction
submission is requested or authorized by this entry.

[Exact SDK contract and34case proof](../COSMIC%20ORM/GELATO_PUBLIC_SOURCE_STATUS.md).
[Gelato Cloud account](https://app.gelato.cloud/).


### TRON priority demo dedicated TronGrid reader access — 8 November 2026

- Remote qualification option (10 November 2026). The existing [TRON CI run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42840387422) passed 38 controlled cases on matching TRON inputs. If the dedicated reader key is available for repository CI, configure it privately as the Actions secret `COSMIC_TRON_PUBLIC_READER_KEY` and let engineering dispatch the existing authenticated-native workflow. This avoids rebuilding the local test bundle.

- Status (8 November 2026): David selected use existing private configuration. Agent checked the retained WEB4 baseline, current Development variables (including nested DNA), local WEB4 settings/default DNA and prior TRON qualification inputs without printing values; no TronGrid reader credential found. Exact existing private configuration location remains needed; ignored JSON-with-comments Core/WEB4 DNA, WEB4 user-secret store and repository secret-name checks also found no reader; organization metadata403 was not retried. Additional read-only Development WEB5–WEB10 setting-name and nested-DNA checks all succeeded with zero reader-key candidates; no values retained and no service changes. Three existing sibling OASIS checkouts were also checked read-only:32configuration files inventoried,31parsed/no reader candidates; one invalid old master DNA template remains unparsed and was not used as absence proof. Existing WEB4/WEB5 production settings were also checked read-only, including WEB5 nested JSON-with-comments DNA, with no reader candidate and no service change or retained value. No native authenticated request made. Priority engineering continues.
- Context: prior anonymous TronGrid activation/range attempts returned429 and the separate PublicNode diagnostic returned403. Preserve those failures; do not retry or switch endpoints after failure. The recovered authenticated reader draft has38controlled passes recorded in [TRON source status](../COSMIC%20ORM/TRON_PUBLIC_SOURCE_STATUS.md), but authenticated native/CQL/WEB4 acceptance remains open.
- Concrete action: configure a dedicated existing TronGrid reader API key privately on this laptop as the user environment setting `COSMIC_TRON_PUBLIC_READER_KEY`, or identify the existing private configuration location. Do not paste the key into chat or commit it. Tell the agent when ready. [Official TronGrid access setup](https://developers.tron.network/reference/select-network).
- Reason: exact process/user/machine checks found no `COSMIC_TRON_PUBLIC_READER_KEY`, `TRONGRID_API_KEY` or `TRON_API_KEY`. Native authenticated qualification cannot be proved without legitimate reader access. Header reads do not require wallet funding or transaction signing; any broader write authority remains outside this request.
- Agent work (8 November 2026): scoped source passed53controlled checks; exact scoped image/package and real API startup/owned cleanup verified in [current status](../COSMIC%20ORM/CURRENT_STATUS.md). No native TRON request or live enrollment has been made. Preserve the original cohort and separate recovered catalogue. Exact existing reader location remains pending; no local SDK/runtime install or failed anonymous endpoint replay.


## Typesense owned Development teaching source — 8 November 2026

- Status: deferred behind the priority-provider live queue; no new access question or account request now. Local/cloud native and manager/HTTP/Mongo scopes passed; live enrollment remains open.
- Context: disposable cloud qualification42497834056/42498399586 verified real Typesense29.0 reads, unchanged-source restart, manager/HTTP5/4 and restricted-reader write denial. Those owned test runtimes/data were removed, so they are not a live WEB4 source.
- Concrete owner step when this queue is reached: identify an existing owned reachable HTTPS Typesense service and make its restricted reader available privately. Grant only collections:get and documents:get on oasis_holons. If no teaching collection exists, identify authorized provisioning access separately so engineering can prepare three public roots, one deleted control and one absent ID under a declared actor. Never paste or commit keys. No new paid setup is assumed.
- Reason: live WEB4 needs a durable authorized HTTPS native source; controlled cloud tests and compiled registration do not establish that access. Engineering owns bounded fixture preparation and native/live verification after access is available.
- Evidence and exact remaining scope: [Typesense CQL MVP](../COSMIC%20ORM/TYPESENSE_CQL_MVP.md). Deployed settings use CQL_TYPESENSE_QUERY_ORIGIN and CQL_TYPESENSE_QUERY_API_KEY; preserve current provider decisions/cohorts.


## Marqo current commerce teaching source — 8 November2026

- Status: deferred behind the priority-provider live queue; no access/account question now. The existing legacy unstructured adapter has26controlled passes but no native/current/live acceptance.
- Context: pinned upstream README explicitly deprecates open source. Its old Holon document-envelope contract does not prove the current commerce API. No historical runtime/model/image, new account or paid resource was created.
- Concrete owner step when queued: identify an existing authorized current Marqo commerce teaching index and its existing private reader configuration, without posting keys. Engineering will first qualify the documented current read contract, approved catalogue and least-privilege credentials; do not assume an old unstructured endpoint or shared API token is compatible. If access does not exist, retain the deferment; no paid signup is assumed.
- Reason: current native/live proof requires authorized real source records. The original bounded legacy implementation and controlled tests cannot stand in for those records or current commerce semantics. A historical runtime demo would be explicitly labelled and separate from current-service acceptance.
- Evidence: [Marqo scope and pinned lifecycle statement](../COSMIC%20ORM/MARQO_CQL_MVP.md). Engineering/API-contract failures remain agent-owned; this entry requests no action ahead of the priority queue.


## Infura Ethereum CQL access · 8 November 2026

- **Pending authorized reader access.** For native qualification, identify an
  existing Ethereum mainnet Infura key with API secret authentication enabled.
  Configure the key and secret privately as `CQL_INFURA_API_KEY` and
  `CQL_INFURA_API_SECRET` for the qualification environment. Share only the
  location or readiness. This enables finalized public balance reads.
- Process and user environment checks found neither configured credential.
  See [Infura qualification](../COSMIC%20ORM/INFURA_PUBLIC_SOURCE_STATUS.md)
  and [Infura key security](https://docs.infura.io/dashboard/how-to/secure-an-api/api-key-secret/).

### Recovery laptop power, 11 November UTC

Open. Windows reported 6% battery and discharging. Connect the charger and close the low-battery alert if it persists. The alert blocks Edge input with the Fhenix two-network preset selected and unrun. The full preset already passed.

Power recovery verified on 11 November UTC: battery 21% and charging (Win32_Battery status 2). The prior Edge window is closed. Engineering owns reopening browser verification. The charger action is resolved.
