# What I need from David

Updated 15 October 2026 (America/Cancun).

These are the remaining owner-account actions for the staging/production gate and OurWorld Unity Cloud. Never paste passwords, API keys, connection strings or tokens into chat. No new Mongo cluster, paid plan or environment is needed for the gate.

## 1. Create two Railway project tokens

1. Open [Railway OASIS](https://railway.com/project/712f50f6-3b7b-46d4-b229-44b49922f16b).
2. Click **Project Settings → Tokens → Create Token**.
3. Name the first token `github-staging-gate`; select environment **staging**. Create it and retain the value securely.
4. Create a second token named `github-production-gate`; select environment **production**.
5. Use environment-scoped **project tokens**, not personal account tokens. If token creation is denied, use the project/workspace owner account; do not broaden an unrelated credential.

### Save the tokens in GitHub

Open [OASIS Settings → Environments](https://github.com/NextGenSoftwareUK/OASIS/settings/environments). Select each environment below, then **Environment secrets → Add secret**. Enter the exact name and corresponding token; click **Add secret**.

| GitHub environment | Secret name | Value |
|---|---|---|
| `oasis-staging-gated` | `RAILWAY_TOKEN` | Staging-scoped project token |
| `oasis-production-gated` | `RAILWAY_TOKEN` | Production-scoped project token |
| `oasis-production-gated` | `RAILWAY_STAGING_TOKEN` | The same staging-scoped project token |

- [ ] All three environment secrets saved.

Do not put these at repository scope. Do not change the existing master-only restrictions, required reviewer `dellams`, or disabled administrator bypass.

## 2. Create the staging-only MongoDB user

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

## 3. Install that user's connection string in Railway staging

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

## 4. Tell me when the gate setup is ready

Reply with this nonsecret confirmation:

> Railway environment secrets installed. Mongo user `oasis_staging_gate` has readWrite only on the two staging databases. Staging connection strings updated without deploying.

If you used a different username, tell me that username only. I will record the reviewed username in gate policy, verify the configuration without exposing secrets, obtain exact-source CI/promotion evidence, and run staging acceptance.

### Your final production approval

After staging passes, I will send the exact GitHub run link. Sign in as **dellams**, open **Review deployments**, select `oasis-production-gated`, inspect the candidate/staging evidence and click **Approve and deploy** only if satisfied. I will not approve on your behalf. Approval is not needed yet; there is no accepted staging candidate waiting today.

The gate promotes the same source SHA and pinned dependencies, not a byte-identical prebuilt container image. Live end-to-end proof remains incomplete until staging, your approval and production acceptance all pass.

## 5. Separate remaining task: OurWorld Unity Cloud

Follow [the detailed OurWorld access guide](OURWORLD_UNITY_CLOUD_ACCESS.md). Owner actions:

- [ ] Sign in to [Unity Cloud](https://cloud.unity.com/), select/create the intended OurWorld project in your organization, and approve the permitted Build Automation scope/budget.
- [ ] Create a scoped Build Automation service account/key for that project.
- [ ] In [OASIS Actions secrets](https://github.com/NextGenSoftwareUK/OASIS/settings/secrets/actions), save `UNITY_CLOUD_KEY_ID` and `UNITY_CLOUD_SECRET_KEY` as repository secrets.
- [ ] In [OASIS Actions variables](https://github.com/NextGenSoftwareUK/OASIS/settings/variables/actions), save `UNITY_CLOUD_ORG_ID` and `UNITY_CLOUD_PROJECT_ID`.
- [ ] Authorize Unity's read access to `NextGenSoftwareUK/Our-World` and required private dependencies/LFS assets only.
- [ ] Tell me the entries are saved, which build targets you approve, and the build/spend limit. Do not send key values.

I can then verify access and configure the Cloud project/integration within that approved scope. Saving credentials alone does not configure Cloud or prove a build. No legacy ULF/GameCI activation or unapproved billing will be used.

## What you do not need to do

Do not republish existing packages/releases, rerun old promotions, enable Railway push autodeploys, enable the staging Stripe webhook manually, or delete caches/environments. The HoloNET hosted-conductor verification repair is my remaining engineering work, not an owner setup task. Physical-device/two-device acceptance is separate and not proved by CI or publication.

Technical details: [staging-gate runbook](RAILWAY_STAGING_GATE.md) · [release status](OASIS_RELEASE_STATUS.md).
