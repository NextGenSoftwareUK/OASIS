# GUN CQL MVP

The standard GUN relay returned HTTP 404 for the graph GET route the
general .NET provider expects. The dedicated teaching companion uses
the real `gun@0.2020.1241` SDK and Radisk directly. It reads the existing
`oasis.holons/{id}` layout, validates the native soul and deletion flag,
and projects only public live payloads without embedded children.

Five provider controls and one native sidecar CQL test passed with zero
skips at Core `472018f5c5df368b14ce4541045cbb6c0b95c790`. The real
Windows source returned 3/2/0 across restart, denied private rows and
rejected a profile bound to another provider. Controlled and native TRX
SHA-256 values are
`aa5e24cbb08d67b0570fdcd13341384b6a52300542443c655ad377d1e9075ffc`
and `2b1ea8c349e831e6948c3dfd423415a3e23c4f61aadbf0fbb6e87d01698d7f14`.

[Companion and verification](../../../Providers/Other/NextGenSoftware.OASIS.API.Providers.GUNOASIS/CqlTeachingCompanion/README.md).

The native companion also passed in the pinned WEB4 Linux Node 24.19.0
image. [Linux qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42236158067/job/127954184240).

The packaged WEB4 companion also passed Linux native process identity and
restart checks. The image-stage job verified both the default and opt-in
packages. [Native job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42238165351/job/127961168696) ·
[Package job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42238165351/job/127961168833).

Opt-in WEB4 admission passed nine combined GUN and OrbitDB checks and
nine ENS regressions at Core `10e464dcf58f4624a27e19f12738978ecf0fcb07`.
Live acceptance needs a co-located sidecar, protected reader and approved
persistent teaching data. General GUN CRUD and peer replication remain
open. LocalFile remains first for live rollout.

[Shared WEB4 runtime settings](ORBITDB_CQL_MVP.md#web4-runtime-settings).

On 6 November 2026 the retained GUN fixture passed local WEB4 HTTP with
MongoDB 8.2 at parent `9559c75f0` and Core
`c2a8b2303264bd159ac55d71b684c712647c6d3b`. Native reads returned 3/2
and federated reads returned 5/4. The foreign actor saw zero private rows
and three public rows. The two SDK-acknowledged private copies were removed
after exact ownership and count checks. Both owned services stopped and the
GUN fixture descriptor was unchanged. TRX SHA-256
`c2a9f0382fa5b3ecf3ece404471e9a0c39ad82bcb1695356c50eb7b0c48238e7`.

## Hosted preparation on the recovered laptop

The original `LocalState/COSMIC/LocalGunCqlQualification` and
`LocalState/COSMIC/LocalOrbitDbCqlQualification` directories are absent here.
Their original native and local HTTP evidence remains accepted and unchanged.
The paths, versions and historical sizes are recorded in
[the recovery ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).

The image workflow now accepts `teaching_sidecars=true`, which supplies
`OASIS_INCLUDE_TEACHING_SIDECARS=true` to the existing Docker stage.
Enabled images use the distinct `<commit>-teaching` tag. The default profile
keeps its ordinary commit tag. Three tests execute the shipping naming step
for both profiles and reject invalid selections. Run:
`python -B -m unittest discover -s Scripts/Sandbox -p test_teaching_image_workflow.py`.
They reuse existing Python/PyYAML and Git Bash; no dependencies were installed.

The full scoped image, persistent hosted source and live acceptance remain open.
Use the existing service and volume. A newly provisioned hosted teaching source
must have its own native identity, write intent and acknowledgement, with an
independent native/restart qualification before enrollment. Do not represent
it as recovery of the old laptop fixture or rerun accepted cohorts.

The scoped hosted candidate is parent
`ce654d0ea468f4c5c7db25df071b97c65abdfaa5`, Core
`f06255216c07593896c054e98eeecc32c33da5e4`, published in both repositories
on `work/cosmic-teaching-release-candidate`. The manifest/gitlink check passed,
both remote SHAs were verified, and the exact candidate runtime passed all
three process-identity/readiness controls. It excludes PouchDB and other
unfinished provider registrations. The production source-link correction for
the subscription protocol test project is included.
[Full enabled-image qualification 42339365263](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42339365263)
is running with `teaching_sidecars=true`. No live settings or source data
were changed by this preparation.

Image run42339365263 failed at CqlV1Compiler provider-list syntax during
scoped assembly. Corrected parent9c6e93003edde4ca28759b455b38e7371184fa1d
retains Coref0625521. Exact candidate C# syntax and manifest checks pass.
[Corrected image qualification42339962522](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42339962522)
is queued with the enabled native profile. No live mutation occurred.

Scripts/Sandbox/hosted-teaching-source.cjs now owns one-time source creation
and separate verification. It fsyncs an exclusive intent before native writes,
refuses existing directories and retains uncertain outcomes. Verification
checks all three public records, the private row, tombstone and absent key
against the original plan. Six controlled lifecycle tests pass, including
concurrent creators and a simulated uncertain write. These are helper controls;
real hosted native/restart acceptance remains open. Run
`node --test Scripts/Sandbox/test_hosted_teaching_source.cjs`.

Corrected image42339962522 passed with teaching runtimes explicitly enabled.
Published image: `ghcr.io/nextgensoftwareuk/oasis-web4@sha256:5e68ea554ccf46556e65e706fe3217b4a56463bb2ffbef97fea14f005c13a327`.
The single build flag was staged without deployment after baseline checks.
Development advanced to9c6e93003edde4ca28759b455b38e7371184fa1d.
Railway job ac51f1c7-693a-4136-b0ed-46c8e72843ea is queued. Runtime
activation flags remain off until the separate native source qualification.
Immutable plans are retained under LocalState/COSMIC/HostedTeachingSourceCurrent.
Deployment-bound readiness visitors are admitted; all25 response-evidence
checks passed including both new provider cases. No live query acceptance yet.

## Guarded native provisioning command

`Scripts/Sandbox/Provision-HostedTeachingSource.py` performs exactly one
explicit `create` or `verify` operation for GUNOASIS or OrbitDBOASIS.
Set COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT and COSMIC_CQL_REVIEWED_RELEASE_REF
to the qualified release, and COSMIC_CQL_REVIEWED_INSTANCE_IDS to its one
observed active replica. Pass the same --deployment-id and --instance-id
to the command. It reuses the existing release and writable-mount guards.
Both native reader flags must remain off while provisioning.

The command reads the immutable provider source-plan.json, reserves a
local operation intent, checks deployment/replica identity inside Node,
and invokes the tracked native helper through structured SSH arguments.
Each outcome is retained with DPAPI protection. A timeout, missing receipt,
failed native operation or existing intent requires read-only diagnosis;
never resend the create operation. `verify` runs in a separate native
process and must pass before activating the managed reader.
Four command-contract tests passed, including wrong deployment, active
reader, mismatched/ambiguous receipts and the successful command envelope.
Run `python -B -m unittest discover -s Scripts/Sandbox -p test_hosted_teaching_provisioning.py`.
These checks use controlled local processes; real hosted native acceptance remains open.
