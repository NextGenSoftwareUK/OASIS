# GitHub Token Setup for Gist Integration

To enable automatic contract import into Solana Playground via GitHub Gist, you need a GitHub Personal Access Token.

---

## 🎯 Why Do I Need This?

GitHub Gist integration allows:
- ✅ **Auto-upload** contracts to GitHub Gist
- ✅ **Easy sharing** - Get a shareable URL
- ✅ **Solana Playground import** - Import Gist directly into Playground
- ✅ **No manual file transfers** - Seamless workflow

**Without token:** Users must download ZIP and manually import  
**With token:** One-click upload → Auto-import into Playground

---

## 📝 Step-by-Step: Get Your GitHub Token

### Step 1: Go to GitHub Settings

1. Log in to GitHub: https://github.com
2. Click your profile picture (top right)
3. Click **"Settings"**

### Step 2: Generate New Token

1. Scroll down to **"Developer settings"** (bottom left)
2. Click **"Personal access tokens"**
3. Click **"Tokens (classic)"**
4. Click **"Generate new token"** → **"Generate new token (classic)"**

### Step 3: Configure Token

**Name:** `AssetRail Contract Generator` (or any name you like)

**Expiration:** Choose your preference
- 30 days (recommended for testing)
- 90 days
- No expiration (for production)

**Scopes:** Select ONLY this permission:
- ✅ **`gist`** - Create gists

**Leave all other checkboxes unchecked!**

### Step 4: Generate and Copy

1. Scroll down and click **"Generate token"**
2. **IMPORTANT:** Copy the token immediately!
3. It looks like: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
4. You won't be able to see it again!

---

## 🔧 Add Token to Your App

### Option 1: Environment Variable (Recommended)

```bash
# In your contract-generator-ui directory
cd /Volumes/Storage/QS_Asset_Rail/apps/contract-generator-ui

# Create or edit .env.local
echo "NEXT_PUBLIC_GITHUB_TOKEN=ghp_your_token_here" >> .env.local
```

### Option 2: Manual Edit

Create or edit `.env.local`:

```env
# Smart Contract Generator API
NEXT_PUBLIC_API_URL=http://localhost:5000

# GitHub Personal Access Token for Gist integration
NEXT_PUBLIC_GITHUB_TOKEN=ghp_your_token_here
```

**⚠️ Replace `ghp_your_token_here` with your actual token!**

---

## 🧪 Test the Integration

### Step 1: Restart the UI

```bash
# Stop the dev server (Ctrl+C)
# Then restart
npm run dev
```

### Step 2: Generate a Contract

1. Go to http://localhost:3001
2. Click "Start with Template"
3. Click "Generate Contract"

### Step 3: Use Gist Integration

You should now see:
- 🎉 **"Share via Gist"** button (primary, highlighted)
- This replaces or supplements the "Open Playground" button

### Step 4: Click "Share via Gist"

1. Click the button
2. Wait 2-3 seconds (uploading to GitHub)
3. A new tab opens → Solana Playground
4. Instructions modal shows the Gist URL
5. Click to copy Gist URL
6. In Playground: Import → From GitHub → Paste URL
7. Your contract loads automatically! ✨

---

## 🔒 Security Best Practices

### ✅ DO:
- Keep token secret (never share publicly)
- Use `.env.local` (ignored by git)
- Create token with minimal permissions (only `gist`)
- Set expiration date for tokens
- Revoke tokens when not needed

### ❌ DON'T:
- Commit tokens to git
- Share tokens in screenshots
- Give unnecessary permissions
- Use same token for multiple apps

---

## 🎯 Troubleshooting

### Token Not Working

**Error:** `GitHub token not configured`

**Solution:** 
```bash
# Check if .env.local exists
cat .env.local

# Should show:
# NEXT_PUBLIC_GITHUB_TOKEN=ghp_...

# If not, create it:
echo "NEXT_PUBLIC_GITHUB_TOKEN=your_token" > .env.local

# Restart dev server
npm run dev
```

---

### API Error 401

**Error:** `GitHub API error: 401`

**Causes:**
- Token expired
- Token invalid
- Token doesn't have `gist` permission

**Solution:**
1. Generate new token with `gist` permission
2. Update `.env.local`
3. Restart dev server

---

### Gist Button Not Showing

**Check:**
1. Is token in `.env.local`?
2. Did you restart dev server?
3. Open browser console (F12) and check for errors

---

### "Failed to create Gist"

**Possible causes:**
- No internet connection
- GitHub API rate limit (60 requests/hour without token, 5000 with token)
- Token revoked
- GitHub service issues

**Solution:**
- Check internet connection
- Wait an hour if rate limited
- Generate new token
- Try again later

---

## 🔄 Alternative: No Token Required

If you don't want to set up a GitHub token:

**Manual workflow still works:**
1. Generate contract ✅
2. Click "Download ZIP" ✅
3. Click "Open Playground" ✅
4. Manually import ZIP in Playground ✅

**This is perfectly fine!** The GitHub Gist integration is optional for convenience.

---

## 📊 Comparison

| Feature | Without Token | With Token |
|---------|---------------|------------|
| **Generate Contract** | ✅ | ✅ |
| **Monaco Editor** | ✅ | ✅ |
| **Download ZIP** | ✅ | ✅ |
| **Open Playground** | ✅ Manual import | ✅ Auto-import |
| **Share Contract** | ❌ | ✅ Via Gist URL |
| **Steps to Test** | 5 steps | 2 steps |

---

## 🎉 Success!

Once configured, you'll see:

```
┌─────────────────────────────────────┐
│ Contract Generated Successfully!    │
│                                     │
│ ┌──────────┐  ┌──────────┐         │
│ │ Share    │  │   Open   │         │
│ │via Gist  │  │Playground│         │
│ │(best!)   │  │(manual)  │         │
│ └──────────┘  └──────────┘         │
│                                     │
│ 💡 Share via Gist uploads to       │
│ GitHub and enables auto-import!    │
└─────────────────────────────────────┘
```

---

## 📝 Example .env.local

```env
# Smart Contract Generator API
NEXT_PUBLIC_API_URL=http://localhost:5000

# GitHub Token for Gist Integration
# Get your token: https://github.com/settings/tokens
# Permissions needed: gist
NEXT_PUBLIC_GITHUB_TOKEN=ghp_1234567890abcdefghijklmnopqrstuvwxyzAB

# Optional: Solana RPC (if needed)
# NEXT_PUBLIC_SOLANA_RPC_URL=https://api.devnet.solana.com
```

---

## 🔗 Quick Links

- **Generate Token:** https://github.com/settings/tokens/new
- **GitHub Gist Docs:** https://docs.github.com/en/rest/gists
- **Solana Playground:** https://beta.solpg.io/

---

**Questions?** Check the main README or open an issue!

**Ready to try it?** Generate a token and add it to `.env.local`! 🚀


