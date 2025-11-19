# Push TimoRides to GitHub (timo-org)

**Quick guide to push code to timo-org repositories**

---

## 📦 Repositories

1. **Backend:** `timo-org/ride-scheduler-be`
2. **Android:** `timo-org/Timo-Android-App`

---

## 🚀 Quick Push (Using Script)

### Push Backend

```bash
cd /Volumes/Storage/OASIS_CLEAN
./TimoRides/push-to-timo.sh backend main
```

### Push Android App

```bash
cd /Volumes/Storage/OASIS_CLEAN
./TimoRides/push-to-timo.sh android main
```

---

## 📝 Manual Steps (If Script Fails)

### 1. Commit All Changes First

```bash
cd /Volumes/Storage/OASIS_CLEAN
git add TimoRides/
git commit -m "Update TimoRides: backend improvements, Android fixes, testing updates"
```

### 2. Push Backend

```bash
# Create subtree split
git subtree split --prefix=TimoRides/ride-scheduler-be -b backend-split

# Push to timo-org
git push git@github.com:timo-org/ride-scheduler-be.git backend-split:main

# Clean up
git branch -D backend-split
```

### 3. Push Android App

```bash
# Create subtree split
git subtree split --prefix=TimoRides/Timo-Android-App -b android-split

# Push to timo-org
git push git@github.com:timo-org/Timo-Android-App.git android-split:main

# Clean up
git branch -D android-split
```

---

## ✅ Verify Push

1. Check GitHub:
   - https://github.com/timo-org/ride-scheduler-be
   - https://github.com/timo-org/Timo-Android-App

2. Verify latest commits are visible

---

## 🔐 SSH Key Setup (If Needed)

If you get "Permission denied" errors:

```bash
# Test SSH connection
ssh -T git@github.com

# If it fails, add your SSH key:
ssh-add ~/.ssh/id_ed25519
# (Enter passphrase when prompted)
```

---

## 📋 Pre-Push Checklist

- [ ] All code changes committed
- [ ] Backend tests passing
- [ ] Android app builds successfully
- [ ] No sensitive data in code (API keys, passwords)
- [ ] `.env` files not included (should be in `.gitignore`)
- [ ] Documentation updated

---

## 🚨 Important Notes

1. **Never push `.env` files** - They contain secrets
2. **Use subtree split** - Keeps repos clean (no monorepo history)
3. **Test before pushing** - Ensure everything builds/runs
4. **Commit message** - Use descriptive messages

---

**Ready to push!** 🚀

