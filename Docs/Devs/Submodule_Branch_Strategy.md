# Submodule Branch Strategy

How the OASIS superproject and its eight submodules stay aligned, why it drifts if left
alone, and what to run when it does.

---

## The rule

```
OASIS master       pins every submodule at that submodule's  main
OASIS Development  pins every submodule at that submodule's  Development
```

`master` deploys to Azure. A `master` pinned at a Development-only commit ships unreleased
code, and a fresh clone of `master` then does not reproduce what `main` contains.

## Why it does not hold automatically

**Git submodules pin commits, not branches.** There is no native mechanism that makes
"master points at each submodule's main" true. A submodule pointer is a commit SHA recorded
in the superproject's tree; nothing re-evaluates it when the submodule moves.

The `branch = ` line in `.gitmodules` does **not** change that. It is read by exactly one
command — `git submodule update --remote` — and is ignored everywhere else. So the rule is
convention plus tooling, and it drifts unless maintained.

This is also why `.gitmodules` differs between the two branches, and must:

| Branch | `.gitmodules` |
|---|---|
| `master` | 8 × `branch = main` |
| `Development` | 8 × `branch = Development` |

That difference is deliberate. The branches can never converge to an identical tree, so
"master and Development are in sync" means *master contains all of Development's work*, not
*the branch tips are equal*.

---

## The three layers that keep it true

### 1. Make the right thing easy — `git submodule update --remote`

With `.gitmodules` correct on each branch, moving every pointer to the right branch tip is
one command:

```bash
git submodule update --remote --merge
git commit -am "chore(submodules): bump pointers to branch tips"
```

On `master` that pulls each submodule's `main`; on `Development`, each `Development`. No
per-submodule checkout.

This is why a wrong `branch = ` value is dangerous rather than cosmetic: it silently sends
this command to the wrong branch.

### 2. Make the wrong thing fail — `Scripts/check_submodule_branches.py`

Runs in CI in the `solution-integrity` job of `.github/workflows/ci-cd.yml`, on every push
and PR to `main`, `master` and `Development`.

```bash
# the branch currently checked out
python3 Scripts/check_submodule_branches.py

# any ref
python3 Scripts/check_submodule_branches.py origin/master

# a raw SHA carries no branch name, so name the rule
python3 Scripts/check_submodule_branches.py 71be1c048 Development
```

It verifies two things per submodule:

- the **recorded pointer** equals the tip of the required branch
- the **declared `branch = `** in `.gitmodules` names the required branch

and distinguishes the failure modes, because they need different fixes:

| Message | Meaning |
|---|---|
| `pinned X is behind Y — bump the pointer` | Right branch, stale. Run `--remote`. |
| `pinned X is NOT on Y — this ships unreleased code` | Wrong branch. The serious one. |
| `.gitmodules says branch = A, expected B` | Config would drift on the next `--remote`. |
| `submodule not initialised` | Checkout problem, not a rule breach. |

Exit 0 = compliant, 1 = not.

### 3. Make it self-maintaining — `.github/workflows/submodule-sync.yml`

Runs 07:00 UTC Mondays, and on demand via **Actions → Submodule Sync → Run workflow**
(optionally for a single branch).

For each of `master` and `Development` it runs `git submodule update --remote`, and if any
pointer moved, opens a PR. It opens a PR rather than pushing, because a pointer bump on
`master` is a release and should be reviewed as one. The branch check runs on that PR.

> **Setup note:** the workflow prefers a `SUBMODULE_SYNC_TOKEN` secret and falls back to
> `GITHUB_TOKEN`. PRs created with `GITHUB_TOKEN` do **not** trigger workflow runs, so with
> the fallback the branch check will not run on the sync PR. Add a repo-scoped PAT as
> `SUBMODULE_SYNC_TOKEN` to get CI on those PRs.

### Still to do: branch protection

The CI check is **detective** — it fails the build *after* the push lands. To make a
violation un-mergeable, enable on `master` and `Development`:

**Settings → Branches → Branch protection → Require status checks to pass → `Solution Integrity`**

Without this, a red PR can still be merged.

---

## Everyday tasks

**Released work in a submodule; want it on OASIS master**

```bash
# 1. get it onto the submodule's main
git -C <submodule> push origin Development:main      # if a clean fast-forward

# 2. bump the superproject pointer
git checkout master
git submodule update --remote --merge
git commit -am "chore(submodules): bump <submodule>"
python3 Scripts/check_submodule_branches.py          # verify before pushing
```

**Check the current state without changing anything**

```bash
python3 Scripts/check_submodule_branches.py origin/master
python3 Scripts/check_submodule_branches.py origin/Development
```

**Merging Development into master**

`.gitmodules` will conflict every time — that is expected, since the two versions
deliberately differ. Resolve in master's favour:

```bash
git checkout --ours .gitmodules && git add .gitmodules
```

Submodule pointers may also conflict. Resolve each to the submodule's `main`:

```bash
git update-index --cacheinfo 160000,"$(git -C "<path>" rev-parse origin/main)","<path>"
```

Then run the check before committing.

---

## Gotchas found the hard way

**A worktree does not initialise submodules.** `git worktree add` leaves submodule
directories empty. Git run inside an empty submodule directory *walks up and silently
answers from the superproject*, so naive checks compare against the wrong repository and
report every submodule as violating. The check now detects this by comparing
`rev-parse --show-toplevel` against the path itself.

**Two submodule directories can hold csproj files with the same filename.** `dotnet sln`
keys on the filename, not the display name, so a leftover stub directory made three
solutions unparseable with a misleading "already exists in the Root solution folder".

**A submodule's `Development` may share no history with its `main`.** HoloNET-ORM's
`Development` was a single "Initial commit" holding only a README — cloning it produced no
source at all. Check `git merge-base origin/main origin/Development` before assuming a
merge is possible. That branch was replaced with `main`; the old tip is archived on that
repo at `archive/Development-initial-commit-5f4017f`.

**On Windows, `git show "ref:path"` gets mangled** by MSYS path conversion (`origin/master:.gitmodules`
becomes `origin\master;.gitmodules`). Set `MSYS_NO_PATHCONV=1` and `MSYS2_ARG_CONV_EXCL='*'`.

**Deep paths exceed the Windows limit.** OpenZeppelin contract paths break
`git worktree add` unless the worktree path is short and `core.longpaths` is set.

---

## The submodules

| Path | Repo |
|---|---|
| `HoloNET-ORM` | HoloNET-ORM |
| `STAR ODK` | STAR ODK |
| `WEB6` | OASIS-WEB6 |
| `OASIS Architecture/NextGenSoftware.OASIS.API.Core` | API.Core |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.Core` | ONODE.Core |
| `ONODE/ONODEManager` | ONODEManager |
| `OASIS Omniverse/OGEngineClient` | OGEngineClient |
| `OASIS Omniverse/OASIS Hub` | OASIS Hub |

---

## Worth reconsidering

Mirrored submodule branches are high-maintenance: every submodule change needs a pointer
bump in the superproject, on two branches. On 2026-09-05 alone this drifted three separate
ways — master pinned off `main` for two submodules, WEB6's own `Development` eleven commits
behind its `main`, and four `.gitmodules` entries on `Development` declaring `main`.

The lower-maintenance alternative is consuming stable submodules as NuGet packages instead.
The csprojs already support this — they use a `ProjectReference` when the submodule is
present and fall back to a `PackageReference` when it is not:

```xml
<ProjectReference Include="..\..\..\OASIS Architecture\...\NextGenSoftware.OASIS.API.Core.csproj"
                  Condition="Exists('...')" />
<PackageReference Include="NextGenSoftware.OASIS.API.Core" Version="2.0.0"
                  Condition="!Exists('...')" />
```

Narrowing submodules to the repos actively co-developed, and taking the rest as packages,
would remove most of this class of problem. That is an architectural decision, not a
maintenance task.
