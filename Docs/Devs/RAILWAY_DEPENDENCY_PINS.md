# Railway dependency pins for WEB4-WEB10

Railway builds WEB4 through WEB10 from the OASIS parent repository. Several required repositories are private submodules, and Railway's source checkout does not populate them. The Docker build therefore clones those repositories explicitly.

## The invariant

[`Docker/oasis-dependency-versions.env`](../../Docker/oasis-dependency-versions.env) is the only authoritative set of dependency revisions for all seven Railway services. Every revision is a full commit SHA so one deployment always builds the same source graph.

The WEB4-WEB10 Dockerfiles must:

1. Copy the shared manifest.
2. Source it when cloning NextGenSoftware-Libraries and holochain-client-csharp.
3. Invoke `Docker/clone-pinned-oasis-dependencies.sh` for the private OASIS repositories.
4. Contain no dependency commit SHAs of their own.

On `Development`, the seven private repository values in the manifest must exactly match the parent repository's gitlinks. This makes the locally tested checkout and the Railway checkout identical.

## Updating a dependency

When a deployed change lands in a submodule:

1. Advance the submodule pointer in the OASIS parent repository to the tested commit.
2. Change the matching value once in `Docker/oasis-dependency-versions.env`.
3. If NextGenSoftware-Libraries or holochain-client-csharp changes, update its manifest value. Update both together when their transport contract changes.
4. Run:

   ```bash
   python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks
   ```

5. Publish all affected services locally. For a shared API or transport change, publish WEB4-WEB10.
6. Commit the submodule pointer and manifest change together.
7. After pushing `Development`, wait for the Railway deployment to succeed and verify the hosted health or Swagger endpoint.

Do not point a Docker build at a moving branch, use `git clone --depth 1` without a checkout SHA, or add a service-specific fallback revision. Those recreate the version-skew failure this policy prevents.

## Enforcement

`Scripts/validate_railway_dependency_manifest.py` checks that:

- all required values exist and use full lowercase commit SHAs;
- all seven Dockerfiles consume the shared manifest and clone script;
- no Dockerfile contains a duplicated commit pin;
- with `--require-gitlinks`, every private pin equals the parent gitlink.

The `solution-integrity` CI job runs the structural check on every supported branch. It additionally enforces gitlink equality on `Development` pushes and pull requests targeting `Development`. A submodule-only update therefore cannot merge into the deployment branch until its manifest is updated in the same change.

The existing `submodule-sync` workflow may open a pointer-update pull request when a submodule's `Development` branch advances. That pull request is expected to fail this policy check until the manifest is deliberately updated and the service matrix has been verified. This makes deployment promotion an explicit reviewed action.
