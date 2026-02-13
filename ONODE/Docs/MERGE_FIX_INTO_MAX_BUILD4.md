# Merge fix/nft-send-after-mint into max-build4

Use this when you want to bring the NFT send-after-mint (and related) work into your main branch **max-build4**.

---

## 1. Commit your current work (required)

Git will not switch branches while you have local changes that would be overwritten. Commit everything you want to include in the merge:

```bash
cd /Users/maxgershfield/OASIS_CLEAN

# See what’s changed
git status

# Add the files you want in this merge (e.g. everything, or pick specific paths)
git add -A
# Or only ONODE + docker + Providers/Blockchain for NFT/Solana:
# git add ONODE/ docker/ "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/"

# Commit on fix/nft-send-after-mint
git commit -m "Telegram mint GIF/video, Docker health check, JWT login fix, merge prep"
```

If you prefer to leave some changes uncommitted, use `git stash` before step 2 and `git stash pop` after the merge (on max-build4).

---

## 2. Switch to max-build4 and merge

```bash
git checkout max-build4
git merge fix/nft-send-after-mint -m "Merge fix/nft-send-after-mint into max-build4"
```

If there are conflicts, fix them in the reported files, then:

```bash
git add <resolved-files>
git commit --no-edit
```

---

## 3. Push (optional)

```bash
git push origin max-build4
```

(Use your actual remote name if it’s not `origin`.)

---

## Summary

| Step | Command |
|------|--------|
| Commit on fix branch | `git add -A && git commit -m "Your message"` |
| Switch to main | `git checkout max-build4` |
| Merge | `git merge fix/nft-send-after-mint -m "Merge fix/nft-send-after-mint into max-build4"` |
| Push | `git push origin max-build4` |

After this, **max-build4** will contain all commits from **fix/nft-send-after-mint**. You can keep or delete the fix branch; building/pushing the Docker image from **max-build4** is then safe.
