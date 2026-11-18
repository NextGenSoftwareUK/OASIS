# Dev API deployment watch policy

WEB4–WEB10 publish individual `.csproj` files, not the root solution. A change that
only registers tests in `The OASIS.sln` must therefore not enqueue seven API builds.

The reviewed dev policy is `Scripts/railway_dev_watch_policy.json`:

```json
["**", "!/The OASIS.sln"]
```

All other paths remain watched, including API code, provider sources, submodule
pins, DNA, Dockerfiles and runtime scripts. This deliberately excludes just one
root file. It does not disable GitHub source/solution validation.

Railway documents ordered inclusion and negation in its
[watch-path configuration](https://docs.railway.com/builds/build-configuration).

Run `python -B Scripts/validate_railway_dev_watch_policy.py` before applying it.
The guard rejects solution references in any of the seven shipping Dockerfiles
or publication commands that no longer name a project directly. If shipping
builds start consuming the root solution, remove/review the exclusion first.

`Scripts/Set-RailwayDevWatchPolicy.ps1` inspects the linked OASIS project's seven
dev services. Add `-Apply` to save the policy. It uses the existing CLI secret
store, preflights all targets, refuses to replace another intentional filter,
verifies each saved value and stops if deployment identity changes during a write.
Its output contains selected watch settings and deployment IDs only.

The operation does not change source, image, DNA or autodeploy enablement. It
does not target staging or production. Saved server-side watch settings are
configuration evidence; an actual skipped deployment can only be accepted when
a later genuine solution-only push is observed. Never manufacture an empty or
test-only Development push to obtain that observation.
