2026-11-19 — ATLAS/DEVELOPMENT CLARIFICATION COMPLETE: David clarified that Development updates were never frozen by his Atlas load restriction. Heavy database qualification stays isolated; small paced Atlas checks and verified Dev rollouts are authorized. Earlier cloud/stand-down wording below is historical and superseded on this point; no further owner confirmation or repeated sign-in is requested. Staging/production approval and PIPER data protections remain.

2026-11-18 — CLOUD ACTIONS SUPERSEDED / DEFERRED BY DAVID: cloud migration is abandoned and work resumes locally with isolated remote CI. This supersedes CLOUD-CQL-ACCESS-PREFLIGHT-2026-11-18 and CLOUD-CQL-HANDOFF-START-2026-11-18 for the current task; no cloud setup or repeated sign-in is requested. The latest named cloud chat reports Core read restored, startup still failed without a task ID. Historical access failures below are retained. Full roadmap and shared-cluster/deployment stand-down remain intact.

## COSMIC-M20-TPT-DEFINITION-2026-11-25 - Decide what table-per-type inheritance means on document/graph stores

Status: OPEN (2026-11-25). COSMIC checklist row M20 (Docs/WIP/COSMIC ORM/CQL_PARITY_CHECKLIST.md) says a document/graph TPT equivalent "needs a faithful definition first". M19 (discriminator, one record per entity) is done on MongoDB, SQLite and Neo4j. M21 depends on this decision too.

Choose one:
1. Split storage: each inheritance level's declared properties live in their own record (collection/table/label per level), joined by the entity id on read and written in one unit of work.
2. Treat M20 as satisfied by M19 (one record per entity) and mark it equivalent rather than separately built.

Reply with 1 or 2. The claim is released on the claims branch until then.

## GITHUB-ACTIONS-RUNNER-CAPACITY-2026-11-25

Status: open, 25 November 2026. GitHub Actions in NextGenSoftwareUK/OASIS runs only 2-3 jobs at a time while 40-130 runs queue, so CI-verified items (native-storage suites, the provider TestHarness gate, CQL/COSMIC checks) wait hours. Agents now batch pushes and avoid new runs while more than about 20 are queued, which slows verification further.

What only you can do (GitHub organisation settings, may cost money):
- Check Settings > Billing and plans for NextGenSoftwareUK: Actions minutes used vs included, and the Actions spending limit. Exhausted minutes or a zero spending limit throttle private-repo jobs.
- Choose one: raise the spending limit or plan concurrency, add larger or self-hosted runners, or tell agents to run heavy suites only on workflow_dispatch.

Unblocks: every queued native-storage suite (currently Zcash) and all other sessions' CI gates.

## CLOUD-CQL-ACCESS-PREFLIGHT-2026-11-18

Status: blocked, 18 November 2026. Current runtime is Windows C:/Source/OASIS2, not a verified cloud checkout; no persistent-goal tool is exposed. GitHub connector reads OASIS/Web4Site, but private OASIS-API-Core returns 404. Connector write to this existing feature branch returns 403 Resource not accessible by integration. Native git access failed with getaddrinfo thread error. These are access/tooling limits; no credentials requested.

Owner step: ensure the actual cloud environment and its GitHub connection can read OASIS-API-Core and write the existing task branches; start the full handoff there. Engineering must verify cloud ownership and repository access before claiming transfer or pausing prior goal ownership. No local builds, database installs, live calls or deployments. Full roadmap and stand-down remain unchanged. The preflight session could not publish this record through its connector (403). This original session integrates the named record through its existing native Git path, preserving unrelated work. Handoff: Docs/WIP/COSMIC ORM/CLOUD_HANDOFF_2026-11-18.md.

Clarification,18 November2026: runtime placement and credentials are separate gates. The ordinary ChatGPT GitHub connector is documented as read-only; selecting the private Core repository there does not establish push capability. Use an actual Cloud coding task and its coding/Git connection. Authorize the required organization repositories (OASIS, OASIS-API-Core, Web4Site and pinned private dependencies) with the coding access needed for scoped existing-branch commits. Do not paste credentials into chat or copy a local credential store into the handoff. Verify the remote runtime, Core pinned-ref read and coding write access before recording transfer. A missing persistent-goal tool is also a separate capability limit, not something a prompt can enable.

The preflight native error was getaddrinfo/thread-resource related and must not be reported as proof of native Git authorization denial. This original session successfully read the parent feature ref through native Git; that does not verify the other session or a future cloud identity. No extra shared-cluster request, build, database, environment or deployment is needed to configure access.

Official references: https://help.openai.com/en/articles/11145903-connecting-github-to-chatgpt ; https://learn.chatgpt.com/docs/environments/cloud-environments#start-a-task-in-codex-cloud

## CLOUD-CQL-HANDOFF-START-2026-11-18 — Start the requested cloud continuation

Status: owner UI step needed after agent-owned tooling failed to complete startup;18 November2026. No cloud session ID has been returned. App create_thread request is still pending; a separate app thread listing showed no new cloud session and the preinstalled Codex CLI cloud list returned zero tasks. The saved Default environment is runnable; its empty repository list is supported by Codex Cloud and is NOT evidence of missing GitHub authorization. No repeat login, deployment key or Bronze grant requested.

Reason: the existing app creation call did not complete; browser inspection could not initialize because the managed Node helper failed with "failed to write kernel assets: The system cannot find the path specified. (os error3)" before any UI action, including after one kernel reset. This is an agent/platform tooling issue, not a request for David to diagnose code or install anything.

Concrete owner step: start a new chat, choose Work in > Cloud > Default environment, and send:
> Continue the full COSMIC/CQL/HyperDrive goal. Clone https://github.com/NextGenSoftwareUK/OASIS.git on branch work/cosmic-cql-ton-public and read Docs/WIP/COSMIC ORM/CLOUD_HANDOFF_2026-11-18.md. Preserve the full goal and all current shared-cluster/deployment holds; start a persistent goal if supported and continue from the exact recorded state.

Then tell this local chat which cloud session appeared so ownership can be verified and duplicate local execution avoided. The original app request may still resolve; check for a duplicate before creating more than this one continuation. Handoff/code are pushed in parent4c4184d92c4f3b7864f2af975b0d358fab74e101 and the existing Core/website branches. No local credential/session was copied. Do not record migration as completed until an actual cloud session and repository access are verified.

Links: [full handoff](../COSMIC%20ORM/CLOUD_HANDOFF_2026-11-18.md), [official Cloud start instructions](https://learn.chatgpt.com/docs/environments/cloud-environments#start-a-task-in-codex-cloud).

# Actions for David

### WEB4 COSMIC demo preview access · 13 November

22 November update: latest reviewed feature preview is [7c002846 visible-tuple DISTINCT demo](https://web4-site-pjwe2l8sd-oasis-team.vercel.app/web4-sandbox.html), READY; HTML and all three changed JS assets match frozen source (HTML differs only by the identified Vercel preview-feedback script). SQL capture14 and actual load-for-review DOM control1 passed. It includes bounded WITH/derived queries, independently staged set inputs and the new unique visible-tuple example; no record query runs merely by loading an example. New record DISTINCT is not yet deployed: Core/Mongo/SQLite passed; Neo/actual WEB4 build45052662341 remains in progress, followed by image/package/live gates. Earlier staged selected Concat already passed populated live MongoDB/SQLite/Neo4j calls on Developmentf29262d8; no longer awaiting its earlier image/package gates. The real Chrome public Holo CQL journey remains accepted on website89cb147/API f292:3rows rendered, session revoked, owned CI profile removed, exact API source checked before/after. That earlier accepted browser scope is not relabelled to this newer site. Assets were staged inside the isolated CI browser at the approved current WEB4 origin; public website promotion remains pending and no production website/domain/protection settings changed. Complex storage-query browser flows and genuine WEB6 inference remain separate gates. Existing access works; no new login or publication approval requested here.

23 November engineering access update. Existing Vercel CLI58.4 authentication successfully inspected the exact team/project and current protected f58c9f4d preview. CLI curl returned HTTP200 with Ask/mapping/date controls; explicit UTF8 capture fixed the local response-decoding issue. No new sign-in, team consent, protection change, env file or public publication. The connected tool still403 is a separate connection; native access is sufficient for this engineering review. Public website promotion and permanent-Free model route decisions remain separate and unchanged.
21 November update — preview access resolved through the existing native Vercel connection; no new login needed. The reviewed six-file sandbox-only candidate bd739705c3f231db9d15cbec71fdf48ef00281f0 is READY: https://web4-site-p6g80bybm-oasis-team.vercel.app/web4-sandbox.html. Static browser review passed with zero application-data requests; exact qualified runtime hashes retained and Ask layout visually reviewed. Decision pending: approve publication of these website assets at the existing public WEB4 sandbox URL. Reason: this changes the public website while the standing goal excludes production promotion. It would not deploy any staging/production backend or alter PIPER data. Real WEB6 model verification remains separately gated by the existing free-model configuration action. Preserve earlier history below.


21 November: preview static access works via existing scoped OIDC; credentialed preview auth preflight returns wildcard/no credentials. Approved public WEB4 origin permits credentials. Bounded candidate browser test there established and revoked one demo session; later test failed to establish its session. SQL journey and auth diagnostic are agent-owned, not a new request for David to sign in or change protection/CORS. Free-model configuration remains a separate existing owner action.


20 November: protected preview access resolved using existing Vercel CLI scoped OIDC (HTTP200), no login/protection/configuration change. Controlled browser rendered sandbox tabs, backend requests blocked. Full SQL/Ask browser journey remains engineering work; Free-model configuration remains the existing separate owner action.


20 November native-access update. Existing installed Vercel58.4 CLI credentials successfully inspected the exact oasis-team/web4-site project without a new login, consent, linking or protection/configuration change. The connected tool still403 is a separate path. Native protected-preview request returned no HTTP result; this is an engineering-owned client diagnostic, not a request for David to sign in again. Signed-in browser acceptance remains open, and the permanent Free model/catalogue action stays separate. No production website publication or access-control change was made.

20 November connected-access evidence: read-only Vercel list_projects for the existing oasis-team scope returned403 forbidden/not authorized. This agrees with the existing team-access blocker; no additional sign-in request, consent link, account change, production publication or competing owner action was made. Existing owner decision/access step remains pending. Native ORM/API work and isolated CI continue independently.



20 November evidence update (same open action; no new sign-in request). Latest existing-branch Web4Site a85d93b6 preview is built successfully: https://web4-site-kpgpby09s-oasis-team.vercel.app/web4-sandbox.html (GitHub deployment7935475840). The public web4.oasisomniverse.one/web4-sandbox.html static page returned200 but lacks the newer Ask Your Data tab and virtual-mapping controls. Engineering's11 new actual-handler acknowledgement/source-guard tests passed; signed-in hosted acceptance remains open. Existing owner step remains authorize the existing OASIS Vercel team connection or arrange the agreed Development sandbox website publication using the already recorded hostname decision. Do not disable deployment protection or expose secrets. Permanent Free WEB6 route/catalogue action remains separate below; no new Bronze grant requested.



19 November verification update. The existing team authorization remains pending (connector403 for OASIS team); known CLI auth paths and VERCEL_TOKEN/VERCEL_OIDC_TOKEN contain no reusable authentication. No new sign-in or expiring link was requested. GitHub confirms [Website4767c837 preview](https://web4-site-fiq9i7t7v-oasis-team.vercel.app/web4-sandbox.html) built successfully, but its static page request redirects to the Vercel account page. The [current public sandbox](https://web4.oasisomniverse.one/web4-sandbox.html) returns200 and still serves the older frontend. Two displayed-example controls passed44607947923, but protected/hosted acceptance is open. Retain David's preferred sandbox.web4/sandbox.web6 hostnames; no DNS/domain/protection setting was changed. Continue isolated provider work while this existing access action remains open.

15 November. A fresh one-time OASIS team consent link was shared privately in chat after the connector returned a team-scoped 403. Approval is pending. The latest confirmed protected preview includes the canonical SQL fields and quoted metadata syntax. The Vercel project lookup still returns a team-scoped HTTP 403. After approval, engineering will inspect the Git integration, verify the Development website domain and credentialed WEB4 origin, and run the signed-in walkthrough.

Open. Authorize the existing Vercel connection for the OASIS team with a current Vercel consent link. The earlier link was time-limited. The connector currently returns HTTP 403 for the team and the installed CLI has no active credentials. This access is for the existing [protected preview of Web4Site `726362ba2d75`](https://web4-site-qbjhk295o-oasis-team.vercel.app/web4-sandbox.html) for authenticated browser acceptance. The public site still serves the previous frontend. [Web4Site feature branch](https://github.com/NextGenSoftwareUK/Web4Site/tree/work/cosmic-cql)

15 November browser check. The latest protected preview is recorded above. The dedicated hostname dev.web4.oasisomniverse.one still does not resolve. After Vercel access is available, verify that the Development domain can be assigned to the WEB4 website project and provisioned in DNS. The DNA policy first served on Development ea42fa05130f and remains in the serving cbd8383d12bf revision. After the Development hostname is assigned, engineering will enable OASIS.Sandbox.EnableDevelopmentWeb4Preview in Development OASISDNA and verify the credentialed session before a browser run. WEB6 uses bearer authentication and accepted Authorization, Content-Type and Idempotency-Key in the 15 November live preflight. WEB4 returned a wildcard without credential admission for the intended Development origin. After the Development domain is verified, engineering will confirm that WEB4 echoes the specific origin and enables credentials before the signed-in test.

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
| 11 | Messaging programme: review and push the `work/oasis-messaging` branches (parent, API.Core, STAR ODK, 7 npm repos), publish the npm packages, physical-device Bluetooth tests, HerzApp/Our World wiring decisions | G |
| 10 | COSMIC/CQL: decide follow-up Default-routing fix publication; hosted Azure/Oracle remain deferred | F |
| 12 | **Post-release 2026-11-24: production SQLite replication held (BINDING_MISMATCH), WEB9 fix to release, test records left on prod/dev** | POST-RELEASE-2026-11-24 |
| 11 | **WEB6 model catalogue: review the estimated prices; decide OpenRouter billing (G2)** (v7 live on Development; OpenRouter prices move hourly) | G |
| 12 | HyperDrive on dev: Neo4j to SQLite hold at sequence 14 — repair ran once and was refused (no write); **decision pending**: the same holon reaches SQLite from both Mongo and Neo4j | I |
| 13 | **Railway dev: add one DNA key so IPFS keeps its catalogue across restarts** (`IPFSOASIS.CatalogueRootPointerPath`) | I |

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

22 November collection-routing release evidence: Development snapshot73a7f82be3f6da1797d1400242c8d1bf0c13cf0b / Core9dcc856add106fa26b27ea5d26ef50b590bf92fd now has seven successful rollouts, native completeness/visibility verification, exact image/package acceptance and actual V2 runtime settings verification. Real PIPER business accounts were not queried. For the eventual authorized staging/production release, use a reviewed release snapshot containing this fix and its reachable paired dependency pins; fresh CI and Development builds fetched these exact pins successfully. Do not rebuild the known-unfetchable historical pins or treat a variable-only restart as a code fix. The existing staging identity/token prerequisites and your final approval still apply; no staging/production settings or deployments changed, and no new sign-in is requested here. [Development evidence](<../COSMIC ORM/COLLECTION_READ_AUTHORITY_DEVELOPMENT_ACCEPTANCE_2026-11-22.json>).

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
| Existing development Wizard/admin session for CQL source recovery (3 November2026) | **David offered an existing Wizard account;awaiting local sign-in**. Code deployed;27controlled cases/live401+403 pass. Secure local helper and4offline checks ready. 11 November UTC: Development fd40b1ad12 still reports Hedera and Nostr unavailable. Hedera timed out waiting for response headers (15,017 ms) and Nostr returned HTTP 503 on WebSocket connect. Operator sign-in remains pending on this laptop. Later on 11 November, c39eba5cd1 started both readers and Hedera public 3/2 and Nostr filtered 2 passed live HTTP. Recovery is deferred while those sources are ready; retain this action for a future failed source. Aptos now reports CQL_SOURCE_UNAVAILABLE on Development 94fe19c958 after startup HTTP 404; four GETs from the live container later passed. The protected operator session is absent on this laptop. Later on 11 November, Development 371f3d5dc4 activated Aptos on startup and its native public HTTP presets passed 3/2. Sign-in is deferred while the source is ready. | In your own terminal run `python Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py` from `C:/Source/OASIS2` (current laptop), enter your existing development Wizard username/password privately,then tell the agent sign-in is complete. No password/token in chat,account creation or role promotion. Needed only if operator recovery is required again;agent owns tests/recovery/journalling. 11 November UTC latest: Development c686a8618d reached 105 available sources and Nostr again returned HTTP 503 during WebSocket activation. Existing operator recovery remains conditional on Nostr still being unavailable after the queued IPFS publication rollout. Earlier signed-event and private-cohort results are retained. 11 November UTC: live WEB4 ea614f3fc6 reports Hedera activation timed out awaiting response headers after 15,008 ms with no HTTP status. Nostr's relay returned HTTP 503 during WebSocket upgrade (NotAWebSocket). Discovery remains 104/106. The protected Development Wizard session is absent on this laptop. Keep the existing operator recovery action open. [Secure steps](../COSMIC%20ORM/CQL_SOURCE_RECOVERY.md#secure-local-sign-in). 12 November UTC: 922321a8aa9d returned 104/106 ready. Hedera timed out before response headers after 15,008 ms. The protected Wizard session directory is absent on this laptop; the existing secure sign-in action applies. 12 November Cancun time. The exact Solr WEB4 rollout reached 106/107 ready. Hedera alone remains unavailable after a 15,010 ms activation response-header timeout. Nostr recovered. The protected local operator session is still absent. The existing Wizard sign-in is now needed to run a targeted Hedera recovery and verify both public presets.  12 November Cancun time. David signed in and Wizard authority was verified. One targeted Hedera recovery advanced generation 1 to 2 and returned HTTP 503 / CQL_SOURCE_UNAVAILABLE. The temporary refresh session was revoked and local encrypted credentials removed. Engineering owns the remaining mirror response-header timeout. No owner action is pending for this attempt. 14 November Cancun time: Hedera and HoloOASIS passed on Development 1ebcb3b772c5. Nostr alone is unavailable after relay upgrade HTTP 521. A later anonymous relay upgrade from the live container returned 101. The protected Wizard session is absent. Use the secure command in this row for one targeted Nostr recovery so the existing signed presets can be checked on live WEB4. The subsequent sign-in returned an application-level password rejection and issued no session. The rejected protected response and intent were archived locally. David requested reuse of a successfully issued Windows-protected session for future recoveries while valid. No password is retained. 15 November UTC: completed for Nostr. Development d7807ff87c7c verified the original three-event and two-event direct and federated CQL presets using the reviewed Primal relay. No additional Wizard sign-in is required for this recovery. |
| Existing development Wizard/admin session for CQL source recovery (3 November2026) | **David offered an existing Wizard account;awaiting local sign-in**. Code deployed;27controlled cases/live401+403 pass. Secure local helper and4offline checks ready. 11 November UTC: Development fd40b1ad12 still reports Hedera and Nostr unavailable. Hedera timed out waiting for response headers (15,017 ms) and Nostr returned HTTP 503 on WebSocket connect. Operator sign-in remains pending on this laptop. Later on 11 November, c39eba5cd1 started both readers and Hedera public 3/2 and Nostr filtered 2 passed live HTTP. Recovery is deferred while those sources are ready; retain this action for a future failed source. Aptos now reports CQL_SOURCE_UNAVAILABLE on Development 94fe19c958 after startup HTTP 404; four GETs from the live container later passed. The protected operator session is absent on this laptop. Later on 11 November, Development 371f3d5dc4 activated Aptos on startup and its native public HTTP presets passed 3/2. Sign-in is deferred while the source is ready. | In your own terminal run `python Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py` from `C:/Source/OASIS2` (current laptop), enter your existing development Wizard username/password privately,then tell the agent sign-in is complete. No password/token in chat,account creation or role promotion. Needed only if operator recovery is required again and for Matrix activation diagnosis on Development 91493edb. The Matrix aliases resolve from that instance. Hedera and Blockscout previously passed public HTTP. Hedera activation cancelled again on 4907da23 and also needs the operator session for recovery. The recovery laptop has no retained operator session. Engineering owns recovery and verification. 11 November UTC latest: Development c686a8618d reached 105 available sources and Nostr again returned HTTP 503 during WebSocket activation. Existing operator recovery remains conditional on Nostr still being unavailable after the queued IPFS publication rollout. Earlier signed-event and private-cohort results are retained. 11 November UTC: live WEB4 ea614f3fc6 reports Hedera activation timed out awaiting response headers after 15,008 ms with no HTTP status. Nostr's relay returned HTTP 503 during WebSocket upgrade (NotAWebSocket). Discovery remains 104/106. The protected Development Wizard session is absent on this laptop. Keep the existing operator recovery action open. [Secure steps](../COSMIC%20ORM/CQL_SOURCE_RECOVERY.md#secure-local-sign-in). 12 November Cancun time. The exact Solr WEB4 rollout reached 106/107 ready. Hedera alone remains unavailable after a 15,010 ms activation response-header timeout. Nostr recovered. The protected local operator session is still absent. The existing Wizard sign-in is now needed to run a targeted Hedera recovery and verify both public presets.  12 November Cancun time. David signed in and Wizard authority was verified. One targeted Hedera recovery advanced generation 1 to 2 and returned HTTP 503 / CQL_SOURCE_UNAVAILABLE. The temporary refresh session was revoked and local encrypted credentials removed. Engineering owns the remaining mirror response-header timeout. No owner action is pending for this attempt. |
| Consolidated Development batch timing (31 October) | **Approved; published** — David replied “Publish one consolidated batch now”. Development now parent57174def3/Core6bbd015, both remote-verified; newer public-provider expansion remains on its separate task branch | One additional consolidated Development publication authorized. No master/production promotion or hosting spend. [Scope/evidence](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#consolidated-release-preflight--31-october-2026). |
| Default-routing follow-up publication (31 October) | **Approved and deployed, 1 November 2026** — David replied “Publish correction now”; Development051ac6af7 is live, Railway d8876691 SUCCESS; Default preservation and three-source failover verified for the live-found V2 controller acquisition bug.13guard+61controllerHTTPtests pass0skips; original routing fixture/3replicas preserved | First approved extra batch57174def3 deployedSUCCESS, revealing Default is bound toMongo before balancing. Dailycadence exception needed for another deployment. No provider expansion/Corepin/hosting spend. [Root cause/tests](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#live-default-routing-root-cause-and-scoped-fix--31-october-2026). |
| Expansion SDK packaging correction (1 November) | **Approved and published, 1 November 2026** - David replied Publish packaging correction now. Development89681ce40 is pushed and remote-verified; observe its exact deployment before enrollment. Expansion build failed before enrollment; the prior routing release remains live | Seven Polkadot imported modules were omitted from the publish manifest. Actual MSBuild publish metadata, five bridge closure checks and two regression tests pass. No settings, new hosting or production/master promotion. [Details](../COSMIC%20ORM/HOSTED_SANDBOX_INTEGRATION_PLAN.md#polkadot-publish-root-cause---1-november-2026). |
| Hosted PostgreSQL CQL demo (1 November 2026) | **Deferred by David, 1 November 2026** - David replied "Leave hosted PostgreSQL pending". Local native/paired HTTP proof is preserved; no new paid resource or account created. | No owner setup is needed now. Preserve this deferral and continue other providers; do not re-ask for access or provision hosting. Revisit only when David resumes hosted PostgreSQL. [Local proof and placement gap](../COSMIC%20ORM/POSTGRESQL_CQL_MVP.md#next-placement-work). |
| Hosted Azure Cosmos | **Deferred** — free only; no existing free subscription; David said leave hosted Azure pending | Only if David chooses to resume. No paid upgrade, card/signup request or new account creation now. |
| Hosted Oracle Database / OCI | **Deferred** — no existing Oracle account; David said leave hosting pending | Only if David resumes. Then record the actual account/connection/wallet setup needed here; request paths or variable names only, never secret values. |
| First AWS demo | **Approved** — use a public S3 dataset | Decision implemented: anonymous native metadata reads and Mongo pairing verified through hosted HTTP. No AWS account or bucket setup needed for this demo. |
| Temporary demo avatar daily calls | **Approved: 200** | Implemented/live-verified. Normal Free accounts stay at 20; token/spending limits unchanged. No further approval requested. |
| Browser access for demo verification | Public browser checks passed for EOSIO, SOLID, TON and Hedera. The inspected recovery browser directory contains the public visitor only. The EOSIO journal is absent from both local worktrees. The COSMIC recovery checkout also lacks the original TON, Hedera and SOLID journals and protected sessions. | The private paired browser checks require the original demo owner and existing MongoDB copies. Restore the session on the originating machine or identify its protected location privately. Preserve the accepted cohorts. |
| Browser access for demo verification | Public browser checks passed for EOSIO, SOLID, TON and Hedera. The inspected recovery browser directory contains the public visitor only. | The private paired browser checks require the original demo owner and existing MongoDB copies. Restore the session on the originating machine or identify its protected location privately. Preserve the accepted cohorts. |
| LocalFile HyperDrive release verification | **Development integration completed 10 November 2026.** The scoped controller and Core dependency repair shipped at 612dd018. LocalFile appeared available on replacement deployment 91493edb. | The local real HTTP regression passed. Current live HyperDrive point-routing verification remains engineering-owned and deferred behind the priority provider demos. Preserve the accepted LocalFile CQL cohort. [Current evidence](../COSMIC%20ORM/CURRENT_STATUS.md#current-development-deployment-observation). |

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

12 November Cancun time: Development now pins HoloNET 9f897b7ace60, which includes 4daa7437 and AutoDownloadConductor. WEB4 Development already has UseLocalNode enabled. The CQL demo still requires engineering verification of the co-located conductor, provisioning the dedicated read grant and synthetic fixtures, and loading the protected read-only settings. This update records Development progress. The later live rollout decision remains open.

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

- Completed 11 November UTC. Engineering ran the existing restricted-role test on disposable CouchDB 3 in [CI run 43044567583](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43044567583). The old draft PR action is closed. Hosted CouchDB qualification remains a separate gate.

### CouchDB hosted teaching source - 11 November 2026

- Open. Identify an existing HTTPS CouchDB service and provide the protected location of a dedicated non-admin reader for the isolated oasis_holons teaching database. The reader must load the teaching records and be denied document creates, updates and deletes by the database validator. If the isolated database does not exist, identify the existing service and secure provisioning access privately. No credentials in chat or git. [Fixture and admission](../COSMIC%20ORM/COUCHDB_CQL_MVP.md).

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

11 November UTC: WEB4 Development variable-name inventory returned no Supabase settings. Native loopback storage conformance passed 52/52. The reviewed HTTPS teaching project and restricted reader are still needed for the hosted demo.

- Status: 36 provider and eight WEB4 admission checks passed. The native HTTPS qualifier compiled.
Priority update. Supabase is in the top-priority WEB4 demo queue. A recovery-laptop preflight found none of the five reviewed source and reader settings in the process, user, or machine environment. No values were printed and no request was made.

- Status: 36 provider and eight WEB4 admission checks passed. The native HTTPS qualifier compiled. The offline hosted verifier also passed its exact-catalogue grant check. Native hosted qualification still awaits the reviewed project and restricted reader access.
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

- Still open, re-checked 23 November 2026: TRON is the only priority chain with no live sandbox CQL demo. It is not enrolled on dev WEB4 because the reader key is missing. Steps: create a free TronGrid API key (trongrid.io, sign in, Create API key), then set it as Railway dev WEB4 variable `COSMIC_TRON_PUBLIC_READER_KEY` (and the Actions secret of the same name). Tell engineering once done; engineering then enrols the catalogue and verifies live. Never paste the key in chat.

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
### BigQuery and Redshift teaching readers

Status on 9 November 2026. Deferred behind the priority WEB4 demo.
The bounded readers passed their local build and admission checks.
Managed verification is pending, and no paid resources were started.

- BigQuery needs an existing approved teaching project and dataset with
  three reviewed public records in `oasis_holons`. The dedicated
  application identity needs read access to that table and permission to
  create query jobs in the approved billing project. The reader caps
  billed bytes at 10 MiB per job. Keep the credential in protected
  application-default configuration.
- Redshift needs an existing approved teaching warehouse with
  certificate-verified connectivity, a dedicated role with SELECT access
  to `oasis_holons`, and three reviewed public IDs. Keep the reader
  connection string protected. Its SHA-256 binds the reviewed catalogue
  to the configured connection without putting credentials in the file.
- Engineering owns catalogue validation, exact probe selection, the
  native/Mongo HTTP fixtures, and WEB4 acceptance after authorized
  access is available.

[Provider matrix](../COSMIC%20ORM/PROVIDER_CQL_AND_SANDBOX_STATUS.md)

### Snowflake teaching reader

Status on 9 November 2026. Deferred behind the priority WEB4 demo.
The WEB4 build, protected settings check and both offline admission
cases passed. The managed fixture was skipped.

If an approved teaching warehouse already exists, identify its
account, database, schema and warehouse and make a dedicated reader
role available privately. It needs warehouse, database and schema
usage and SELECT on the reviewed `oasis_holons` table. The approved
connection belongs in protected `CQL_SNOWFLAKE_QUERY_CONNECTION_STRING`.
Engineering will bind its SHA-256 to a reviewed catalogue, set
`CQL_SNOWFLAKE_QUERY_PROBE_ID` from the first reviewed ID, and run the
paired native and Mongo HTTP fixture. No new paid warehouse is requested.

[Local build and qualification evidence](../COSMIC%20ORM/SNOWFLAKE_WEB4_DEPENDENCY_GATE.md)

### Databricks teaching source

Status on 9 November 2026. Deferred behind the priority WEB4 demo.
The provider and WEB4 builds, protected settings check and four
offline admission cases passed. The managed fixture skipped.

If an approved teaching SQL warehouse already exists, identify its
workspace HTTPS origin, warehouse ID, catalog and schema. Arrange a
dedicated identity that can use that warehouse and SELECT only from
the reviewed `oasis_holons` teaching table. Keep its token in protected
`CQL_DATABRICKS_QUERY_READER_TOKEN`. No new warehouse or spending is
requested. Confirm use of the existing warehouse is approved before
submitting any statements.

Engineering owns the reviewed three-record descriptor, binding of the
first probe and protected settings, and the native/Mongo HTTP fixture.
[Provider matrix](../COSMIC%20ORM/PROVIDER_CQL_AND_SANDBOX_STATUS.md)

### Zilliz Cloud teaching reader

Status on 9 November 2026. Deferred behind the priority WEB4 demo.
The provider and WEB4 builds and protected configuration checks passed.
Five offline WEB4 admission cases passed. The managed fixture skipped.

If an approved teaching cluster already exists, identify its reviewed
HTTPS cluster endpoint and arrange a dedicated API key restricted to
reading the `oasis_holons` teaching collection. Keep the key in protected
`CQL_ZILLIZ_QUERY_READER_TOKEN`. No cluster creation or spending is
requested. Engineering owns the reviewed record catalogue, origin and
probe binding, and the paired native and Mongo HTTP checks.

[Provider matrix](../COSMIC%20ORM/PROVIDER_CQL_AND_SANDBOX_STATUS.md)

### Xata PostgreSQL teaching reader

Deferred behind the priority WEB4 demo. The Xata provider and protected
configuration checks passed. WEB4 verification is in progress.

Managed qualification requires an existing approved Xata PostgreSQL
teaching database and a dedicated SELECT-only role. Keep the verified
TLS connection string in protected `CQL_XATA_QUERY_CONNECTION_STRING`.
Engineering will bind the reviewed connection digest and first public
probe ID and run the paired HTTP/MongoDB fixture. No new paid database
is requested.

[Provider matrix](../COSMIC%20ORM/PROVIDER_CQL_AND_SANDBOX_STATUS.md)

### Five-reader CI source publication

10 November 2026. The scoped WEB4 and Core previews are ready for source
publication. The working-copy Release build and 14 focused admission tests
passed. The exact image has not been built. GitHub's Build WEB4 image workflow
requires a remote parent revision and a fetchable Core revision matching the
parent gitlink and manifest.

Approved 10 November 2026. David lifted the commit/push hold for scoped
publication on the existing COSMIC refs and required exact image verification
before Development integration. Core 4018beec and WEB4 3e44bdc2 were pushed.
Build WEB4 image run 42840429678 was dispatched with 3e44bdc2 as its source.
The exact Development candidate passed CI run 42842227152 and merged at 111165cf. Railway 25b466ab is building. The 12 protected reader settings await deployment success. Engineering owns verification and rollout.
[Scope and evidence](../COSMIC%20ORM/CURRENT_STATUS.md)

Power recovery verified on 11 November UTC: battery 21% and charging (Win32_Battery status 2). The prior Edge window is closed. Engineering owns reopening browser verification. The charger action is resolved.

### GOMap public demo routing origin (11 November 2026)

**Status** Awaiting an approved dedicated endpoint for Development WEB4. The exact GOMap candidate passed its 12 boundary checks and native WEB4 HTTP/MongoDB qualification in [CI run 43108783977](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43108783977). Scoped Development merge 274ff42adb is prepared and waits on the existing WEB4 deployment queue. Identify the existing dedicated HTTPS OSRM origin, if available, and where its operator terms are documented. The demo needs permission for concurrent sandbox traffic and must meet the operator's attribution rules. If no endpoint is available, the linked review documents an owned Berlin-only OSRM deployment. Capacity measurement and a hosting decision are still open. Keep credentials in private configuration. [GOMap scope and endpoint review](../COSMIC%20ORM/GOMAP_PUBLIC_SOURCE_STATUS.md).
**Status** Awaiting an approved dedicated endpoint for Development WEB4. Identify the existing dedicated HTTPS OSRM origin, if available, and where its operator terms are documented. The demo needs permission for concurrent sandbox traffic and must meet the operator's attribution rules. If no endpoint is available, the COSMIC task will document hosting options before any paid provisioning decision. Keep credentials in private configuration. [GOMap scope and endpoint review](../COSMIC%20ORM/GOMAP_PUBLIC_SOURCE_STATUS.md).

### Hosted ArangoDB and QuestDB teaching fixtures (11 November 2026)

**Status** Awaiting identification of existing approved HTTPS teaching fixtures for Development WEB4. For ArangoDB, identify the HTTPS origin for the dedicated `oasis/holons` dataset, the reviewed record IDs, and the protected location of its restricted read-only credentials. For QuestDB, identify the HTTPS query origin and the reviewed table, `SYMBOL` column and key set. Keep credentials in private configuration. These inputs let engineering verify the exact full and subset results before admitting either source to the live demo. Local and CI evidence is already recorded. [ArangoDB qualification](../COSMIC%20ORM/ARANGODB_CQL_MVP.md) · [QuestDB qualification](../COSMIC%20ORM/QUESTDB_PUBLIC_SOURCE_STATUS.md)

## Urbit Development teaching fixture — 12 November 2026 UTC

- Status: awaiting an existing approved fixture. Native Urbit and MongoDB federation passed before and after restart, including owner isolation and cleanup. WEB4 Development has none of the six Urbit reader settings and no service named for Urbit. The recovery laptop also has none of the six settings. Only names were inspected.
- Owner action: identify an existing HTTPS Urbit ship suitable for a private teaching contact book and the protected location of its reader session, or state whether to defer hosted enrollment. Use synthetic contacts only. The reader requires the HTTPS origin, ship, restricted session, designated owner, stable connection ID and reviewed page IDs. Keep credentials out of chat and Git.
- Engineering next step: qualify only the reviewed pages and owner, stage the six scoped Development settings, and verify native and MongoDB paired presets and cross-owner denial. [Urbit scope and qualification](../COSMIC%20ORM/URBIT_CQL_MVP.md).

### WEB6 Ask your data Development demo entitlement · 13 November

25 November update (CQL sandbox session): still open, and now the only blocker for the sandbox's Ask Your Data tab. The tab's code is finished on Web4Site `work/cosmic-cql`. A live call to `dev.api.web6.oasisomniverse.one/v1/complete` with a demo session returns `WEB6_FREE_ROUTE_UNAVAILABLE` ("No zero-price completion route is configured"). What David needs to do is configure one free inference route in Development WEB6's saved DNA: a Gemini free-tier API key, an OpenRouter key (with a free model), or an Ollama endpoint and model. That unblocks the Ask Your Data demo for every visitor.

23 November runtime update: selected startup configuration-presence flags on the running Development WEB6 source4b8d3923 show no dedicated Gemini Free key, no OpenRouter credential and no Ollama endpoint/model. No secrets were printed, model requests made or settings changed. The existing permanent-Free-route action remains deferred/pending; no new login, entitlement grant or repeated owner request. Engineering continues native query delivery independently.


22 November update — same existing permanent-Free-route action remains pending. Scoped saved Development WEB6 configuration inspection found no DNA/environment OpenRouter key, no dedicated Gemini Free key, and no Ollama URL/model. Presence booleans only; no values printed, configuration writes, model inference, sign-in or Bronze grant. [Current presence proof](../COSMIC%20ORM/WEB6_FREE_ROUTE_PRESENCE_2026-11-22.json). Keep provider credentials in private OASIS DNA/secret storage; the prior owner decision is preserved and is not being re-asked. Engineering completed the SDK startup recovery on Development94d9966b: deployment00b75010 is SUCCESS and public /v1/health returned200/ok in579ms for the verified current revision. Exact seven-API/real-assembly run45031220175 passed. No model call was made; the existing permanent-Free-route configuration action is still pending.

21 November UTC update: a new scoped read of saved Development WEB6 OASIS DNA still found GeminiFree/OpenRouter API keys and Ollama URL/model unset (four presence booleans only). No secret values printed, inference/data calls, setting changes, new sign-in request or Bronze grant. Existing permanent-Free-route action remains pending; saved settings do not prove loaded runtime configuration. Evidence: [presence-only verification](../COSMIC%20ORM/WEB6_FREE_ROUTE_PRESENCE_2026-11-21.json). Earlier owner decisions and history below are preserved.


**Current action, 17 November UTC** Configure a permanent Free WEB6 route and publish the reviewed additive catalogue using existing authorized access or direct owner publication. The earlier one-time Bronze grants are closed and the visitor is back on Free. Earlier sign-in and grant instructions below record previous steps. No new Bronze approval or sign-in is pending. The two Development configuration steps are described below.

Read-only verification during CQL image44208771977: the Development WEB6 service's saved OASIS_DNA_JSON has none of the four requested Free-route fields configured (GeminiFree/OpenRouter keys and Ollama URL/model). Only presence booleans were reported; no secrets/settings were changed. Saved configuration is not proof of what the running deployment has loaded. Existing action remains pending; no new login or Bronze approval is requested.

16 November UTC. Live Development WEB6 still uses the older published catalogue and exposes only Ollama entries to a fresh Free avatar. Audited additive publication remains pending a reusable authorized Development administration session or direct owner publication. The owner asked us to reuse existing access and avoid repeated sign-ins. The update retains paid models and rates and adds 16 reviewed Gemini/OpenRouter zero-price profiles. It grants no Bronze entitlement. A configured private inference route is also required for a real model call.

16 November UTC. Permanent Free demo setup is pending provider access. The existing Railway WEB6 AI API settings have no configured zero-price inference route. For Development, use the saved OASIS DNA secret configuration to set Web6.ApiKeys.GeminiFree from a separate Google project with billing disabled and Web6.ApiKeys.OpenRouter from a restricted OpenRouter key. To add self-hosted Ollama, supply a private WEB6-reachable service URL and a locally installed approved model via Web6.OllamaBaseUrl and Web6.OllamaFreeModel. Enter values privately. No keys or endpoint credentials belong in Git or chat. Configure an available route first for browser acceptance. Keep staging and production configurations isolated and unchanged pending rollout approval. [Google free-tier billing](https://ai.google.dev/gemini-api/docs/billing) · [OpenRouter free models](https://openrouter.ai/collections/free-models) · [Ollama local API](https://docs.ollama.com/api/introduction).

16 November completion update. David approved exactly one temporary grant and completed the private administrator sign-in. The approved browser attempt received a real WEB6 draft. The requested two-account CQL scope was not confirmed, so execution stopped before making a WEB4 query. The visitor’s prior Free entitlement was verified restored. The one-time grant is consumed. David asked for a permanent free-model route for temporary avatars.

Historical entry (closed by the completion update above). 16 November UTC. David approved one fresh 30-minute Bronze grant for the existing dedicated Development visitor to draft and run one read-only CQL query. The previous protected subscription administrator session is absent from this laptop. Sign in privately from the OASIS2 checkout with `python -B Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py --subscription-admin`. Report successful sign-in only. After applying the scoped grant, engineering will run the browser check from the current WEB4 origin and restore the prior entitlement. No Stripe checkout.

Historical entry (closed by the completion update above). 16 November UTC. The original one-time Bronze grant expired and the dedicated Development visitor is back on Free. The current public WEB4 origin passed Development WEB4 credential checks and the live WEB6 authorization preflight. Engineering verified the exact website build and can use the current public origin with the staged assets for a browser check before the new DNS records arrive. Owner decision pending for one fresh 30-minute Bronze grant for the same visitor, or use an existing authorized Development demo session with WEB6 access. The test will draft one read-only CQL query, show it in the editor for review, and run it against WEB4. The audited grant uses no Stripe checkout. An operator session is needed only if a new grant is selected. [Exact WEB4 preview](https://web4-site-nthsy7gwn-oasis-team.vercel.app/web4-sandbox.html).

16 November follow-up. The compact draft now passes token reservation with the existing Free visitor and reaches model admission. WEB6 reported that Free has no available model. The scoped entitlement request remains pending. The test made no WEB4 query.

14 November completed. The subsequent Development sign-in verified administrator access, the approved 30-minute Bronze grant was acknowledged, and live WEB6 drafting and WEB4 execution returned both requested public Hedera rows. The separate hosted browser walkthrough still needs the preview access and browser origin below.

Historical entry (closed by the completion update above). Earlier 14 November update. Development rejected the first private sign-in and issued no session. The failed local attempt was reconciled and archived, so the original command can be rerun with an existing Development subscription administrator account. The approved grant is still unapplied.

Historical entry (closed by the completion update above). Approved 13 November. David approved the 30-minute Bronze grant for the dedicated Development test visitor and agreed to sign in privately. Waiting for the separate protected Development administrator session before applying it. The Free catalogue only admits Ollama and no Ollama inference route is configured in Development. The deployed WEB4 subscription admin API supports audited plan grants with an expiry and no Stripe checkout. Proposed grant: 30 minutes, dedicated test visitor only, for one real WEB6 draft and WEB4 CQL execution. The prior Wizard sign-in was scoped to Hedera recovery. Run `python -B Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py --subscription-admin` privately from the OASIS2 checkout with the existing Development subscription administrator account. The password is hidden and the separate session is protected locally by Windows. Report sign-in completion only. [Plan grant API](../../Devs/API%20Documentation/WEB4%20OASIS%20API/Subscription-API.md)

## COSMIC Web4Site preview access

13 November update. Vercel denied access to the OASIS team when checking the Development preview hostname. A fresh one-time Vercel approval request was sent privately in chat. Once approved, verify the existing preview and inspect the project domain configuration for dev.web4.oasisomniverse.one. The approval link expires after ten minutes and is intentionally omitted here.

Open 13 November. Approve the Vercel OASIS team access request when a fresh one-time consent link is provided, so engineering can open and verify the protected Development demo preview in a browser. No production change is requested. GitHub deployment 7802106867 confirms the successful preview for Web4Site revision 068b892f558a. The connected Vercel account still needs OASIS team authorization. Team authorization allows preview inspection. The preview hostname separately lacks the WEB4 credentialed session origin required for a live browser run. [Protected preview](https://web4-site-26o21ws1l-oasis-team.vercel.app/web4-sandbox.html).

16 November update. Existing authenticated Vercel CLI access verified the protected WEB4 preview. The earlier team-consent action is closed. Vercel verified ownership and assigned dev.web4.oasisomniverse.one to the work/cosmic-cql preview branch.

### WEB4 and WEB6 sandbox DNS

- **Status (17 November UTC)** Open. The previous Vercel project assignments are documented below. Both sandbox names still return NXDOMAIN. Development WEB4 already permits credentialed browser sessions for the intended WEB4 sandbox origin. The generated Vercel preview origin permits bearer requests through wildcard CORS, without session cookies. David chose the sandbox names for consistency with HoloNET. The earlier dev.web4 record request is superseded. The WEB4 alias currently points to the ready preview for website revision 1216613a9c66. Authenticated access to that preview returned HTTP 200 and served the mixed CQL paging asset. Anonymous access to the generated preview URL still redirects to Vercel sign-in.
- **David action** Add two CNAME records in the existing NameSilo zone for oasisomniverse.one.
  - Host sandbox.web4 points to 6022d70f3a03e68f.vercel-dns-017.com.
  - Host sandbox.web6 points to d8f3f9dacdc03719.vercel-dns-017.com.
- **Reason** These records provide the HTTPS addresses for the Development sandboxes. Engineering will verify DNS, certificates, the redirects from each sandbox root, preview protection and the WEB4 signed browser flow. [WEB4 domains](https://vercel.com/oasis-team/web4-site/settings/domains) · [WEB6 domains](https://vercel.com/oasis-team/web6-site/settings/domains)

## Older tracked wallet export review

15 November 2026 UTC. An existing WEB4 wallet export is tracked in the COSMIC history. It includes fields named PrivateKey and SecretRecoveryPhrase. Engineering has not established whether this is synthetic or still usable. Confirm the origin of the export with the wallet owner. If the wallets are real, handle rotation or asset migration through the existing private owner process and coordinate any repository-history remediation separately. Keep values out of chat, Git and this list.

- File: ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/wallets.jsonwallets_83dfa5f8-7e19-4937-a09b-ed70d84fb160.json
- Status: open. Scope limited to identifying the tracked export.

### Blockscout Universal API credential for Development CQL

- Status on 15 November 2026 UTC. Deferred by David. Revisit when he is ready to add the key.
- Use the [Blockscout developer portal](https://dev.blockscout.com/) to create or identify a dedicated Universal API key on the free plan. Store it privately in Development OASISDNA at OASIS.Cql.BlockscoutUniversalApiKey. Keep the value out of chat and source control.
- Engineering wired both hosted readers to this OASISDNA field on the COSMIC feature branch. Hosted configuration and authenticated verification remain deferred.
- Reason. [Blockscout deprecated per-instance API support](https://docs.blockscout.com/devs/apis). The monitored address and Connext log readers need the supported authenticated route. Engineering owns migration, identity preservation, exact live verification and performance checks.

### WEB4 dev MongoDB storage quota

- **Status (2026-11-16 UTC)** Development session and scoped CQL writes recovered. The original visitor renewed successfully and nine owned SQL fixtures were saved and conditionally deleted across the three providers. Engineering still needs to verify the subscription and worker paths. Railway dev deployment 887a53d4 previously logged MongoDB insert and findAndModify rejections at 512 MB of 512 MB. Two disposable sandbox sign-in attempts returned HTTP 400 during avatar creation, before issuing a session.
- **David action** Restore writable storage for the MongoDB deployment backing Railway WEB4 dev. Increase its storage quota, or identify the exact disposable development collection or records that an administrator can remove to recover capacity. If identifying data, provide collection names and their retention scope only.
- **Reason** The quota blocks disposable sandbox avatar creation and other MongoDB writes. Engineering will verify sandbox sign-in, subscriptions and HyperDrive after capacity is restored. Railway continues to log rejected writes from the WEB4 usage settlement and reconciliation workers. Engineering will verify both worker cycles after storage recovery. Railway deployment 3480bc4b subsequently crashed at startup when Atlas rejected subscription index initialization at the same quota. 16 November follow-up: the subscription and outbox index startup repair restored public WEB4 health on Railway 506283cd. MongoDB quota rejections still gate sandbox sign-in, usage worker verification and write-dependent CQL acceptance. [WEB4 dev deployment](https://railway.com/) · [Current status](../HyperDrive-Providers/CURRENT_STATUS.md)

## PIPER parent collection routing and load-balancing decision

21 November latest: David explicitly instructed immediate Development re-enablement for testing. Completed: seven Development saved copies true; existing-image redeploys SUCCESS and seven running startup DNA files true. Fourteen staging/production saved copies remain false; no rollout there. Earlier conditional hold is superseded for Development. PIPER SA/SLP/student-service completeness verification remains engineering work, not a new owner approval or login request.

21 November update: David conditionally authorized re-enabling load balancing only once verified safe. Holo catalogue availability alone is insufficient. Separate SA/SLP/student-service completeness acceptance is still not established in current evidence; retain false and continue engineering verification without new login requests. This supersedes the older wording that the owner decision itself is pending. No source/deployed setting changed and no staging/production authorization inferred.

16 November Cancun time. The owner confirmed that disabling automatic load balancing restored PIPER's missing records. Development `4d0d2022170c` includes the authoritative-parent routing and async visibility fixes. Exact image CI 43981891022 passed. Development WEB4 deployment `b41caa36-ffb0-41cd-a956-f9b86aaa2948` is queued. The real SA, SLP and student-service results remain separate verification items, and the owner has retained the decision to turn load balancing back on.

The saved `OASIS_DNA_JSON` settings on all 21 Railway copies were checked read-only. `OASIS.StorageProviders.AutoLoadBalanceEnabled` is false and `AutoFailOverEnabled` is true in Development, staging and production. Each environment has the same seven service copies: WEB4 OASIS API, WEB5 STAR API, WEB6 AI API, WEB7 SYMBIOTIC API, WEB8 IGN API, WEB9 SINGULARITIY API and WEB10 THE SOURCE API. Existing active staging and production WEB4 deployments predate the saved flag changes. Promote a verified release snapshot with reachable dependency pins after approval, starting with staging. Railway restart reuses the existing deployment and its configuration.

If the owner elects to re-enable the setting after verification, review these exact source paths changed by `dc9230c41cf1`, plus `Docs/Devs/OASIS_DNA_Reference.md`. Preserve the typed default and any intentional isolated fixture settings. This inventory records the mitigation and does not authorize flipping any copy.

- `Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.TestHarness/OASIS_DNA.json`
- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json`
- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.Development.json`
- `ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BaseOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BaseOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.COSMOSOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.RadixOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Network/NextGenSoftware.OASIS.API.Providers.PinataOASIS.TestHarnes/OASIS_DNA.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.TelegramOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.TestHarness/OASIS_DNA.json`

## GATED-RELEASE-RAILWAY-CREDENTIALS-2026-11-20

Status: blocked, 20 November 2026. The login hotfix is on `master` (d62d28e9, master CI green, run 44672495323) but the `Gated Railway release` run 44675640513 failed in its `staging` job with `ValueError: Missing environment-scoped Railway credential`. Production was skipped. Nothing was deployed by this workflow.

Reason: `Scripts/railway_release_gate.py` needs a Railway project token scoped to the target environment, read from the secret `RAILWAY_TOKEN` of each GitHub environment. Agents cannot create it (`projectTokenCreate` returns Not Authorized) and credentials must never be copied into chat or files.

Owner steps:
1. Railway, project OASIS, Settings, Tokens: create a project token for the **staging** environment.
2. GitHub, NextGenSoftwareUK/OASIS, Settings, Environments, `oasis-staging-gated`, Environment secrets: add `RAILWAY_TOKEN` with that token.
3. Create a project token for the **production** environment and add it as `RAILWAY_TOKEN` in `oasis-production-gated`; the production job also reads `RAILWAY_STAGING_TOKEN` (add the staging token there as well).
4. Re-run the failed `Gated Railway release` run (or dispatch it from master with the full SHA), then press Review deployments for `oasis-staging-gated` and later `oasis-production-gated` (reviewer dellams).

Also seen in that run's log: `digest-mismatch: error` during staging preflight; the owning session should check it after the credential is in place.

Links: https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44675640513

### HD-CLOUD-READINESS — inspect whether the requested cloud continuation appeared

- 2026-11-18 update: David installed the ChatGPT Codex Connector for NextGenSoftwareUK. The GitHub connector now sees the organization installation and resolves the exact pinned Core and private dependencies and the recorded OPORTAL commit. Repository authorization is complete for these reads. Cloud-side checkout and execution remain unverified because the cloud environment listing returned a Codex app tool error.

- Status: UI observation answered: David reports “No—I don’t see it”. No repeat request; cloud service recovery remains agent-owned. Original request date: 2026-11-18T20:17:45.937147+00:00.
- Context: David requested moving the existing goal to cloud. Source and full handoff are committed/pushed on work/hyperdrive-provider-completion. One cloud create request and one read-only task-list request remain unresolved, with no task ID returned. Do not create a second task while the first outcome is unknown.
- Owner step: check whether a sidebar task titled “OASIS HyperDrive and OPORTAL — cloud continuation” appears. If present, report whether it shows Cloud and any setup/access message; otherwise report that it is absent. No credentials or private record contents are requested.
- Reason: the app-control requests have not returned and the available Windows UI automation policy does not permit controlling this app, so the agent cannot verify that UI state itself. This observation is not a request to repair the tool service or to approve a deployment.
- Evidence: Docs/WIP/HyperDrive-Providers/CLOUD_HANDOFF_2026-11-18.md and CURRENT_STATUS.md. Cloud repository/goal access and local stop remain unconfirmed.

2026-11-18T20:24:40.308423+00:00 HD-CLOUD-READINESS update: David confirmed the cloud continuation does not appear in his sidebar. Installed Codex CLI cloud list authenticated successfully and returned an empty tasks page; this does not prove the outstanding modern app creation call is cancelled or terminal. Both app creation and task-list calls remain unresolved; no task ID, private repository access or cloud goal is confirmed. Do not submit another creation request merely because observation is slow. No new owner action requested.

2026-11-18T20:41:31.1042921Z HD-CLOUD-READINESS manual-start update: David asked whether to start a cloud session and what prompt to use. Owner steps: new Codex chat > Work in > Cloud > Default environment (or a configured OASIS environment), then use NextGenSoftwareUK/OASIS branch work/hyperdrive-provider-completion and read Docs/WIP/HyperDrive-Providers/CLOUD_HANDOFF_2026-11-18.md plus current status. Restore the full durable objective; OPORTAL/subscriptions/performance first; preserve shared Atlas stand-down, deliveries535/543, worker holds and COSMIC isolation. Verify cloud runtime/private source access and continue the full scope, retaining passed evidence. Latest test-only observer source is c2fc56670e9852bd9d65541b4282dbe2f8003395; only the one affected failover case needs requalification on the frozen successor, not four passing cases or unchanged API build. Once cloud reports ready with source access, tell this local chat explicitly to pause its local goal. No plaintext credentials or new deployment approval requested. Original automatic creation is still unconfirmed and bootstrap-only; no second engineering writer was started by this chat.

HD-CLOUD-READINESS: Cloud readiness requirements: the newer CQL chat titled Cloud Transfer Blocked reports Windows-local execution, not cloud. Independent current GitHub integration checks: OASIS metadata exposes account push rights, but create-tree write returned403; private OASIS-API-Core metadata returned404. The existing desktop Git identity remains separate and can publish authorized changes, but is not transferred. Owner setup is an actual Work in > Cloud environment with coding repository access: parent OASIS, private Core and required recorded dependencies, plus write access to owned task branches; verify cloud runtime and source access before claiming transfer. Do not paste or copy desktop credentials. Original automatic cloud bootstrap remains unresolved; no duplicate engineering writer has been created.


2026-11-18: HD-CLOUD-READINESS closed/deferred by owner. David abandoned cloud after web and Codex attempts. Reviewed Hyperdrive & Providers Cloud (6afe0e96-4420-83e8-a389-b4ec09a55317): GitHub worked, but cloud execution and a durable cloud goal were never verified; documentation-only commit 70211d0, no implementation changes reported. No further owner setup action requested. Continue existing active desktop goal on work/hyperdrive-provider-completion. OPORTAL, subscriptions and performance remain first. Preserve shared Atlas stand-down, paused workers and deliveries 535/543. SDK packet 55c9f622 / Core 2ae9c233 is pushed, pending scoped qualification. No build, download, environment or database created for this review.


## Part H — Isolated warning-email provider verification (21 November 2026)

- **H1 — Complete: corrected real-provider verification accepted and temporary secrets removed (21 November 2026).** The new OPORTAL warning action is implemented and compiled; real Mongo event persistence, current HTTP capability/disabled/anonymous checks and cleanup have passed. Real Resend acceptance and the enabled button flow are still unverified. Read-only selected configuration confirms dev WEB4 has a Resend key, warning emails enabled and email not globally disabled. The OASIS GitHub repository has no Resend/email-test secret; no credential value was printed or copied.
- **Decision/steps:** either add a dedicated scoped Resend test credential as GitHub Actions secret `OASIS_WARNING_TEST_RESEND_KEY` in [OASIS repository secrets](https://github.com/NextGenSoftwareUK/OASIS/settings/secrets/actions), or authorize the agent to stage the existing dev Resend credential in that isolated test secret. Do not paste the key in chat. The test must use the provider's controlled test recipient, never a real user, one bounded new submission and an identical idempotent replay, owned loopback Mongo only, no shared Atlas/auth/payment operations. Remove the temporary CI secret after accepted verification if it is not retained for a designated reusable test environment.
- **Reason:** the native runner currently cannot access an approved provider credential. Moving the existing deployment credential into CI changes its access scope and requires this owner decision. No live-node email/DNA setting change is needed.
- **Agent-owned follow-up:** prepare exact credential-safe runner/recipient/source, verify actual provider ID and durable acknowledgement, exercise the enabled OPORTAL control, keep provider acceptance distinct from inbox delivery, and clean up. Engineering and runner work is not David's task.
- Evidence: [billing delivery record](../HyperDrive-Providers/OPORTAL_PERSONAL_BILLING_DELIVERY_GAP_2026-11-21.json), [current status](../HyperDrive-Providers/CURRENT_STATUS.md). David authorized using the existing dev credential in a temporary CI secret in this chat. Agent must stage it without printing/file persistence, run the bounded test and remove the temporary secret afterward. No further owner decision is pending.

H1 execution update: existing dev credential and configured sender staged in OASIS_WARNING_TEST_RESEND_KEY and OASIS_WARNING_TEST_EMAIL_FROM; selected metadata verified22November03:41/03:49UTC. Values stayed in process memory/stdin only, no files/log output. Initial sender upload failed; metadata reconciled then one bounded retry succeeded. Agent must remove both temporary secrets after terminal provider verification and verify absence. Deployment settings unchanged.

H1 terminal/cleanup:44984161552/job137090971079 built0errors then exposed a real SDK/HttpClient lifecycle fault; full provider replay not accepted. Both temporary CI secrets were deleted immediately afterward and exact-name404absence verified. Shipping gateway fix is agent-owned; David has no new action. Existing authorization covers the same bounded controlled-recipient verification after correction; restage only for that run and remove afterward.

H1 corrected verification: root SDK lifecycle fix frozen fbe6c724;44985151774/control4dd759d982 active. Existing owner authorization reused for the same bounded controlled-recipient test. Both temporary secrets restaged04:27UTC after prior absence verification; agent must delete/verify absence again after terminal run. No new David action.

H1 completed: corrected44985151774/job137093990560 SUCCESS,0error4m17.55 build; real controlled-sink SDK submission+identical provider-ID replay/native acknowledgement passed. Both temporary CI secrets deleted and exact-name404absence verified after run. No deployment credential changed. Inbox delivery, enabled OPORTAL browser and hosted acceptance remain separate engineering work. No David action remains for H1.

H1 enabled-button follow-up (22 November):44990168013/job137109268095 SUCCESS on6de38f50. Actual Portal SDK/fragment/button accepted one controlled-recipient warning and second click reported previously accepted; one native provider acknowledgement, unchanged avatar/quota. Both temporary CI secrets removed and exact-name GET404 absence verified after terminal; CI DB/tempDNA/container/browser runtime/cache absent. H1 remains complete; no David action. Current Development integration/full Startup/hosted are agent-owned open work.
---

- Status: awaiting an existing approved fixture. Native Urbit and MongoDB federation passed before and after restart, including owner isolation and cleanup. WEB4 Development has none of the six Urbit reader settings and no service named for Urbit. The recovery laptop also has none of the six settings. Only names were inspected.
- Owner action: identify an existing HTTPS Urbit ship suitable for a private teaching contact book and the protected location of its reader session, or state whether to defer hosted enrollment. Use synthetic contacts only. The reader requires the HTTPS origin, ship, restricted session, designated owner, stable connection ID and reviewed page IDs. Keep credentials out of chat and Git.
- Engineering next step: qualify only the reviewed pages and owner, stage the six scoped Development settings, and verify native and MongoDB paired presets and cross-owner denial. [Urbit scope and qualification](../COSMIC%20ORM/URBIT_CQL_MVP.md).

### WEB6 Ask your data Development demo entitlement · 13 November

21 November UTC update: a new scoped read of saved Development WEB6 OASIS DNA still found GeminiFree/OpenRouter API keys and Ollama URL/model unset (four presence booleans only). No secret values printed, inference/data calls, setting changes, new sign-in request or Bronze grant. Existing permanent-Free-route action remains pending; saved settings do not prove loaded runtime configuration. Evidence: [presence-only verification](../COSMIC%20ORM/WEB6_FREE_ROUTE_PRESENCE_2026-11-21.json). Earlier owner decisions and history below are preserved.


**Current action, 17 November UTC** Configure a permanent Free WEB6 route and publish the reviewed additive catalogue using existing authorized access or direct owner publication. The earlier one-time Bronze grants are closed and the visitor is back on Free. Earlier sign-in and grant instructions below record previous steps. No new Bronze approval or sign-in is pending. The two Development configuration steps are described below.

Read-only verification during CQL image44208771977: the Development WEB6 service's saved OASIS_DNA_JSON has none of the four requested Free-route fields configured (GeminiFree/OpenRouter keys and Ollama URL/model). Only presence booleans were reported; no secrets/settings were changed. Saved configuration is not proof of what the running deployment has loaded. Existing action remains pending; no new login or Bronze approval is requested.

16 November UTC. Live Development WEB6 still uses the older published catalogue and exposes only Ollama entries to a fresh Free avatar. Audited additive publication remains pending a reusable authorized Development administration session or direct owner publication. The owner asked us to reuse existing access and avoid repeated sign-ins. The update retains paid models and rates and adds 16 reviewed Gemini/OpenRouter zero-price profiles. It grants no Bronze entitlement. A configured private inference route is also required for a real model call.

16 November UTC. Permanent Free demo setup is pending provider access. The existing Railway WEB6 AI API settings have no configured zero-price inference route. For Development, use the saved OASIS DNA secret configuration to set Web6.ApiKeys.GeminiFree from a separate Google project with billing disabled and Web6.ApiKeys.OpenRouter from a restricted OpenRouter key. To add self-hosted Ollama, supply a private WEB6-reachable service URL and a locally installed approved model via Web6.OllamaBaseUrl and Web6.OllamaFreeModel. Enter values privately. No keys or endpoint credentials belong in Git or chat. Configure an available route first for browser acceptance. Keep staging and production configurations isolated and unchanged pending rollout approval. [Google free-tier billing](https://ai.google.dev/gemini-api/docs/billing) · [OpenRouter free models](https://openrouter.ai/collections/free-models) · [Ollama local API](https://docs.ollama.com/api/introduction).

16 November completion update. David approved exactly one temporary grant and completed the private administrator sign-in. The approved browser attempt received a real WEB6 draft. The requested two-account CQL scope was not confirmed, so execution stopped before making a WEB4 query. The visitor’s prior Free entitlement was verified restored. The one-time grant is consumed. David asked for a permanent free-model route for temporary avatars.

Historical entry (closed by the completion update above). 16 November UTC. David approved one fresh 30-minute Bronze grant for the existing dedicated Development visitor to draft and run one read-only CQL query. The previous protected subscription administrator session is absent from this laptop. Sign in privately from the OASIS2 checkout with `python -B Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py --subscription-admin`. Report successful sign-in only. After applying the scoped grant, engineering will run the browser check from the current WEB4 origin and restore the prior entitlement. No Stripe checkout.

Historical entry (closed by the completion update above). 16 November UTC. The original one-time Bronze grant expired and the dedicated Development visitor is back on Free. The current public WEB4 origin passed Development WEB4 credential checks and the live WEB6 authorization preflight. Engineering verified the exact website build and can use the current public origin with the staged assets for a browser check before the new DNS records arrive. Owner decision pending for one fresh 30-minute Bronze grant for the same visitor, or use an existing authorized Development demo session with WEB6 access. The test will draft one read-only CQL query, show it in the editor for review, and run it against WEB4. The audited grant uses no Stripe checkout. An operator session is needed only if a new grant is selected. [Exact WEB4 preview](https://web4-site-nthsy7gwn-oasis-team.vercel.app/web4-sandbox.html).

16 November follow-up. The compact draft now passes token reservation with the existing Free visitor and reaches model admission. WEB6 reported that Free has no available model. The scoped entitlement request remains pending. The test made no WEB4 query.

14 November completed. The subsequent Development sign-in verified administrator access, the approved 30-minute Bronze grant was acknowledged, and live WEB6 drafting and WEB4 execution returned both requested public Hedera rows. The separate hosted browser walkthrough still needs the preview access and browser origin below.

Historical entry (closed by the completion update above). Earlier 14 November update. Development rejected the first private sign-in and issued no session. The failed local attempt was reconciled and archived, so the original command can be rerun with an existing Development subscription administrator account. The approved grant is still unapplied.

Historical entry (closed by the completion update above). Approved 13 November. David approved the 30-minute Bronze grant for the dedicated Development test visitor and agreed to sign in privately. Waiting for the separate protected Development administrator session before applying it. The Free catalogue only admits Ollama and no Ollama inference route is configured in Development. The deployed WEB4 subscription admin API supports audited plan grants with an expiry and no Stripe checkout. Proposed grant: 30 minutes, dedicated test visitor only, for one real WEB6 draft and WEB4 CQL execution. The prior Wizard sign-in was scoped to Hedera recovery. Run `python -B Scripts/Sandbox/Prepare-CqlRecoveryOperatorSession.py --subscription-admin` privately from the OASIS2 checkout with the existing Development subscription administrator account. The password is hidden and the separate session is protected locally by Windows. Report sign-in completion only. [Plan grant API](../../Devs/API%20Documentation/WEB4%20OASIS%20API/Subscription-API.md)

## COSMIC Web4Site preview access

13 November update. Vercel denied access to the OASIS team when checking the Development preview hostname. A fresh one-time Vercel approval request was sent privately in chat. Once approved, verify the existing preview and inspect the project domain configuration for dev.web4.oasisomniverse.one. The approval link expires after ten minutes and is intentionally omitted here.

Open 13 November. Approve the Vercel OASIS team access request when a fresh one-time consent link is provided, so engineering can open and verify the protected Development demo preview in a browser. No production change is requested. GitHub deployment 7802106867 confirms the successful preview for Web4Site revision 068b892f558a. The connected Vercel account still needs OASIS team authorization. Team authorization allows preview inspection. The preview hostname separately lacks the WEB4 credentialed session origin required for a live browser run. [Protected preview](https://web4-site-26o21ws1l-oasis-team.vercel.app/web4-sandbox.html).

16 November update. Existing authenticated Vercel CLI access verified the protected WEB4 preview. The earlier team-consent action is closed. Vercel verified ownership and assigned dev.web4.oasisomniverse.one to the work/cosmic-cql preview branch.

### WEB4 and WEB6 sandbox DNS

- **Status (17 November UTC)** Open. The previous Vercel project assignments are documented below. Both sandbox names still return NXDOMAIN. Development WEB4 already permits credentialed browser sessions for the intended WEB4 sandbox origin. The generated Vercel preview origin permits bearer requests through wildcard CORS, without session cookies. David chose the sandbox names for consistency with HoloNET. The earlier dev.web4 record request is superseded. The WEB4 alias currently points to the ready preview for website revision 1216613a9c66. Authenticated access to that preview returned HTTP 200 and served the mixed CQL paging asset. Anonymous access to the generated preview URL still redirects to Vercel sign-in.
- **David action** Add two CNAME records in the existing NameSilo zone for oasisomniverse.one.
  - Host sandbox.web4 points to 6022d70f3a03e68f.vercel-dns-017.com.
  - Host sandbox.web6 points to d8f3f9dacdc03719.vercel-dns-017.com.
- **Reason** These records provide the HTTPS addresses for the Development sandboxes. Engineering will verify DNS, certificates, the redirects from each sandbox root, preview protection and the WEB4 signed browser flow. [WEB4 domains](https://vercel.com/oasis-team/web4-site/settings/domains) · [WEB6 domains](https://vercel.com/oasis-team/web6-site/settings/domains)

## Older tracked wallet export review

15 November 2026 UTC. An existing WEB4 wallet export is tracked in the COSMIC history. It includes fields named PrivateKey and SecretRecoveryPhrase. Engineering has not established whether this is synthetic or still usable. Confirm the origin of the export with the wallet owner. If the wallets are real, handle rotation or asset migration through the existing private owner process and coordinate any repository-history remediation separately. Keep values out of chat, Git and this list.

- File: ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/wallets.jsonwallets_83dfa5f8-7e19-4937-a09b-ed70d84fb160.json
- Status: open. Scope limited to identifying the tracked export.

### Blockscout Universal API credential for Development CQL

- Status on 15 November 2026 UTC. Deferred by David. Revisit when he is ready to add the key.
- Use the [Blockscout developer portal](https://dev.blockscout.com/) to create or identify a dedicated Universal API key on the free plan. Store it privately in Development OASISDNA at OASIS.Cql.BlockscoutUniversalApiKey. Keep the value out of chat and source control.
- Engineering wired both hosted readers to this OASISDNA field on the COSMIC feature branch. Hosted configuration and authenticated verification remain deferred.
- Reason. [Blockscout deprecated per-instance API support](https://docs.blockscout.com/devs/apis). The monitored address and Connext log readers need the supported authenticated route. Engineering owns migration, identity preservation, exact live verification and performance checks.

### WEB4 dev MongoDB storage quota

- **Status (2026-11-16 UTC)** Development session and scoped CQL writes recovered. The original visitor renewed successfully and nine owned SQL fixtures were saved and conditionally deleted across the three providers. Engineering still needs to verify the subscription and worker paths. Railway dev deployment 887a53d4 previously logged MongoDB insert and findAndModify rejections at 512 MB of 512 MB. Two disposable sandbox sign-in attempts returned HTTP 400 during avatar creation, before issuing a session.
- **David action** Restore writable storage for the MongoDB deployment backing Railway WEB4 dev. Increase its storage quota, or identify the exact disposable development collection or records that an administrator can remove to recover capacity. If identifying data, provide collection names and their retention scope only.
- **Reason** The quota blocks disposable sandbox avatar creation and other MongoDB writes. Engineering will verify sandbox sign-in, subscriptions and HyperDrive after capacity is restored. Railway continues to log rejected writes from the WEB4 usage settlement and reconciliation workers. Engineering will verify both worker cycles after storage recovery. Railway deployment 3480bc4b subsequently crashed at startup when Atlas rejected subscription index initialization at the same quota. 16 November follow-up: the subscription and outbox index startup repair restored public WEB4 health on Railway 506283cd. MongoDB quota rejections still gate sandbox sign-in, usage worker verification and write-dependent CQL acceptance. [WEB4 dev deployment](https://railway.com/) · [Current status](../HyperDrive-Providers/CURRENT_STATUS.md)

## PIPER parent collection routing and load-balancing decision

21 November latest: David explicitly instructed immediate Development re-enablement for testing. Completed: seven Development saved copies true; existing-image redeploys SUCCESS and seven running startup DNA files true. Fourteen staging/production saved copies remain false; no rollout there. Earlier conditional hold is superseded for Development. PIPER SA/SLP/student-service completeness verification remains engineering work, not a new owner approval or login request.

21 November update: David conditionally authorized re-enabling load balancing only once verified safe. Holo catalogue availability alone is insufficient. Separate SA/SLP/student-service completeness acceptance is still not established in current evidence; retain false and continue engineering verification without new login requests. This supersedes the older wording that the owner decision itself is pending. No source/deployed setting changed and no staging/production authorization inferred.

16 November Cancun time. The owner confirmed that disabling automatic load balancing restored PIPER's missing records. Development `4d0d2022170c` includes the authoritative-parent routing and async visibility fixes. Exact image CI 43981891022 passed. Development WEB4 deployment `b41caa36-ffb0-41cd-a956-f9b86aaa2948` is queued. The real SA, SLP and student-service results remain separate verification items, and the owner has retained the decision to turn load balancing back on.

The saved `OASIS_DNA_JSON` settings on all 21 Railway copies were checked read-only. `OASIS.StorageProviders.AutoLoadBalanceEnabled` is false and `AutoFailOverEnabled` is true in Development, staging and production. Each environment has the same seven service copies: WEB4 OASIS API, WEB5 STAR API, WEB6 AI API, WEB7 SYMBIOTIC API, WEB8 IGN API, WEB9 SINGULARITIY API and WEB10 THE SOURCE API. Existing active staging and production WEB4 deployments predate the saved flag changes. Promote a verified release snapshot with reachable dependency pins after approval, starting with staging. Railway restart reuses the existing deployment and its configuration.

If the owner elects to re-enable the setting after verification, review these exact source paths changed by `dc9230c41cf1`, plus `Docs/Devs/OASIS_DNA_Reference.md`. Preserve the typed default and any intentional isolated fixture settings. This inventory records the mitigation and does not authorize flipping any copy.

- `Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.TestHarness/OASIS_DNA.json`
- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json`
- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.Development.json`
- `ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BaseOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BaseOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.COSMOSOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.RadixOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Network/NextGenSoftware.OASIS.API.Providers.PinataOASIS.TestHarnes/OASIS_DNA.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.TestHarness/OASIS_DNA.Development.json`
- `Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.TelegramOASIS.TestHarness/OASIS_DNA.json`
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.TestHarness/OASIS_DNA.json`

## GATED-RELEASE-RAILWAY-CREDENTIALS-2026-11-20

Status: blocked, 20 November 2026. The login hotfix is on `master` (d62d28e9, master CI green, run 44672495323) but the `Gated Railway release` run 44675640513 failed in its `staging` job with `ValueError: Missing environment-scoped Railway credential`. Production was skipped. Nothing was deployed by this workflow.

Reason: `Scripts/railway_release_gate.py` needs a Railway project token scoped to the target environment, read from the secret `RAILWAY_TOKEN` of each GitHub environment. Agents cannot create it (`projectTokenCreate` returns Not Authorized) and credentials must never be copied into chat or files.

Owner steps:
1. Railway, project OASIS, Settings, Tokens: create a project token for the **staging** environment.
2. GitHub, NextGenSoftwareUK/OASIS, Settings, Environments, `oasis-staging-gated`, Environment secrets: add `RAILWAY_TOKEN` with that token.
3. Create a project token for the **production** environment and add it as `RAILWAY_TOKEN` in `oasis-production-gated`; the production job also reads `RAILWAY_STAGING_TOKEN` (add the staging token there as well).
4. Re-run the failed `Gated Railway release` run (or dispatch it from master with the full SHA), then press Review deployments for `oasis-staging-gated` and later `oasis-production-gated` (reviewer dellams).

Also seen in that run's log: `digest-mismatch: error` during staging preflight; the owning session should check it after the credential is in place.

Links: https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44675640513

---

## Part G — WEB6 model catalogue (22 November 2026)

### G1. Review the estimated prices in the catalogue (now v7)

**Status:** open (price review only), updated 25 November 2026. v6 was pushed on your instruction (WEB6 `0348ded`, OASIS `a712210`). v7 (WEB6 `4c54731`, OASIS `4426ead`) re-imported OpenRouter's current list after 8 imported models were retired and 21 repriced within three days, and bills per-million prices exactly instead of rounding to 4 decimals. Your review is still only the estimated prices below; the OpenRouter prices are OpenRouter's own.

**What it contains**
- OpenRouter: 336 paid text models at OpenRouter's own published per-token prices, imported by `WEB6/Scripts/import_openrouter_models.py` (flat-priced chat models only). Re-run it to track OpenRouter's list. See `WEB6/docs/MODEL_CATALOGUE.md`.
- Six text models: NVIDIA NIM, Meta Llama API, Reka Core, Writer Palmyra X 004, AI71 Falcon 180B, JAIS 30B.
- OpenAI image calls send `quality=medium`, and the quality is part of the billed model key. The per-request hold is unchanged at $2.70.

**What you need to check:** the prices that are my estimates, not published list prices:
- Media: StabilityAI image $0.03, OpenAI gpt-image-1 1024x1024 medium $0.042, Runway 5s $0.25, Luma $0.40, Pika 3s $0.20, Kling 5s std $0.14, ElevenLabs $0.30 per 1,000 characters (calls capped at 4,000 characters), AssemblyAI nano $0.12/hour, Deepgram nova-3 $0.0043/minute (calls capped at 4 hours).
- The six text models above.
If any is wrong, a corrected catalogue needs a new version string (`web6-model-catalogue-YYYY-MM-DD-vN`), because published versions are immutable.

**Verified:** the full WEB6 unit suite passes, 191/191 including the 11 catalogue invariant tests, on a clean checkout of OASIS `Development` (WEB6 `add09f4`, Core `a9fe1ec`), 25 November 2026.

### G2. Decide whether OpenRouter should bill from OpenRouter's reported cost

**Status:** open decision, updated 25 November 2026. Today every billed model needs a fixed price and each request holds back its worst case up front, so tiered and router models (`openrouter/auto`, per-request fees) stay unavailable.

New evidence: OpenRouter's fixed prices do not stay fixed. `qwen/qwen3.5-9b` input went 0.17 -> 0.10 -> 0.17 USD per million within hours on 25 November, as OpenRouter switched upstream providers. Between re-imports WEB6 therefore bills some OpenRouter models below or above cost. The new weekly `WEB6 OpenRouter price drift` workflow fails when a model is retired or priced below cost, but re-importing only narrows the gap.

Billing from OpenRouter's reported per-call cost would close it and open up the router models. It needs a per-call spending cap (the hold) and a change to how billing settles. Please decide: keep fixed prices plus weekly re-imports, or bill OpenRouter from reported cost.

### G3. Security advisories in shared packages

**Status:** resolved, agent-owned, 25 November 2026. AutoMapper is fixed on `Development` (provider projects no longer reference it; ONODE WebAPI uses 16.3.0). `dotnet list package --vulnerable --include-transitive` on a clean `Development` checkout reports no vulnerable packages for WEB6 WebAPI or ONODE WebAPI, so the `Microsoft.AspNetCore.WebUtilities` advisory no longer applies. Nothing is needed from you.

---

## Part G — Messaging, Edge nearby sync, HerzApp and Our World (added 2026-11-19)

### G1. Messaging branches

- **Status (2026-11-23)** Done. `work/oasis-messaging` is merged into Development in OASIS, OASIS-API-Core and STAR ODK, with gitlinks and `Docker/oasis-dependency-versions.env` repinned.

### G3. Publish the messaging npm packages and push the branches

- **Status (2026-11-19)** Open. Committed locally on branch `work/oasis-messaging` in each repo, nothing pushed or published: `OASIS-API-Javascipt-Package-WEB4` (becomes `@oasisomniverse/web4-api` 2.1.0, adds `/messaging`), the six UI libraries `OASIS-{React,Vue,Angular,Svelte,NextJS,JS}-UI-Component-Library` (1.3.0), and the STAR ODK submodule (DevKit downloads; STAR ODK is now merged).
- **David action** After review: tell Claude to push the branches; publish `@oasisomniverse/web4-api@2.1.0` first, then the six UI libraries (they depend on `>=2.1.0`). Publishing needs your npm 2FA (same as A1/A2).
- **Reason** npm publish needs your account and 2FA; repo policy forbids push without your review.
- **Context** [Messaging component](../../Devs/OASIS_MESSAGING_COMPONENT.md).

### G4. Our World Unity package and HerzApp wiring

- **Status (2026-11-19)** Open. HerzApp does not exist as a repository yet; the integration is documented (HerzID login, then `OASISChatWidget`). The Our World integration is documented; the two new Edge assemblies are not yet in the Unity package build.
- **David action** Decide where HerzApp lives so the wiring can be added; approve adding `Edge.NearbySync` and `Edge.Messaging` to the Edge release packaging (the global release takes about 2 hours, so it should ride the next real release, not a separate dry run).
- **Reason** Needs your decision on the HerzApp repository and a release slot.

### G2. Physical-device Bluetooth and mesh acceptance

- **Status (2026-11-19)** Open once the Android/iOS GATT shims exist. The shared stack is verified only with an in-memory radio and real TCP sockets.
- **David action** Provide two Android phones (and one iPhone for cross-platform) for a test: send a message with Wi-Fi/mobile data off, relay through a third phone, then reconnect and confirm it converges on the server with no duplicate.
- **Reason** Radios, MTU negotiation and background restrictions cannot be proven in software.
- **Context** [Edge offline messaging](../../Devs/EDGE_OFFLINE_MESSAGING.md).

## Google Maps and HERE API keys for the map providers

- Open 2026-11-22. GoogleMapsOASIS and HEREMapsOASIS now call the real vendor APIs. They are verified against recorded vendor responses in `map-platforms-unit-tests.yml`, but not yet live.
- Google: in a Google Cloud project with billing, enable the Routes API, Places API (New) and Geocoding API, create one API key restricted to those three APIs, and store it privately as `OASIS_GOOGLE_ROUTES_API_KEY` wherever the providers run (Railway WEB4 service variables, and the GitHub Actions secret for the live routing test). [Routes API](https://developers.google.com/maps/documentation/routes/compute_route_directions) · [Places Text Search (New)](https://developers.google.com/maps/documentation/places/web-service/text-search) · [Geocoding API](https://developers.google.com/maps/documentation/geocoding/requests-geocoding)
- HERE: in the HERE platform portal, create an API key for an app with Routing v8 and Geocoding & Search v1, and store it privately as `OASIS_HERE_ROUTING_API_KEY` in the same places. [Routing v8](https://docs.here.com/routing/docs/routing-v8-get-started) · [Discover](https://docs.here.com/geocoding-and-search/docs/code-discover-place)
- MapLibre needs no key. It defaults to the public OSRM demo and Nominatim servers, which are limited to one request per second and not for production. For production volume, decide whether to self-host OSRM and Nominatim and pass their URLs.
- Never put the keys in Git or chat. Engineering runs the opt-in live tests once the keys exist.

## Hosted InfluxDB and Weaviate read-only CQL sources

- Open 2026-11-24. WEB4 now registers both sources (`CqlInfluxDbReadSourceRegistration`, `CqlWeaviateReadSourceRegistration`). With the settings below unset they stay off. Their hosted tests (`InfluxDbHostedCqlNativeTests`, `WeaviateHostedCqlNativeTests`) compile but cannot run until a hosted teaching source exists. None of these names exist yet on Railway dev or in GitHub secrets.
- InfluxDB (for example InfluxDB Cloud): create an organization and a bucket, a token with read-only access to that bucket, and seed it with the three public teaching records plus one deleted record. Set, on the Railway WEB4 service (dev first) and as GitHub Actions secrets for the hosted test:
  - `CQL_INFLUXDB_QUERY_ORIGIN` (the HTTPS origin only, for example `https://eu-central-1-1.aws.cloud2.influxdata.com/`)
  - `CQL_INFLUXDB_QUERY_ORGANIZATION`
  - `CQL_INFLUXDB_QUERY_BUCKET`
  - `CQL_INFLUXDB_QUERY_READER_TOKEN` (read-only token)
  - `CQL_INFLUXDB_QUERY_PROBE_ID` (the ID of the first live teaching record)
- Weaviate (for example Weaviate Cloud): create a cluster with an `OasisHolon` collection and a read-only API key, seeded the same way. Set `CQL_WEAVIATE_QUERY_ORIGIN`, `CQL_WEAVIATE_QUERY_READER_API_KEY` and `CQL_WEAVIATE_QUERY_PROBE_ID` in the same places.
- The hosted tests also read a small JSON manifest of the seeded IDs, from `COSMIC_INFLUXDB_HOSTED_CQL_SOURCE` and `COSMIC_WEAVIATE_HOSTED_CQL_SOURCE`. Engineering writes that manifest once the sources exist.
- Never put the tokens in Git or chat.

## Promote the cleaned WEB4 sandbox page to the public site

- Open 2026-11-25. The public sandbox at https://web4.oasisomniverse.one/web4-sandbox.html still shows 49 "local demo" steps that tell visitors to run `pwsh -File Scripts/TestHosts/*.ps1` from `C:/Source/OASIS-cosmic`. Visitors cannot run those, and PowerShell is no longer used. Every provider those steps mention now runs live in the hosted CQL Playground.
- Fixed on the Web4Site branch `work/cosmic-cql` in commit 6930aff. The same branch now also has f301621: 'Prepare my sample records' gives the built-in MongoDB, SQLite and Neo4j presets real rows for a new demo visitor (they returned zero rows before), verified against the live dev API. And 3099bf0: the 47 'Open local query' links that point at 127.0.0.1 are hidden for public visitors (they still show when the page is opened locally), and changed scripts get new cache-busting versions. Only the launcher command lines were removed: provider descriptions, the C# walkthrough links and the page anchors are kept, and the site's tests pass (two test files need Node 21+ for `JSON.rawJSON`; this laptop has Node 18).
- What David needs to do: approve promoting `work/cosmic-cql` to the public site (Web4Site `main`). That branch is 343 commits ahead of `main` (and `main` has 1 commit the branch lacks), so promoting it ships all the CQL playground work since mid-November, so review the Vercel preview of `work/cosmic-cql` before approving.
- What it unblocks: the public sandbox page stops showing internal instructions.

## Stop docs-only pushes from redeploying WEB4-WEB10 (Railway Watch Paths)

- Open 2026-11-25. Every push to `Development` rebuilds the dev WEB4-WEB10 services, even when only docs change. The new WORK-CLAIMS rule makes every session push `claim:` commits to Development: 11 of about 22 commits in the last two hours were docs-only claims, and 7 WEB4 dev deploys were queued at once. The sandbox keeps restarting, live checks can't find a stable window, and builds pile up.
- What David needs to do: in the Railway dashboard, for each dev service WEB4-WEB10 (Settings, then Build, then Watch Paths), add patterns so changes that touch only docs never deploy, for example `**` plus the exclusions `!Docs/**`, `!**/*.md` and `!.github/**`. The same setting can be applied to production too.
- Why David: this changes Railway service settings, which agents must not change on their own.
- What it unblocks: claims, status and doc updates stop restarting the live sandbox; real code changes still deploy as before.

## GitHub Actions has stalled: 100+ runs queued, one running

- Open 2026-11-25 (about 02:50 UTC). In the OASIS repo, 100+ workflow runs are queued and only one is running: 61 queued from Development pushes, plus every session's branches. CI jobs queued an hour ago have not started. The organisation is on GitHub's Free plan, so the private-repo Actions minutes are probably used up or a spending limit has been hit. Agents cannot check this: it needs the org admin billing scope.
- What David needs to do: open GitHub, NextGenSoftwareUK organisation, Settings, then Billing and plans (Actions usage and spending limit). Either raise the spending limit or upgrade the plan, or decide that sessions must run less CI. Optionally, cancel the stale queued Development runs so the newest ones run first.
- Why David: billing and plan changes are his decision.
- What it unblocks: every session's CI checks (COSMIC ORM rows, HyperDrive providers, the CQL sandbox fixes). Right now no verified batch can reach Development.
## Cloud session `cloud-hyperdrive-A` cannot push to GitHub (2026-11-25)

- Context: the HyperDrive provider loop must push claims to Development and run CI. In this cloud session both `git push` and the GitHub MCP write calls return 403 ("Resource not accessible by integration"). Reads work.
- Steps: connect or reconnect GitHub at https://claude.ai/connect-github, and install the Claude GitHub App on NextGenSoftwareUK/OASIS (or ask an org owner): https://github.com/apps/claude/installations/select_target. Then start a new session with the repo selected.
- Reason: without push there are no claims and no CI runs, so no provider can be marked done.
- Status: DONE 2026-11-25. GitHub reconnected; claim and this entry pushed to Development (b025d919).

## Hosted-only providers: accounts needed for native storage tests (cloud-hyperdrive-A, 2026-11-25)

- Context: these providers have no self-hostable backend, so their native storage suite (26/26 sync + async) can only run against a real hosted account. Each needs a dedicated, empty test project/database that CI may fill and clear. Never reuse a production database. Put the values in GitHub Actions secrets (names below), never in Git or chat.
- Steps, one account each (free tiers are enough):
  - Nile (thenile.dev): create a database `oasis-ci`, create a database user; secret `NILE_CI_CONNECTION_STRING`.
  - Cloudflare D1: create a D1 database `oasis-ci`; an API token with D1 edit on that account; secrets `CLOUDFLARE_ACCOUNT_ID`, `CLOUDFLARE_D1_DATABASE_ID`, `CLOUDFLARE_D1_TOKEN`.
  - Netlify Blobs: create a site `oasis-ci`, a personal access token; secrets `NETLIFY_SITE_ID`, `NETLIFY_BLOBS_TOKEN`.
  - MotherDuck: create database `oasis_ci`, an access token; secret `MOTHERDUCK_CI_TOKEN`.
  - Cloudinary: create a product environment `oasis-ci`; secrets `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`.
  - Deno Deploy (KV): create a project `oasis-ci` with a KV database, an access token; secrets `DENO_DEPLOY_PROJECT`, `DENO_DEPLOY_TOKEN`.
  - Fly.io: create an app `oasis-ci` with the storage the FlyIOOASIS provider uses (Postgres/LiteFS per its README), a deploy token; secrets `FLY_CI_APP`, `FLY_CI_TOKEN`.
  - Neo4j Aura: create a free AuraDB instance `oasis-ci`; secrets `NEO4J_AURA_CI_URI`, `NEO4J_AURA_CI_USER`, `NEO4J_AURA_CI_PASSWORD`. (Neo4jOASIS itself already passes on a local Neo4j; Aura only adds the hosted path.)
  - LanceDB Cloud: create a project and API key; secrets `LANCEDB_CLOUD_URI`, `LANCEDB_CLOUD_API_KEY`.
- Decision needed, services that appear to be discontinued: Fauna (FaunaDB shut down in 2025), Vercel KV (sunset; existing stores moved to Upstash, which UpstashOASIS already covers) and Polybase (testnet no longer reachable; please confirm). Options: (a) mark FaunaOASIS, VercelKVOASIS and PolybaseOASIS as retired and remove them from the provider matrix, or (b) keep them as historical code with no test target. Engineering's recommendation is (a).
- Reason: without these, these providers cannot be marked done; every self-hostable provider is being done meanwhile.
- Status: OPEN 2026-11-25. Claims marked BLOCKED in WORK-CLAIMS.md.

## Live CQL sandbox: demo databases and indexer keys (cloud-cql-C, 2026-11-25)

- Context: about 40 self-hostable database providers already have CQL adapters with local or CI proof, for example Redis, Valkey, KeyDB, Dragonfly, Garnet, PostgreSQL, TimescaleDB, CockroachDB, SQL Server, Cassandra, ScyllaDB, Elasticsearch, OpenSearch, Solr, Meilisearch, Typesense, Qdrant, Weaviate, Milvus, Chroma, CouchDB, Couchbase, ArangoDB, SurrealDB, RavenDB, InfluxDB and Memcached. They are not live in the dev sandbox only because Development WEB4 has no running instance of them to read. The public chains are mostly done, and the remaining keyless public sources are being added by the cloud sessions (Rootstock, Story, Lens, QuestDB on 25 Nov).
- Decision needed: may the CQL sessions add small demo database services to the Railway **development** environment only (one service per provider, using Railway's own templates where they exist, for example Redis and PostgreSQL), seeded with the same three public teaching records each provider already uses in CI?
  - If yes: create a project token scoped to the development environment and store it as the GitHub Actions secret `RAILWAY_DEV_DEMO_TOKEN`, not in Git or chat. The sessions then add each service, seed it, set its `CQL_*` variable and verify that its presets return 3 and 2 records.
  - If no: these providers stay CI-verified only and are not shown live.
  - Nothing has been created or changed yet. This entry only asks for approval.
  - Cost: yes, this adds Railway cost. Railway bills usage around the clock, even for idle services. Published rates checked 25 Nov 2026 ([railway.com/pricing](https://railway.com/pricing), [docs](https://docs.railway.com/reference/pricing/plans)): RAM $10 per GB-month, CPU $20 per vCPU-month, volume storage $0.15 per GB-month, egress $0.05 per GB. Memory held while idle is most of the cost. These estimates are for an idle demo with three records each, not measured:

    | Group | Providers | Idle memory each | Rough cost each |
    | --- | --- | --- | --- |
    | Light | Redis, Valkey, KeyDB, Dragonfly, Garnet, Memcached, PostgreSQL, TimescaleDB, CouchDB, Meilisearch, Typesense, Qdrant, Chroma, SurrealDB, PocketBase, InfluxDB | 50-200 MB | about $1-3 a month |
    | Medium | ArangoDB, RavenDB, CockroachDB, SQL Server | 300-800 MB | about $4-9 a month |
    | Heavy (JVM or multi-process) | Cassandra, ScyllaDB, Elasticsearch, OpenSearch, Solr, Couchbase, Milvus | 1-2 GB | about $11-25 a month |

    All of them together: roughly $150-300 a month, most of it from the heavy group. The light group alone: roughly $25-50 a month.
  - Ways to keep it small:
    1. Recommended first batch: only the light group (16 services, about $25-50 a month). It covers most of the list.
    2. Railway's serverless (sleep when idle) setting cuts idle cost a lot. The first query after a sleep waits while the database wakes, so the sandbox looks slow or briefly unavailable for that provider.
    3. Share one service where the adapter allows it, for example one PostgreSQL for the PostgreSQL, TimescaleDB and pgvector demos, as separate databases.
    4. Leave the heavy group CI-verified only. They already pass in CI.
- Keyed indexer APIs (free tiers are enough, read-only): Alchemy, QuickNode, Ankr, Covalent (GoldRush), Moralis, Dune, Zapper, Tenderly, Goldsky and Nansen. Each needs one read-only key. Save each key on the Railway development WEB4 service as `CQL_<PROVIDER>_PUBLIC_READER_KEY`, for example `CQL_ALCHEMY_PUBLIC_READER_KEY`. These are lower priority than the demo databases.
- GOMap routing origin: WEB4 deliberately refuses the public OSRM demo servers (`router.project-osrm.org`, `routing.openstreetmap.de`), because their usage policies forbid this use. Choose one option: (a) a small OSRM container on Railway development with the Berlin extract (fits the same demo-database approval above), or (b) a hosted routing API account. Then set `CQL_GOMAP_ROUTE_QUERY_SOURCE_ORIGIN` and `CQL_GOMAP_ROUTE_QUERY_SOURCE_OWNER`.
- Retirement decision (same question as Fauna, Vercel KV and Polybase above): BasechainOASIS and LoomOASIS target Loom Network's Basechain sidechain. **Not verified:** this comes from background knowledge that Loom stopped developing Basechain years ago and that the network may be shut down or unmaintained. Nobody has probed its RPC (`basechain.dappchains.com`) or found an official notice. Polybase is also unconfirmed (its testnet was reported unreachable). Fauna (hosted FaunaDB, shutdown announced in 2025) and Vercel KV (retired, stores moved to Upstash, which UpstashOASIS covers) match public announcements. On request, a CQL session will send one read-only request to the Basechain and Polybase endpoints and look for official notices before you decide. Options: retire them from the matrix, or keep them as historical code.
- Reason: without these, the providers above cannot return real records in the live sandbox. Everything that needs no account is being done meanwhile.
- Status: OPEN 2026-11-25.

## ONODE Manager release, signing and alerts - 2026-11-25

Status: code work continues without these; only David can do them.

1. Code signing (optional; unsigned builds still ship). Add these GitHub Actions secrets to NextGenSoftwareUK/OASIS. `release-onode-manager.yml` signs automatically once they exist:
   - Windows: `WINDOWS_CERT_PFX_BASE64` (base64 of the .pfx) and `WINDOWS_CERT_PASSWORD`.
   - macOS: `MACOS_CERT_P12_BASE64`, `MACOS_CERT_PASSWORD`, `MACOS_SIGN_APP_IDENTITY`, `MACOS_SIGN_INSTALL_IDENTITY`, `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_PASSWORD` (app-specific password).
2. First public ONODE Manager release. Run Global OASIS Release with `optional_components` including `onode_manager` and `publish=true`. It takes about 2 hours and creates a public GitHub release. Until it runs, the ONODE Manager download links on the sites stay dead.
3. Dependabot: GitHub reports 669 alerts on the default branch (47 critical). Authorise the GitHub connector in Claude Code (`/mcp`) so a session can list and triage them.

## GitHub Actions queue stall blocks COSMIC row merges - 2026-11-25 (cloud-cosmic-B)

- Context: the COSMIC native qualification runs for O18 local view ([45519967748](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45519967748), queued since 00:40 UTC), M24 defaults/computed values ([45524783092](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45524783092), queued since 01:18) and O17 model diagnostics (branch `work/cloud-cosmic-o17`) have not started. Runners are occupied by full `OASIS CI/CD Pipeline` and `Subscription usage protocol` runs that every push to Development starts, including one-line claim commits; several have run for over an hour.
- All three rows pass their native MongoDB/SQLite/Neo4j journeys locally; they merge to Development as soon as their CI is green. Nothing is needed from you for the code.
- Decision for you (optional): either raise the account's concurrent-runner limit, or approve adding `paths-ignore: ["Docs/**"]` (or a concurrency group with cancel-in-progress) to `ci-cd.yml` and `subscription-usage-protocol.yml` so documentation-only claim commits stop queuing full pipelines. Agents will not change these deployment workflows without your go-ahead.
- Status: RESOLVED for these rows 2026-11-25 07:45 UTC: all four runs eventually ran and passed (O18 finished 02:20, M24 03:08, O17 03:15, M19 03:14; up to 1h40m queued) and were merged. The docs-only-push decision above remains open for you.

## Ceramic test node image: approve a community build or build from source (cloud-hyperdrive-A, 2026-11-25)

- Context: CeramicOASIS now passes the native storage suite against a real ceramic-one 0.58.0 node. The official image `public.ecr.aws/r5b3e0r5/3box/ceramic-one` no longer exists (the registry answers NAME_UNKNOWN), so the CI fixture uses the community build `docker.io/dclimate/ceramic-one:0.58.0`, pinned by digest (see `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CeramicOASIS.IntegrationTests/start-ceramic-fixture.sh`). It runs only in CI, against throwaway data.
- Decision: (a) accept the pinned community image for CI tests, or (b) build ceramic-one from the upstream rust-ceramic source in CI (slower, adds a Rust toolchain step). Engineering's recommendation is (a), keeping the digest pin.
- Status: OPEN 2026-11-25. Option (a) is in use until you decide.

## POST-RELEASE-2026-11-24

Production runs OASIS master cfea33eae2c (PR #518) on WEB4-WEB10.

What passed:
- Health: WEB4 /api/Health and authenticated avatar, HyperDrive and subscription calls; WEB6 a2a network calls; WEB7 symbiosis (real lookup); WEB8 mesh nodes; WEB5 (api.starnet) health. All 200 or a correct domain error.
- CQL: query-cql and query-cql/scalar return without errors on MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS.

1. WEB9 refused every bearer (ACCESS_SESSION_AUTHORITY_NOT_ADMITTED).
   Cause: WEB9 booted OASIS only when PREPARED_STORAGE_ADMISSION_JSON was set. Fixed on Development in e9b0c22fd18 (boots the same way as WEB6-WEB8).
   Your step: promote to master and redeploy WEB9 production. Then check GET https://api.web9.oasisomniverse.one/v1/singularity/status with a sandbox bearer.

2. Production SQLite replication is still held. Route hyperdrive-target-v2:SQLLiteDBOASIS:...AB2C2, seq 2, operation 41844155-6e4a-4c17-bc09-46d03a4a61c3.
   - It was held with STORAGE_BINDING_INVALID. The shipped /app/OASISSQLLiteDB.db is now gone, as confirmed in the container.
   - It was re-armed once with --cosmic-rearm-held-head: operator a5e52d14-7e82-43b4-8f21-0a53529246b6, request /data/COSMIC/ReplicaRearm/prod-seq2.json. The result was success, and a Rearm audit record is on the row.
   - The retry (attempt 2) failed with COSMIC_REPLICA_STORAGE_BINDING_MISMATCH: "The native receiver does not match the enrolled storage identity." Seq 4-20 (9 entries) are Pending behind it.
   - MISMATCH is deliberately not re-armable, and --cosmic-repair-baseline covers only lineage conflicts. No supported tool releases it. Do not re-arm again.
   - Needs a code investigation: the route's enrolled target fingerprint versus the /data/OASISSQLLiteDB.db CosmicReplicationStoreIdentity row.
   - Dev is separate and unchanged: v2 ...4982 is held at 543 (AUTHORIZATION_DENIED, 333 behind), and v1 SQLite is held at 535 (LINEAGE_CONFLICT). The dev current route ...2461 is drained.

3. Test records are still on production and dev, because the sandbox sweep is broken.
   - The production log says, on every 15-minute sweep: "Listing MongoDBOASIS holons of <avatar> failed: Mongo holon collection read failed", then "removed 0 demo avatars and 0 holons; 6 kept for retry".
   - The read is MongoDBOASIS.ReadHolonCollectionGraphAsync. Its exception is swallowed, so the cause needs a reproduction.
   - Production leftovers:
     - demo holons 61b08937-ea30-6ed0-ed21-9642411c8b76 (Espresso), 16074723-5dbe-a6a2-e71d-6d1860e5e393 (Flat White) and b2b542d7-f855-89a4-2421-468bcf14666c (Tea), owner 7e2a53a2
     - probe holons ed6b2550-eace-3f0e-d20e-abad408680df, 11eec3b0-b947-b0f5-c62b-ef67af11a7dc, e0efb380-7470-9afa-11a4-ec68697080ad and 950cf30a-bcfb-c18d-b77f-3ae4fec2dbb5
     - sandbox avatars from this session: a9ca5a2e-dd6a-4def-98ef-d33e7cae60e0 and d0745e8f-8df3-4e0e-8fce-3690f53ff416. They own no holons.
   - Dev leftover: probe holon 4c15ba78-ff19-5a49-89d4-5c78abf8d6c6.
   - Owner sessions have expired, and avatar delete needs admin rights. Direct database deletes would leave their replication obligations (some are in the held queue above) inconsistent, so nothing was force-deleted.
   - Once the sweep is fixed, it removes them by itself. Then confirm in Mongo that the IDs above are absent.

## ONET: two decisions before the last protocol items (2026-11-24)

Status: decided by David, 24 November 2026; engineering in progress (no further owner action). Plan: [ONET_STORAGE_AND_HOLONET_PLAN_2026-11.md](../ONET/ONET_STORAGE_AND_HOLONET_PLAN_2026-11.md).

1. Message encryption (audit item #24): **encrypt, with a coordinated upgrade.** ONET application frames move to version 2 (encrypted for the target node and signed). Older nodes stop exchanging messages with upgraded ones, so every ONET node (Railway WEB4 and any self-hosted ONODEs) must be on the same release.
2. Holochain-carried ONET traffic (plan item 8): **build the `onet` zome now.** It is a coordinator-only module in the OASIS hApp (DNA hash unchanged).

### POST-RELEASE-2026-11-24: update 2026-11-25
- Sandbox sweep: root cause found and fixed in PR #527 (CI running).
  - V2 HolonManager.LoadAllHolons dropped the avatar, so the sweep listed the whole Mongo Holon collection. Production has 18 legacy duplicate-HolonId groups (259 extra docs, June/July 2026), which that read rejects as ambiguous.
  - Fix: Core 1aa5cb4b (on Core Development) plus a native owner filter in Mongo, with a real-Mongo regression test.
  - Your decision: the 259 legacy duplicate documents need a reviewed data repair. Nothing was deleted.
- BINDING_MISMATCH: root cause found.
  - The 2026-11-23 process enrolled route ...AB2C2 against /app/OASISSQLLiteDB.db: endpoint fingerprint 4171877D... (that is sha("sqlite-file-v1:/app/OASISSQLLiteDB.db")), store bfcf03a6-dbe4-408b-b3d7-97a733e5e229.
  - That image-shipped file and the identity it created disappeared on redeploy. The live store is /data/OASISSQLLiteDB.db, identity 93d05201-d755-4e9a-a24d-be34e269af7c.
  - Current boot uses the DNA path /data, and no SQLite enrollments have happened since 2026-11-23, so new writes are not affected.
  - All 10 stuck obligations (seq 2-20) belong to the 2026-11-23 demo and probe test holons, which are due for deletion anyway.
  - Your decision: an obligation whose enrolled target store no longer exists can never be delivered as enrolled. Releasing it needs a new reviewed tool, either to retarget it to the successor store (re-binding plus normal or baseline delivery) or to retire it with recorded evidence that the target is gone. Both change the "never reset or skip" rule, so I did not build one without your choice.
- The production release (promotion PR and Railway deploy of WEB9 + sweep fixes) is waiting for your go-ahead.

## Part I — HyperDrive replication on dev (24 November 2026)

### I1. Neo4j to SQLite hold at sequence 14 (dev)

- Status, 25 November 2026: David approved the reviewed baseline repair in the CQL sandbox session, which ran it once.
  The target refused it before any write: `COSMIC_REPLICA_INITIALIZATION_RECONCILIATION_REQUIRED` (exit 1). Request
  `/data/COSMIC/ReplicaRepair/neo-seq14-request.json` (init operation 9c165163-0a7d-4a14-93e7-a0fb500b5c23) is spent:
  never resubmit or regenerate it; its journal stays at phase Submitted with no receipt. The source obligation is
  unchanged and 535/543 are untouched.
- What the inspection showed (`--cosmic-inspect-held Neo4jOASIS 14`, read-only, Development 8b7614d3): operation
  b64bce63-3aeb-40c0-b3b2-b75675a87023, root holon 715eb09e-ce75-4ab3-9f4e-e76c0493281a, actor
  c0b43739-cc8e-48c3-9a6f-6a071a95170f, payload digest 6B8C1A22…398D, state ReconciliationRequired.
- Why it was refused: that holon is already in the SQLite target, installed on 30 October by the Mongo to SQLite
  sequence-8 repair. The same holon now arrives from a second source (Neo4j), and a baseline only installs into an
  absent target. This is not a data loss; it is two enrolled sources governing one target replica.
- Next step (engineering, read-only): compare the held Neo4j payload with SQLite's current head for 715eb09e (version
  and content digest). Then:
  - If they are the same content, the hold can be acknowledged as already satisfied. That needs a new reviewed
    "equivalent head" completion in the repair command (it does not exist yet); engineering will build it.
  - If they differ, Mongo and Neo4j have diverged for this holon and **David decides which lineage wins** (keep the
    Mongo-derived SQLite copy and retire the Neo4j obligation, or replace it with the Neo4j version).
- Also found: `OASIS_SANDBOX` is not set on the dev service, so the shell repair/inspect commands need
  `OASIS_SANDBOX=true` on their command line.

### I2. Add the IPFS catalogue pointer key to Railway dev

- Status: open, 25 November 2026.
- What: in Railway, project OASIS, environment **dev**, service WEB4 (`5123199a-43c4-4439-9921-e10e2aec7841`), edit the
  variable `OASIS_DNA_JSON`. In `OASIS` → `StorageProviders` → `IPFSOASIS`, add
  `"CatalogueRootPointerPath": "/data/IPFS/catalogue-root"` next to `LookUpIPFSAddress`. Change nothing else.
- Why: the entrypoint rewrites the DNA file from `OASIS_DNA_JSON` on every boot, so IPFS forgot its current catalogue on
  each restart and replication failed with "The IPFS catalogue has no persisted storage identity". The code fix (now on
  Development, commit 8ebfa79c) keeps the catalogue root in this file on the `/data` volume. The agent could not set it:
  the 31.6K-character value is too long for a Windows command line, and Railway's API refuses the CLI login token from a
  script.
- After: the next deploy picks it up; the first IPFS write upgrades the old catalogue and the replication-source error
  stops. Tell the agent when it is saved and it will verify in the dev logs.
- Unblocks: IPFS as a HyperDrive replication source on dev. Staging/production need the same key when promoted.

## COSMIC A05 decision: should `ParentHolon` keep driving persisted parent ids? - 2026-11-25 (cloud-cosmic-B)

- Context: checklist row A05 still needs EF-style inverse reference fix-up (a tracked child's `ParentHolon` pointing at its tracked parent instance). It was implemented and tested in cloud-cosmic-B, then withdrawn before pushing.
- Blocker: the save path (`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Private.Helpers.cs`, around line 594) treats a non-null `ParentHolon` as authoritative. It overwrites `ParentHolonId` from it, and as a "fail-safe/fallback" copies the parent's celestial ids (ParentStarId, ParentPlanetId, ParentMultiverseId and others) into the child. With fix-up on, re-parenting by `ParentHolonId` was silently undone on all three stores, and saving a child could change its stored celestial ids. Bypassing this inside the COSMIC session would be a parallel path, which the root-cause policy forbids.
- Decision needed (pick one):
  1. COSMIC sessions treat `ParentHolonId` as the persisted truth and `ParentHolon` as a non-persisted navigation. The legacy derivation stays for non-COSMIC STAR/ODK callers only. A05 then ships with fix-up. Recommended.
  2. Keep the legacy behaviour everywhere. A05 is then marked as not applicable, documenting that `ParentHolon` assignment is a write.
- Reason: either choice changes what a save writes, which affects STAR/ODK celestial graphs, so it is your call.
- Status: OPEN 2026-11-25 08:40 UTC. The claim in WORK-CLAIMS.md is marked BLOCKED; other rows continue.

## Approve deleting the six LOST_METHODS_REVIEW files

- Open 2026-11-25. These files hold commented-out methods from the file-split audit, each annotated with a verdict (almost all "superseded"). They compile to nothing but clutter the code base:
  - `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.LOST_METHODS_REVIEW.cs`
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.LOST_METHODS_REVIEW.cs`
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/OAPP System/OAPPManager.LOST_METHODS_REVIEW.cs`
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/OAPP System/OAPPTemplateManager.LOST_METHODS_REVIEW.cs`
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/OAPP System/RuntimeManager.LOST_METHODS_REVIEW.cs`
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/STARNET System/STARNETManagerBase.LOST_METHODS_REVIEW.cs`
- Decision needed: reply "delete them" to have them removed, or name any method you want restored first. The audit found no genuine losses beyond WalletManager, which was already restored.

## WRLD 3D is gone: decide the WRLD3D provider and check the Our World map

- Open 2026-11-25. wrld3d.com no longer serves WRLD: the home page, developer page and wrld.js docs return unrelated gambling spam, and docs.wrld3d.com redirects there. WRLD never offered a server-side API (maps were drawn client-side by wrld.js and its Unity and mobile SDKs).
- Done by engineering: WRLD3DOASIS no longer reports success for map operations; each returns false with the reason in `LastError`. The Web4Site homepage no longer loads WRLD's logo from wrld3d.com (Web4Site main 065b4ee).
- Decision 1: keep WRLD3DOASIS as an unavailable provider, or remove it (project, solution entries, `MapProviderType.WRLD3D`, NuGet listing). Reply "remove WRLD3D" or "keep".
- Decision 2: the Our World Unity project ships WRLD streaming resources (`Assets/StreamingAssets/WrldResources/`), so its in-app map probably depends on WRLD's servers. Please open Our World and check whether the map still loads. If it does not, choose a replacement renderer for Unity (Mapbox Maps SDK for Unity, Google Maps 3D, or Cesium for Unity) and engineering will migrate it. Mapbox already has a working OASIS provider (MapboxOASIS) to pair with its Unity SDK.

## Published ONODE.Core 2.0.1 NuGet package cannot be restored

- Open 2026-11-25. `NextGenSoftware.OASIS.API.ONODE.Core` 2.0.1 on nuget.org depends on `Ipfs.Http.Client` >= 0.42, which does not exist on nuget.org (the newest there is 0.33.0). Any project that falls back to the package (for example a checkout without the ONODE.Core submodule, or an external NuGet consumer) fails restore with NU1102.
- The source is already fixed: IPFSOASIS now packs the local Ipfs.Http.Client DLL privately instead of declaring it as a NuGet dependency. Only the published package is stale.
- Decision needed: approve publishing a new ONODE.Core version (and the IPFSOASIS package) through the normal NuGet release, so the fixed dependency graph reaches nuget.org. Existing versions are not changed or republished.

## Shared NuGet cache on the dev machine is corrupt (Mvc.Testing 10.0.0)

- Open 2026-11-25. `C:\Users\David\.nuget\packages\microsoft.aspnetcore.mvc.testing\10.0.0` is half-deleted and its `Microsoft.AspNetCore.Mvc.Testing.Tasks.dll` is locked by a running build server, so restores that need it fail with "Access denied". Sessions currently work around it with a private `NUGET_PACKAGES` folder and `-nodeReuse:false`.
- Fix (briefly interrupts other sessions' builds): when no release build is running, run `dotnet build-server shutdown`, delete that `10.0.0` folder, then restore any project once to re-download it. Reply "repair the NuGet cache" and Claude will do it.

## Bot and pinning accounts for Telegram, Discord and Pinata native storage tests (cloud-hyperdrive-A, 2026-11-25)

- Context: these three providers can only be tested against the real hosted services. Each needs a dedicated test account or bot that CI may fill and clear. Put values in GitHub Actions secrets, never in Git or chat.
- Steps:
  - Telegram: create a bot with @BotFather and a private test channel with the bot as admin; secrets `TELEGRAM_CI_BOT_TOKEN`, `TELEGRAM_CI_CHAT_ID`.
  - Discord: create an application and bot in the Discord developer portal, invite it to a private test server with a dedicated channel (send/read/manage messages); secrets `DISCORD_CI_BOT_TOKEN`, `DISCORD_CI_CHANNEL_ID`.
  - Pinata: create a free Pinata account and a scoped API key (pinning + unpinning) for a test group; secret `PINATA_CI_JWT`.
- Note: Telegram and Discord message history is not a durable keyed store. Engineering will first check whether either can meet the suite's contract within platform rules, and will classify a provider as non-storage rather than force it, as was done for Farcaster.
- Status: OPEN 2026-11-25. Claims marked BLOCKED on the claims branch.
