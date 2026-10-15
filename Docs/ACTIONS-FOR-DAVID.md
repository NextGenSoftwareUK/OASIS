# Actions for David

For the current release-gate and OurWorld Cloud owner setup, use [What I need from David](Devs/OWNER_SETUP_CHECKLIST.md), updated 15 October 2026. The older task list below covers separate work and is not the current gate checklist.

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

## 9. Decisions from the HoloNET testing: done

1. **Synchronous methods now wait for and return the result** (you chose this, 2026-10-14). 66 methods; events still fire.
   Behaviour change for anyone relying on the old immediate return; noted in the CHANGELOG for 4.1.0.
2. **Holochain capability-grants bug**: already reported upstream
   ([holochain/holochain#5950](https://github.com/holochain/holochain/issues/5950)) and fixed in **Holochain 0.7.1**
   (released 2026-10-14). HoloNET's workaround is correct on 0.7.0 and 0.7.1.

---

## 10. Holochain 0.7.1: hApps rebuilt (informational, done)

HoloNET supports **both 0.7.0 and 0.7.1** (96/96 live tests pass on each). The OASIS hApp's **main now builds for
0.7.1** (hdk 0.7.1 / hdi 0.8.1). A 0.7.0 conductor needs the tag `holochain-0.7.0`, because the two builds aren't
interchangeable ("callback has invalid parameters").

## Status: HoloNET test coverage (measured 2026-10-14)

Not 100% yet. Honest numbers:

- **93 automated tests, all passing live against a real Holochain 0.7.0 conductor** on a fresh sandbox
  (72 run offline; the 21 live ones report as **skipped** without a conductor, instead of silently "passing" as before).
- **HoloNET.Client line coverage: 54.9%** (branches 49.6%) with the conductor; **5.2%** offline-only.
- Writing tests for untested paths found **nine real bugs**, now fixed in `holochain-client-csharp` `main` (`9109f80`, `9691416`),
  so they'll ship in 4.1.0:
  `app_info` rejected by 0.7; `app_info` responses crashing; `list_app_interfaces` crashing; the synchronous
  `CallZomeFunction` always returning null; **every HoloNET client sharing one DNA** (creating one client overwrote
  the others' settings); a thread-safety crash in shared signing credentials; `AddAgentInfoAsync` never completing; no way to add admin interfaces;
  and capability grants always listing empty (that one is a **Holochain 0.7.0 conductor bug**, worked around in HoloNET).
- Not covered by design: countersigning and `GetCompatibleCells` (only in Holochain's `unstable-*` builds).
- No Unity test project for HoloNET yet; the TestHarness is a manual demo app.

Claude is continuing to raise coverage; progress is in `holochain-client-csharp/CHANGELOG.md`.
