# Railway Miden SDK publish-graph repair — 2026-11-28

Failed8c7a3148builds: WEB6a4425b89, WEB74f31889b, WEB990a5c2e1. Selected build logs show failure at the added unconditional install-miden-sdk.shstep, after publishing API hosts with different provider graphs. No runtime config, secrets or shared backend were changed by diagnosis.

Invariant: each actual shipping provider assembly has its complete required SDK in published output; an API without that provider has no such SDK dependency. The existing package-provider-sdk-bridges.showns this boundary. Miden is enrolled there, and7unconditional Docker install steps are removed. Missing SDK assets still fail when the Miden assembly is present. No second runtime path or silent substitute is introduced.

Qualification pending: publish the real Miden consumer and real WEB6API into the Docker-owned/app/publish on isolated runners using pinned Node24.19.0, run the same canonical packager, verify actual Miden exports for the provider graph and absence for WEB6. Native52-check/lifecycle evidence46106768815is preserved; no backend rerun needed for this packaging change. Owned output is removed on success/failure; no laptop builds/downloads/environments.

## Qualified correction

CI46113637170passed2/2actual publish graphs on the pinned Docker Node version, including both ownership-checked output cleanup steps. Earlier46113155793had already passed publish/install and failed only directory removal permissions; corrected root-owned-parent authority is now verified. No native ledger/check/lifecycle cases were repeated. Source rollback is unnecessary; corrected-source Railway rollout and all7service outcomes remain to verify.
