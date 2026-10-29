# Actions for David

The single list of everything that needs you: your accounts, phones, admin rights, approvals or a decision. Everything else is done or in progress. Tick items off and tell Claude when one is done; it picks up the next step. Never paste passwords, API keys, tokens or connection strings into chat.

Last updated 28 October 2026. This file replaces `OWNER_SETUP_CHECKLIST.md` (merged into Part B below) and the PIPER work log's action list (Part A).

## Open actions at a glance

| # | Action | Part |
|---|---|---|
| 1 | **Publish the fixed OASIS npm SDKs** (fixes PIPER "Save failed: Stable operation intent…") | A1 |
| 2 | Set up npm trusted publishing for the 7 SDK packages (one-time) | A2 |
| 3 | PIPER: set `SESSION_PLAN_GITHUB_TOKEN` on piper-server, check the screens, tell Hailey it's ready for Gate 2 | A3 |
| 4 | Release gate: Railway tokens, staging Mongo user, staging connection strings | B |
| 5 | OurWorld Unity Cloud access | B |
| 6 | Provider acceptance access: ActivityPub, EVM decision, AWS S3, Solana devnet funding, Oracle Cloud, CoinGecko key | C |
| 9 | Holon permissions: publish web4-api 2.0.17, then click Reapply access once Railway has deployed | A5 |
| 8 | Railway pin sync (one-time): enable the pre-push hook in each OASIS clone | A4 |
| 7 | Older items: HoloNET NuGet secrets, phone tests, WSL disk, Vercel branch check, oasisweb4 site decision, iOS runtime script, Git LFS budget, upstream Holochain PRs | D |

---

## Part A — npm SDKs and PIPER (added 28 October 2026)

### A1. Publish the fixed OASIS npm SDK packages

**Why:** OASIS HyperDrive V2 rejects writes without a stable operation id, which broke every PIPER save. The fixes are committed and pushed but not on npm, because publishing needs your 2FA.

| Package | Version | Change |
|---|---|---|
| `@oasisomniverse/web4-api` | 2.0.15 | `X-COSMIC-Operation-Id` on every write + `Idempotency-Key` on every request (same id on writes) |
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

### A4. Railway pin sync (one-time, added 28 October 2026)

**Why:** Railway builds the private submodules at the commits in `Docker/oasis-dependency-versions.env`, not at the parent's gitlinks. A Core fix pushed without moving that pin never deployed (it cost a day). `sync-railway-pins.yml` now corrects the pins after each push to `Development`/`master`, and `.githooks/pre-push` stops a mismatched push before it deploys.

- [ ] **Enable the hook** in each OASIS clone you push from (once per clone): `git config core.hooksPath .githooks`
- No new secret is needed: the workflow pushes with the existing `PRIVATE_SUBMODULE_PAT`.

Details: `Docs/Devs/RAILWAY_DEPENDENCY_PINS.md`.

---

### A5. Holon permissions rollout (29 October 2026)

**Why:** PIPER staff (SLP, Head of School) could not read the records the System Administrator created, which broke
their sign-in. OASIS now has per-holon read, write, delete, add-children and manage permissions, and PIPER uses them.
See `Docs/Devs/HOLON_PERMISSIONS.md`.

- [ ] **Wait for Railway** to deploy the OASIS API from Development (root commit `6b32f6116` or later, Core `bceaee8`). Until then PIPER saves show "who can see it could not be set".
- [ ] **Publish `@oasisomniverse/web4-api` 2.0.17** (`npm login` then `npm run publish-all`). It adds `share.setPermissions` and `share.getPermissions`. piper-server does not need it yet (it calls the endpoint directly), but other apps will.
- [ ] **Sign in as the SA, open Staff Management and press Reapply access** once. It sets permissions on every existing district, school, staff and student record. A toast says how many were updated.
- [ ] Sign in as an SLP (e.g. david@piperspeech.com) and check the dashboard loads their students with no "not linked" error.

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

## Part D — Older owner actions (14–15 October 2026)

Things only you can do: they need your phone, your accounts, admin rights or a decision.
Everything else is done or in progress. Last updated 2026-10-14.

Tick them off as you go; tell Claude when one is done and it will pick up the next step.

---

### 1. Publish HoloNET 4.1.0 to NuGet

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

### 3. Shrink the WSL disk (gets ~10+ GB back)

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

Optional, your call: `miniforge3` (33 GB), ML models (2.6 GB) and two PyTorch wheels (1.6 GB) in WSL aren't from this work.
Delete them before compacting if you don't need them.

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
