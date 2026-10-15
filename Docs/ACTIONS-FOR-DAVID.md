# Actions for David

Things only you can do: they need your phone, your accounts, admin rights or a decision.
Everything else is done or in progress. Last updated 2026-10-14.

Tick them off as you go; tell Claude when one is done and it will pick up the next step.

---

## 1. Publish HoloNET 4.1.0 to NuGet

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

## 2. Test on your phones

**Why:** everything mobile has passed on emulators/simulators only.

**Android:** plug in the phone with **USB debugging** on (Settings → Developer options) and accept the "Allow USB debugging" prompt.
Claude installs the 0.7 shared runtime and runs the bridge test; you'll only need to tap "approve" on one notification.

**iPhone:** needs a Mac (or a device farm). The simulator tests pass in CI.

## 3. Shrink the WSL disk (gets ~10+ GB back)

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

## 4. Check Vercel before deleting old branches

**Why:** Max's guides say **www.oasisweb4.com** deploys to Vercel from the OASIS **`max-build2`** branch.

1. In Vercel, open the oasisweb4.com project → Settings → Git. Note the repo and production branch.
2. If it's `NextGenSoftwareUK/OASIS` / `max-build2`: point it at the `oasisweb4.com` repo first (or tell Claude to).
3. Then tell Claude to delete the OASIS **`main`** and **`max-build2`** branches. Everything on `main` is backed up in the private
   `OASIS-main-archive` repo and its projects are split into their own private repos.

## 5. Decide: Max's newer oasisweb4 site

`main` had a newer version of the website (login, mobile menu fixes). It's in `OASIS-main-archive` and on the
`from-oasis-main` branch of `oasisweb4site`. Adopt it, or keep the current site?

## 6. Run one PowerShell script (permission was blocked for Claude)

Downloads the pinned iOS conductor release and checks its SHA-256s:
```bash
pwsh -File Scripts/fetch_holochain_ios_runtime.ps1
```
(from `C:\Source\OASIS`). Tell Claude if it fails.

## 7. Optional: Git LFS budget

The org's Git LFS budget is used up, so `OASIS-main-archive` holds LFS pointers rather than copies (the files stay in the OASIS repo's
LFS storage). Raise the budget in the org's billing settings if you want the archive self-contained.

## 8. Watch the upstream Holochain PRs

Offered to `holochain/android-service-runtime`; reply to any review comments, or tell Claude to:

- [#149](https://github.com/holochain/android-service-runtime/pull/149): client parcelers for 0.7
- [#150](https://github.com/holochain/android-service-runtime/pull/150): Tauri service plugin network config for 0.7
- [#151](https://github.com/holochain/android-service-runtime/pull/151): iOS support

---

## Status: HoloNET test coverage (measured 2026-10-14)

Not 100%. Honest numbers:

- **85 automated tests**: 71 offline (serialization, wire format, signing) + 14 live-conductor tests.
- **Offline line coverage of HoloNET.Client: 5.2%.** The live tests exercise far more (every admin/app API was verified against a
  real 0.7.0 conductor in October), but they only run with a conductor, so they aren't in that number yet.
- Until today the 14 live tests silently "passed" without a conductor; they now report as **skipped**.
- No Unity test project for HoloNET; the TestHarness is a manual demo app, not automated tests.
- Not covered by design: countersigning and `GetCompatibleCells` (only in Holochain's `unstable-*` builds, which we don't use).

Claude is working on raising this (see `holochain-client-csharp/AUDIT.md` / `CHANGELOG.md` for progress).
