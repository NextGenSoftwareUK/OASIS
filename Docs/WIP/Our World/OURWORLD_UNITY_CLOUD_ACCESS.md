# OurWorld Unity Cloud access

Cloud Build Automation only: do not use UNITY_LICENSE, ULF activation or the legacy GameCI job.

## Owner actions

1. Sign in at https://cloud.unity.com as an Organization Owner or Manager. Select the organization that owns OurWorld. Select its existing OurWorld project, or create an OurWorld project if none exists. Send the organization ID and project ID (or project dashboard URL); these are identifiers, not credentials.
2. Enable DevOps / Build Automation for that project if it is not enabled. Review and approve Unity's service/billing terms and spending limit yourself. Configuration access does not authorize an unlimited build budget.
3. Account menu → Manage organization → Administration → Service Accounts → Create service account. Name it `OurWorld Build Automation`. Under Keys, Create key; retain its key ID and secret securely. Add a project role that grants Build Automation project/configuration read and write plus build execution. Use the role permissions shown in the current dashboard; do not assume a generic organization administrator role is necessary. If project creation is still required, create the project yourself first so credentials can remain project-scoped.
4. In the OASIS GitHub repository, Settings → Secrets and variables → Actions → Secrets, create `UNITY_CLOUD_KEY_ID` and `UNITY_CLOUD_SECRET_KEY`. Use the generated values. GitHub secrets cannot be read back by this assistant; the setup will run through a hosted workflow using them with masked output.
5. On the same page's Variables tab, create `UNITY_CLOUD_ORG_ID` and `UNITY_CLOUD_PROJECT_ID`. Tell the assistant that the four entries are saved. Never paste secret values in chat, source files, release notes or command arguments.
6. Unity Build Automation needs read access to `NextGenSoftwareUK/Our-World` and its required private submodules/LFS assets. Authorize its GitHub connection for the selected repository (and required private dependencies) when prompted. Do not give blanket access to unrelated repositories. For an SSH integration, add the public deploy key Unity supplies with read-only access; never disclose the private key.

## Assistant work after access is available

Verify the service account can access the exact project; configure and validate the repository connection, project root, source revision, exact project Unity version, build targets and hosted workflow integration. Keep auto-build disabled until the owner-approved scope/budget and initial validation are established. Record actual target IDs and results in the release matrix. Store signing credentials only through secure platform facilities if a selected target requires them; successful desktop publication is not proof of mobile/IL2CPP or two-device behavior.

## References

- [Unity service-account creation and authentication](https://docs.unity.com/en-us/services-web-apis/service-account-auth)
- [Build Automation API](https://docs.unity.com/en-us/oas-build-automation-client/3.0.0)
- [Connect source control](https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system)

Access is not yet configured or verified. Creating a secret does not itself configure Unity Cloud or prove a build succeeds.
