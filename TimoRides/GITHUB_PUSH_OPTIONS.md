# Push to GitHub - Options (No SSH Passphrase Needed)

You have **3 options** to push without remembering your SSH passphrase:

---

## Option 1: Use HTTPS with Personal Access Token (Recommended)

### Step 1: Create GitHub Personal Access Token

1. Go to: https://github.com/settings/tokens
2. Click **"Generate new token"** → **"Generate new token (classic)"**
3. Name it: "TimoRides Push"
4. Select scope: ✅ **`repo`** (full control of private repositories)
5. Click **"Generate token"**
6. **Copy the token immediately** (you won't see it again!)

### Step 2: Use the HTTPS Push Script

```bash
cd /Volumes/Storage/OASIS_CLEAN

# Push backend (will prompt for token)
./TimoRides/push-to-timo-https.sh backend main

# Push Android (will prompt for token)
./TimoRides/push-to-timo-https.sh android main
```

**Or set token as environment variable:**
```bash
export GITHUB_TOKEN=your_token_here
./TimoRides/push-to-timo-https.sh backend main
./TimoRides/push-to-timo-https.sh android main
```

---

## Option 2: Generate New SSH Key (No Passphrase)

### Step 1: Generate New Key

```bash
ssh-keygen -t ed25519 -C "timo-github" -f ~/.ssh/id_ed25519_timo -N ""
```

**Note:** The `-N ""` means no passphrase

### Step 2: Add to GitHub

```bash
# Copy public key
cat ~/.ssh/id_ed25519_timo.pub
```

1. Go to: https://github.com/settings/keys
2. Click **"New SSH key"**
3. Paste the public key
4. Save

### Step 3: Update Push Script

Edit `push-to-timo.sh` to use the new key:

```bash
# Add to ~/.ssh/config
cat >> ~/.ssh/config <<EOF
Host github-timo
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_timo
EOF
```

Then use:
```bash
git push git@github-timo:timo-org/ride-scheduler-be.git ...
```

---

## Option 3: Use GitHub CLI (gh)

### Step 1: Install GitHub CLI

```bash
brew install gh
```

### Step 2: Login

```bash
gh auth login
```

### Step 3: Push Using gh

```bash
cd /Volumes/Storage/OASIS_CLEAN

# Create subtree split
git subtree split --prefix=TimoRides/ride-scheduler-be -b backend-split

# Push using gh
gh repo sync timo-org/ride-scheduler-be --source . --branch backend-split
```

---

## 🎯 Recommended: Option 1 (HTTPS Token)

**Why:**
- ✅ No passphrase needed
- ✅ Works immediately
- ✅ Secure (token can be revoked)
- ✅ Easy to use

**Steps:**
1. Get token from GitHub
2. Run: `./TimoRides/push-to-timo-https.sh backend main`
3. Paste token when prompted
4. Done!

---

## 🔐 Security Note

**Never commit tokens to git!**

- Use environment variables
- Or enter when prompted
- Or use a password manager

---

**Choose Option 1 for the fastest solution!** 🚀

