# Reusable protocol demo environments

27 October 2026. User explicitly authorized creating missing environments.
Both environments below are fresh synthetic demo state, not recovery of missing
historical keys/data. Existing runtime/container/volume resources were reused.

| Resource | Persistent identity/data | Verified state |
|---|---|---|
| IPFS Kubo0.43.1 | `LocalState/COSMIC/IPFSCurrent/repo`, protected storage-key.dpapi, environment.json | Created once; repeat initializer reused peer/key. Offline RPC version and peer identity pass. Daemon stopped. Initially7,750B. |
| SOLID CSS | Retained oasis-solid/image58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a/volume oasis-solid-data; fresh Pod oasis-cosmic-cql | New account/Pod/client and protected storage key created. Issuer discovery and DPoP authenticated ACL read pass. Clean stopped state exited143/notOOM. |
| SOLID client state | `LocalState/COSMIC/SolidCurrent`, protected account/password/login/Pod/client/key files and environment.json | Initially8,550B; repeat initializer preserved protected key. No secret material belongs in Git/docs/output. |

Paths are relative to `C:/Source/OASIS-cosmic`. Raw logs/TRX/readiness summaries
go in the worktree's `TestResults/IPFSCurrent` or `TestResults/SolidCurrent`.
Persistent credentials, repository data and unresolved journals stay in ignored
LocalState so build cleanup cannot erase them again.

## Reuse commands

```powershell
& Scripts/TestHosts/Initialize-CosmicIpfsEnvironment.ps1
& Scripts/TestHosts/Test-CosmicIpfsEnvironment.ps1

& Scripts/TestHosts/Initialize-CosmicSolidEnvironment.ps1
# After the bounded cached provider/integration build; preserves earlier reports:
& Scripts/TestHosts/Run-ReusableSolidReadiness.ps1 -RequireCleanShutdown
```

Initializers refuse unknown/mismatched identities and unresolved provisioning.
They never download runtimes, replace an existing storage key or recreate a Pod
to escape a failed outcome. Provisioning responses are DPAPI protected before
the next step; interrupted mutations require inspection, not blind replay.

IPFS uses only loopback18547/18548/18549, empty bootstrap, disabled mDNS and
offline daemon mode. Runtime SHA256:
FD0D46DF76F455D938713C98C7FCA5FA117B74C934C9FC0D37730FDB33313651.
SOLID reuses localhost13000 and the retained server/data; its new namespace does
not overwrite the old Pod or claim to decrypt the old records.

## Evidence and remaining work

Six setup/runner scripts parse; IPFS create/reuse/native readiness pass; SOLID
provider/integration builds and one actual cold discovery/authenticated ACL read
pass with no skips. Socket admission11,138ms; stop1,114ms. Native test6seconds.

| Evidence relative to TestResults | SHA256 |
|---|---|
| IPFSCurrent/readiness.json | AA77E228A46F632A6519CFD668C9F4A25C524B3F01DFFF95024E0F115A722F78 |
| SolidCurrent/solid-ready-read-current.trx | FA2BFD72AEB1C440A1CF91931055F4EF613CCB6C766AA68C821432C367B6BF0E |

These are environment/readiness proofs, not52/52 storage or CQL certification.
Next: IPFS V2 storage conformance on this fresh owned repository, then CQL
admission; SOLID native record/CQL verification on its new namespace follows.
Full record runners still need their fixture namespace/configuration reviewed
for the new state; do not run old hardcoded public/old-Pod fixtures blindly.
Solana's original unavailable history remains a separate preserved issue.

Do not delete these state directories while work continues. After fixture review,
stop the exact owned process/container and remove only owned disposable data.
Never delete the shared SOLID volume or assume deleting Windows client files
removes its server-side account/Pod. See the resource ledger for owners/lifecycle.
